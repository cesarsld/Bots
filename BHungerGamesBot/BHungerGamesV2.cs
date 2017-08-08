using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Linq;
using Discord;
using Discord.Commands;


namespace BHungerGaemsBot
{
    public class BHungerGamesV2
    {
        private readonly Random _random;
        public BHungerGamesV2()
        {
            _random = new Random();
        }
        private int duelCooldown = 4;
        public List<Trap> traps;
        public  List<InteractivePlayer> contestants;
        public  List<int> enhancedIndexList = new List<int>();
        private static readonly Scenario[] Scenarios;
        //private BotGameInstance botGameInstance = new BotGameInstance();

        private class Scenario {
            private readonly string _description;
            public int _typeValue;
            public enum Type
            {
                Damaging,
                Lethal,
                Healing,
                LootFind
            }
            public Type _type;
            public Scenario(string description, Type type, int typeValue) //creates instance of a scenario, defining its description and players needed
            {
                _description = description;
                _type = type;
                _typeValue = typeValue;
                if (type != Type.Lethal)
                {
                    _description = _description.Replace("{_typeValue}", _typeValue.ToString());
                }
            }
            public string GetText(string player) //replace {@Px} by player name
            {
                string value = _description?.Replace("{@P1}", player);
                return value;
            }
        }

        static BHungerGamesV2()
        {
            Scenarios = new[]
            {
                new Scenario ("{@P1} has been dealt {_typeValue} HP", Scenario.Type.Damaging, 20),
                new Scenario ("{@P1} has been killed", Scenario.Type.Lethal, 100),
                new Scenario ("{@P1} has been healed for {_typeValue} HP", Scenario.Type.Healing, 20),
                new Scenario ("{@P1} has increased loot find pf {_typeValue} for the next turn", Scenario.Type.LootFind, 10),
            };
        }
        public class Trap
        {
            public int damage;
            public string trapID;
            Random _random = new Random(Guid.NewGuid().GetHashCode());
            public Trap(InteractivePlayer contestant)
            {
                damage = 5 * _random.Next(2, 9);
                trapID = contestant.UserNameWithDiscriminator;
            }
        }


