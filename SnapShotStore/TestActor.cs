using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Akka;
using Akka.Actor;
using Akka.Event;
using Akka.Persistence;


namespace SnapShotStore
{
    #region Command and Message classes
    // The message to be published
    public class SomeMessage
    {
    }
    #endregion

    class TestActor : ReceivePersistentActor
    {
        private ILoggingAdapter _log;

        // The actor state to be persisted
        private Hashtable Acc;

        public override string PersistenceId
        {
            get
            {
                return (string)Acc["AccountID"];
            }
        }

        public TestActor(Hashtable acc)
        {
            _log = Context.GetLogger();

            // Store the actor state 
            Acc = acc;

            // Recover
            Recover<SnapshotOffer>(offer => RecoverSnapshot(offer));

            // Commands
            Command<SaveSnapshotSuccess>(cmd => SnapshotSuccess(cmd));
            Command<SaveSnapshotFailure>(cmd => SnapshotFailure(cmd));
            Command<SomeMessage>(msg => Process(msg));
        }



        private void SnapshotSuccess(SaveSnapshotSuccess cmd)
        {
            _log.Info("Processing SnapShotSuccess command");

        }

        private void SnapshotFailure(SaveSnapshotFailure cmd)
        {
            _log.Info("Processing SnapShotFailure command");

        }

        private void Process(SomeMessage msg)
        {
            SaveSnapshot(Acc);
            _log.Info("Processing SaveSnapshot in testactor");
        }

        private void RecoverSnapshot(SnapshotOffer offer)
        {
            _log.Info("Processing RecoverSnapshot");
            /*
            Hashtable ht = (Hashtable)offer;
            foreach (string key in ((Hashtable)offer).Keys)
            {
                Console.WriteLine(String.Format("{0}: {1}", key, offer[key]));
            }*/
        }




    }
}
