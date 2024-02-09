// Copyright (c) 2024 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

using HarmonyLib;

using PhantomBrigade;
using PhantomBrigade.Data;

using UnityEngine;

namespace EchKode.PBMods.ShowEscalationValue
{
	[HarmonyPatch]
	static partial class Patch
	{
		[HarmonyPatch(typeof(CIViewOverworldOverlays), nameof(CIViewOverworldOverlays.OnEntityChange))]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Civoo_OnEntityChangeTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			// Replace statements assigning text values for the threat rating with a call to a routine to include
			// the escalation value in the resulting text.

			var cm = new CodeMatcher(instructions, generator);
			var getHasThreatRatingEscalatedMethodInfo = AccessTools.DeclaredPropertyGetter(typeof(PersistentEntity), nameof(PersistentEntity.hasThreatRatingEscalated));
			var roundToIntMethodInfo = AccessTools.DeclaredMethod(typeof(Mathf), nameof(Mathf.RoundToInt));
			var getHasThreatRatingEscalatedMatch = new CodeMatch(OpCodes.Callvirt, getHasThreatRatingEscalatedMethodInfo);
			var roundToIntMatch = new CodeMatch(OpCodes.Call, roundToIntMethodInfo);
			var branchMatch = new CodeMatch(OpCodes.Br);
			var loadPersistentEntity = new CodeMatch(OpCodes.Ldloc_0);
			var loadOverworldEntity = new CodeInstruction(OpCodes.Ldarg_1);
			var populateText = CodeInstruction.Call(typeof(Patch), nameof(PopulateOverlayText));

			cm.MatchEndForward(getHasThreatRatingEscalatedMatch)
				.MatchEndForward(roundToIntMatch)
				.Advance(1);
			var loadThreatRating = new CodeInstruction(OpCodes.Ldloc_S, cm.Operand);

			cm.Advance(2)
				.SetInstructionAndAdvance(loadPersistentEntity)
				.InsertAndAdvance(loadOverworldEntity)
				.Advance(1)
				.InsertAndAdvance(loadThreatRating)
				.InsertAndAdvance(populateText);
			var deleteStart = cm.Pos;

			cm.MatchStartForward(branchMatch)
				.Advance(-1);
			var offset = deleteStart - cm.Pos;
			cm.RemoveInstructionsWithOffsets(offset, 0);

			return cm.InstructionEnumeration();
		}

		[HarmonyPatch(typeof(CIViewOverworldSelectionInfo), "Start")]
		[HarmonyPostfix]
		static void Civosi_StartPostfix(CIViewOverworldSelectionInfo __instance)
		{
			var go = Object.Instantiate(__instance.labelDescription, __instance.holderOffset.transform);
			labelVictoryValue = go.GetComponent<UILabel>();
			labelVictoryValue.enabled = true;
			labelVictoryValue.text = "";
			labelVictoryValue.bottomAnchor.Set(__instance.labelDescription.transform, 1f, labelOffset);
			holderTop = __instance.holderTopMain.transform.parent.gameObject.GetComponent<UIWidget>();
			holderTop.SetAnchor(labelVictoryValue.transform);

			if (ModLink.Settings.logDiagnostics)
			{
				Debug.LogFormat(
					"Mod {0} ({1}) victory value label instantiated | name: {2}",
					ModLink.modIndex,
					ModLink.modID,
					UtilityTransform.GetTransformPath(labelVictoryValue.transform));
			}

			// Lift the buttons above the faction branch name label.
			var depth = __instance.labelGarrisonBranch.depth + 1;
			new Traverse(__instance.buttonDevInfo).Property<UIWidget>("widget").Value.depth = depth;
			new Traverse(__instance.buttonDevInfoMemory).Property<UIWidget>("widget").Value.depth = depth;
			new Traverse(__instance.buttonDevInfoReward).Property<UIWidget>("widget").Value.depth = depth;
		}

