using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Tracking
{
    public class Config
    {
        public string LocalIp = "0.0.0.0";
        public string RemoteIp = "0.0.0.0";
        public bool Multicast = false;
        public int Port = 12000; //6301;
        public int Delay = 0;
        public int ReadIntervalMs = 10;
        public bool ConsumeWhileAvailable = true;

        public static string FileName = "Tracking.json";
        

        public virtual bool Load(string filename)
        {
            if (!System.IO.File.Exists(filename))
                return false;

            try
            {
                string json_str = System.IO.File.ReadAllText(filename);
                if (json_str.Length > 0)
                {
                    UnityEngine.JsonUtility.FromJsonOverwrite(json_str, this);
                    return true;
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
            return false;
        }


        public virtual void Save(string filename)
        {
            System.IO.FileInfo fileInfo = new System.IO.FileInfo(filename);
            if (!fileInfo.Exists)
                System.IO.Directory.CreateDirectory(fileInfo.Directory.FullName);

            bool prettyPrint = true;
            string json_str = UnityEngine.JsonUtility.ToJson(this, prettyPrint);
            System.IO.File.WriteAllText(filename, json_str);
        }
    };


    public abstract class Packet
    {
        protected byte[] data = null;

        // The id/counter of the package
        public abstract uint Counter { get; }

        // Verify if the package is valid
        public abstract bool IsValid { get; }
    }


    public interface IRingBuffer<T>
    {
        int Length { get; }
        int Delay { get; set; }
        void Insert(T packet);
        T Packet { get; }
        T GetPacket(int index);
        uint Drops { get; }
        void ResetDrops();
    }


    [System.Serializable]
    public class RingBuffer<T> : IRingBuffer<T> where T : Packet
    {
        public const int MinSize = 2;   // 2 is the minimum value (2 fields)
        private CircularBuffer<T> buffer;
        private uint dropCount;
        private uint lastPacketCounter;


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


        public uint Drops
        {
            get { return dropCount; }
        }


        public void ResetDrops()
        {
            dropCount = 0;
            lastPacketCounter = buffer[buffer.Count - 1].Counter;
        }
    }


    [System.Serializable]
    public abstract class INetReader<T>
    {
        public int PackageSize = 0;
        public int PackageAccum = 0;
        public int TotalCounter = 0;

        protected IPEndPoint localEP = null;
        protected IPEndPoint remoteEP = null;
        protected UdpClient client = null;

        public Tracking.Config Config { get; set; }
        public Tracking.IRingBuffer<T> Buffer { get; set; }

        public abstract void Connect(Tracking.Config config, Tracking.IRingBuffer<T> ringBuffer);
        public abstract void Disconnect();
        public abstract bool IsReading { get; }

        public virtual void Connect()
        {
            Connect(Config, Buffer);
        }

        public virtual int ReadNow()
        {
            return 0;
        }
    }



    [System.Serializable]
    public class NetReader<T> : INetReader<T>
    {
        protected Thread readThread = null;
        protected static readonly System.Object threadLocked = new System.Object();
        protected volatile bool threadRunning = false;
        

        public override void Connect(Tracking.Config config, Tracking.IRingBuffer<T> ringBuffer)
        {
            try
            { 
                Disconnect();

                Config = config;
                Buffer = ringBuffer;

                if (Buffer == null)
                {
                    //UnityEngine.Debug.LogError("Missing reference to StpeGrip.RingBuffer");
                    return;
                }

                if (Config == null)
                {
                    //UnityEngine.Debug.LogError("Missing reference to StypeGrip.Config");
                    return;
                }

                // Update delay value
                Buffer.Delay = Config.Delay;

                localEP = new IPEndPoint(IPAddress.Parse(Config.LocalIp), Config.Port);
                remoteEP = new IPEndPoint(IPAddress.Parse(Config.RemoteIp), 0);

                if (Config.Multicast)
                {
                    client = new UdpClient();
                    client.Client.Bind(localEP);
                    client.JoinMulticastGroup(IPAddress.Parse(Config.RemoteIp));
                }
                else
                {
                    client = new UdpClient(localEP);
                }

                readThread = new Thread(ReadDataThread) { Name = "Read Thread" };
                readThread.Start();
            }
            catch (System.Exception e)
            {
                System.Console.WriteLine(e.ToString());
            }
        }


        public override void Disconnect()
        {
            try
            { 
                threadRunning = false;
            
                if (readThread != null)
                {
                    readThread.Join(33);
                    if (readThread.IsAlive)
                        readThread.Abort();
                    readThread = null;
                }

                if (client != null)
                {
                    client.Close();
                    client = null;
                }
            }
            catch (System.Exception e)
            {
                System.Console.WriteLine(e.ToString());
            }
        }


        public override bool IsReading
        {
            get
            {
                if (readThread == null)
                    return false;
                else
                    return readThread.IsAlive;
            }
        }


        protected virtual void ReadDataThread()
        {
            bool consume_while_available = Config.ConsumeWhileAvailable;
            int read_interval_ms = Config.ReadIntervalMs;

            lock (threadLocked)
            {
                threadRunning = true;
                TotalCounter = 0;
            }

            Buffer.ResetDrops();

            while (threadRunning)
            {
                if (read_interval_ms > 0)
                    Thread.Sleep(read_interval_ms);

                if (client.Available < 1)
                    continue;

                byte[] received_data = client.Receive(ref remoteEP);

                if (consume_while_available)
                {
                    while (client.Available > 0)
                        received_data = client.Receive(ref remoteEP);
                }

                lock (threadLocked)
                {
                    PackageSize = received_data.Length;
                    TotalCounter++;

                    Buffer.Insert((T)System.Activator.CreateInstance(typeof(T), received_data));
                }
            }
        }
    }


    [System.Serializable]
    public class NetReaderSync<T> : INetReader<T>
    {
        public override void Connect(Tracking.Config config, Tracking.IRingBuffer<T> ringBuffer)
        {
            try
            {
                Disconnect();

                Config = config;
                Buffer = ringBuffer;

                if (Buffer == null)
                {
                    //UnityEngine.Debug.LogError("Missing reference to StpeGrip.RingBuffer");
                    return;
                }

                if (Config == null)
                {
                    //UnityEngine.Debug.LogError("Missing reference to StypeGrip.Config");
                    return;
                }

                // Update delay value
                Buffer.Delay = Config.Delay;

                localEP = new IPEndPoint(IPAddress.Parse(Config.LocalIp), Config.Port);
                remoteEP = new IPEndPoint(IPAddress.Parse(Config.RemoteIp), 0);

                if (Config.Multicast)
                {
                    client = new UdpClient();
                    client.Client.Bind(localEP);
                    client.JoinMulticastGroup(IPAddress.Parse(Config.RemoteIp));
                }
                else
                {
                    client = new UdpClient(localEP);
                }

            }
            catch (System.Exception e)
            {
                System.Console.WriteLine(e.ToString());
            }
        }


        public override void Disconnect()
        {
            try
            {
                if (client != null)
                {
                    client.Close();
                    client = null;
                }
            }
            catch (System.Exception e)
            {
                System.Console.WriteLine(e.ToString());
            }
        }


        public override bool IsReading
        {
            get
            {
                return false;
            }
        }


        public override int ReadNow()
        {
            bool consume_while_available = Config.ConsumeWhileAvailable;
            int read_interval_ms = Config.ReadIntervalMs;

            if (client.Available < 1)
                return 0;

            byte[] received_data = client.Receive(ref remoteEP);

            if (consume_while_available)
            {
                while (client.Available > 0)
                    received_data = client.Receive(ref remoteEP);
            }

            PackageSize = received_data.Length;
            TotalCounter++;

            Buffer.Insert((T)System.Activator.CreateInstance(typeof(T), received_data));

            return received_data.Length;
        }
    }




    internal class UdpState
    {
        internal UdpState(UdpClient c, IPEndPoint localEP, IPEndPoint remoteEP)
        {
            this.Client = c;
            this.LocalEndPoint = localEP;
            this.RemoteEndPoint = remoteEP;
        }

        internal UdpClient Client;
        internal IPEndPoint LocalEndPoint;
        internal IPEndPoint RemoteEndPoint;
    }

    [System.Serializable]
    public class NetReaderAsync<T> : INetReader<T>
    {
        public override void Connect(Tracking.Config config, Tracking.IRingBuffer<T> ringBuffer)
        {
            try
            { 
                Disconnect();

                Config = config;
                Buffer = ringBuffer;

                if (Buffer == null)
                {
                    //UnityEngine.Debug.LogError("Missing reference to RingBuffer");
                    return;
                }

                if (Config == null)
                {
                    //UnityEngine.Debug.LogError("Missing reference to Config");
                    return;
                }

                // Update delay value
                Buffer.Delay = Config.Delay;


                localEP = new IPEndPoint(IPAddress.Parse(Config.LocalIp), Config.Port);
                remoteEP = new IPEndPoint(IPAddress.Parse(Config.RemoteIp), 0);

                if (Config.Multicast)
                {
                    client = new UdpClient();
                    client.Client.Bind(localEP);
                    client.JoinMulticastGroup(IPAddress.Parse(Config.RemoteIp));
                }
                else
                {
                    client = new UdpClient(localEP);
                }

                UdpState state = new UdpState(client, localEP, remoteEP);


                // Start async receiving
                client.BeginReceive(new System.AsyncCallback(DataReceived), state);
            }
            catch (System.Exception e)
            {
                System.Console.WriteLine(e.ToString());
            }
        }


        public override void Disconnect()
        {
            try
            {
                if (client != null)
                {
                    client.Close();
                    client = null;
                }
            }
            catch (System.Exception e)
            {
                System.Console.WriteLine(e.ToString());
            }
        }


        public override bool IsReading
        {
            get
            {
                return (client != null);
            }
        }


        protected virtual void DataReceived(System.IAsyncResult ar)
        {
            UdpState state = (UdpState)ar.AsyncState;
            UdpClient clnt = (UdpClient)state.Client;

            try
            {
                IPEndPoint wantedIpEndPoint = (IPEndPoint)state.RemoteEndPoint;
                IPEndPoint receivedIpEndPoint = (IPEndPoint)state.LocalEndPoint;
                System.Byte[] data = clnt.EndReceive(ar, ref receivedIpEndPoint);

                
                
                // Check sender
                bool isRightHost = (wantedIpEndPoint.Address.Equals(receivedIpEndPoint.Address)) || wantedIpEndPoint.Address.Equals(IPAddress.Any);
                bool isRightPort = (wantedIpEndPoint.Port == receivedIpEndPoint.Port) || wantedIpEndPoint.Port == 0;
                if (isRightHost && isRightPort)
                {
                    TotalCounter++;
                    //buffer.Insert((T)System.Activator.CreateInstance(typeof(T), data), Counter);
                }

                // Restart listening for udp data packages
                clnt.BeginReceive(new System.AsyncCallback(DataReceived), ar.AsyncState);
            }
            catch (System.Exception e)
            {
                System.Console.WriteLine(e.ToString());
            }
        }
    }


}
