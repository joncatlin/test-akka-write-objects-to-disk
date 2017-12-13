using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using Akka.Persistence.Snapshot;
using Akka.Persistence.Serialization;
using Akka.Persistence;
using System.Threading.Tasks;
using Akka.Event;
using Akka.Dispatch;
using Akka.Serialization;
using System.Runtime.CompilerServices;
using System.Text;

namespace SnapShotStore
{
    /* STUFF TO DO
     * 1. Check that the sequence of event is correct. Snapshot offer does not seem to work
     * 2. Figure out how to store a snaphot with a seqnumber and time
     * 3. Change Initialize so it can read a snapshot with a seq# and time back in
     * 
     * 
     * 
     */

    /// <summary>
    /// This class holds the information stored in the snapshot map. It identifies the snapshot and the location 
    /// it is stored in the snapshot file
    /// </summary>
    /// <param name="metadata">The metadata of the snapshot.</param>
    /// <param name="position">The position the snapshot resides in the file, as an offset from the 
    /// start of the file in bytes.</param>
    /// <param name="length">The length of the snapshot. Required when reading back the snapshot from the file</param>
    class SnapshotMapEntry
    {
        public SnapshotMapEntry (SnapshotMetadata metadata, long position, int length)
        {
            Metadata = metadata;
            Position = position;
            Length = length;
        }
        public SnapshotMetadata Metadata { get; private set; }
        public long Position { get; private set; }
        public int Length { get; private set; }
    }


    /// <summary>
    /// This class holds the information stored in the snapshot file, for each snapshot that is saved.
    /// </summary>
    /// <param name="metadata">The metadata of the snapshot.</param>
    /// <param name="snapshot">The content of the snapshot as presented to the Save method.</param>
    class SnapshotFileEntry
    {
        public SnapshotFileEntry(SnapshotMetadata metadata, object snapshot)
        {
            Metadata = metadata;
            Snapshot = snapshot;
        }
        public SnapshotMetadata Metadata { get; private set; }
        public object Snapshot { get; private set; }
    }


    class FileSnapshotStore3 : SnapshotStore
    {


        private ILoggingAdapter _log;
        private readonly int _maxLoadAttempts;
        private readonly MessageDispatcher _streamDispatcher;
        private readonly string _dir;
        private readonly ISet<SnapshotMetadata> _saving;
        private readonly Akka.Serialization.Serialization _serialization;
        private string _defaultSerializer;


        //??
        private long _latestSnapshotSeqNr;

        private readonly Akka.Serialization.Serialization _serializerEntry;

        private FileStream _writeStream = null;
        private FileStream _readStream = null;
        private BinaryFormatter _formatter;

        // Default constants in case the coniguration item is missing
        private const int NUM_ACTORS = 4;
        private const int MAX_SNAPHOT_SIZE = 40000;     // Maximum size for an item to be saved as a snapshot

        private static int NumActors = NUM_ACTORS;
        private int _maxSnapshotSize = MAX_SNAPHOT_SIZE;

        // Create the map to the items held in the snapshot store
        private const int INITIAL_SIZE = 10000;
        private Dictionary<string, SnapshotMapEntry> SnapshotMap = new Dictionary<string, SnapshotMapEntry>(INITIAL_SIZE);

        // The mechanism to allow multiple copies of the class to work alongside each other without
        // overlapping on which actor stores which snapshot
        private readonly int ActorNumber;