		[HarmonyPatch(typeof(CIViewOverworldSelectionInfo), nameof(CIViewOverworldSelectionInfo.OnEntityRefresh))]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Civosi_OnEntityRefreshTranspiler2(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			// Replace formatting code with my routine. This puts the formatted string into a separate
			// StringBuilder than the one being used for the rest of the description.

			if (Harmony.DEBUG) { FileLog.Log("OnEntityRefreshTranspiler2"); }
			var cm = new CodeMatcher(instructions, generator);
			var escalationFieldInfo = AccessTools.DeclaredField(typeof(DataContainerOverworldEntityBlueprint), nameof(DataContainerOverworldEntityBlueprint.escalationProcessed));
			var toStringMethodInfo = AccessTools.Method(typeof(object), nameof(ToString));
			var clearMethodInfo = AccessTools.DeclaredMethod(typeof(StringBuilder), nameof(StringBuilder.Clear));
			var escalationMatch = new CodeMatch(OpCodes.Ldfld, escalationFieldInfo);
			var clearMatch = new CodeMatch(OpCodes.Callvirt, clearMethodInfo);
			var branchMatch = new CodeMatch(OpCodes.Brtrue);
			var loadDashMatch = new CodeMatch(OpCodes.Ldstr, "[-]");
			var loadSitePersistent = new CodeInstruction(OpCodes.Ldloc_0);
			var loadSiteOverworld = new CodeInstruction(OpCodes.Ldloc_1);
			var format = CodeInstruction.Call(typeof(Patch), nameof(FormatEscalationAndWarScore));
			var loadStringBuilder = CodeInstruction.LoadField(typeof(Patch), nameof(sb));
			var clear = new CodeInstruction(OpCodes.Callvirt, clearMethodInfo);
			var pop = new CodeInstruction(OpCodes.Pop);

			cm.Start()
				.MatchStartForward(escalationMatch)
				.MatchStartBackwards(clearMatch)
				.MatchStartBackwards(branchMatch)
				.Advance(-1)
				.InsertAndAdvance(loadStringBuilder)
				.InsertAndAdvance(clear)
				.InsertAndAdvance(pop)
				.MatchStartForward(escalationMatch)
				.Advance(-1);
			var loadBlueprint = cm.Instruction.Clone();

			cm.MatchEndForward(escalationMatch)
				.Advance(2);
			var deleteStart = cm.Pos;

			cm.MatchEndForward(loadDashMatch)
				.Advance(2);
			var offset = deleteStart - cm.Pos;
			cm.RemoveInstructionsWithOffsets(offset, 0)
				.Advance(offset)
				.InsertAndAdvance(loadSitePersistent)
				.InsertAndAdvance(loadSiteOverworld)
				.InsertAndAdvance(loadBlueprint)
				.InsertAndAdvance(format);

			return cm.InstructionEnumeration();
		}

		[HarmonyPatch(typeof(CIViewOverworldSelectionInfo), nameof(CIViewOverworldSelectionInfo.OnEntityRefresh))]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Civosi_OnEntityRefreshTranspiler3(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			// Always show dev info buttons on card in event screen.

			if (Harmony.DEBUG) { FileLog.Log("OnEntityRefreshTranspiler3"); }
			var cm = new CodeMatcher(instructions, generator);
			var isEventLocal = generator.DeclareLocal(typeof(bool));
			var onFadeMethodInfo = AccessTools.DeclaredMethod(typeof(CIViewOverworldSelectionInfo), nameof(CIViewOverworldSelectionInfo.OnFade));
			var developerModeFieldInfo = AccessTools.DeclaredField(typeof(DataContainerSettingsDebug), nameof(DataContainerSettingsDebug.developerMode));
			var onFadeMatch = new CodeMatch(OpCodes.Call, onFadeMethodInfo);
			var developerModeMatch = new CodeMatch(OpCodes.Ldfld, developerModeFieldInfo);
			var storeIsEvent = new CodeInstruction(OpCodes.Stloc_S, isEventLocal);
			var loadIsEvent = new CodeInstruction(OpCodes.Ldloc_S, isEventLocal);

			cm.MatchStartForward(onFadeMatch)
				.InsertAndAdvance(storeIsEvent)
				.InsertAndAdvance(loadIsEvent);

			cm.MatchEndForward(developerModeMatch)
				.Advance(-1);
			cm.CreateLabel(out var developerModeLabel);
			var jumpToDeveloperMode = new CodeInstruction(OpCodes.Brtrue_S, developerModeLabel);

			cm.Advance(-1)
				.InsertAndAdvance(jumpToDeveloperMode)
				.InsertAndAdvance(loadIsEvent);

			return cm.InstructionEnumeration();
		}

