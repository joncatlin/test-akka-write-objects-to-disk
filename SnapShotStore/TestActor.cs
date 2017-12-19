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
    public class SomeMessage
    {
    }


    public class CompareState
    {
        public CompareState(Account acc)
        {
            Acc = acc;
        }

        public Account Acc { get; private set; }
    }

    #endregion

    class TestActor : ReceivePersistentActor
    {
        private ILoggingAdapter _log;

        // The actor state to be persisted
        private Account Acc;

        public override string PersistenceId
        {
            get
            {
                return (string)Acc.AccountID;
            }
        }

        public TestActor(Account acc)
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
            Command<CompareState>(msg => Compare(msg));
        }



        private void SnapshotSuccess(SaveSnapshotSuccess cmd)
        {
            _log.Debug("Processing SnapShotSuccess command, ID={0}", Acc.AccountID);
        }

        private void SnapshotFailure(SaveSnapshotFailure cmd)
        {
            _log.Debug("Processing SnapShotFailure command, ID={0}, cause={1} \nStacktrace={2}", Acc.AccountID, cmd.Cause.Message, cmd.Cause.StackTrace);
        }

        private void Process(SomeMessage msg)
        {
            // Modify the actor state 
            Acc.Desc1 = "Hi jon the time is: " + DateTime.Now;
            SaveSnapshot(Acc);
            _log.Debug("Processing SaveSnapshot in testactor, ID={0}", Acc.AccountID);
        }

        private void Compare(CompareState state)
        {
            _log.Debug("Processing CompareState in testactor, ID={0}, the new desc is: {1}", Acc.AccountID, Acc.Desc1);
        }

        private void RecoverSnapshot(SnapshotOffer offer)
        {
            _log.Debug("Processing RecoverSnapshot, ID={0}", Acc.AccountID);
            /*
            Hashtable ht = (Hashtable)offer;
            foreach (string key in ((Hashtable)offer).Keys)
            {
                Console.WriteLine(String.Format("{0}: {1}", key, offer[key]));
            }*/
        }




    }
}
