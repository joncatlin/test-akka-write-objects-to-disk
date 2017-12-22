using System;
using System.Collections.Generic;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.ComponentModel;
using Akka.Actor;
using Akka.Configuration;
using Akka.Routing;
using System.Collections;

namespace SnapShotStore
{

    class Program
    {
        private const int NUM_SNAPSHOT_ACTORS = 4;

        static void Main(string[] args)
        {
            int NUM_ACTORS=0;
            try
            {
                NUM_ACTORS = Int32.Parse(Environment.GetEnvironmentVariable("NUM_ACTORS"));
                Console.WriteLine("ENV NUM_ACTORS={0}", NUM_ACTORS);
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR trying to obtain value for Env var: ENV NUM_ACTORS. Exception msg={0}", e.Message);
                NUM_ACTORS = 4;
            }

            // Get the configuration of the akka system
            var config = ConfigurationFactory.ParseString(GetConfiguration());

            // Create the containers for all the actors. Using multiple systems to see if startup time is reduced
            var actorSystem = ActorSystem.Create("csl-arch-poc1", config);

            // Create the accounts
            List<Account> accounts = CreateAccounts(NUM_ACTORS);

            // Create the actors
            IActorRef[] irefs = new IActorRef[NUM_ACTORS];
            for (int i=0; i < NUM_ACTORS; i++)
            {
                Props testActorProps = Props.Create(() => new TestActor(accounts[i]));
                // Spread the actors across the systems to see if we get better performance
                irefs[i] = actorSystem.ActorOf(testActorProps);
            }

            Console.WriteLine("Hit return to display actor state");
            Console.ReadLine();

            irefs[0].Tell(new DisplayState());
            irefs[1].Tell(new DisplayState());
            irefs[NUM_ACTORS - 2].Tell(new DisplayState());
            irefs[NUM_ACTORS - 1].Tell(new DisplayState());

            // Start the timer to measure how long it takes to complete the test
            Console.WriteLine("Starting the test to persist actor state");
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();


            // Send three msgs to see if the metadata seq number changes
            for (int i = 0; i < NUM_ACTORS; i++)
            {
                irefs[i].Tell(new SomeMessage());
            }

            // Get the elapsed time as a TimeSpan value.
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;

            // Format and display the TimeSpan value.
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
            Console.WriteLine("RunTime for telling the actors" + elapsedTime);

            Console.WriteLine("Hit return to cause some actors to print out some of their state. This is to check that their state has been saved and restored correctly");
            Console.ReadLine();

            irefs[0].Tell(new DisplayState());
            irefs[1].Tell(new DisplayState());
            irefs[NUM_ACTORS-2].Tell(new DisplayState());
            irefs[NUM_ACTORS-1].Tell(new DisplayState());



            Console.WriteLine("Hit return to terminate AKKA");
            Console.ReadLine();

            // Wait until actor system terminated
            actorSystem.Terminate();

            Console.WriteLine("Hit return to terminate program");
            Console.ReadLine();

        }


