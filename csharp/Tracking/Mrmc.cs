using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Tracking
{
    namespace Mrmc
    {
        [System.Serializable]
        public class Config : Tracking.Config
        {
            public int ImageWidth = 1920;
            public int ImageHeight = 1080;

            public Config()
            {
                FileName = "Mrmc.json";
            }
        };


        [System.Serializable]
        public class Packet : Tracking.Packet
        {
            private static System.Diagnostics.Stopwatch TimeWatch = new System.Diagnostics.Stopwatch();
            [System.Serializable]
            public static class DataIndex
            {
                public const short Xv = 0;
                public const short Yv = 4;
                public const short Zv = 8;
                public const short Xt = 12;
                public const short Yt = 16;
                public const short Zt = 20;
                public const short Focus = 24;
                public const short Zoom = 28;
                public const short Header = 32;
                public const short Checksum = 36;
                public const short Counter = 40;
                public const short Total = 44;
            };

            public System.Int64 TimeMilliseconds { get; private set; }

            public Packet()
            {
                if (!TimeWatch.IsRunning)
                    TimeWatch.Start();

                data = new byte[DataIndex.Total];
                //TimeTicks = TimeWatch.ElapsedTicks;
                TimeMilliseconds = TimeWatch.ElapsedMilliseconds;
            }


            public Packet(byte[] packet_data)
            {
                if (!TimeWatch.IsRunning)
                    TimeWatch.Start();

                data = new byte[DataIndex.Total];
                System.Array.Copy(packet_data, data, packet_data.Length);
                //TimeTicks = TimeWatch.ElapsedTicks;
                TimeMilliseconds = TimeWatch.ElapsedMilliseconds;
            }


            // Copy new array of bytes
            public byte[] Data
            {
                set
                {
                    System.Array.Copy(value, data, value.Length);
                }
            }


            public override uint Counter
            {
                get
                {
                    return data[DataIndex.Counter];
                }
            }


            public System.Int32 Header
            {
                get
                {
                    return (data[DataIndex.Header]);
                }
            }

            public System.Int32 Checksum
            {
                get
                {
                    return (data[DataIndex.Checksum]);
                }
            }


            // Camera Position in meters
            public UnityEngine.Vector3 Position
            {
                get
                {
                    //if (System.BitConverter.IsLittleEndian)
                    //    System.Array.Reverse(data); // Convert big endian to little endian

                    return new UnityEngine.Vector3(
                        System.BitConverter.ToSingle(data, DataIndex.Xv),
                        System.BitConverter.ToSingle(data, DataIndex.Yv),
                        System.BitConverter.ToSingle(data, DataIndex.Zv));
                }
            }


            // Target Position in meters
            public UnityEngine.Vector3 Target
            {
                get
                {
                    return new UnityEngine.Vector3(
                        System.BitConverter.ToSingle(data, DataIndex.Xt),
                        System.BitConverter.ToSingle(data, DataIndex.Yt),
                        System.BitConverter.ToSingle(data, DataIndex.Zt));
                }
            }


            


            // Zoom value
            public float Zoom
            {
                get { return System.BitConverter.ToSingle(data, DataIndex.Zoom); }
            }


            // Focus value
            public float Focus
            {
                get { return System.BitConverter.ToSingle(data, DataIndex.Focus); }
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

    }   // end Mrmc namespace

}   // end Tracking namespace
