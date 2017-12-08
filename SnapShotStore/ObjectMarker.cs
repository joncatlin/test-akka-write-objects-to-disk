using System;
using System.Collections.Generic;
using System.Text;

namespace SnapShotStore
{
    [Serializable]
    public class ObjectMarker
    {
        public string PersistenceID { get; set; }
        public long version { get; set; }
    }
}