		[HarmonyPatch(typeof(CIViewOverworldSelectionInfo), nameof(CIViewOverworldSelectionInfo.OnEntityRefresh))]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Civosi_OnEntityRefreshTranspiler4(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			// Replace call to OnFade() with one that doesn't set location.

			if (Harmony.DEBUG) { FileLog.Log("OnEntityRefreshTranspiler4"); }
			var cm = new CodeMatcher(instructions, generator);
			var onFadeMethodInfo = AccessTools.DeclaredMethod(typeof(CIViewOverworldSelectionInfo), nameof(CIViewOverworldSelectionInfo.OnFade));
			var onFadeMatch = new CodeMatch(OpCodes.Call, onFadeMethodInfo);
			var onFadeSimple = CodeInstruction.Call(typeof(Patch), nameof(OnFadeSimple));

			cm.MatchStartForward(onFadeMatch)
				.SetInstructionAndAdvance(onFadeSimple);

			return cm.InstructionEnumeration();
		}

		[HarmonyPatch(typeof(CIViewOverworldSelectionInfo), nameof(CIViewOverworldSelectionInfo.OnEntityRefresh))]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Civosi_OnEntityRefreshTranspiler5(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			// Use my routine to finalize layout at end of method.

			if (Harmony.DEBUG) { FileLog.Log("OnEntityRefreshTranspiler5"); }
			var cm = new CodeMatcher(instructions, generator);
			var spriteBackgroundFieldInfo = AccessTools.DeclaredField(typeof(CIViewOverworldSelectionInfo), nameof(CIViewOverworldSelectionInfo.spriteBackground));
			var descriptionLabelFieldInfo = AccessTools.DeclaredField(typeof(CIViewOverworldSelectionInfo), nameof(CIViewOverworldSelectionInfo.labelDescription));
			var getGameObjectMethodInfo = AccessTools.DeclaredPropertyGetter(typeof(Component), nameof(GameObject.gameObject));
			var getActiveSelfMethodInfo = AccessTools.DeclaredPropertyGetter(typeof(GameObject), nameof(GameObject.activeSelf));
			var setActiveMethodInfo = AccessTools.DeclaredMethod(typeof(GameObject), nameof(GameObject.SetActive));
			var convR4Match = new CodeMatch(OpCodes.Conv_R4);
			var spriteBackgroundMatch = new CodeMatch(OpCodes.Ldfld, spriteBackgroundFieldInfo);
			var setActiveMatch = new CodeMatch(OpCodes.Callvirt, setActiveMethodInfo);
			var branchMatch = new CodeMatch(OpCodes.Brfalse_S);
			var loadThis = new CodeInstruction(OpCodes.Ldarg_0);
			var loadSelected = new CodeInstruction(OpCodes.Ldarg_2);
			var finalizeLayout = CodeInstruction.Call(typeof(Patch), nameof(FinalizeLayout));

			cm.End()
				.MatchStartBackwards(convR4Match)
				.Advance(-1);
			var loadOffset2 = cm.Instruction.Clone();

			cm.MatchStartBackwards(convR4Match)
				.Advance(-1);
			var loadOffset1 = cm.Instruction.Clone();

			cm.MatchStartBackwards(spriteBackgroundMatch)
				.MatchEndBackwards(setActiveMatch)
				.MatchStartForward(branchMatch)
				.Advance(-1);
			var labels = new List<Label>(cm.Labels);
			cm.Labels.Clear();
			var loadFlag = cm.Instruction.Clone();
			var deleteStart = cm.Pos;

			cm.Advance(2);
			var loadAbsolute = cm.Instruction.Clone();

			cm.End().Advance(-1);
			var offset = deleteStart - cm.Pos;

			cm.RemoveInstructionsWithOffsets(offset, 0)
				.Advance(offset);

			cm.End()
				.Insert(loadThis)
				.AddLabels(labels)
				.Advance(1)
				.InsertAndAdvance(loadSelected)
				.InsertAndAdvance(loadFlag)
				.InsertAndAdvance(loadAbsolute)
				.InsertAndAdvance(loadOffset1)
				.InsertAndAdvance(loadOffset2)
				.InsertAndAdvance(finalizeLayout);

			return cm.InstructionEnumeration();
		}

		[HarmonyPatch(typeof(CIViewOverworldSelectionInfo), nameof(CIViewOverworldSelectionInfo.SetLocation))]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Civosi_SetLocationTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			// Don't move the card down as much in the event screen if there is escalation information to show.

