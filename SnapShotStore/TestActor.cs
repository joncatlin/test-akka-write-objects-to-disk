using System;
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
        public SomeMessage(Account acc)
        {
            this.Acc = acc;
        }

        public Account Acc { get; private set; }
    }
    #endregion

    class TestActor : ReceivePersistentActor
    {

        // The actor state to be persisted
        private readonly Account Acc;

        public override string PersistenceId
        {
            get
            {
                return Acc.AccountID;
            }
        }

        public TestActor(Account acc)
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

            // Show the system configuration
            Console.WriteLine(Context.System.Settings.Config);

        }

        private void RecoverSnapshot(SnapshotOffer offer)
        {
            Console.WriteLine("Processing RecoverSnapshot");

        }




    }
}
