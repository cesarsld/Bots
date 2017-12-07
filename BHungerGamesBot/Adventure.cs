using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;


namespace BHungerGaemsBot
{
    class Adventure
    {

        private readonly Random _random;
        public int AdventureCompletion { get; set; }
        public HeroClass HeroAffinity { get; set; }
        //                                    Tr  Co   Uc   Ra   Ep   He   Le   An   Ar   Un 
        private static int[] eee = new int[] {  50, 250, 500, 700, 845, 925, 965, 985, 995  };

        private static readonly ReadOnlyCollection<LootTable> LootTables;

        public Adventure()
        {
            _random = new Random();
        }

        static Adventure()
        {
            LootTables = new ReadOnlyCollection<LootTable>( new List<LootTable>()
            {//               Tr  Co   Uc   Ra   Ep   He   Le   An   Ar   Un 
                new LootTable( 100, 400, 600, 760, 865, 925, 965, 985, 995),
                new LootTable(  50, 380, 580, 740, 850, 914, 958, 982, 994),
                new LootTable(  25, 360, 560, 720, 835, 903, 951, 979, 993),
                new LootTable(  10, 340, 540, 700, 820, 892, 944, 976, 992),
                new LootTable(   0, 320, 320, 680, 805, 881, 937, 973, 991),
                new LootTable(   0, 300, 500, 660, 790, 870, 930, 970, 990),
            });
        }

        private class LootTable
        {
            public readonly int TrashChance;
            public readonly int CommonChance;
            public readonly int UncommonChance;
            public readonly int RareChance;
            public readonly int EpicChance;
            public readonly int HeroicChance;
            public readonly int LegendaryChance;
            public readonly int AncientChance;
            public readonly int RelicChance;
            //public readonly int UniqueChance;

            public LootTable(int trashChance, int commonChance, int uncommonChance, int rareChance, int epicChance, int heroicChance, int legendaryChance, int ancientChance, int relicChance)
            {
                TrashChance = trashChance;
                CommonChance = commonChance;
                UncommonChance = uncommonChance;
                RareChance = rareChance;
                EpicChance = epicChance;
                HeroicChance = heroicChance;
                LegendaryChance = legendaryChance;
                AncientChance = ancientChance;
                RelicChance = relicChance;
                //UniqueChance = uniqueChance;
            }

            public RarityRPG GetRarity(int value)
            {
                if (value < TrashChance) return RarityRPG.Trash;
                if (value < CommonChance) return RarityRPG.Common;
                if (value < UncommonChance) return RarityRPG.Common;
                if (value < RareChance) return RarityRPG.Rare;
                if (value < EpicChance) return RarityRPG.Epic;
                if (value < HeroicChance) return RarityRPG.Heroic;
                if (value < LegendaryChance) return RarityRPG.Legendary;
                if (value < AncientChance) return RarityRPG.Ancient;
                if (value < RelicChance) return RarityRPG.Relic;
                //if (value < UniqueChance) return RarityRPG.Unique;
                return RarityRPG.Unique;
            }
        }

        public void PerformAdventure(PlayerRPG player, int turn, HeroClass adventureAffinity)
        {
            AdventureCompletion = 0;
            HeroAffinity = adventureAffinity;
            int turnSCaling = 25;
            int levelScaling = 20;
            float CPscaling = 0.25f;

            int adventureCombatPower = 0;
            adventureCombatPower = turn * turnSCaling + player.Level * levelScaling + Convert.ToInt32(player.EffectiveCombatStats * CPscaling);
            for (int i = 0; i < 10; i++)
            {
                if (TierTrial(player.EffectiveCombatStats, adventureCombatPower))
                {
                    AdventureCompletion++;
                }
            }

            if (AdventureCompletion == 10)
            {
                player.Notoriety++;
            }
            GetLoot(player);
            GetExp(player);
            GetScore(player, AdventureCompletion);

            
        }

        private bool TierTrial(int a, int b)
        {
            Random rnd = new Random();
            float heroAdvantage = 3f;
            int totalChances = Convert.ToInt32(a * heroAdvantage);
            if (rnd.Next(totalChances + b) < totalChances)
            {
                return true;
            }
            return false;
        }
        private void GetLoot(PlayerRPG player)
        {
            int luckModifier = Convert.ToInt32(75 * Math.Log(Math.Pow(player.HeroStats[6], 0.5) / 2));
            if (player.InteractiveRPGDecision == InteractiveRPGDecision.LookForLoot)
            {
                luckModifier = Convert.ToInt32(luckModifier * 1.5);
            }
            int itemsToLoot = AdventureCompletion / 2 + 1;
            for (int i = 0; i < itemsToLoot; i++)
            {
                double lootMultiplier = 2 + player.Level / 7;
                RarityRPG itemRarity = LootTables[i].GetRarity(_random.Next(luckModifier, 1000));
                player.Items[_random.Next(4)].GetNewItem(player.Level, itemRarity, player.HeroClass, GetDistribution());
                player.Points += Convert.ToInt32(Math.Pow((int)itemRarity,lootMultiplier));
            }
        }

        private ItemDistribution GetDistribution()
        {
            int index = _random.Next(100);
            if (index < 20) return ItemDistribution.Average;
            if (index < 80) return ItemDistribution.Advantageous;
            if (index < 100) return ItemDistribution.Extreme;
            return ItemDistribution.Average;
        }

        private void GetExp(PlayerRPG player)
        {
            int totalExp = 0;
            int exp = 10 + player.Level;
            if (player.InteractiveRPGDecision == InteractiveRPGDecision.LookForExp)
            {
                exp = Convert.ToInt32(exp * 1.25);
            }
            
            for (int i = 0; i < AdventureCompletion; i++)
            {
                totalExp += _random.Next(Convert.ToInt32(0.8 * exp), Convert.ToInt32(1.2 * exp));
                exp = Convert.ToInt32(1.2 * exp);
            }
            player.AddExp(exp);
            player.Points += Convert.ToInt32(exp / 2);
        }
        private void GetScore(PlayerRPG player, int adventureCompletion)
        {
            float scoreMultiplier = 1f + (player.Level / 2);
            player.Points += Convert.ToInt32(adventureCompletion * scoreMultiplier);
        }
        
    }
}
