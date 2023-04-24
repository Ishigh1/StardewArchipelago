﻿using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewArchipelago.Archipelago;
using StardewArchipelago.GameModifications;
using StardewArchipelago.GameModifications.CodeInjections;
using StardewArchipelago.GameModifications.EntranceRandomizer;
using StardewArchipelago.Goals;
using StardewArchipelago.Items;
using StardewArchipelago.Items.Mail;
using StardewArchipelago.Locations;
using StardewArchipelago.Locations.CodeInjections;
using StardewArchipelago.Serialization;
using StardewArchipelago.Stardew;
using StardewArchipelago.Test;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;

namespace StardewArchipelago
{
    public class ModEntry : Mod
    {
        private const string CONNECT_SYNTAX = "Syntax: connect ip:port slot password";
        private const string AP_DATA_KEY = "ArchipelagoData";
        private const string AP_EXPERIENCE_KEY = "ArchipelagoSkillsExperience";

        private IModHelper _helper;
        private Harmony _harmony;
        private ArchipelagoClient _archipelago;
        private AdvancedOptionsManager _advancedOptionsManager;
        private Mailman _mail;
        private GiftHandler _giftHandler;
        private BundleReader _bundleReader;
        private ItemManager _itemManager;
        private RandomizedLogicPatcher _logicPatcher;
        private MailPatcher _mailPatcher;
        private LocationChecker _locationChecker;
        private LocationPatcher _locationsPatcher;
        private ItemPatcher _itemPatcher;
        private GoalManager _goalManager;
        private StardewItemManager _stardewItemManager;
        private UnlockManager _unlockManager;
        private MultiSleep _multiSleep;
        private JojaDisabler _jojaDisabler;

        private Tester _tester;

        private ArchipelagoStateDto _state;
        private ArchipelagoConnectionInfo _apConnectionOverride;

        public ModEntry() : base()
        {
        }

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            _apConnectionOverride = null;

            _helper = helper;
            _harmony = new Harmony(this.ModManifest.UniqueID);

            _archipelago = new ArchipelagoClient(Monitor, _helper, _harmony, OnItemReceived, ModManifest);

            _helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            _helper.Events.GameLoop.SaveCreating += this.OnSaveCreating;
            _helper.Events.GameLoop.SaveCreated += this.OnSaveCreated;
            _helper.Events.GameLoop.Saving += this.OnSaving;
            _helper.Events.GameLoop.Saved += this.OnSaved;
            _helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            _helper.Events.GameLoop.TimeChanged += this.OnTimeChanged;
            _helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            _helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            _helper.Events.GameLoop.DayEnding += this.OnDayEnding;
            _helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;


            _helper.ConsoleCommands.Add("connect_override", $"Overrides your next connection to Archipelago. {CONNECT_SYNTAX}", this.OnCommandConnectToArchipelago);

#if DEBUG
            _helper.ConsoleCommands.Add("connect", $"Connect to Archipelago. {CONNECT_SYNTAX}", this.OnCommandConnectToArchipelago);
            _helper.ConsoleCommands.Add("disconnect", $"Disconnects from Archipelago. {CONNECT_SYNTAX}", this.OnCommandDisconnectFromArchipelago);
            _helper.ConsoleCommands.Add("test_getallitems", "Tests if every AP item in the stardew_valley_item_table json file are supported by the mod", this.TestGetAllItems);
            _helper.ConsoleCommands.Add("test_getitem", "Get one specific item", this.TestGetSpecificItem);
            //_helper.ConsoleCommands.Add("test_sendalllocations", "Tests if every AP item in the stardew_valley_location_table json file are supported by the mod", _tester.TestSendAllLocations);
            // _helper.ConsoleCommands.Add("load_entrances", "Loads the entrances file", (_, _) => _entranceRandomizer.LoadTransports());
            _helper.ConsoleCommands.Add("save_entrances", "Saves the entrances file", (_, _) => EntranceInjections.SaveNewEntrancesToFile());
            _helper.ConsoleCommands.Add("debugMethod", "Runs whatever is currently in the debug method", this.DebugMethod);
#endif
        }

