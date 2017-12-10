﻿using System;
using System.Collections.Generic;
using System.IO;
using Akka.Persistence.Snapshot;
using Akka.Persistence;
using System.Threading.Tasks;
using Akka.Event;
using Akka.Dispatch;

namespace SnapShotStore
{
    class FileSnapshotStoreEntry
    {
        public FileSnapshotStoreEntry(string persistenceID, object state)
        {
            PersistenceID = persistenceID;
            State = state;
        }

        public string PersistenceID { get; private set; }
        public object State { get; private set; }
    }


    class FileSnapshotStore : SnapshotStore
    {
        private ILoggingAdapter _log;
        private readonly int _maxLoadAttempts;
        private readonly MessageDispatcher _streamDispatcher;
        private readonly string _dir;
        private readonly ISet<SnapshotMetadata> _saving;
        private readonly Akka.Serialization.Serialization _serialization;
        private string _defaultSerializer;


//??
        private readonly Akka.Serialization.Serialization _serializerEntry;
            
        private FileStream _stream = null;

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


        public FileSnapshotStore()
        {
            _log = Context.GetLogger();

            // Get this actors number in the pool
            ActorNumber = FileSnapshotStore.GetActorNumber();
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

            _serialization = Context.System.Serialization;
//            _saving = new SortedSet<SnapshotMetadata>(SnapshotMetadata.Comparer); // saving in progress

            // Log the configuration parameters
            // TODO remove or use this, depending if we can figure out how to make a router group out of this actor
            _log.Info("This actor name= {0}", Context.Self.Path);

            // Open the file that is the snapshot store
            string filename = Path.Combine(_dir, "file-snapshot-store" + ActorNumber + ".bin");
            _log.Info("Opening the snapshot store for this instance, filename = {0}", filename);
            try
            {
                _stream = File.Open(filename, FileMode.Append, FileAccess.Write, FileShare.None);
            }
            catch (Exception e)
            {
                _log.Error("Error opening the snapshot store file, error: {0}", e.StackTrace);
                throw e;
            }


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
//                _saving.Add(metadata);
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
            //FileSnapshotStoreEntry(string persistenceID, object state)

            // Write the ID of the object to store first so on Initialize() the objects can all be identified correctly
            // TODO PERFORMANCE IMPROVEMENT - get one of these serializers at startup and reuse



            var serializerID = _serialization.FindSerializerFor(metadata.PersistenceId, _defaultSerializer);
            var bytes = serializerID.ToBinary(metadata.PersistenceId);
            _stream.Write(bytes, 0, bytes.Length);

            // Get the current location of the file stream so we know where the object is stored on the disk
            long pos = _stream.Position;

            // Write the object to store
            // TODO PERFORMANCE IMPROVEMENT - get one of these serializers at startup and reuse
            var serializerObject = _serialization.FindSerializerFor(snapshot, _defaultSerializer);
            bytes = serializerObject.ToBinary(snapshot);
            _stream.Write(bytes, 0, bytes.Length);

            // Save the information about where the object is located in the file
            SnapshotMap.Add(metadata.PersistenceId, pos);

            // Figure out when to flush
            // TODO flush the stream periodically
            // PERFORMANCE - save any outstanding tasks until the flush
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
            //            object obj = Read(pos);

            var snapshot = new SelectedSnapshot(metadata, null);
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

            // Loop through the snapshot store file and find all the previous objects written
            // add any objects found to the map
            // TODO must cope with corrupt files or missing items in a file. For example what happens
            // when the ID of the snapshot is writen but the snapshot object itself is missing or corrupt
            while (_stream.Position < _stream.Length)
            {/*
                try
                {
                    // Get the PersistenceID of the item in the file
                    _stream.Read(buffer, 0, buffer.Length);
                    var serializer = _serialization.FindSerializerForType(typeof(string), _defaultSerializer);
                    id = (string)serializer.FromBinary(buffer, typeof(string));
                    serializer.
                    // Position the stream to the correct place for the next read as we have read too many bytes 

                    // Get the snapshot for the PersistenceID just read
                    var serializerID = _serialization.FindSerializerFor(metadata.PersistenceId, _defaultSerializer);
                    var bytes = serializerID.ToBinary(metadata.PersistenceId);
                    _stream.Write(bytes, 0, bytes.Length);

                    // Read the account from disk
                    id = (string)formatter.Deserialize(stream);

                    // Get the current location of the file stream so we know where the object is stored on the disk
                    pos = stream.Position;

                                        // Read the account from disk
                                        obj = formatter.Deserialize(stream);

                                        // Save the information about where the object is located in the file
                                        objectLocation.Add(id, pos);
                }
                catch (SerializationException e)
                {
                    Console.WriteLine("Failed to deserialize. Reason: " + e.Message);
                    throw;
                }
                */
            }
        }


    }
}

