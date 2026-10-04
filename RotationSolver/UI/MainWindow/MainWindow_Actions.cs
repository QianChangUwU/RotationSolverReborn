using RotationSolver.Basic.Localization;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using RotationSolver.Data;
using RotationSolver.UI.Material;
using RotationSolver.Updaters;

namespace RotationSolver.UI;

public partial class MainWindow
{
	private static unsafe void DrawActions()
	{
		DrawPageIntro(UiString.ConfigWindow_Actions_Description.GetDescription());

		using var table = ImRaii.Table("Rotation Solver Actions", 2, ImGuiTableFlags.Resizable);

		if (table)
		{
			ImGui.TableSetupColumn(Loc.T("Action Column"), ImGuiTableColumnFlags.WidthFixed, ImGui.GetWindowWidth() / 2);
			ImGui.TableNextColumn();

			if (_actionsList != null)
			{
				_actionsList.ClearCollapsingHeader();

				var allGroupedActions = RotationUpdater.AllGroupedActions;
				if (DataCenter.CurrentRotation != null && allGroupedActions != null)
				{
					var size = 30 * Scale;
					var count = Math.Max(1, (int)MathF.Floor(ImGui.GetColumnWidth() / ((size * 1.1f) + ImGui.GetStyle().ItemSpacing.X)));
					foreach (var pair in allGroupedActions)
					{
						_actionsList.AddCollapsingHeader(() => pair.Key, () =>
						{
							var index = 0;

							List<IAction> source = [.. pair];

							var byAdjusted = new Dictionary<uint, List<IAction>>();
							for (var i = 0; i < source.Count; i++)
							{
								var a = source[i];
								if (!byAdjusted.TryGetValue(a.AdjustedID, out var list))
								{
									list = [];
									byAdjusted[a.AdjustedID] = list;
								}
								list.Add(a);
							}

							var groups = new List<(uint AdjustedID, byte MinLevel, List<IAction> Items)>(byAdjusted.Count);
							foreach (var kv in byAdjusted)
							{
								var items = kv.Value;
								items.Sort((x, y) =>
								{
									var cmp = x.Level.CompareTo(y.Level);
									return cmp != 0 ? cmp : x.ID.CompareTo(y.ID);
								});
								var minLvl = items.Count > 0 ? items[0].Level : (byte)0;
								groups.Add((kv.Key, minLvl, items));
							}

							groups.Sort((g1, g2) =>
							{
								var cmp = g1.MinLevel.CompareTo(g2.MinLevel);
								return cmp != 0 ? cmp : g1.AdjustedID.CompareTo(g2.AdjustedID);
							});

							List<IAction> sorted = new(source.Count);
							foreach ((var AdjustedID, var MinLevel, var Items) in groups)
							{
								sorted.AddRange(Items);
							}

							foreach (var item in sorted)
							{
								if (!IconSet.GetTexture(item.IconID, out var icon))
								{
									continue;
								}

								if (index++ % count != 0)
								{
									ImGui.SameLine();
								}

								ImGui.BeginGroup();
								var cursor = ImGui.GetCursorPos();
								if (ImGuiHelper.NoPaddingNoColorImageButton(icon, Vector2.One * size, item.Name + item.ID))
								{
									_activeAction = item;
								}
								ImGuiHelper.DrawActionOverlay(cursor, size, _activeAction == item ? 1 : 0);

								if (IconSet.GetTexture("ui/uld/readycheck_hr1.tex", out var texture))
								{
									Vector2 offset = new(1 / 12f, 1 / 6f);
									ImGui.SetCursorPos(cursor + (new Vector2(0.6f, 0.7f) * size));
									ImGui.Image(texture.Handle, Vector2.One * size * 0.5f,
										new Vector2(item.IsEnabled ? 0 : 0.5f, 0) + offset,
										new Vector2(item.IsEnabled ? 0.5f : 1, 1) - offset);
								}
								ImGui.EndGroup();

								var key = $"Action Macro Usage {item.Name} {item.ID}";
								var cmd = ToCommandStr(OtherCommandType.DoActions, $"{item}-{5}");
								ImGuiHelper.DrawHotKeysPopup(key, cmd);
								ImGuiHelper.ExecuteHotKeysPopup(key, cmd, item.Name, false);
							}
						});
					}
				}

				_actionsList.Draw();
			}

			ImGui.TableNextColumn();

			DrawConfigsOfAction();
			DrawActionDebug();
		}

		static void DrawConfigsOfAction()
		{
			if (_activeAction == null)
			{
				return;
			}

			var isEnabled = _activeAction.IsEnabled;
			if (M3Widgets.RowSwitch($"{_activeAction.Name}##{_activeAction.Name} Enabled", ref isEnabled, out var enableHovered))
			{
				_activeAction.IsEnabled = isEnabled;
			}

			const string key = "Action Enable Popup";
			var cmd = ToCommandStr(OtherCommandType.ToggleActions, _activeAction.ToString()!);
			ImGuiHelper.DrawHotKeysPopup(key, cmd);
			ImGuiHelper.ExecuteHotKeysPopupAt(enableHovered, key, cmd, string.Empty, false);

			var isIntercepted = _activeAction.IsIntercepted;
			if (M3Widgets.RowSwitch($"{UiString.ConfigWindow_Actions_IsIntercepted.GetDescription()}##{_activeAction.Name}", ref isIntercepted))
			{
				_activeAction.IsIntercepted = isIntercepted;
			}

			var minHPFeatureSet = _activeAction.MinHPFeature;
			if (M3Widgets.RowSwitch($"{UiString.ConfigWindow_Actions_MinHPFeature.GetDescription()}##{_activeAction.Name}", ref minHPFeatureSet))
			{
				_activeAction.MinHPFeature = minHPFeatureSet;
			}

			if (_activeAction is IBaseAction movesAction &&
			(movesAction.Setting.SpecialType == SpecialActionType.FixedDistanceMoveForward
			|| movesAction.Setting.SpecialType == SpecialActionType.FixedDistanceMoveBackward
			|| movesAction.Setting.SpecialType == SpecialActionType.HostileMovingForward
			|| movesAction.Setting.SpecialType == SpecialActionType.FriendlyMovingForward
			|| movesAction.Setting.SpecialType == SpecialActionType.HostileFriendlyMovingForward
			|| movesAction.Setting.SpecialType == SpecialActionType.HostileMovingAttack
			|| movesAction.Setting.SpecialType == SpecialActionType.ObjectBasedMovement))
			{
				var skipPosSafety = _activeAction.SkipPositionSafetyCheck;
				if (M3Widgets.RowSwitch($"{UiString.ConfigWindow_Actions_SkipPositionSafetyCheck.GetDescription()}##{_activeAction.Name}", ref skipPosSafety))
				{
					_activeAction.SkipPositionSafetyCheck = skipPosSafety;
				}
			}

			var isRestrictedDOT = _activeAction.IsRestrictedDOT;
			if (M3Widgets.RowSwitch($"{UiString.ConfigWindow_Actions_IsRestrictedDOT.GetDescription()}##{_activeAction.Name}", ref isRestrictedDOT))
			{
				_activeAction.IsRestrictedDOT = isRestrictedDOT;
			}

			var minHPPercentSet = _activeAction.MinHPPercent;
			if (_activeAction.MinHPFeature == true)
			{
				var minHPPercentUi = Math.Clamp(_activeAction.MinHPPercent * 100f, 0f, 100f);
				if (M3Widgets.RowDragFloat($"{UiString.ConfigWindow_Actions_MinHPPercent.GetDescription()}##{_activeAction.Name}",
					ref minHPPercentUi, 0f, 100f, $"{minHPPercentUi:F1}{ConfigUnitType.Percent.ToSymbol()}"))
				{
					_activeAction.MinHPPercent = Math.Clamp(minHPPercentUi / 100f, 0f, 1f);
				}
			}

			if (_activeAction is IBaseAction a)
			{
				DrawConfigsOfBaseAction(a);
			}

			ImGui.Separator();

			static void DrawConfigsOfBaseAction(IBaseAction a)
			{
				var config = a.Config;

				ImGui.Separator();

				var ttk = config.TimeToKill;
				if (M3Widgets.RowDragFloat($"{UiString.ConfigWindow_Actions_TTK.GetDescription()}##{a}",
					ref ttk, 0, 120, $"%.1f{ConfigUnitType.Seconds.ToSymbol()}"))
				{
					config.TimeToKill = ttk;
				}
				ImguiTooltips.HoveredTooltip(ConfigUnitType.Seconds.GetDescription());

				if (a.Setting.StatusProvide != null || a.Setting.TargetStatusProvide != null)
				{
					var shouldStatus = config.ShouldCheckStatus;
					if (M3Widgets.RowSwitch($"{UiString.ConfigWindow_Actions_CheckStatus.GetDescription()}##{a}", ref shouldStatus))
					{
						config.ShouldCheckStatus = shouldStatus;
					}

					if (shouldStatus)
					{
						int StatusRefreshGcdCount = config.StatusRefreshGcdCount;
						if (M3Widgets.RowDragInt($"{UiString.ConfigWindow_Actions_GcdCount.GetDescription()}##{a}",
							ref StatusRefreshGcdCount, 1, 10))
						{
							config.StatusRefreshGcdCount = (byte)StatusRefreshGcdCount;
						}
					}
				}

				if (!a.TargetInfo.IsSingleTarget)
				{
					int aoeCount = config.AoeCount;
					if (M3Widgets.RowDragInt($"{UiString.ConfigWindow_Actions_AoeCount.GetDescription()}##{a}",
						ref aoeCount, 1, 10))
					{
						config.AoeCount = (byte)aoeCount;
					}
				}

				var ratio = config.AutoHealRatio;
				if (M3Widgets.RowDragFloat($"{UiString.ConfigWindow_Actions_HealRatio.GetDescription()}##{a}",
					ref ratio, 0, 1, $"{ratio * 100:F1}{ConfigUnitType.Percent.ToSymbol()}"))
				{
					config.AutoHealRatio = ratio;
				}
				ImguiTooltips.HoveredTooltip(ConfigUnitType.Percent.GetDescription());

			}
		}

		static void DrawActionDebug()
		{
			if (!Player.Available || !Service.Config.InDebug)
			{
				return;
			}

			if (_activeAction is IBaseAction action)
			{
				try
				{
					var target = action.Target.Target;
					ImGui.Text(Loc.T("Can Use: ") + action.CanUse(out _));
					ImGui.Spacing();
					ImGui.Spacing();
					ImGui.Text(Loc.T("ID: ") + action.Info.ID);
					ImGui.Text(Loc.T("Cast Type: ") + action.Info.CastType);
					ImGui.Text(Loc.T("GCDSingleHeal: ") + action.Config.GCDSingleHeal);
					ImGui.Text(Loc.T("MinHPPercent: ") + action.MinHPPercent);
					ImGui.Text(Loc.T("AdjustedID: ") + Service.GetAdjustedActionId(action.Info.ID));
					ImGui.Text(Loc.F($"IsQuestUnlocked: {action.Info.IsQuestUnlocked()} ({action.Action.UnlockLink.RowId})"));
					ImGui.Text(Loc.T("EnoughLevel: ") + action.EnoughLevel);
					if (!action.TargetInfo.IsSingleTarget)
					{
						ImGui.Text(Loc.T("AoeCount: ") + action.Config.AoeCount);
					}
					ImGui.Text(Loc.T("ShouldCheckStatus: ") + action.Config.ShouldCheckStatus);
					ImGui.Text(Loc.T("ShouldCheckTargetStatus: ") + action.Config.ShouldCheckTargetStatus);
					ImGui.Text(Loc.T("StatusFromSelf: ") + action.Setting.StatusFromSelf);
					ImGui.Text(Loc.T("Is Real GCD: ") + action.Info.IsRealGCD);
					ImGui.Text(Loc.T("Is PvP Action: ") + action.Info.IsPvP);

					if (ActionManager.Instance() != null && action.AdjustedID != 0)
					{
						ImGui.Text(Loc.T("Resources: ") + ActionManager.Instance()->CheckActionResources(ActionType.Action, action.AdjustedID));
						ImGui.Text(Loc.T("Status: ") + ActionManager.Instance()->GetActionStatus(ActionType.Action, action.AdjustedID));
					}
					ImGui.Text(Loc.T("Cast Time: ") + action.Info.CastTime);
					ImGui.Text(Loc.T("MP: ") + action.Info.MPNeed);
					ImGui.Text(Loc.T("HasEnoughMP: ") + action.Info.HasEnoughMP());
					ImGui.Text(Loc.T("AttackType: ") + action.Info.AttackType);
					ImGui.Text(Loc.T("Level: ") + action.Info.Level);
					ImGui.Text(Loc.T("Range: ") + action.Info.Range);
					ImGui.Text(Loc.T("EffectRange: ") + action.Info.EffectRange);
					ImGui.Text(Loc.T("Aspects: ") + string.Join(", ", action.Info.Aspects));
					ImGui.Text(Loc.T("Has One:") + action.Cooldown.HasOneCharge);
					ImGui.Text(Loc.T("Recast One: ") + action.Cooldown.RecastTimeOneChargeRaw);
					ImGui.Text(Loc.T("Recast Elapsed: ") + action.Cooldown.RecastTimeElapsed);
					ImGui.Text(Loc.T("Recast Time Elapsed One Charge: ") + action.Cooldown.RecastTimeElapsedOneCharge);
					ImGui.Text(Loc.T("Recast Time Remain One Charge: ") + action.Cooldown.RecastTimeRemainOneCharge);
					ImGui.Text(Loc.F($"Charges: {action.Cooldown.CurrentCharges} / {action.Cooldown.MaxCharges}"));

					ImGui.Text(Loc.T("IgnoreCastCheck:") + action.CanUse(out _, skipCastingCheck: true));
					action.CanUse(out _, skipCastingCheck: true, skipStatusProvideCheck: true, skipTargetStatusNeedCheck: true, skipAoeCheck: true);
					if (target == null)
					{
						ImGui.TextColored(ImGuiColors.DalamudRed, Loc.T("Target is not set."));
					}
					else if (target != null)
					{
						ImGui.Text(Loc.T("Target Name: ") + action.Target.Target?.Name ?? string.Empty);
						ImGui.Text(Loc.T("AffectedTarget Count: ") + (action.Target.AffectedTargets?.Length ?? 0));

						if (IsMovingSpecialType(action.Setting.SpecialType))
						{
							var safetyResult = GetMovementSafetyStatus(action);
							var color = safetyResult.Status switch
							{
								MovementSafetyStatus.Safe => ImGuiColors.HealerGreen,
								MovementSafetyStatus.NotSafe => ImGuiColors.DalamudRed,
								MovementSafetyStatus.NotApplicable => ImGuiColors.DalamudGrey,
								_ => ImGuiColors.DalamudWhite
							};
							var statusText = safetyResult.Status switch
							{
								MovementSafetyStatus.Safe => Loc.T("Pass"),
								MovementSafetyStatus.NotSafe => Loc.T("Fail"),
								MovementSafetyStatus.NotApplicable => Loc.T("N/A"),
								_ => Loc.T("Unknown")
							};
							ImGui.TextColored(color, Loc.F($"BMR Safetycheck: {statusText}"));
							if (!string.IsNullOrEmpty(safetyResult.Reason))
							{
								ImGui.Text(Loc.F($"Reason: {safetyResult.Reason}"));
							}
						}
					}
				}
				catch (Exception ex)
				{
					ImGui.TextColored(ImGuiColors.DalamudRed, Loc.T("Error: ") + ex.Message);
				}
			}
			else if (_activeAction is IBaseItem item)
			{
				try
				{
					if (ActionManager.Instance() != null)
					{
						ImGui.Text(Loc.T("Status: ") + ActionManager.Instance()->GetActionStatus(ActionType.Item, item.ID).ToString());
						ImGui.Text(Loc.T("Status HQ: ") + ActionManager.Instance()->GetActionStatus(ActionType.Item, item.ID + 1000000).ToString());
						var remain = ActionManager.Instance()->GetRecastTime(ActionType.Item, item.ID) - ActionManager.Instance()->GetRecastTimeElapsed(ActionType.Item, item.ID);
						ImGui.Text(Loc.T("remain: ") + remain.ToString());
						ImGui.Text(Loc.T("ID: ") + item.ID.ToString());
						ImGui.Text(Loc.T("A4: ") + item.A4.ToString());
						ImGui.Text(Loc.T("AdjustedID: ") + item.AdjustedID.ToString());
					}

					ImGui.Text(Loc.T("CanUse: ") + item.CanUse(out _, true).ToString());

					if (item is HpPotionItem healPotionItem)
					{
						ImGui.Text(Loc.T("MaxHP:") + healPotionItem.MaxHp.ToString());
					}
				}
				catch (Exception ex)
				{
					ImGui.TextColored(ImGuiColors.DalamudRed, Loc.T("Error: ") + ex.Message);
				}
			}
		}
	}

