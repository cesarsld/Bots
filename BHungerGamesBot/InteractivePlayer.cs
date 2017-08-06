using System;
using System.Collections.Generic;
using System.Text;
using Discord;
using Discord.WebSocket;

namespace BHungerGaemsBot
{
    public class InteractivePlayer: Player
    {
        public int hp { get; set; }
        public int scenarioLikelihood { get; set; }
        public int alertCooldown { get; set; }
        public int debuffTimer { get; set; }
        public int weaponLife { get; set; }
        public int armourLife { get; set; }

        public InteractivePlayer(IUser userParm) : base(userParm)
        {
            hp = 100;
            scenarioLikelihood = 30;
            alertCooldown = 0;
            debuffTimer = 0;
            weaponLife = 0;
            armourLife = 0;
        }

        public InteractiveDecision interactiveDecision;
        public enum InteractiveDecision
        {
            DoNothing,
            Loot,
            StayOnAlert
        }
        public EnhancedDecision enhancedDecision;
        public enum EnhancedDecision
        {
            None,
            MakeATrap,
            Sabotage,
            Steal
        }

        public Rarity weaponRarity;
        public Rarity armourRarity;
        public enum Rarity
        {
            Common,
            Rare,
            Epic,
            Legendary,
            Set
        }
        public Debuff debuff;
        public enum Debuff
        {
            DecreasedItemFind,
            SevererlyDecreasedItemFind,

            IncreasedScenarioLikelihood,
            SeverlyIncreasedScenarioLikelihood,

            DecreasedDuelChance,
            SeverlyDecreasedDuelChance,

            IncreasedDamageTaken,
            SeverlyIncreasedDamgeTaken
        }
    }
}
