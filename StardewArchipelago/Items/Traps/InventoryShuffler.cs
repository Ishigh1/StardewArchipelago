﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewArchipelago.Extensions;
using StardewValley;
using StardewValley.Objects;
using xTile.Dimensions;

namespace StardewArchipelago.Items.Traps
{
    public class InventoryShuffler
    {
        private class ItemSlot
        {
            public IList<Item> Inventory { get; set; }
            public int SlotNumber { get; set; }

            public ItemSlot(IList<Item> inventory, int slotNumber)
            {
                Inventory = inventory;
                SlotNumber = slotNumber;
            }

            public void SetItem(Item item)
            {
                while (SlotNumber >= Inventory.Count)
                {
                    Inventory.Add(null);
                }
                Inventory[SlotNumber] = item;
            }
        }

        public void ShuffleInventories(ShuffleInventoryTarget targets)
        {
            if (targets == ShuffleInventoryTarget.None)
            {
                return;
            }

            var slotsToShuffle = new Dictionary<ItemSlot, Item>();

            AddItemSlotsFromPlayerInventory(slotsToShuffle, targets == ShuffleInventoryTarget.Hotbar);
            if (targets == ShuffleInventoryTarget.InventoryAndChests)
            {
                AddItemSlotsFromChestsInEntireWorld(slotsToShuffle);
            }

            var allSlots = slotsToShuffle.Keys.ToList();
            var allItems = slotsToShuffle.Values.ToList();
            var random = new Random((int)(Game1.uniqueIDForThisGame + Game1.stats.DaysPlayed));
            var allItemsShuffled = allItems.Shuffle(random);

            for (var i = 0; i < allSlots.Count; i++)
            {
                allSlots[i].SetItem(allItemsShuffled[i]);
            }

            foreach (var chest in FindAllChests())
            {
                chest.clearNulls();
            }
        }

        private static void AddItemSlotsFromPlayerInventory(Dictionary<ItemSlot, Item> slotsToShuffle, bool hotbarOnly)
        {
            var player = Game1.player;
            var maxSlot = hotbarOnly ? 12 : player.MaxItems;
            for (var i = 0; i < maxSlot; i++)
            {
                Item item = null;
                if (player.Items.Count > i && player.Items[i] != null)
                {
                    item = player.Items[i];
                }

                var slot = new ItemSlot(player.Items, i);
                slotsToShuffle.Add(slot, item);
            }
        }

        private static void AddItemSlotsFromChestsInEntireWorld(Dictionary<ItemSlot, Item> slotsToShuffle)
        {
            foreach (var chest in FindAllChests())
            {
                AddItemSlotsFromChest(slotsToShuffle, chest);
            }
        }

        private static IEnumerable<Chest> FindAllChests()
        {
            var locations = Game1.locations.ToList();
            foreach (var building in Game1.getFarm().buildings)
            {
                if (building?.indoors.Value == null)
                {
                    continue;
                }
                locations.Add(building.indoors.Value);
            }

            foreach (var gameLocation in locations)
            {
                foreach (var (tile, gameObject) in gameLocation.Objects.Pairs)
                {
                    if (gameObject is not Chest chest || chest.SpecialChestType != Chest.SpecialChestTypes.None || chest.giftbox.Value)
                    {
                        continue;
                    }

                    yield return chest;
                }
            }
        }

        private static void AddItemSlotsFromChest(Dictionary<ItemSlot, Item> slotsToShuffle, Chest chest)
        {
            var capacity = chest.GetActualCapacity();
            for (var i = 0; i < capacity; i++)
            {
                Item item = null;
                if (chest.items.Count > i && chest.items[i] != null)
                {
                    item = chest.items[i];
                }

                var slot = new ItemSlot(chest.items, i);
                slotsToShuffle.Add(slot, item);
            }
        }
    }
}