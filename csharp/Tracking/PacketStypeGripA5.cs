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

                Timecode = System.DateTime.Now.Ticks;
            }


            public PacketA5(byte[] packet_data)
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
                    return 'x';
                }
            }


            // Timecode data
            public override long Timecode
            {
                get { return _TimeCode; }
                protected set { _TimeCode = value; }
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
                get { return 5.39f; }
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

    }   // end StypeGrip namespace

}   // end Tracking namespace
