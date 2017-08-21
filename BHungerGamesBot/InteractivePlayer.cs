using Discord;

namespace BHungerGaemsBot
{
    public class InteractivePlayer: Player
    {
        public int Hp { get; set; }
        public int ScenarioLikelihood { get; set; }
        public int AlertCooldown { get; set; }
        public int DebuffTimer { get; set; }
        public int WeaponLife { get; set; }
        public int ArmourLife { get; set; }
        public int OffhandLife { get; set; }
        public int HelmetLife { get; set; }
        public int ScenarioItemFindBonus { get; set; }
        public InteractiveDecision InteractiveDecision { get; set; }
        public EnhancedDecision EnhancedDecision { get; set; }
        public Rarity WeaponRarity { get; set; }
        public Rarity ArmourRarity { get; set; }
        public Rarity HelmetRarity { get; set; }
        public Rarity OffhandRarity { get; set; }
        public Debuff Debuff { get; set; }

        public InteractivePlayer(IUser userParm) : base(userParm)
        {
            Hp = 100;
            ScenarioLikelihood = 30;
            AlertCooldown = 0;
            DebuffTimer = 0;
            WeaponLife = 0;
            ArmourLife = 0;
            ScenarioItemFindBonus = 0;
        }

        public void Reset()
        {
            ScenarioLikelihood = 20;
            InteractiveDecision = InteractiveDecision.DoNothing;
            EnhancedDecision = EnhancedDecision.None;
        }

    }
}
