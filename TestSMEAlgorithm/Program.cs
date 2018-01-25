using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NLog;

namespace TestSMEAlgorithm
{
    [Serializable]
    class SnapshotMetadata
    {
        public SnapshotMetadata(string persistenceId, long sequenceNr, DateTime timestamp)
        {
            PersistenceId = persistenceId;
            SequenceNr = sequenceNr;
            Timestamp = timestamp;
        }

        public long SequenceNr { get; private set; }
        public string PersistenceId { get; private set; }
        public DateTime Timestamp { get; private set; }

        public bool Equals(SnapshotMetadata smd)
        {
            if (SequenceNr != smd.SequenceNr) return false;
            if (!PersistenceId.Equals(smd.PersistenceId)) return false;
            if (Timestamp != smd.Timestamp) return false;
            return true;
        }

    }

    class SnapshotMapEntry
    {
        public SnapshotMapEntry(SnapshotMetadata metadata, long position, int length, bool deleted)
        {
            Metadata = metadata;
            Position = position;
            Length = length;
            Deleted = deleted;
        }
        public SnapshotMetadata Metadata { get; private set; }
        public long Position { get; private set; }
        public int Length { get; private set; }
        public bool Deleted { get; private set; }

        public bool Equals(SnapshotMapEntry sme)
        {
            if (!Metadata.Equals(sme.Metadata)) return false;
            if (Position != sme.Position) return false;
            if (Length != sme.Length) return false;
            if (Deleted != sme.Deleted) return false;
            return true;
        }
    }

    class Program
    {
        // private static Logger _log = LogManager.GetCurrentClassLogger();

        // Counters for debug
        long _loadasync = 0;
        long _load = 0;
        long _saveasync = 0;
        long _save = 0;
        int _smeMaxLength = 0;
        int _readSME = 0;

        // Locks to prevent thread collision
        private Object _smeLock = new object();

        // Constants for the offsets when reading and writing SFE's
        const int SIZE_OF_PERSISTENCE_ID_LENGTH = 4;
        const int SIZE_OF_SEQ_NUM = 8;
        const int SIZE_OF_DATE_TIME = 8;
        const int SIZE_OF_SNAPSHOT_LENGTH = 4;
        const int SIZE_OF_SNAPSHOT_POSITION = 8;
        const int SIZE_OF_DELETED = 1;




