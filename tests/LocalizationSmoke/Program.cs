using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using RotationSolver.Basic;
using RotationSolver.Basic.Localization;

var passed = 0;
void Check(bool success, string message)
{
    if (!success) throw new InvalidOperationException(message);
    passed++;
}

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
Loc.Language = UILanguage.Chinese;
Check(Loc.T("Show State Window") == "显示状态窗口", "New setting translation");
Check(Loc.T("First start tutorial") == "首次使用教程", "Tutorial translation");
Check(Loc.T("Lead developer") == "主要开发者", "Credits translation");
Check(Loc.T("Use Snarl and Challenge based on your HP and your familiar's HP (Crucible only)")!.Contains("使魔"), "New rotation settings");
Check(Loc.T("Unregistered player name") == "Unregistered player name", "Unknown text remains unchanged");
Check(Loc.T(null) == null && Loc.T("") == "", "Optional text remains optional");
Check(Loc.F($"Step {2} of {14}") == "第 2 步，共 14 步", "Formatted tutorial progress");
Check(Loc.F($" - Cast {1.25:F2}s") == " - 咏唱 1.25 秒", "Numeric precision survives translation");
Check(Loc.F($"Copied {"/rotation Auto"}") == "已复制 /rotation Auto", "Commands in messages are preserved");
var reusable = FormattableStringFactory.Create("Copied {0}", "Settings");
_ = Loc.F(reusable);
Check((string)reusable.GetArgument(0)! == "Settings", "Formatting does not mutate caller arguments");

var labels = new[] { "Enable", "Enable##action42", "Welcome###tutorial", "##internal-id" };
string Identity(string text) => text.Contains("###", StringComparison.Ordinal) ? text[(text.LastIndexOf("###", StringComparison.Ordinal) + 3)..] : text;
var chineseLabels = labels.Select(Loc.Label).ToArray();
Check(chineseLabels[0].StartsWith("启用", StringComparison.Ordinal), "Visible interactive label");
Check(Loc.Label("##internal-id") == "##internal-id", "Hidden ID is unchanged");
Loc.Language = UILanguage.English;
for (var i = 0; i < labels.Length; i++)
    Check(Identity(Loc.Label(labels[i])) == Identity(chineseLabels[i]), "Stable widget identity: " + labels[i]);
Check(Loc.T("Show State Window") == "Show State Window", "Switch back to English");
Check(Loc.F($"Step {2} of {14}") == "Step 2 of 14", "English formatted message");
Service.Config = new TestConfig();
Loc.ClientIsChinese = true;
Check(Loc.IsChinese, "Reset follows Chinese client");
Loc.ClientIsChinese = false;
Check(!Loc.IsChinese, "Automatic language follows English client");
Service.Config = new TestConfig { UILanguage = UILanguage.Chinese };
Check(Loc.IsChinese, "Restored configuration takes effect immediately");

using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("RotationSolver.Basic.Localization.zh-CN.json")!;
using var json = JsonDocument.Parse(stream);
var keys = new HashSet<string>(StringComparer.Ordinal);
var placeholders = new Regex(@"(?<!\{)\{(\d+)(?:,[^}:]+)?(?::[^}]+)?\}(?!\})");
foreach (var entry in json.RootElement.EnumerateObject())
{
    Check(keys.Add(entry.Name), "Duplicate translation key: " + entry.Name);
    var value = entry.Value.GetString()!;
    Check(!string.IsNullOrEmpty(value), "Empty translation: " + entry.Name);
    var sourceArgs = placeholders.Matches(entry.Name).Select(m => m.Groups[1].Value).Order().ToArray();
    var targetArgs = placeholders.Matches(value).Select(m => m.Groups[1].Value).Order().ToArray();
    Check(sourceArgs.SequenceEqual(targetArgs), "Changed format arguments: " + entry.Name);
    if (sourceArgs.Length > 0)
    {
        var sourceFormat = CompositeFormat.Parse(entry.Name);
        var targetFormat = CompositeFormat.Parse(value);
        Check(sourceFormat.MinimumArgumentCount == targetFormat.MinimumArgumentCount, "Invalid format: " + entry.Name);
    }
    var separator = entry.Name.IndexOf("##", StringComparison.Ordinal);
    if (separator >= 0)
        Check(value.EndsWith(entry.Name[separator..], StringComparison.Ordinal), "Changed ID suffix: " + entry.Name);
}
Console.WriteLine($"PASS: {passed} checks across {keys.Count} translations.");

// Only the configuration boundary is substituted. The production Loc.cs and embedded
// dictionary are compiled unchanged, so these tests do not require a running game.
namespace RotationSolver.Basic
{
    internal sealed class TestConfig
    {
        public UILanguage UILanguage { get; set; } = UILanguage.Auto;
    }

    internal static class Service
    {
        public static TestConfig Config { get; set; } = new();
    }
}
