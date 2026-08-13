using Dalamud.Interface.Colors;

namespace RotationSolver.Basic.Rotations.Basic;

public partial class GunbreakerRotation
{
	/// <inheritdoc/>
	public override MedicineType MedicineType => MedicineType.Strength;

	#region Job Gauge
	/// <summary>
	/// Gets the amount of ammo available.
	/// </summary>
	public static byte Ammo => JobGauge.Ammo;

	/// <summary>
	/// 
	/// </summary>
	public static byte AmmoComboStep => JobGauge.AmmoComboStep;

	/// <summary>
	/// Gets the maximum amount of ammo available.
	/// </summary>
	public static byte MaxAmmo()
	{
		if (HasBloodfest)
		{
			if (CartridgeChargeIiTrait.EnoughLevel)
			{
				return 6;
			}

			return 4;
		}
		if (CartridgeChargeIiTrait.EnoughLevel)
		{
			return 3;
		}

		if (CartridgeChargeTrait.EnoughLevel)
		{
			return 2;
		}

		return 0;
	}

	/// <summary>
	/// Gets the maximum amount of ammo available not accounting for Bloodfest.
	/// </summary>
	public static byte NormalMaxAmmo()
	{
		if (CartridgeChargeIiTrait.EnoughLevel)
		{
			return 3;
		}

		if (CartridgeChargeTrait.EnoughLevel)
		{
			return 2;
		}

		return 0;
	}

	/// <summary>
	/// 
	/// </summary>
	public static byte OvercappedAmmo()
	{
		return (byte)(Ammo - NormalMaxAmmo());
	}

	/// <summary>
	/// Gets whether the current ammo is at the maximum allowed.
	/// </summary>
	public static bool IsAmmoCapped => Ammo == MaxAmmo();

	/// <summary>
	/// Gets the max combo time of the Gnashing Fang combo.
	/// </summary>
	public static short MaxTimerDuration => JobGauge.MaxTimerDuration;

	/// <summary>
	/// Gets whether the player is in the Gnashing Fang combo.
	/// </summary>
	public static bool InGnashingFang => AmmoComboStep is 1 or 2;

	/// <summary>
	/// Gets whether the player is in the Reign combo.
	/// </summary>
	public static bool InReignCombo => AmmoComboStep is 3 or 4;

	/// <summary>
	/// Has No Mercy buff.
	/// </summary>
	public static bool HasNoMercy => !StatusHelper.PlayerWillStatusEndGCD(0, 0, true, StatusID.NoMercy);

	/// <summary>
	/// Has No Mercy buff.
	/// </summary>
	public static bool HasBloodfest => !StatusHelper.PlayerWillStatusEndGCD(0, 0, true, StatusID.Bloodfest);

	/// <summary>
	/// Able to execute Sonic Break.
	/// </summary>
	public static bool HasReadyToBreak => !StatusHelper.PlayerWillStatusEndGCD(0, 0, true, StatusID.ReadyToBreak);

	/// <summary>
	/// Able to execute Reign of Beasts.
	/// </summary>
	public static bool HasReadyToReign => !StatusHelper.PlayerWillStatusEndGCD(0, 0, true, StatusID.ReadyToReign);

	/// <summary>
	/// Able to execute Jugular Rip.
	/// </summary>
	public static bool HasReadyToRip => !StatusHelper.PlayerWillStatusEndGCD(0, 0, true, StatusID.ReadyToRip);

	/// <summary>
	/// Able to execute Abdomen Tear.
	/// </summary>
	public static bool HasReadyToTear => !StatusHelper.PlayerWillStatusEndGCD(0, 0, true, StatusID.ReadyToTear);

	/// <summary>
	/// Able to execute Fated Brand.
	/// </summary>
	public static bool HasReadyToRaze => !StatusHelper.PlayerWillStatusEndGCD(0, 0, true, StatusID.ReadyToRaze);

	/// <summary>
	/// Able to execute Eye Gouge.
	/// </summary>
	public static bool HasReadyToGouge => !StatusHelper.PlayerWillStatusEndGCD(0, 0, true, StatusID.ReadyToGouge);

