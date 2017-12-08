using System;
using System.Collections.Generic;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Diagnostics;
using System.ComponentModel;

namespace SnapShotStore
{

    class Program
    {
        static void Main(string[] args)
        {
            string dir = @"C:\Users\jcatlin.CSC\Documents\Development\VisualStudioWorkspace\SnapShotStore";

            // GOAL determine what the likely time it would take to write 120K accounts to a file system and have gluster replicate them.
            // Construct a ConcurrentQueue.
            ConcurrentQueue<Account> cq = new ConcurrentQueue<Account>();

            // Create the accounts to be used in the test
            createAccounts(cq);

            // Start the timer to measure how long it takes to complete the test
            Console.WriteLine("Starting the test");
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            // Create a number of threads to read items out of the queue and write them to a seperate file.
            // You can also use an anonymous delegate to do this.
            int numThreads = 4;
            Thread[] threads = new Thread[numThreads];
            for (int i = 0; i < numThreads; i++)
            {
                string filename = Path.Combine(dir, "snapshot-store0" + i + ".bin");
                threads[i] = new Thread(delegate ()
                {
                    writeFile4(filename, cq);
                });
                threads[i].Start();
            }

            // Wait for the last thread to complete 
            for (int i = 0; i < numThreads; i++)
            {
                threads[i].Join();
            }

            // Get the elapsed time as a TimeSpan value.
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;

            // Format and display the TimeSpan value.
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
            Console.WriteLine("RunTime " + elapsedTime);



            stopWatch.Start();

            // Create a number of threads to read the files generated and track where each object is within the file
            threads = new Thread[numThreads];
            for (int i = 0; i < numThreads; i++)
            {
                string filename = Path.Combine(dir, "snapshot-store0" + i + ".bin");
                threads[i] = new Thread(delegate ()
                {
                    readFile(filename);
                });
                threads[i].Start();
            }

            // Wait for the last thread to complete 
            for (int i = 0; i < numThreads; i++)
            {
                threads[i].Join();
            }

            // Get the elapsed time as a TimeSpan value.
            stopWatch.Stop();
            ts = stopWatch.Elapsed;

            // Format and display the TimeSpan value.
            elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
            Console.WriteLine("RunTime for reads" + elapsedTime);







            // Wait
            Console.ReadLine();
        }

        // Write accounts to a file as fast as we can but done synchronously
        // Takes 9s when on surface pro with files locally
        static void writeFile1(string filename, ConcurrentQueue<Account> queue)
        {
            Account account = null;
            int counter = 0;

            // Open the file for writing
            using (FileStream stream = File.Open(filename, FileMode.Append))
            {
                // Create a binary formatter to convert the object into something that can be saved to a file
                BinaryFormatter formatter = new BinaryFormatter();

                // Loop around reading the concurrent queue and writing items to a file as fast as we can.
                while (true)
                {
                    if (queue.TryDequeue(out account))
                    {
                        try
                        {
                            formatter.Serialize(stream, account);
                            counter++;
                        }
                        catch (SerializationException e)
                        {
                            Console.WriteLine("Failed to serialize. Reason: " + e.Message);
                            throw;
                        }
                    }
                    else
                    {
                        // Nothing left in the queue so we are finished
                        stream.Flush();
                        break;
                    }
                }
            }

            Console.WriteLine("Finished file " + filename + ", counter=" + counter);
        }

