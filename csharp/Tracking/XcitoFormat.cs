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
        public class Format
        {
            public const uint trkMatrix = 0x0000;
            public const uint trkEuler = 0x0001;

            public const uint trkCameraToStudio = 0x0000;
            public const uint trkStudioToCamera = 0x0002;

            public const uint trkCameraZ_Up = 0x0000;
            public const uint trkCameraY_Up = 0x0004;

            public const uint trkStudioZ_Up = 0x0000;
            public const uint trkStudioY_Up = 0x0008;

            public const uint trkImageDistance = 0x0000;
            public const uint trkFieldOfView = 0x0010;

            public const uint trkHorizontal = 0x0000;
            public const uint trkVertical = 0x0020;
            public const uint trkDiagonal = 0x0040;

            public const uint trkConsiderBlank = 0x0000;
            public const uint trkIgnoreBlank = 0x0080;

            public const uint trkAdjustAspect = 0x0000;
            public const uint trkKeepAspect = 0x0100;

            public const uint trkShiftOnChip = 0x0000;
            public const uint trkShiftInPixels = 0x0200;

            public static bool Euler(uint format)
            {
                return System.Convert.ToBoolean(format & trkEuler);
            }

            public static bool StudioToCamera(uint format)
            {
                return System.Convert.ToBoolean(format & trkStudioToCamera);
            }

            public static bool CameraYUp(uint format)
            {
                return System.Convert.ToBoolean(format & trkCameraY_Up);
            }

            public static bool StudioYUp(uint format)
            {
                return System.Convert.ToBoolean(format & trkStudioY_Up);
            }

            public static bool FieldOfView(uint format)
            {
                return System.Convert.ToBoolean(format & trkFieldOfView);
            }

            public static bool FovVertical(uint format)
            {
                return System.Convert.ToBoolean(format & trkVertical);
            }

            public static bool FovDiagonal(uint format)
            {
                return System.Convert.ToBoolean(format & trkDiagonal);
            }

            public static bool IgnoreBlank(uint format)
            {
                return System.Convert.ToBoolean(format & trkIgnoreBlank);
            }

            public static bool KeepAspect(uint format)
            {
                return System.Convert.ToBoolean(format & trkKeepAspect);
            }

            public static bool ShiftInPixels(uint format)
            {
                return System.Convert.ToBoolean(format & trkShiftInPixels);
            }

            public static string ToString(uint format)
            {
                string str = "Format: ";

                if (System.Convert.ToBoolean(format & trkEuler))
                    str += "euler";
                else
                {
                    str += "matrix";
                    if (System.Convert.ToBoolean(format & trkStudioToCamera))
                        str += " studio_to_camera";
                    else
                        str += " camera_to_studio";
                    if (System.Convert.ToBoolean(format & trkCameraY_Up))
                        str += " camera_y_up";
                    else
                        str += " camera_z_up";
                }

                if (System.Convert.ToBoolean(format & trkStudioY_Up))
                    str += " studio_y_up";
                else
                    str += " studio_z_up";

                if (System.Convert.ToBoolean(format & trkFieldOfView))
                {
                    str += " field_of_view";
                    if (System.Convert.ToBoolean(format & trkVertical))
                        str += " vertical";
                    else if (System.Convert.ToBoolean(format & trkDiagonal))
                        str += " diagonal";
                    else
                        str += " horizontal";
                }
                else
                    str += " image_distance";


                if (System.Convert.ToBoolean(format & trkIgnoreBlank))
                {
                    str += " ignore_blank";
                    if (System.Convert.ToBoolean(format & trkKeepAspect))
                        str += " keep_aspect";
                    else
                        str += " adjust_aspect";
                }
                else
                    str += " consider_blank";

                if (System.Convert.ToBoolean(format & trkShiftInPixels))
                    str += " shift_in_pixels";
                else
                    str += " shift_on_chip";

                return str;
            }
        }





    } // end Xcito namespace

}   // end Tracking namespace
