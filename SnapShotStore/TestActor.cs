using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Akka;
using Akka.Actor;
using Akka.Persistence;


namespace SnapShotStore
{
    #region Command and Message classes
    // The message to be published
    public class SomeMessage
    {
        public SomeMessage(Hashtable acc)
        {
            this.Acc = acc;
        }

        public Hashtable Acc { get; private set; }
    }
    #endregion

    class TestActor : ReceivePersistentActor
    {

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
            Console.WriteLine("Processing SnapShotSuccess command");

        }

        private void SnapshotFailure(SaveSnapshotFailure cmd)
        {
            Console.WriteLine("Processing SnapShotFailure command");

        }

        private void Process(SomeMessage msg)
        {
            SaveSnapshot(Acc);
            Console.WriteLine("Processing SaveSnapshot in testactor");
        }

        private void RecoverSnapshot(SnapshotOffer offer)
        {
            Console.WriteLine("Processing RecoverSnapshot");
            /*
            Hashtable ht = (Hashtable)offer;
            foreach (string key in ((Hashtable)offer).Keys)
            {
                Console.WriteLine(String.Format("{0}: {1}", key, offer[key]));
            }*/
        }




    }
}
