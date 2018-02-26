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
        public class Config : Tracking.Config
        {
            public float ImageScale = 1.0f;   // Oversize image to compensate distortion

            public Config()
            {
                FileName = "StypeGrip.json";
            }
        };


        [System.Serializable]
        public class PacketHF : Tracking.Packet
        {
            [System.Serializable]
            public static class DataIndex
            {
                public const short Header = 0;
                public const short Command = 1;
                public const short Timecode = 2;
                public const short PackageNumber = 5;
                public const short X = 6;
                public const short Y = 10;
                public const short Z = 14;
                public const short Pan = 18;
                public const short Tilt = 22;
                public const short Roll = 26;
                public const short FovX = 30;
                public const short AspectRatio = 34;
                public const short Focus = 38;
                public const short Zoom = 42;
                public const short K1 = 46;
                public const short K2 = 50;
                public const short CenterX = 54;
                public const short CenterY = 58;
                public const short ChipWidth = 62;
                public const short Checksum = 66;
                public const short Total = 67;
            };


            public PacketHF()
            {
                data = new byte[DataIndex.Total];

                data[DataIndex.PackageNumber] = 0;                      // package number

                byte[] fov = System.BitConverter.GetBytes(80.0f);              // setup fov_x
                for (int i = 0; i < fov.Length; ++i)
                    data[DataIndex.FovX + i] = fov[i];

                byte[] aspect = System.BitConverter.GetBytes(1.778f);          // setup aspect ratio
                for (int i = 0; i < aspect.Length; ++i)
                    data[DataIndex.AspectRatio + i] = aspect[i];

                byte[] chip_width = System.BitConverter.GetBytes(9.59f);       // Setup chip width
                for (int i = 0; i < chip_width.Length; ++i)
                    data[DataIndex.ChipWidth + i] = chip_width[i];

                Timecode = System.DateTime.Now.Ticks;
            }


            public PacketHF(byte[] packet_data)
            {
                data = new byte[DataIndex.Total];
                System.Array.Copy(packet_data, data, packet_data.Length);

                Timecode = System.DateTime.Now.Ticks;
            }


            // Copy new array of bytes
            public byte[] Data
            {
                set
                {
                    System.Array.Copy(value, data, value.Length);
                }
            }


            // Fixed value: 0x0F
            public char Header
            {
                get
                {
                    return (char)(data[DataIndex.Header]);
                }
            }


            // Commands to the PC
            public char Command
            {
                get
                {
                    return (char)(data[DataIndex.Command]);
                }
            }


            // Timecode data
            long _TimeCode;
            public long Timecode
            {
                get
                {
                    //byte[] time = { 0, data[DataIndex.Timecode], data[DataIndex.Timecode + 1], data[DataIndex.Timecode + 2] };
                    //_TimeCode = System.Convert.ToInt64(System.BitConverter.ToInt32(time, 0));
                    //return System.Convert.ToInt64(System.BitConverter.ToInt32(time, 0));
                    return _TimeCode;
                }

                private set { _TimeCode = value; }
            }


            // Packet ordinary number, start at 0 and loop back when reaches 255
            public override uint Counter
            {
                get
                {
                    return data[DataIndex.PackageNumber];
                }
            }


            // Position in meters
            public UnityEngine.Vector3 Position
            {
                get
                {
                    return new UnityEngine.Vector3(
                        System.BitConverter.ToSingle(data, DataIndex.X),
                        System.BitConverter.ToSingle(data, DataIndex.Y),
                        System.BitConverter.ToSingle(data, DataIndex.Z));
                }
            }

            public float[] XYZ
            {
                get
                {
                    return new float[]{
                        System.BitConverter.ToSingle(data, DataIndex.X),
                        System.BitConverter.ToSingle(data, DataIndex.Y),
                        System.BitConverter.ToSingle(data, DataIndex.Z)};
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

            public float[] PTR
            {
                get
                {
                    return new float[]
                    {
                        System.BitConverter.ToSingle(data, DataIndex.Pan),
                        System.BitConverter.ToSingle(data, DataIndex.Tilt),
                        System.BitConverter.ToSingle(data, DataIndex.Roll)
                    };
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


            // Horizontal field of view
            public float FovX
            {
                get { return System.BitConverter.ToSingle(data, DataIndex.FovX); }
            }


            // Vertical field of view
            public float FovY
            {
                get { return FovX / AspectRatio; }
            }


            // Aspect ratio
            public float AspectRatio
            {
                get { return System.BitConverter.ToSingle(data, DataIndex.AspectRatio); }
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


            // Fisrt radial distortion harmonic (mm^-2)
            public float K1
            {
                get { return System.BitConverter.ToSingle(data, DataIndex.K1); }
            }


            // Second radial distortion harmonic (mm^-4)
            public float K2
            {
                get { return System.BitConverter.ToSingle(data, DataIndex.K2); }
            }


            // Horizontal center shift (im mm)
            public float CenterX
            {
                get { return System.BitConverter.ToSingle(data, DataIndex.CenterX); }
            }


            // Vertical center shift (im mm)
            public float CenterY
            {
                get { return System.BitConverter.ToSingle(data, DataIndex.CenterY); }
            }


            // Projection area width
            public float ChipWidth
            {
                get { return System.BitConverter.ToSingle(data, DataIndex.ChipWidth); }
            }


            // Projection area height
            public float ChipHeight
            {
                get { return ChipWidth / AspectRatio; }
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
                    this.Counter + ' ' + this.Timecode + '\n' +
                    this.Position.ToString() + '\n' + this.Rotation.ToString() + '\n' +
                    this.Zoom + ' ' + this.Focus + '\n' +
                    this.K1 + ' ' + this.K2;
            }
        };


        [System.Serializable]
        public class PacketA5 : Tracking.Packet
        {

            [System.Serializable]
            public static class DataIndex
            {
                public const short Header = 0;
                public const short X = 1;
                public const short Y = 5;
                public const short Z = 9;
                public const short Pan = 13;
                public const short Tilt = 17;
                public const short Roll = 21;
                public const short FovX = 25;
                public const short FovY = 29;
                public const short Focus = 33;
                public const short K1 = 37;
                public const short K2 = 41;
                public const short CenterX = 45;
                public const short CenterY = 49;
                public const short Checksum = 53;
                public const short Total = 54;
            };

            public PacketA5()
            {
                data = new byte[DataIndex.Total];

                byte[] fov_x = System.BitConverter.GetBytes(80.0f);              // setup fov_x
                for (int i = 0; i < fov_x.Length; ++i)
                    data[DataIndex.FovX + i] = fov_x[i];

                byte[] fov_y = System.BitConverter.GetBytes(45.0f);              // setup fov_y
                for (int i = 0; i < fov_y.Length; ++i)
                    data[DataIndex.FovY + i] = fov_y[i];

                //_TimeCode = System.DateTime.Now.Ticks;
            }


            public PacketA5(byte[] packet_data)
            {
                data = new byte[DataIndex.Total];
                System.Array.Copy(packet_data, data, packet_data.Length);
                //_TimeCode = System.DateTime.Now.Ticks;
            }


            // Copy new array of bytes
            public byte[] Data
            {
                set
                {
                    System.Array.Copy(value, data, value.Length);
                }
            }


            // Fixed value: 0x0F
            public char Header
            {
                get
                {
                    return (char)(data[DataIndex.Header]);
                }
            }


            // Commands to the PC
            public char Command
            {
                get
                {
                    return 'x';
                }
            }


            // Timecode data
            long _TimeCode;
            public long Timecode
            {
                get { return _TimeCode; }
                set { _TimeCode = value; }
            }


            // Packet ordinary number, start at 0 and loop back when reaches 255
            public override uint Counter
            {
                get { return 0; }
            }


            // Position in meters
            public UnityEngine.Vector3 Position
            {
                get
                {
                    return new UnityEngine.Vector3(
                        System.BitConverter.ToSingle(data, DataIndex.X),
                        System.BitConverter.ToSingle(data, DataIndex.Y),
                        System.BitConverter.ToSingle(data, DataIndex.Z));
                }
            }

            public float[] XYZ
            {
                get
                {
                    return new float[]{
                        System.BitConverter.ToSingle(data, DataIndex.X),
                        System.BitConverter.ToSingle(data, DataIndex.Y),
                        System.BitConverter.ToSingle(data, DataIndex.Z)};
                }
            }

            
            public float[] PTR
            {
                get
                {
                    return new float[]{
                        System.BitConverter.ToSingle(data, DataIndex.Pan),
                        System.BitConverter.ToSingle(data, DataIndex.Tilt),
                        System.BitConverter.ToSingle(data, DataIndex.Roll)};
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


            // Horizontal field of view
            public float FovX
            {
                get { return System.BitConverter.ToSingle(data, DataIndex.FovX); }
            }


            // Vertical field of view
            public float FovY
            {
                get { return System.BitConverter.ToSingle(data, DataIndex.FovY); }
            }


            // Aspect ratio
            public float AspectRatio
            {
                get { return 1.7778f; }
            }


            // Zoom: 0-wide, 1-tight 
            public float Zoom
            {
                get { return 0; }
            }


            // Focus: 0-close, 1-infinite
            public float Focus
            {
                get { return System.BitConverter.ToSingle(data, DataIndex.Focus); }
            }


            // Fisrt radial distortion harmonic (mm^-2)
            public float K1
            {
                get { return System.BitConverter.ToSingle(data, DataIndex.K1); }
            }


            // Second radial distortion harmonic (mm^-4)
            public float K2
            {
                get { return System.BitConverter.ToSingle(data, DataIndex.K2); }
            }


            // Horizontal center shift (im mm)
            public float CenterX
            {
                get { return System.BitConverter.ToSingle(data, DataIndex.CenterX); }
            }


            // Vertical center shift (im mm)
            public float CenterY
            {
                get { return System.BitConverter.ToSingle(data, DataIndex.CenterY); }
            }


            // Projection area width
            public float ChipWidth
            {
                get { return 9.59f; }
            }


            // Projection area height
            public float ChipHeight
            {
                get { return ChipWidth / AspectRatio; }
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

                    bool isRightHost = (remoteEP.Address.Equals(receivedEP.Address)) || remoteEP.Address.Equals(IPAddress.Any);
                    bool isRightPort = (remoteEP.Port == receivedEP.Port) || remoteEP.Port == 0;

                    //if (!isRightHost || !isRightPort)
                    //    continue;

                    lock (threadLocked)
                    {
                        Buffer.Insert((T)System.Activator.CreateInstance(typeof(T), received_data));
                        TotalCounter++;

                        //System.Console.WriteLine(" ===========================> {0}", received_data.Length);
                        //string[] words = ASCIIEncoding.ASCII.GetString(received_data, 0, received_data.Length).Split(' ');
                        //string[] words = ASCIIEncoding.ASCII.GetString(received_data, 9, received_data.Length - 9).Split(' ');
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
                    if (isRightHost && isRightPort)
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
