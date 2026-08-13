using RotationSolver.Basic.Rotations.Duties;
using RotationSolver.Basic.Localization;

namespace RotationSolver.RebornRotations.Duty;

[Rotation("Hardboiled Reborn", CombatType.PvE)]

internal class HardboiledDefault : HardboiledRotation
{
	public override void DisplayDutyStatus()
	{
		ImGui.Spacing();
		ImGui.Text(string.Format(Loc.T("ShootHardPvE Slotted: {0}"), ShootHardPvE.Info.IsOnSlot));
		ImGui.Text(string.Format(Loc.T("ShootHardPvE Charges: {0}"), ShootHardPvE.Cooldown.CurrentCharges));
		ImGui.Spacing();
		ImGui.Text(string.Format(Loc.T("ShootHarderPvE Slotted: {0}"), ShootHarderPvE.Info.IsOnSlot));
		ImGui.Text(string.Format(Loc.T("ShootHarderPvE Charges: {0}"), ShootHarderPvE.Cooldown.CurrentCharges));
		ImGui.Spacing();
		ImGui.Text(string.Format(Loc.T("SmokingGunPvE Slotted: {0}"), SmokingGunPvE.Info.IsOnSlot));
		ImGui.Text(string.Format(Loc.T("SmokingGunPvE Charges: {0}"), SmokingGunPvE.Cooldown.CurrentCharges));
		ImGui.Spacing();
		ImGui.Text(string.Format(Loc.T("PulpFissionPvE Slotted: {0}"), PulpFissionPvE.Info.IsOnSlot));
		ImGui.Text(string.Format(Loc.T("PulpFissionPvE Charges: {0}"), PulpFissionPvE.Cooldown.CurrentCharges));
		ImGui.Spacing();
		ImGui.Text(string.Format(Loc.T("MoltingPythonPvE Slotted: {0}"), MoltingPythonPvE.Info.IsOnSlot));
		ImGui.Text(string.Format(Loc.T("MoltingPythonPvE Charges: {0}"), MoltingPythonPvE.Cooldown.CurrentCharges));
		ImGui.Spacing();
		ImGui.Text(string.Format(Loc.T("NightStallionPvE Slotted: {0}"), NightStallionPvE.Info.IsOnSlot));
		ImGui.Text(string.Format(Loc.T("NightStallionPvE Charges: {0}"), NightStallionPvE.Cooldown.CurrentCharges));
		ImGui.Spacing();
		ImGui.Text(string.Format(Loc.T("ThickSkinPvE Slotted: {0}"), ThickSkinPvE.Info.IsOnSlot));
		ImGui.Text(string.Format(Loc.T("ThickSkinPvE Charges: {0}"), ThickSkinPvE.Cooldown.CurrentCharges));
		ImGui.Spacing();
		ImGui.Text(string.Format(Loc.T("TheShortGoodbyePvE Slotted: {0}"), TheShortGoodbyePvE.Info.IsOnSlot));
		ImGui.Text(string.Format(Loc.T("TheShortGoodbyePvE Charges: {0}"), TheShortGoodbyePvE.Cooldown.CurrentCharges));
		ImGui.Spacing();
	}

	public override bool EmergencyAbility(IAction nextGCD, out IAction? act)
	{
		if (DataCenter.IsLichCastingSpecialIndicator() || StatusHelper.PlayerHasStatus(false, StatusID.Bind_3776))
		{
			if (MoltingPythonPvE.CanUse(out act, usedUp: true, skipComboCheck: true))
			{
				return true;
			}
		}

		return base.EmergencyAbility(nextGCD, out act);
	}

	public override bool GeneralAbility(IAction nextGCD, out IAction? act)
	{
		if (NightStallionPvE.CanUse(out act))
		{
			return true;
		}

		return base.GeneralAbility(nextGCD, out act);
	}

	public override bool DefenseAreaGCD(out IAction? act)
	{
		if (ThickSkinPvE.CanUse(out act))
		{
			return true;
		}

		return base.HealSingleGCD(out act);
	}

	public override bool HealSingleGCD(out IAction? act)
	{
		if (ThickSkinPvE.CanUse(out act, skipStatusProvideCheck: true))
		{
			return true;
		}

		return base.HealSingleGCD(out act);
	}

	public override bool GeneralGCD(out IAction? act)
	{
		if (TheShortGoodbyePvE.CanUse(out act))
		{
			return true;
		}

		if (PulpFissionPvE.CanUse(out act, skipComboCheck: true))
		{
			return true;
		}

		if (SmokingGunPvE.Info.IsOnSlot)
		{
			if (SmokingGunPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (ShootHarderPvE.Info.IsOnSlot)
		{
			if (ShootHarderPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (ShootHardPvE.Info.IsOnSlot)
		{
			if (ShootHardPvE.CanUse(out act))
			{
				return true;
			}
		}

		return base.GeneralGCD(out act);
	}
}