        private void ResetArchipelago()
        {
            _archipelago.DisconnectPermanently();
            if (_state != null)
            {
                _state.APConnectionInfo = null;
            }
            _state = new ArchipelagoStateDto();

            _harmony.UnpatchAll(ModManifest.UniqueID);
            _multiSleep = new MultiSleep(Monitor, _helper, _harmony);
            _advancedOptionsManager = new AdvancedOptionsManager(this, _harmony, _archipelago);
            _advancedOptionsManager.InjectArchipelagoAdvancedOptions();
            _giftHandler = new GiftHandler();
            SkillInjections.ResetSkillExperience();
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            ResetArchipelago();
        }

        private void OnSaveCreating(object sender, SaveCreatingEventArgs e)
        {
            _state.ItemsReceived = new List<ReceivedItem>();
            _state.LocationsChecked = new List<string>();
            _state.LocationsScouted = new Dictionary<string, ScoutedLocation>();
            _state.LettersGenerated = new Dictionary<string, string>();
            SkillInjections.ResetSkillExperience();
            _helper.Data.WriteSaveData(AP_DATA_KEY, _state);
            _helper.Data.WriteSaveData(AP_EXPERIENCE_KEY, SkillInjections.GetArchipelagoExperience());

            if (!_archipelago.IsConnected)
            {
                Monitor.Log("You are not allowed to create a new game without connecting to Archipelago", LogLevel.Error);
                Game1.ExitToTitle();
                return;
            }
        }

        private void OnSaveCreated(object sender, SaveCreatedEventArgs e)
        {
        }

        private void OnSaving(object sender, SavingEventArgs e)
        {
            _state.ItemsReceived = _itemManager.GetAllItemsAlreadyProcessed();
            _state.LocationsChecked = _locationChecker.GetAllLocationsAlreadyChecked();
            _state.LocationsScouted = _archipelago.ScoutedLocations;
            _state.LettersGenerated = _mail.GetAllLettersGenerated();
            _helper.Data.WriteSaveData(AP_DATA_KEY, _state);
            _helper.Data.WriteSaveData(AP_EXPERIENCE_KEY, SkillInjections.GetArchipelagoExperience());
        }

        private void OnSaved(object sender, SavedEventArgs e)
        {
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            ReadPersistentArchipelagoData();

            _stardewItemManager = new StardewItemManager();
            _mail = new Mailman(_state.LettersGenerated);
            _tester = new Tester(_helper, Monitor, _mail);
            _bundleReader = new BundleReader();
            _unlockManager = new UnlockManager();
            _itemManager = new ItemManager(_archipelago, _stardewItemManager, _unlockManager, _mail, _state.ItemsReceived);
            _mailPatcher = new MailPatcher(Monitor, new LetterActions(_mail), _harmony);
            _locationChecker = new LocationChecker(Monitor, _archipelago, _state.LocationsChecked);
            _locationsPatcher = new LocationPatcher(Monitor, _archipelago, _bundleReader, _helper, _harmony, _locationChecker, _stardewItemManager);
            _itemPatcher = new ItemPatcher(Monitor, _helper, _harmony, _archipelago);
            _goalManager = new GoalManager(Monitor, _helper, _harmony, _archipelago, _locationChecker);
            _logicPatcher = new RandomizedLogicPatcher(Monitor, _helper, _harmony, _archipelago, _locationChecker, _stardewItemManager);
            _jojaDisabler = new JojaDisabler(Monitor, _helper, _harmony);

            if (_state.APConnectionInfo == null)
            {
                return;
            }

            if (!_archipelago.IsConnected)
            {
                if (_apConnectionOverride != null)
                {
                    _state.APConnectionInfo = _apConnectionOverride;
                    _apConnectionOverride = null;
                }
                _archipelago.Connect(_state.APConnectionInfo, _giftHandler, out var errorMessage);

                if (!_archipelago.IsConnected)
                {
                    _state.APConnectionInfo = null;
                    Game1.activeClickableMenu = new InformationDialog(errorMessage, onCloseBehavior: (_) => OnCloseBehavior());
                    return;
                }
            }

            _giftHandler.Initialize(_stardewItemManager, _mail, _archipelago);
            _logicPatcher.PatchAllGameLogic();
            _mailPatcher.PatchMailBoxForApItems();
            _archipelago.SlotData.ReplaceAllBundles();
            _archipelago.SlotData.ReplaceEntrances();
            _locationsPatcher.ReplaceAllLocationsRewardsWithChecks();
            _itemPatcher.PatchApItems();
            _goalManager.InjectGoalMethods();
            _jojaDisabler.DisableJojaMembership();
            _multiSleep.InjectMultiSleepOption(_archipelago.SlotData);
            TravelingMerchantInjections.UpdateTravelingMerchantForToday(Game1.getLocationFromName("Forest") as Forest, Game1.dayOfMonth);

            Game1.chatBox?.addMessage($"Connected to Archipelago as {_archipelago.SlotData.SlotName}. Type !!help for client commands", Color.Green);
        }