        private static string GetConfiguration()
        {
            return @"
                akka {  
                    stdout-loglevel = ERROR
                    loglevel = ERROR
                    log-config-on-start = on        
                }

                # Dispatcher for the Snapshot file store
                snapshot-dispatcher {
                  type = Dispatcher
                  throughput = 10000
                }

                # Persistence Plugin for SNAPSHOT
                akka.persistence {
            	    snapshot-store {
		                jonfile {
			                # qualified type name of the File persistence snapshot actor
            			    class = ""SnapShotStore.FileSnapshotStore3, SnapShotStore""
                            max-load-attempts=19
#                            dir = ""/temp""
                            dir = ""C:\\temp""

                            # dispatcher used to drive snapshot storage actor
                            #plugin-dispatcher = ""akka.actor.default-dispatcher""
                            plugin-dispatcher = ""snapshot-dispatcher""
                        }
                    }
                }

                akka.persistence.snapshot-store.plugin = ""akka.persistence.snapshot-store.jonfile""

                akka.persistence.max-concurrent-recoveries = 100

                # Deployment configuration
                akka.actor.deployment {

                    # Dispatcher for the TestActors to see if this changes the performance
                    test-actor-dispatcher {
                        type = ForkJoinDispatcher
                        throughput = 10
                        dedicated-thread-pool {
                            thread-count = 8
                            deadlock-timeout = 0s
                            threadtype = background
                        }
                    }

                    # Configuration for test-actor deployment
                    ""/**"" {
                        dispatcher = test-actor-dispatcher
                    }
                }


            ";
        }



        static List<Account> CreateAccounts(int limit)
        {
            Console.WriteLine("Creating the accounts");
            int counter = 0;
            string line;
            List<Account> list = new List<Account>(limit);

            try
            {

                // Read the file and display it line by line.  
                System.IO.StreamReader file =
                    new System.IO.StreamReader(@"c:\temp\datagen.bin");
//                new System.IO.StreamReader(@"/temp/datagen.bin");
                while ((line = file.ReadLine()) != null)
                {
                    if (counter == 0)
                    {
                        counter++;
                        continue; // skip the headers in the file
                    }

                    //                System.Console.WriteLine(line);
                    string[] tokens = line.Split(',');
                    Account account = new Account(tokens[0]);

                    account.CompanyIDCustomerID = tokens[1];
                    account.AccountTypeID = tokens[2];
                    account.PrimaryAccountCodeID = tokens[3];
                    account.PortfolioID = Int32.Parse(tokens[4]);
                    account.ContractDate = tokens[5];
                    account.DelinquencyHistory = tokens[6];
                    account.LastPaymentAmount = tokens[7];
                    account.LastPaymentDate = tokens[8];
                    account.SetupDate = tokens[9];
                    account.CouponNumber = tokens[10];
                    account.AlternateAccountNumber = tokens[11];
                    account.Desc1 = tokens[12];
                    account.Desc2 = tokens[13];
                    account.Desc3 = tokens[14];
                    account.ConversionAccountID = tokens[15];
                    account.SecurityQuestionsAnswered = tokens[16];
                    account.LegalName = tokens[17];
                    account.RandomText0 = Guid.NewGuid() + "SOme random lot of text that is front and ended with a guid to make it uique and fairly long so it taxes the actor creation mechanism to determine if it takes too long" + Guid.NewGuid();
                    account.RandomText1 = Guid.NewGuid() + "SOme random lot of text that is front and ended with a guid to make it uique and fairly long so it taxes the actor creation mechanism to determine if it takes too long" + Guid.NewGuid();
                    account.RandomText3 = Guid.NewGuid() + "SOme random lot of text that is front and ended with a guid to make it uique and fairly long so it taxes the actor creation mechanism to determine if it takes too long" + Guid.NewGuid();
                    account.RandomText4 = Guid.NewGuid() + "SOme random lot of text that is front and ended with a guid to make it uique and fairly long so it taxes the actor creation mechanism to determine if it takes too long" + Guid.NewGuid();
                    account.RandomText5 = Guid.NewGuid() + "SOme random lot of text that is front and ended with a guid to make it uique and fairly long so it taxes the actor creation mechanism to determine if it takes too long" + Guid.NewGuid();
                    account.RandomText6 = Guid.NewGuid() + "SOme random lot of text that is front and ended with a guid to make it uique and fairly long so it taxes the actor creation mechanism to determine if it takes too long" + Guid.NewGuid();
                    account.RandomText7 = Guid.NewGuid() + "SOme random lot of text that is front and ended with a guid to make it uique and fairly long so it taxes the actor creation mechanism to determine if it takes too long" + Guid.NewGuid();
                    account.RandomText8 = Guid.NewGuid() + "SOme random lot of text that is front and ended with a guid to make it uique and fairly long so it taxes the actor creation mechanism to determine if it takes too long" + Guid.NewGuid();
                    account.RandomText9 = Guid.NewGuid() + "SOme random lot of text that is front and ended with a guid to make it uique and fairly long so it taxes the actor creation mechanism to determine if it takes too long" + Guid.NewGuid();

                    // Store the Account in the List
                    list.Add(account);

                    if (counter > limit + 1) break;
                    counter++;
                }

                file.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }

            Console.WriteLine("Finished creating the accounts");
            return list;
        }

        
    }




}
