using RotationSolver.Data;
using RotationSolver.Basic.Localization;

namespace RotationSolver
{
	public static class CommandTypeExtensions
	{
		public static string ToStateString(this StateCommandType stateType, JobRole role)
		{
			return stateType == StateCommandType.Auto || stateType == StateCommandType.TargetOnly
				? $"{Loc.T(stateType.ToString())} ({Loc.T(DataCenter.TargetingType.GetDescription())})"
				: Loc.T(stateType.ToString());
		}

		public static string ToSpecialString(this SpecialCommandType specialType, JobRole role)
		{
			return specialType.ToString();
		}
	}
}