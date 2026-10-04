using RotationSolver.Basic.Localization;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using RotationSolver.Basic.Configuration;
using RotationSolver.Basic.Rotations.Duties;
using RotationSolver.Helpers;
using RotationSolver.IPC;
using RotationSolver.Updaters;
using System.Diagnostics;
using System.Text;

namespace RotationSolver.UI;

public partial class MainWindow
{
	private static void DrawDebug()
	{
		_allSearchable.DrawItems(Configs.Debug);

		{
			var tracePath = ActionTracer.CurrentFilePath;
			var hasFile = !string.IsNullOrEmpty(tracePath) && File.Exists(tracePath);
			var hasAnyData = hasFile
				|| !string.IsNullOrEmpty(ActionTracer.LastFrameSummary)
				|| ActionTracer.HasAnyTraceFiles();

			if (!hasFile)
			{
				ImGui.BeginDisabled();
			}
			if (ImGui.Button(Loc.Label("Open Action Trace File")))
			{
				try
				{
					_ = Process.Start("explorer.exe", $"\"{tracePath}\"");
				}
				catch (Exception ex)
				{
					PluginLog.Warning($"Failed to open trace file: {ex.Message}");
				}
			}
			if (!hasFile)
			{
				ImGui.EndDisabled();
			}
			if (ImGui.IsItemHovered())
			{
				ImGui.SetTooltip(Loc.T(hasFile
					? tracePath
					: "No trace file yet — enable the tracer and enter combat to create one."));
			}

			ImGui.SameLine();

			if (!hasAnyData)
			{
				ImGui.BeginDisabled();
			}
			if (ImGui.Button(Loc.Label("Clear Trace")))
			{
				ActionTracer.ClearTrace();
			}
			if (!hasAnyData)
			{
				ImGui.EndDisabled();
			}
			if (ImGui.IsItemHovered())
			{
				ImGui.SetTooltip(Loc.T("Delete every actiontrace_*.log file in the Traces folder and clear the buffered last-frame data."));
			}
		}

		if (!Player.Available || !Service.Config.InDebug)
		{
			return;
		}

		_debugHeader?.Draw();

		if (ImGui.Button(Loc.Label("Reset Action Configs")))
		{
			DataCenter.ResetActionConfigs = DataCenter.ResetActionConfigs != true;
		}
		ImGui.Text(Loc.F($"Reset Action Configs: {DataCenter.ResetActionConfigs}"));
		if (ImGui.Button(Loc.Label("Add Test Warning")))
		{
			BasicWarningHelper.AddSystemWarning("This is a test warning.");
		}
	}

	private static readonly CollapsingHeaderGroup _debugHeader = BuildDebugHeaderGroup();

	private static CollapsingHeaderGroup BuildDebugHeaderGroup()
	{
		var group = new CollapsingHeaderGroup(new()
	{
		{() => DataCenter.CurrentRotation != null ? "Loaded Rotation Info" : string.Empty, DrawDebugRotationStatus},
		{() => DataCenter.CurrentRotation != null ? "Base Rotation Info" : string.Empty, DrawDebugBaseStatus},
		{() => "Player Status", DrawStatus },
		{() => "Raise Info", DrawRaiseInfo },
		{() => "Duty Info", DrawDutyInfo },
		{() => "Party", DrawParty },
		{() => "Target Data", DrawTargetData },
		{() => "Next Action", DrawNextAction },
		{() => "Last Action", DrawLastAction },
		{() => "IPC Testing", DrawIPC },
		{() => "BMR Data", DrawBMRData },
		{() => "Occult Crescent Weaknesses", DrawOccultWeaknesses },

		{() => "Effect", () =>
			{
				ImGui.Text(Loc.T(Watcher.ShowStrSelf));
				ImGui.Separator();
				ImGui.Text(Loc.T(DataCenter.Role.ToString()));
			} },
		{() => "Material 3 Gallery", MaterialGallery.Draw },
	});

		group.SetHeaderIcon("Loaded Rotation Info", FontAwesomeIcon.Sync);
		group.SetHeaderIcon("Base Rotation Info", FontAwesomeIcon.Cube);
		group.SetHeaderIcon("Player Status", FontAwesomeIcon.User);
		group.SetHeaderIcon("Raise Info", FontAwesomeIcon.Ankh);
		group.SetHeaderIcon("Duty Info", FontAwesomeIcon.Dungeon);
		group.SetHeaderIcon("Party", FontAwesomeIcon.Users);
		group.SetHeaderIcon("Target Data", FontAwesomeIcon.Crosshairs);
		group.SetHeaderIcon("Next Action", FontAwesomeIcon.StepForward);
		group.SetHeaderIcon("Last Action", FontAwesomeIcon.StepBackward);
		group.SetHeaderIcon("IPC Testing", FontAwesomeIcon.PlugCircleBolt);
		group.SetHeaderIcon("BMR Data", FontAwesomeIcon.ChartLine);
		group.SetHeaderIcon("Occult Crescent Weaknesses", FontAwesomeIcon.Moon);
		group.SetHeaderIcon("Effect", FontAwesomeIcon.Magic);
		group.SetHeaderIcon("Material 3 Gallery", FontAwesomeIcon.Palette);

		return group;
	}

	private static void DrawDebugRotationStatus()
	{
		DataCenter.CurrentRotation?.DisplayRotationStatus();
	}

