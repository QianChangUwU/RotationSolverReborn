using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using RebornMaterial;
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
			if (ImGui.Button(Loc.T("Open Action Trace File")))
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
				ImGui.SetTooltip(hasFile
					? tracePath
					: "No trace file yet — enable the tracer and enter combat to create one.");
			}

			ImGui.SameLine();

			if (!hasAnyData)
			{
				ImGui.BeginDisabled();
			}
			if (ImGui.Button(Loc.T("Clear Trace")))
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

		if (ImGui.Button(Loc.T("Reset Action Configs")))
		{
			DataCenter.ResetActionConfigs = DataCenter.ResetActionConfigs != true;
		}
		ImGui.Text(string.Format(Loc.T("Reset Action Configs: {0}"), DataCenter.ResetActionConfigs));
		if (ImGui.Button(Loc.T("Add Test Warning")))
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
				ImGui.Text(Watcher.ShowStrSelf);
				ImGui.Separator();
				ImGui.Text(DataCenter.Role.ToString());
			} },
		{() => "Material 3 Gallery", M3Gallery.Draw },
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
			"encountered in Occult Crescent, keyed by their NameId. This is populated automatically while in " +
			"Occult Crescent.");

		if (ImGui.Button(Loc.T("Open Weakness Data Folder")))
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
		if (ImGui.Button(Loc.T("Clear Weakness Data")))
		{
			OtherConfiguration.ResetOccultWeaknessRecords();
		}
		ImGui.SameLine();
		if (ImGui.Button(Loc.T("Copy as Curated List Entries")))
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
					ImGui.TextUnformatted(zoneName);
					_ = ImGui.TableNextColumn();
					ImGui.TextUnformatted(kvp.Key.ToString());
					_ = ImGui.TableNextColumn();
					var npcName = string.Empty;
					try
					{
						npcName = Service.GetSheet<Lumina.Excel.Sheets.BNpcName>().GetRow(kvp.Key).Singular.ToString();
					}
					catch { }
					ImGui.TextUnformatted(npcName);
					_ = ImGui.TableNextColumn();
					ImGui.TextUnformatted(string.Join(", ", kvp.Value));
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
			ImGui.TextUnformatted(string.Join(", ", unknownNames));
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
		ImGui.Text(string.Format(Loc.T("PlayerSyncedLevel: {0}"), DataCenter.PlayerSyncedLevel()));
		ImGui.Text(string.Format(Loc.T("PlayerUnsyncedLevel: {0}"), DataCenter.PlayerMaxLevel));
		ImGui.Text(string.Format(Loc.T("Merged Status: {0}"), DataCenter.MergedStatus));
		ImGui.Text(string.Format(Loc.T("PlayerHasLockActions: {0}"), ActionUpdater.PlayerHasLockActions()));
		ImGui.Text(string.Format(Loc.T("Height: {0}"), Player.Character->ModelContainer.CalculateHeight()));
		ImGui.Text(string.Format(Loc.T("AutoFaceTargetOnActionSetting: {0}"), DataCenter.AutoFaceTargetOnActionSetting()));
		ImGui.Text(string.Format(Loc.T("MoveModeSetting: {0}"), DataCenter.MoveModeSetting()));
		Dalamud.Game.ClientState.Conditions.ConditionFlag[] conditions = [.. Svc.Condition.AsReadOnlySet()];
		ImGui.Text(Loc.T("InternalCondition:"));
		foreach (var condition in conditions)
		{
			ImGui.Text(string.Format(Loc.T("    {0}"), condition));
		}
		ImGui.Text(string.Format(Loc.T("OnlineStatus: {0}"), Player.OnlineStatus.RowId));
		ImGui.Text(string.Format(Loc.T("CanBeRaised: {0}"), Player.Object.CanBeRaised()));
		ImGui.Text(string.Format(Loc.T("Current Hp: {0}"), Player.Object.CurrentHp));
		ImGui.Text(string.Format(Loc.T("Effective Hp: {0}"), ObjectHelper.GetEffectiveHp(Player.Object)));
		ImGui.Text(string.Format(Loc.T("Effective Hp Percent: {0}"), ObjectHelper.GetEffectiveHpPercent(Player.Object)));
		ImGui.Text(string.Format(Loc.T("IsDead: {0}"), Player.Object.IsDead));
		ImGui.Text(string.Format(Loc.T("DoomNeedHealing: {0}"), Player.Object.DoomNeedHealing()));
		ImGui.Text(string.Format(Loc.T("Dead Time: {0}"), DataCenter.DeadTimeRaw));
		ImGui.Text(string.Format(Loc.T("Alive Time: {0}"), DataCenter.AliveTimeRaw));
		ImGui.Text(string.Format(Loc.T("Moving: {0}"), DataCenter.IsMoving));
		ImGui.Text(string.Format(Loc.T("Moving Time: {0}"), DataCenter.MovingRaw));
		ImGui.Text(string.Format(Loc.T("Stop Moving: {0}"), DataCenter.StopMovingRaw));
		ImGui.Text(string.Format(Loc.T("CountDownTime: {0}"), Service.CountDownTime));
		ImGui.Text(string.Format(Loc.T("Combo Time: {0}"), DataCenter.ComboTime));
		ImGui.Text(string.Format(Loc.T("TargetingType: {0}"), DataCenter.TargetingType));
		ImGui.Spacing();
		ImGui.Text(string.Format(Loc.T("IsHostileCastingToTank: {0}"), DataCenter.IsHostileCastingToTank));
		ImGui.Text(string.Format(Loc.T("AttackedTargets: {0}"), DataCenter.AttackedTargets?.Count ?? 0));
		if (DataCenter.AttackedTargets != null)
		{
			foreach ((var id, var time) in DataCenter.AttackedTargets)
			{
				ImGui.Text(id.ToString() ?? "Unknown ID");
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
			ImGui.Text(string.Format(Loc.T("Path: {0}"), vfx.Path));
		}

		var partyMembers = DataCenter.PartyMembers;
		if (partyMembers.Count != 0)
		{
			ImGui.Text(Loc.T("Party Members:"));
			foreach (var member in partyMembers)
			{
				ImGui.Text(string.Format(Loc.T("- {0}"), member.Name));
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
				ImGui.Text(string.Format(Loc.T("- {0}"), member.Name));
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
			ImGui.Text(string.Format(Loc.T("- {0}"), dispelTarget.Name));
		}
		else
		{
			ImGui.Text(Loc.T("Dispel Target: None"));
		}

		ImGui.Text(string.Format(Loc.T("DPSTaken: {0}"), DataCenter.DPSTaken));
		ImGui.Text(string.Format(Loc.T("CurrentRotation: {0}"), DataCenter.CurrentRotation));
		ImGui.Text(string.Format(Loc.T("Job: {0}"), DataCenter.Job));
		ImGui.Text(string.Format(Loc.T("JobRange: {0}"), DataCenter.JobRange));
		ImGui.Text(string.Format(Loc.T("Job Role: {0}"), DataCenter.Role));
		ImGui.Text(string.Format(Loc.T("Have pet: {0}"), DataCenter.HasPet()));
		ImGui.Text(string.Format(Loc.T("Hostile Near Count: {0}"), DataCenter.NumberOfHostilesInRange));
		ImGui.Text(string.Format(Loc.T("Hostile Near Count Max Range: {0}"), DataCenter.NumberOfHostilesInMaxRange));
		ImGui.Text(string.Format(Loc.T("Have Companion: {0}"), DataCenter.HasCompanion));
		ImGui.Text(string.Format(Loc.T("MP: {0}"), DataCenter.CurrentMp));
		ImGui.Text(string.Format(Loc.T("Count Down: {0}"), Service.CountDownTime));

		ImGui.Spacing();
		ImGui.Text(Loc.T("Statuses:"));
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
				ImGui.TextUnformatted(status.GameData.Value.Name.ToString());
				_ = ImGui.TableNextColumn();
				ImGui.TextUnformatted(status.StatusId.ToString());
				_ = ImGui.TableNextColumn();
				ImGui.TextUnformatted(source);
				_ = ImGui.TableNextColumn();
				ImGui.TextUnformatted(stackDisplay);
				_ = ImGui.TableNextColumn();
				ImGui.TextUnformatted(timeDisplay);
			}
		}
	}

	private static void DrawRaiseInfo()
	{
		ImGui.Text(string.Format(Loc.T("Can Raise: {0}"), DataCenter.CanRaise()));
		ImGui.Text(string.Format(Loc.T("Death Target: {0}"), DataCenter.DeathTarget));

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
				ImGui.Text(string.Format(Loc.T("- {0}"), member.Name));
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
				ImGui.Text(string.Format(Loc.T("- {0}"), member.Name));
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
		ImGui.Text(string.Format(Loc.T("DC State: {0}"), DataCenter.State));
		ImGui.Text(string.Format(Loc.T("Your combat state: {0}"), DataCenter.InCombat));
		ImGui.Text(string.Format(Loc.T("Combat Time: {0}"), DataCenter.CombatTimeRaw));
		ImGui.Text(string.Format(Loc.T("TerritoryID: {0}"), DataCenter.TerritoryID));
		ImGui.Text(string.Format(Loc.T("TerritoryType: {0}"), DataCenter.Territory?.ContentType));
		ImGui.Text(string.Format(Loc.T("Is in Alliance Raid: {0}"), DataCenter.IsInAllianceRaid));
		ImGui.Spacing();
		ImGui.Text(string.Format(Loc.T("IsPvP: {0}"), DataCenter.IsPvP));
		ImGui.Text(string.Format(Loc.T("IsInFate: {0}"), DataCenter.IsInFate));
		if ((IntPtr)FateManager.Instance() != IntPtr.Zero)
		{
			ImGui.Text(string.Format(Loc.T("Fate ID: {0}"), DataCenter.PlayerFateId));
		}
		ImGui.Spacing();
		ImGui.Text(string.Format(Loc.T("IsInWindurst: {0}"), DataCenter.IsInWindurst));
		ImGui.Spacing();
		ImGui.Text(string.Format(Loc.T("In Field Operations: {0}"), DataCenter.IsInFieldOperations));
		ImGui.Text(string.Format(Loc.T("In Field Raid: {0}"), DataCenter.IsInFieldRaid));
		ImGui.Spacing();
		if (DataCenter.IsInBozjanFieldOp)
		{
			ImGui.Text(string.Format(Loc.T("IsInBozjanFieldOp: {0}"), DataCenter.IsInBozjanFieldOp));
			ImGui.Text(string.Format(Loc.T("IsInBozjanFieldOpCE: {0}"), DataCenter.IsInBozjanFieldOpCE));
			ImGui.Text(string.Format(Loc.T("IsInDelubrumNormal: {0}"), DataCenter.IsInDelubrumNormal));
			ImGui.Text(string.Format(Loc.T("IsInDelubrumSavage: {0}"), DataCenter.IsInDelubrumSavage));
			ImGui.Text(string.Format(Loc.T("IsInBozja: {0}"), DataCenter.IsInBozja));
		}
		if (DataCenter.IsInOccultCrescentOp)
		{
			ImGui.Text(string.Format(Loc.T("In North Horn: {0}"), DataCenter.IsInNorthHorn));
			ImGui.Text(string.Format(Loc.T("In South Horn: {0}"), DataCenter.IsInSouthHorn));
			ImGui.Text(string.Format(Loc.T("Is In Forked Tower Blood: {0}"), DataCenter.IsInForkedTowerBlood));
			ImGui.Text(string.Format(Loc.T("FreelancerLevel: {0}"), DutyRotation.FreelancerLevel));
			ImGui.Text(string.Format(Loc.T("KnightLevel: {0}"), DutyRotation.KnightLevel));
			ImGui.Text(string.Format(Loc.T("MonkLevel: {0}"), DutyRotation.MonkLevel));
			ImGui.Text(string.Format(Loc.T("BardLevel: {0}"), DutyRotation.BardLevel));
			ImGui.Text(string.Format(Loc.T("ChemistLevel: {0}"), DutyRotation.ChemistLevel));
			ImGui.Text(string.Format(Loc.T("TimeMageLevel: {0}"), DutyRotation.TimeMageLevel));
			ImGui.Text(string.Format(Loc.T("CannoneerLevel: {0}"), DutyRotation.CannoneerLevel));
			ImGui.Text(string.Format(Loc.T("OracleLevel: {0}"), DutyRotation.OracleLevel));
			ImGui.Text(string.Format(Loc.T("BerserkerLevel: {0}"), DutyRotation.BerserkerLevel));
			ImGui.Text(string.Format(Loc.T("RangerLevel: {0}"), DutyRotation.RangerLevel));
			ImGui.Text(string.Format(Loc.T("ThiefLevel: {0}"), DutyRotation.ThiefLevel));
			ImGui.Text(string.Format(Loc.T("SamuraiLevel: {0}"), DutyRotation.SamuraiLevel));
			ImGui.Text(string.Format(Loc.T("GeomancerLevel: {0}"), DutyRotation.GeomancerLevel));
			ImGui.Text(string.Format(Loc.T("MysticKnightLevel: {0}"), DutyRotation.MysticKnightLevel));
			ImGui.Text(string.Format(Loc.T("DancerLevel: {0}"), DutyRotation.DancerLevel));
			ImGui.Text(string.Format(Loc.T("NinjaLevel: {0}"), DutyRotation.NinjaLevel));
			ImGui.Text(string.Format(Loc.T("WhiteMageLevel: {0}"), DutyRotation.WhiteMageLevel));
			ImGui.Text(string.Format(Loc.T("BlackMageLevel: {0}"), DutyRotation.BlackMageLevel));
			ImGui.Text(string.Format(Loc.T("DragoonLevel: {0}"), DutyRotation.DragoonLevel));
			ImGui.Text(string.Format(Loc.T("SummonerLevel: {0}"), DutyRotation.SummonerLevel));
			ImGui.Text(string.Format(Loc.T("BlueMageLevel: {0}"), DutyRotation.BlueMageLevel));
			ImGui.Text(string.Format(Loc.T("RedMageLevel: {0}"), DutyRotation.RedMageLevel));
			ImGui.Text(string.Format(Loc.T("NecromancerLevel: {0}"), DutyRotation.NecromancerLevel));
		}
		ImGui.Text(string.Format(Loc.T("InVariantDungeon: {0}"), DataCenter.InVariantDungeon));
		ImGui.Text(string.Format(Loc.T("The Merchant's Tale Advanced: {0}"), DataCenter.TheMerchantsTaleAdvanced));
		ImGui.Text(string.Format(Loc.T("The Merchant's Tale: {0}"), DataCenter.TheMerchantsTale));
		ImGui.Text(string.Format(Loc.T("AloaloIsland: {0}"), DataCenter.AloaloIsland));
		ImGui.Text(string.Format(Loc.T("MountRokkon: {0}"), DataCenter.MountRokkon));
		ImGui.Text(string.Format(Loc.T("SildihnSubterrane: {0}"), DataCenter.SildihnSubterrane));
		ImGui.Spacing();
		ImGui.Text(string.Format(Loc.T("AreHostilesCastingKnockback: {0}"), DataCenter.AreHostilesCastingKnockback));
		ImGui.Text(string.Format(Loc.T("IsHostileCastingAOE: {0}"), DataCenter.IsHostileCastingAOE));
		ImGui.Text(string.Format(Loc.T("IsHostileCastingToTank: {0}"), DataCenter.IsHostileCastingToTank));
		ImGui.Text(string.Format(Loc.T("IsHostileCastingStop: {0}"), DataCenter.IsHostileCastingStop));
		ImGui.Spacing();
		ImGui.Text(string.Format(Loc.T("IsCastingMultiHit: {0}"), DataCenter.IsCastingMultiHit()));
		ImGui.Text(string.Format(Loc.T("IsCastingAreaVfx: {0}"), DataCenter.IsCastingAreaVfx()));
		ImGui.Text(string.Format(Loc.T("IsCastingTankVfx: {0}"), DataCenter.IsCastingTankVfx()));
		ImGui.Text(string.Format(Loc.T("TankbusterTargets: {0}"), DataCenter.TankbusterTargetIds.Count));
		ImGui.Spacing();
		ImGui.Text(string.Format(Loc.T("IsInM11S: {0}"), DataCenter.IsInM11S));
		ImGui.Text(string.Format(Loc.T("IsTyrantCastingSpecialIndicator2: {0}"), DataCenter.IsTyrantCastingSpecialIndicator2()));
		ImGui.Text(string.Format(Loc.T("IsLichCastingSpecialIndicator: {0}"), DataCenter.IsLichCastingSpecialIndicator()));
	}

	private static void DrawParty()
	{
		ImGui.Text(string.Format(Loc.T("Number of Party Members: {0}"), DataCenter.PartyMembers.Count));
		ImGui.Text(string.Format(Loc.T("Number of Alliance Members: {0}"), DataCenter.AllianceMembers.Count));
		ImGui.Text(string.Format(Loc.T("Average Party HP Percent: {0}"), DataCenter.PartyMembersAverHP * 100));
		ImGui.Text(string.Format(Loc.T("Average Lowest Party HP Percent: {0}"), DataCenter.LowestPartyMembersAverHP * 100));
		var doomedCount = 0;
		foreach (var member in DataCenter.PartyMembers)
		{
			if (member.DoomNeedHealing())
			{
				doomedCount++;
			}
		}
		ImGui.Text(string.Format(Loc.T("Number of Party Members with Doomed To Heal status: {0}"), doomedCount));


		if (Player.Object != null && Player.Object.IsJobs(Job.AST))
		{
			var spear = ActionTargetInfo.FindTargetByType(DataCenter.PartyMembers, TargetType.TheSpear, 0, SpecialActionType.None, TargetType.TheSpear, true);
			var balance = ActionTargetInfo.FindTargetByType(DataCenter.PartyMembers, TargetType.TheBalance, 0, SpecialActionType.None, TargetType.TheBalance, true);
			ImGui.Spacing();
			ImGui.Text(Loc.T("AST Card Targets (Preview):"));
			ImGui.Text(string.Format(Loc.T("- The Spear: {0}"), spear?.Name ?? Loc.T("None")));
			ImGui.Text(string.Format(Loc.T("- The Balance: {0}"), balance?.Name ?? Loc.T("None")));
			ImGui.Spacing();
		}

		foreach (var p in DataCenter.PartyMembers)
		{
			var text = $"Name: {p.Name}, HP: {p.GetEffectiveHpPercent()}%";

			ImGui.Text(text);
		}

		foreach (var p in Svc.Party)
		{
			if (p.GameObject is not IBattleChara b)
			{
				continue;
			}

			var text = $"Name: {b.Name}, In Combat: {b.InCombat()}";
			if (b.TimeAlive() > 0)
			{
				text += $", Time Alive: {b.TimeAlive()}";
			}

			if (b.TimeDead() > 0)
			{
				text += $", Time Dead: {b.TimeDead()}";
			}

			ImGui.Text(text);
		}
		ImGui.Spacing();
		ImGui.Text(string.Format(Loc.T("Limit Break: {0}"), CustomRotation.LimitBreakLevel));
		ImGui.Spacing();
		ImGui.Text(Loc.T("Object Data"));
		ImGui.Text(string.Format(Loc.T("NumberOfPartyMembersInRangeOf 5m: {0}"), DataCenter.NumberOfPartyMembersInRangeOf(5)));
		ImGui.Text(string.Format(Loc.T("AllTargets Count: {0}"), DataCenter.AllTargets.Count));
		ImGui.Text(string.Format(Loc.T("AllHostileTargets Count: {0}"), DataCenter.AllHostileTargets.Count));
		foreach (var item in DataCenter.AllHostileTargets)
		{
			ImGui.Text(item.Name.ToString());
		}
		ImGui.Spacing();
		ImGui.Text(Loc.T("Party Composition:"));
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
				var jobName = classJob.Abbreviation.ToString() ?? classJob.Name.ToString() ?? $"Job #{i}";
				ImGui.Text(string.Format(Loc.T("{0}: {1}"), i + 1, jobName));
			}
		}
		ImGui.Spacing();
		var mitigationFraction = CustomRotation.GetCurrentMitigationPercent();
		ImGui.Text(string.Format(Loc.T("Current Mitigation Percent: {0:F1}%"), mitigationFraction * 100f));
		ImGui.Text(string.Format(Loc.T("Current Mitigation Percent RAW: {0}"), mitigationFraction));

		ImGui.Text(string.Format(Loc.T("Is Magical Damage Incoming: {0}"), CustomRotation.IsMagicalDamageIncoming));
	}

	private static unsafe void DrawTargetData()
	{
		if (Svc.Targets.Target is not IBattleChara target)
		{
			return;
		}

		ImGui.Text(string.Format(Loc.T("Height: {0}"), target.Struct()->Height));
		ImGui.Text(string.Format(Loc.T("Kind: {0}"), target.GetObjectKind()));
		ImGui.Text(string.Format(Loc.T("SubKind: {0}"), target.GetBattleNPCSubKind()));

		var owner = Svc.Objects.SearchById(target.OwnerId);
		if (owner != null)
		{
			ImGui.Text(string.Format(Loc.T("Owner: {0}"), owner.Name));
		}

		if (target is IBattleChara battleChara)
		{
			ImGui.Text(string.Format(Loc.T("IsCasting: {0}"), battleChara.IsCasting));
			ImGui.Text(string.Format(Loc.T("CastID: {0}"), battleChara.CastInfo.ActionId));
			ImGui.Text(string.Format(Loc.T("Is Status Capped: {0}"), StatusHelper.IsStatusCapped(battleChara)));
			ImGui.Text(string.Format(Loc.T("CanSee: {0}"), battleChara.CanSee()));
			ImGui.Text(string.Format(Loc.T("CanBeRaised: {0}"), battleChara.CanBeRaised()));
			ImGui.Text(string.Format(Loc.T("HP: {0} / {1}"), battleChara.CurrentHp, battleChara.MaxHp));
			ImGui.Text(string.Format(Loc.T("HealthRatio: {0}"), battleChara.GetHealthRatio()));
			ImGui.Text(string.Format(Loc.T("HitboxRadius: {0}"), battleChara.HitboxRadius));
			ImGui.Text(string.Format(Loc.T("Distance To Player: {0}"), battleChara.DistanceToPlayer()));
			ImGui.Spacing();
			ImGui.Text(string.Format(Loc.T("NamePlate Icon ID: {0}"), battleChara.GetNamePlateIcon()));
			ImGui.Text(string.Format(Loc.T("Event Type: {0}"), battleChara.GetEventType()));
			ImGui.Text(string.Format(Loc.T("TargetCharaCondition: {0}"), battleChara.TargetCharaCondition()));
			var npcName = string.Empty;
			var npcEnumName = string.Empty;
			if (battleChara.NameId != 0)
			{
				var bnpcName = Service.GetSheet<Lumina.Excel.Sheets.BNpcName>().GetRow(battleChara.NameId);
				npcName = bnpcName.Singular.ToString();

				if (Enum.IsDefined(typeof(NPCName), battleChara.NameId))
				{
					npcEnumName = $"{Enum.GetName(typeof(NPCName), battleChara.NameId)}";
				}
			}
			ImGui.Text(string.Format(Loc.T("NPC Name: {0}"), npcEnumName));
			ImGui.Text(string.Format(Loc.T("Name Id: {0}"), battleChara.NameId));
			ImGui.Text(string.Format(Loc.T("Data Id: {0}"), battleChara.BaseId));
			ImGui.Spacing();
			ImGui.Text(string.Format(Loc.T("Is Attackable: {0}"), battleChara.IsAttackable()));
			ImGui.Text(string.Format(Loc.T("Is Others Players Mob: {0}"), battleChara.IsOthersPlayersMob()));
			ImGui.Text(string.Format(Loc.T("Is Alliance: {0}"), battleChara.IsAllianceMember()));
			ImGui.Text(string.Format(Loc.T("Is Enemy Action Check: {0}"), battleChara.IsEnemy()));
			ImGui.Text(string.Format(Loc.T("IsSpecialExecptionImmune: {0}"), battleChara.IsSpecialExceptionImmune()));
			ImGui.Text(string.Format(Loc.T("IsSpecialImmune: {0}"), battleChara.IsSpecialImmune()));
			ImGui.Text(string.Format(Loc.T("IsTopPriorityNamedHostile: {0}"), battleChara.IsTopPriorityNamedHostile()));
			ImGui.Text(string.Format(Loc.T("IsTopPriorityHostile: {0}"), battleChara.IsTopPriorityHostile()));
			ImGui.Spacing();
			ImGui.Text(string.Format(Loc.T("FateID: {0}"), battleChara.FateId().ToString() ?? string.Empty));
			ImGui.Text(string.Format(Loc.T("EventType: {0}"), battleChara.GetEventType().ToString() ?? string.Empty));
			if (DataCenter.IsInBozja)
			{
				ImGui.Text(string.Format(Loc.T("IsBozjanCEFateMob: {0}"), battleChara.IsBozjanCEMob()));
			}
			ImGui.Spacing();
			if (DataCenter.IsInOccultCrescentOp)
			{
				ImGui.Text(string.Format(Loc.T("IsOccultCEMob: {0}"), battleChara.IsOccultCEMob()));
				ImGui.Text(string.Format(Loc.T("IsOccultFateMob: {0}"), battleChara.IsOccultFateMob()));
				ImGui.Text(string.Format(Loc.T("IsOCUndeadTarget: {0}"), battleChara.IsOCUndeadTarget()));
				ImGui.Text(string.Format(Loc.T("IsOCSlowgaImmuneTarget: {0}"), battleChara.IsOCSlowgaImmuneTarget()));
				ImGui.Text(string.Format(Loc.T("IsOCDoomImmuneTarget: {0}"), battleChara.IsOCDoomImmuneTarget()));
				ImGui.Text(string.Format(Loc.T("IsOCStunImmuneTarget: {0}"), battleChara.IsOCStunImmuneTarget()));
				ImGui.Text(string.Format(Loc.T("IsOCFreezeImmuneTarget: {0}"), battleChara.IsOCFreezeImmuneTarget()));
				ImGui.Text(string.Format(Loc.T("IsOCBlindImmuneTarget: {0}"), battleChara.IsOCBlindImmuneTarget()));
				ImGui.Text(string.Format(Loc.T("IsOCParalysisImmuneTarget: {0}"), battleChara.IsOCParalysisImmuneTarget()));
				ImGui.Spacing();
			}
			ImGui.Text(string.Format(Loc.T("Is Current Focus Target: {0}"), battleChara.IsFocusTarget()));
			ImGui.Text(string.Format(Loc.T("TTK: {0}"), battleChara.GetTTK()));
			ImGui.Text(string.Format(Loc.T("Next DoT tick: {0:F2}s"), battleChara.TimeToNextDotTick()));
			ImGui.Text(string.Format(Loc.T("Is Boss TTK: {0}"), battleChara.IsBossFromTTK()));
			ImGui.Text(string.Format(Loc.T("Is Boss Icon: {0}"), battleChara.IsBossFromIcon()));
			ImGui.Text(string.Format(Loc.T("Rank: {0}"), battleChara.GetObjectNPC()?.Rank.ToString() ?? string.Empty));
			ImGui.Text(string.Format(Loc.T("Has Positional: {0}"), battleChara.HasPositional()));
			ImGui.Text(string.Format(Loc.T("IsNpcPartyMember: {0}"), battleChara.IsNpcPartyMember()));
			ImGui.Text(string.Format(Loc.T("IsPlayerCharacterChocobo: {0}"), battleChara.IsPlayerCharacterChocobo()));
			ImGui.Text(string.Format(Loc.T("IsFriendlyBattleNPC: {0}"), battleChara.IsFriendlyBattleNPC()));
			ImGui.Text(string.Format(Loc.T("Is Dying: {0}"), battleChara.IsDying()));
			ImGui.Text(string.Format(Loc.T("Is Alive: {0}"), battleChara.IsAlive()));
			ImGui.Text(string.Format(Loc.T("Is Party: {0}"), battleChara.IsParty()));
			ImGui.Text(string.Format(Loc.T("Is Healer: {0}"), battleChara.IsJobCategory(JobRole.Healer)));
			ImGui.Text(string.Format(Loc.T("Is DPS: {0}"), battleChara.IsJobCategory(JobRole.AllDPS)));
			ImGui.Text(string.Format(Loc.T("Is Tank: {0}"), battleChara.IsJobCategory(JobRole.Tank)));
			ImGui.Text(string.Format(Loc.T("Is Alliance: {0}"), battleChara.IsAllianceMember()));
			ImGui.Text(string.Format(Loc.T("CanProvoke: {0}"), battleChara.CanProvoke()));
			ImGui.Text(string.Format(Loc.T("StatusFlags: {0}"), battleChara.StatusFlags));
			ImGui.Text(string.Format(Loc.T("InView: {0}"), Svc.GameGui.WorldToScreen(battleChara.Position, out _)));
			ImGui.Text(string.Format(Loc.T("Enemy Positional: {0}"), battleChara.FindEnemyPositional()));
			ImGui.Text(string.Format(Loc.T("NameplateKind: {0}"), battleChara.GetNameplateKind()));
			ImGui.Text(string.Format(Loc.T("BattleNPCSubKind: {0}"), battleChara.GetBattleNPCSubKind()));
			ImGui.Text(string.Format(Loc.T("Is Top Priority Hostile: {0}"), battleChara.IsTopPriorityHostile()));
			ImGui.Text(string.Format(Loc.T("Targetable: {0}"), battleChara.Struct()->Character.GameObject.TargetableStatus));
			if (DataCenter.IsInMaskedCarnivale)
			{
				ImGui.Spacing();
				ImGui.Text(string.Format(Loc.T("Aspect Resistance (Fire): {0}"), MaskedCarnivaleHelper.GetAspectResistance(battleChara, Aspect.Fire)));
				ImGui.Text(string.Format(Loc.T("Aspect Resistance (Ice): {0}"), MaskedCarnivaleHelper.GetAspectResistance(battleChara, Aspect.Ice)));
				ImGui.Text(string.Format(Loc.T("Aspect Resistance (Wind): {0}"), MaskedCarnivaleHelper.GetAspectResistance(battleChara, Aspect.Wind)));
				ImGui.Text(string.Format(Loc.T("Aspect Resistance (Earth): {0}"), MaskedCarnivaleHelper.GetAspectResistance(battleChara, Aspect.Earth)));
				ImGui.Text(string.Format(Loc.T("Aspect Resistance (Lightning): {0}"), MaskedCarnivaleHelper.GetAspectResistance(battleChara, Aspect.Lightning)));
				ImGui.Text(string.Format(Loc.T("Aspect Resistance (Water): {0}"), MaskedCarnivaleHelper.GetAspectResistance(battleChara, Aspect.Water)));
				ImGui.Text(string.Format(Loc.T("Aspect Resistance (Slashing): {0}"), MaskedCarnivaleHelper.GetAspectResistance(battleChara, Aspect.Slashing)));
				ImGui.Text(string.Format(Loc.T("Aspect Resistance (Piercing): {0}"), MaskedCarnivaleHelper.GetAspectResistance(battleChara, Aspect.Piercing)));
				ImGui.Text(string.Format(Loc.T("Aspect Resistance (Blunt): {0}"), MaskedCarnivaleHelper.GetAspectResistance(battleChara, Aspect.Blunt)));
				ImGui.Spacing();
				ImGui.Text(string.Format(Loc.T("IsVulnerableToSlow: {0}"), MaskedCarnivaleHelper.IsVulnerableToSlow(battleChara)));
				ImGui.Text(string.Format(Loc.T("IsVulnerableToPetrification: {0}"), MaskedCarnivaleHelper.IsVulnerableToPetrification(battleChara)));
				ImGui.Text(string.Format(Loc.T("IsVulnerableToParalysis: {0}"), MaskedCarnivaleHelper.IsVulnerableToParalysis(battleChara)));
				ImGui.Text(string.Format(Loc.T("IsVulnerableToInterruption: {0}"), MaskedCarnivaleHelper.IsVulnerableToInterruption(battleChara)));
				ImGui.Text(string.Format(Loc.T("IsVulnerableToBlind: {0}"), MaskedCarnivaleHelper.IsVulnerableToBlind(battleChara)));
				ImGui.Text(string.Format(Loc.T("IsVulnerableToStun: {0}"), MaskedCarnivaleHelper.IsVulnerableToStun(battleChara)));
				ImGui.Text(string.Format(Loc.T("IsVulnerableToSleep: {0}"), MaskedCarnivaleHelper.IsVulnerableToSleep(battleChara)));
				ImGui.Text(string.Format(Loc.T("IsVulnerableToBind: {0}"), MaskedCarnivaleHelper.IsVulnerableToBind(battleChara)));
				ImGui.Text(string.Format(Loc.T("IsVulnerableToHeavy: {0}"), MaskedCarnivaleHelper.IsVulnerableToHeavy(battleChara)));
				ImGui.Text(string.Format(Loc.T("IsVulnerableToFlatOrDeath: {0}"), MaskedCarnivaleHelper.IsVulnerableToFlatOrDeath(battleChara)));
			}
			ImGui.Spacing();
			ImGui.Text(Loc.T("Statuses:"));
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
					ImGui.TextUnformatted(status.GameData.Value.Name.ToString());
					_ = ImGui.TableNextColumn();
					ImGui.TextUnformatted(status.StatusId.ToString());
					_ = ImGui.TableNextColumn();
					ImGui.TextUnformatted(source);
					_ = ImGui.TableNextColumn();
					ImGui.TextUnformatted(stackDisplay);
					_ = ImGui.TableNextColumn();
					ImGui.TextUnformatted(timeDisplay);
				}
			}
		}
	}

	private static void DrawNextAction()
	{
		ImGui.Text(DataCenter.CurrentRotation?.GetAttributes()?.Name);
		ImGui.Text(DataCenter.SpecialType.ToString());

		ImGui.Text(ActionUpdater.NextAction?.Name ?? Loc.T("null"));
		ImGui.Text(string.Format(Loc.T("GCD Total: {0}"), DataCenter.DefaultGCDTotal));
		ImGui.Text(string.Format(Loc.T("GCD Remain: {0}"), DataCenter.DefaultGCDRemain));
		ImGui.Text(string.Format(Loc.T("GCD Elapsed: {0}"), DataCenter.DefaultGCDElapsed));
		ImGui.Text(string.Format(Loc.T("Calculated Action Ahead: {0}"), DataCenter.CalculatedActionAhead));
		ImGui.Text(string.Format(Loc.T("Animation Lock Delay: {0}"), DataCenter.AnimationLock));
	}

	private static void DrawLastAction()
	{
		DrawAction(DataCenter.LastAction, nameof(DataCenter.LastAction));
		DrawAction(DataCenter.LastAbility, nameof(DataCenter.LastAbility));
		DrawAction(DataCenter.LastGCD, nameof(DataCenter.LastGCD));
		DrawAction(DataCenter.LastComboAction, nameof(DataCenter.LastComboAction));
		ImGui.Text(string.Format(Loc.T("IsLastActionAbility: {0}"), IActionHelper.IsLastActionAbility()));
		ImGui.Text(string.Format(Loc.T("IsLastActionGCD: {0}"), IActionHelper.IsLastActionGCD()));
	}

	private static string _ipcTestText = "Sent data";

	private static void DrawIPC()
	{
		ImGui.SetNextItemWidth(200 * Scale);
		ImGui.InputText("##IPCTextBox", ref _ipcTestText, 128);
		ImGui.SameLine();
		if (ImGui.Button(Loc.T("Test Function")))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.Test(_ipcTestText);
		}

		if (ImGui.Button(Loc.T("Test ChangeOperatingMode to Manual IPC")))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.ChangeOperatingMode(StateCommandType.Manual);
		}

		if (ImGui.Button(Loc.T("Test ChangeOperatingMode to Off IPC")))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.ChangeOperatingMode(StateCommandType.Off);
		}

		if (ImGui.Button(Loc.T("Test TriggerSpecialState DefenseArea IPC")))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.TriggerSpecialState(SpecialCommandType.DefenseArea);
		}

		if (ImGui.Button(Loc.T("Test TriggerSpecialState AntiKnockback IPC")))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.TriggerSpecialState(SpecialCommandType.AntiKnockback);
		}

		if (ImGui.Button(Loc.T("Test Setting IPC (Changing engage setting to All Target)")))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.OtherCommand(OtherCommandType.Settings, "HostileType AllTargetsCanAttack");
		}

		if (ImGui.Button(Loc.T("Test OtherCommand DoAction IPC (Magick Barrier on RDM)")))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.OtherCommand(OtherCommandType.DoActions, "Magick Barrier-5");
		}

		if (ImGui.Button(Loc.T("Test ToggleAction IPC (Magick Barrier on RDM)")))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.OtherCommand(OtherCommandType.ToggleActions, "Magick Barrier");
		}

		if (ImGui.Button(Loc.T("Test ActionCommand IPC (Magick Barrier on RDM)")))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.ActionCommand("Magick Barrier", 7);
		}
		if (ImGui.Button(Loc.T("Test AutodutyChangeOperatingMode IPC (AutoDuty, HighHPPercent)")))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.AutodutyChangeOperatingMode(StateCommandType.AutoDuty, TargetingType.HighHPPercent);
		}
		if (ImGui.Button(Loc.T("Test ChangeOperatingModeWithOverrides IPC (Auto, AllTargetsCanAttack, TargetFreely on, AutoOffAfterCombat off, NPC heal/raise on)")))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.ChangeOperatingModeWithOverrides(StateCommandType.Auto, TargetHostileType.AllTargetsCanAttack, SettingOverride.ForceOn, SettingOverride.ForceOff, SettingOverride.ForceOn);
		}
		if (ImGui.Button(Loc.T("Test AutodutyChangeOperatingModeWithOverrides IPC (AutoDuty, HighHPPercent, AllTargetsCanAttack, others unchanged)")))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.AutodutyChangeOperatingModeWithOverrides(StateCommandType.AutoDuty, TargetingType.HighHPPercent, TargetHostileType.AllTargetsCanAttack, SettingOverride.UseSetting, SettingOverride.UseSetting, SettingOverride.UseSetting);
		}
		ImGui.Text($"IPC overrides: {DataCenter.IpcOverrides?.ToString() ?? "None"} (Active: {DataCenter.ActiveIpcOverrides is not null})");
		ImGui.Text(string.Format(Loc.T("Effective: HostileType {0}, TargetFreely {1}, AutoOffAfterCombat {2}, FriendlyPartyNpcHealRaise {3}"), DataCenter.CurrentTargetToHostileType, DataCenter.TargetFreelyEnabled, DataCenter.AutoOffAfterCombatEnabled, DataCenter.FriendlyPartyNpcHealRaiseEnabled));
		if (ImGui.Button(Loc.T("Test Henchman IPC support")))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.ChangeOperatingMode(StateCommandType.Henched);
		}
	}

	private static void DrawBMRData()
	{
		ImGui.Text(string.Format(Loc.T("Cooldown Planner IPC Enabled: {0}"), BMRPlan_IPCSubscriber.IsEnabled));
		ImGui.Text(string.Format(Loc.T("BMRPlannedActionsCount: {0}"), DataCenter.BMRPlannedActions.Count));
		ImGui.Text(string.Format(Loc.T("BMRForceCancelCast: {0}"), DataCenter.BMRForceCancelCast));
		ImGui.Text(string.Format(Loc.T("BMRForceCancelCastAI: {0}"), DataCenter.BMRForceCancelCastAI));
		ImGui.Text(string.Format(Loc.T("BMRIsMoving: {0}"), DataCenter.BMRIsMoving));
	}

	private static void DrawAction(ActionID id, string type)
	{
		ImGui.Text(string.Format(Loc.T("{0}: {1}"), type, id));
	}
}
