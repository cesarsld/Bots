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
        //private static int[] eee = new int[] {  50, 250, 500, 700, 845, 925, 965, 985, 995  };

        private static readonly ReadOnlyCollection<LootTable> LootTables;

        public Adventure()
        {
            _random = new Random(Guid.NewGuid().GetHashCode());
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

        public StringBuilder PerformAdventure(PlayerRPG player, int turn, HeroClass adventureAffinity, int playerNumber, ScenarioRPG[] scenarios)
        {
            StringBuilder returnStringBuilder = new StringBuilder(10000);

            AdventureCompletion = 0;
            HeroAffinity = adventureAffinity;
            int turnSCaling = 85;
            int levelScaling = 120;
            float CPscaling = 0.5f;

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
                returnStringBuilder.Append($"{player.NickName} has fully completed the adventure! Extra rewards and * notoriety * will be granted to them!\n\n");
            }
            GetLoot(player, HeroAffinity, ref returnStringBuilder, playerNumber, scenarios);
            player.GetExp(AdventureCompletion);
            player.GetScore(AdventureCompletion);

            return returnStringBuilder;
        }

        private bool TierTrial(int a, int b)
        {
            float heroAdvantage = 2f;
            int totalChances = Convert.ToInt32(a * heroAdvantage);
            if (_random.Next(totalChances + b) < totalChances)
            {
                return true;
            }
            return false;
        }
        private void GetLoot(PlayerRPG player, HeroClass adventureClass, ref StringBuilder sbLoot, int playerCount, ScenarioRPG[] scenarios)
        {
            //in the future add few line that prioritise adventureClass but also give out other classes 
            int luckModifier = Convert.ToInt32(75 * Math.Log(Math.Pow(player.HeroStats[6], 0.5) / 2));
            RarityRPG bestRarity = RarityRPG.Trash;
            if (player.InteractiveRPGDecision == InteractiveRPGDecision.LookForLoot)
            {
                luckModifier = Convert.ToInt32(luckModifier * 1.5);
            }
            int itemsToLoot = AdventureCompletion / 2 + 1;
            for (int i = 0; i < itemsToLoot; i++)
            {
                double lootMultiplier = 2 + player.Level / 4;
                int roll = _random.Next(luckModifier, 1000);
                RarityRPG itemRarity = LootTables[i].GetRarity(roll);
                bestRarity = ((int)itemRarity > (int)bestRarity) ? itemRarity : bestRarity;
                Console.WriteLine($"{player.NickName} item dropped rarity : {itemRarity} + luckmod is {luckModifier} + rolls is {roll}");
                player.Items[_random.Next(4)].GetNewItem(player.Level, itemRarity, adventureClass, GetDistribution());
                player.Points += Convert.ToInt32(Math.Pow((int)itemRarity,lootMultiplier));
            }
            ReduceTextCongestion(GetScenario(scenarios).GetText(player.NickName, bestRarity, adventureClass), ref sbLoot, playerCount, bestRarity);
            sbLoot.Append("\n");
        }

        private ItemDistribution GetDistribution()
        {
            int index = _random.Next(100);
            if (index < 20) return ItemDistribution.Average;
            if (index < 80) return ItemDistribution.Advantageous;
            if (index < 100) return ItemDistribution.Extreme;
            return ItemDistribution.Average;
        }

        private ScenarioRPG GetScenario(ScenarioRPG[] scenarios)
        {
            while (true)
            {
                int randIndex = _random.Next(scenarios.Length);
                if (scenarios[randIndex].Timer <= 0 )
                {
                    scenarios[randIndex].Timer = 0;
                    return scenarios[randIndex];
                }
            }
        }

        private void ReduceTextCongestion(string text, ref StringBuilder sb, int players, RarityRPG rarity)
        {
            if (players < 15 && (int)rarity > 4)
            {
                sb.Append(text + "\n");
            }
            else if (players < 30 && (int)rarity > 5)
            {
                sb.Append(text + "\n");
            }
            else if (players < 60 && (int)rarity > 6)
            {
                sb.Append(text + "\n");
            }
        }

    }
}
