// Copyright (c) 2024 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using PhantomBrigade;
using PhantomBrigade.Data;

using UnityEngine;

namespace EchKode.PBMods.ShowEscalationValue
{
	static class OverworldUtility
	{
		internal static (ProvinceStatus, int, int, int) GetWarValues(
			PersistentEntity sitePersistent,
			OverworldEntity siteOverworld,
			DataContainerOverworldEntityBlueprint siteBlueprint)
		{
			var blueprintEscalation = siteBlueprint.escalationProcessed;
			if (blueprintEscalation == null)
			{
				if (ModLink.Settings.logDiagnostics)
				{
					Debug.LogFormat(
						"Mod {0} ({1}) no escalation for site | site: {2}",
						ModLink.modIndex,
						ModLink.modID,
						siteOverworld.ToLog());
				}
				return (ProvinceStatus.Unknown, 0, 0, 0);
			}

			var faction = sitePersistent.hasFaction ? sitePersistent.faction.s : "";
			var friendly = CombatUIUtility.IsFactionFriendly(faction);
			if (friendly)
			{
				if (ModLink.Settings.logDiagnostics)
				{
					Debug.LogFormat(
						"Mod {0} ({1}) site is friendly | site: {2}",
						ModLink.modIndex,
						ModLink.modID,
						siteOverworld.ToLog());
				}
				return (ProvinceStatus.Unknown, 0, 0, 0);
			}

			var province = IDUtility.GetPersistentEntity(DataHelperProvince.GetProvinceKeyAtEntity(siteOverworld));
			if (province == null)
			{
				if (ModLink.Settings.logDiagnostics)
				{
					Debug.LogFormat(
						"Mod {0} ({1}) unable to get province key | site: {2}",
						ModLink.modIndex,
						ModLink.modID,
						siteOverworld.ToLog());
				}
				return (ProvinceStatus.Unknown, 0, 0, 0);
			}
			if (province.faction.s == Factions.player)
			{
				if (ModLink.Settings.logDiagnostics)
				{
					Debug.LogFormat(
						"Mod {0} ({1}) province is liberated | site: {2}",
						ModLink.modIndex,
						ModLink.modID,
						siteOverworld.ToLog());
				}
				return (ProvinceStatus.Liberated, 0, 0, 0);
			}

			var provinceOverworld = IDUtility.GetLinkedOverworldEntity(province);
			if (provinceOverworld == null)
			{
				if (ModLink.Settings.logDiagnostics)
				{
					Debug.LogFormat(
						"Mod {0} ({1}) unable to get linked overworld entity | province: {2}",
						ModLink.modIndex,
						ModLink.modID,
						province.ToLog());
				}
				return (ProvinceStatus.Unknown, 0, 0, 0);
			}

			var provinceBlueprint = provinceOverworld.hasDataLinkOverworldProvince
				? provinceOverworld.dataLinkOverworldProvince.data
				: null;
			if (provinceBlueprint == null)
			{
				if (ModLink.Settings.logDiagnostics)
				{
					Debug.LogFormat(
						"Mod {0} ({1}) province blueprint is null | province: {2}",
						ModLink.modIndex,
						ModLink.modID,
						provinceOverworld.ToLog());
				}
				return (ProvinceStatus.Unknown, 0, 0, 0);
			}

			var provinceStatus = ProvinceStatus.Occupied;
			var escalationValue = 0f;
			if (provinceBlueprint.escalationDisabled)
			{
				if (ModLink.Settings.logDiagnostics)
				{
					Debug.LogFormat(
						"Mod {0} ({1}) escalation is disabled in province | province: {2}",
						ModLink.modIndex,
						ModLink.modID,
						provinceOverworld.ToLog());
				}
			}
			else
			{
				escalationValue = blueprintEscalation.escalationGain;
			}

			var homeGuardValue = 0f;
			var enemyValue = 0f;
			var overworldContext = Contexts.sharedInstance.overworld;
			var atWar = overworldContext.hasProvinceAtWar && overworldContext.provinceAtWar.persistentID == province.id.id;
			if (atWar)
			{
				provinceStatus = ProvinceStatus.AtWar;
				escalationValue *= blueprintEscalation.escalationGainWarMultiplier;
				homeGuardValue *= blueprintEscalation.warScoreRestored;
				enemyValue = blueprintEscalation.warScoreDealt *
					(siteOverworld.isWarObjective
						? DataShortcuts.escalation.enemyWarScoreDamageFromObjectives
						: DataShortcuts.escalation.enemyWarScoreDamageFromOthers);
			}

			return (
				provinceStatus,
				Mathf.RoundToInt(escalationValue),
				Mathf.RoundToInt(enemyValue),
				Mathf.RoundToInt(homeGuardValue));
		}

		internal enum ProvinceStatus
		{
			Unknown,
			Occupied,
			AtWar,
			Liberated,
		}
	}
}
