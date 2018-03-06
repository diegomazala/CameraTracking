using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Tracking
{
    
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

        protected virtual void OnReceiveData(byte[] received_data)
        {
            if (received_data.Length > 0)
            {
                Buffer.Insert((T)System.Activator.CreateInstance(typeof(T), received_data));
                PackageSize = received_data.Length;
                TotalCounter++;
            }
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
                    //client.ExclusiveAddressUse = false;
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
                lock (threadLocked)
                {
                    OnReceiveData(client.Receive(ref remoteEP));
                }


                if (consume_while_available)
                {
                    while (client.Available > 0)
                    {
                        lock (threadLocked)
                        {
                            OnReceiveData(client.Receive(ref remoteEP));
                        }
                    }
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
            OnReceiveData(received_data);

            if (consume_while_available)
            {
                while (client.Available > 0)
                {
                    OnReceiveData(client.Receive(ref remoteEP));
                }
            }

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
                System.Byte[] received_data = clnt.EndReceive(ar, ref receivedIpEndPoint);

                
                
                // Check sender
                bool isRightHost = (wantedIpEndPoint.Address.Equals(receivedIpEndPoint.Address)) || wantedIpEndPoint.Address.Equals(IPAddress.Any);
                bool isRightPort = (wantedIpEndPoint.Port == receivedIpEndPoint.Port) || wantedIpEndPoint.Port == 0;
                if (isRightHost && isRightPort)
                {
                    OnReceiveData(received_data);
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
