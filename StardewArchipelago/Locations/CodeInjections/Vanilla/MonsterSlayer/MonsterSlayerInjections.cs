﻿using System;
using System.Linq;
using StardewArchipelago.Archipelago;
using StardewArchipelago.Goals;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;

namespace StardewArchipelago.Locations.CodeInjections.Vanilla.MonsterSlayer
{
    public static class MonsterSlayerInjections
    {
        private const string MONSTER_ERADICATION_AP_PREFIX = "Monster Eradication: ";

        private static IMonitor _monitor;
        private static IModHelper _modHelper;
        private static ArchipelagoClient _archipelago;
        private static LocationChecker _locationChecker;
        private static MonsterKillList _killList;

        public static void Initialize(IMonitor monitor, IModHelper modHelper, ArchipelagoClient archipelago, LocationChecker locationChecker)
        {
            _monitor = monitor;
            _modHelper = modHelper;
            _archipelago = archipelago;
            _locationChecker = locationChecker;
            _killList = new MonsterKillList(_archipelago);
        }

        // public void showMonsterKillList()
        public static bool ShowMonsterKillList_CustomListFromAP_Prefix(AdventureGuild __instance, ref bool __result)
        {
            try
            {
                if (!Game1.player.mailReceived.Contains("checkedMonsterBoard"))
                {
                    Game1.player.mailReceived.Add("checkedMonsterBoard");
                }

                var killListContent = _killList.GetKillListLetterContent();
                Game1.drawLetterMessage(killListContent);

                return false; // don't run original logic
            }
            catch (Exception ex)
            {
                _monitor.Log($"Failed in {nameof(ShowMonsterKillList_CustomListFromAP_Prefix)}:\n{ex}",
                    LogLevel.Error);
                return true; // run original logic
            }
        }

        // public void monsterKilled(string name)
        public static void MonsterKilled_SendMonstersanityCheck_Postfix(Stats __instance, string name)
        {
            try
            {
                var category = GetCategory(name);
                switch (_archipelago.SlotData.Monstersanity)
                {
                    case Monstersanity.None:
                        return;
                    case Monstersanity.OnePerCategory:
                        CheckLocation(category);
                        return;
                    case Monstersanity.OnePerMonster:
                        CheckLocation(name);
                        return;
                    case Monstersanity.Goals:
                    case Monstersanity.ShortGoals:
                    case Monstersanity.VeryShortGoals:
                        CheckLocationIfEnoughMonstersInCategory(category);
                        return;
                    case Monstersanity.ProgressiveGoals:
                        CheckLocationIfEnoughMonstersInProgressiveCategory(category);
                        return;
                    case Monstersanity.SplitGoals:
                        CheckLocationIfEnoughMonsters(name);
                        return;
                    default:
                        return;
                }

                return;
            }
            catch (Exception ex)
            {
                _monitor.Log($"Failed in {nameof(MonsterKilled_SendMonstersanityCheck_Postfix)}:\n{ex}",
                    LogLevel.Error);
                return;
            }
        }

        // public void monsterKilled(string name)
        public static void MonsterKilled_CheckGoalCompletion_Postfix(Stats __instance, string name)
        {
            try
            {
                GoalCodeInjection.CheckProtectorOfTheValleyGoalCompletion();
                return;
            }
            catch (Exception ex)
            {
                _monitor.Log($"Failed in {nameof(MonsterKilled_CheckGoalCompletion_Postfix)}:\n{ex}",
                    LogLevel.Error);
                return;
            }
        }



        private static void CheckLocationIfEnoughMonstersInCategory(string category)
        {
            var amountNeeded = _killList.MonsterGoals[category];
            if (_killList.GetMonstersKilledInCategory(category) >= amountNeeded)
            {
                CheckLocation(category);
            }
        }

        private static void CheckLocationIfEnoughMonsters(string monster)
        {
            var amountNeeded = _killList.MonsterGoals[monster];
            if (_killList.GetMonstersKilled(monster) >= amountNeeded)
            {
                CheckLocation(monster);
            }
        }

        private static void CheckLocationIfEnoughMonstersInProgressiveCategory(string category)
        {
            var lastAmountNeeded = _killList.MonsterGoals[category];
            var progressiveStep = lastAmountNeeded / 5;
            var monstersKilled = _killList.GetMonstersKilledInCategory(category);
            for (var i = progressiveStep; i <= lastAmountNeeded; i += progressiveStep)
            {
                if (monstersKilled < i)
                {
                    return;
                }

                var progressiveCategoryName = (i == lastAmountNeeded) ? category : $"{i} {category}";
                CheckLocation(progressiveCategoryName);
            }
        }

        private static string GetCategory(string name)
        {
            foreach (var (category, monsters) in _killList.MonstersByCategory)
            {
                if (monsters.Contains(name))
                {
                    return category;
                }
            }

            _monitor.Log($"Could not find a monster slayer category for monster {name}");
            return "";
        }

        private static void CheckLocation(string goalName)
        {
            if (string.IsNullOrEmpty(goalName))
            {
                return;
            }

            var apLocation = $"{MONSTER_ERADICATION_AP_PREFIX}{goalName}";
            if (_archipelago.GetLocationId(apLocation) > -1)
            {
                _locationChecker.AddCheckedLocation(apLocation);
            }
            else
            {
                _monitor.Log($"Tried to check a monster slayer goal, but it doesn't exist! [{apLocation}]", LogLevel.Error);
            }
        }
    }
}
