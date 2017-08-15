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
        private const int DelayValue = 10;
        private readonly Random _random;
        public BHungerGamesV2()
        {
            _random = new Random();
        }
        private int duelCooldown = 4;
        private List<Trap> traps;
        public  List<InteractivePlayer> contestants;
        public  List<int> enhancedIndexList = new List<int>();
        public bool enhancedOptions = false;
        private static readonly Scenario[] Scenarios;
        //private BotGameInstance botGameInstance = new BotGameInstance();
        
        private List<IEmote> emojiListOptions = new List<IEmote> { new Emoji("💰"), new Emoji("❗") };
        private List<IEmote> emojiListEnhancedOptions = new List<IEmote> { new Emoji("💣"), new Emoji("🔫"), new Emoji ("🔧") };
        

        private class Scenario {
            private readonly string _description;
            public int _typeValue;
            public int Delay { get; set; }
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
                Delay = 0;
            }
            public string GetText(string player) //replace {@Px} by player name
            {
                string value = _description?.Replace("{@P1}", player);
                return value;
            }
            public void ReduceDelay()// reduce delay after scenario has been used
            {
                if (Delay > 0)
                    Delay -= 1;
            }

        }

        static BHungerGamesV2()
        {
            Scenarios = new[]
            {
                //new Scenario ("{@P1} has been dealt {_typeValue} HP", Scenario.Type.Damaging, 20),
                //new Scenario ("{@P1} has been killed", Scenario.Type.Lethal, 100),
                //new Scenario ("{@P1} has been healed for {_typeValue} HP", Scenario.Type.Healing, 20),
                //new Scenario ("{@P1} has increased loot find pf {_typeValue} for the next turn", Scenario.Type.LootFind, 10),

                //Miscellaneous stuff
                new Scenario ("{@P1} swam though a pond filled with Blubbler's acidic waste to pursue his journey. (-{_typeValue}HP)", Scenario.Type.Damaging, 20),
                new Scenario ("{@P1} stubbed his toe on a hypershard. (-100000000HP)", Scenario.Type.Lethal, 100000000),
                new Scenario ("{@P1} forgot that this was the INTERACTIVE Hunger Games and stood idle for 5 minutes which was just enough time for a Grampz to come by and smack him with his cane. (-{_typeValue}HP)", Scenario.Type.Damaging, 5),
                new Scenario ("{@P1} saw a Booty fly and tried to catch it. It noticed and, unhappy about that, decided to boop {@P1} on the head.  (-{_typeValue}HP)", Scenario.Type.Damaging, 10),
                new Scenario ("{@P1} encounters Ragnar in his quest for Loot. Ragnar will only let him pass if {@P1} beats him at a game of chess. Sadly, {@P1} forgot  Ragnar was an avit chess player... (-{_typeValue}HP)", Scenario.Type.Damaging, 15),
                new Scenario ("'Lets play hangman' said Zorul. Sadly Zorul never really understood that game. {@P1} got hanged and died.", Scenario.Type.Lethal, 10),
                new Scenario ("{@P1} got caught staring at Kov'Alg's cleavage... (-{_typeValue}HP)", Scenario.Type.Damaging, 15),
                new Scenario ("{@P1} entered R3 and saw Woodbeard talk to Beido, his long distance brother he hasn't seen for months who got addicted to meth. Being an intimate moment, Woodbeard kicked you out of the dungeon. (-{_typeValue}HP)", Scenario.Type.Damaging, 15),
                new Scenario ("{@P1} encountered the legendary Wemmbo in the woods while searching for cover! 'Heal me please!' - 'Get lost kiddo, I'm just a mantis *bzzt bzzt*' (-{_typeValue}HP)", Scenario.Type.Damaging, 10),
                new Scenario ("While adventuring out into the wilderness {@P1} found a horde of Zirg. Attempting to back away {@P1} steps on a twig and causes the Zirg to zerg him. (-{_typeValue}HP) ", Scenario.Type.Damaging, 30),
                new Scenario ("{@P1} finds several piles of bones from previous adventurers. While searching some of the bones starts to shake violently. {@P1} proceeds to get Jacked up. (-{_typeValue}HP)", Scenario.Type.Damaging, 40),
                new Scenario ("{@P1} attempted to venture out in search of treasure.  Sadly the treasure chest was actually Mimzy. (-{_typeValue}HP)", Scenario.Type.Damaging, 20),
                new Scenario ("{@P1} went in search of his old friend Bob whom they had heard lived in a small cottage deep inside the forest. Wait... wrong Bob. (-{_typeValue}HP)", Scenario.Type.Damaging, 25),
                new Scenario ("While trekking across a mountain range in an attempt to get a better view of the arena {@P1} slipped on a loose rock and tumbled back down to the base. Time to start over... (-{_typeValue}HP)", Scenario.Type.Damaging, 5),
                new Scenario ("While searching for shelter in the jungle P1 came across a roaming Trixie. {@P1} runs as fast as possible, but trips on a log...oh shiieeeet. (-{_typeValue})", Scenario.Type.Damaging, 10),
                new Scenario ("{@P1} found a slightly damaged parachute. 'This will surely work'...it didn't. (-{_typeValue}HP)", Scenario.Type.Damaging, 40),
                new Scenario ("{@P1} encountered the almighty Bobodom and tried slaying him for some loot. While fighting {P@1} could hear Bobodom hum 'Can't Touch This' by MC Hammer  (-{_typeValue}HP)", Scenario.Type.Damaging, 15),
                new Scenario ("{@P1} sees a dark figure in the horizon. It is the powerful SSS1. It is said that people who witness his existence see a bright light before their death. {@P1} isn't an exception (-10000HP)", Scenario.Type.Lethal, 10000), //to be edited
                //new Scenario ("{@P1} bumped into Tarri in his journey to slay Grimz. Tarri didn't like that and cock slapped him with her well endowed penis (-{_typeValue}HP)", Scenario.Type.Lethal, 696969),
                new Scenario ("{@P1} ran into a battle with 4 Bargz on his way to slay Woodbeard. They all bombarded him with dozens of cannon shots. (-{_typeValue}HP))", Scenario.Type.Damaging, 35),
                new Scenario ("{@P1} is walking in the woods. He sees Gobby, Olxa, Mimzy AND Bully swinging their sacks onto a poor defenceless Batty. {@P1} tried to interfere, but ended up getting sack-whacked. (-{_typeValue}HP)", Scenario.Type.Damaging, 45),
                new Scenario ("{@P1} was standing on the pier, waiting for a fishing minigame to be implemented. The wood broke under their feet, and they fell into the water. (-{_typeValue}HP)", Scenario.Type.Damaging, 20),
                new Scenario ("{@P1} mistook Capt. Woodbeard for Jack Sparrow and asked him for an autograph. Woodbeard signed whith his cutlass and slaps {@P1} with the book (-{_typeValue}HP)", Scenario.Type.Damaging, 5),
                new Scenario ("{@P1}, while hiding in a tree woke up a group of Batties that startled him. {@P1} fell off the tree (-{_typeValue}HP)", Scenario.Type.Damaging, 15),
                new Scenario ("{@P1} hurt their back carrying all the unnecessary common and rare mats in his bag. (-{_typeValue}HP)", Scenario.Type.Damaging, 5),
                new Scenario ("{@P1} attacked Mimzy while he was sleeping! Inside his chest he found a minor healing potion! (+{_typeValue}HP)", Scenario.Type.Healing, 25),
                new Scenario ("{@P1} challenged Krackers to a tickle fight! He didn't realised Krackers had eight legs... (-{_typeValue}HP)", Scenario.Type.Damaging, 10),
                new Scenario ("{@P1} tried to beat Conan in an arm wrestle. 'Tried' (-{_typeValue})", Scenario.Type.Damaging, 15),
                new Scenario ("{@P1} is exhausted... He is on the verge of dying. But wait! A wild HP shrine appears! (-{_typeValue})", Scenario.Type.Healing, 100),
                new Scenario ("{@P1} equipped Epic Speed Kick to reach loot faster! Sadly he didn't tie his laces properly, tripped and fell on his face *ouch* (-{_typeValue}hP)", Scenario.Type.Damaging, 25),
                new Scenario ("{@P1} sees a Shrump and tried to bribe him. tsk tsk tsk... Shrump can't be bribed! {@P1} got bribed instead and forced to serve Shrump. (-{_typeValue}HP)", Scenario.Type.Damaging, 15),
                new Scenario ("Feeling thirsty, {@P1} ventured in Quirell's fortress for water. He found Juice instead who promptly attempted to empale him. (-{_typeValue}HP)", Scenario.Type.Damaging, 30),
                new Scenario ("In his quest, {@P1} found a sad Trixie sat on a rock. {@P1} tried to give it a hug but Trixie couldn't hug back due to its small arms. Filled with rage, Trixie chomped {@P1}'s arm off (-{_typeValue}HP)", Scenario.Type.Damaging, 65),
				new Scenario ("{@P1} found Zayu cheating on his body pillow with an actual woman! Zayu made sure {@P1} couldn't see anything anymore. (-{_typeValue}HP)\n", Scenario.Type.Damaging, 30),
				new Scenario ("'Nice legs you got there, Woodbea-errr... legendaries, nice legendaries' said {@P1}. Woodbeard proceeded to plunder {@P1}'s booty (-{_typeValue}HP)", Scenario.Type.Damaging, 25),
				new Scenario ("{@P1} sneaked into Warty's dungeon looking for the Wemmbo schematic. Sadly {@P1} encountered a fleet of Zammies haeding towards him (-{_typeValue}HP)", Scenario.Type.Damaging, 10),
				new Scenario ("While avoiding the other survivors, {@P1} unknowingly entered Remruade's hunting grounds. Remruade shot an arrow towards {@P1}. *Thunk*. {@P1} takes an arrow to the knee!  (-{_typeValue}HP)\" _typeValue = 15} (-{_typeValue}hP)", Scenario.Type.Damaging, 10),
				new Scenario ("{@P1} found Blubber's mating grounds. Many Blubbies (bably Blubbers) start rudshing towards {@P1} and nearly suffocate him to death (-{_typeValue}HP)", Scenario.Type.Damaging, 65),
                new Scenario ("{@P1} imagined a fusion in between Gemm and Conan. In his deep thinking, a wild Tubbo appeared and kicked him in the groin. (-{_typeValue}HP)", Scenario.Type.Damaging, 15),
				new Scenario ("A rock fell onto {@P1}'s head. Wait~ what? But it already happened before! HG is rigged!! (-{_typeValue}HP)", Scenario.Type.Lethal, 100),
				new Scenario ("{@P1} is on his way to defeat the mighty King Dina. HP shrine available, familiars not potted, what could go wrong? Dina got slayed but at the cost of {@P1} left arm (-{_typeValue}HP)", Scenario.Type.Damaging, 80),
                new Scenario ("{@P1} found the Legendary B.I.T. Chain! It is guarded by the mighty Kaleido. On his attempt it to steal it, {@P1} bumped into a Rolace that tried slaying him (-{_typeValue}HP)", Scenario.Type.Damaging, 30),
               
                //pet related
                new Scenario ("{@P1} sees a flock of legendary Nemos feasting on a Rexxie carcass. Those things look deadly. *crack* {@P1} stepped on a twig. All Nemos started flying towards the sound. {@P1} managed to escape  with minor bruises. (-{_typeValue}HP)", Scenario.Type.Damaging, 15),
				new Scenario ("{@P1} found a Legendary Nerder. It is said no one likes them, that they're too selfish. But this Nerder looked different. Argh, {@P1}, how can he be fooled like this. Nerder proceeded to rob {@P1}  (-{_typeValue}HP)", Scenario.Type.Damaging, 20),
				new Scenario ("{@P1} is blessed with Gemmi's great healing! (+{_typeValue})", Scenario.Type.Healing, 15),
				new Scenario ("{@P1} was heading back towards B.I.T. Town when he bumped into a lone Sudz. They spent the evening together. Drunken {@P1} tripped on stairs a hurt his head (-{_typeValue})", Scenario.Type.Damaging, 20),
                new Scenario ("Even in the darkest of times, light can be seen if you look well enough. {@P1} sees a dim orange light in the horizon. It is the Legendary Crem! {@P1} is granted an immense revitalising heal. (+{_typeValue}HP)", Scenario.Type.Healing, 40),
				new Scenario ("{@P1} (-{_typeValue})", Scenario.Type.Damaging, 10),
                new Scenario ("{@P1} (-{_typeValue})", Scenario.Type.Damaging, 10),
                new Scenario ("{@P1} (-{_typeValue})", Scenario.Type.Damaging, 10),


                //material related
                new Scenario ("{@P1} found a Doubloon on the floor! But Bully saw this and knocked out {@P1} to steal it. (-{_typeValue})", Scenario.Type.Damaging, 30),
				new Scenario ("After many miles travelled, {@P1} encounters his first Hypershard. Tears start dripping on the rare crystal has {@P1} is filled with relief. But wait! He forgot Hypershards dissolved in water. Filled with anger, {@P1} slammed himself on a tree. (-{_typeValue})", Scenario.Type.Damaging, 25),
				new Scenario ("{@P1} didn't realise he used all his rare mats on rare enchants reroll. {@P1} facepalmed himself so hard, he lost {_typeValue}HP", Scenario.Type.Damaging, 10),
				new Scenario ("{@P1} (-{_typeValue})", Scenario.Type.Damaging, 10),
				new Scenario ("{@P1} (-{_typeValue})", Scenario.Type.Damaging, 10),
                new Scenario ("{@P1} (-{_typeValue})", Scenario.Type.Damaging, 10),
                new Scenario ("{@P1} (-{_typeValue})", Scenario.Type.Damaging, 10),
                new Scenario ("{@P1} (-{_typeValue})", Scenario.Type.Damaging, 10),
                new Scenario ("{@P1} (-{_typeValue})", Scenario.Type.Damaging, 10),
				
                //Notorious players related
                new Scenario ("{@P1}, on his journey to become the best BIT Hero, thought about all the past legends. Blasian, Zim, Leg0Lars.. how he wished he was like them. He also ended up wishing he had paid more attention, but instead ended up walking right towards a raging Tubbo. (-{_typeValue})", Scenario.Type.Damaging, 15),
				new Scenario ("{@P1} (-{_typeValue})", Scenario.Type.Damaging, 10),
				new Scenario ("{@P1} (-{_typeValue})", Scenario.Type.Damaging, 10),
				new Scenario ("{@P1} (-{_typeValue})", Scenario.Type.Damaging, 10),

                //fam related
                new Scenario ("{@P1} (-{_typeValue})", Scenario.Type.Damaging, 10),
                new Scenario ("{@P1} (-{_typeValue})", Scenario.Type.Damaging, 10),
                new Scenario ("{@P1} (-{_typeValue})", Scenario.Type.Damaging, 10),
                new Scenario ("{@P1} (-{_typeValue})", Scenario.Type.Damaging, 10),
                new Scenario ("{@P1} (-{_typeValue})", Scenario.Type.Damaging, 10),
                new Scenario ("{@P1} (-{_typeValue})", Scenario.Type.Damaging, 10),
                new Scenario ("{@P1} (-{_typeValue})", Scenario.Type.Damaging, 10),
                new Scenario ("{@P1} (-{_typeValue})", Scenario.Type.Damaging, 10),


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
        private Scenario GetScenario( ref int randIndex) //get a scenario
        {
            while (true)
            {
                randIndex = _random.Next(Scenarios.Length);
                if (Scenarios[randIndex].Delay <= 0)
                {
                    Scenarios[randIndex].Delay = DelayValue;
                    return Scenarios[randIndex];
                }
            }
        }


        public void Run(int numWinners, List<InteractivePlayer> contestantsTransfer, Action<string, string> showMessageAction, Func<bool> cannelGame, Action<List<IEmote>> AddReaction, int maxPlayers = 0)
        {
            TimeSpan delayBetweenCycles = new TimeSpan(0, 0, 0, 20);
            TimeSpan delayAfterOptions = new TimeSpan(0, 0, 0, 20);
            TimeSpan delayAfterScenarios = new TimeSpan(0, 0, 0, 20);
            int day = 0;
            int night = 0;
            int scenarioToBeExecuted;
            int index;
            contestants = contestantsTransfer;
            StringBuilder sb = new StringBuilder(2000);
            StringBuilder sbLoot = new StringBuilder(2000);
            traps = new List<Trap>();
            List<Trap> trapsToBeRemoved = new List<Trap>();

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
                List<int> scenarioImmune = new List<int>();
                int startingContestantCount = contestants.Count;
                scenarioToBeExecuted = startingContestantCount / 4;
                if (scenarioToBeExecuted < 1)
                {
                    scenarioToBeExecuted = 1;
                }
                int playerToEnhance = contestants.Count / 4;
                if (playerToEnhance <= 0)
                {
                    playerToEnhance = 1;
                }
                // Select Enhanced players
                
                
                showMessageAction($"\n Day**{day}**\nYou have 30 seconds to input your decision\n"
                    + $" You may select :moneybag: to Loot or :exclamation: to Stay On Alert! If you do NOT select a reaction, you will Do Nothing.", null);
                AddReaction(emojiListOptions);
                Thread.Sleep(delayAfterOptions);

                //sbLoot.Append("People that successfully dropped loot: \n\n");
                foreach (InteractivePlayer contestant in contestants)
                {
                    switch (contestant.interactiveDecision)
                    {
                        case InteractivePlayer.InteractiveDecision.Loot:
                            loot(contestant, ref sbLoot);
                            break;
                        case InteractivePlayer.InteractiveDecision.StayOnAlert:
                            stayOnAlert(contestant, ref sb);
                            break;
                        default:
                            //do nothing
                            break;
                    }
                }
                showMessageAction("" + sbLoot + sb, null);
                sbLoot.Clear();
                sb.Clear();

                //enhanced
                enhancedOptions = true;
                sb.Append("Enhanced Decisions have been attributed to:\n");
                enhancedIndexList.Add(_random.Next(contestants.Count));
                playerToEnhance--;
                while (playerToEnhance != 0)
                {
                    int enhancedIndexCheck = _random.Next(contestants.Count);
                    if (enhancedIndexList.Contains(enhancedIndexCheck))
                    {
                        enhancedIndexCheck = _random.Next(contestants.Count);
                    }
                    else
                    {
                        enhancedIndexList.Add(enhancedIndexCheck);
                        playerToEnhance--;
                    }
                }
                foreach (int enhanced in enhancedIndexList)
                {
                    sb.Append($"<{contestants[enhanced].NickName}>\n");
                }
                showMessageAction(sb +"You have 30 seconds to input your decision\n"
                    + $"You may select :bomb: to Make A Trap, :gun: To Steal or :wrench: To Sabotage! If you do NOT select a reaction, you will Do Nothing.\n", null);
                AddReaction(emojiListEnhancedOptions);
                //make bot react to prevent players from searching emojis
                sb.Clear();
                Thread.Sleep(delayAfterOptions);
                

                foreach (InteractivePlayer contestant in contestants)
                {
                    switch (contestant.enhancedDecision)
                    {
                        case InteractivePlayer.EnhancedDecision.Sabotage:
                            sabotage(contestant, contestants, ref sb);
                            break;
                        case InteractivePlayer.EnhancedDecision.Steal:
                            steal(contestant, contestants, ref sb);
                            break;
                        case InteractivePlayer.EnhancedDecision.MakeATrap:
                            trap(contestant, ref sb);
                            break;
                        default:
                            //player not selected for enhanced options
                            break;
                    }
                }
                enhancedOptions = false;
                showMessageAction("" + sb, null);
                sb.Clear();

                    //night cycle
                    night++;
                while (scenarioToBeExecuted != 0) //scenarios
                {
                    string selectedPlayer;
                    index = _random.Next(contestants.Count);
                    if (scenarioImmune.Contains(index))
                    {
                        continue;
                    }
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
                        scenarioImmune.Add(index);
                        scenarioToBeExecuted--;
                        selectedPlayer = "<" + contestants[index].NickName + ">";

                        //scenario method 
                        int scenarioIndex = 0;
                        sb.Append(GetScenario(ref scenarioIndex).GetText(selectedPlayer)).Append("\n\n");
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
                foreach (Trap trap in traps)
                {
                    if (RNGroll(15))
                    {
                        index = _random.Next(contestants.Count);
                        while (trap.trapID == contestants[index].UserNameWithDiscriminator)
                        {
                            index = _random.Next(contestants.Count);
                        }
                        sb.Append($"<{contestants[index].NickName}> fell into a trap damaging him for {trap.damage}HP\n\n");
                        contestants[index].hp -= trap.damage;

                        if (contestants[index].hp < 0)
                        {
                            sb.Append($"<{contestants[index].NickName}> died from the trap.\n\n");
                            contestants.RemoveAt(index);
                        }
                        trapsToBeRemoved.Add(trap);
                    }
                }
                traps = traps.Except(trapsToBeRemoved).ToList();
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
                foreach (Scenario scenario in Scenarios)
                {
                    scenario.ReduceDelay();
                }
                showMessageAction($"\nNight**{night}** <{startingContestantCount}> players remaining\n\n" + sb, null);
                scenarioImmune.Clear();
                enhancedIndexList.Clear();
                sb.Clear();
                Thread.Sleep(delayBetweenCycles);

                if (cannelGame())
                    return;

            }
        }

        //duel method

        void duel(List <InteractivePlayer> contestants,ref StringBuilder sb)
        {
            TimeSpan duelTimeSpan = new TimeSpan(0, 0, 10);
            int duelChance = 50;
            int duelist_1, duelist_2;
            duelist_1 = _random.Next(contestants.Count);
            duelist_2 = _random.Next(contestants.Count);
            while (duelist_1 == duelist_2)
            {
                duelist_2 = _random.Next(contestants.Count);
            }
            sb.Append($"A Duel started in betwwen <{contestants[duelist_1].NickName}> and <{contestants[duelist_2].NickName}>\n\n");
            if (contestants[duelist_1].weaponLife > 0)
            {
                contestants[duelist_1].weaponLife--;
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
                contestants[duelist_2].weaponLife--;
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
                sb.Append($"<{contestants[duelist_1].NickName}> won the duel and slayed <{contestants[duelist_2].NickName}>\n\n");
                contestants.RemoveAt(duelist_2);
            }
            else
            {
                sb.Append($"<{contestants[duelist_2].NickName}> won the duel and slayed <{contestants[duelist_1].NickName}>\n\n");
                contestants.RemoveAt(duelist_1);
            }
        }
        //interactive options
        private void loot(InteractivePlayer contestant, ref StringBuilder sbLoot)
        {
            contestant.scenarioLikelihood += 10;
            int lootChance = 60;
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
                //lootRarity = 55;
                switch (lootType)
                {
                    case 0 when lootRarity < 60:
                        contestant.weaponRarity = InteractivePlayer.Rarity.Common;
                        sbLoot.Append($"<{contestant.NickName}> has obtained a Common weapon!\n");
                        break;
                    case 0 when lootRarity < 87:
                        contestant.weaponRarity = InteractivePlayer.Rarity.Rare;
                        sbLoot.Append($"<{contestant.NickName}> has obtained a Rare weapon!\n");
                        break;
                    case 0 when lootRarity < 97:
                        contestant.weaponRarity = InteractivePlayer.Rarity.Epic;
                        sbLoot.Append($"<{contestant.NickName}> has obtained a Epic weapon!\n");
                        break;
                    case 0 when lootRarity < 99:
                        contestant.weaponRarity = InteractivePlayer.Rarity.Legendary;
                        sbLoot.Append($"<{contestant.NickName}> has obtained a Legendary weapon!\n");
                        break;
                    case 0 when lootRarity < 100:
                        contestant.weaponRarity = InteractivePlayer.Rarity.Set;
                        sbLoot.Append($"<{contestant.NickName}> has obtained a Set weapon!\n");
                        break;
                    case 1 when lootRarity < 60:
                        contestant.armourRarity = InteractivePlayer.Rarity.Common;
                        sbLoot.Append($"<{contestant.NickName}> has obtained a Common body!\n");
                        break;
                    case 1 when lootRarity < 87:
                        contestant.armourRarity = InteractivePlayer.Rarity.Rare;
                        sbLoot.Append($"<{contestant.NickName}> has obtained a Rare body!\n");
                        break;
                    case 1 when lootRarity < 97:
                        contestant.armourRarity = InteractivePlayer.Rarity.Epic;
                        sbLoot.Append($"<{contestant.NickName}> has obtained a Epic body!\n");
                        break;
                    case 1 when lootRarity < 99:
                        contestant.armourRarity = InteractivePlayer.Rarity.Legendary;
                        sbLoot.Append($"<{contestant.NickName}> has obtained a Legendary body!\n");
                        break;
                    case 1 when lootRarity < 100:
                        contestant.armourRarity = InteractivePlayer.Rarity.Set;
                        sbLoot.Append($"<{contestant.NickName}> has obtained a Set body!\n");
                        break;
                }
            }
        }
        private void stayOnAlert(InteractivePlayer contestant,  ref StringBuilder sb)
        {
            if (contestant.alertCooldown == 0)
            {
                contestant.alertCooldown = 4;
                contestant.scenarioLikelihood -= 10;
                sb.Append($"<{contestant.NickName}> successfully staryed On Alert. -10% Scenario likelihood. \n");
            }
            else
            {
                int failureChance = _random.Next(10);
                if (failureChance < 6)
                {
                    contestant.alertCooldown = 4;
                    contestant.scenarioLikelihood += 40;
                    sb.Append($"<{contestant.NickName}> tried to Stay On Alert but fell in a deep sleep. +40% Scenario likelihood. \n");
                }
                else
                {
                    contestant.alertCooldown = 4;
                    contestant.scenarioLikelihood -= 10;
                    sb.Append($"<{contestant.NickName}> successfully staryed On Alert. -10% Scenario likelihood. \n");

                }
            }
        }


        //enhanced options
        private void sabotage(InteractivePlayer contestant, List<InteractivePlayer> contestants, ref StringBuilder sb)
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
                sb.Append($"<{contestant.NickName}> has sabotaged <{contestants[index].NickName}> by giving him a {contestants[index].debuff} debuff for {contestants[index].debuffTimer} turns!\n");

            }
            else
            {
                sb.Append($"<{contestant.NickName}> failed to sabotage someone... U trash or wut?");
            }
        }
        private void trap(InteractivePlayer contestant, ref StringBuilder sb)
        {
            Logger.Log("accessed trap method");
            if (_random.Next(10) < 12)
            {
                Logger.Log("trap made");
                traps.Add(new Trap(contestant));
                sb.Append($"<{contestant.NickName}> made a Trap!\n");

            }
            else
            {
                sb.Append($"<{contestant.NickName}> has failed to make a trap.\n");
            }
        }
        private void steal(InteractivePlayer contestant, List<InteractivePlayer> contestants, ref StringBuilder sb)
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
                            contestants[index].weaponLife = 0;

                            sb.Append($"<{contestant.NickName}> stole <{contestants[index].NickName}>'s {contestants[index].weaponRarity} Weapon!\n");
                        }
                        break;
                    case 1:
                        if (contestants[index].armourLife > 0)
                        {
                            contestant.armourRarity = contestants[index].armourRarity;
                            contestant.armourLife = 5;
                            contestants[index].armourLife = 0;
                            sb.Append($"<{contestant.NickName}> stole <{contestants[index].NickName}>'s {contestants[index].armourRarity} Armour!\n");
                        }
                        break;
                    default:
                        break;
                }
            }
            else
            {
                sb.Append($"<{contestant.NickName}> failed to steal something... git gud ¯_(ツ)_/¯");
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