	private static void DrawOccultWeaknesses()
	{
		ImGui.TextWrapped(Loc.T("Records the elemental weaknesses (Lightning, Fire, Ice, Wind) observed on hostiles ") +
			Loc.T("encountered in Occult Crescent, keyed by their NameId. This is populated automatically while in ") +
			Loc.T("Occult Crescent."));

		if (ImGui.Button(Loc.Label("Open Weakness Data Folder")))
		{
			try
			{
				var path = Svc.PluginInterface.ConfigDirectory.FullName;
				_ = Process.Start("explorer.exe", $"\"{path}\"");
			}
			catch (Exception ex)
			{
				PluginLog.Warning($"Failed to open weakness data folder: {ex.Message}");
			}
		}
		ImGui.SameLine();
		if (ImGui.Button(Loc.Label("Clear Weakness Data")))
		{
			OtherConfiguration.ResetOccultWeaknessRecords();
		}
		ImGui.SameLine();
		if (ImGui.Button(Loc.Label("Copy as Curated List Entries")))
		{
			var sb = new StringBuilder();
			void AppendEntries(Dictionary<uint, List<string>> records)
			{
				foreach (var kvp in records)
				{
					var statusesSb = new StringBuilder();
					for (var i = 0; i < kvp.Value.Count; i++)
					{
						if (i > 0)
						{
							_ = statusesSb.Append(", ");
						}
						_ = statusesSb.Append("StatusID.").Append(kvp.Value[i]);
					}
					_ = sb.AppendLine($"\t\t{{ {kvp.Key}, [{statusesSb}] }},");
				}
			}
			sb.AppendLine("// North Horn");
			AppendEntries(OtherConfiguration.NorthHornWeaknessRecords);
			sb.AppendLine("// South Horn");
			AppendEntries(OtherConfiguration.SouthHornWeaknessRecords);
			ImGui.SetClipboardText(sb.ToString());
		}
		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip(Loc.T("Copies each recorded NameId/weakness."));
		}

		using var table = ImRaii.Table("OccultWeaknessTable", 4,
			ImGuiTableFlags.BordersInner | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp,
			new Vector2(0, 200 * Scale));
		if (table)
		{
			ImGui.TableSetupScrollFreeze(0, 1);
			ImGui.TableSetupColumn(Loc.T("Zone"));
			ImGui.TableSetupColumn(Loc.T("NameId"));
			ImGui.TableSetupColumn(Loc.T("Name"));
			ImGui.TableSetupColumn(Loc.T("Weaknesses"));
			ImGui.TableHeadersRow();

			void DrawRows(string zoneName, Dictionary<uint, List<string>> records)
			{
				foreach (var kvp in records)
				{
					ImGui.TableNextRow();
					_ = ImGui.TableNextColumn();
					ImGui.TextUnformatted(Loc.T(zoneName));
					_ = ImGui.TableNextColumn();
					ImGui.TextUnformatted(Loc.T(kvp.Key.ToString()));
					_ = ImGui.TableNextColumn();
					var npcName = string.Empty;
					try
					{
						npcName = Service.GetSheet<Lumina.Excel.Sheets.BNpcName>().GetRow(kvp.Key).Singular.ToString();
					}
					catch { }
					ImGui.TextUnformatted(Loc.T(npcName));
					_ = ImGui.TableNextColumn();
					ImGui.TextUnformatted(Loc.T(string.Join(", ", kvp.Value)));
				}
			}

			DrawRows("North Horn", OtherConfiguration.NorthHornWeaknessRecords);
			DrawRows("South Horn", OtherConfiguration.SouthHornWeaknessRecords);
		}

		ImGui.Spacing();
		ImGui.Separator();
		ImGui.TextWrapped(Loc.T("Hostiles in Range Without Weakness Data"));

		var unknownNames = new List<string>();
		var seenNameIds = new HashSet<uint>();
		var hostiles = DataCenter.AllHostileTargets;
		if (hostiles != null)
		{
			for (var i = 0; i < hostiles.Count; i++)
			{
				var hostile = hostiles[i];
				if (hostile == null || hostile.NameId == 0)
				{
					continue;
				}

				if (!seenNameIds.Add(hostile.NameId))
				{
					continue;
				}

				var alreadyRecorded = DataCenter.IsInNorthHorn
					? OtherConfiguration.NorthHornWeaknessRecords.ContainsKey(hostile.NameId)
					: DataCenter.IsInSouthHorn && OtherConfiguration.SouthHornWeaknessRecords.ContainsKey(hostile.NameId);

				if (StatusHelper.HasKnownOccultWeakness(hostile.NameId) || alreadyRecorded)
				{
					continue;
				}

				var npcName = string.Empty;
				try
				{
					npcName = Service.GetSheet<Lumina.Excel.Sheets.BNpcName>().GetRow(hostile.NameId).Singular.ToString();
				}
				catch { }

				unknownNames.Add(string.IsNullOrEmpty(npcName) ? $"NameId {hostile.NameId}" : npcName);
			}
		}

