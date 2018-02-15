using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Tracking
{
    namespace FreeD
    {
        [System.Serializable]
        public class Config : Tracking.Config
        {
            public Config()
            {
                FileName = "FreeD.json";
            }
        };


        [System.Serializable]
        public class Packet : Tracking.Packet
        {
            [System.Serializable]
            public static class DataIndex
            {
                public const short MessageType = 0;
                public const short Id = 1;
                public const short Pan = 2;
                public const short Tilt = 5;
                public const short Roll = 8;
                public const short X = 11;
                public const short Y = 14;  // Depth
                public const short Z = 17;  // Height
                public const short Zoom = 20;
                public const short Focus = 23;
                public const short Spare = 26;
                public const short Checksum = 28;
                public const short Total = 29;
            };


            public Packet()
            {
                data = new byte[DataIndex.Total];
            }


            public Packet(byte[] packet_data)
            {
                data = new byte[DataIndex.Total];
                System.Array.Copy(packet_data, data, packet_data.Length);
            }


            // Copy new array of bytes
            public byte[] Data
            {
                set
                {
                    System.Array.Copy(value, data, value.Length);
                }
            }

            // Packet ordinary number, start at 0 and loop back when reaches 255
            public override uint Counter
            {
                get { return 0; }
            }


            // Fixed value: 0x0F
            public char MessageType
            {
                get
                {
                    return (char)(data[DataIndex.MessageType]);
                }
            }


            // Commands to the PC
            public char Id
            {
                get
                {
                    return (char)(data[DataIndex.Id]);
                }
            }


            // Position in meters
            public UnityEngine.Vector3 Position
            {
                get
                {
                    return new UnityEngine.Vector3(
                        System.BitConverter.ToSingle(data, DataIndex.X),
                        System.BitConverter.ToSingle(data, DataIndex.Z),
                        System.BitConverter.ToSingle(data, DataIndex.Y));
                }
            }

            // Rotation (pan, tilt, roll) in euler angles
            public UnityEngine.Vector3 EulerAngles
            {
                get
                {
                    return new UnityEngine.Vector3(
                        System.BitConverter.ToSingle(data, DataIndex.Tilt),
                        System.BitConverter.ToSingle(data, DataIndex.Pan),
                        System.BitConverter.ToSingle(data, DataIndex.Roll));
                }
            }


            // Rotation (pan, tilt, roll)
            public UnityEngine.Quaternion Rotation
            {
                get
                {
                    return UnityEngine.Quaternion.Euler(EulerAngles);
                }
            }


            // Zoom: 0-wide, 1-tight 
            public float Zoom
            {
                get { return System.BitConverter.ToSingle(data, DataIndex.Zoom); }
            }


            // Focus: 0-close, 1-infinite
            public float Focus
            {
                get { return System.BitConverter.ToSingle(data, DataIndex.Focus); }
            }



            // Unsigned sum of all preceeding bytes
            public byte Checksum
            {
                get
                {
                    return (data[DataIndex.Checksum]);
                }
            }


            // Verify if the package is valid
            public override bool IsValid
            {
                get
                {
                    byte check = 0;
                    for (byte x = 0; x < DataIndex.Checksum; x++)
                        check += data[x];

                    return (check == Checksum);
                }
            }

            public override string ToString()
            {
                return 
                    this.MessageType + ' ' + Id + '\n' +
                    this.Position.ToString() + '\n' + this.Rotation.ToString() + '\n' +
                    this.Zoom + ' ' + this.Focus;
            }
        };



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
                    TotalCounter = 0;
                }

                //
                // Receive the very first packages in order to reset the counters
                // 
                byte[] received_data = client.Receive(ref receivedEP);



                if (received_data.Length == Packet.DataIndex.Total)    //  Is a valid packet
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

                    bool isRightHost = (remoteEP.Address.Equals(receivedEP.Address)) || remoteEP.Address.Equals(IPAddress.Any);
                    bool isRightPort = (remoteEP.Port == receivedEP.Port) || remoteEP.Port == 0;

                    if (!isRightHost || !isRightPort)
                        continue;

                    lock (threadLocked)
                    {
                        Buffer.Insert((T)System.Activator.CreateInstance(typeof(T), received_data));
                        TotalCounter++;

                        //System.Console.WriteLine(" ===========================> {0}", received_data.Length);
                        //string[] words = ASCIIEncoding.ASCII.GetString(received_data, 0, received_data.Length).Split(' ');
                        ////string[] words = ASCIIEncoding.ASCII.GetString(received_data, 9, received_data.Length - 9).Split(' ');
                        //for (int i = 0; i < words.Length; ++i)
                        //{
                        //    System.Console.WriteLine("{0} - {1}", i, words[i]);
                        //}
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
                    if (isRightHost && isRightPort && data.Length > 0)
                    {
                        Buffer.Insert((T)System.Activator.CreateInstance(typeof(T), data));
                        TotalCounter++;
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
    }   // end StypeGrip namespace

}   // end Tracking namespace
