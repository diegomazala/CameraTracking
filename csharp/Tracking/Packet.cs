using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Tracking
{
   
    public abstract class Packet
    {
        protected byte[] data = null;

        // The id/counter of the package
        long _Counter;
        public virtual long Counter { get { return _Counter; } set { _Counter = value; } }

        // Timecode data
        protected long _TimeCode;
        public virtual long Timecode
        {
            get { return _TimeCode; }
            protected set { _TimeCode = value; }
        }

        // Verify if the package is valid
        public abstract bool IsValid { get; }
    }

    
}
