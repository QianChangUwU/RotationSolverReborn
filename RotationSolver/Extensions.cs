using RotationSolver.Data;

namespace RotationSolver
{
	public static class CommandTypeExtensions
	{
		public static string ToStateString(this StateCommandType stateType)
		{
			var name = Loc.T(stateType.ToString());
			return stateType == StateCommandType.Auto || stateType == StateCommandType.TargetOnly
				? $"{name} ({DataCenter.TargetingType.GetDescription()})"
				: name;
		}

		public static string ToSpecialString(this SpecialCommandType specialType)
		{
			return Loc.T(specialType.ToString());
		}
	}
}