		if (unknownNames.Count == 0)
		{
			ImGui.TextUnformatted(Loc.T("None."));
		}
		else
		{
			ImGui.TextUnformatted(Loc.T(string.Join(", ", unknownNames)));
		}
	}

	private static void DrawDebugBaseStatus()
	{
		DataCenter.CurrentRotation?.DisplayBaseStatus();
	}

	private static unsafe void DrawStatus()
	{
		if (Player.Object == null)
		{
			return;
		}
		ImGui.Text(Loc.F($"PlayerSyncedLevel: {DataCenter.PlayerSyncedLevel()}"));
		ImGui.Text(Loc.F($"PlayerUnsyncedLevel: {DataCenter.PlayerMaxLevel}"));
		ImGui.Text(Loc.F($"Merged Status: {DataCenter.MergedStatus}"));
		ImGui.Text(Loc.F($"PlayerHasLockActions: {ActionUpdater.PlayerHasLockActions()}"));
		ImGui.Text(Loc.F($"Height: {Player.Character->ModelContainer.CalculateHeight()}"));
		ImGui.Text(Loc.F($"AutoFaceTargetOnActionSetting: {DataCenter.AutoFaceTargetOnActionSetting()}"));
		ImGui.Text(Loc.F($"MoveModeSetting: {DataCenter.MoveModeSetting()}"));
		Dalamud.Game.ClientState.Conditions.ConditionFlag[] conditions = [.. Svc.Condition.AsReadOnlySet()];
		ImGui.Text(Loc.T("InternalCondition:"));
		foreach (var condition in conditions)
		{
			ImGui.Text(Loc.F($"    {condition}"));
		}
		ImGui.Text(Loc.F($"OnlineStatus: {Player.OnlineStatus.RowId}"));
		ImGui.Text(Loc.F($"CanBeRaised: {Player.Object.CanBeRaised()}"));
		ImGui.Text(Loc.F($"Current Hp: {Player.Object.CurrentHp}"));
		ImGui.Text(Loc.F($"Effective Hp: {ObjectHelper.GetEffectiveHp(Player.Object)}"));
		ImGui.Text(Loc.F($"Effective Hp Percent: {ObjectHelper.GetEffectiveHpPercent(Player.Object)}"));
		ImGui.Text(Loc.F($"IsDead: {Player.Object.IsDead}"));
		ImGui.Text(Loc.F($"DoomNeedHealing: {Player.Object.DoomNeedHealing()}"));
		ImGui.Text(Loc.F($"Dead Time: {DataCenter.DeadTimeRaw}"));
		ImGui.Text(Loc.F($"Alive Time: {DataCenter.AliveTimeRaw}"));
		ImGui.Text(Loc.F($"Moving: {DataCenter.IsMoving}"));
		ImGui.Text(Loc.F($"Moving Time: {DataCenter.MovingRaw}"));
		ImGui.Text(Loc.F($"Stop Moving: {DataCenter.StopMovingRaw}"));
		ImGui.Text(Loc.F($"CountDownTime: {Service.CountDownTime}"));
		ImGui.Text(Loc.F($"Combo Time: {DataCenter.ComboTime}"));
		ImGui.Text(Loc.F($"TargetingType: {DataCenter.TargetingType}"));
		ImGui.Spacing();
		ImGui.Text(Loc.F($"IsHostileCastingToTank: {DataCenter.IsHostileCastingToTank}"));
		ImGui.Text(Loc.F($"AttackedTargets: {DataCenter.AttackedTargets?.Count ?? 0}"));
		if (DataCenter.AttackedTargets != null)
		{
			foreach ((var id, var time) in DataCenter.AttackedTargets)
			{
				ImGui.Text(Loc.T(id.ToString() ?? "Unknown ID"));
			}
		}


		ImGui.Text(Loc.T("Casting Vfx:"));
		List<VfxNewData> filteredVfx = [];
		foreach (var s in DataCenter.VfxDataQueue)
		{
			if (s.Path.StartsWith("vfx/lockon/eff/") && s.TimeDuration.TotalSeconds > 0 && s.TimeDuration.TotalSeconds < 6)
			{
				filteredVfx.Add(s);
			}
		}
		foreach (var vfx in filteredVfx)
		{
			ImGui.Text(Loc.F($"Path: {vfx.Path}"));
		}

		var partyMembers = DataCenter.PartyMembers;
		if (partyMembers.Count != 0)
		{
			ImGui.Text(Loc.T("Party Members:"));
			foreach (var member in partyMembers)
			{
				ImGui.Text(Loc.F($"- {member.Name}"));
			}
		}
		else
		{
			ImGui.Text(Loc.T("Party Members: None"));
		}

		List<IBattleChara> tankPartyMembers = [];
		foreach (var member in DataCenter.PartyMembers)
		{
			if (member.IsJobCategory(JobRole.Tank))
			{
				tankPartyMembers.Add(member);
			}
		}
		if (tankPartyMembers.Count != 0)
		{
			ImGui.Text(Loc.T("Tank Party Members:"));
			foreach (var member in tankPartyMembers)
			{
				ImGui.Text(Loc.F($"- {member.Name}"));
			}
		}
		else
		{
			ImGui.Text(Loc.T("Tank Party Members: None"));
		}

		var dispelTarget = DataCenter.DispelTarget;
		if (dispelTarget != null)
		{
			ImGui.Text(Loc.T("Dispel Target:"));
			ImGui.Text(Loc.F($"- {dispelTarget.Name}"));
		}
		else
		{
			ImGui.Text(Loc.T("Dispel Target: None"));
		}

		ImGui.Text(Loc.F($"DPSTaken: {DataCenter.DPSTaken}"));
		ImGui.Text(Loc.F($"CurrentRotation: {DataCenter.CurrentRotation}"));
		ImGui.Text(Loc.F($"Job: {DataCenter.Job}"));
		ImGui.Text(Loc.F($"JobRange: {DataCenter.JobRange}"));
		ImGui.Text(Loc.F($"Job Role: {DataCenter.Role}"));
		ImGui.Text(Loc.F($"Have pet: {DataCenter.HasPet()}"));
		ImGui.Text(Loc.F($"Hostile Near Count: {DataCenter.NumberOfHostilesInRange}"));
		ImGui.Text(Loc.F($"Hostile Near Count Max Range: {DataCenter.NumberOfHostilesInMaxRange}"));
		ImGui.Text(Loc.F($"Have Companion: {DataCenter.HasCompanion}"));
		ImGui.Text(Loc.F($"MP: {DataCenter.CurrentMp}"));
		ImGui.Text(Loc.F($"Count Down: {Service.CountDownTime}"));

		ImGui.Spacing();
		ImGui.Text(Loc.F($"Statuses:"));
		using var statusTable = ImRaii.Table("TargetStatusTable", 5,
			ImGuiTableFlags.BordersInner | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.ScrollY,
			new Vector2(0, 200 * Scale));
		if (statusTable)
		{
			ImGui.TableSetupScrollFreeze(0, 1);
			ImGui.TableSetupColumn(Loc.T("Name"));
			ImGui.TableSetupColumn(Loc.T("ID"));
			ImGui.TableSetupColumn(Loc.T("Source"));
			ImGui.TableSetupColumn(Loc.T("Stacks"));
			ImGui.TableSetupColumn(Loc.T("Time"));
			ImGui.TableHeadersRow();

			foreach (var status in Player.Object.StatusList)
			{
				if (Player.Object == null)
				{
					continue;
				}

				var source = status.SourceId == Player.Object.GameObjectId ? "You" : Svc.Objects.SearchById(status.SourceId) == null ? "None" : "Others";
				var stacks = Player.Object.StatusStack(true, (StatusID)status.StatusId);
				var stackDisplay = stacks == byte.MaxValue ? "N/A" : stacks.ToString();
				var timeDisplay = status.RemainingTime <= 0f ? "Perm" : $"{status.RemainingTime:F1}s";

				ImGui.TableNextRow();
				_ = ImGui.TableNextColumn();
				ImGui.TextUnformatted(Loc.T(status.GameData.Value.Name.ToString()));
				_ = ImGui.TableNextColumn();
				ImGui.TextUnformatted(Loc.T(status.StatusId.ToString()));
				_ = ImGui.TableNextColumn();
				ImGui.TextUnformatted(Loc.T(source));
				_ = ImGui.TableNextColumn();
				ImGui.TextUnformatted(Loc.T(stackDisplay));
				_ = ImGui.TableNextColumn();
				ImGui.TextUnformatted(Loc.T(timeDisplay));
			}
		}
	}

	private static void DrawRaiseInfo()
	{
		ImGui.Text(Loc.F($"Can Raise: {DataCenter.CanRaise()}"));
		ImGui.Text(Loc.F($"Death Target: {DataCenter.DeathTarget}"));

		var deadPartyMembersList = new List<IBattleChara>();
		foreach (var member in DataCenter.PartyMembers.GetDeath())
		{
			deadPartyMembersList.Add(member);
		}

		if (deadPartyMembersList.Count > 0)
		{
			ImGui.Text(Loc.T("Dead Party Members:"));
			foreach (var member in deadPartyMembersList)
			{
				ImGui.Text(Loc.F($"- {member.Name}"));
			}
		}
		else
		{
			ImGui.Text(Loc.T("Dead Party Members: None"));
		}

		var deadAllianceMembersList = new List<IBattleChara>();
		foreach (var member in DataCenter.AllianceMembers.GetDeath())
		{
			deadAllianceMembersList.Add(member);
		}

		if (deadAllianceMembersList.Count > 0)
		{
			ImGui.Text(Loc.T("Dead Alliance Members:"));
			foreach (var member in deadAllianceMembersList)
			{
				ImGui.Text(Loc.F($"- {member.Name}"));
			}
		}
		else
		{
			ImGui.Text(Loc.T("Dead Alliance Members: None"));
		}
	}

	private static unsafe void DrawDutyInfo()
	{
		ImGui.Spacing();
		ImGui.Text(Loc.F($"DC State: {DataCenter.State}"));
		ImGui.Text(Loc.F($"Your combat state: {DataCenter.InCombat}"));
		ImGui.Text(Loc.F($"Combat Time: {DataCenter.CombatTimeRaw}"));
		ImGui.Text(Loc.F($"TerritoryID: {DataCenter.TerritoryID}"));
		ImGui.Text(Loc.F($"TerritoryType: {DataCenter.Territory?.ContentType}"));
		ImGui.Text(Loc.F($"Is in Alliance Raid: {DataCenter.IsInAllianceRaid}"));
		ImGui.Spacing();
		ImGui.Text(Loc.F($"IsPvP: {DataCenter.IsPvP}"));
		ImGui.Text(Loc.F($"IsInFate: {DataCenter.IsInFate}"));
		if ((IntPtr)FateManager.Instance() != IntPtr.Zero)
		{
			ImGui.Text(Loc.F($"Fate ID: {DataCenter.PlayerFateId}"));
		}
		ImGui.Spacing();
		ImGui.Text(Loc.F($"IsInWindurst: {DataCenter.IsInWindurst}"));
		ImGui.Spacing();
		ImGui.Text(Loc.F($"In Field Operations: {DataCenter.IsInFieldOperations}"));
		ImGui.Text(Loc.F($"In Field Raid: {DataCenter.IsInFieldRaid}"));
		ImGui.Spacing();
		if (DataCenter.IsInBozjanFieldOp)
		{
			ImGui.Text(Loc.F($"IsInBozjanFieldOp: {DataCenter.IsInBozjanFieldOp}"));
			ImGui.Text(Loc.F($"IsInBozjanFieldOpCE: {DataCenter.IsInBozjanFieldOpCE}"));
			ImGui.Text(Loc.F($"IsInDelubrumNormal: {DataCenter.IsInDelubrumNormal}"));
			ImGui.Text(Loc.F($"IsInDelubrumSavage: {DataCenter.IsInDelubrumSavage}"));
			ImGui.Text(Loc.F($"IsInBozja: {DataCenter.IsInBozja}"));
		}
		if (DataCenter.IsInOccultCrescentOp)
		{
			ImGui.Text(Loc.F($"In North Horn: {DataCenter.IsInNorthHorn}"));
			ImGui.Text(Loc.F($"In South Horn: {DataCenter.IsInSouthHorn}"));
			ImGui.Text(Loc.F($"Is In Forked Tower Blood: {DataCenter.IsInForkedTowerBlood}"));
			ImGui.Text(Loc.F($"FreelancerLevel: {DutyRotation.FreelancerLevel}"));
			ImGui.Text(Loc.F($"KnightLevel: {DutyRotation.KnightLevel}"));
			ImGui.Text(Loc.F($"MonkLevel: {DutyRotation.MonkLevel}"));
			ImGui.Text(Loc.F($"BardLevel: {DutyRotation.BardLevel}"));
			ImGui.Text(Loc.F($"ChemistLevel: {DutyRotation.ChemistLevel}"));
			ImGui.Text(Loc.F($"TimeMageLevel: {DutyRotation.TimeMageLevel}"));
			ImGui.Text(Loc.F($"CannoneerLevel: {DutyRotation.CannoneerLevel}"));
			ImGui.Text(Loc.F($"OracleLevel: {DutyRotation.OracleLevel}"));
			ImGui.Text(Loc.F($"BerserkerLevel: {DutyRotation.BerserkerLevel}"));
			ImGui.Text(Loc.F($"RangerLevel: {DutyRotation.RangerLevel}"));
			ImGui.Text(Loc.F($"ThiefLevel: {DutyRotation.ThiefLevel}"));
			ImGui.Text(Loc.F($"SamuraiLevel: {DutyRotation.SamuraiLevel}"));
			ImGui.Text(Loc.F($"GeomancerLevel: {DutyRotation.GeomancerLevel}"));
			ImGui.Text(Loc.F($"MysticKnightLevel: {DutyRotation.MysticKnightLevel}"));
			ImGui.Text(Loc.F($"DancerLevel: {DutyRotation.DancerLevel}"));
			ImGui.Text(Loc.F($"NinjaLevel: {DutyRotation.NinjaLevel}"));
			ImGui.Text(Loc.F($"WhiteMageLevel: {DutyRotation.WhiteMageLevel}"));
			ImGui.Text(Loc.F($"BlackMageLevel: {DutyRotation.BlackMageLevel}"));
			ImGui.Text(Loc.F($"DragoonLevel: {DutyRotation.DragoonLevel}"));
			ImGui.Text(Loc.F($"SummonerLevel: {DutyRotation.SummonerLevel}"));
			ImGui.Text(Loc.F($"BlueMageLevel: {DutyRotation.BlueMageLevel}"));
			ImGui.Text(Loc.F($"RedMageLevel: {DutyRotation.RedMageLevel}"));
			ImGui.Text(Loc.F($"NecromancerLevel: {DutyRotation.NecromancerLevel}"));
		}
		ImGui.Text(Loc.F($"InVariantDungeon: {DataCenter.InVariantDungeon}"));
		ImGui.Text(Loc.F($"The Merchant's Tale Advanced: {DataCenter.TheMerchantsTaleAdvanced}"));
		ImGui.Text(Loc.F($"The Merchant's Tale: {DataCenter.TheMerchantsTale}"));
		ImGui.Text(Loc.F($"AloaloIsland: {DataCenter.AloaloIsland}"));
		ImGui.Text(Loc.F($"MountRokkon: {DataCenter.MountRokkon}"));
		ImGui.Text(Loc.F($"SildihnSubterrane: {DataCenter.SildihnSubterrane}"));
		ImGui.Spacing();
		ImGui.Text(Loc.F($"AreHostilesCastingKnockback: {DataCenter.AreHostilesCastingKnockback}"));
		ImGui.Text(Loc.F($"IsHostileCastingAOE: {DataCenter.IsHostileCastingAOE}"));
		ImGui.Text(Loc.F($"IsHostileCastingToTank: {DataCenter.IsHostileCastingToTank}"));
		ImGui.Text(Loc.F($"IsHostileCastingStop: {DataCenter.IsHostileCastingStop}"));
		ImGui.Spacing();
		ImGui.Text(Loc.F($"IsCastingMultiHit: {DataCenter.IsCastingMultiHit()}"));
		ImGui.Text(Loc.F($"IsCastingAreaVfx: {DataCenter.IsCastingAreaVfx()}"));
		ImGui.Text(Loc.F($"IsCastingTankVfx: {DataCenter.IsCastingTankVfx()}"));
		ImGui.Text(Loc.F($"TankbusterTargets: {DataCenter.TankbusterTargets.Count}"));
		ImGui.Spacing();
		ImGui.Text(Loc.F($"IsInM11S: {DataCenter.IsInM11S}"));
		ImGui.Text(Loc.F($"IsTyrantCastingSpecialIndicator2: {DataCenter.IsTyrantCastingSpecialIndicator2()}"));
		ImGui.Text(Loc.F($"IsLichCastingSpecialIndicator: {DataCenter.IsLichCastingSpecialIndicator()}"));
	}

	private static void DrawParty()
	{
		ImGui.Text(Loc.F($"Number of Party Members: {DataCenter.PartyMembers.Count}"));
		ImGui.Text(Loc.F($"Number of Alliance Members: {DataCenter.AllianceMembers.Count}"));
		ImGui.Text(Loc.F($"Average Party HP Percent: {DataCenter.PartyMembersAverHP * 100}"));
		ImGui.Text(Loc.F($"Average Lowest Party HP Percent: {DataCenter.LowestPartyMembersAverHP * 100}"));
		var doomedCount = 0;
		foreach (var member in DataCenter.PartyMembers)
		{
			if (member.DoomNeedHealing())
			{
				doomedCount++;
			}
		}
		ImGui.Text(Loc.F($"Number of Party Members with Doomed To Heal status: {doomedCount}"));


		if (Player.Object != null && Player.Object.IsJobs(Job.AST))
		{
			var spear = ActionTargetInfo.FindTargetByType(DataCenter.PartyMembers, TargetType.TheSpear, 0, SpecialActionType.None, TargetType.TheSpear, true);
			var balance = ActionTargetInfo.FindTargetByType(DataCenter.PartyMembers, TargetType.TheBalance, 0, SpecialActionType.None, TargetType.TheBalance, true);
			ImGui.Spacing();
			ImGui.Text(Loc.T("AST Card Targets (Preview):"));
			ImGui.Text(Loc.F($"- The Spear: {spear?.Name ?? "None"}"));
			ImGui.Text(Loc.F($"- The Balance: {balance?.Name ?? "None"}"));
			ImGui.Spacing();
		}

		foreach (var p in DataCenter.PartyMembers)
		{
			var text = Loc.F($"Name: {p.Name}, HP: {p.GetEffectiveHpPercent()}%");

			ImGui.Text(Loc.T(text));
		}

		foreach (var p in Svc.Party)
		{
			if (p.GameObject is not IBattleChara b)
			{
				continue;
			}

			var text = Loc.F($"Name: {b.Name}, In Combat: {b.InCombat()}");
			if (b.TimeAlive() > 0)
			{
				text += Loc.F($", Time Alive: {b.TimeAlive()}");
			}

			if (b.TimeDead() > 0)
			{
				text += Loc.F($", Time Dead: {b.TimeDead()}");
			}

			ImGui.Text(Loc.T(text));
		}
		ImGui.Spacing();
		ImGui.Text(Loc.F($"Limit Break: {CustomRotation.LimitBreakLevel}"));
		ImGui.Spacing();
		ImGui.Text(Loc.F($"Object Data"));
		ImGui.Text(Loc.F($"NumberOfPartyMembersInRangeOf 5m: {DataCenter.NumberOfPartyMembersInRangeOf(5)}"));
		ImGui.Text(Loc.F($"AllTargets Count: {DataCenter.AllTargets.Count}"));
		ImGui.Text(Loc.F($"AllHostileTargets Count: {DataCenter.AllHostileTargets.Count}"));
		foreach (var item in DataCenter.AllHostileTargets)
		{
			ImGui.Text(Loc.T(item.Name.ToString()));
		}
		ImGui.Spacing();
		ImGui.Text(Loc.F($"Party Composition:"));
		var party = CustomRotation.PartyComposition;
		if (party.Count == 0)
		{
			ImGui.Text(Loc.T("No party members."));
		}
		else
		{
			for (var i = 0; i < party.Count; i++)
			{
				var classJob = party[i].Value;
				var jobName = classJob.Abbreviation.ToString() ?? classJob.Name.ToString() ?? Loc.F($"Job #{i}");
				ImGui.Text(Loc.F($"{i + 1}: {jobName}"));
			}
		}
		ImGui.Spacing();
		var mitigationFraction = CustomRotation.GetCurrentMitigationPercent();
		ImGui.Text(Loc.F($"Current Mitigation Percent: {mitigationFraction * 100f:F1}%"));
		ImGui.Text(Loc.F($"Current Mitigation Percent RAW: {mitigationFraction}"));

		ImGui.Text(Loc.F($"Is Magical Damage Incoming: {CustomRotation.IsMagicalDamageIncoming}"));
	}

	private static unsafe void DrawTargetData()
	{
		if (Svc.Targets.Target is not IBattleChara target)
		{
			return;
		}

		ImGui.Text(Loc.F($"Height: {target.Struct()->Height}"));
		ImGui.Text(Loc.F($"Kind: {target.GetObjectKind()}"));
		ImGui.Text(Loc.F($"SubKind: {target.GetBattleNPCSubKind()}"));

		var owner = Svc.Objects.SearchById(target.OwnerId);
		if (owner != null)
		{
			ImGui.Text(Loc.F($"Owner: {owner.Name}"));
		}

		if (target is IBattleChara battleChara)
		{
			ImGui.Text(Loc.F($"IsCasting: {battleChara.IsCasting}"));
			ImGui.Text(Loc.F($"CastID: {battleChara.CastInfo.ActionId}"));
			ImGui.Text(Loc.F($"Is Status Capped: {StatusHelper.IsStatusCapped(battleChara)}"));
			ImGui.Text(Loc.F($"CanSee: {battleChara.CanSee()}"));
			ImGui.Text(Loc.F($"CanBeRaised: {battleChara.CanBeRaised()}"));
			ImGui.Text(Loc.F($"HP: {battleChara.CurrentHp} / {battleChara.MaxHp}"));
			ImGui.Text(Loc.F($"HealthRatio: {battleChara.GetHealthRatio()}"));
			ImGui.Text(Loc.F($"HitboxRadius: {battleChara.HitboxRadius}"));
			ImGui.Text(Loc.F($"Distance To Player: {battleChara.DistanceToPlayer()}"));
			ImGui.Spacing();
			ImGui.Text(Loc.F($"NamePlate Icon ID: {battleChara.GetNamePlateIcon()}"));
			ImGui.Text(Loc.F($"Event Type: {battleChara.GetEventType()}"));
			ImGui.Text(Loc.F($"TargetCharaCondition: {battleChara.TargetCharaCondition()}"));
			var npcName = string.Empty;
			var npcEnumName = string.Empty;
			if (battleChara.NameId != 0)
			{
				var bnpcName = Service.GetSheet<Lumina.Excel.Sheets.BNpcName>().GetRow(battleChara.NameId);
				npcName = bnpcName.Singular.ToString();

				if (Enum.IsDefined(typeof(NPCName), battleChara.NameId))
				{
					npcEnumName = Loc.F($"{Enum.GetName(typeof(NPCName), battleChara.NameId)}");
				}
			}
			ImGui.Text(Loc.F($"NPC Name: {npcEnumName}"));
			ImGui.Text(Loc.F($"Name Id: {battleChara.NameId}"));
			ImGui.Text(Loc.F($"Data Id: {battleChara.BaseId}"));
			ImGui.Spacing();
			ImGui.Text(Loc.F($"Is Attackable: {battleChara.IsAttackable()}"));
			ImGui.Text(Loc.F($"Is Others Players Mob: {battleChara.IsOthersPlayersMob()}"));
			ImGui.Text(Loc.F($"Is Alliance: {battleChara.IsAllianceMember()}"));
			ImGui.Text(Loc.F($"Is Enemy Action Check: {battleChara.IsEnemy()}"));
			ImGui.Text(Loc.F($"IsSpecialExecptionImmune: {battleChara.IsSpecialExceptionImmune()}"));
			ImGui.Text(Loc.F($"IsSpecialImmune: {battleChara.IsSpecialImmune()}"));
			ImGui.Text(Loc.F($"IsTopPriorityNamedHostile: {battleChara.IsTopPriorityNamedHostile()}"));
			ImGui.Text(Loc.F($"IsTopPriorityHostile: {battleChara.IsTopPriorityHostile()}"));
			ImGui.Spacing();
			ImGui.Text(Loc.F($"FateID: {battleChara.FateId().ToString() ?? string.Empty}"));
			ImGui.Text(Loc.F($"EventType: {battleChara.GetEventType().ToString() ?? string.Empty}"));
			if (DataCenter.IsInBozja)
			{
				ImGui.Text(Loc.F($"IsBozjanCEFateMob: {battleChara.IsBozjanCEMob()}"));
			}
			ImGui.Spacing();
			if (DataCenter.IsInOccultCrescentOp)
			{
				ImGui.Text(Loc.F($"IsOccultCEMob: {battleChara.IsOccultCEMob()}"));
				ImGui.Text(Loc.F($"IsOccultFateMob: {battleChara.IsOccultFateMob()}"));
				ImGui.Text(Loc.F($"IsOCUndeadTarget: {battleChara.IsOCUndeadTarget()}"));
				ImGui.Text(Loc.F($"IsOCSlowgaImmuneTarget: {battleChara.IsOCSlowgaImmuneTarget()}"));
				ImGui.Text(Loc.F($"IsOCDoomImmuneTarget: {battleChara.IsOCDoomImmuneTarget()}"));
				ImGui.Text(Loc.F($"IsOCStunImmuneTarget: {battleChara.IsOCStunImmuneTarget()}"));
				ImGui.Text(Loc.F($"IsOCFreezeImmuneTarget: {battleChara.IsOCFreezeImmuneTarget()}"));
				ImGui.Text(Loc.F($"IsOCBlindImmuneTarget: {battleChara.IsOCBlindImmuneTarget()}"));
				ImGui.Text(Loc.F($"IsOCParalysisImmuneTarget: {battleChara.IsOCParalysisImmuneTarget()}"));
				ImGui.Spacing();
			}
			ImGui.Text(Loc.F($"Is Current Focus Target: {battleChara.IsFocusTarget()}"));
			ImGui.Text(Loc.F($"TTK: {battleChara.GetTTK()}"));
			ImGui.Text(Loc.F($"Is Boss TTK: {battleChara.IsBossFromTTK()}"));
			ImGui.Text(Loc.F($"Is Boss Icon: {battleChara.IsBossFromIcon()}"));
			ImGui.Text(Loc.F($"Rank: {battleChara.GetObjectNPC()?.Rank.ToString() ?? string.Empty}"));
			ImGui.Text(Loc.F($"Has Positional: {battleChara.HasPositional()}"));
			ImGui.Text(Loc.F($"IsNpcPartyMember: {battleChara.IsNpcPartyMember()}"));
			ImGui.Text(Loc.F($"IsPlayerCharacterChocobo: {battleChara.IsPlayerCharacterChocobo()}"));
			ImGui.Text(Loc.F($"IsFriendlyBattleNPC: {battleChara.IsFriendlyBattleNPC()}"));
			ImGui.Text(Loc.F($"Is Dying: {battleChara.IsDying()}"));
			ImGui.Text(Loc.F($"Is Alive: {battleChara.IsAlive()}"));
			ImGui.Text(Loc.F($"Is Party: {battleChara.IsParty()}"));
			ImGui.Text(Loc.F($"Is Healer: {battleChara.IsJobCategory(JobRole.Healer)}"));
			ImGui.Text(Loc.F($"Is DPS: {battleChara.IsJobCategory(JobRole.AllDPS)}"));
			ImGui.Text(Loc.F($"Is Tank: {battleChara.IsJobCategory(JobRole.Tank)}"));
			ImGui.Text(Loc.F($"Is Alliance: {battleChara.IsAllianceMember()}"));
			ImGui.Text(Loc.F($"CanProvoke: {battleChara.CanProvoke()}"));
			ImGui.Text(Loc.F($"StatusFlags: {battleChara.StatusFlags}"));
			ImGui.Text(Loc.F($"InView: {Svc.GameGui.WorldToScreen(battleChara.Position, out _)}"));
			ImGui.Text(Loc.F($"Enemy Positional: {battleChara.FindEnemyPositional()}"));
			ImGui.Text(Loc.F($"NameplateKind: {battleChara.GetNameplateKind()}"));
			ImGui.Text(Loc.F($"BattleNPCSubKind: {battleChara.GetBattleNPCSubKind()}"));
			ImGui.Text(Loc.F($"Is Top Priority Hostile: {battleChara.IsTopPriorityHostile()}"));
			ImGui.Text(Loc.F($"Targetable: {battleChara.Struct()->Character.GameObject.TargetableStatus}"));
			if (DataCenter.IsInMaskedCarnivale)
			{
				ImGui.Spacing();
				ImGui.Text(Loc.F($"Aspect Resistance (Fire): {MaskedCarnivaleHelper.GetAspectResistance(battleChara, Aspect.Fire)}"));
				ImGui.Text(Loc.F($"Aspect Resistance (Ice): {MaskedCarnivaleHelper.GetAspectResistance(battleChara, Aspect.Ice)}"));
				ImGui.Text(Loc.F($"Aspect Resistance (Wind): {MaskedCarnivaleHelper.GetAspectResistance(battleChara, Aspect.Wind)}"));
				ImGui.Text(Loc.F($"Aspect Resistance (Earth): {MaskedCarnivaleHelper.GetAspectResistance(battleChara, Aspect.Earth)}"));
				ImGui.Text(Loc.F($"Aspect Resistance (Lightning): {MaskedCarnivaleHelper.GetAspectResistance(battleChara, Aspect.Lightning)}"));
				ImGui.Text(Loc.F($"Aspect Resistance (Water): {MaskedCarnivaleHelper.GetAspectResistance(battleChara, Aspect.Water)}"));
				ImGui.Text(Loc.F($"Aspect Resistance (Slashing): {MaskedCarnivaleHelper.GetAspectResistance(battleChara, Aspect.Slashing)}"));
				ImGui.Text(Loc.F($"Aspect Resistance (Piercing): {MaskedCarnivaleHelper.GetAspectResistance(battleChara, Aspect.Piercing)}"));
				ImGui.Text(Loc.F($"Aspect Resistance (Blunt): {MaskedCarnivaleHelper.GetAspectResistance(battleChara, Aspect.Blunt)}"));
				ImGui.Spacing();
				ImGui.Text(Loc.F($"IsVulnerableToSlow: {MaskedCarnivaleHelper.IsVulnerableToSlow(battleChara)}"));
				ImGui.Text(Loc.F($"IsVulnerableToPetrification: {MaskedCarnivaleHelper.IsVulnerableToPetrification(battleChara)}"));
				ImGui.Text(Loc.F($"IsVulnerableToParalysis: {MaskedCarnivaleHelper.IsVulnerableToParalysis(battleChara)}"));
				ImGui.Text(Loc.F($"IsVulnerableToInterruption: {MaskedCarnivaleHelper.IsVulnerableToInterruption(battleChara)}"));
				ImGui.Text(Loc.F($"IsVulnerableToBlind: {MaskedCarnivaleHelper.IsVulnerableToBlind(battleChara)}"));
				ImGui.Text(Loc.F($"IsVulnerableToStun: {MaskedCarnivaleHelper.IsVulnerableToStun(battleChara)}"));
				ImGui.Text(Loc.F($"IsVulnerableToSleep: {MaskedCarnivaleHelper.IsVulnerableToSleep(battleChara)}"));
				ImGui.Text(Loc.F($"IsVulnerableToBind: {MaskedCarnivaleHelper.IsVulnerableToBind(battleChara)}"));
				ImGui.Text(Loc.F($"IsVulnerableToHeavy: {MaskedCarnivaleHelper.IsVulnerableToHeavy(battleChara)}"));
				ImGui.Text(Loc.F($"IsVulnerableToFlatOrDeath: {MaskedCarnivaleHelper.IsVulnerableToFlatOrDeath(battleChara)}"));
			}
			ImGui.Spacing();
			ImGui.Text(Loc.F($"Statuses:"));
			using var statusTable = ImRaii.Table("TargetStatusTable", 5,
				ImGuiTableFlags.BordersInner | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.ScrollY,
				new Vector2(0, 200 * Scale));
			if (statusTable)
			{
				ImGui.TableSetupScrollFreeze(0, 1);
				ImGui.TableSetupColumn(Loc.T("Name"));
				ImGui.TableSetupColumn(Loc.T("ID"));
				ImGui.TableSetupColumn(Loc.T("Source"));
				ImGui.TableSetupColumn(Loc.T("Stacks"));
				ImGui.TableSetupColumn(Loc.T("Time"));
				ImGui.TableHeadersRow();

				foreach (var status in battleChara.StatusList)
				{
					if (Player.Object == null)
					{
						continue;
					}

					var source = status.SourceId == Player.Object.GameObjectId ? "You" : Svc.Objects.SearchById(status.SourceId) == null ? "None" : "Others";
					var stacks = battleChara.StatusStack(true, (StatusID)status.StatusId);
					var stackDisplay = stacks == byte.MaxValue ? "N/A" : stacks.ToString();
					var timeDisplay = status.RemainingTime <= 0f ? "Perm" : $"{status.RemainingTime:F1}s";

					ImGui.TableNextRow();
					_ = ImGui.TableNextColumn();
					ImGui.TextUnformatted(Loc.T(status.GameData.Value.Name.ToString()));
					_ = ImGui.TableNextColumn();
					ImGui.TextUnformatted(Loc.T(status.StatusId.ToString()));
					_ = ImGui.TableNextColumn();
					ImGui.TextUnformatted(Loc.T(source));
					_ = ImGui.TableNextColumn();
					ImGui.TextUnformatted(Loc.T(stackDisplay));
					_ = ImGui.TableNextColumn();
					ImGui.TextUnformatted(Loc.T(timeDisplay));
				}
			}
		}
	}

	private static void DrawNextAction()
	{
		ImGui.Text(Loc.T(DataCenter.CurrentRotation?.GetAttributes()?.Name));
		ImGui.Text(Loc.T(DataCenter.SpecialType.ToString()));

		ImGui.Text(Loc.T(ActionUpdater.NextAction?.Name ?? "null"));
		ImGui.Text(Loc.F($"GCD Total: {DataCenter.DefaultGCDTotal}"));
		ImGui.Text(Loc.F($"GCD Remain: {DataCenter.DefaultGCDRemain}"));
		ImGui.Text(Loc.F($"GCD Elapsed: {DataCenter.DefaultGCDElapsed}"));
		ImGui.Text(Loc.F($"Calculated Action Ahead: {DataCenter.CalculatedActionAhead}"));
		ImGui.Text(Loc.F($"Animation Lock Delay: {DataCenter.AnimationLock}"));
	}

	private static void DrawLastAction()
	{
		DrawAction(DataCenter.LastAction, nameof(DataCenter.LastAction));
		DrawAction(DataCenter.LastAbility, nameof(DataCenter.LastAbility));
		DrawAction(DataCenter.LastGCD, nameof(DataCenter.LastGCD));
		DrawAction(DataCenter.LastComboAction, nameof(DataCenter.LastComboAction));
		ImGui.Text(Loc.F($"IsLastActionAbility: {IActionHelper.IsLastActionAbility()}"));
		ImGui.Text(Loc.F($"IsLastActionGCD: {IActionHelper.IsLastActionGCD()}"));
	}

	private static string _ipcTestText = "Sent data";

	private static void DrawIPC()
	{
		ImGui.SetNextItemWidth(200 * Scale);
		ImGui.InputText(Loc.Label("##IPCTextBox"), ref _ipcTestText, 128);
		ImGui.SameLine();
		if (ImGui.Button(Loc.Label("Test Function")))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.Test(_ipcTestText);
		}

		if (ImGui.Button(Loc.Label("Test ChangeOperatingMode to Manual IPC")))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.ChangeOperatingMode(StateCommandType.Manual);
		}

		if (ImGui.Button(Loc.Label("Test ChangeOperatingMode to Off IPC")))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.ChangeOperatingMode(StateCommandType.Off);
		}

		if (ImGui.Button(Loc.Label("Test TriggerSpecialState DefenseArea IPC")))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.TriggerSpecialState(SpecialCommandType.DefenseArea);
		}

		if (ImGui.Button(Loc.Label("Test TriggerSpecialState AntiKnockback IPC")))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.TriggerSpecialState(SpecialCommandType.AntiKnockback);
		}

		if (ImGui.Button(Loc.Label("Test Setting IPC (Changing engage setting to All Target)")))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.OtherCommand(OtherCommandType.Settings, "HostileType AllTargetsCanAttack");
		}

		if (ImGui.Button(Loc.Label("Test OtherCommand DoAction IPC (Magick Barrier on RDM)")))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.OtherCommand(OtherCommandType.DoActions, "Magick Barrier-5");
		}

		if (ImGui.Button(Loc.Label("Test ToggleAction IPC (Magick Barrier on RDM)")))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.OtherCommand(OtherCommandType.ToggleActions, "Magick Barrier");
		}

		if (ImGui.Button(Loc.Label("Test ActionCommand IPC (Magick Barrier on RDM)")))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.ActionCommand("Magick Barrier", 7);
		}
		if (ImGui.Button(Loc.Label("Test AutodutyChangeOperatingMode IPC (AutoDuty, HighHPPercent)")))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.AutodutyChangeOperatingMode(StateCommandType.AutoDuty, TargetingType.HighHPPercent);
		}
		if (ImGui.Button(Loc.Label("Test Henchman IPC support")))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.ChangeOperatingMode(StateCommandType.Henched);
		}
	}

	private static void DrawBMRData()
	{
		ImGui.Text(Loc.F($"Cooldown Planner IPC Enabled: {BMRPlan_IPCSubscriber.IsEnabled}"));
		ImGui.Text(Loc.F($"BMRPlannedActionsCount: {DataCenter.BMRPlannedActions.Count}"));
		ImGui.Text(Loc.F($"BMRForceCancelCast: {DataCenter.BMRForceCancelCast}"));
		ImGui.Text(Loc.F($"BMRForceCancelCastAI: {DataCenter.BMRForceCancelCastAI}"));
		ImGui.Text(Loc.F($"BMRIsMoving: {DataCenter.BMRIsMoving}"));
	}

	private static void DrawAction(ActionID id, string type)
	{
		ImGui.Text(Loc.F($"{type}: {id}"));
	}
}
