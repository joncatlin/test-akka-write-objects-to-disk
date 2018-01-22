using System;
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
        private static Logger _log = LogManager.GetCurrentClassLogger();

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
            Program pg = new Program();

            FileStream _writeSMEStream = null;
            FileStream _readSMEStream = null;

            try
            {
                string filenameSME = Path.Combine("c:\\temp\\", "file-snapshot-map.bin");
                _writeSMEStream = File.Open(filenameSME, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                _readSMEStream = File.Open(filenameSME, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }

        }







        private void WriteSME(FileStream stream, SnapshotMapEntry sme)
        {
            try
            {
                var pos = stream.Position;

                // Convert the PersistenceId to bytes and store them in the buffer, leaving space at the beginning to store its length
                byte[] temp = Encoding.ASCII.GetBytes(sme.Metadata.PersistenceId);
                //            int length = Encoding.ASCII.GetBytes(
                //                sme.Metadata.PersistenceId, 0, .Length, buffer, SIZE_OF_PERSISTENCE_ID_LENGTH);

                int length = temp.Length;
                var buffer = new byte[length + SIZE_OF_PERSISTENCE_ID_LENGTH + SIZE_OF_SEQ_NUM + SIZE_OF_DATE_TIME +
                        SIZE_OF_SNAPSHOT_LENGTH + SIZE_OF_SNAPSHOT_POSITION + SIZE_OF_DELETED];

                // Convert and store the length of the persistence ID
                buffer[0] = (byte)(length >> 24);
                buffer[1] = (byte)(length >> 16);
                buffer[2] = (byte)(length >> 8);
                buffer[3] = (byte)(length);



                // This is slower than the original code that placed the bytes from the persistence Id straight into the buffer
                // Copy the bytes into the main buffer
                temp.CopyTo(buffer, SIZE_OF_PERSISTENCE_ID_LENGTH);

                // Convert the sequence number of the snapshot
                int offset = length + SIZE_OF_PERSISTENCE_ID_LENGTH;
                buffer[offset] = (byte)((long)(sme.Metadata.SequenceNr) >> 56);
                buffer[offset + 1] = (byte)((long)(sme.Metadata.SequenceNr) >> 48);
                buffer[offset + 2] = (byte)((long)(sme.Metadata.SequenceNr) >> 40);
                buffer[offset + 3] = (byte)((long)(sme.Metadata.SequenceNr) >> 32);
                buffer[offset + 4] = (byte)((long)(sme.Metadata.SequenceNr) >> 24);
                buffer[offset + 5] = (byte)((long)(sme.Metadata.SequenceNr) >> 16);
                buffer[offset + 6] = (byte)((long)(sme.Metadata.SequenceNr) >> 8);
                buffer[offset + 7] = (byte)((long)(sme.Metadata.SequenceNr));

                // Convert and store the timestamp of the snapshot
                long datetime = sme.Metadata.Timestamp.ToBinary();
                offset += SIZE_OF_SEQ_NUM;
                buffer[offset] = (byte)((long)(datetime) >> 56);
                buffer[offset + 1] = (byte)((long)(datetime) >> 48);
                buffer[offset + 2] = (byte)((long)(datetime) >> 40);
                buffer[offset + 3] = (byte)((long)(datetime) >> 32);
                buffer[offset + 4] = (byte)((long)(datetime) >> 24);
                buffer[offset + 5] = (byte)((long)(datetime) >> 16);
                buffer[offset + 6] = (byte)((long)(datetime) >> 8);
                buffer[offset + 7] = (byte)((long)(datetime));

                // Convert and store the position of the snapshot in the snapshot file
                long position = sme.Position;
                offset += SIZE_OF_DATE_TIME;
                buffer[offset] = (byte)((long)(position) >> 56);
                buffer[offset + 1] = (byte)((long)(position) >> 48);
                buffer[offset + 2] = (byte)((long)(position) >> 40);
                buffer[offset + 3] = (byte)((long)(position) >> 32);
                buffer[offset + 4] = (byte)((long)(position) >> 24);
                buffer[offset + 5] = (byte)((long)(position) >> 16);
                buffer[offset + 6] = (byte)((long)(position) >> 8);
                buffer[offset + 7] = (byte)((long)(position));


                // Convert and store the length of the snapshot
                int snapLength = sme.Length;
                offset += SIZE_OF_SNAPSHOT_POSITION;
                buffer[offset] = (byte)((int)(snapLength) >> 24);
                buffer[offset + 1] = (byte)((int)(snapLength) >> 16);
                buffer[offset + 2] = (byte)((int)(snapLength) >> 8);
                buffer[offset + 3] = (byte)((int)(snapLength));

                // Convert and store the deleted marker that denotes if this snapshot is deleted
                offset += SIZE_OF_SNAPSHOT_LENGTH;
                buffer[offset] = (byte)((byte)(sme.Deleted ? 1 : 0));

                // Write to stream
                stream.Write(buffer, 0, offset + 1);

                //            _log.Debug("WRITE-SME\tPersistenceId={0}\tsequenceNum={1}\tdateTime={2}\tposition={3}\tsnapshotLength={4}\tdeleted={5}",
                //                sme.Metadata.PersistenceId, sme.Metadata.SequenceNr, datetime, sme.Position, sme.Length, sme.Deleted);
                _log.Debug("WRITE-SME ENTRY\t PersistenceId={0}\t pos={1}\t length={2}", sme.Metadata.PersistenceId, pos, offset + 1);

                // TODO Debug code remove
                // Read the length back and see if they are the same
                int debugLength = (buffer[0] << 24 | (buffer[1] & 0xFF) << 16 | (buffer[2] & 0xFF) << 8 | (buffer[3] & 0xFF));
                if (length != debugLength)
                {
                    // Something is terribly wrong !!
                    _log.Error("Converted and deconverted lengths do not match. Original length = {0}, Converted length = {1}",
                        length, debugLength);
                }
                if (offset + 1 > 1000)
                {
                    // Something is terribly wrong !!
                    _log.Error("Writing SME entry larger than expected. Length = {0}, PersistenceId = {1}",
                        offset + 1, sme.Metadata.PersistenceId);
                }

            }
            catch (Exception e)
            {
                _log.Error("Error writing SME, msg = {0}, location = {1}",
                    e.Message, e.StackTrace);
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
                int length = (lengthBuffer[0] << 24 | (lengthBuffer[1] & 0xFF) << 16 | (lengthBuffer[2] & 0xFF) << 8 | (lengthBuffer[3] & 0xFF));
                var buffer = new byte[length + SIZE_OF_SEQ_NUM + SIZE_OF_DATE_TIME + SIZE_OF_SNAPSHOT_LENGTH + SIZE_OF_SNAPSHOT_POSITION + SIZE_OF_DELETED];

                // Check to see if the length read is greate than written, only works if keep the SW running and do not STOP
                // TODO remove this after debug
                if (length > 1000)
                {
                    // Something is terribly wrong !!
                    _log.Error("Read an SME entry from the file that is longer than something written. Length read = {0}, max length written = {1}, bytes representing length = {2}",
                        length, _smeMaxLength, lengthBuffer);
                }

                // Get the PersistenceID string from the file
                var bytesRead = stream.Read(buffer, 0, buffer.Length);
                var persistenceId = Encoding.ASCII.GetString(buffer, 0, length);

                int offset = length;
                long sequenceNum = buffer[offset] << 56 |
                    (buffer[offset + 1] & 0xFF) << 48 |
                    (buffer[offset + 2] & 0xFF) << 40 |
                    (buffer[offset + 3] & 0xFF) << 32 |
                    (buffer[offset + 4] & 0xFF) << 24 |
                    (buffer[offset + 5] & 0xFF) << 16 |
                    (buffer[offset + 6] & 0xFF) << 8 |
                    (buffer[offset + 7] & 0xFF);

                offset = length + SIZE_OF_SEQ_NUM;
                long datetime = buffer[offset] << 56 |
                    (buffer[offset++] & 0xFF) << 48 |
                    (buffer[offset++] & 0xFF) << 40 |
                    (buffer[offset++] & 0xFF) << 32 |
                    (buffer[offset++] & 0xFF) << 24 |
                    (buffer[offset++] & 0xFF) << 16 |
                    (buffer[offset++] & 0xFF) << 8 |
                    (buffer[offset++] & 0xFF);

                long position = buffer[offset++] << 56 |
                    (buffer[offset++] & 0xFF) << 48 |
                    (buffer[offset++] & 0xFF) << 40 |
                    (buffer[offset++] & 0xFF) << 32 |
                    (buffer[offset++] & 0xFF) << 24 |
                    (buffer[offset++] & 0xFF) << 16 |
                    (buffer[offset++] & 0xFF) << 8 |
                    (buffer[offset++] & 0xFF);
                if (position < 0)
                {
                    Console.WriteLine("WTF");
                }
                int snapshotLength =
                    (buffer[offset++] & 0xFF) << 24 |
                    (buffer[offset++] & 0xFF) << 16 |
                    (buffer[offset++] & 0xFF) << 8 |
                    (buffer[offset++] & 0xFF);

                bool deleted = (buffer[offset++] == 1) ? true : false;

                //                _log.Debug("READ-SME\tPersistenceId={0}\tsequenceNum={1}\tdateTime={2}\tposition={3}\tsnapshotLength={4}\tdeleted={5}",
                //                    persistenceId, sequenceNum, datetime, position, snapshotLength, deleted);
                _log.Debug("READ-SME ENTRY\t PersistenceId={0}\t pos={1}\t length={2}", persistenceId, pos, buffer.Length + lengthBuffer.Length);

                // Check to see if the length read is greater than written, only works if keep the SW running and do not STOP
                // TODO remove this after debug
                if (length > 1000)
                {
                    // Something is terribly wrong !!
                    _log.Error("Read an SME entry from the file that is longer than something written. Length read = {0}, PersistenceId={1}",
                        length, persistenceId);
                }

                return new SnapshotMapEntry(new SnapshotMetadata(persistenceId, sequenceNum, DateTime.FromBinary(datetime)), position, snapshotLength, deleted);

            }
            catch (Exception e)
            {
                _log.Error("Error reading SME, msg = {0}, location = {1}",
                    e.Message, e.StackTrace);
            }

            return null;
        }

    }

}
