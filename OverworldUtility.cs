// Copyright (c) 2024 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using PhantomBrigade;
using PhantomBrigade.Data;

using UnityEngine;

namespace EchKode.PBMods.ShowEscalationValue
{
	static class OverworldUtility
	{
		internal static (ProvinceStatus, int, int) GetWarValues(
			PersistentEntity sitePersistent,
			OverworldEntity siteOverworld,
			DataContainerOverworldEntityBlueprint siteBlueprint)
		{
			var blueprintEscalation = siteBlueprint.escalationProcessed;
			if (blueprintEscalation == null)
			{
				return (ProvinceStatus.Unknown, 0, 0);
			}

			var faction = sitePersistent.hasFaction ? sitePersistent.faction.s : "";
			var friendly = CombatUIUtility.IsFactionFriendly(faction);
			if (friendly)
			{
				return (ProvinceStatus.Unknown, 0, 0);
			}

			var province = IDUtility.GetPersistentEntity(DataHelperProvince.GetProvinceKeyAtEntity(siteOverworld));
			if (province == null)
			{
				return (ProvinceStatus.Unknown, 0, 0);
			}
			if (province.faction.s == Factions.player)
			{
				return (ProvinceStatus.Liberated, 0, 0);
			}

			var provinceOverworld = IDUtility.GetLinkedOverworldEntity(province);
			if (provinceOverworld == null)
			{
				return (ProvinceStatus.Unknown, 0, 0);
			}

			var provinceBlueprint = provinceOverworld.hasDataLinkOverworldProvince
				? provinceOverworld.dataLinkOverworldProvince.data
				: null;
			if (provinceBlueprint == null)
			{
				return (ProvinceStatus.Unknown, 0, 0);
			}
			if (provinceBlueprint.escalationDisabled)
			{
				return (ProvinceStatus.Unknown, 0, 0);
			}

			var escalationValue = blueprintEscalation.escalationGain;
			var warValue = 0f;
			var overworldContext = Contexts.sharedInstance.overworld;
			var atWar = overworldContext.hasProvinceAtWar && overworldContext.provinceAtWar.persistentID == province.id.id;
			if (atWar)
			{
				escalationValue *= blueprintEscalation.escalationGainWarMultiplier;
				warValue = blueprintEscalation.warScoreDealt *
					(siteOverworld.isWarObjective
						? DataShortcuts.escalation.enemyWarScoreDamageFromObjectives
						: DataShortcuts.escalation.enemyWarScoreDamageFromOthers);
			}

			return (
				atWar ? ProvinceStatus.AtWar : ProvinceStatus.Occupied,
				Mathf.RoundToInt(escalationValue),
				Mathf.RoundToInt(warValue));
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
