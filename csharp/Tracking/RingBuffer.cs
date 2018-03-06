using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Tracking
{
   
    public interface IRingBuffer<T>
    {
        int Length { get; }
        int Delay { get; set; }
        void Insert(T packet);
        T Packet { get; }
        T LastPacket { get; }
        T GetPacket(int index);
        long Drops { get; set; }
        void ResetDrops();
    }


    [System.Serializable]
    public class RingBuffer<T> : IRingBuffer<T> where T : Packet
    {
        public const int MinSize = 2;   // 2 is the minimum value (2 fields)
        private CircularBuffer<T> buffer;
        private long dropCount;
        private long lastPacketCounter;


        public RingBuffer(int size)
        {
            buffer = new CircularBuffer<T>(System.Math.Max(MinSize, size));
            buffer.Enqueue((T)System.Activator.CreateInstance(typeof(T)));
            buffer.Enqueue((T)System.Activator.CreateInstance(typeof(T)));

            dropCount = 0;
            lastPacketCounter = System.UInt32.MaxValue;
        }


        public CircularBuffer<T> Data
        {
            get { return buffer; }
        }

        public int Length
        {
            get { return buffer.Count; }
        }

        public int Delay
        {
            get { return buffer.Capacity - MinSize; }     // 2 is the minimum value (2 fields)
            set { buffer.Capacity = value + MinSize; }
        }


        public T Packet
        {
            get
            {
                return buffer[0];
            }
        }

        public T LastPacket
        {
            get
            {
                return buffer[buffer.Count - 1];
            }
        }


        public T GetPacket(int index)
        {
            return buffer[index % buffer.Count];
        }


        public void Insert(T packet)
        {
            buffer.Enqueue(packet);

            if (packet.Counter > lastPacketCounter)
            {
                dropCount += System.Math.Max(packet.Counter - lastPacketCounter - 1, 0);
            }
                        
            lastPacketCounter = packet.Counter;
        }


        public long Drops
        {
            get { return dropCount; }
            set { dropCount = value; }
        }


        public void ResetDrops()
        {
            dropCount = 0;
            lastPacketCounter = buffer[buffer.Count - 1].Counter;
        }
    }
    

}
