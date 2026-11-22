namespace RotationSolver.Commands
{
	public static class CommandTypeExtensions
	{
		public static string ToStateString(this StateCommandType stateType)
		{
			return stateType switch
			{
				StateCommandType.Auto => DataCenter.TargetingType.GetDescription(),
				StateCommandType.Manual => Loc.T("Manual select target") ?? string.Empty,
				_ => Loc.T(stateType.ToString()) ?? stateType.ToString(),
			};
		}

		public static string ToSpecialString(this SpecialCommandType specialType)
		{
			return Loc.T(specialType.ToString());
		}
	}
}