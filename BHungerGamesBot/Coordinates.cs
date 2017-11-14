using System;
using System.Collections.Generic;
using System.Text;

namespace BHungerGaemsBot
{
    class Coordinates
    {
        private int posX;
        private int posY;
        public LootQuality lootQuality;
        public bool isWater;
        public bool isDropable;

        public Coordinates(int x, int y)
        {
            posX = x;
            posX = y;
            lootQuality = LootQuality.Micro;
            isWater = false;
            isDropable = false;
        }


    }
}
