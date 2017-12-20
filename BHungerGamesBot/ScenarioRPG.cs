using System;
using System.Collections.Generic;
using System.Text;

namespace BHungerGaemsBot
{
    class ScenarioRPG
    {
        private string Description;
        private RarityRPG Rarity;
        public int Timer;

        public ScenarioRPG(string description, RarityRPG rarity)
        {
            Description = description;
            Rarity = rarity;
            Timer = 0;
        }

        public string GetText(string player, RarityRPG rarity, HeroClass heroClass)
        {
            string value = Description?.Replace("{player_name}", player);
            value = value?.Replace("{rarity_type}", rarity.ToString());
            value = value?.Replace("{class_type}", heroClass.ToString());

            return value;
        }

        public void ReduceTimer()
        {
            if (Timer != 0) Timer--;
        }
    }
}
