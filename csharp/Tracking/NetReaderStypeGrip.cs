using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Tracking
{
    namespace StypeGrip
    {

        [System.Serializable]
        public class NetReader<T> : Tracking.NetReader<T>
        {
            protected override void ReadDataThread()
            {
                IPEndPoint receivedEP = remoteEP;

                bool consume_while_available = Config.ConsumeWhileAvailable;
                int read_interval_ms = Config.ReadIntervalMs;

                lock (threadLocked)
                {
                    threadRunning = true;
                }

                //
                // Receive the very first packages in order to reset the counters
                // 
                byte[] received_data = client.Receive(ref receivedEP);


                if (received_data.Length == PacketHF.DataIndex.Total 
                    || received_data.Length == PacketA5.DataIndex.Total)    //  Is a valid packet
                {
                    Buffer.Insert((T)System.Activator.CreateInstance(typeof(T), received_data));
                }
                else
                {
                    System.Console.WriteLine("Unknown Format/Encoding");
                }


                Buffer.ResetDrops();

                while (threadRunning)
                {
                    if (read_interval_ms > 0)
                        Thread.Sleep(read_interval_ms);

                    if (client.Available < 1)
                        continue;

                    received_data = client.Receive(ref receivedEP);

                    while (consume_while_available && client.Available > 0)
                    {
                        received_data = client.Receive(ref receivedEP);
                    }

                    if (!Config.Multicast)
                    {
                        bool isRightHost = (remoteEP.Address.Equals(receivedEP.Address)) || remoteEP.Address.Equals(IPAddress.Any);
                        bool isRightPort = (remoteEP.Port == receivedEP.Port) || remoteEP.Port == 0;

                        if (!isRightHost || !isRightPort)
                            continue;
                    }

                    lock (threadLocked)
                    {
                        Buffer.Insert((T)System.Activator.CreateInstance(typeof(T), received_data));
                    }
                }
            }
        }


        [System.Serializable]
        public class NetReaderAsync<T> : Tracking.NetReaderAsync<T>
        {
            public override void Connect(Tracking.Config config, Tracking.IRingBuffer<T> ringBuffer)
            {
                base.Connect(config, ringBuffer);

                // Wait a bit to reset the number of drops in ring buffer
                Thread.Sleep(50);
                Buffer.ResetDrops();
            }


            protected override void DataReceived(System.IAsyncResult ar)
            {
                UdpState state = (UdpState)ar.AsyncState;

                try
                {
                    IPEndPoint wantedIpEndPoint = (IPEndPoint)state.RemoteEndPoint;
                    IPEndPoint receivedIpEndPoint = (IPEndPoint)state.LocalEndPoint; ;

                    System.Byte[] data = state.Client.EndReceive(ar, ref receivedIpEndPoint);

                    // Check sender
                    bool isRightHost = (wantedIpEndPoint.Address.Equals(receivedIpEndPoint.Address)) || wantedIpEndPoint.Address.Equals(IPAddress.Any);
                    bool isRightPort = (wantedIpEndPoint.Port == receivedIpEndPoint.Port) || wantedIpEndPoint.Port == 0;
                    if (isRightHost && isRightPort)
                    {
                        Buffer.Insert((T)System.Activator.CreateInstance(typeof(T), data));
                    }

                    // Restart listening for udp data packages
                    state.Client.BeginReceive(new System.AsyncCallback(DataReceived), ar.AsyncState);
                }
                catch (System.ObjectDisposedException)
                {
                    // No problem. Socket has been closed.
                    //System.Console.WriteLine(e.ToString());
                }
                catch (System.Exception e)
                {
                    System.Console.WriteLine(e.ToString());
                }
            }

        }

        [System.Serializable]
        public class NetReaderSync<T> : Tracking.NetReaderSync<T>
        {
            public override void Connect(Tracking.Config config, Tracking.IRingBuffer<T> ringBuffer)
            {
                base.Connect(config, ringBuffer);

                // Wait a bit to reset the number of drops in ring buffer
                Thread.Sleep(50);
                Buffer.ResetDrops();
            }
        }
    }   // end StypeGrip namespace

}   // end Tracking namespace
