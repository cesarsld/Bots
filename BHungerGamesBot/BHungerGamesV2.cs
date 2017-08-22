using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Linq;
using Discord;

namespace BHungerGaemsBot
{
    public class BHungerGamesV2
    {
        private const int DelayValue = 80;
        private readonly Random _random;
        private volatile bool _ignoreReactions = true;
        private int _duelCooldown = 4;
        private List<Trap> _traps;
        public  List<InteractivePlayer> Contestants;
        public  List<int> EnhancedIndexList = new List<int>();
        public bool EnhancedOptions;
        private static readonly Scenario[] Scenarios;

        private int CommonChance;
        private int RareChance;
        private int EpicChance;
        private int LegendaryChance;
        private int SetChance = 100;
        private int failToLoot;

        private readonly Dictionary<string, int[]> DangerToLoot = new Dictionary<string, int[]>()
        {//                 FailChance Co  Ra  Ep  Le  Se
            { "Safe",      new int[] { 0, 60, 87, 97, 99, 100} },
            { "Unsafe",    new int[] {10, 57, 82, 94, 98, 100} },
            { "Dangerous", new int[] {25, 40, 70, 85, 95, 100} },
            { "Deadly",    new int[] {50,  0, 40, 70, 90, 100} },
        };

        private readonly List<IEmote> _emojiListOptions = new List<IEmote> { new Emoji("💰"), new Emoji("❗"), new Emoji("⚔") };
        private readonly List<IEmote> _emojiListEnhancedOptions = new List<IEmote> { new Emoji("💣"), new Emoji("🔫"), new Emoji ("🔧") };
        private static readonly int[] ShowPlayersWhenCountEqual = {10, 5, 2, 0 };
        private List<InteractivePlayer> DuelImmune = new List<InteractivePlayer>();

        public class Trap
        {
            public int Damage;
            public ulong TrapUserID;

            public Trap(InteractivePlayer contestant, Random random)
            {
                Damage = 5 * random.Next(2, 9);
                TrapUserID = contestant.UserId;
            }
        }

        private class Scenario
        {
            private readonly string _description;
            public readonly int TypeValue;
            public readonly ScenarioType Type;
            public int Delay { get; set; }

