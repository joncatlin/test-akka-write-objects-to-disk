using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace TestDeserialize
{
    class Program
    {
        static void Main(string[] args)
        {

            Random rnd = new Random();


            // Create some objects to serialize 
            Console.WriteLine("Creating objects to write to SE file and then read and compare");
            List<Hashtable> list = new List<Hashtable>(100);
            for (int i = 0; i < 100000; i++)
            {
                var ht = new Hashtable();
                ht.Add("PersistenceId", rnd.Next(1, 1000000).ToString());
                ht.Add("Position", (long)rnd.Next(1, 10000));
                ht.Add("Length", (int)rnd.Next(20, 500));
                bool deleted = ((i % 2 == 0) ? true : false);
                ht.Add("Deleted", deleted);
                list.Add(ht);
            }

            Console.WriteLine("Writing SEs to file");
            WriteSE(list);

            Console.WriteLine("Reading SEs from file");
            var readList = ReadSE();


            for (int i = 0; i < 100; i++)
            {
                if (!list[i].Equals(readList[i]))
                {
                    Console.WriteLine($"Value at {0} does not match", i);
                }
            }
                /*
                var _formatter = new BinaryFormatter();
                Stream stream = new FileStream(@"C:\Users\jcatlin.CSC\Documents\Development\temp\file-snapshot-store4.bin", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

                var acc = new Hashtable
                {
                    ["AccountID"] = "1234",
                    ["Name"] = "Jon Catlin",
                    ["Description"] = "This is a description",
                    ["Age"] = 34
                };
                */
                /*
                            Write(stream, _formatter, acc, "1234");

                            acc["AccountID"] = "1235";
                            acc["Description"] = "This is the second object we are writing";
                            Write(stream, _formatter, acc, "1235");

                            acc["AccountID"] = "1236";
                            acc["Description"] = "This is yet another object that we are writing and has a longer length than the other objects";
                            Write(stream, _formatter, acc, "1236");

                            acc["AccountID"] = "1237";
                            acc["Description"] = "A short desc";
                            Write(stream, _formatter, acc, "1237");
                */
                // Read(stream, _formatter);
            }




            private static void Read(Stream _readStream, BinaryFormatter _formatter)
        {
            long pos = 0;
            object obj;

            // Ensure that the position in the stream is at the start of the file
//            _readStream.Seek(0, SeekOrigin.Begin);

            // Loop through the snapshot store file and find all the previous objects written
            // add any objects found to the map
            // TODO must cope with corrupt files or missing items in a file. For example what happens
            // when the ID of the snapshot is writen but the snapshot object itself is missing or corrupt
            while (_readStream.Position < _readStream.Length)
            {
                try
                {
                    // Read the account from disk
                    string id = (string)_formatter.Deserialize(_readStream);
                    long seq = (long)_formatter.Deserialize(_readStream);
                    DateTime time = (DateTime)_formatter.Deserialize(_readStream);

//                    metadata = new SnapshotMetadata(id, seq, time);
                    //                    metadata = (SnapshotMetadata)_formatter.Deserialize(_readStream);


  
                    // Get the current location of the file stream so we know where the object is stored on the disk
                    pos = _readStream.Position;

                    // Read the account from disk
                    obj = _formatter.Deserialize(_readStream);

                    // Save the information about where the object is located in the file

                }
                catch (SerializationException e)
                {
                    Console.WriteLine("Failed to deserialize. Reason: " + e.Message);
                    throw;
                }
            }
        }


        private static void Write (Stream _writeStream, BinaryFormatter _formatter, object obj, string id)
        {
            long num = 42;
            DateTime time = new DateTime();

            _writeStream.Seek(0, SeekOrigin.End);

            // Write the ID of the object to store first so on Initialize() the objects can all be identified correctly
            _formatter.Serialize(_writeStream, id);
            _formatter.Serialize(_writeStream, num);
            _formatter.Serialize(_writeStream, time);

            // Get the current location of the file stream so we know where the object is stored on the disk
            long pos = _writeStream.Position;

            // Writre the object to the store
            _formatter.Serialize(_writeStream, obj);

        }


        private static void WriteSE(List<Hashtable> list)
        {
            // Open the file for writing
            Stream stream = new FileStream(@"C:\temp\test.bin", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

            // Serialize object
            const int PERSISTENCE_ID_OFFSET = 4;
            const int MAX_ID_SIZE = 10000;
            const int SIZE_OF_LENGTH = 4;
            const int SIZE_OF_SNAPSHOT_LENGTH = 4;
            const int SIZE_OF_POSITION = 8;
            const int SIZE_OF_DELETED = 1;
            var buffer = new byte[MAX_ID_SIZE + SIZE_OF_LENGTH + SIZE_OF_POSITION + SIZE_OF_SNAPSHOT_LENGTH + SIZE_OF_DELETED];

            for (int i = 0; i < list.Count; i++)
            {
                string s = (string)(list[i]["PersistenceId"]);
                int length = Encoding.ASCII.GetBytes(s, 0, s.Length, buffer, PERSISTENCE_ID_OFFSET);

                // TODO throw an exception on this
                if (length > MAX_ID_SIZE)
                    Console.WriteLine("Error: PersistenceId is too large");

                // Convert and store the length of the string 
                buffer[0] = (byte)(length >> 24);
                buffer[1] = (byte)(length >> 16);
                buffer[2] = (byte)(length >> 8);
                buffer[3] = (byte)(length);

                // Convert the position in the file to bytes
                buffer[length + PERSISTENCE_ID_OFFSET] = (byte)((long)(list[i]["Position"]) >> 56);
                buffer[length + PERSISTENCE_ID_OFFSET + 1] = (byte)((long)(list[i]["Position"]) >> 48);
                buffer[length + PERSISTENCE_ID_OFFSET + 2] = (byte)((long)(list[i]["Position"]) >> 40);
                buffer[length + PERSISTENCE_ID_OFFSET + 3] = (byte)((long)(list[i]["Position"]) >> 32);
                buffer[length + PERSISTENCE_ID_OFFSET + 4] = (byte)((long)(list[i]["Position"]) >> 24);
                buffer[length + PERSISTENCE_ID_OFFSET + 5] = (byte)((long)(list[i]["Position"]) >> 16);
                buffer[length + PERSISTENCE_ID_OFFSET + 6] = (byte)((long)(list[i]["Position"]) >> 8);
                buffer[length + PERSISTENCE_ID_OFFSET + 7] = (byte)((long)(list[i]["Position"]));

                // Convert and store the length of the snapshot
                buffer[length + PERSISTENCE_ID_OFFSET + 8] = (byte)((int)list[i]["Length"] >> 24);
                buffer[length + PERSISTENCE_ID_OFFSET + 9] = (byte)((int)list[i]["Length"] >> 16);
                buffer[length + PERSISTENCE_ID_OFFSET + 10] = (byte)((int)list[i]["Length"] >> 8);
                buffer[length + PERSISTENCE_ID_OFFSET + 11] = (byte)((int)list[i]["Length"]);

                // Convert the Deleted value to bytes
                bool deleted = ((bool)(list[i]["Deleted"]));
                buffer[length + PERSISTENCE_ID_OFFSET + 12] = (byte)(deleted ? 1 : 0);

                // Write to stream
                stream.Write(buffer, 0, length + PERSISTENCE_ID_OFFSET + 12 + 1);
            }

            stream.Close();
        }

        private static List<Hashtable> ReadSE()
        {

            List<Hashtable> list = new List<Hashtable>(100);

            // Open the file for writing
            Stream stream = new FileStream(@"C:\temp\test.bin", FileMode.Open, FileAccess.Read, FileShare.Read);

            // Serialize object
            const int PERSISTENCE_ID_OFFSET = 4;
            const int MAX_ID_SIZE = 10000;
            const int SIZE_OF_LENGTH = 4;
            const int SIZE_OF_SNAPSHOT_LENGTH = 4;
            const int SIZE_OF_POSITION = 8;
            const int SIZE_OF_DELETED = 1;
            var lengthBuffer = new byte[SIZE_OF_LENGTH];
            var buffer = new byte[MAX_ID_SIZE + SIZE_OF_LENGTH + SIZE_OF_POSITION + SIZE_OF_SNAPSHOT_LENGTH + SIZE_OF_DELETED];
            long filePos = 0;

            // Read all of the Snapshot File Entries from the file
            while (filePos < stream.Length)
            {
                // Determine the size of the PersistenceId
                stream.Read(lengthBuffer, 0, lengthBuffer.Length);
                int length = (lengthBuffer[0] << 24 | (lengthBuffer[1] & 0xFF) << 16 | (lengthBuffer[2] & 0xFF) << 8 | (lengthBuffer[3] & 0xFF));


                // Get the PersistenceID string from the file
                stream.Read(buffer, 0, length + SIZE_OF_POSITION + SIZE_OF_SNAPSHOT_LENGTH + SIZE_OF_DELETED);
                var PersistenceID = Encoding.ASCII.GetString(buffer, 0, length);

                // Get the Position from the file
                long position = buffer[length] << 56 |
                    (buffer[length + 1] & 0xFF) << 48 |
                    (buffer[length + 2] & 0xFF) << 40 |
                    (buffer[length + 3] & 0xFF) << 32 |
                    (buffer[length + 4] & 0xFF) << 24 |
                    (buffer[length + 5] & 0xFF) << 16 |
                    (buffer[length + 6] & 0xFF) << 8 |
                    (buffer[length + 7] & 0xFF);

                // Get the snapshotLength
                int snapshotLength = (buffer[length + 8] << 24 | (buffer[length + 9] & 0xFF) << 16 | 
                    (buffer[length + 10] & 0xFF) << 8 | (buffer[length + 11] & 0xFF));

                // Convert the Deleted value to bytes
                bool deleted = (buffer[length + 12] == 1) ? true : false;

                // Save the data in a hashtable and then add tot he lsit
                var ht = new Hashtable();
                ht.Add("PersistenceId", PersistenceID);
                ht.Add("Position", position);
                ht.Add("Length", snapshotLength);
                ht.Add("Deleted", deleted);
                list.Add(ht);

                filePos = stream.Position;
            }

            // Close the file
            stream.Close();

            return list;
        }

    }
}
