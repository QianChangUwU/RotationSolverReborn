namespace RotationSolver.Basic.Rotations.Duties;

/// <summary>
/// Represents a rotation for variant duties in the game.
/// </summary>
[DutyTerritory(761, 762, 1300, 1306)]
public abstract class MonsterHunterRotation : DutyRotation
{
}

public partial class DutyRotation
{
	/// <summary>
	/// Displays the rotation status on the window.
	/// </summary>
	public virtual void PhantomDisplayDutyStatus()
	{
		if (!DataCenter.IsInMonsterHunterDuty)
		{
			return;
		}

		ImGui.Spacing();
		ImGui.Text(string.Format(Loc.T("MegaPotionPvE Slotted: {0}"), MegaPotionPvE.Info.IsOnSlot));
		ImGui.Text(string.Format(Loc.T("MegaPotionPvE Charges: {0}"), MegaPotionPvE.Cooldown.CurrentCharges));
		ImGui.Spacing();
		ImGui.Text(string.Format(Loc.T("Rathalos Normal: {0}"), RathalosNormal));
		ImGui.Text(string.Format(Loc.T("Rathalos EX: {0}"), RathalosEX));
		ImGui.Spacing();
		ImGui.Spacing();
		ImGui.Text(string.Format(Loc.T("MegaPotionPvE_44247 Slotted: {0}"), MegaPotionPvE_44247.Info.IsOnSlot));
		ImGui.Text(string.Format(Loc.T("MegaPotionPvE_44247 Charges: {0}"), MegaPotionPvE_44247.Cooldown.CurrentCharges));
		ImGui.Spacing();
		ImGui.Text(string.Format(Loc.T("Arkveld Normal: {0}"), ArkveldNormal));
		ImGui.Text(string.Format(Loc.T("Arkveld EX: {0}"), ArkveldEX));
		ImGui.Spacing();
	}

	/// <summary>
	/// Modifies the settings for MegaPotionPvE.
	/// </summary>
	/// <param name="setting">The action setting to modify.</param>
	static partial void ModifyMegaPotionPvE(ref ActionSetting setting)
	{
		setting.TargetType = TargetType.Self;
		setting.IsFriendly = true;
		setting.StatusNeed = [StatusID.Scalebound];
	}

	/// <summary>
	/// Modifies the settings for MegaPotionPvE.
	/// </summary>
	/// <param name="setting">The action setting to modify.</param>
	static partial void ModifyMegaPotionPvE_44247(ref ActionSetting setting)
	{
		setting.TargetType = TargetType.Self;
		setting.IsFriendly = true;
	}
}
