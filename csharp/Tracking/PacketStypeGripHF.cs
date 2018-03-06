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
            public override long Timecode
            {
                get
                {
                    //byte[] time = { 0, data[DataIndex.Timecode], data[DataIndex.Timecode + 1], data[DataIndex.Timecode + 2] };
                    //_TimeCode = System.Convert.ToInt64(System.BitConverter.ToInt32(time, 0));
                    //return System.Convert.ToInt64(System.BitConverter.ToInt32(time, 0));
                    return _TimeCode;
                }

                protected set { _TimeCode = value; }
            }


            // Packet ordinary number, start at 0 and loop back when reaches 255
            public override long Counter
            {
                get
                {
                    return (long)((uint)data[DataIndex.PackageNumber]);
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
                        -System.BitConverter.ToSingle(data, DataIndex.Z));  // invert z
                }
            }

            public float[] XYZ
            {
                get
                {
                    return new float[]{
                        System.BitConverter.ToSingle(data, DataIndex.X),
                        System.BitConverter.ToSingle(data, DataIndex.Y),
                        -System.BitConverter.ToSingle(data, DataIndex.Z)};  // invert z
                }
            }


            // Rotation (tilt, pan, roll) in euler angles
            public UnityEngine.Vector3 EulerAngles
            {
                get
                {
                    return new UnityEngine.Vector3(
                        System.BitConverter.ToSingle(data, DataIndex.Tilt),
                        -System.BitConverter.ToSingle(data, DataIndex.Pan),  // invert pan
                        System.BitConverter.ToSingle(data, DataIndex.Roll));
                }
            }

            public float[] PTR
            {
                get
                {
                    return new float[]
                    {
                        -System.BitConverter.ToSingle(data, DataIndex.Pan),
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
                    float pan = -System.BitConverter.ToSingle(data, DataIndex.Pan); // invert pan 
                    float tilt = System.BitConverter.ToSingle(data, DataIndex.Tilt);
                    float roll = System.BitConverter.ToSingle(data, DataIndex.Roll);
                    UnityEngine.Quaternion x = UnityEngine.Quaternion.AngleAxis(tilt, UnityEngine.Vector3.right);
                    UnityEngine.Quaternion y = UnityEngine.Quaternion.AngleAxis(pan, UnityEngine.Vector3.up);
                    UnityEngine.Quaternion z = UnityEngine.Quaternion.AngleAxis(roll, UnityEngine.Vector3.forward);

                    // This is not the default rotation order in unity
                    // Pan * Tilt * Roll
                    return y * x * z;
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


    }   // end StypeGrip namespace

}   // end Tracking namespace