            public Scenario(string description, ScenarioType type, int typeValue) //creates instance of a scenario, defining its description and players needed
            {
                _description = description;
                Type = type;
                TypeValue = typeValue;
                if (type != ScenarioType.Lethal)
                {
                    _description = _description.Replace("{_typeValue}", TypeValue.ToString());
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
                //new Scenario ("{@P1} has been dealt {_typeValue} HP", ScenarioType.Damaging, 20),
                //new Scenario ("{@P1} has been killed", ScenarioType.Lethal, 100),
                //new Scenario ("{@P1} has been healed for {_typeValue} HP", ScenarioType.Healing, 20),
                //new Scenario ("{@P1} has increased loot find pf {_typeValue} for the next turn", ScenarioType.LootFind, 10),

                //Miscellaneous stuff
                new Scenario ("{@P1} swam though a pond filled with Blubbler's acidic waste to pursue their journey. (-{_typeValue}HP)", ScenarioType.Damaging, 20),
                new Scenario ("{@P1} stubbed their toe on a hypershard. (-100000000HP)", ScenarioType.Lethal, 100000000),
                new Scenario ("{@P1} forgot that this was the INTERACTIVE Hunger Games and stood idle for 5 minutes which was just enough time for a Grampz to come by and smack them with his cane. (-{_typeValue}HP)", ScenarioType.Damaging, 5),
                new Scenario ("{@P1} saw a Booty fly and tried to catch it. It noticed and, unhappy about that, decided to boop {@P1} on the head.  (-{_typeValue}HP)", ScenarioType.Damaging, 10),
                new Scenario ("{@P1} encounters Ragnar in his quest for Loot. Ragnar will only let them pass if {@P1} beats them at a game of chess. Sadly, {@P1} forgot  Ragnar was an avid chess player... (-{_typeValue}HP)", ScenarioType.Damaging, 15),
                new Scenario ("'Lets play hangman' said Zorul. Sadly Zorul never really understood that game. {@P1} got hanged and died.", ScenarioType.Lethal, 100),
                new Scenario ("{@P1} got caught staring at Kov'Alg's cleavage... (-{_typeValue}HP)", ScenarioType.Damaging, 15),
                //new Scenario ("{@P1} entered R3 and saw Woodbeard talk to Beido, his long distance brother he hasn't seen for months who got addicted to meth. Being an intimate moment, Woodbeard kicked you out of the dungeon. (-{_typeValue}HP)", ScenarioType.Damaging, 15),
                new Scenario ("{@P1} encountered the legendary Wemmbo in the woods while searching for cover! 'Heal me please!' - 'Get lost kiddo, I'm just a mantis *bzzt bzzt*' (-{_typeValue}HP)", ScenarioType.Damaging, 10),
                new Scenario ("While adventuring out into the wilderness {@P1} found a horde of Zirg. Attempting to back away {@P1} steps on a twig and causes the Zirg to zerg them. (-{_typeValue}HP) ", ScenarioType.Damaging, 30),
                new Scenario ("{@P1} finds several piles of bones from previous adventurers. While searching some of the bones starts to shake violently. {@P1} proceeds to get Jacked up. (-{_typeValue}HP)", ScenarioType.Damaging, 40),
                new Scenario ("{@P1} attempted to venture out in search of treasure. Sadly the treasure chest was actually Mimzy. (-{_typeValue}HP)", ScenarioType.Damaging, 20),
                new Scenario ("{@P1} went in search of his old friend Bob whom they had heard lived in a small cottage deep inside the forest. Wait... wrong Bob. (-{_typeValue}HP)", ScenarioType.Damaging, 25),
                new Scenario ("While trekking across a mountain range in an attempt to get a better view of the arena {@P1} slipped on a loose rock and tumbled back down to the base. Time to start over... (-{_typeValue}HP)", ScenarioType.Damaging, 5),
                new Scenario ("While searching for shelter in the jungle {@P1} came across a roaming Trixie. {@P1} ran as fast as possible, but tripped on a log...oh shiieeeet. (-{_typeValue}HP)", ScenarioType.Damaging, 25),
                new Scenario ("{@P1} found a slightly damaged parachute. 'This will surely work'...it didn't. (-{_typeValue}HP)", ScenarioType.Damaging, 40),
                new Scenario ("{@P1} encountered the almighty Bobodom and tried slaying him for some loot. While fighting {P@1} could hear Bobodom hum 'Can't Touch This' by MC Hammer  (-{_typeValue}HP)", ScenarioType.Damaging, 15),
                new Scenario ("{@P1} sees a dark figure in the horizon. It is the powerful SSS1. It is said that people who witness his existence see a bright light before their death. {@P1} isn't an exception (-10000HP)", ScenarioType.Lethal, 10000), //to be edited
                //new Scenario ("{@P1} bumped into Tarri in his journey to slay Grimz. Tarri didn't like that and cock slapped him with her well endowed penis (-{_typeValue}HP)", ScenarioType.Lethal, 696969),
                new Scenario ("{@P1} ran into a battle with 4 Bargz on his way to slay Woodbeard. They all bombarded them with dozens of cannon shots. (-{_typeValue}HP))", ScenarioType.Damaging, 35),
                new Scenario ("{@P1} is walking in the woods. They sees Gobby, Olxa, Mimzy AND Bully swinging their sacks onto a poor defenceless Batty. {@P1} tried to interfere, but ended up getting sack-whacked. (-{_typeValue}HP)", ScenarioType.Damaging, 45),
                new Scenario ("{@P1} was standing on the pier, waiting for a fishing minigame to be implemented. The wood broke under their feet, and they fell into the water. (-{_typeValue}HP)", ScenarioType.Damaging, 20),
                new Scenario ("{@P1} mistook Capt. Woodbeard for Jack Sparrow and asked him for an autograph. Woodbeard signed whith his cutlass and slapped {@P1} with the book (-{_typeValue}HP)", ScenarioType.Damaging, 5),
                new Scenario ("{@P1}, while hiding in a tree woke up a group of Batties that startled them. {@P1} fell off the tree (-{_typeValue}HP)", ScenarioType.Damaging, 15),
                new Scenario ("{@P1} hurt their back carrying all the unnecessary common and rare mats in his bag. (-{_typeValue}HP)", ScenarioType.Damaging, 5),
                new Scenario ("{@P1} attacked Mimzy while he was sleeping! Inside his chest they found a minor healing potion! (+{_typeValue}HP)", ScenarioType.Healing, 25),
                new Scenario ("{@P1} challenged Krackers to a tickle fight! They didn't realised Krackers had eight legs... (-{_typeValue}HP)", ScenarioType.Damaging, 10),
                new Scenario ("{@P1} tried to beat Conan in an arm wrestle. 'Tried' (-{_typeValue}HP)", ScenarioType.Damaging, 15),
                new Scenario ("{@P1} is exhausted... They were on the verge of dying. But wait! A wild HP shrine appears! (+{_typeValue})", ScenarioType.Healing, 100),
                new Scenario ("{@P1} equipped Epic Speed Kick to reach loot faster! Sadly they didn't tie his laces properly, tripped and fell on his face *ouch* (-{_typeValue}hP)", ScenarioType.Damaging, 25),
                new Scenario ("{@P1} sees a Shrump and tried to bribe him. tsk tsk tsk... Shrump can't be bribed! {@P1} got bribed instead and forced to serve Shrump. (-{_typeValue}HP)", ScenarioType.Damaging, 15),
                new Scenario ("Feeling thirsty, {@P1} ventured in Quirell's fortress for water. They found Juice instead who promptly attempted to empale them. (-{_typeValue}HP)", ScenarioType.Damaging, 30),
                new Scenario ("In his quest, {@P1} found a sad Trixie sat on a rock. {@P1} tried to give it a hug but Trixie couldn't hug back due to its small arms. Filled with rage, Trixie chomped {@P1}'s arm off (-{_typeValue}HP)", ScenarioType.Damaging, 65),
                new Scenario ("{@P1} found Zayu cheating on his body pillow with an actual woman! Zayu made sure {@P1} couldn't see anything anymore. (-{_typeValue}HP)", ScenarioType.Damaging, 30),
                new Scenario ("'Nice legs you got there, Woodbea-errr... legendaries, nice legendaries' said {@P1}. Woodbeard proceeded to plunder {@P1}'s booty (-{_typeValue}HP)", ScenarioType.Damaging, 25),
                new Scenario ("{@P1} sneaked into Warty's dungeon looking for the Wemmbo schematic. Sadly {@P1} encountered a fleet of Zammies haeding towards them (-{_typeValue}HP)", ScenarioType.Damaging, 10),
                new Scenario ("While avoiding the other survivors, {@P1} unknowingly entered Remruade's hunting grounds. Remruade shot an arrow towards {@P1}. *Thunk*. {@P1} takes an arrow to the knee!  (-{_typeValue}HP)\" _typeValue = 15} (-{_typeValue}hP)", ScenarioType.Damaging, 10),
                new Scenario ("{@P1} found Blubber's mating grounds. Many Blubbies (baby Blubbers) started rushing towards {@P1} and nearly suffocated them to death (-{_typeValue}HP)", ScenarioType.Damaging, 65),
                new Scenario ("{@P1} imagined a fusion in between Gemm and Conan. In his deep thinking, a wild Tubbo appeared and kicked them in the groin. (-{_typeValue}HP)", ScenarioType.Damaging, 15),
                new Scenario ("A rock fell onto {@P1}'s head. Wait~ what? But it already happened before! HG is rigged!! (-100HP)", ScenarioType.Lethal, 100),
                new Scenario ("{@P1} is on his way to defeat the mighty King Dina. HP shrine available, familiars not potted, what could go wrong? Dina got slain but at the cost of {@P1} left arm (-{_typeValue}HP)", ScenarioType.Damaging, 80),
                new Scenario ("{@P1} found the Legendary B.I.T. Chain! It is guarded by the mighty Kaleido. On his attempt it to steal it, {@P1} bumped into a Rolace that tried slaying them (-{_typeValue}HP)", ScenarioType.Damaging, 30),
               
               
               
               
               
                //pet related
                new Scenario ("{@P1} sees a flock of legendary Nemos feasting on a Rexxie carcass. Those things look deadly. *crack* {@P1} stepped on a twig. All Nemos started flying towards the sound. {@P1} managed to escape  with minor bruises. (-{_typeValue}HP)", ScenarioType.Damaging, 15),
                new Scenario ("{@P1} found a Legendary Nerder. It is said no one likes them, that they're too selfish. But this Nerder looked different. Argh, {@P1}, how can they be fooled like this. Nerder proceeded to rob {@P1}  (-{_typeValue}HP)", ScenarioType.Damaging, 20),
                new Scenario ("{@P1} is blessed with Gemmi's great healing! (+{_typeValue}HP)", ScenarioType.Healing, 15),
                new Scenario ("{@P1} was heading back towards B.I.T. Town when they bumped into a lone Sudz. They spent the evening together. Drunk, {@P1} tripped on stairs a hurt his head (-{_typeValue}HP)", ScenarioType.Damaging, 20),
                new Scenario ("Even in the darkest of times, light can be seen if you look well enough. {@P1} sees a dim orange light in the horizon. It is the Legendary Crem! {@P1} is granted an immense revitalising heal. (+{_typeValue}HP)", ScenarioType.Healing, 40),
                new Scenario ("{@P1} encounters the Legendary Nelson! A majestic creature. His bite is eve moreso majestic and painful(-{_typeValue}HP)", ScenarioType.Damaging, 15),
                new Scenario ("{@P1} was about to eat a BITburger when they realised it was in fact Boiguh. Unhappy, Boiguh headbutted them. No one eats Boiguh. (-{_typeValue}HP)", ScenarioType.Damaging, 25),
                new Scenario ("{@P1}'s melvin champ slipped off their stick and double kicked {@P1} in the face (-{_typeValue}HP)", ScenarioType.Damaging, 25),
                //new Scenario ("{@P1} (-{_typeValue})", ScenarioType.Damaging, 10),
                //new Scenario ("{@P1} (-{_typeValue})", ScenarioType.Damaging, 10),
                //new Scenario ("{@P1} (-{_typeValue})", ScenarioType.Damaging, 10),
                
           

                //material related
                new Scenario ("{@P1} found a Doubloon on the floor! But Bully saw this and knocked out {@P1} to steal it. (-{_typeValue}HP)", ScenarioType.Damaging, 30),
                new Scenario ("After many miles travelled, {@P1} encounters his first Hypershard. Tears start dripping on the rare crystal as {@P1} is filled with relief. But wait! They forgot Hypershards dissolved in water. Filled with anger, {@P1} slammed himself on a tree. (-{_typeValue}HP)", ScenarioType.Damaging, 25),
                new Scenario ("{@P1} didn't realise they used all his rare mats on rare enchants reroll. {@P1} facepalmed themself so hard, they lost {_typeValue}HP", ScenarioType.Damaging, 10),
                new Scenario ("{@P1} was lookin' for dem leg sneks in Z5D3. Sadly, {@P1} only found dust and a Brute charging at them. (-{_typeValue}HP)", ScenarioType.Damaging, 20),
                new Scenario ("{@P1}, after a long search of riches, found a Harmony Orb! A valuable ressource. It would be his if there wasn't a Bluz guarding it. (-{_typeValue}HP)", ScenarioType.Damaging, 20),
                new Scenario ("{@P1}, while pausing briefly to catch their breath, saw movement in the bushes. An enchanted snek! {@P1} sprung headlong into the brambles, seizing the alarmed serpent, before quickly discovering that it was just a regular (but incredibly venomous and now angry) one.  (-{_typeValue}HP)", ScenarioType.Damaging, 10),
                new Scenario ("{@P1} was running through a forest glade when they tripped over a rock, sending their knee 10% more painfully into the ground thanks to their Bushido set. (-{_typeValue}HP)", ScenarioType.Damaging, 22),
                //new Scenario ("{@P1} (-{_typeValue}HP)", ScenarioType.Damaging, 10),
                //new Scenario ("{@P1} (-{_typeValue}HP)", ScenarioType.Damaging, 10),
                
                //Notorious players related
                new Scenario ("{@P1}, on his journey to become the best BIT Hero, thought about all the past legends. Blasian, Zim, Leg0Lars.. how they wished they were like them. They also ended up wishing they had paid more attention, but instead ended up walking right towards a raging Tubbo. (-{_typeValue}HP)", ScenarioType.Damaging, 15),
                new Scenario ("{@P1} laughed at Shadown88's build. Shadown88 laughed too when he Dual crit-empower striked {@P1} for 30386 damage. (-30386HP)", ScenarioType.Lethal, 10),
                //new Scenario ("{@P1} (-{_typeValue}HP)", ScenarioType.Damaging, 10),
                //new Scenario ("{@P1} (-{_typeValue}HP)", ScenarioType.Damaging, 10),

                //fam related
                new Scenario ("{@P1} encountered Prof. Oak. 'Don't come back here until you've completed your Juppiodex!' *kicks {@P1} out of his lab*. (-{_typeValue}HP)", ScenarioType.Damaging, 15),
                new Scenario ("{@P1} went to walk around the beach to fish when an Ives attacked them unexpectedly (-{_typeValue}HP)", ScenarioType.Damaging, 15),
                new Scenario ("{@P1} sees a doubloon in the corner of the cabin! {@P1} walks right in front of Woodbeard who was waiting for them. 'You arrrr mine!'. (-{_typeValue}HP)", ScenarioType.Damaging, 40),
                new Scenario ("{@P1} walked through the Hyper Dimension and got ran over by a herd of Oevor. (-{_typeValue}HP)", ScenarioType.Damaging, 35),
                new Scenario ("{@P1} lost his mind eating some psychedelic Shrump offsprings. they attempted to ride a wild Trixxie while shouting 'Toga! Toga!'.  (-{_typeValue}HP)", ScenarioType.Damaging, 35),
                new Scenario ("{@P1} was apprehensively moving through shadowy woods when they were suddenly startled by the sound of heavy footsteps behind them. Out of desperation, and perhaps a dash of curiosity, {@P1} tried mounting their Driffin like a horse to gallop to a hasty getaway. The Driffin made its displeasure known. Painfully. (-{_typeValue}HP)", ScenarioType.Damaging, 45),
                //new Scenario ("{@P1} (-{_typeValue}HP)", ScenarioType.Damaging, 10),
               // new Scenario ("{@P1} (-{_typeValue}HP)", ScenarioType.Damaging, 10),

                //cosmetic related
                new Scenario ("{@P1} was hiding in the bushes when they overheard other players making fun of their choice in cosmetics. Sometimes, the trauma of the arena leaves no physical mark, but hurts just the same. (-{_typeValue}HP)", ScenarioType.Damaging, 10),

                //HG related
                new Scenario ("{@P1} stepped on a mine. (-{_typeValue}HP)", ScenarioType.Damaging, 25),
                new Scenario ("{@P1} died\n.\n.\n.\njk. The did lose -{_typeValue}HP though.", ScenarioType.Damaging, 10),
                new Scenario ("{@P1} died\n.\n.\n.\njk. The did lose -{_typeValue}HP though.", ScenarioType.Damaging, 25),
                new Scenario ("{@P1} died\n.\n.\n.\njk. The did lose -{_typeValue}HP though.", ScenarioType.Damaging, 15),
                new Scenario ("{@P1} died\n.\n.\n.\njk. The did lose -{_typeValue}HP though.", ScenarioType.Damaging, 20),


               // new Scenario ("{@P1} (-{_typeValue}HP)", ScenarioType.Damaging, 10),
              //  new Scenario ("{@P1} (-{_typeValue}HP)", ScenarioType.Damaging, 10),
             //   new Scenario ("{@P1} (-{_typeValue}HP)", ScenarioType.Damaging, 10),
              //  new Scenario ("{@P1} (-{_typeValue}HP)", ScenarioType.Damaging, 10),
               // new Scenario ("{@P1} (-{_typeValue}HP)", ScenarioType.Damaging, 10),
               // new Scenario ("{@P1} (-{_typeValue}HP)", ScenarioType.Damaging, 10),


            };
        }

