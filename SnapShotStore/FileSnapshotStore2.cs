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

    class FileSnapshotStore2 : SnapshotStore
    {


        private ILoggingAdapter _log;
        private readonly int _maxLoadAttempts;
        private readonly MessageDispatcher _streamDispatcher;
        private readonly string _dir;
        private readonly ISet<SnapshotMetadata> _saving;
//        private readonly Akka.Serialization.Serialization _serialization;
//        private string _defaultSerializer;


//??
        private readonly Akka.Serialization.Serialization _serializerEntry;
            
        private FileStream _stream = null;
        private BinaryFormatter _formatter;

        // Default constants in case the coniguration item is missing
        private const int NUM_ACTORS = 4;
        private const int MAX_SNAPHOT_SIZE = 40000;     // Maximum size for an item to be saved as a snapshot

        private static int NumActors = NUM_ACTORS;
        private int _maxSnapshotSize = MAX_SNAPHOT_SIZE;

        // Create the map to the items held in the snapshot store
        private const int INITIAL_SIZE = 10000;
        private Dictionary<string, long> SnapshotMap = new Dictionary<string, long>(INITIAL_SIZE);

        // The mechanism to allow multiple copies of the class to work alongside each other without
        // overlapping on which actor stores which snapshot
        private readonly int ActorNumber;

        // TODO this needs to be threadsafe if we create more actors
        private static int GetActorNumber()
        {
            if (NumActors < 0)
            {
                // Throw an exception
            }
            return NumActors--;
        }


        public FileSnapshotStore2()
        {
            _log = Context.GetLogger();

            // Create a binary formatter to convert the object into something that can be saved to a file
            _formatter = new BinaryFormatter();

            // Get this actors number in the pool
            ActorNumber = FileSnapshotStore2.GetActorNumber();
            _log.Info("Initialized with ActorNumber = {0}", ActorNumber);

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

            //            _defaultSerializer = config.GetString("serializer");

//            _serialization = Context.System.Serialization;

            // Log the configuration parameters
            // TODO remove or use this, depending if we can figure out how to make a router group out of this actor
            _log.Info("This actor name= {0}", Context.Self.Path);

            // Open the file that is the snapshot store
            string filename = Path.Combine(_dir, "file-snapshot-store" + ActorNumber + ".bin");
            _log.Info("Opening the snapshot store for this instance, filename = {0}", filename);
            try
            {
                _stream = File.Open(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (Exception e)
            {
                _log.Error("Error opening the snapshot store file, error: {0}", e.StackTrace);
                throw e;
            }

            // Initialize the snapshot map with all the positions of the stored snapshots
            Initiailize();
        }

        protected override Task DeleteAsync(SnapshotMetadata metadata)
        {
            throw new NotImplementedException();
        }

        protected override Task DeleteAsync(string persistenceId, SnapshotSelectionCriteria criteria)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Finds the requested snapshot in the file and returns it asynchronously. If no snapshot is found it returns null without waiting
        /// </summary>
        protected override Task<SelectedSnapshot> LoadAsync(string persistenceId, SnapshotSelectionCriteria criteria)
        {
            // Create an empty SnapshotMetadata
            SnapshotMetadata metadata = new SnapshotMetadata(persistenceId, -1);

            return RunWithStreamDispatcher(() => Load(metadata));
        }

        protected override Task SaveAsync(SnapshotMetadata metadata, object snapshot)
        {
            {
                return RunWithStreamDispatcher(() =>
                {
                    Save(metadata, snapshot);
                    return new object();
                });
            }
        }


        /// <summary>
        /// Saves the snapshot to the end of the file.
        /// </summary>
        /// <param name="metadata">TBD</param>
        /// <param name="snapshot">TBD</param>
        protected virtual void Save(SnapshotMetadata metadata, object snapshot)
        {
            try
            {
                // Write the ID of the object to store first so on Initialize() the objects can all be identified correctly
                _formatter.Serialize(_stream, metadata.PersistenceId);

                // Get the current location of the file stream so we know where the object is stored on the disk
                long pos = _stream.Position;

                // Writre the object to the store
                _formatter.Serialize(_stream, snapshot);
 //               counter++;

                // Save the information about where the object is located in the file
                SnapshotMap.Add(metadata.PersistenceId, pos);
                /*
                // Figure out when to flush
                // TODO flush the stream periodically
                // PERFORMANCE - save any outstanding tasks until the flush
                if (counter % 100 == 0)
                {
                    stream.Flush(true);
                }
               */
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
        private SelectedSnapshot Load(SnapshotMetadata metadata)
        {
            // Find the snapshot that matches the criteria or return null
            if (!SnapshotMap.ContainsKey(metadata.PersistenceId)) return null;

            // Find the id in the map to get the position within the file
            var pos = SnapshotMap[metadata.PersistenceId];
            object obj = null;
            try
            {
                // Position to the saved location for the object
                _stream.Seek(pos, SeekOrigin.Begin);

                // Read the account to disk
                obj = _formatter.Deserialize(_stream);
            }
            catch (SerializationException e)
            {
                _log.Error("Failed to deserialize. Reason: {0} at {1}", e.Message, e.StackTrace);
                throw e;
            }

            var snapshot = new SelectedSnapshot(metadata, obj);
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


        private void Initiailize()
        {
            string id;
            long pos = 0;
            object obj;
            var buffer = new byte[_maxSnapshotSize];

            // Ensure that the position in the stream is at the start of the file
            _stream.Seek(0, SeekOrigin.Begin);
            
            // Loop through the snapshot store file and find all the previous objects written
            // add any objects found to the map
            // TODO must cope with corrupt files or missing items in a file. For example what happens
            // when the ID of the snapshot is writen but the snapshot object itself is missing or corrupt
            while (_stream.Position < _stream.Length)
            {
                try
                {
                    // Read the account from disk
                    id = (string)_formatter.Deserialize(_stream);

                    // Get the current location of the file stream so we know where the object is stored on the disk
                    pos = _stream.Position;

                    // Read the account from disk
                    obj = _formatter.Deserialize(_stream);

                    // Save the information about where the object is located in the file
                    SnapshotMap.Add(id, pos);
                }
                catch (SerializationException e)
                {
                    Console.WriteLine("Failed to deserialize. Reason: " + e.Message);
                    throw;
                }
            }

            // TODO determine if this routine needs to leave the file position at the end
        }


    }
}