        public void Run(int numWinners, List<InteractivePlayer> contestantsTransfer, Action<string, string> showMessageAction, Func<bool> cannelGame, int maxPlayers = 0)
        {
            TimeSpan delayBetweenCycles = new TimeSpan(0, 0, 0, 30);
            int day = 0;
            int night = 0;
            int scenarioToBeExecuted;
            int index;
            contestants = contestantsTransfer;
            StringBuilder sb = new StringBuilder(2000);
            traps = new List<Trap>();

            if (maxPlayers > 0 && contestants.Count > maxPlayers)
            {
                int numToRemove = contestants.Count - maxPlayers;
                for (int i = 0; i < numToRemove; i++)
                {
                    int randIndex = _random.Next(contestants.Count);
                    sb.Append($"<{contestants[randIndex].ContestantName}>\t");
                    contestants.RemoveAt(randIndex);
                }
                showMessageAction("Players killed in the stampede trying to get to the arena:\r\n" + sb, null);
                sb.Clear();
            }
            
            while (contestants.Count > numWinners)
            {

                // day cycle
                day++;
                int startingContestantCount = contestants.Count;
                scenarioToBeExecuted = startingContestantCount / 4;
                if (scenarioToBeExecuted < 1)
                {
                    scenarioToBeExecuted = 1;
                }
                int playerToEnhance = contestants.Count / 4;
                if (playerToEnhance < 0)
                {
                    playerToEnhance = 1;
                }
                // Select Enhanced players
                showMessageAction("Reached Enahnced Slection code", null);
                /*enhancedIndexList.Add(_random.Next(contestants.Count));
                playerToEnhance--;
                while (playerToEnhance != 0)
                {
                    int enhancedIndexCheck = _random.Next(contestants.Count);
                    foreach (int enhanced in enhancedIndexList.ToList())
                    {
                        if (enhancedIndexCheck == enhanced)
                        {
                            enhancedIndexCheck = _random.Next(contestants.Count);
                            break;
                        }
                        enhancedIndexList.Add(enhancedIndexCheck);
                        playerToEnhance--;
                    }
                    
                }*/
                showMessageAction("You have 30 seconds to input your decision"
                    + $" You may select 💰 to Loot or ❗ to Stay On Alert! If you do NOT select a reaction, you will Do Nothing.", null);
                Thread.Sleep(delayBetweenCycles);

                //botGameInstance.fetchInteractivePlayerInput(30, channel);
                //code to collect users choice to be implemented
                //Bot.DiscordClient.ReactionAdded += 
                sb.Append("People that successfully dropped loot: \n\n");
                foreach (InteractivePlayer contestant in contestants)
                {
                    switch (contestant.interactiveDecision)
                    {
                        case InteractivePlayer.InteractiveDecision.Loot:
                            loot(contestant, ref sb);
                            break;
                        case InteractivePlayer.InteractiveDecision.StayOnAlert:
                            stayOnAlert(contestant);
                            break;
                        default:
                            //do nothing
                            break;
                    }
                    /*switch (contestant.enhancedDecision)
                    {
                        case InteractivePlayer.EnhancedDecision.Sabotage:
                            sabotage(contestant, contestants);
                            break;
                        case InteractivePlayer.EnhancedDecision.Steal:
                            steal(contestant, contestants);
                            break;
                        case InteractivePlayer.EnhancedDecision.MakeATrap:
                            trap(contestant);
                            break;
                        default:
                            //player not selected for enhanced options
                            break;
                    }*/
                }
                showMessageAction("" + sb, null);
                sb.Clear();

                //night cycle
                night++;
                while (scenarioToBeExecuted != 0) //scenarios
                {
                    index = _random.Next(contestants.Count);
                    switch (contestants[index].debuff)
                    {
                        case InteractivePlayer.Debuff.IncreasedScenarioLikelihood when contestants[index].debuffTimer > 0:
                            contestants[index].scenarioLikelihood += 10;
                            contestants[index].debuffTimer--;
                            break;
                        case InteractivePlayer.Debuff.SeverlyIncreasedScenarioLikelihood when contestants[index].debuffTimer > 0:
                            contestants[index].scenarioLikelihood += 10;
                            contestants[index].debuffTimer--;
                            break;
                        default:
                            break;
                    }
                    if (RNGroll(contestants[index].scenarioLikelihood))
                    {
                        scenarioToBeExecuted--;
                        //scenario method 
                        int scenarioIndex = _random.Next(Scenarios.Length);
                        switch (Scenarios[scenarioIndex]._type)
                        {
                            case Scenario.Type.Damaging:
                                contestants[index].hp -= Scenarios[scenarioIndex]._typeValue;
                                if (contestants[index].hp < 0)
                                {
                                    contestants.RemoveAt(index);
                                }
                                break;
                            case Scenario.Type.Lethal:
                                contestants.RemoveAt(index);
                                break;
                            case Scenario.Type.Healing:
                                contestants[index].hp += Scenarios[scenarioIndex]._typeValue;
                                if (contestants[index].hp < 100)
                                {
                                    contestants[index].hp = 100;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }

                for (int i = 0; i < traps.Count; i++)

                foreach (Trap trap in traps)
                {
                    if (RNGroll(15))
                    {
                        index = _random.Next(contestants.Count);
                        while (trap.trapID == contestants[index].UserNameWithDiscriminator)
                        {
                            index = _random.Next(contestants.Count);
                        }
                        contestants[index].hp -= trap.damage;
                        if (contestants[index].hp < 0)
                        {
                            contestants.RemoveAt(index);
                        }
                            traps.Remove(trap);
                    }
                }
                if (duelCooldown != 0)
                {
                    duelCooldown--;
                }
                else
                {
                    duel(contestants, ref sb);
                }
                foreach (InteractivePlayer contestant in contestants)
                {
                    reset(contestant); //resets value of scenariolikelihood and interactive options
                }
                Thread.Sleep(delayBetweenCycles);

                if (cannelGame())
                    return;

            }
        }

        //duel method

        void duel(List <InteractivePlayer> contestants,ref StringBuilder sb)
        {
            int duelChance = 50;
            int duelist_1, duelist_2;
            duelist_1 = _random.Next(contestants.Count);
            duelist_2 = _random.Next(contestants.Count);
            while (duelist_1 == duelist_2)
            {
                duelist_2 = _random.Next(contestants.Count);
            }
            if (contestants[duelist_1].weaponLife > 0)
            {
                switch (contestants[duelist_1].weaponRarity)
                {
                    case InteractivePlayer.Rarity.Common:
                        duelChance += 5;
                        break;
                    case InteractivePlayer.Rarity.Rare:
                        duelChance += 10;
                        break;
                    case InteractivePlayer.Rarity.Epic:
                        duelChance += 15;
                        break;
                    case InteractivePlayer.Rarity.Legendary:
                        duelChance += 20;
                        break;
                    case InteractivePlayer.Rarity.Set:
                        duelChance += 25;
                        break;

                }
            }
            switch (contestants[duelist_1].debuff)
            {
                case InteractivePlayer.Debuff.DecreasedDuelChance when contestants[duelist_1].debuffTimer > 0:
                    duelChance -= 5;
                    contestants[duelist_1].debuffTimer--;
                    break;
                case InteractivePlayer.Debuff.SeverlyDecreasedDuelChance when contestants[duelist_1].debuffTimer > 0:
                    duelChance -= 10;
                    contestants[duelist_1].debuffTimer--;
                    break;
            }
            if (contestants[duelist_2].weaponLife > 0)
            {
                switch (contestants[duelist_2].weaponRarity)
                {
                    case InteractivePlayer.Rarity.Common:
                        duelChance -= 5;
                        break;
                    case InteractivePlayer.Rarity.Rare:
                        duelChance -= 10;
                        break;
                    case InteractivePlayer.Rarity.Epic:
                        duelChance -= 15;
                        break;
                    case InteractivePlayer.Rarity.Legendary:
                        duelChance -= 20;
                        break;
                    case InteractivePlayer.Rarity.Set:
                        duelChance -= 25;
                        break;

                }
            }
            switch (contestants[duelist_2].debuff)
            {
                case InteractivePlayer.Debuff.DecreasedDuelChance when contestants[duelist_2].debuffTimer > 0:
                    duelChance += 5;
                    contestants[duelist_2].debuffTimer--;
                    break;
                case InteractivePlayer.Debuff.SeverlyDecreasedDuelChance when contestants[duelist_2].debuffTimer > 0:
                    duelChance += 10;
                    contestants[duelist_2].debuffTimer--;
                    break;
            }
            if (RNGroll(duelChance))
            {
                contestants.RemoveAt(duelist_2);
            }
            else
            {
                contestants.RemoveAt(duelist_1);
            }
        }
        //interactive options
        private void loot(InteractivePlayer contestant, ref StringBuilder sb)
        {
            contestant.scenarioLikelihood += 10;
            int lootChance = 100;
            if (contestant.debuff == InteractivePlayer.Debuff.DecreasedItemFind && contestant.debuffTimer > 0)
            {
                lootChance -= 5;
                contestant.debuffTimer--;
            }
            else if (contestant.debuff == InteractivePlayer.Debuff.SevererlyDecreasedItemFind && contestant.debuffTimer > 0)
            {
                lootChance -= 10;
                contestant.debuffTimer--;
            }
            if (RNGroll(lootChance)) // chance to loot item
            {
                int lootType = _random.Next(2); // armour or weapon
                switch (lootType)
                {
                    case 0:
                        contestant.weaponLife = 5;
                        break;
                    default:
                        contestant.armourLife = 5;
                        break;
                }
                int lootRarity = _random.Next(100);
                lootRarity = 55;
                switch (lootType)
                {
                    case 0 when lootRarity < 60:
                        contestant.weaponRarity = InteractivePlayer.Rarity.Common;
                        sb.Append($"{contestant.NickName} has obtained a Common item!");
                        break;
                    case 0 when lootRarity < 87:
                        contestant.weaponRarity = InteractivePlayer.Rarity.Rare;
                        sb.Append($"{contestant.NickName} has obtained a Rare item!");
                        break;
                    case 0 when lootRarity < 97:
                        contestant.weaponRarity = InteractivePlayer.Rarity.Epic;
                        sb.Append($"{contestant.NickName} has obtained a Epic item!");
                        break;
                    case 0 when lootRarity < 99:
                        contestant.weaponRarity = InteractivePlayer.Rarity.Legendary;
                        sb.Append($"{contestant.NickName} has obtained a Legendary item!");
                        break;
                    case 0 when lootRarity < 100:
                        contestant.weaponRarity = InteractivePlayer.Rarity.Set;
                        sb.Append($"{contestant.NickName} has obtained a Set item!");
                        break;
                    case 1 when lootRarity < 60:
                        contestant.armourRarity = InteractivePlayer.Rarity.Common;
                        break;
                    case 1 when lootRarity < 87:
                        contestant.armourRarity = InteractivePlayer.Rarity.Rare;
                        break;
                    case 1 when lootRarity < 97:
                        contestant.armourRarity = InteractivePlayer.Rarity.Epic;
                        break;
                    case 1 when lootRarity < 99:
                        contestant.armourRarity = InteractivePlayer.Rarity.Legendary;
                        break;
                    case 1 when lootRarity < 100:
                        contestant.armourRarity = InteractivePlayer.Rarity.Set;
                        break;
                }
            }
        }
        private void stayOnAlert(InteractivePlayer contestant)
        {
            if (contestant.alertCooldown == 0)
            {
                contestant.alertCooldown = 4;
                contestant.scenarioLikelihood -= 10;
            }
            else
            {
                int failureChance = _random.Next(10);
                if (failureChance < 6)
                {
                    contestant.alertCooldown = 4;
                    contestant.scenarioLikelihood += 40;
                }
                else
                {
                    contestant.alertCooldown = 4;
                    contestant.scenarioLikelihood -= 10;
                }
            }
        }


        //enhanced options
        private void sabotage(InteractivePlayer contestant, List<InteractivePlayer> contestants)
        {
            if (RNGroll(75))
            {
                int index = _random.Next(contestants.Count);
                while (String.Compare(contestants[index].UserNameWithDiscriminator, contestant.UserNameWithDiscriminator) == 0)
                {
                    index = _random.Next(contestants.Count);
                }

                bool severityFactor = RNGroll(20);
                int debuffSelection = _random.Next(4);
                switch (debuffSelection)
                {
                    case 0 when severityFactor == false:
                        contestants[index].debuff = InteractivePlayer.Debuff.DecreasedItemFind;
                        contestants[index].debuffTimer = 5;
                        break;
                    case 0 when severityFactor == true:
                        contestants[index].debuff = InteractivePlayer.Debuff.SevererlyDecreasedItemFind;
                        contestants[index].debuffTimer = 3;
                        break;
                    case 1 when severityFactor == false:
                        contestants[index].debuff = InteractivePlayer.Debuff.IncreasedDamageTaken;
                        contestants[index].debuffTimer = 5;
                        break;
                    case 1 when severityFactor == true:
                        contestants[index].debuff = InteractivePlayer.Debuff.SeverlyIncreasedDamgeTaken;
                        contestants[index].debuffTimer = 3;
                        break;
                    case 2 when severityFactor == false:
                        contestants[index].debuff = InteractivePlayer.Debuff.DecreasedDuelChance;
                        contestants[index].debuffTimer = 5;
                        break;
                    case 2 when severityFactor == true:
                        contestants[index].debuff = InteractivePlayer.Debuff.SeverlyDecreasedDuelChance;
                        contestants[index].debuffTimer = 3;
                        break;
                    case 3 when severityFactor == false:
                        contestants[index].debuff = InteractivePlayer.Debuff.IncreasedScenarioLikelihood;
                        contestants[index].debuffTimer = 5;
                        break;
                    case 3 when severityFactor == true:
                        contestants[index].debuff = InteractivePlayer.Debuff.SeverlyIncreasedScenarioLikelihood;
                        contestants[index].debuffTimer = 3;
                        break;
                    default:
                        break;
                }
            }
        }
        private void trap(InteractivePlayer contestant)
        {
            if (_random.Next(10) < 7)
            {
                traps.Add(new Trap(contestant));
            }
        }
        private void steal(InteractivePlayer contestant, List<InteractivePlayer> contestants)
        {
            if (RNGroll(30))
            {
                int index = _random.Next(contestants.Count);
                while (String.Compare(contestants[index].UserNameWithDiscriminator, contestant.UserNameWithDiscriminator) == 0)
                {
                    index = _random.Next(contestants.Count);
                }
                int stealType = _random.Next(2);
                switch (stealType)
                {
                    case 0:
                        if (contestants[index].weaponLife > 0)
                        {
                            contestant.weaponRarity = contestants[index].weaponRarity;
                            contestant.weaponLife = 5;
                        }
                        break;
                    case 1:
                        if (contestants[index].armourLife > 0)
                        {
                            contestant.armourRarity = contestants[index].armourRarity;
                            contestant.armourLife = 5;
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private  bool RNGroll(int a)
        {
           
            bool outcome;
            int chance = a * 10;
            int roll = _random.Next(0, 1000);
            if (roll <= chance)
            {
                outcome = true;
            }
            else
            {
                outcome = false;
            }
            return outcome;
        }


        //reset values
        private void reset(InteractivePlayer contestant)
        {
            contestant.scenarioLikelihood = 20;
            contestant.interactiveDecision = InteractivePlayer.InteractiveDecision.DoNothing;
            contestant.enhancedDecision = InteractivePlayer.EnhancedDecision.None;
        }
    }
}
