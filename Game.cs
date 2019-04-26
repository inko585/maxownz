using AE.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxAI
{


    class Game
    {

        private Logger Logger = Logger.GetLogger("WATTEN");

        private void log(string msg)
        {
            if (showLog)
            {
                Logger.Info(msg);
            }
        }

        public Player[] Players { get; set; }
        public int ScoreA { get; set; }
        public int ScoreB { get; set; }

        public List<Card> PlayedCards = new List<Card>();

        private int DealPosition = 0;


        private CardDeck Deck;

        public Game()
        {
            Players = new Player[4];
            Deck = new CardDeck();
        }
        public Game(int seed)
        {
            Players = new Player[4];
            Deck = new CardDeck(seed);
        }
        private int GetWinnerPosition(IEnumerable<Card> table, int numberTrump, CardColor colorTrump)
        {

            var highcard = table.OrderByDescending(c => GetCardValue(c, numberTrump, colorTrump, table.First().Color)).First();

            return table.ToList().IndexOf(highcard);

        }

        public const int MAX_VALUE = 30;

        public static int GetCardValue(Card card, int numberTrump, CardColor colorTrump, CardColor leadColor)
        {
            if (card.Equals(Card.Max))
            {
                return MAX_VALUE;
            }

            if (card.Equals(Card.Boelle))
            {
                return MAX_VALUE - 1;
            }

            if (card.Equals(Card.Spitz))
            {
                return MAX_VALUE - 2;
            }

            if (card.Number == numberTrump)
            {
                return (card.Color == colorTrump) ? MAX_VALUE - 3 : MAX_VALUE - 6;
            }

            if (card.Color == colorTrump)
            {
                return 16 + card.Number;
            }

            if (card.Color == leadColor)
            {
                return 8 + card.Number;
            }

            return card.Number;
        }

        public void NextDeal()
        {

            log("*****NEW DEAL*****");
            log("Dealer: " + Players[DealPosition]);

            Deck.Shuffle();

            var hands = new List<Card>[4];
            for (int i = 0; i < 4; i++)
            {
                hands[i] = Deck.Top(5).ToList();
                if (!Players.Any(p => p is Human) || Players[i] is Human)
                {
                    log(Players[i].Name() + ": " + hands[i].MakeString(", ", x => x.ToString()));
                }
            }


                log("******************");
            

            var scoreA = 0;
            var scoreB = 0;
            var peek = Deck.Top(1).First();

            log("Peek: " + peek);

            var leadPosition = (DealPosition + 1) % 4;
            var numberTrump = Players[leadPosition].PickNumber(hands[leadPosition], peek);
            log(Players[leadPosition].Name() + ": " + Card.TranslateNumber(numberTrump));
            var colorTrump = Players[DealPosition].PickColor(hands[DealPosition], peek, numberTrump);
            log(Players[DealPosition].Name() + ": " + Card.TranslateColor(colorTrump));

            var aTrumps = (double)hands[0].Sum(x => Game.GetCardValue(x, numberTrump, colorTrump, colorTrump)) + hands[2].Sum(x => Game.GetCardValue(x, numberTrump, colorTrump, colorTrump));
            var bTrumps = (double)hands[1].Sum(x => Game.GetCardValue(x, numberTrump, colorTrump, colorTrump)) + hands[3].Sum(x => Game.GetCardValue(x, numberTrump, colorTrump, colorTrump));

            var aRatio = aTrumps / bTrumps;
            var bRatio = bTrumps / aTrumps;
            log("******************");
            while (scoreA < 3 && scoreB < 3)
            {
                var table = new List<Card>();
                for (int i = leadPosition; i < leadPosition + 4; i++)
                {
                    var turn = i % 4;
                    CardColor leadColor = table.Count == 0 ? colorTrump : table.First().Color;
                    var c = Players[turn].PickCard(hands[turn], table, scoreA, scoreB, leadColor, numberTrump, colorTrump);
                    log(Players[turn] + " lays " + c);
                    table.Add(c);
                    hands[turn].Remove(c);
                }

                var winner = (leadPosition + GetWinnerPosition(table, numberTrump, colorTrump)) % 4;
                PlayedCards.AddRange(table);

                log("**" + Players[winner] + " is high**");




                if (winner == 0 || winner == 2)
                {
                    scoreA++;
                }
                else
                {
                    scoreB++;
                }

                leadPosition = winner;

            }
            log("******************");
            var winIndex = 0;
            if (scoreA > scoreB)
            {
                ScoreA += 2;
                log("WIR wins the deal!");
            }
            else
            {
                winIndex = 1;
                ScoreB += 2;
                log("DIE ANDEREN wins the deal!");
            }

            var playerList = Players.ToList();
            var teamInfo = Players.Where(p => p is AI && !(p is TrainingAI)).Select(p => Tuple.Create(playerList.IndexOf(p) % 2, (p as AI).AgentTeam)).ToList();
            teamInfo.ForEach(p =>
            {
                p.Item2.Feedback(p.Item1 == winIndex, p.Item1 == 0 ? aRatio : bRatio);
            });


            log("New Score: WIR: " + ScoreA + ", DIE ANDEREN: " + ScoreB);
            DealPosition = (DealPosition + 1) % 4;
        }

        private bool showLog = false;
        public void Play(bool showLog)
        {
            this.showLog = showLog;

            log("*****NEW GAME*****");
            while (ScoreA < 15 && ScoreB < 15)
            {
                NextDeal();
            }

            var winner = ScoreA >= 15 ? "WIR" : "DIE ANDEREN";

            log("WINNER: " + winner);
        }

    }


    class CardDeck
    {

        private Random SeededRandom;
        public CardDeck()
        {
            Shuffle();
        }

        public CardDeck(int seed)
        {
            SeededRandom = new Random(seed);
            Shuffle();
        }

        public static List<Card> All ()
        {
            var ret = new List<Card>();
            var colors = new List<CardColor>() { CardColor.Green, CardColor.Hearts, CardColor.Jingles, CardColor.Oaks };
            for (int i = 0; i < 8; i++)
            {
                foreach (var c in colors)
                {
                    ret.Add(new Card() { Color = c, Number = i });
                }
            }
            return ret;
        }

        private void Init()
        {
            CardCache.Clear();
            CardCache = All();
            
        }

        private List<Card> CardCache = new List<Card>();

        public IEnumerable<Card> Top(int n)
        {

            for (int i = 0; i < n; i++)
            {
                var c = CardCache[i];
                CardCache.Remove(c);
                yield return c;
            };

        }

        public void Shuffle()
        {
            Init();
            CardCache.Shuffle(SeededRandom);
        }
    }


    public interface Player
    {

        string Name();

        Card PickCard(IEnumerable<Card> hand, IEnumerable<Card> table, int score, int scoreOther, CardColor leadColor, int numberTrump, CardColor colorTrump);

        CardColor PickColor(IEnumerable<Card> hand, Card peek, int trumpNumber);
        int PickNumber(IEnumerable<Card> hand, Card peek);
        //bool Raise(int curGameValue, IEnumerable<Card> table, IEnumerable<Card> hand, IEnumerable<Card> playedCards);
        void Feedback(bool success, double trumpRatio);

    }

    class Human : Player
    {
        private string name;
        public Human(string name)
        {
            this.name = name;
            Won = 0;
            Total = 0;
        }

        public string Name()
        {
            return name;
        }
        public CardColor PickColor(IEnumerable<Card> hand, Card peek, int trumpNumber)
        {
            Console.WriteLine("Pick the color trump (" + Card.Colors().MakeString(", ") + "):");
            hand.MakeString(", ", c => c.ToString());
            var x = Console.ReadLine();
            CardColor p;
            while ((p = Card.TranslateColor(x)) == CardColor.NA)
            {
                Console.WriteLine("Unknown color, please enter again");
                x = Console.ReadLine();
            }

            return p;
        }

        public int PickNumber(IEnumerable<Card> hand, Card peek)
        {
            Console.WriteLine("Pick the number trump (" + Card.Numbers().MakeString(", ") + "):");
            hand.MakeString(", ", c => c.ToString());
            var x = Console.ReadLine();
            int p;
            while ((p = Card.TranslateNumber(x)) == -1)
            {
                Console.WriteLine("Unknown number, please enter again");
                x = Console.ReadLine();
            }

            return p;
        }

        public Card PickCard(IEnumerable<Card> hand, IEnumerable<Card> table, int score, int scoreOther, CardColor leadColor, int numberTrump, CardColor colorTrump)
        {
            var ordered = hand.OrderByDescending(c => Game.GetCardValue(c, numberTrump, colorTrump, colorTrump)).ToList();
            Console.WriteLine("Pick a Card (" + ordered.MakeString(", ", c => c.ToString()) + "):");
            
            var x = Console.ReadLine();
            int p;
            while (!int.TryParse(x, out p) && !(p <= hand.Count() && p > 0))
            {
                Console.WriteLine("Please enter a number from 1 to " + hand.Count());
                x = Console.ReadLine();
            }

            return ordered.ElementAt(p - 1);
        }

        public double Won { get; set; }
        public double Total { get; set; }
        public void Feedback(bool success, double trumpRatio)
        {
            if (success)
            {
                Won++;
            }

            Total++;
        }
    }

    class TrainingAI : AI
    {

        public TrainingAI(string name) : base(null, name)
        {

        }

        public override void Feedback(bool success, double trumpRatio)
        {
            //do nothing
        }


        public override Card PickCard(IEnumerable<Card> hand, IEnumerable<Card> table, int score, int scoreOther, CardColor leadColor, int numberTrump, CardColor colorTrump)
        {
            var high = table.MaxBy(x => Game.GetCardValue(x, numberTrump, colorTrump, leadColor));
            var orderedHand = hand.OrderByDescending(x => Game.GetCardValue(x, numberTrump, colorTrump, colorTrump)).ToList();

            if (table.Count() == 0)
            {
                return orderedHand[orderedHand.Count / 2];
            }
            if (table.Count() == 1)
            {
                return LowestPossibleTrick(orderedHand, leadColor, numberTrump, colorTrump, high);

            }

            if (table.Count() == 2)
            {
                if (table.ToList().IndexOf(high) != 0)
                {
                    return LowestPossibleTrick(orderedHand, leadColor, numberTrump, colorTrump, high);
                }
                else
                {
                    return orderedHand.Last();
                }
            }

            if (table.ToList().IndexOf(high) != 1)
            {

                return LowestPossibleTrick(orderedHand, leadColor, numberTrump, colorTrump, high);
            }
            else
            {
                return orderedHand.Last();
            }

        }

        private static Card LowestPossibleTrick(IEnumerable<Card> hand, CardColor leadColor, int numberTrump, CardColor colorTrump, Card high)
        {
            var low = hand.Where(x => Game.GetCardValue(x, numberTrump, colorTrump, leadColor) > Game.GetCardValue(high, numberTrump, colorTrump, leadColor)).MinBy(x => Game.GetCardValue(x, numberTrump, colorTrump, leadColor));
            return (low == null) ? hand.Last() : low;
        }
    }

    class AI : Player
    {

        public override string ToString()
        {
            return Name();
        }

        public WattenAgentTeam AgentTeam { get; set; }



        public AI(WattenAgentTeam agentTeam, string name)
        {
            AgentTeam = agentTeam;

            //TrainAgents();

            this.name = name;
        }

        public CardColor PickColor(IEnumerable<Card> hand, Card peek, int trumpNumber)
        {
            var colors = new CardColor[] { CardColor.Green, CardColor.Hearts, CardColor.Jingles, CardColor.Oaks };

            var ordered = colors.OrderByDescending(col =>
            {
                return hand.Sum(card => Game.GetCardValue(card, trumpNumber, col, col));
            });

            return ordered.First();

        }

        public int PickNumber(IEnumerable<Card> hand, Card peek)
        {
            var numbers = 8.Times(n => n);

            var ordered = numbers.OrderByDescending(n =>
            {
                return hand.Count(card => !card.IsCritical() && card.Number.Equals(n));
            }).ThenBy(n => 8 - n);

            return ordered.First();
        }

        public virtual Card PickCard(IEnumerable<Card> hand, IEnumerable<Card> table, int score, int scoreOther, CardColor leadColor, int numberTrump, CardColor colorTrump)
        {

            if (hand.Count() == 1)
            {
                return hand.First();
            }

            var ordered = table.OrderByDescending(c => Game.GetCardValue(c, numberTrump, colorTrump, leadColor));
            var highCard = ordered.Count() == 0 ? null : ordered.First();
            var highValue = ordered.Count() == 0 ? 0 : Game.GetCardValue(ordered.First(), numberTrump, colorTrump, leadColor);
            var vector = new List<int>();
            vector.AddRange(hand.Select(c =>
            {
                return Game.GetCardValue(c, numberTrump, colorTrump, leadColor);
            }).OrderByDescending(x => x));
            switch (table.Count())
            {
                case 1:
                    vector.Add(highValue);
                    break;
                case 2:
                    if (highCard == table.ElementAt(0))
                    {
                        vector.Add(highValue);
                        vector.Add(0);
                    }
                    else
                    {
                        vector.Add(0);
                        vector.Add(highValue);
                    }
                    break;
                case 3:
                    if (highCard == table.ElementAt(1))
                    {
                        vector.Add(0);
                        vector.Add(highValue);
                    }
                    else
                    {
                        vector.Add(highValue);
                        vector.Add(0);
                    }
                    break;
            }
            //vector.AddRange(table.Select(c => Game.GetCardValue(c, numberTrump, colorTrump, leadColor)));
            //vector.Add(Game.GetCardValue(peek, numberTrump, colorTrump, leadColor));
            vector.Add(score);
            vector.Add(scoreOther);
            var agent = AgentTeam.Agents[5 - hand.Count()][table.Count()];
            var z = agent.GetSolution(vector);
            return hand.OrderBy(c => Math.Abs(z - GetRelativeCardValue(c, numberTrump, colorTrump, leadColor, highValue))).ThenBy(c => Game.GetCardValue(c, numberTrump, colorTrump, colorTrump)).First();
        }



        private static int GetRelativeCardValue(Card c, int numberTrump, CardColor colorTrump, CardColor leadColor, int high)
        {
            var real = Game.GetCardValue(c, numberTrump, colorTrump, leadColor);
            return real > high ? real : 0;
        }

        //private void TrainAgents()
        //{
        //    if (AgentTeam != null)
        //    {
        //        foreach (var arr in AgentTeam.Agents)
        //        {
        //            foreach (var a in arr)
        //            {
        //                a.Learn();
        //            }
        //        }
        //    }
        //}


        private string name;
        public string Name()
        {
            return name;
        }


        public virtual void Feedback(bool success, double trumpRatio)
        {

            AgentTeam.Total++;
            if (success)
            {
                AgentTeam.Won++;
            }
            AgentTeam.TrumpRatios.Add(trumpRatio);

        }


    }

    public class Chromosome
    {
        public int[] InputVector { get; set; }

        public double Output { get; set; }

    }

    public class WattenAgentPopulation
    {

        public int LearningRate { get; set; }

        public WattenAgentPopulation()
        {
            TrainingPlayers = new Player[] { new TrainingAI("ai1"), new TrainingAI("ai2"), new TrainingAI("ai3"), new TrainingAI("ai4") }; ;
        }
        public Player[] TrainingPlayers { get; set; }

        public WattenAgentPopulation(int agentCount, int learningRate)
        {
            LearningRate = learningRate;
            AgentTeams = CreateAgents(agentCount);
            TrainingPlayers = new Player[] { new TrainingAI("ai1"), new TrainingAI("ai2"), new TrainingAI("ai3"), new TrainingAI("ai4") }; ;
        }

        private List<WattenAgentTeam> CreateAgents(int agentCount)
        {
            var ret = new List<WattenAgentTeam>();
            for (int k = 0; k < agentCount; k++)
            {
                var agents = new WattenAgent[4][];
                for (int i = 0; i < 4; i++)
                {
                    agents[i] = new WattenAgent[4];
                    for (int j = 0; j < 4; j++)
                    {
                        //var handCaps = (5 - i).Times(n => Game.MAX_VALUE);
                        //var tableCaps = j.Times(n => Game.MAX_VALUE);
                        //var inputVector = new List<int>();
                        //inputVector.AddRange(handCaps);
                        //inputVector.AddRange(tableCaps);
                        //inputVector.Add(Game.MAX_VALUE);
                        //inputVector.Add(2);
                        //inputVector.Add(2);
                        agents[i][j] = new WattenAgent(i, j);

                    }

                }
                ret.Add(new WattenAgentTeam(agents, 0, new string[] { }));
            }
            return ret;
        }

        public List<WattenAgentTeam> AgentTeams { get; set; }


        public List<WattenAgentTeam> Fittest(int count)
        {
            return AgentTeams.OrderByDescending(a => a.Won).Take(count).ToList();
        }

        public int GenerationNumber = 0;

        private Logger logger = Logger.GetLogger("GENETIK");

        private static Random rng = new Random(285);

        private WattenAgentTeam NextParent(List<WattenAgentTeam> teams)
        {
            var ordered = teams.OrderBy(x => x.Won);
            var total = (double)teams.Sum(at => at.Won);
            var prev = 0d;

            var propMap = new List<Tuple<WattenAgentTeam, double>>();
            foreach (var lta in ordered)
            {
                var p = (double)lta.Won / (total) + prev;
                propMap.Add(Tuple.Create(lta, p));
                prev = p;
            }

            var rnd = rng.NextDouble();
            foreach (var t in propMap)
            {
                if (rnd < t.Item2)
                {
                    return t.Item1;
                }
            }

            return null;
        }
        public void NextGeneration()
        {

            logger.Info("Generation: " + GenerationNumber);
            GenerationNumber++;

            EstimateFitness4Agents();

            var nextGen = new List<WattenAgentTeam>();
            var fittest = Fittest(AgentTeams.Count / 4);
            nextGen.AddRange(fittest);
            logger.Info("Fitness:");
            var fitValues = new List<double>();
            foreach (var team in AgentTeams.OrderByDescending(x => x.Won))
            {
                fitValues.Add(team.Fitness);
                logger.Info(team.Name + " (" + team.Won.ToString() + ") [" + team.Parents.MakeString("|", x => x) + "]");

            }
            logger.Info("Average Fitness: " + fitValues.Average());
            logger.Info("Top Fitness: " + fitValues.Max());


            var all = new List<WattenAgentTeam>();
            all.AddRange(AgentTeams);


            var parents = new List<WattenAgentTeam>();

            (AgentTeams.Count / 4).Times(() =>
            {
                parents.Add(NextParent(all));
                all.Remove(parents.Last());
            });


            for (int k = 0; k < (parents.Count() - 1); k += 2)
            {
                var children = parents[k].CrossOver(parents[k + 1], GenerationNumber);
                nextGen.Add(children.Item1);
                nextGen.Add(children.Item2);
            }


            var mutationParents = new List<WattenAgentTeam>();

            all = new List<WattenAgentTeam>();
            all.AddRange(AgentTeams);
            while (nextGen.Count < AgentTeams.Count)
            {
                var p = NextParent(all);
                all.Remove(p);
                nextGen.Add(p.Mutate(0.2, this.GenerationNumber));
            }
            //var mut = fittest.Skip(nextGen.Count);

            //foreach (var m in mut)
            //{
            //    nextGen.Add(m.Mutate(0.4, GenerationNumber));
            //}

            //nextGen.AddRange(fittest);
            this.AgentTeams = nextGen;

        }

        private static Tuple<WattenAgentTeam, double> NextParent(List<Tuple<WattenAgentTeam, double>> propMap)
        {
            var rnd = rng.NextDouble();
            foreach (var t in propMap)
            {
                if (rnd < t.Item2)
                {
                    return t;
                }
            }

            return null;
        }

        public void EstimateFitness4Agents()
        {

            foreach (var team in AgentTeams)
            {
                team.Won = 0;
                team.Total = 0;
                team.TrumpRatios.Clear();
            }



            int gameCount = 0;


            int seed = GenerationNumber * 10;
            LearningRate.Times(() =>
            {

                var playerPos = -1;
                var playerPos2 = -1;

                foreach (var team in AgentTeams)
                {

                    var g = new Game(seed);
                    playerPos = (playerPos + 1) % 4;
                    playerPos2 = (playerPos + 2) % 4;

                    var participantList = new List<Player>();
                    participantList.AddRange(TrainingPlayers);
                    participantList[playerPos] = new AI(team, team.Name);
                    participantList[playerPos2] = new AI(team, team.Name + "_2");

                    g.Players = participantList.ToArray();
                    //logger.Debug("Game Count: " + gameCount);
                    g.Play(false);
                    gameCount++;
                }

                seed++;

            });

        }
    }

    public class WattenAgentTeam
    {

        public List<int> Outputs = new List<int>();

        public string[] Parents { get; set; }

        public int Generation { get; set; }
        public WattenAgent[][] Agents { get; set; }

        public WattenAgent RaiseAgent { get; set; }

        public string Name { get; set; }

        public List<double> TrumpRatios = new List<double>();

        public double TrumpRatio
        {
            get { return TrumpRatios.Count == 0 ? 1 : TrumpRatios.Average(); }
        }

        public int Won { get; set; }

        public int Total { get; set; }

        public double FitnessFromLoadedState { get; set; }

        public WattenAgentTeam(WattenAgent[][] agents, int generation, string[] parents)
        {
            Agents = agents;
            Generation = generation;
            Name = Util.GetRandomName() + "_" + generation;
            FitnessFromLoadedState = 0;
            Parents = parents;
            //RaiseAgent = raiseAgent;
        }


        public void Feedback(bool success, double trumpRatio)
        {
            Total++;
            if (success)
            {
                Won++;
            }
            TrumpRatios.Add(trumpRatio);

        }
        public WattenAgentTeam Mutate(double mutationChance, int generation)
        {
            var mut = Agents.Select(arr => arr.Select(ag => ag.Mutate(mutationChance)).ToArray()).ToArray();
            return new WattenAgentTeam(mut, generation, new string[] { this.Name });

        }

        private static Random rng = new Random(DateTime.Now.Millisecond);

        public Tuple<WattenAgentTeam, WattenAgentTeam> CrossOver(WattenAgentTeam other, int generation)
        {

            var c1 = new WattenAgent[4][];
            var c2 = new WattenAgent[4][];

            for (int i = 0; i < 4; i++)
            {
                c1[i] = new WattenAgent[4];
                c2[i] = new WattenAgent[4];
                for (int j = 0; j < 4; j++)
                {
                    if (rng.NextDouble() < 0.5)
                    {
                        var t = Agents[i][j].CrossOver(other.Agents[i][j]);
                        c1[i][j] = t.Item1;
                        c2[i][j] = t.Item2;
                    }
                    else
                    {
                        var t = other.Agents[i][j].CrossOver(Agents[i][j]);
                        c1[i][j] = t.Item1;
                        c2[i][j] = t.Item2;
                    }
                }
            }

            return Tuple.Create(new WattenAgentTeam(c1, generation, new string[] { this.Name, other.Name }), new WattenAgentTeam(c2, generation, new string[] { this.Name, other.Name }));
        }
        public double Fitness
        {
            get { return ((Total > 0) ? (double)((double)Won / (double)Total) : 0d) / TrumpRatio;  }
        }

    }
    public class WattenAgent
    {

        private double Sigmoid(double x)
        {
            return (1 / (1 + Math.Exp(-2 * x)));
        }

        public List<double> Weights { get; set; }

        //int TablePosition { get; set; }

        //public List<Chromosome> Chromosomes { get; set; }

        //public int Turn { get; set; }

        //public List<int> InputCaps { get; set; }

        //private ActivationNetwork ann;
        //private BackPropagationLearning teacher;

        public List<Tuple<double, double>> WeightRanges { get; set; }


        public WattenAgent(int turn, int tablePos)
        {
            //Turn = turn;
            //TablePosition = tablePos;
            Weights = new List<double>();
            WeightRanges = new List<Tuple<double, double>>();

            (5 - turn).Times(() =>
            {
                WeightRanges.Add(Tuple.Create(0d, 1d));
                Weights.Add(CreateWeight(0d, 1d));
            });

            if (tablePos > 0)
            {
                Weights.Add(CreateWeight(0d, 1d));
                WeightRanges.Add(Tuple.Create(0d, 1d));
                if (tablePos > 1)
                {
                    Weights.Add(CreateWeight(-1d, 0d));
                    WeightRanges.Add(Tuple.Create(-1d, 0d));
                }
            }
            //Weights.AddRange(tablePos.Times(n => (CreateWeight())));
            2.Times(() =>
            {
                WeightRanges.Add(Tuple.Create(0d, 1d));
                Weights.Add(CreateWeight(0d, 1d));
            });
        }

        private static double CreateWeight(double from, double to)
        {
            return from + (to - from) * rng.NextDouble();
        }

        public WattenAgent()
        {
            Weights = new List<double>();
            WeightRanges = new List<Tuple<double, double>>();
        }

        //public LearningAgent(int chromsomeCount, IEnumerable<int> inputCaps, int turn)
        //{
        //    Chromosomes = new List<Chromosome>();
        //    for (int i = 0; i < chromsomeCount; i++)
        //    {
        //        Chromosomes.Add(CreateChromosome(inputCaps, 5 - turn));
        //    }
        //    InputCaps = inputCaps.ToList();
        //    BuildNeuronalNetwork();
        //    Turn = turn;


        //}


        public int GetSolution(IEnumerable<int> input)
        {
            var inputConv = new List<double>();
            for (int i = 0; i < input.Count(); i++)
            {
                var total = (i < input.Count() - 2) ? (double)Game.MAX_VALUE : 2d;
                inputConv.Add(Weights[i] * (double)((double)input.ElementAt(i) / total));
            }
            var prop = inputConv.Sum() / Weights.Sum();
            return (int)(prop * Game.MAX_VALUE);

            //var inputArr = input.Select(i => (double)((double)i / (double)Game.MAX_VALUE)).ToArray();
            //var output = ann.Compute(inputArr);
            //return (int)(Game.MAX_VALUE * output[0]);
        }

        //private bool trained = false;
        //public void Learn()
        //{
        //    if (!trained)
        //    {
        //        teacher.RunEpoch(Chromosomes.Select(c => c.InputVector.Select(i => (double)((double)i / (double)Game.MAX_VALUE)).ToArray()).ToArray(), Chromosomes.Select(c => new double[] { c.Output }).ToArray());
        //        trained = true;
        //    }

        //}



        //public void BuildNeuronalNetwork()
        //{
        //    ann = new ActivationNetwork(new BipolarSigmoidFunction(), InputCaps.Count, InputCaps.Count, 1);

        //    teacher = new BackPropagationLearning(ann);
        //    teacher.LearningRate = 0.33;
        //    teacher.Momentum = 0d;
        //}

        private static Random rng = new Random(DateTime.Now.Millisecond);

        //private Chromosome CreateChromosome(IEnumerable<int> inputCaps, int handCount)
        //{
        //    var handInput = inputCaps.Take(handCount).Select(x => rng.Next(0, x)).OrderByDescending(x => x);
        //    var rest = inputCaps.Skip(handCount).Select(x => rng.Next(0, x));
        //    var inputVector = new List<int>();
        //    inputVector.AddRange(handInput);
        //    inputVector.AddRange(rest);
        //    return new Chromosome()
        //    {

        //        InputVector = inputVector.ToArray(),
        //        Output = rng.NextDouble()
        //    };
        //}

        public WattenAgent Mutate(double mutationChance)
        {
            var succ = new WattenAgent();
            //{
            //InputCaps = this.InputCaps,
            //Turn = this.Turn,
            //TablePosition = this.TablePosition
            //};



            for (var i = 0; i < Weights.Count; i++)
            {
                succ.WeightRanges.Add(WeightRanges[i]);
                if (rng.NextDouble() < mutationChance)
                {

                    succ.Weights.Add(CreateWeight(WeightRanges[i].Item1, WeightRanges[i].Item2));
                }
                else
                {
                    succ.Weights.Add(this.Weights[i]);
                }
            }

            //succ.BuildNeuronalNetwork();
            return succ;

        }
        public Tuple<WattenAgent, WattenAgent> CrossOver(WattenAgent other)
        {
            var childA = new WattenAgent();
            var childB = new WattenAgent();

            for (int i = 0; i < Weights.Count; i++)
            {
                childA.WeightRanges.Add(WeightRanges[i]);
                childB.WeightRanges.Add(WeightRanges[i]);
                if (rng.NextDouble() < 0.5)
                {
                    childA.Weights.Add(Weights[i]);
                    childB.Weights.Add(other.Weights[i]);
                }
                else
                {
                    childB.Weights.Add(Weights[i]);
                    childA.Weights.Add(other.Weights[i]);
                }
            }

            return Tuple.Create(childA, childB);

        }
    }


    public enum CardColor
    {
        Hearts, Green, Oaks, Jingles, NA
    }



    public class Card
    {
        private static Dictionary<int, string> mapNumberString;
        private static Dictionary<string, int> mapStringNumber;
        private static Dictionary<CardColor, string> mapColorString;
        private static Dictionary<string, CardColor> mapStringColor;
        public static string TranslateNumber(int number)
        {

            if (mapNumberString == null)
            {
                mapNumberString = new Dictionary<int, string>();
                mapNumberString[0] = "7";
                mapNumberString[1] = "8";
                mapNumberString[2] = "9";
                mapNumberString[3] = "10";
                mapNumberString[4] = "Unter";
                mapNumberString[5] = "Ober";
                mapNumberString[6] = "König";
                mapNumberString[7] = "Sau";
            }


            return mapNumberString[number];
        }

        public static int TranslateNumber(string desc)
        {
            if (mapStringNumber == null)
            {
                mapStringNumber = new Dictionary<string, int>();
                mapStringNumber["7"] = 0;
                mapStringNumber["8"] = 1;
                mapStringNumber["9"] = 2;
                mapStringNumber["10"] = 3;
                mapStringNumber["unter"] = 4;
                mapStringNumber["ober"] = 5;
                mapStringNumber["könig"] = 6;
                mapStringNumber["sau"] = 7;
            }

            int val;
            if (mapStringNumber.TryGetValue(desc.ToLower(), out val))
            {
                return val;
            }
            return -1;

        }

        public static List<string> Numbers()
        {
            return mapNumberString.Values.ToList();
        }

        public static List<string> Colors()
        {
            return mapColorString.Values.ToList();
        }


        public static string TranslateColor(CardColor color)
        {

            if (mapColorString == null)
            {
                mapColorString = new Dictionary<CardColor, string>();
                mapColorString[CardColor.Green] = "Grün";
                mapColorString[CardColor.Hearts] = "Herz";
                mapColorString[CardColor.Jingles] = "Schellen";
                mapColorString[CardColor.Oaks] = "Eichel";

            }

            return mapColorString[color];
        }

        public static CardColor TranslateColor(string desc)
        {
            if (mapStringColor == null)
            {
                mapStringColor = new Dictionary<string, CardColor>();
                mapStringColor["grün"] = CardColor.Green;
                mapStringColor["herz"] = CardColor.Hearts;
                mapStringColor["schellen"] = CardColor.Jingles;
                mapStringColor["eichel"] = CardColor.Oaks;
            }

            CardColor col;
            if (mapStringColor.TryGetValue(desc, out col))
            {
                return col;
            }
            else
            {
                return CardColor.NA;
            }
        }

        public static readonly Card Max = new Card() { Color = CardColor.Hearts, Number = 6 };
        public static readonly Card Boelle = new Card() { Color = CardColor.Jingles, Number = 0 };
        public static readonly Card Spitz = new Card() { Color = CardColor.Oaks, Number = 0 };

        public bool IsCritical()
        {
            return Equals(Max) || Equals(Boelle) || Equals(Spitz);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Card))
            {
                return false;
            }

            var oCard = obj as Card;

            return Number.Equals(oCard.Number) && Color.Equals(oCard.Color);
        }

        public override string ToString()
        {
            if (Equals(Max))
            {
                return "Max";
            }

            if (Equals(Boelle))
            {
                return "Bölle";
            }

            if (Equals(Spitz))
            {
                return "Spitz";
            }

            return TranslateColor(Color) + " " + TranslateNumber(Number);
        }

        public int Number { get; set; }
        public CardColor Color { get; set; }

    }
}
