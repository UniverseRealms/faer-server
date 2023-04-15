﻿using Shared;
using Shared.resources;
using GameServer.realm;
using GameServer.realm.entities;
using GameServer.realm.entities.player;
using NLog;

namespace GameServer.logic.loot
{
    public struct LootDef
    {
        public LootDef(Item item, double probability)
        {
            Probability = probability;
            Item = item;
        }

        public readonly Item Item;
        public readonly double Probability;
    }

    public class Loot : List<ILootDef>
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public Loot(params ILootDef[] lootDefs)
        {
            //For independent loots(e.g. chests)
            AddRange(lootDefs);
        }

        private static readonly Random Rand = new();

        public IEnumerable<Item> GetLoots(RealmManager manager, int min, int max)
        {
            //For independent loots(e.g. chests)
            var consideration = new List<LootDef>();
            foreach (var i in this)
                i.Populate(manager, null, null, Rand, consideration);

            var retCount = Rand.Next(min, max);
            foreach (var i in consideration)
            {
                if (Rand.NextDouble() < i.Probability)
                {
                    yield return i.Item;
                    retCount--;
                }

                if (retCount == 0)
                    yield break;
            }
        }

        public static readonly ushort BrownBag = 0x0407;
        public static readonly ushort BlackBag = 0x0408;
        private static readonly ushort EggBasket = 0x0409;
        private static readonly ushort BlueBag = 0x040A;
        private static readonly ushort GreyBag = 0x040B;
        private static readonly ushort GoldenBag = 0x040C;
        private static readonly ushort RedBag = 0x040D;
        private static readonly ushort PinkBag = 0x040E;
        private static readonly ushort CyanBag = 0x040F;

        public void Handle(Enemy enemy)
        {
            if (enemy.Spawned) 
                return;

            var consideration = new List<LootDef>();
            var shared = new List<Item>();
            foreach (var i in this)
                i.Populate(enemy.Manager, enemy, null, Rand, consideration);

            var dats = enemy.DamageCounter.GetPlayerData();
            foreach (var i in consideration)
                if (Rand.NextDouble() < i.Probability && i.Item?.Untradable == false)
                    shared.Add(i.Item);
            if (shared.Count > 0)
                AddBagToWorld(enemy, shared, new Player[0]);

            foreach (var dat in dats)
            {
                consideration.Clear();
                foreach (var i in this) 
                    i.Populate(enemy.Manager, enemy, dat, Rand, consideration);

                var lootDropBoost = dat.Item1.LDBoostTime > 0 ? 1.5 : 1;
                var luckStatBoost = 1 + dat.Item1.Stats[10] / 100.0;
                var globalLBoost = DateTime.UtcNow.ToUnixTimestamp() < Constants.EventEnds.ToUnixTimestamp()
                    ? Constants.GlobalLootBoost ?? 1
                    : 1;

                var playerLoot = new List<Item>();
                foreach (var i in consideration)
                    if (Rand.NextDouble() < i.Probability * lootDropBoost * luckStatBoost * globalLBoost)
                        playerLoot.Add(i.Item);
                if (playerLoot.Count > 0)
                    AddBagToWorld(enemy, playerLoot, new[] { dat.Item1 });
            }
        }

        private static void AddBagToWorld(Enemy enemy, List<Item> items, Player[] owners)
        {
            var bag = BrownBag;
            var bagType = 0;
            var player = owners.Length > 0 ? owners[0] : null;
            for (var i = 0; i < items.Count; i++)
            {
                var type = items[i]?.BagType;
                if (type != null && type > bagType)
                    bagType = (int)type;
            }
            switch (bagType)
            {
                case 0:
                    bag = BrownBag;
                    break;
                case 1:
                    bag = BlackBag;
                    break;
                case 2:
                    bag = EggBasket;
                    break;
                case 3:
                    bag = BlueBag;
                    break;
                case 4:
                    bag = GreyBag;
                    break;
            }

            var container = new Container(enemy.Manager, bag, 1000 * 60, true);
            container.Inventory.SetItems(items.ToArray());
            container.BagOwners = owners.Select(x => x.AccountId).ToArray();
            container.Move(
                enemy.X + (float)((Rand.NextDouble() * 2 - 1) * 0.5),
                enemy.Y + (float)((Rand.NextDouble() * 2 - 1) * 0.5));
            container.SetDefaultSize((ushort) (bagType > 3 ? 110 : 80));
            enemy.Owner.EnterWorld(container);
            container.AlwaysTick = true;
        }
    }
}