        public FileSnapshotStore3()
        {
            _log = Context.GetLogger();

            // Create a binary formatter to convert the object into something that can be saved to a file
            _formatter = new BinaryFormatter();

            // Get the configuration
            var config = Context.System.Settings.Config.GetConfig("akka.persistence.snapshot-store.jonfile");
            _maxLoadAttempts = config.GetInt("max-load-attempts");

            _streamDispatcher = Context.System.Dispatchers.Lookup(config.GetString("plugin-dispatcher"));
            _dir = config.GetString("dir");
            if (config.GetInt("max-snapshot-size") > 0)
            {
                _maxSnapshotSize = config.GetInt("max-snapshot-size");
            }
            _log.Info("Max Snapshot Size in bytes = {0}", _maxSnapshotSize);

            _defaultSerializer = config.GetString("serializer");
            _serialization = Context.System.Serialization;

            // Log the configuration parameters
            // TODO remove or use this, depending if we can figure out how to make a router group out of this actor
            _log.Info("This actor name= {0}", Context.Self.Path);

            // Open the file that is the snapshot store
            string filename = Path.Combine(_dir, "file-snapshot-store" + ActorNumber + ".bin");
            _log.Info("Opening the snapshot store for this instance, filename = {0}", filename);
            try
            {
                _writeStream = File.Open(filename, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                _readStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            catch (Exception e)
            {
                _log.Error("Error opening the snapshot store file, error: {0}", e.StackTrace);
                throw e;
            }
        }

        protected override Task DeleteAsync(SnapshotMetadata metadata)
        {
            _log.Debug("DeleteAsync() - metadata: {0}, metadata.Timestamp {1:yyyy-MMM-dd-HH-mm-ss ffff}", metadata, metadata.Timestamp);
            return RunWithStreamDispatcher(() =>
            {
                Delete(metadata);
                return new object();
            });
        }


        protected override Task DeleteAsync(string persistenceId, SnapshotSelectionCriteria criteria)
        {
            _log.Debug("DeleteAsync() -persistenceId: {0}", persistenceId);

            // Create an empty SnapshotMetadata
            SnapshotMetadata metadata = new SnapshotMetadata(persistenceId, -1);

            return RunWithStreamDispatcher(() =>
            {
                Delete(metadata);
                return new object();
            });

        }

        /// <summary>
        /// Deletes a snapshot from the store
        /// </summary>
        /// <param name="metadata">TBD</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        protected virtual void Delete(SnapshotMetadata metadata)
        {
            _log.Debug("Delete() - metadata: {0}, metadata.Timestamp {1:yyyy-MMM-dd-HH-mm-ss ffff}", metadata, metadata.Timestamp);
        }




        /// <summary>
        /// Finds the requested snapshot in the file and returns it asynchronously. If no snapshot is found it returns null without waiting
        /// </summary>
        protected override Task<SelectedSnapshot> LoadAsync(string persistenceId, SnapshotSelectionCriteria criteria)
        {
            _log.Debug("LoadAsync() -persistenceId: {0}", persistenceId);

            // Create an empty SnapshotMetadata
            SnapshotMetadata metadata = new SnapshotMetadata(persistenceId, -1);

            return RunWithStreamDispatcher(() => Load(metadata));
        }



        /// <summary>
        /// Stores the snapshot in the file asdynchronously
        /// </summary>
        protected override Task SaveAsync(SnapshotMetadata metadata, object snapshot)
        {
            _log.Debug("SaveAsync() - metadata: {0}, metadata.Timestamp {1:yyyy-MMM-dd-HH-mm-ss ffff}", metadata, metadata.Timestamp);

            return RunWithStreamDispatcher(() =>
            {
                Save(metadata, snapshot);
                return new object();
            });
        }


        /// <summary>
        /// Saves the snapshot to the end of the file.
        /// </summary>
        /// <param name="metadata">TBD</param>
        /// <param name="snapshot">TBD</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        protected virtual void Save(SnapshotMetadata metadata, object snapshot)
        {
            _log.Debug("Save() - metadata: {0}, metadata.Timestamp {1:yyyy-MMM-dd-HH-mm-ss ffff}", metadata, metadata.Timestamp);

            try
            {
                // Create the entry to be stored in the file that represents the snapshot
                var sfe = new SnapshotFileEntry(metadata, snapshot);

                // Serialize the object that describes the snapshot
                var serializerSfe = _serialization.FindSerializerFor(sfe, _defaultSerializer);
                var bytesSfe = serializerSfe.ToBinary(sfe);

                // Convert the length of the file entry into a byte array of fixed length
                // Max int = 2,147,483,647 or 10 digits
                int sfeLength = bytesSfe.Length;
                var bytesForLength = Encoding.ASCII.GetBytes(sfeLength.ToString("D10"));

                // First write the length of the entry to the file, so the size to be read back is known first
                _writeStream.Write(bytesForLength, 0, bytesForLength.Length);

                // Get the current location of the file stream so we know where the object is stored on the disk
                long pos = _writeStream.Position;

                // Write the Snapshot File Entry to disk
                _writeStream.Write(bytesSfe, 0, bytesSfe.Length);

                // Save the information about the snapshot and where it is located in the file to the map
                // Create a snapshot map entry to describe the snapshot
                var sme = new SnapshotMapEntry(metadata, pos, sfeLength);
                SnapshotMap.Add(metadata.PersistenceId, sme);
                /*
                // Figure out when to flush
                // TODO flush the stream periodically
                // PERFORMANCE - save any outstanding tasks until the flush
                if (counter % 100 == 0)
                {
                    stream.Flush(true);
                }
               */
                _writeStream.Flush();
            }
            catch (SerializationException e)
            {
                _log.Error("Failed to serialize. Reason: {0}\n{1}", e.Message, e.StackTrace);
                throw e;
            }

        }



        /// <summary>
        /// Finds the requested snapshot in the file and returns it.
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private SelectedSnapshot Load(SnapshotMetadata metadata)
        {
            _log.Debug("Load() - metadata: {0}, metadata.Timestamp {1:yyyy-MMM-dd-HH-mm-ss ffff}", metadata, metadata.Timestamp);

            // Find the snapshot that matches the criteria or return null
            // TODO Implement the mechanism to match the criteria
            if (!SnapshotMap.ContainsKey(metadata.PersistenceId)) return null;

            // Find the id in the map to get the position within the file
            var sme = SnapshotMap[metadata.PersistenceId];
            SnapshotFileEntry sfe = null;
            try
            {
                // Position to the saved location for the object
                _readStream.Seek(sme.Position, SeekOrigin.Begin);

                // Get the snapshot file entry from the file
                var sfeBuffer = new byte[sme.Length];
                _readStream.Read(sfeBuffer, 0, sfeBuffer.Length);
                var type = typeof(SnapshotFileEntry);
                var serializer = _serialization.FindSerializerForType(type, _defaultSerializer);
                sfe = (SnapshotFileEntry)serializer.FromBinary(sfeBuffer, type);
            }
            catch (SerializationException e)
            {
                _log.Error("Failed to deserialize. Reason: {0} at {1}", e.Message, e.StackTrace);
                throw e;
            }

            var snapshot = new SelectedSnapshot(metadata, sfe.Snapshot);
            _log.Debug("Snapshot found for id: {0}", metadata.PersistenceId);
            return snapshot;
        }





        private Task<T> RunWithStreamDispatcher<T>(Func<T> fn)
        {
            var promise = new TaskCompletionSource<T>();

            _streamDispatcher.Schedule(() =>
            {
                try
                {
                    var result = fn();
                    promise.SetResult(result);
                }
                catch (Exception e)
                {
                    promise.SetException(e);
                }
            });

            return promise.Task;
        }



        protected override void PreStart()
        {
            SnapshotMetadata metadata;
            long pos = 0;
            object obj;
            var buffer = new byte[10]; // Max int is 10- digits long

            _log.Debug("PreStart() - reading the snapshot file to build map");


            // Ensure that the position in the stream is at the start of the file
            _readStream.Seek(0, SeekOrigin.Begin);

            // Loop through the snapshot store file and find all the previous objects written
            // add any objects found to the map
            // TODO must cope with corrupt files or missing items in a file. For example what happens
            // when the ID of the snapshot is writen but the snapshot object itself is missing or corrupt
            while (_readStream.Position < _readStream.Length)
            {
                try
                {
                    // Read the length of the snapshot file entry that follows
                    _readStream.Read(buffer, 0, buffer.Length);
                    string lengthString = Encoding.ASCII.GetString(buffer);
                    int length = Int32.Parse(lengthString);

                    // Get the current location of the file stream so we know where the object is stored on the disk
                    pos = _readStream.Position;

                    // Get the snapshot file entry from the file
                    var sfeBuffer = new byte[length];
                    _readStream.Read(sfeBuffer, 0, sfeBuffer.Length);
                    var type = typeof(SnapshotFileEntry);
                    var serializer = _serialization.FindSerializerForType(type, _defaultSerializer);
                    var sfe = (SnapshotFileEntry)serializer.FromBinary(sfeBuffer, type);

                    // Save the information about the snapshot and where it is located in the file to the map
                    var sme = new SnapshotMapEntry(sfe.Metadata, pos, length);
                    if (!SnapshotMap.TryAdd(sfe.Metadata.PersistenceId, sme))
                    {
                        SnapshotMap.Remove(sfe.Metadata.PersistenceId);
                        SnapshotMap.Add(sfe.Metadata.PersistenceId, sme);
                    }

                    _log.Debug("PreStart() - read metadata from file: {0}, metadata.Timestamp {1:yyyy-MMM-dd-HH-mm-ss ffff}", sfe.Metadata, sfe.Metadata.Timestamp);


                }
                catch (SerializationException e)
                {
                    Console.WriteLine("Failed to deserialize. Reason: " + e.Message);
                    throw;
                }
            }
        }


        protected override void PostStop()
        {
            _log.Debug("PostStop() - flushing and closing the file");

            // Close the file and ensure that everything is flushed correctly
            _writeStream.Flush();
            _writeStream.Close();
            _readStream.Close();

        }

    }
}