        private void OnCloseBehavior()
        {
            Monitor.Log("You are not allowed to load a save without connecting to Archipelago", LogLevel.Error);
            // TitleMenu.subMenu = previousMenu;
            Game1.ExitToTitle();
        }

        private void ReadPersistentArchipelagoData()
        {
            var state = _helper.Data.ReadSaveData<ArchipelagoStateDto>(AP_DATA_KEY);
            if (state != null)
            {
                _state = state;
                _archipelago.ScoutedLocations = _state.LocationsScouted;
            }

            var apExperience = _helper.Data.ReadSaveData<Dictionary<int, int>>(AP_EXPERIENCE_KEY);
            SkillInjections.SetArchipelagoExperience(apExperience);
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            if (!_archipelago.MakeSureConnected(5))
            {
                return;
            }

            if (MultiSleep.DaysToSkip > 0)
            {
                MultiSleep.DaysToSkip--;
                Game1.NewDay(0);
                return;
            }

            FarmInjections.DeleteStartingDebris();
            _mail.SendToday();
            _locationChecker.VerifyNewLocationChecksWithArchipelago();
            _locationChecker.SendAllLocationChecks();
            _itemManager.ReceiveAllNewItems();
            _goalManager.CheckGoalCompletion();
            _mail.SendTomorrow();
            PlayerBuffInjections.CheckForApBuffs();
            Entrances.UpdateDynamicEntrances();
        }

        private void OnDayEnding(object sender, DayEndingEventArgs e)
        {
            _giftHandler.ReceiveAllGiftsTomorrow();
        }

        private void OnTimeChanged(object sender, TimeChangedEventArgs e)
        {
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            _archipelago.APUpdate();
        }

        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            ResetArchipelago();
        }

        private void OnCommandConnectToArchipelago(string arg1, string[] arg2)
        {
            if (arg2.Length < 2)
            {
                Monitor.Log($"You must provide an IP with a port, and a slot name, to connect to archipelago. {CONNECT_SYNTAX}", LogLevel.Info);
                return;
            }

            var ipAndPort = arg2[0].Split(":");
            if (ipAndPort.Length < 2)
            {
                Monitor.Log($"You must provide an IP with a port, and a slot name, to connect to archipelago. {CONNECT_SYNTAX}", LogLevel.Info);
                return;
            }

            var ip = ipAndPort[0];
            var port = int.Parse(ipAndPort[1]);
            var slot = arg2[1];
            var password = arg2.Length >= 3 ? arg2[2] : "";
            _apConnectionOverride = new ArchipelagoConnectionInfo(ip, port, slot, null, password);
        }

        private void OnCommandDisconnectFromArchipelago(string arg1, string[] arg2)
        {
            ArchipelagoDisconnect();
        }

        private void OnItemReceived()
        {
            _itemManager?.ReceiveAllNewItems();
        }

        private void DebugMethod(string arg1, string[] arg2)
        {

        }

        public bool ArchipelagoConnect(string ip, int port, string slot, string password, out string errorMessage)
        {
            var apConnection = new ArchipelagoConnectionInfo(ip, port, slot, null, password);
            _archipelago.Connect(apConnection, _giftHandler, out errorMessage);
            if (!_archipelago.IsConnected)
            {
                return false;
            }

            _state.APConnectionInfo = apConnection;
            return true;
        }

        public void ArchipelagoDisconnect()
        {
            Game1.ExitToTitle();
            _archipelago.DisconnectPermanently();
            _state.APConnectionInfo = null;
        }

        private void TestGetSpecificItem(string arg1, string[] arg2)
        {
            _tester.TestGetSpecificItem(arg1, arg2);
        }

        private void TestGetAllItems(string arg1, string[] arg2)
        {
            _tester.TestGetAllItems(arg1, arg2);
        }
    }
}
