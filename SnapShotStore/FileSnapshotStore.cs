using System;
using System.Collections.Generic;
using System.Text;
using Akka.Actor;
using System.IO;
using System.Threading;
using System.Runtime.Serialization;
using Akka.Routing;
using Akka.Persistence.Snapshot;
using Akka.Persistence;
using System.Threading.Tasks;
using Akka.Event;
using Akka.Dispatch;

namespace SnapShotStore
{

    class FileSnapshotStore : SnapshotStore
    {
        private ILoggingAdapter _log;
        private readonly int _maxLoadAttempts;
        private readonly MessageDispatcher _streamDispatcher;
        private readonly DirectoryInfo _dir;
        private readonly ISet<SnapshotMetadata> _saving;
        private readonly Akka.Serialization.Serialization _serialization;
        private string _defaultSerializer;

        private const int NUM_ACTORS = 4;
        private static int NumActors = NUM_ACTORS;

        private static int GetActorNumber()
        {
            if (NumActors < 0)
            {
                // Throw an exception
            }
            return NumActors--;
        }

        private readonly int ActorNumber;

        public FileSnapshotStore()
        {
            // Get this actors number in the pool
            ActorNumber = FileSnapshotStore.GetActorNumber();

            // Get the configuration
            var config = Context.System.Settings.Config.GetConfig("akka.persistence.snapshot-store.jonfile");
            _maxLoadAttempts = config.GetInt("max-load-attempts");

//            _streamDispatcher = Context.System.Dispatchers.Lookup(config.GetString("stream-dispatcher"));
            _dir = new DirectoryInfo(config.GetString("dir"));

//            _defaultSerializer = config.GetString("serializer");

            _serialization = Context.System.Serialization;
            _saving = new SortedSet<SnapshotMetadata>(SnapshotMetadata.Comparer); // saving in progress
            _log = Context.GetLogger();
        }

        protected override Task DeleteAsync(SnapshotMetadata metadata)
        {
            throw new NotImplementedException();
        }

        protected override Task DeleteAsync(string persistenceId, SnapshotSelectionCriteria criteria)
        {
            throw new NotImplementedException();
        }

        protected override Task<SelectedSnapshot> LoadAsync(string persistenceId, SnapshotSelectionCriteria criteria)
        {
            throw new NotImplementedException();
        }

        protected override Task SaveAsync(SnapshotMetadata metadata, object snapshot)
        {
            throw new NotImplementedException();
        }
    }
}

