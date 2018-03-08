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
            public int ImageWidth = 1920;
            public int ImageHeight = 1080;
            public bool LogToFile = false;

            public Config()
            {
                FileName = "StypeGrip.json";
            }
        };

    }   // end StypeGrip namespace

}   // end Tracking namespace
