using AE.Logging;
using MaxAI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace MaxAI
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>

        static void Main()
        {

            var l = Logger.GetLogger("WATTEN");
            var l2 = Logger.GetLogger("GENETIK");
            var rfa = new RollingCustomFileAppender("RFA", Logger.LOG_ALL, "C:/watten/", "watten", 100000, true);
            var rfa2 = new RollingCustomFileAppender("GRFA", Logger.LOG_INFO, "C:/watten/", "genetik", 100000, true);
            var ca = new MessageAppender("CA", Logger.LOG_ALL, x => Console.WriteLine(x));
            l.AddAppender(rfa);
            l.AddAppender(ca);
            l2.AddAppender(rfa);
            l2.AddAppender(ca);
            l2.AddAppender(rfa2);
            //var p = "C:/watten/save_p12.xml";
            //var p2 = "C:/watten/save_g1_p1.xml";
            //var p3 = "C:/watten/save_p13.xml";
            var pop = new WattenAgentPopulation(80, 1000);
            10.Times(() => pop.NextGeneration());

            //var pop2 = LoadPopulation(p);

            ////pop2.TrainingPlayers = pop.Fittest(4).Select(at => new AI(at)).ToArray();
            //pop.LearningRate = 1000;
            //4.Times(() => pop.NextGeneration());

            //SavePopulation(pop, p3);

            var human = new Human("Jens");

            10.Times(() =>
            {
                var g = new Game();
                var best = pop.Fittest(3);
                g.Players = new Player[] { human, new AI(best[0], "Enemy1"), new AI(best[1], "Friend"), new AI(best[2], "Enemy2") };
                g.Play(true);
            });
            Console.WriteLine((double)human.Won / human.Total);
            Console.ReadLine();

            //var pop2 = LoadPopulation(p);

            //2.Times(() => pop2.NextGeneration());S


            //var g = new Game();
            //var best = pop2.Fittest(4);
            //g.Players = new Player[] { new AI(best.AssembleAt(0)), new AI(best.AssembleAt(1)), new AI(best.AssembleAt(2)), new AI(best.AssembleAt(3)) };
            //g.Play(true);

            //SavePopulation(pop2, p);
        }



        private static void SavePopulation(WattenAgentPopulation p, string path)
        {

            var popEntity = new PopulationEntity()
            {
                Generation = p.GenerationNumber,
                LearningRate = p.LearningRate,
                AgentTeams = new AgentTeamEntity[p.AgentTeams.Count]
            };

            for (int k = 0; k < p.AgentTeams.Count; k++)
            {
                var team = new AgentTeamEntity()
                {
                    Fitness = p.AgentTeams[k].Fitness,
                    Name = p.AgentTeams[k].Name,
                    Total = p.AgentTeams[k].Total,
                    Won = p.AgentTeams[k].Won,
                    Generation = p.AgentTeams[k].Generation,
                    Parents = p.AgentTeams[k].Parents
                };

                team.AgentsForTablePosition = new TablePosition[4];
                for (int i = 0; i < 4; i++)
                {
                    team.AgentsForTablePosition[i] = new TablePosition()
                    {
                        Id = i
                    };
                    team.AgentsForTablePosition[i].AgentsForTurn = p.AgentTeams[k].Agents[i].Select(a => new AgentEntity()
                    {
                        Weights = a.Weights.ToArray()
                    }).ToArray();

                }

                popEntity.AgentTeams[k] = team;
            }


            var serializer = new XmlSerializer(typeof(PopulationEntity));
            var writer = new StreamWriter(path);
            serializer.Serialize(writer, popEntity);
        }

        private static WattenAgentPopulation LoadPopulation(string path)
        {
            var pop = new WattenAgentPopulation();
            var serializer = new XmlSerializer(typeof(PopulationEntity));
            using (var sr = new StreamReader(path))
            {
                PopulationEntity popEntity = (PopulationEntity)serializer.Deserialize(sr);
                pop.LearningRate = popEntity.LearningRate;
                pop.GenerationNumber = popEntity.Generation;
                pop.AgentTeams = new List<WattenAgentTeam>();

                foreach (var teamEnt in popEntity.AgentTeams)
                {
                    var agents = new WattenAgent[4][];

                    for (int i = 0; i < 4; i++)
                    {
                        int j = 0;
                        agents[i] = teamEnt.AgentsForTablePosition[i].AgentsForTurn.Select(a =>
                       {
                           var la = new WattenAgent()
                           {
                               Weights = a.Weights.ToList()

                               //InputCaps = a.InputCaps.ToList(),
                           };

                           (5 - i).Times(() =>
                           {
                               la.WeightRanges.Add(Tuple.Create(0d, 1d));
                           });

                           if (j > 0)
                           {
                               la.WeightRanges.Add(Tuple.Create(0d, 1d));
                               if (j > 1)
                               {                                 
                                   la.WeightRanges.Add(Tuple.Create(-1d, 0d));
                               }
                           }

                           3.Times(() =>
                           {
                               la.WeightRanges.Add(Tuple.Create(0d, 1d));
                           });

                           j++;
                           //la.BuildNeuronalNetwork();
                           return la;
                       }).ToArray();
                    }

                    var wat = new WattenAgentTeam(agents, 0, new string[] { })
                    {
                        Name = teamEnt.Name,
                        Won = teamEnt.Won,
                        Total = teamEnt.Total,
                        FitnessFromLoadedState = teamEnt.Fitness,
                        Generation = teamEnt.Generation
                    };
                    if (teamEnt.Parents != null)
                    {
                        wat.Parents = teamEnt.Parents;
                    }
                    pop.AgentTeams.Add(wat);
                }

            }

            return pop;
        }
    }

    public class PopulationEntity
    {

        public int Generation { get; set; }

        public int LearningRate { get; set; }

        public AgentTeamEntity[] AgentTeams { get; set; }


    }

    public class TablePosition
    {
        public int Id { get; set; }
        public AgentEntity[] AgentsForTurn { get; set; }
    }



    public class AgentTeamEntity
    {
        public string Name { get; set; }

        public string [] Parents { get; set; }

        public int Total { get; set; }

        public int Won { get; set; }

        public double Fitness { get; set; }

        public TablePosition[] AgentsForTablePosition { get; set; }

        public int Generation { get; set; }
    }

    public class AgentEntity
    {

       public double[] Weights { get; set; }


    }





}
