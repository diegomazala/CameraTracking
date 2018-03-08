using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Tracking
{
    public class Config
    {
        public string LocalIp = "0.0.0.0";
        public string RemoteIp = "224.0.0.2";
        public bool Multicast = true;
        public int Port = 6302;
        public int Delay = 0;
        public int ReadIntervalMs = 0;
        public bool ConsumeWhileAvailable = false;
        public float FrameRatePerSecond = 59.94f;

        public static string FileName = "Tracking.json";
        

        public virtual bool Load(string filename)
        {
            if (!System.IO.File.Exists(filename))
                return false;

            try
            {
                string json_str = System.IO.File.ReadAllText(filename);
                if (json_str.Length > 0)
                {
                    UnityEngine.JsonUtility.FromJsonOverwrite(json_str, this);
                    return true;
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
            return false;
        }


        public virtual void Save(string filename)
        {
            System.IO.FileInfo fileInfo = new System.IO.FileInfo(filename);
            if (!fileInfo.Exists)
                System.IO.Directory.CreateDirectory(fileInfo.Directory.FullName);

            bool prettyPrint = true;
            string json_str = UnityEngine.JsonUtility.ToJson(this, prettyPrint);
            System.IO.File.WriteAllText(filename, json_str);
        }
    };

    
}