        public BHungerGamesV2()
        {
            _random = new Random();
        }

        private Scenario GetScenario() //get a scenario
        {
            while (true)
            {
                int randIndex = _random.Next(Scenarios.Length);
                if (Scenarios[randIndex].Delay <= 0)
                {
                    Scenarios[randIndex].Delay = DelayValue;
                    return Scenarios[randIndex];
                }
            }
        }

        public void Run(int numWinners, List<Player> contestantsTransfer, BotGameInstance.ShowMessageDelegate showMessageDelegate, Func<bool> cannelGame, int maxPlayers = 0)
        {
            TimeSpan delayBetweenCycles = new TimeSpan(0, 0, 0, 25);
            TimeSpan delayAfterOptions = new TimeSpan(0, 0, 0, 15);
            int day = 0;
            int night = 0;
            int showPlayersWhenCountEqualIndex = 0;

            StringBuilder sb = new StringBuilder(2000);
            StringBuilder sbLoot = new StringBuilder(2000);
            _traps = new List<Trap>();
            List<Trap> trapsToBeRemoved = new List<Trap>();
            List<InteractivePlayer> playersToBeRemoved = new List<InteractivePlayer>();

            if (maxPlayers > 0 && contestantsTransfer.Count > maxPlayers)
            {
                int numToRemove = contestantsTransfer.Count - maxPlayers;
                for (int i = 0; i < numToRemove; i++)
                {
                    int randIndex = _random.Next(contestantsTransfer.Count);
                    sb.Append($"<{contestantsTransfer[randIndex].ContestantName}>\t");
                    contestantsTransfer.RemoveAt(randIndex);
                }
                showMessageDelegate("Players killed in the stampede trying to get to the arena:\r\n" + sb);
                sb.Clear();
            }

            Contestants = new List<InteractivePlayer>();
            foreach (Player player in contestantsTransfer)
            {
                InteractivePlayer interactivePlayer = player as InteractivePlayer;
                if (interactivePlayer != null)
                {
                    Contestants.Add(interactivePlayer);
                }
            }

            while (Contestants.Count > numWinners)
            {
                
                // day cycle
                day++;
                List<int> scenarioImmune = new List<int>();
                int startingContestantCount = Contestants.Count;
                var scenarioToBeExecuted = startingContestantCount / 4;
                if (scenarioToBeExecuted < 1)
                {
                    scenarioToBeExecuted = 1;
                }
                int playerToEnhance = Contestants.Count / 4;
                if (playerToEnhance <= 0)
                {
                    playerToEnhance = 1;
                }

                int dangerIndex = _random.Next(4);

                CommonChance = DangerToLoot.ElementAt(dangerIndex).Value.ElementAt(1);
                RareChance = DangerToLoot.ElementAt(dangerIndex).Value.ElementAt(2);
                EpicChance = DangerToLoot.ElementAt(dangerIndex).Value.ElementAt(3);
                LegendaryChance = DangerToLoot.ElementAt(dangerIndex).Value.ElementAt(4);
                //SetChance = DangerToLoot.ElementAt(danger).Value.ElementAt(5);
                failToLoot = DangerToLoot.ElementAt(dangerIndex).Value.ElementAt(0);
                sb.Append($"\nDanger level to look for loot = * {DangerToLoot.ElementAt(dangerIndex).Key} *");
                // Select Enhanced players

                _ignoreReactions = false;
                showMessageDelegate($"\n Day**{day}**\nYou have 30 seconds to input your decision\n"
                    + " You may select <:moneybag:> to Loot, <:exclamation:> to Stay On Alert or <:crossed_swords:> to be immuned to Duels! If you do NOT select a reaction, you will Do Nothing." + sb, null, _emojiListOptions);
                sb.Clear();
                Thread.Sleep(delayAfterOptions);
                _ignoreReactions = true;

                //sbLoot.Append("People that successfully dropped loot: \n\n");
                foreach (InteractivePlayer contestant in Contestants)
                {
                    switch (contestant.InteractiveDecision)
                    {
                        case InteractiveDecision.Loot:
                            Loot(contestant, sbLoot, playersToBeRemoved);
                            break;
                        case InteractiveDecision.StayOnAlert:
                            StayOnAlert(contestant, sb);
                            break;
                        case InteractiveDecision.ImmuneToDuel:
                            DuelImmuneOption(sb, contestant);
                            break;
                    }
                }
                showMessageDelegate("" + sbLoot + sb);
                sbLoot.Clear();
                sb.Clear();

                Contestants = Contestants.Except(playersToBeRemoved).ToList();

                //enhanced
                EnhancedOptions = true;
                sb.Append("Enhanced Decisions have been attributed to:\n");
                EnhancedIndexList.Add(_random.Next(Contestants.Count));
                playerToEnhance--;
                while (playerToEnhance != 0)
                {
                    int enhancedIndexCheck = _random.Next(Contestants.Count);
                    if (EnhancedIndexList.Contains(enhancedIndexCheck) == false)
                    {
                        EnhancedIndexList.Add(enhancedIndexCheck);
                        playerToEnhance--;
                    }
                }
                foreach (int enhanced in EnhancedIndexList)
                {
                    sb.Append($"<{Contestants[enhanced].NickName}>\n");
                }

                _ignoreReactions = false;
                showMessageDelegate(sb +"You have 30 seconds to input your decision\n"
                    + "You may select <:bomb:> to Make A Trap, <:gun:> To Steal or <:wrench:> To Sabotage! If you do NOT select a reaction, you will Do Nothing.\n", null, _emojiListEnhancedOptions);
                //make bot react to prevent players from searching emojis
                sb.Clear();
                Thread.Sleep(delayAfterOptions);
                _ignoreReactions = true;


                foreach (InteractivePlayer contestant in Contestants)
                {
                    switch (contestant.EnhancedDecision)
                    {
                        case EnhancedDecision.Sabotage:
                            Sabotage(contestant, sb);
                            break;
                        case EnhancedDecision.Steal:
                            Steal(contestant, sb);
                            break;
                        case EnhancedDecision.MakeATrap:
                            MakeTrap(contestant, sb);
                            break;
                    }
                }
                EnhancedOptions = false;
                showMessageDelegate(sb.ToString());
                sb.Clear();

                //night cycle
                night++;
                int index;
                while (scenarioToBeExecuted != 0 && Contestants.Count > 2) //scenarios
                {
                    index = _random.Next(Contestants.Count);
                    if (scenarioImmune.Contains(index))
                    {
                        continue;
                    }
                    switch (Contestants[index].Debuff)
                    {
                        case Debuff.IncreasedScenarioLikelihood when Contestants[index].DebuffTimer > 0:
                            Contestants[index].ScenarioLikelihood += 10;
                            Contestants[index].DebuffTimer--;
                            break;
                        case Debuff.SeverlyIncreasedScenarioLikelihood when Contestants[index].DebuffTimer > 0:
                            Contestants[index].ScenarioLikelihood += 10;
                            Contestants[index].DebuffTimer--;
                            break;
                    }
                    if (RngRoll(Contestants[index].ScenarioLikelihood))
                    {
                        scenarioImmune.Add(index);
                        scenarioToBeExecuted--;
                        var selectedPlayer = "<" + Contestants[index].NickName + ">";

                        //scenario method
                        Scenario currentScenario = GetScenario();
                        sb.Append(currentScenario.GetText(selectedPlayer)).Append(" ");
                        switch (currentScenario.Type)
                        {
                            case ScenarioType.Damaging:
                                Contestants[index].Hp -= currentScenario.TypeValue;
                                sb.Append($"Current HP = {Contestants[index].Hp}\n\n");
                                if (Contestants[index].Hp <= 0)
                                {
                                    Contestants.RemoveAt(index);
                                }
                                break;
                            case ScenarioType.Lethal:
                                Contestants.RemoveAt(index);
                                sb.Append("\n\n");
                                break;
                            case ScenarioType.Healing:
                                Contestants[index].Hp += currentScenario.TypeValue;
                                if (Contestants[index].Hp > 100)
                                {
                                    Contestants[index].Hp = 100;
                                }
                                sb.Append($"Current HP = {Contestants[index].Hp}\n\n");
                                break;
                        }
                    }
                }
                foreach (Trap trap in _traps)
                {
                    if (RngRoll(15))
                    {
                        index = _random.Next(Contestants.Count);
                        while (trap.TrapUserID == Contestants[index].UserId)
                        {
                            index = _random.Next(Contestants.Count);
                        }
                        Contestants[index].Hp -= trap.Damage;
                        sb.Append($"<{Contestants[index].NickName}> fell into a trap damaging them for {trap.Damage}HP. Current HP = {Contestants[index].Hp}\n\n");
                        

                        if (Contestants[index].Hp <= 0)
                        {
                            sb.Append($"<{Contestants[index].NickName}> died from the trap.\n\n");
                            Contestants.RemoveAt(index);
                        }
                        trapsToBeRemoved.Add(trap);
                    }
                }
                _traps = _traps.Except(trapsToBeRemoved).ToList();
                if (_duelCooldown != 0)
                {
                    _duelCooldown--;
                }
                else if (Contestants.Count >= 2 && Contestants.Count - DuelImmune.Count >= 2)
                {
                    Duel(sb);
                }
                else
                {
                    sb.Append("#No Duel occured due to lack of available players.\n\n");
                }
                foreach (InteractivePlayer contestant in Contestants)
                {
                    contestant.Reset(); //resets value of scenariolikelihood and interactive options
                }
                foreach (Scenario scenario in Scenarios)
                {
                    scenario.ReduceDelay();
                }
                showMessageDelegate($"\nNight**{night}** <{startingContestantCount}> players remaining\n\n" + sb);
                scenarioImmune.Clear();
                EnhancedIndexList.Clear();
                trapsToBeRemoved.Clear();
                playersToBeRemoved.Clear();
                DuelImmune.Clear();
                sb.Clear();

                if (Contestants.Count <= ShowPlayersWhenCountEqual[showPlayersWhenCountEqualIndex])
                {
                    showPlayersWhenCountEqualIndex++;
                    foreach (InteractivePlayer contestant in Contestants)
                    {
                        sb.Append($"<{contestant.ContestantName}> * HP = {contestant.Hp} *\t");
                    }
                    showMessageDelegate("Players Remaining:\r\n" + sb);
                    sb.Clear();
                }

                Thread.Sleep(delayBetweenCycles);

                if (cannelGame())
                    return;

            }

            sb.Append("\n\n**Game Over**\r\n\r\n");
            StringBuilder sbP = new StringBuilder(1000);
            foreach (Player contestant in Contestants)
            {
                sbP.Append($"(ID:{contestant.UserId})<{contestant.FullUserName}> is victorious!\r\n");
                sb.Append($"<{contestant.FullUserName}> is victorious!\r\n");
            }
            showMessageDelegate(sb.ToString(), sbP.ToString());
        }

