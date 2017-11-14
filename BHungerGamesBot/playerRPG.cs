using Discord;
using System;

namespace BHungerGaemsBot
{
    class PlayerRPG : Player
    {
        public int Points { get; set; }
        public Adventure adventure;
        public int Notoriety { get; set; }

        public bool IsInLeaderboard { get; set; }

        private const float LevelFactor = 1.4f;
        private const float BaseExp = 50f;
        private int Experience { get; set; }
        public int Level
        {
            get
            {
                return Convert.ToInt32(Math.Pow(Experience / BaseExp, 1f / LevelFactor));
            }
        }

        public int EffectiveCombatStats {
            get
            {
                int CombatPower = 0;
                for (int i = 0; i < HeroStats.Length; i++)
                {
                    CombatPower += Convert.ToInt32(HeroStats[i] * BHungerGamesV3.HeroScalingDictionary[HeroClass][i]);
                }
                foreach (ItemRPG item in Items)
                {
                    CombatPower += Convert.ToInt32(item.GetEffectiveCombatStats(HeroClass, item.ItemStats));
                }
                return CombatPower;
            }
        }

        public int[] HeroStats = new int[7];
        public float[] HeroStatMult = new float[7];

        public HeroClass HeroClass { get; set; }

        public InteractiveRPGDecision InteractiveRPGDecision { get; set; }

        public ItemRPG[] Items { get; set; }
        public Familiar Familiar = new Familiar();

        public PlayerRPG(IUser userParm) : base(userParm)
        {
            adventure = new Adventure();
            HeroClass = HeroClass.Mage;
            IsInLeaderboard = false;
            Items = new ItemRPG[BHungerGamesV3.NumItemTypes];
            for (var index = 0; index < Items.Length; index++)
            {
                Items[index] = new ItemRPG();
            }
        }

    }
}
