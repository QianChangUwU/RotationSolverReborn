using RotationSolver.Basic.Rotations.Duties;
using RotationSolver.Basic.Localization;

namespace RotationSolver.RebornRotations.Duty;

[Rotation("Beauty's Wicked Wiles", CombatType.PvE)]

internal class EmanationDefault : EmanationRotation
{
	public override void DisplayDutyStatus()
	{
		ImGui.Spacing();
		ImGui.Text(string.Format(Loc.T("VrilPvE Slotted: {0}"), VrilPvE.Info.IsOnSlot));
		ImGui.Text(string.Format(Loc.T("VrilPvE Charges: {0}"), VrilPvE.Cooldown.CurrentCharges));
		ImGui.Spacing();
		ImGui.Text(string.Format(Loc.T("VrilPvE_9345 Slotted: {0}"), VrilPvE_9345.Info.IsOnSlot));
		ImGui.Text(string.Format(Loc.T("VrilPvE_9345 Charges: {0}"), VrilPvE_9345.Cooldown.CurrentCharges));
		ImGui.Spacing();
	}

	#region Configs
	[RotationConfig(CombatType.PvE, Name = "Auto Use Vril")]
	public bool AllowVril2 { get; set; } = true;
	#endregion

	public override bool EmergencyAbility(IAction nextGCD, out IAction? act)
	{
		if (AllowVril2)
		{
			if (VrilPvE.Cooldown.CurrentCharges > 0)
			{
				if (VrilPvE.CanUse(out act, usedUp: true))
				{
					return true;
				}
			}

			if (VrilPvE_9345.Cooldown.CurrentCharges > 0)
			{
				if (VrilPvE_9345.CanUse(out act, usedUp: true))
				{
					return true;
				}
			}
		}

		return base.EmergencyAbility(nextGCD, out act);
	}
}