	private static IAction? _activeAction;

	private static readonly CollapsingHeaderGroup _actionsList = new([]);

	static bool IsMovingSpecialType(SpecialActionType specialType)
	{
		return specialType == SpecialActionType.FixedDistanceMoveForward
			|| specialType == SpecialActionType.FixedDistanceMoveBackward
			|| specialType == SpecialActionType.HostileMovingForward
			|| specialType == SpecialActionType.FriendlyMovingForward
			|| specialType == SpecialActionType.HostileFriendlyMovingForward
			|| specialType == SpecialActionType.HostileMovingAttack
			|| specialType == SpecialActionType.ObjectBasedMovement;
	}

	enum MovementSafetyStatus
	{
		Safe,
		NotSafe,
		NotApplicable
	}

	struct MovementSafetyResult
	{
		public MovementSafetyStatus Status;
		public string Reason;
	}

	static MovementSafetyResult GetMovementSafetyStatus(IBaseAction action)
	{
		if (Player.Object == null)
		{
			return new MovementSafetyResult { Status = MovementSafetyStatus.NotApplicable, Reason = "Player not available" };
		}

		var playerPos = Player.Object.Position;
		var specialType = action.Setting.SpecialType;

		try
		{
			switch (specialType)
			{
				case SpecialActionType.FixedDistanceMoveForward:
					{
						var range = action.TargetInfo.Range;
						var faceVector = Player.Object.GetFaceVector();
						var destination = playerPos + (Vector3.Normalize(faceVector) * range);
						var isSafe = DataCenter.IsFixedDashSafe(playerPos, destination);
						return new MovementSafetyResult
						{
							Status = isSafe ? MovementSafetyStatus.Safe : MovementSafetyStatus.NotSafe,
							Reason = isSafe ? string.Empty : "Destination unsafe (IsFixedDashSafe)"
						};
					}

				case SpecialActionType.FixedDistanceMoveBackward:
					{
						var range = action.TargetInfo.Range;
						var faceVector = Player.Object.GetFaceVector();
						var destination = playerPos - (Vector3.Normalize(faceVector) * range);
						var isSafe = DataCenter.IsFixedDashSafe(playerPos, destination);
						return new MovementSafetyResult
						{
							Status = isSafe ? MovementSafetyStatus.Safe : MovementSafetyStatus.NotSafe,
							Reason = isSafe ? string.Empty : "Destination unsafe (IsFixedDashSafe)"
						};
					}

				case SpecialActionType.HostileMovingForward:
				case SpecialActionType.FriendlyMovingForward:
				case SpecialActionType.HostileFriendlyMovingForward:
				case SpecialActionType.HostileMovingAttack:
					{
						var target = action.Target.Target;
						if (target == null)
						{
							return new MovementSafetyResult { Status = MovementSafetyStatus.NotApplicable, Reason = "No target" };
						}

						var directionToTarget = target.Position - playerPos;
						var distanceToTarget = directionToTarget.Length();

						if (distanceToTarget < 0.001f)
						{
							return new MovementSafetyResult { Status = MovementSafetyStatus.Safe, Reason = "Already at target" };
						}

						var normalizedDirection = directionToTarget / distanceToTarget;
						var distanceToHitbox = Math.Max(0, distanceToTarget - target.HitboxRadius);
						var destination = playerPos + (normalizedDirection * distanceToHitbox);

						var isSafe = DataCenter.IsDashSafe(playerPos, destination);
						return new MovementSafetyResult
						{
							Status = isSafe ? MovementSafetyStatus.Safe : MovementSafetyStatus.NotSafe,
							Reason = isSafe ? string.Empty : "Path to target unsafe (IsDashSafe)"
						};
					}

				case SpecialActionType.ObjectBasedMovement:
					{
						if (action.Setting.ObjectBasedMovementObjectOID == 0)
						{
							return new MovementSafetyResult { Status = MovementSafetyStatus.NotApplicable, Reason = "No object OID configured" };
						}

						Vector3? objectPosition = null;
						var playerId = Player.Object.GameObjectId;
						foreach (var obj in Svc.Objects)
						{
							if (obj != null && obj.BaseId == action.Setting.ObjectBasedMovementObjectOID && obj.OwnerId == playerId)
							{
								objectPosition = obj.Position;
								break;
							}
						}

						if (!objectPosition.HasValue)
						{
							return new MovementSafetyResult { Status = MovementSafetyStatus.NotApplicable, Reason = "Object not found" };
						}

						var isSafe = DataCenter.IsDashSafe(playerPos, objectPosition.Value);
						return new MovementSafetyResult
						{
							Status = isSafe ? MovementSafetyStatus.Safe : MovementSafetyStatus.NotSafe,
							Reason = isSafe ? string.Empty : "Object position unsafe (IsDashSafe)"
						};
					}

				default:
					return new MovementSafetyResult { Status = MovementSafetyStatus.NotSafe, Reason = "Unknown movement type" };
			}
		}
		catch (Exception ex)
		{
			return new MovementSafetyResult { Status = MovementSafetyStatus.NotSafe, Reason = Loc.F($"Error: {ex.Message}") };
		}
	}
}