			if (Harmony.DEBUG) { FileLog.Log("OnSetLocation"); }
			var cm = new CodeMatcher(instructions, generator);
			var getHeightMethodInfo = AccessTools.DeclaredPropertyGetter(typeof(UIWidget), nameof(UIWidget.height));
			var getLengthMethodInfo = AccessTools.DeclaredPropertyGetter(typeof(StringBuilder), nameof(StringBuilder.Length));
			var getHeightMatch = new CodeMatch(OpCodes.Callvirt, getHeightMethodInfo);
			var loadAltLoc = new CodeInstruction(OpCodes.Ldarg_1);
			var loadStringBuilder = CodeInstruction.LoadField(typeof(Patch), nameof(sb));
			var getLength = new CodeInstruction(OpCodes.Callvirt, getLengthMethodInfo);
			var loadOffset = new CodeInstruction(OpCodes.Ldc_I4, labelOffset / 2);
			var sub = new CodeInstruction(OpCodes.Sub);

			cm.MatchEndForward(getHeightMatch)
				.Advance(1);
			cm.CreateLabel(out var jumpLabel);
			var skipAdjustment = new CodeInstruction(OpCodes.Brfalse_S, jumpLabel);

			cm.InsertAndAdvance(loadAltLoc)
				.InsertAndAdvance(skipAdjustment)
				.InsertAndAdvance(loadStringBuilder)
				.InsertAndAdvance(getLength)
				.InsertAndAdvance(skipAdjustment)
				.InsertAndAdvance(loadOffset)
				.InsertAndAdvance(sub);

