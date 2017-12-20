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
        public bool HasDueled { get; set; }

        Random _random;

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
            _random = new Random(Guid.NewGuid().GetHashCode());
            HasDueled = false;
        }
        public PlayerRPG(int index) : base(index)
        {
            adventure = new Adventure();
            HeroClass = HeroClass.Mage;
            IsInLeaderboard = false;
            Items = new ItemRPG[BHungerGamesV3.NumItemTypes];
            for (var i = 0; i < Items.Length; i++)
            {
                Items[i] = new ItemRPG();
            }
            HasDueled = false;
        }

        public void GetExp(int adventureCompletion)
        {
            int totalExp = 0;
            int exp = 10 + Level;
            if (InteractiveRPGDecision == InteractiveRPGDecision.LookForExp)
            {
                exp = Convert.ToInt32(exp * 1.25);
            }

            for (int i = 0; i < adventureCompletion; i++)
            {
                totalExp += _random.Next(Convert.ToInt32(0.8 * exp), Convert.ToInt32(1.2 * exp));
                exp = Convert.ToInt32(1.2 * exp);
            }
            AddExp(exp);
            Points += Convert.ToInt32(exp / 2);
        }
        public void GetScore(int adventureCompletion)
        {
            float scoreMultiplier = 1.5f + (Level * 1.5f);
            Points += Convert.ToInt32(adventureCompletion * scoreMultiplier);
        }

        public void Train()
        {
            int exp = 5 + Level * 5;
            int totalExp = _random.Next(8 * exp, 12 * exp);
            AddExp(totalExp);
        }

        public void AddExp(int value)
        {
            Experience += value;
        }

    }
}
