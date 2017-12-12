using System;
using System.Collections;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace TestDeserialize
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var _formatter = new BinaryFormatter();
            Stream stream = new FileStream(@"C:\Users\jcatlin.CSC\Documents\Development\temp\file-snapshot-store4.bin", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

            var acc = new Hashtable
            {
                ["AccountID"] = "1234",
                ["Name"] = "Jon Catlin",
                ["Description"] = "This is a description",
                ["Age"] = 34
            };
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
            Read(stream, _formatter);
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
    }
}