        static void Main(string[] args)
        {
            List<SnapshotMapEntry> writeValues, readValues;
            Program pg = new Program();

            FileStream _writeSMEStream = null;
            FileStream _readSMEStream = null;

            try
            {
                string filenameSME = Path.Combine("c:\\temp\\", "test-file-snapshot-map.bin");
                _writeSMEStream = File.Open(filenameSME, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                _readSMEStream = File.Open(filenameSME, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                writeValues = pg.Write(_writeSMEStream);
                readValues = pg.Read(_readSMEStream);

                // Compare what was written to what was read
                for (int i=0; i< writeValues.Count; i++)
                {
                    if (i % 10000 == 0) Console.WriteLine("Compare at: {0}", i);
                    if (!writeValues[i].Equals(readValues[i]))
                    {
                        Console.WriteLine("Values written and read do not match. Index={0}", i);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                Console.ReadLine();
            }

            Console.WriteLine("FINISHED");
            Console.ReadLine();
        }


        private List<SnapshotMapEntry> Write (FileStream stream)
        {
            const string allowedChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789!@$?_-";
            Random rnd = new Random();
            List <SnapshotMapEntry> values = new List<SnapshotMapEntry>();

            for (int i=0; i < 10000000; i++)
            {
                if (i % 10000 == 0) Console.WriteLine("Write at: {0}", i);

                // create a random persistencId
                int stringLength = rnd.Next(20, 1000);
                char[] chars = new char[stringLength];

                for (int j = 0; j < stringLength; j++)
                    chars[j] = allowedChars[rnd.Next(0, allowedChars.Length)];
                string persistenceId = new string(chars) + "-" + i;

                // create a position
                long temp1 = rnd.Next(1000, int.MaxValue);
                long temp2 = rnd.Next(1000, int.MaxValue/2);
                long position = temp1 * temp2;

                // create a sequenceNr
                temp1 = rnd.Next(1000, int.MaxValue);
                temp2 = rnd.Next(0, int.MaxValue / 2);
                long sequenceNr = temp1 * temp2;

                DateTime timestamp = DateTime.Now;

                SnapshotMetadata smd = new SnapshotMetadata(persistenceId, sequenceNr, timestamp);

                bool deleted = (i % 1 == 0) ? true : false;

                int length = rnd.Next(10, int.MaxValue);

                SnapshotMapEntry sme = new SnapshotMapEntry(smd, position, length, deleted);
                values.Add(sme);
                WriteSME(stream, sme);
            }

            // Flush to disk not the OS
            stream.Flush(true);

            return values;
        }


        private List<SnapshotMapEntry> Read(FileStream stream)
        {
            int counter = 0;
            List<SnapshotMapEntry> values = new List<SnapshotMapEntry>();

            stream.Seek(0, SeekOrigin.Begin);

            while (stream.Position < stream.Length)
            {
                SnapshotMapEntry sme = ReadSME(stream);
                values.Add(sme);
                if (counter % 10000 == 0) Console.WriteLine("Read at: {0}", counter);
                counter++;
            }

            return values;
        }


        private void WriteSME(FileStream stream, SnapshotMapEntry sme)
        {
            try
            {
                var pos = stream.Position;

                // Convert the PersistenceId to bytes and store them in the buffer, leaving space at the beginning to store its length
                byte[] temp = Encoding.ASCII.GetBytes(sme.Metadata.PersistenceId);
                int length = temp.Length;
                var buffer = new byte[length + SIZE_OF_PERSISTENCE_ID_LENGTH + SIZE_OF_SEQ_NUM + SIZE_OF_DATE_TIME +
                        SIZE_OF_SNAPSHOT_LENGTH + SIZE_OF_SNAPSHOT_POSITION + SIZE_OF_DELETED];

                // Convert and store the length of the persistence ID
                var bits = BitConverter.GetBytes(length);
                bits.CopyTo(buffer, 0);

                // This is slower than the original code that placed the bytes from the persistence Id straight into the buffer
                // Copy the bytes into the main buffer
                temp.CopyTo(buffer, SIZE_OF_PERSISTENCE_ID_LENGTH);

                // Convert the sequence number of the snapshot
                int offset = length + SIZE_OF_PERSISTENCE_ID_LENGTH;
                var bits1 = BitConverter.GetBytes(sme.Metadata.SequenceNr);
                bits1.CopyTo(buffer, offset);

                // Convert and store the timestamp of the snapshot
                long datetime = sme.Metadata.Timestamp.ToBinary();
                offset += SIZE_OF_SEQ_NUM;
                var bits2 = BitConverter.GetBytes(datetime);
                bits2.CopyTo(buffer, offset);

                // Convert and store the position of the snapshot in the snapshot file
                long position = sme.Position;
                offset += SIZE_OF_DATE_TIME;
                var bits3 = BitConverter.GetBytes(position);
                bits3.CopyTo(buffer, offset);

                // Convert and store the length of the snapshot
                int snapLength = sme.Length;
                offset += SIZE_OF_SNAPSHOT_POSITION;
                var bits4 = BitConverter.GetBytes(snapLength);
                bits4.CopyTo(buffer, offset);

                // Convert and store the deleted marker that denotes if this snapshot is deleted
                offset += SIZE_OF_SNAPSHOT_LENGTH;
                buffer[offset] = (byte)((byte)(sme.Deleted ? 1 : 0));

                // Write to stream
                stream.Write(buffer, 0, offset + 1);
            }
            catch (Exception e)
            {
                //_log.Error("Error writing SME, msg = {0}, location = {1}",
    //                e.Message, e.StackTrace);
            }

        }


        // TODO change back to private after the test
        private SnapshotMapEntry ReadSME(FileStream stream)
        {
            _readSME++;
            try
            {
                var pos = stream.Position;

                // Get the Snapshot Map Entry attributes from the file
                var lengthBuffer = new byte[SIZE_OF_PERSISTENCE_ID_LENGTH];

                // Determine the size of the PersistenceId
                stream.Read(lengthBuffer, 0, lengthBuffer.Length);
                int length = BitConverter.ToInt32(lengthBuffer, 0);
                var buffer = new byte[length + SIZE_OF_SEQ_NUM + SIZE_OF_DATE_TIME + SIZE_OF_SNAPSHOT_LENGTH + SIZE_OF_SNAPSHOT_POSITION + SIZE_OF_DELETED];

                // Get the PersistenceID string from the file
                var bytesRead = stream.Read(buffer, 0, buffer.Length);
                var persistenceId = Encoding.ASCII.GetString(buffer, 0, length);

                int offset = length;
                long sequenceNum = BitConverter.ToInt64(buffer, offset);

                offset += SIZE_OF_SEQ_NUM;
                long datetime = BitConverter.ToInt64(buffer, offset);

                offset += SIZE_OF_DATE_TIME;
                long position = BitConverter.ToInt64(buffer, offset);

                offset += SIZE_OF_SNAPSHOT_POSITION;
                int snapshotLength = BitConverter.ToInt32(buffer, offset);

                offset += SIZE_OF_SNAPSHOT_LENGTH;
                bool deleted = BitConverter.ToBoolean(buffer, offset);

                return new SnapshotMapEntry(new SnapshotMetadata(persistenceId, sequenceNum, DateTime.FromBinary(datetime)), position, snapshotLength, deleted);
            }
            catch (Exception e)
            {
                //_log.Error("Error reading SME, msg = {0}, location = {1}",
          //          e.Message, e.StackTrace);
            }

            return null;
        }










/*
        private void StringToByteArray(string from)
        {
            unsafe
            {
                fixed (void* ptr = from)
                {
                    //                System.Runtime.InteropServices.Marshal.Copy(new IntPtr(ptr), tempByte, 0, len);
                }

            }

        }










/*
fixed (byte* bptr = tempByte)
    {
        char* cptr = (char*)(bptr + offset);
        tempText = new string(cptr, 0, len / 2);
    }
*/

}

}