        static void createAccounts(ConcurrentQueue<Account> cq)
        {
            Console.WriteLine("Creating the accounts");

            try
            {

                int counter = 0;
                string line;
                int limit = 120000;

                // Read the file and display it line by line.  
                System.IO.StreamReader file =
                    new System.IO.StreamReader(@"C:\Users\jcatlin.CSC\Documents\Development\VisualStudioWorkspace\cad-create-accounts\datagen.txt");
                while ((line = file.ReadLine()) != null)
                {
                    if (counter == 0)
                    {
                        counter++;
                        continue; // skip the headers in the file
                    }

                    //                System.Console.WriteLine(line);
                    string[] tokens = line.Split(',');
                    Account account = new Account();

                    account.AccountID = tokens[0];
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
                    cq.Enqueue(account);

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

        }

        
        // Write accounts to a file as safely as we can but done synchronously
        static void writeFile2(string filename, ConcurrentQueue<Account> queue)
        {
            Account account = null;
            int counter = 0;

            // Open the file for writing
            using (FileStream stream = File.Open(filename, FileMode.Append))
            {
                // Create a binary formatter to convert the object into something that can be saved to a file
                BinaryFormatter formatter = new BinaryFormatter();

                // Loop around reading the concurrent queue and writing items to a file as fast as we can.
                while (true)
                {
                    if (queue.TryDequeue(out account))
                    {
                        try
                        {
                            formatter.Serialize(stream, account);
                            stream.Flush(true);
                            counter++;
                        }
                        catch (SerializationException e)
                        {
                            Console.WriteLine("Failed to serialize. Reason: " + e.Message);
                            throw;
                        }
                    }
                    else
                    {
                        // Nothing left in the queue so we are finished
                        stream.Flush(true);
                        break;
                    }
                }
            }

            Console.WriteLine("Finished file " + filename + ", counter=" + counter);
        }


        // Write accounts to a file with a balance between fat and secure, done synchronously
        // Takes about 16s with a flush every 100 msgs
        static void writeFile3(string filename, ConcurrentQueue<Account> queue)
        {
            Account account = null;
            int counter = 0;

            // Open the file for writing
            using (FileStream stream = File.Open(filename, FileMode.Append))
            {
                // Create a binary formatter to convert the object into something that can be saved to a file
                BinaryFormatter formatter = new BinaryFormatter();

                // Loop around reading the concurrent queue and writing items to a file as fast as we can.
                while (true)
                {
                    if (queue.TryDequeue(out account))
                    {
                        try
                        {
                            formatter.Serialize(stream, account);
                            counter++;

                            if (counter % 100 == 0)
                            {
                                stream.Flush(true);
                            }
                        }
                        catch (SerializationException e)
                        {
                            Console.WriteLine("Failed to serialize. Reason: " + e.Message);
                            throw;
                        }
                    }
                    else
                    {
                        // Nothing left in the queue so we are finished
                        stream.Flush(true);
                        break;
                    }
                }
            }

            Console.WriteLine("Finished file " + filename + ", counter=" + counter);
        }


        // Write accounts to a file with a marker showing where the account is positioned, done synchronously
        // Keeps track of where each account's position in the file is.
        static void writeFile4(string filename, ConcurrentQueue<Account> queue)
        {
            Account account = null;
            ObjectMarker om = new ObjectMarker();
            int counter = 0;
            const int INITIAL_SIZE = 100000;
            Dictionary<string, long> objectLocation = new Dictionary<string, long>(INITIAL_SIZE);

            // Open the file for writing
            using (FileStream stream = File.Open(filename, FileMode.Append, FileAccess.Write, FileShare.None))
            {
                // Create a binary formatter to convert the object into something that can be saved to a file
                BinaryFormatter formatter = new BinaryFormatter();

                // Loop around reading the concurrent queue and writing items to a file as fast as we can.
                while (true)
                {
                    if (queue.TryDequeue(out account))
                    {
                        try
                        {
                            // Get the current location of the file stream so we know where the object is stored on the disk
                            long pos = stream.Position;
                            
                            // Write the account to disk
                            formatter.Serialize(stream, account);
                            counter++;

                            // Save the information about where the object is located in the file
                            objectLocation.Add(account.AccountID, pos);

                            if (counter % 1000 == 0)
                            {
                                stream.Flush(true);
                            }
                        }
                        catch (SerializationException e)
                        {
                            Console.WriteLine("Failed to serialize. Reason: " + e.Message);
                            throw;
                        }
                    }
                    else
                    {
                        // Nothing left in the queue so we are finished
                        stream.Flush(true);
                        break;
                    }
                }
            }

            Console.WriteLine("Finished file " + filename + ", counter=" + counter);

        }


        static void readFile(string filename)
        {
            Account account = null;
            int counter = 0;
            const int INITIAL_SIZE = 100000;
            Dictionary<string, long> objectLocation = new Dictionary<string, long>(INITIAL_SIZE);

            // Open the file for reading
            using (FileStream stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                // Create a binary formatter to convert the object into something that can be saved to a file
                BinaryFormatter formatter = new BinaryFormatter();

                // Loop around reading the concurrent queue and writing items to a file as fast as we can.
                while (stream.Position < stream.Length) 
                {
                    try
                    {
                        // Get the current location of the file stream so we know where the object is stored on the disk
                        long pos = stream.Position;

                        // Read the account to disk
                        account = (Account)formatter.Deserialize(stream);

                        // Save the information about where the object is located in the file
                        objectLocation.Add(account.AccountID, pos);
                        counter++;
                    }
                    catch (SerializationException e)
                    {
                        Console.WriteLine("Failed to deserialize. Reason: " + e.Message);
                        throw;
                    }
                }
                Console.WriteLine("Finished file " + filename + ", counter=" + counter);


                // Pick the first few objects in the dictionary and write them out
                counter = 0;

                foreach (KeyValuePair<string, long> entry in objectLocation)
                {
                    if (counter > 2) break;

                    // Position to the saved location for the object
                    long pos = entry.Value;
                    stream.Seek(pos, SeekOrigin.Begin);

                    // Read the account to disk
                    account = (Account)formatter.Deserialize(stream);

                    dumpObject(account);
                    counter++;
                }


            }


        }



        private static void dumpObject(Object obj)
        {
            foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(obj))
            {
                string name = descriptor.Name;
                object value = descriptor.GetValue(obj);
                Console.WriteLine("{0}={1}", name, value);
            }
        }
















    }




}