        //duel method
        void Duel(StringBuilder sb)
        {
            int duelChance = 50;
            int duelist1 = _random.Next(Contestants.Count);
            while (DuelImmune.Contains(Contestants[duelist1]))
            {
                duelist1 = _random.Next(Contestants.Count);
            }
            int duelist2 = _random.Next(Contestants.Count);
            while (duelist1 == duelist2 && DuelImmune.Contains(Contestants[duelist2]))
            {
                duelist2 = _random.Next(Contestants.Count);
            }
            sb.Append($"A Duel started in between <{Contestants[duelist1].NickName}> and <{Contestants[duelist2].NickName}>\n\n");
            if (Contestants[duelist1].WeaponLife > 0)
            {
                Contestants[duelist1].WeaponLife--;
                switch (Contestants[duelist1].WeaponRarity)
                {
                    case Rarity.Common:
                        duelChance += 5;
                        break;
                    case Rarity.Rare:
                        duelChance += 10;
                        break;
                    case Rarity.Epic:
                        duelChance += 15;
                        break;
                    case Rarity.Legendary:
                        duelChance += 25;
                        break;
                    case Rarity.Set:
                        duelChance += 35;
                        break;

                }
            }
            if (Contestants[duelist1].ArmourLife > 0)
            {
                Contestants[duelist1].ArmourLife--;
                switch (Contestants[duelist1].ArmourRarity)
                {
                    case Rarity.Common:
                        duelChance += 5;
                        break;
                    case Rarity.Rare:
                        duelChance += 10;
                        break;
                    case Rarity.Epic:
                        duelChance += 15;
                        break;
                    case Rarity.Legendary:
                        duelChance += 25;
                        break;
                    case Rarity.Set:
                        duelChance += 35;
                        break;

                }
            }
            if (Contestants[duelist1].HelmetLife > 0)
            {
                Contestants[duelist1].HelmetLife--;
                switch (Contestants[duelist1].HelmetRarity)
                {
                    case Rarity.Common:
                        duelChance += 5;
                        break;
                    case Rarity.Rare:
                        duelChance += 10;
                        break;
                    case Rarity.Epic:
                        duelChance += 15;
                        break;
                    case Rarity.Legendary:
                        duelChance += 25;
                        break;
                    case Rarity.Set:
                        duelChance += 35;
                        break;

                }
            }
            if (Contestants[duelist1].OffhandLife > 0)
            {
                Contestants[duelist1].OffhandLife--;
                switch (Contestants[duelist1].OffhandRarity)
                {
                    case Rarity.Common:
                        duelChance += 5;
                        break;
                    case Rarity.Rare:
                        duelChance += 10;
                        break;
                    case Rarity.Epic:
                        duelChance += 15;
                        break;
                    case Rarity.Legendary:
                        duelChance += 25;
                        break;
                    case Rarity.Set:
                        duelChance += 35;
                        break;

                }
            }


            switch (Contestants[duelist1].Debuff)
            {
                case Debuff.DecreasedDuelChance when Contestants[duelist1].DebuffTimer > 0:
                    duelChance -= 5;
                    Contestants[duelist1].DebuffTimer--;
                    break;
                case Debuff.SeverlyDecreasedDuelChance when Contestants[duelist1].DebuffTimer > 0:
                    duelChance -= 10;
                    Contestants[duelist1].DebuffTimer--;
                    break;
            }
            if (Contestants[duelist2].WeaponLife > 0)
            {
                Contestants[duelist2].WeaponLife--;
                switch (Contestants[duelist2].WeaponRarity)
                {
                    case Rarity.Common:
                        duelChance -= 5;
                        break;
                    case Rarity.Rare:
                        duelChance -= 10;
                        break;
                    case Rarity.Epic:
                        duelChance -= 15;
                        break;
                    case Rarity.Legendary:
                        duelChance -= 25;
                        break;
                    case Rarity.Set:
                        duelChance -= 35;
                        break;

                }
            }
            if (Contestants[duelist2].ArmourLife > 0)
            {
                Contestants[duelist2].ArmourLife--;
                switch (Contestants[duelist2].ArmourRarity)
                {
                    case Rarity.Common:
                        duelChance -= 5;
                        break;
                    case Rarity.Rare:
                        duelChance -= 10;
                        break;
                    case Rarity.Epic:
                        duelChance -= 15;
                        break;
                    case Rarity.Legendary:
                        duelChance -= 25;
                        break;
                    case Rarity.Set:
                        duelChance -= 35;
                        break;

                }
            }
            if (Contestants[duelist2].HelmetLife > 0)
            {
                Contestants[duelist2].HelmetLife--;
                switch (Contestants[duelist2].HelmetRarity)
                {
                    case Rarity.Common:
                        duelChance -= 5;
                        break;
                    case Rarity.Rare:
                        duelChance -= 10;
                        break;
                    case Rarity.Epic:
                        duelChance -= 15;
                        break;
                    case Rarity.Legendary:
                        duelChance -= 25;
                        break;
                    case Rarity.Set:
                        duelChance -= 35;
                        break;

                }
            }
            if (Contestants[duelist2].OffhandLife > 0)
            {
                Contestants[duelist2].OffhandLife--;
                switch (Contestants[duelist2].OffhandRarity)
                {
                    case Rarity.Common:
                        duelChance -= 5;
                        break;
                    case Rarity.Rare:
                        duelChance -= 10;
                        break;
                    case Rarity.Epic:
                        duelChance -= 15;
                        break;
                    case Rarity.Legendary:
                        duelChance -= 25;
                        break;
                    case Rarity.Set:
                        duelChance -= 35;
                        break;

                }
            }
            switch (Contestants[duelist2].Debuff)
            {
                case Debuff.DecreasedDuelChance when Contestants[duelist2].DebuffTimer > 0:
                    duelChance += 5;
                    Contestants[duelist2].DebuffTimer--;
                    break;
                case Debuff.SeverlyDecreasedDuelChance when Contestants[duelist2].DebuffTimer > 0:
                    duelChance += 10;
                    Contestants[duelist2].DebuffTimer--;
                    break;
            }
            if (RngRoll(duelChance))
            {
                sb.Append($"<{Contestants[duelist1].NickName}> won the duel and slew <{Contestants[duelist2].NickName}>\n\n");
                Contestants.RemoveAt(duelist2);
            }
            else
            {
                sb.Append($"<{Contestants[duelist2].NickName}> won the duel and slew <{Contestants[duelist1].NickName}>\n\n");
                Contestants.RemoveAt(duelist1);
            }
        }

