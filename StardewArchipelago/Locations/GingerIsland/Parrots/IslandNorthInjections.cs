﻿using System;
using Microsoft.Xna.Framework;
using StardewArchipelago.Archipelago;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Locations;

namespace StardewArchipelago.Locations.GingerIsland.Parrots
{
    public class IslandNorthInjections : IParrotReplacer
    {
        private const string AP_BRIDGE_PARROT = "Dig Site Bridge";
        private const string AP_TRADER_PARROT = "Island Trader";

        private static IMonitor _monitor;
        private static IModHelper _modHelper;
        private static ArchipelagoClient _archipelago;
        private static LocationChecker _locationChecker;

        private IslandLocation _islandLocation;

        public static void Initialize(IMonitor monitor, IModHelper modHelper, ArchipelagoClient archipelago, LocationChecker locationChecker)
        {
            _monitor = monitor;
            _modHelper = modHelper;
            _archipelago = archipelago;
            _locationChecker = locationChecker;
        }

        public IslandNorthInjections()
        {
            _islandLocation = (IslandNorth)Game1.getLocationFromName("IslandNorth");
        }

        public void ReplaceParrots()
        {
            _islandLocation.parrotUpgradePerches.Clear();
            AddDigSiteBridgeParrot(_islandLocation);
            AddIslandTraderParrot(_islandLocation);
        }

        private static void AddDigSiteBridgeParrot(IslandLocation __instance)
        {
            var digSiteBridgeParrot = new ParrotUpgradePerch(__instance, new Point(35, 52),
                new Rectangle(31, 52, 4, 4), 10, PurchaseBridgeParrot, IsBridgeParrotPurchased,
                "Bridge", "Island_Turtle");
            __instance.parrotUpgradePerches.Add(digSiteBridgeParrot);
        }

        private static void PurchaseBridgeParrot()
        {
            _locationChecker.AddCheckedLocation(AP_BRIDGE_PARROT);
        }

        private static bool IsBridgeParrotPurchased()
        {
            return _locationChecker.IsLocationChecked(AP_BRIDGE_PARROT);
        }

        private static void AddIslandTraderParrot(IslandLocation __instance)
        {
            var islandTraderParrot = new ParrotUpgradePerch(__instance, new Point(32, 72),
                new Rectangle(33, 68, 5, 5), 10, PurchaseTraderParrot, IsTraderParrotPurchased,
                "Trader", "Island_UpgradeHouse");
            __instance.parrotUpgradePerches.Add(islandTraderParrot);
        }

        private static void PurchaseTraderParrot()
        {
            _locationChecker.AddCheckedLocation(AP_TRADER_PARROT);
        }

        private static bool IsTraderParrotPurchased()
        {
            return _locationChecker.IsLocationChecked(AP_TRADER_PARROT);
        }
    }
}