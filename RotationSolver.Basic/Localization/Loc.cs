using Newtonsoft.Json;
using System.Reflection;

namespace RotationSolver.Basic.Localization;

/// <summary>
/// The language used for the user interface.
/// </summary>
public enum UILanguage
{
	/// <summary>
	/// Follow the game client language.
	/// </summary>
	[Description("Follow the client language")]
	Auto,

	/// <summary>
	/// Always use Chinese.
	/// </summary>
	[Description("Chinese")]
	Chinese,

	/// <summary>
	/// Always use English.
	/// </summary>
	[Description("English")]
	English,
}

/// <summary>
/// A simple dictionary-based localization helper. English strings are used as keys,
/// and translations are loaded from an embedded resource file.
/// </summary>
public static class Loc
{
	private static readonly Lock _lock = new();
	private static Dictionary<string, string>? _dict;
	private static bool _clientIsChinese = true;

	/// <summary>
	/// The language choice. Defaults to <see cref="UILanguage.Auto"/>.
	/// </summary>
	public static UILanguage Language
	{
		get => Service.Config.UILanguage;
		set => Service.Config.UILanguage = value;
	}

	/// <summary>
	/// Whether the game client is running in Chinese. Set by the plugin on initialization.
	/// </summary>
	public static bool ClientIsChinese
	{
		get => _clientIsChinese;
		set => _clientIsChinese = value;
	}

	/// <summary>
	/// Whether localized Chinese text should be used.
	/// </summary>
	public static bool IsChinese => Language switch
	{
		UILanguage.Chinese => true,
		UILanguage.English => false,
		_ => _clientIsChinese,
	};

	/// <summary>
	/// Translates the given English text into Chinese if available, otherwise returns the original text.
	/// </summary>
	public static string T(string en)
	{
		if (string.IsNullOrEmpty(en) || !IsChinese)
		{
			return en;
		}

		EnsureLoaded();
		return _dict!.TryGetValue(en, out var zh) ? zh : en;
	}

	private static void EnsureLoaded()
	{
		if (_dict != null)
		{
			return;
		}

		lock (_lock)
		{
			if (_dict != null)
			{
				return;
			}

			try
			{
				using var stream = Assembly.GetExecutingAssembly()
					.GetManifestResourceStream("RotationSolver.Basic.Localization.zh-CN.json");
				if (stream == null)
				{
					_dict = [];
					return;
				}

				using var reader = new StreamReader(stream);
				_dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(reader.ReadToEnd()) ?? [];
			}
			catch
			{
				_dict = [];
			}
		}
	}
}