			return cm.InstructionEnumeration();
		}

		[HarmonyPatch(typeof(CIViewOverworldSelectionInfo), nameof(CIViewOverworldSelectionInfo.SetLocation))]
		[HarmonyPrefix]
		static void Civosi_SetLocationPrefix2(CIViewOverworldSelectionInfo __instance, bool alternateLocationUsed)
		{
			// If we've entered an event, refresh the card if the event target wasn't the last entity
			// displayed by the card. This prevents the card from being pushed too far down because
			// the wrong description text is loaded which results in the wrong height being used for the
			// offset.

			var overworldContext = Contexts.sharedInstance.overworld;
			if (!overworldContext.hasEventInProgress)
			{
				return;
			}

			var siteOverworld = IDUtility.GetOverworldEntity(overworldContext.eventInProgress.targetOverworldID);
			if (siteOverworld == null)
			{
				return;
			}

			var sitePersistent = IDUtility.GetLinkedPersistentEntity(siteOverworld);
			if (sitePersistent == null)
			{
				return;
			}

			var lastID = new Traverse(__instance).Field<int>("entityPersistentIDDisplayedLast").Value;
			if (lastID == sitePersistent.id.id)
			{
				return;
			}

			__instance.OnEntityRefresh(
				sitePersistent.id.id,
				DataShortcuts.debug.developerMode && alternateLocationUsed);
		}

		[HarmonyPatch(typeof(CIViewOverworldSelectionInfo), nameof(CIViewOverworldSelectionInfo.SetLocation))]
		[HarmonyPostfix]
		static void Civosi_SetLocationPostfix(CIViewOverworldSelectionInfo __instance, bool alternateLocationUsed)
		{
			// Animate moving developer buttons.

			if (!DataShortcuts.debug.developerMode)
			{
				return;
			}

			holder = __instance.holderDevInfo.transform;
			shiftFrom = holder.localPosition;
			shiftTo = Vector3.zero;
			if (alternateLocationUsed)
			{
				shiftTo -= __instance.holderOffsetPosFaded;
				shiftTo += Vector3.up * __instance.labelDescription.height;
			}

			LeanTween.cancelIfTweening(__instance.holderDevInfo.gameObject);
			var ltDescr = LeanTween.value(__instance.holderDevInfo.gameObject, 0.0f, 1f, 0.5f);
			ltDescr.setEase(LeanTweenType.easeInOutSine);
			ltDescr.setOnUpdate(OnShiftAnimation);
			ltDescr.setIgnoreTimeScale(true);
		}

		public static void FormatEscalationAndWarScore(
			PersistentEntity sitePersistent,
			OverworldEntity siteOverworld,
			DataContainerOverworldEntityBlueprint siteBlueprint)
		{
			var (provinceStatus, escalationGain, warScoreDealt, warScoreRestored) = OverworldUtility.GetWarValues(
				sitePersistent,
				siteOverworld,
				siteBlueprint);
			if (provinceStatus != OverworldUtility.ProvinceStatus.AtWar
				&& provinceStatus != OverworldUtility.ProvinceStatus.Occupied)
			{
				return;
			}
			if (escalationGain == 0f && warScoreRestored == 0f && warScoreDealt == 0f)
			{
				return;
			}

			sb.AppendFormat("[7ebeff]{0}: ", Txt.Get(TextLibs.uiCombat, "end_header_victory_total"));
			if (escalationGain != 0f)
			{
				sb.AppendFormat(
					"\n+[b]{0:F0}[/b][aa] — {1}[ff]",
					escalationGain,
					Txt.Get(TextLibs.uiDifficultySettings, "overworld_rate_escalation_increase__header"));
			}
			if (warScoreRestored != 0f)
			{
				sb.AppendFormat(
					"\n+[b]{0:F0}[/b][aa] — {1}[ff]",
					warScoreRestored,
					Txt.Get(TextLibs.uiOverworld , "province_state_war_score_player_header"));
			}
			if (warScoreDealt != 0f)
			{
				sb.AppendFormat(
					"\n−[b]{0:F0}[/b][aa] — {1}[ff]",
					warScoreDealt,
					Txt.Get(TextLibs.uiOverworld, "province_state_war_score_enemy_header"));
			}
			sb.Append("[-]");
		}

		public static void FinalizeLayout(
			CIViewOverworldSelectionInfo view,
			bool selected,
			bool knownAndRecognized,
			int absolute,
			int offset1,
			int offset2)
		{
			if (knownAndRecognized)
			{
				var height = view.textureLocationImage.height;
				absolute += height;
				offset1 += height;
			}

			var hasEscalation = knownAndRecognized && sb.Length != 0;
			if (hasEscalation)
			{
				labelVictoryValue.text = sb.ToString();
				labelVictoryValue.bottomAnchor.Set(view.labelDescription.transform, 1f, labelOffset);

				absolute += labelVictoryValue.height + labelOffset;
				holderTop.SetAnchor(labelVictoryValue.transform);
			}
			else
			{
				labelVictoryValue.text = "";
				holderTop.SetAnchor(view.labelDescription.transform);
			}

			view.spriteBackground.topAnchor.Set(labelVictoryValue.transform, 1f, absolute);
			view.spriteEventFade.topAnchor.Set(labelVictoryValue.transform, 1f, absolute + 8);
			view.holderTopContext.transform.localPosition = Vector3.up * offset1;
			view.holderTopMain.transform.localPosition = Vector3.up * offset2;
			view.holderSelectedOptions.gameObject.SetActive(selected);
		}

		public static void OnFadeSimple(CIViewOverworldSelectionInfo view, bool faded)
		{
			view.holderEventFade.SetActive(faded);
			view.holderTopContext.gameObject.SetActive(!faded);
		}

		public static void PopulateOverlayText(
			CIHelperOverlayOverworldEntity overlay,
			PersistentEntity sitePersistent,
			OverworldEntity siteOverworld,
			DataContainerOverworldEntityBlueprint siteBlueprint,
			int threatRating)
		{
			var (provinceStatus, escalationValue, enemyValue, _) = OverworldUtility.GetWarValues(sitePersistent, siteOverworld, siteBlueprint);
			if (provinceStatus == OverworldUtility.ProvinceStatus.AtWar)
			{
				overlay.textFullLast = string.Format("{0}\n[ffaaaa]{1}[ffffff]/[ffaaaa][aa]{2}[ff][ffffff]/[ffaaaa]{3}", siteBlueprint.textNameProcessed?.s, threatRating, escalationValue, enemyValue);
				overlay.textShortLast = string.Format("{0}/[aa]{1}[ff]/{2}", threatRating, escalationValue, enemyValue);
				return;
			}
			if (provinceStatus == OverworldUtility.ProvinceStatus.Occupied && escalationValue != 0)
			{
				overlay.textFullLast = string.Format("{0}\n[ffaaaa]{1}[ffffff]/[ffaaaa]{2}", siteBlueprint.textNameProcessed?.s, threatRating, escalationValue);
				overlay.textShortLast = string.Format("{0}/{1}", threatRating, escalationValue);
				return;

			}
			overlay.textFullLast = string.Format("{0}\n[ffaaaa]{1}", siteBlueprint.textNameProcessed?.s, threatRating);
			overlay.textShortLast = threatRating.ToString();
		}

		static void OnShiftAnimation(float time) => holder.localPosition = Vector3.Lerp(shiftFrom, shiftTo, time);

		const int labelOffset = 16;
		static UILabel labelVictoryValue;
		static UIWidget holderTop;

		static Transform holder;
		static Vector3 shiftFrom;
		static Vector3 shiftTo;

		static readonly StringBuilder sb = new StringBuilder();

		const string escalationTextKey = "selection_escalation_value_prefix";
		const string warScoreTextKey = "selection_war_score_prefix";
	}
}
