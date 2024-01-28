// Copyright (c) 2024 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;
using System.Reflection.Emit;

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

			cm.MatchStartForward(getHasThreatRatingEscalatedMatch)
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
			var go = Object.Instantiate(__instance.labelGarrisonBranch, __instance.holderTopMain.transform);
			labelVictoryValue = go.GetComponent<UILabel>();
			labelVictoryValue.text = "";

			// Lift the buttons above the faction branch name label.
			var depth = __instance.labelGarrisonBranch.depth + 1;
			new Traverse(__instance.buttonDevInfo).Property<UIWidget>("widget").Value.depth = depth;
			new Traverse(__instance.buttonDevInfoMemory).Property<UIWidget>("widget").Value.depth = depth;
			new Traverse(__instance.buttonDevInfoReward).Property<UIWidget>("widget").Value.depth = depth;
		}

		[HarmonyPatch(typeof(CIViewOverworldSelectionInfo), nameof(CIViewOverworldSelectionInfo.OnEntityRefresh))]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Civosi_OnEntityRefreshTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			// Always show dev info buttons on card in event screen.

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
		static IEnumerable<CodeInstruction> Civosi_OnEntityRefreshTranspiler2(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			// Replace call to OnFade() with one that doesn't set location.

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
		static IEnumerable<CodeInstruction> Civosi_OnEntityRefreshTranspiler3(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			// Call routine to format the text for escalation value and show it.

			var cm = new CodeMatcher(instructions, generator);
			var loadThis = new CodeInstruction(OpCodes.Ldarg_0);
			var loadPersistentEntity = new CodeInstruction(OpCodes.Ldloc_0);
			var loadOverworldEntity = new CodeInstruction(OpCodes.Ldloc_1);
			var displayText = CodeInstruction.Call(typeof(Patch), nameof(DisplaySelectionText));

			cm.End()
				.InsertAndAdvance(loadThis)
				.InsertAndAdvance(loadPersistentEntity)
				.InsertAndAdvance(loadOverworldEntity)
				.InsertAndAdvance(displayText);

			return cm.InstructionEnumeration();
		}

		[HarmonyPatch(typeof(CIViewOverworldSelectionInfo), nameof(CIViewOverworldSelectionInfo.SetLocation))]
		[HarmonyPrefix]
		static void Civosi_SetLocationPrefix(CIViewOverworldSelectionInfo __instance, bool alternateLocationUsed)
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
			var (provinceStatus, escalationValue, warValue) = OverworldUtility.GetWarValues(sitePersistent, siteOverworld, siteBlueprint);
			if (provinceStatus == OverworldUtility.ProvinceStatus.AtWar)
			{
				overlay.textFullLast = string.Format("{0}\n[ffaaaa]{1}[ffffff]/[ffaaaa][aa]{2}[ff][ffffff]/[ffaaaa]{3}", siteBlueprint.textNameProcessed?.s, threatRating, escalationValue, warValue);
				overlay.textShortLast = string.Format("{0}/[aa]{1}[ff]/{2}", threatRating, escalationValue, warValue);
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

		public static void DisplaySelectionText(
			CIViewOverworldSelectionInfo view,
			PersistentEntity sitePersistent,
			OverworldEntity siteOverworld)
		{
			var siteBlueprint = siteOverworld.hasDataLinkOverworldEntityBlueprint
				? siteOverworld.dataLinkOverworldEntityBlueprint.data
				: null;
			if (siteBlueprint == null)
			{
				labelVictoryValue.text = "";
				labelVictoryValue.gameObject.SetActive(false);
				return;
			}

			var (provinceStatus, escalationValue, warValue) = OverworldUtility.GetWarValues(sitePersistent, siteOverworld, siteBlueprint);
			var atWar = provinceStatus == OverworldUtility.ProvinceStatus.AtWar;
			var occupied = provinceStatus == OverworldUtility.ProvinceStatus.Occupied;
			if (!(atWar || occupied)
				|| (occupied && escalationValue == 0)
				|| (atWar && warValue == 0))
			{
				labelVictoryValue.text = "";
				labelVictoryValue.gameObject.SetActive(false);
				return;
			}

			var textKey = atWar ? warScoreTextKey : escalationTextKey;
			var displayValue = atWar ? warValue : escalationValue;
			labelVictoryValue.text = string.Format("[aa]{0} [ff]{1}", Txt.Get(TextLibs.uiOverworld, textKey), displayValue);
			var imageLeftTop = GetLeftTopCorner(view.textureLocationImage);
			var labelLeftBottom = GetLeftBottomCorner(labelVictoryValue);
			if (imageLeftTop.y > labelLeftBottom.y)
			{
				var offset = imageLeftTop.y - labelLeftBottom.y;
				var y = labelVictoryValue.transform.localPosition.y;
				labelVictoryValue.transform.SetPositionLocalY(y + offset);
			}
			labelVictoryValue.gameObject.SetActive(true);
		}

		static void OnShiftAnimation(float time) => holder.localPosition = Vector3.Lerp(shiftFrom, shiftTo, time);

		static Vector2Int GetLeftBottomCorner(UIWidget widget)
		{
			var root = widget.root;
			var corners = widget.worldCorners;
			var leftBottom = root.transform.InverseTransformPoint(corners[0]);
			return Vector2Int.RoundToInt(leftBottom);
		}

		static Vector2Int GetLeftTopCorner(UIWidget widget)
		{
			var root = widget.root;
			var corners = widget.worldCorners;
			var leftTop = root.transform.InverseTransformPoint(corners[1]);
			return Vector2Int.RoundToInt(leftTop);
		}

		static UILabel labelVictoryValue;
		static Transform holder;
		static Vector3 shiftFrom;
		static Vector3 shiftTo;

		const string escalationTextKey = "selection_escalation_value_prefix";
		const string warScoreTextKey = "selection_war_score_prefix";
	}
}
