using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Tracking
{
    namespace Xcito
    {
        [System.Serializable]
        public class CameraConstants
        {
            private short HeaderSize = 8;

            public int Id;                         // == 0 if not explicitly specified 

            public int ImageWidth = 1920;
            public int ImageHeight = 1080;
            public int BlankLeft = 0;
            public int BlankRight = 0;
            public int BlankTop = 0;
            public int BlankBottom = 0;
            public float ChipWidth = 9.59f;
            public float ChipHeight = 5.39f;
            public float FakeChipWidth = 9.59f;
            public float FakeChipHeight = 5.39f;

            public float AspectRatio
            {
                get { return ChipWidth / ChipHeight; }
            }

            public CameraConstants()
            { }

            public CameraConstants(byte[] packet_data)
            {
                //string[] words = ASCIIEncoding.ASCII.GetString(packet_data, HeaderSize, packet_data.Length - HeaderSize).Split(' ');

                //if (packet_data[7] == 'A')  // ASCII
                //{
                //    System.Int32.TryParse(words[0], out ImageWidth);
                //    System.Int32.TryParse(words[1], out ImageHeight);
                //    System.Int32.TryParse(words[2], out BlankLeft);
                //    System.Int32.TryParse(words[3], out BlankRight);
                //    System.Int32.TryParse(words[4], out BlankTop);
                //    System.Int32.TryParse(words[5], out BlankBottom);
                //    System.Single.TryParse(words[6], out ChipWidth);
                //    System.Single.TryParse(words[7], out ChipHeight);
                //    System.Single.TryParse(words[8], out FakeChipWidth);
                //    System.Single.TryParse(words[9], out FakeChipHeight);
                //}

                FromByte(packet_data);
            }

            public bool FromByte(byte[] packet_data)
            {
                if (packet_data[6] == 'C' && packet_data[7] == 'A')
                {
                    string[] words = ASCIIEncoding.ASCII.GetString(packet_data, HeaderSize, packet_data.Length - HeaderSize).Split(' ');

                    return
                        System.Int32.TryParse(words[0], out ImageWidth) &&
                        System.Int32.TryParse(words[1], out ImageHeight) &&
                        System.Int32.TryParse(words[2], out BlankLeft) &&
                        System.Int32.TryParse(words[3], out BlankRight) &&
                        System.Int32.TryParse(words[4], out BlankTop) &&
                        System.Int32.TryParse(words[5], out BlankBottom) &&
                        System.Single.TryParse(words[6], out ChipWidth) &&
                        System.Single.TryParse(words[7], out ChipHeight) &&
                        System.Single.TryParse(words[8], out FakeChipWidth) &&
                        System.Single.TryParse(words[9], out FakeChipHeight);
                }
                else
                {
                    // It is not a valid packet
                    return false;
                }
            }
        };


        [System.Serializable]
        public class CameraParams
        {
            private short HeaderSize = 9;

            public int Id;                          // == 0 if not explicitly specified 
            public uint Format;                     // bit mask of format options 

            public double Fov = 60.0f;              // field of view or image distance 
            public double CenterX = 0.0f;           // center shift X
            public double CenterY = 0.0f;           // center shift Y
            public double K1 = 0.0f;                // distortion coefficients
            public double K2 = 0.0f;                // distortion coefficients
            public double FocalDistance = 0.0f;     // focal distance
            public double Aperture = 0.0f;          // aperture
            //public double[] Matrix = new double[12] { 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0 };
            public double[] Matrix = new double[12] { 0.61, 0.0, -0.79, -0.45, 0.81, -0.35, 0.64, 0.57, 0.49, 2.19, 2.35, 1.24 };

            public uint Counter { get; protected set; }

            public CameraParams()
            { }

            public CameraParams(byte[] packet_data)
            {
                string[] words = ASCIIEncoding.ASCII.GetString(packet_data, HeaderSize, packet_data.Length - HeaderSize).Split(' ');

                int index = 1;
                if (packet_data[7] == 'A')  // ASCII
                {
                    System.UInt32.TryParse(System.BitConverter.ToString(packet_data, 8, 1).Replace("-", string.Empty), out Format);

                    if (Xcito.Format.Euler(Format))
                    {
                        System.Double.TryParse(words[index++], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out Matrix[0]);
                        System.Double.TryParse(words[index++], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out Matrix[1]);
                        System.Double.TryParse(words[index++], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out Matrix[2]);

                        System.Double.TryParse(words[index++], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out Matrix[3]);
                        System.Double.TryParse(words[index++], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out Matrix[4]);
                        System.Double.TryParse(words[index++], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out Matrix[5]);
                    }
                    else
                    {
                        for (int i = 0; i < 12; ++i)
                        {
                            System.Double.TryParse(words[index++], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out Matrix[i]);
                        }
                    }

                    System.Double.TryParse(words[index++], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out Fov);
                    System.Double.TryParse(words[index++], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out CenterX);
                    System.Double.TryParse(words[index++], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out CenterY);
                    System.Double.TryParse(words[index++], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out K1);
                    System.Double.TryParse(words[index++], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out K2);
                    System.Double.TryParse(words[index++], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out FocalDistance);
                    System.Double.TryParse(words[index++], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out Aperture);

                    uint count = 0;
                    if (System.UInt32.TryParse(words[index++], out count))
                        Counter = count;
                }
            }

        };


        [System.Serializable]
        public class Packet : Tracking.Packet
        {
            public CameraParams Params = null;


            public Packet()
            {
                Params = new CameraParams();
            }


            public Packet(byte[] packet_data_params)
            {
                Params = new CameraParams(packet_data_params);
            }


            // Packet ordinary number, start at 0 and loop back when reaches 255
            public override uint Counter
            {
                get { return Params.Counter; }
            }

            
            // Verify if the package is valid
            public override bool IsValid
            {
                get
                {
                    return true;
                }
            }

            // Position in meters
            public UnityEngine.Vector3 Position
            {
                get
                {
                    if (!Xcito.Format.Euler(Params.Format))     // Matrix
                    {
                        return new UnityEngine.Vector3((float)Params.Matrix[9], (float)Params.Matrix[10], -(float)Params.Matrix[11]);
                    }
                    else                                       // Euler
                    {
                        return new UnityEngine.Vector3((float)Params.Matrix[0], (float)Params.Matrix[1], (float)Params.Matrix[2]);
                    }
                }
            }


            // Position in meters
            public UnityEngine.Vector3 EulerAngles
            {
                get
                {
                    if (!Xcito.Format.Euler(Params.Format))     // Matrix
                    {
                        return Rotation.eulerAngles;
                    }
                    else                                       // Euler
                    {
                        return new UnityEngine.Vector3((float)Params.Matrix[4], (float)Params.Matrix[3], (float)Params.Matrix[5]);
                    }
                }
            }

            public UnityEngine.Quaternion Rotation
            {
                get
                {
                    if (!Xcito.Format.Euler(Params.Format))     // Matrix
                    {
                        UnityEngine.Vector4 col1 = new UnityEngine.Vector4((float)Params.Matrix[3], (float)Params.Matrix[4], (float)Params.Matrix[5], 0);
                        UnityEngine.Vector4 col2 = new UnityEngine.Vector4((float)Params.Matrix[6], (float)Params.Matrix[7], (float)Params.Matrix[8], 0);
                        UnityEngine.Quaternion rot = UnityEngine.Quaternion.LookRotation(col2, col1);
                        rot.z *= -1.0f;
                        rot.w *= -1.0f;
                        return rot;
                        //return UnityEngine.Quaternion.LookRotation(col2, col1);
                    }
                    else                                       // Euler
                    {
                        return UnityEngine.Quaternion.Euler(EulerAngles);
                    }
                }
            }
            

            // Vertical field of view
            public float FovY
            {
                get { return (float)Params.Fov; }
            }

            // Fisrt radial distortion harmonic (mm^-2)
            public float K1
            {
                get { return (float)Params.K1; }
            }


            // Second radial distortion harmonic (mm^-4)
            public float K2
            {
                get { return (float)Params.K2; }
            }


            // Horizontal center shift (im mm)
            public float CenterX
            {
                get { return (float)Params.CenterX; }
            }


            // Vertical center shift (im mm)
            public float CenterY
            {
                get { return (float)Params.CenterY; }
            }

        };

    }   // end Xcito namespace
}   // end Tracking namespace