	/// <summary>
	/// Able to execute Hypervelocity.
	/// </summary>
	public static bool HasReadyToBlast => !StatusHelper.PlayerWillStatusEndGCD(0, 0, true, StatusID.ReadyToBlast);

	//public bool NoMercyWindow => NoMercyPvE.Cooldown.RecastTimeElapsed >= 39.5f && NoMercyPvE.Cooldown.RecastTimeElapsed <= 60;

	#endregion

	#region PvE Actions Unassignable

	/// <summary>
	/// 
	/// </summary>
	public static bool SavageClawPvEReady => Service.GetAdjustedActionId(ActionID.GnashingFangPvE) == ActionID.SavageClawPvE;

	/// <summary>
	/// 
	/// </summary>
	public static bool WickedTalonPvEReady => Service.GetAdjustedActionId(ActionID.GnashingFangPvE) == ActionID.WickedTalonPvE;

	/// <summary>
	/// 
	/// </summary>
	public static bool JugularRipPvEReady => Service.GetAdjustedActionId(ActionID.ContinuationPvE) == ActionID.JugularRipPvE;

	/// <summary>
	/// 
	/// </summary>
	public static bool AbdomenTearPvEReady => Service.GetAdjustedActionId(ActionID.ContinuationPvE) == ActionID.AbdomenTearPvE;

	/// <summary>
	/// 
	/// </summary>
	public static bool EyeGougePvEReady => Service.GetAdjustedActionId(ActionID.ContinuationPvE) == ActionID.EyeGougePvE;

	/// <summary>
	/// 
	/// </summary>
	public static bool HypervelocityPvEReady => Service.GetAdjustedActionId(ActionID.ContinuationPvE) == ActionID.HypervelocityPvE;

	/// <summary>
	/// 
	/// </summary>
	public static bool FatedBrandPvEReady => Service.GetAdjustedActionId(ActionID.ContinuationPvE) == ActionID.FatedBrandPvE;

	/// <summary>
	/// 
	/// </summary>
	public static bool NobleBloodPvEReady => Service.GetAdjustedActionId(ActionID.ReignOfBeastsPvE) == ActionID.NobleBloodPvE;

	/// <summary>
	/// 
	/// </summary>
	public static bool LionHeartPvEReady => Service.GetAdjustedActionId(ActionID.ReignOfBeastsPvE) == ActionID.LionHeartPvE;
	#endregion

	#region Debug Status