        void DuelImmuneOption(StringBuilder sb, InteractivePlayer contestant)
        {
            if (contestant.DuelCooldown == 0)
            {
                contestant.DuelCooldown = 5;
                DuelImmune.Add(contestant);
                sb.Append($"<{contestant.NickName}> will not participate in a Duel today.\n\n");
            }
            else
            {
                sb.Append($"<{contestant.NickName}> has already used that option less than 5 days ago. They will instead Do Nothing today.\n\n");
            }
        }
        //interactive options
        private void Loot(InteractivePlayer contestant, StringBuilder sbLoot, List<InteractivePlayer> playersToBeRemoved)
        {
            contestant.ScenarioLikelihood += 10;
            int lootChance = 100 - failToLoot;
            if (contestant.Debuff == Debuff.DecreasedItemFind && contestant.DebuffTimer > 0)
            {
                lootChance -= 5;
                contestant.DebuffTimer--;
            }
            else if (contestant.Debuff == Debuff.SeverlyDecreasedItemFind && contestant.DebuffTimer > 0)
            {
                lootChance -= 10;
                contestant.DebuffTimer--;
            }
            if (RngRoll(lootChance)) // chance to loot item
            {
                int lootType = _random.Next(4); // armour or weapon
                switch (lootType)
                {
                    case 0:
                        contestant.WeaponLife = 5;
                        break;
                    case 1:
                        contestant.ArmourLife = 5;
                        break;
                    case 2:
                        contestant.HelmetLife = 5;
                        break;
                    default:
                        contestant.OffhandLife = 5;
                        break;
                }
                int lootRarity = _random.Next(100);
                //lootRarity = 55;
                switch (lootType)
                {
                    case 0 when lootRarity < CommonChance:
                        if ((int)contestant.WeaponRarity <= 0)
                        {
                            contestant.WeaponRarity = Rarity.Common;
                            sbLoot.Append($"<{contestant.NickName}> has obtained a * Common * weapon!\n");
                        }
                        else
                        {
                            sbLoot.Append($"<{contestant.NickName}> has obtained an inferior weapon and decided to throw it away\n");
                        }
                        break;
                    case 0 when lootRarity < RareChance:
                        if ((int)contestant.WeaponRarity <= 1)
                        {
                            contestant.WeaponRarity = Rarity.Rare;
                            sbLoot.Append($"<{contestant.NickName}> has obtained a * Rare * weapon!\n");
                        }
                        else
                        {
                            sbLoot.Append($"<{contestant.NickName}> has obtained an inferior weapon and decided to throw it away\n");
                        }
                        break;
                    case 0 when lootRarity < EpicChance:
                        if ((int)contestant.WeaponRarity <= 2)
                        {
                            contestant.WeaponRarity = Rarity.Epic;
                            sbLoot.Append($"<{contestant.NickName}> has obtained a * Epic * weapon!\n");
                        }
                        else
                        {
                            sbLoot.Append($"<{contestant.NickName}> has obtained an inferior weapon and decided to throw it away\n");
                        }
                        break;
                    case 0 when lootRarity < LegendaryChance:
                        if ((int)contestant.WeaponRarity <= 3)
                        {
                            contestant.WeaponRarity = Rarity.Legendary;
                            sbLoot.Append($"<{contestant.NickName}> has obtained a * Legendary * weapon!\n");
                        }
                        else
                        {
                            sbLoot.Append($"<{contestant.NickName}> has obtained an inferior weapon and decided to throw it away\n");
                        }
                        break;
                    case 0 when lootRarity < SetChance:
                        if ((int)contestant.WeaponRarity <= 4)
                        {
                            contestant.WeaponRarity = Rarity.Set;
                            sbLoot.Append($"<{contestant.NickName}> has obtained a * Set * weapon!\n");
                        }
                        else
                        {
                            sbLoot.Append($"<{contestant.NickName}> has obtained an inferior weapon and decided to throw it away\n");
                        }
                        break;
                    case 1 when lootRarity < CommonChance:
                        if ((int)contestant.ArmourRarity <= 0)
                        {
                            contestant.ArmourRarity = Rarity.Common;
                            sbLoot.Append($"<{contestant.NickName}> has obtained a * Common * body!\n");
                        }
                        else
                        {
                            sbLoot.Append($"<{contestant.NickName}> has obtained an inferior armour and decided to throw it away\n");
                        }
                        break;
                    case 1 when lootRarity < RareChance:
                        if ((int)contestant.ArmourRarity <= 1)
                        {
                            contestant.ArmourRarity = Rarity.Rare;
                            sbLoot.Append($"<{contestant.NickName}> has obtained a * Rare * body!\n");
                        }
                        else
                        {
                            sbLoot.Append($"<{contestant.NickName}> has obtained an inferior armour and decided to throw it away\n");
                        }
                        break;
                    case 1 when lootRarity < EpicChance:
                        if ((int)contestant.ArmourRarity <= 2)
                        {
                            contestant.ArmourRarity = Rarity.Epic;
                            sbLoot.Append($"<{contestant.NickName}> has obtained a * Epic * body!\n");
                        }
                        else
                        {
                            sbLoot.Append($"<{contestant.NickName}> has obtained an inferior armour and decided to throw it away\n");
                        }
                        break;
                    case 1 when lootRarity < LegendaryChance:
                        if ((int)contestant.ArmourRarity <= 3)
                        {
                            contestant.ArmourRarity = Rarity.Legendary;
                            sbLoot.Append($"<{contestant.NickName}> has obtained a * Legendary * body!\n");
                        }
                        else
                        {
                            sbLoot.Append($"<{contestant.NickName}> has obtained an inferior armour and decided to throw it away\n");
                        }
                        break;
                    case 1 when lootRarity < SetChance:
                        if ((int)contestant.ArmourRarity <= 4)
                        {
                            contestant.ArmourRarity = Rarity.Set;
                            sbLoot.Append($"<{contestant.NickName}> has obtained a * Set * body!\n");
                        }
                        else
                        {
                            sbLoot.Append($"<{contestant.NickName}> has obtained an inferior armour and decided to throw it away\n");
                        }
                        break;
                    case 2 when lootRarity < CommonChance:
                        if ((int)contestant.HelmetRarity <= 0)
                        {
                            contestant.HelmetRarity = Rarity.Common;
                            sbLoot.Append($"<{contestant.NickName}> has obtained a * Common * body!\n");
                        }
                        else
                        {
                            sbLoot.Append($"<{contestant.NickName}> has obtained an inferior armour and decided to throw it away\n");
                        }
                        break;
                    case 2 when lootRarity < RareChance:
                        if ((int)contestant.HelmetRarity <= 1)
                        {
                            contestant.HelmetRarity = Rarity.Rare;
                            sbLoot.Append($"<{contestant.NickName}> has obtained a * Rare * helmet!\n");
                        }
                        else
                        {
                            sbLoot.Append($"<{contestant.NickName}> has obtained an inferior helmet and decided to throw it away\n");
                        }
                        break;
                    case 2 when lootRarity < EpicChance:
                        if ((int)contestant.HelmetRarity <= 2)
                        {
                            contestant.HelmetRarity = Rarity.Epic;
                            sbLoot.Append($"<{contestant.NickName}> has obtained a * Epic * helmet!\n");
                        }
                        else
                        {
                            sbLoot.Append($"<{contestant.NickName}> has obtained an inferior helmet and decided to throw it away\n");
                        }
                        break;
                    case 2 when lootRarity < LegendaryChance:
                        if ((int)contestant.HelmetRarity <= 3)
                        {
                            contestant.HelmetRarity = Rarity.Legendary;
                            sbLoot.Append($"<{contestant.NickName}> has obtained a * Legendary * helmet!\n");
                        }
                        else
                        {
                            sbLoot.Append($"<{contestant.NickName}> has obtained an inferior armour and decided to throw it away\n");
                        }
                        break;
                    case 2 when lootRarity < SetChance:
                        if ((int)contestant.HelmetRarity <= 4)
                        {
                            contestant.HelmetRarity = Rarity.Set;
                            sbLoot.Append($"<{contestant.NickName}> has obtained a * Set * helmet!\n");
                        }
                        else
                        {
                            sbLoot.Append($"<{contestant.NickName}> has obtained an inferior helmet and decided to throw it away\n");
                        }
                        break;
                    case 3 when lootRarity < CommonChance:
                        if ((int)contestant.OffhandRarity <= 0)
                        {
                            contestant.OffhandRarity = Rarity.Common;
                            sbLoot.Append($"<{contestant.NickName}> has obtained a * Common * offhand!\n");
                        }
                        else
                        {
                            sbLoot.Append($"<{contestant.NickName}> has obtained an inferior offhand and decided to throw it away\n");
                        }
                        break;
                    case 3 when lootRarity < RareChance:
                        if ((int)contestant.OffhandRarity <= 1)
                        {
                            contestant.OffhandRarity = Rarity.Rare;
                            sbLoot.Append($"<{contestant.NickName}> has obtained a * Rare * offhand!\n");
                        }
                        else
                        {
                            sbLoot.Append($"<{contestant.NickName}> has obtained an inferior offhand and decided to throw it away\n");
                        }
                        break;
                    case 3 when lootRarity < EpicChance:
                        if ((int)contestant.OffhandRarity <= 2)
                        {
                            contestant.OffhandRarity = Rarity.Epic;
                            sbLoot.Append($"<{contestant.NickName}> has obtained a * Epic * offhand!\n");
                        }
                        else
                        {
                            sbLoot.Append($"<{contestant.NickName}> has obtained an inferior offhand and decided to throw it away\n");
                        }
                        break;
                    case 3 when lootRarity < LegendaryChance:
                        if ((int)contestant.OffhandRarity <= 3)
                        {
                            contestant.OffhandRarity = Rarity.Legendary;
                            sbLoot.Append($"<{contestant.NickName}> has obtained a * Legendary * offhand!\n");
                        }
                        else
                        {
                            sbLoot.Append($"<{contestant.NickName}> has obtained an inferior offhand and decided to throw it away\n");
                        }
                        break;
                    case 3 when lootRarity < SetChance:
                        if ((int)contestant.OffhandRarity <= 4)
                        {
                            contestant.OffhandRarity = Rarity.Set;
                            sbLoot.Append($"<{contestant.NickName}> has obtained a * Set * offhand!\n");
                        }
                        else
                        {
                            sbLoot.Append($"<{contestant.NickName}> has obtained an inferior offhand and decided to throw it away\n");
                        }
                        break;
                }
            }
            else
            {
                int failureDamage;
                switch (failToLoot)
                {
                    case 10:
                        failureDamage = (_random.Next(2) + 2) * 5;
                        contestant.Hp -= failureDamage;
                        sbLoot.Append($"<{contestant.NickName}> got ambushed while looking for loot and got injured for {failureDamage}HP. Current HP = {contestant.Hp}\n ");
                        if (contestant.Hp <= 0)
                        {
                            playersToBeRemoved.Add(contestant);
                        }
                        break;
                    case 25:
                        failureDamage = (_random.Next(2) + 6) * 5;
                        contestant.Hp -= failureDamage;
                        sbLoot.Append($"<{contestant.NickName}> encountered a mini boss while looking for loot and got injured for {failureDamage}HP. Current HP = {contestant.Hp}\n ");
                        if (contestant.Hp <= 0)
                        {
                            playersToBeRemoved.Add(contestant);
                        }
                        break;
                    case 50:
                        failureDamage = (_random.Next(4) + 10) * 5;
                        contestant.Hp -= failureDamage;
                        sbLoot.Append($"<{contestant.NickName}> recieved a nearly life taking blow by a powerful beast while looking for loot and got injured for {failureDamage}HP. Current HP = {contestant.Hp}\n ");
                        if (contestant.Hp <= 0)
                        {
                            playersToBeRemoved.Add(contestant);
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private void StayOnAlert(InteractivePlayer contestant,  StringBuilder sb)
        {
            if (contestant.AlertCooldown == 0)
            {
                contestant.AlertCooldown = 4;
                contestant.ScenarioLikelihood -= 10;
                sb.Append($"<{contestant.NickName}> successfully stayed On Alert. -10% Scenario likelihood. \n");
            }
            else
            {
                int failureChance = _random.Next(10);
                if (failureChance < 6)
                {
                    contestant.AlertCooldown = 4;
                    contestant.ScenarioLikelihood += 40;
                    sb.Append($"<{contestant.NickName}> tried to Stay On Alert but fell in a deep sleep. +40% Scenario likelihood. \n");
                }
                else
                {
                    contestant.AlertCooldown = 4;
                    contestant.ScenarioLikelihood -= 10;
                    sb.Append($"<{contestant.NickName}> successfully stayed On Alert. -10% Scenario likelihood. \n");

                }
            }
        }

        private Debuff ConvertToDebuff(int id, bool severityFactor)
        {
            if (severityFactor == false)
            {
                switch (id)
                {
                    case 0:
                        return Debuff.DecreasedItemFind;
                    //case 1:
                    //    return Debuff.IncreasedDamageTaken;
                    case 1:
                        return Debuff.DecreasedDuelChance;
                    case 2:
                        return Debuff.IncreasedScenarioLikelihood;
                }
                return Debuff.DecreasedItemFind; // what should be default if not found?
            }
            switch (id)
            {
                case 0:
                    return Debuff.SeverlyDecreasedItemFind;
                //case 1:
                 //   return Debuff.SeverlyIncreasedDamageTaken;
                case 1:
                    return Debuff.SeverlyDecreasedDuelChance;
                case 2:
                    return Debuff.SeverlyIncreasedScenarioLikelihood;
            }
            return Debuff.SeverlyDecreasedItemFind; // what should be default if not found?
        }

        //enhanced options
        private void Sabotage(InteractivePlayer contestant, StringBuilder sb)
        {
            if (RngRoll(75))
            {
                int index = _random.Next(Contestants.Count);
                while (Contestants[index].UserId == contestant.UserId)
                {
                    index = _random.Next(Contestants.Count);
                }

                bool severityFactor = RngRoll(20);
                int debuffSelection = _random.Next(3);

                Contestants[index].Debuff = ConvertToDebuff(debuffSelection, severityFactor);
                Contestants[index].DebuffTimer = severityFactor ? 3 : 5;
                sb.Append($"<{contestant.NickName}> has sabotaged <{Contestants[index].NickName}> by giving themm a {Contestants[index].Debuff} debuff for {Contestants[index].DebuffTimer} turns!\n");
            }
            else
            {
                sb.Append($"<{contestant.NickName}> failed to sabotage someone... U trash or wut?\n");
            }
        }

        private void MakeTrap(InteractivePlayer contestant, StringBuilder sb)
        {
            Logger.Log("accessed trap method");
            if (_random.Next(10) < 12)
            {
                Logger.Log("trap made");
                _traps.Add(new Trap(contestant, _random));
                sb.Append($"<{contestant.NickName}> made a Trap!\n");

            }
            else
            {
                sb.Append($"<{contestant.NickName}> has failed to make a trap.\n");
            }
        }

        private void Steal(InteractivePlayer contestant, StringBuilder sb)
        {
            if (RngRoll(35))
            {
                int index = _random.Next(Contestants.Count);
                while (Contestants[index].UserId == contestant.UserId)
                {
                    index = _random.Next(Contestants.Count);
                }
                int stealType = _random.Next(4);
                switch (stealType)
                {
                    case 0:
                        if (Contestants[index].WeaponLife > 0 && (int)contestant.WeaponRarity < (int)Contestants[index].WeaponRarity)
                        {
                            contestant.WeaponRarity = Contestants[index].WeaponRarity;
                            contestant.WeaponLife = 5;
                            Contestants[index].WeaponLife = 0;

                            sb.Append($"<{contestant.NickName}> stole <{Contestants[index].NickName}>'s {Contestants[index].WeaponRarity} Weapon!\n");
                        }
                        else
                        {
                            sb.Append($"<{contestant.NickName}> tried to steal from <{Contestants[index].NickName}> but realised that his Weapon was worse than theirs. \n");
                        }
                        break;
                    case 1:
                        if (Contestants[index].ArmourLife > 0 && (int)contestant.ArmourRarity < (int)Contestants[index].ArmourRarity)
                        {
                            contestant.ArmourRarity = Contestants[index].ArmourRarity;
                            contestant.ArmourLife = 5;
                            Contestants[index].ArmourLife = 0;
                            sb.Append($"<{contestant.NickName}> stole <{Contestants[index].NickName}>'s {Contestants[index].ArmourRarity} Armour!\n");
                        }
                        else
                        {
                            sb.Append($"<{contestant.NickName}> tried to steal from <{Contestants[index].NickName}> but realised that his armour was worse than theirs. \n");
                        }
                        break;
                    case 2:
                        if (Contestants[index].OffhandLife > 0 && (int)contestant.OffhandRarity < (int)Contestants[index].OffhandRarity)
                        {
                            contestant.OffhandRarity = Contestants[index].OffhandRarity;
                            contestant.OffhandLife = 5;
                            Contestants[index].OffhandLife = 0;
                            sb.Append($"<{contestant.NickName}> stole <{Contestants[index].NickName}>'s {Contestants[index].OffhandRarity} Offhand!\n");
                        }
                        else
                        {
                            sb.Append($"<{contestant.NickName}> tried to steal from <{Contestants[index].NickName}> but realised that his Offhand was worse than theirs. \n");
                        }
                        break;
                    case 3:
                        if (Contestants[index].HelmetLife > 0 && (int)contestant.HelmetRarity < (int)Contestants[index].HelmetRarity)
                        {
                            contestant.HelmetRarity = Contestants[index].HelmetRarity;
                            contestant.HelmetLife = 5;
                            Contestants[index].HelmetLife = 0;
                            sb.Append($"<{contestant.NickName}> stole <{Contestants[index].NickName}>'s {Contestants[index].HelmetRarity} Helmet!\n");
                        }
                        else
                        {
                            sb.Append($"<{contestant.NickName}> tried to steal from <{Contestants[index].NickName}> but realised that his Helmet was worse than theirs. \n");
                        }
                        break;
                }
            }
            else
            {
                sb.Append($"<{contestant.NickName}> failed to steal something... git gud ¯\\_(ツ)_/¯\n");
            }
        }

        private  bool RngRoll(int a)
        {
            int chance = a * 10;
            int roll = _random.Next(0, 1000);
            return roll <= chance;
        }

        public void HandlePlayerInput(ulong userId, string reactionName)
        {
            if (_ignoreReactions) return;

            if (EnhancedOptions)
            {
                foreach (int enhanceCheck in EnhancedIndexList)
                {
                    if (Contestants[enhanceCheck].UserId == userId)
                    {
                        switch (reactionName)
                        {
                            case "💣":
                                Contestants[enhanceCheck].EnhancedDecision = EnhancedDecision.MakeATrap;
                                break;
                            case "🔫":
                                Contestants[enhanceCheck].EnhancedDecision = EnhancedDecision.Steal;
                                break;
                            case "🔧":
                                Contestants[enhanceCheck].EnhancedDecision = EnhancedDecision.Sabotage;
                                break;
                        }
                    }
                }
            }
            else
            {
                var authenticPlayer = Contestants.FirstOrDefault(contestant => contestant.UserId == userId);
                if (authenticPlayer != null)
                {
                    switch (reactionName)
                    {
                        case "💰":
                            authenticPlayer.InteractiveDecision = InteractiveDecision.Loot;
                            break;
                        case "❗":
                            authenticPlayer.InteractiveDecision = InteractiveDecision.StayOnAlert;
                            break;
                        case "⚔":
                            authenticPlayer.InteractiveDecision = InteractiveDecision.ImmuneToDuel;
                            break;
                    }
                }
            }
        }
    }
}