	/// <inheritdoc/>
	public override void DisplayBaseStatus()
	{
		ImGui.Text(Loc.T("InGnashingFang: ") + InGnashingFang.ToString());
		ImGui.Text(Loc.T("InReignCombo: ") + InReignCombo.ToString());
		ImGui.Text(Loc.T("HasNoMercy: ") + HasNoMercy.ToString());
		ImGui.Text(Loc.T("HasReadyToBreak: ") + HasReadyToBreak.ToString());
		ImGui.Text(Loc.T("HasReadyToReign: ") + HasReadyToReign.ToString());
		ImGui.Text(Loc.T("HasReadyToRip: ") + HasReadyToRip.ToString());
		ImGui.Text(Loc.T("HasReadyToTear: ") + HasReadyToTear.ToString());
		ImGui.Text(Loc.T("HasReadyToRaze: ") + HasReadyToRaze.ToString());
		ImGui.Text(Loc.T("HasReadyToGouge: ") + HasReadyToGouge.ToString());
		ImGui.Text(Loc.T("HasReadyToBlast: ") + HasReadyToBlast.ToString());
		ImGui.Spacing();
		//ImGui.Text(Loc.T("NoMercyWindow: ") + NoMercyWindow.ToString());
		ImGui.Text(Loc.T("Ammo: ") + Ammo.ToString());
		ImGui.Text(Loc.T("AmmoComboStep: ") + AmmoComboStep.ToString());
		ImGui.Text(Loc.T("MaxAmmo: ") + MaxAmmo().ToString());
		ImGui.Text(Loc.T("Is Ammo Capped: ") + IsAmmoCapped.ToString());
		ImGui.Text(Loc.T("MaxTimerDuration: ") + MaxTimerDuration.ToString());
		ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.T("PvE Actions"));
		ImGui.Text(Loc.T("SavageClawPvEReady: ") + SavageClawPvEReady.ToString());
		ImGui.Text(Loc.T("WickedTalonPvEReady: ") + WickedTalonPvEReady.ToString());
		ImGui.Spacing();
		ImGui.Text(Loc.T("JugularRipPvEReady: ") + JugularRipPvEReady.ToString());
		ImGui.Text(Loc.T("AbdomenTearPvEReady: ") + AbdomenTearPvEReady.ToString());
		ImGui.Text(Loc.T("EyeGougePvEReady: ") + EyeGougePvEReady.ToString());
		ImGui.Text(Loc.T("HypervelocityPvEReady: ") + HypervelocityPvEReady.ToString());
		ImGui.Spacing();
		ImGui.Text(Loc.T("FatedBrandPvEReady: ") + FatedBrandPvEReady.ToString());
		ImGui.Spacing();
		ImGui.Text(Loc.T("NobleBloodPvEReady: ") + NobleBloodPvEReady.ToString());
		ImGui.Text(Loc.T("LionHeartPvEReady: ") + LionHeartPvEReady.ToString());
		ImGui.TextColored(ImGuiColors.DalamudYellow, Loc.T("PvP Actions"));
		ImGui.Text(Loc.T("SavageClawPvPReady: ") + SavageClawPvPReady.ToString());
		ImGui.Text(Loc.T("WickedTalonPvPReady: ") + WickedTalonPvPReady.ToString());
		ImGui.Spacing();
		ImGui.Text(Loc.T("HypervelocityPvPReady: ") + HypervelocityPvPReady.ToString());
		ImGui.Text(Loc.T("FatedBrandPvPReady: ") + FatedBrandPvPReady.ToString());
		ImGui.Text(Loc.T("JugularRipPvPReady: ") + JugularRipPvPReady.ToString());
		ImGui.Text(Loc.T("AbdomenTearPvPReady: ") + AbdomenTearPvPReady.ToString());
		ImGui.Text(Loc.T("EyeGougePvPReady: ") + EyeGougePvPReady.ToString());
		ImGui.Spacing();
		ImGui.Text(Loc.T("HasTerminalTrigger: ") + HasTerminalTrigger.ToString());
	}
	#endregion

	#region PvE Actions

	static partial void ModifyKeenEdgePvE(ref ActionSetting setting)
	{

	}

	static partial void ModifyNoMercyPvE(ref ActionSetting setting)
	{
		setting.CreateConfig = () => new ActionConfig()
		{
			TimeToKill = 5,
		};
		setting.IsFriendly = true;
	}

	static partial void ModifyBrutalShellPvE(ref ActionSetting setting)
	{

	}

	static partial void ModifyCamouflagePvE(ref ActionSetting setting)
	{
		setting.TargetType = TargetType.Self;
		setting.IsFriendly = true;
	}

	static partial void ModifyDemonSlicePvE(ref ActionSetting setting)
	{
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 2,
		};
	}

	private protected sealed override IBaseAction TankStance => RoyalGuardPvE;

	static partial void ModifyRoyalGuardPvE(ref ActionSetting setting)
	{
		setting.IsFriendly = true;
	}

	static partial void ModifyReleaseRoyalGuardPvE(ref ActionSetting setting)
	{
		setting.IsFriendly = true;
	}

	static partial void ModifyLightningShotPvE(ref ActionSetting setting)
	{
		setting.SpecialType = SpecialActionType.MeleeRangedAttack;
	}

	static partial void ModifyDangerZonePvE(ref ActionSetting setting)
	{

	}

	static partial void ModifySolidBarrelPvE(ref ActionSetting setting)
	{

	}

	static partial void ModifyBurstStrikePvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => Ammo >= 1;
	}

	static partial void ModifyNebulaPvE(ref ActionSetting setting)
	{
		setting.StatusProvide = StatusHelper.RampartStatus;
		setting.TargetType = TargetType.Self;
		setting.IsFriendly = true;
	}

	static partial void ModifyDemonSlaughterPvE(ref ActionSetting setting)
	{
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 2,
		};
	}

	static partial void ModifyAuroraPvE(ref ActionSetting setting)
	{
		setting.TargetStatusProvide = [StatusID.Aurora];
		setting.TargetType = TargetType.Self;
		setting.IsFriendly = true;
	}

	static partial void ModifySuperbolidePvE(ref ActionSetting setting)
	{
		setting.StatusProvide = [StatusID.Superbolide];
		setting.ActionCheck = () => InCombat;
		setting.TargetType = TargetType.Self;
		setting.IsFriendly = true;
	}

	static partial void ModifySonicBreakPvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => HasReadyToBreak;
		setting.TargetStatusProvide = [StatusID.SonicBreak];
	}

	static partial void ModifyTrajectoryPvE(ref ActionSetting setting)
	{
		setting.SpecialType = SpecialActionType.HostileMovingForward;
	}

	static partial void ModifyGnashingFangPvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => AmmoComboStep == 0 && Ammo >= 1;
	}

	static partial void ModifySavageClawPvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => AmmoComboStep == 1;
	}

	static partial void ModifyWickedTalonPvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => AmmoComboStep == 2;
	}

	static partial void ModifyBowShockPvE(ref ActionSetting setting)
	{
		setting.TargetStatusProvide = [StatusID.BowShock];
	}

	static partial void ModifyHeartOfLightPvE(ref ActionSetting setting)
	{
		setting.StatusProvide = [StatusID.HeartOfLight];
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
		setting.IsFriendly = true;
	}

	static partial void ModifyHeartOfStonePvE(ref ActionSetting setting)
	{
		setting.StatusProvide = [StatusID.HeartOfStone];
		setting.TargetType = TargetType.Self;
		setting.IsFriendly = true;
	}

	static partial void ModifyContinuationPvE(ref ActionSetting setting)
	{
		setting.UnlockedByQuestID = 68802;
	}

	static partial void ModifyJugularRipPvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => JugularRipPvEReady;
		setting.StatusNeed = [StatusID.ReadyToRip];
	}

	static partial void ModifyAbdomenTearPvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => AbdomenTearPvEReady;
		setting.StatusNeed = [StatusID.ReadyToTear];
	}

	static partial void ModifyEyeGougePvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => EyeGougePvEReady;
		setting.StatusNeed = [StatusID.ReadyToGouge];
	}

	static partial void ModifyFatedCirclePvE(ref ActionSetting setting)
	{
		setting.StatusProvide = [StatusID.ReadyToRaze];
		setting.ActionCheck = () => Ammo >= 1;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 2,
		};
	}

	static partial void ModifyBloodfestPvE(ref ActionSetting setting)
	{
		setting.StatusProvide = [StatusID.ReadyToReign];
	}

	static partial void ModifyBlastingZonePvE(ref ActionSetting setting)
	{

	}

	static partial void ModifyHeartOfCorundumPvE(ref ActionSetting setting)
	{
		setting.StatusProvide = [StatusID.CatharsisOfCorundum, StatusID.ClarityOfCorundum];
		setting.TargetType = TargetType.Self;
		setting.IsFriendly = true;
	}

	static partial void ModifyHypervelocityPvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => HypervelocityPvEReady;
		setting.StatusNeed = [StatusID.ReadyToBlast];
	}

	static partial void ModifyDoubleDownPvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => Ammo >= 2;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyGreatNebulaPvE(ref ActionSetting setting)
	{
		setting.StatusProvide = StatusHelper.RampartStatus;
		setting.TargetType = TargetType.Self;
		setting.IsFriendly = true;
	}

	static partial void ModifyFatedBrandPvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => FatedBrandPvEReady;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 2,
		};
	}

	static partial void ModifyReignOfBeastsPvE(ref ActionSetting setting)
	{
		setting.StatusNeed = [StatusID.ReadyToReign];
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1
		};
	}

	static partial void ModifyNobleBloodPvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => AmmoComboStep == 3;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1
		};
	}

	static partial void ModifyLionHeartPvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => AmmoComboStep == 4;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1
		};
	}

	#endregion

	#region PvP Actions Unassignable

	/// <summary>
	/// Gnashing Fang 2
	/// </summary>
	public static bool SavageClawPvPReady => Service.GetAdjustedActionId(ActionID.GnashingFangPvP) == ActionID.SavageClawPvP;

	/// <summary>
	/// Gnashing Fang 3
	/// </summary>
	public static bool WickedTalonPvPReady => Service.GetAdjustedActionId(ActionID.GnashingFangPvP) == ActionID.WickedTalonPvP;

	/// <summary>
	/// 
	/// </summary>
	public static bool HypervelocityPvPReady => Service.GetAdjustedActionId(ActionID.ContinuationPvP) == ActionID.HypervelocityPvP;

	/// <summary>
	/// 
	/// </summary>
	public static bool FatedBrandPvPReady => Service.GetAdjustedActionId(ActionID.ContinuationPvP) == ActionID.FatedBrandPvP;

	/// <summary>
	/// 
	/// </summary>
	public static bool JugularRipPvPReady => Service.GetAdjustedActionId(ActionID.ContinuationPvP) == ActionID.JugularRipPvP;

	/// <summary>
	/// 
	/// </summary>
	public static bool AbdomenTearPvPReady => Service.GetAdjustedActionId(ActionID.ContinuationPvP) == ActionID.AbdomenTearPvP;

	/// <summary>
	/// 
	/// </summary>
	public static bool EyeGougePvPReady => Service.GetAdjustedActionId(ActionID.ContinuationPvP) == ActionID.EyeGougePvP;
	#endregion

	/// <summary>
	/// 
	/// </summary>
	public static bool HasTerminalTrigger => StatusHelper.PlayerHasStatus(true, StatusID.RelentlessRush);

	#region PvP Actions

	static partial void ModifyGnashingFangPvP(ref ActionSetting setting)
	{

	}

	static partial void ModifyFatedCirclePvP(ref ActionSetting setting)
	{
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyContinuationPvP(ref ActionSetting setting)
	{
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyRoughDividePvP(ref ActionSetting setting)
	{
		//setting.SpecialType = SpecialActionType.MovingForward;
	}

	static partial void ModifyBlastingZonePvP(ref ActionSetting setting)
	{

	}

	static partial void ModifyHeartOfCorundumPvP(ref ActionSetting setting)
	{

	}

	static partial void ModifySavageClawPvP(ref ActionSetting setting)
	{
		setting.ActionCheck = () => SavageClawPvPReady;
	}

	static partial void ModifyWickedTalonPvP(ref ActionSetting setting)
	{
		setting.ActionCheck = () => WickedTalonPvPReady;
	}

	static partial void ModifyHypervelocityPvP(ref ActionSetting setting)
	{
		setting.ActionCheck = () => HypervelocityPvPReady;
		setting.MPOverride = () => 0;
	}

	static partial void ModifyFatedBrandPvP(ref ActionSetting setting)
	{
		setting.ActionCheck = () => FatedBrandPvPReady;
		setting.MPOverride = () => 0;
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyJugularRipPvP(ref ActionSetting setting)
	{
		setting.ActionCheck = () => JugularRipPvPReady;
		setting.MPOverride = () => 0;
	}

	static partial void ModifyAbdomenTearPvP(ref ActionSetting setting)
	{
		setting.ActionCheck = () => AbdomenTearPvPReady;
		setting.MPOverride = () => 0;
	}

	static partial void ModifyEyeGougePvP(ref ActionSetting setting)
	{
		setting.ActionCheck = () => EyeGougePvPReady;
		setting.MPOverride = () => 0;
	}
	#endregion

	/// <inheritdoc/>
	protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
	{
		return (SuperbolidePvE.CanUse(out act)
			&& Player?.GetHealthRatio() <= Service.Config.HealthForDyingTanks) || base.EmergencyAbility(nextGCD, out act);
	}

	/// <inheritdoc/>
	public override bool IsBursting()
	{
		if (StatusHelper.PlayerHasStatus(true, StatusID.NoMercy) || NoMercyPvE.Cooldown.RecastTimeRemainOneCharge > 15f)
		{
			return true; // Either have No Mercy or more than 15 seconds until we can use it
		}
		return false;
	}
}