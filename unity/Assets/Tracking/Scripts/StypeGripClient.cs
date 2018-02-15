using UnityEngine;
using System.Collections;

using StypeGripPacket = Tracking.StypeGrip.PacketHF;
//using StypeGripPacket = Tracking.StypeGrip.PacketA5;

public class StypeGripClient : MonoBehaviour
{
    public int PackSize = 0;
    public int PackCount = 0;
    

    public Camera[] TargetCamera = { null, null };

    public Tracking.StypeGrip.Config config;

    private Tracking.RingBuffer<StypeGripPacket> ringBuffer;
    public Tracking.NetReader<StypeGripPacket> netReader = null;


    private StypeGripClientUI stypeClientUI = null;
    public StypeGripClientUI UI
    {
        set
        {
            stypeClientUI = value;
            stypeClientUI.netReader = netReader;
            if (stypeClientUI.imageScaleText)
            {
                stypeClientUI.imageScaleText.text = config.ImageScale.ToString("0.000");
            }
        }

        get
        {
            return stypeClientUI;
        }
    }


    public static string ConfigFile
    {
        get
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(64);
            sb.Append(UnityEngine.Application.dataPath);
            sb.Append(@"/");
#if !UNITY_EDITOR
            sb.Append(@"../");
#endif
            sb.Append(@"Config/Tracking/");

            return (new System.IO.DirectoryInfo(sb.ToString())).FullName + Tracking.StypeGrip.Config.FileName;
        }
    }


    public bool Distortion
    {
        set
        {
            foreach (Camera c in TargetCamera)
            {
                if (c == null)
                {
                    Debug.LogError("StypeGripClient: Missing camera");
                    continue;
                }

                StypeGripDistortion dist = c.GetComponent<StypeGripDistortion>();
                if (dist != null && dist.enabled != value)
                    dist.enabled = value;
            }
        }

        get
        {
            if (TargetCamera.Length > 0)
            {
                StypeGripDistortion dist = TargetCamera[0].GetComponent<StypeGripDistortion>();
                if (dist != null)
                    return dist.enabled;
                else
                    return false;
            }
            else
            {
                return false;
            }
        }
    }

    void Awake()
    {
        netReader = new Tracking.NetReader<StypeGripPacket>();
        netReader.Config = config;
        ringBuffer = new Tracking.RingBuffer<StypeGripPacket>(config.Delay);
        netReader.Buffer = ringBuffer;
    }


    void OnEnable()
    {
        // Try to load a configuration file
        // If didn't find a config file, create a default
#if UNITY_EDITOR
        //if (!netReader.Config.Load(ConfigFile))
        //  netReader.Config.Save(ConfigFile); 
        if (System.IO.File.Exists(ConfigFile))
        {
            string json_str = System.IO.File.ReadAllText(ConfigFile);
            if (json_str.Length > 0)
                UnityEngine.JsonUtility.FromJsonOverwrite(json_str, config);
        }
        else
        {
            System.IO.FileInfo fileInfo = new System.IO.FileInfo(ConfigFile);
            if (!fileInfo.Exists)
                System.IO.Directory.CreateDirectory(fileInfo.Directory.FullName);

            bool prettyPrint = true;
            string json_str = UnityEngine.JsonUtility.ToJson(config, prettyPrint);
            System.IO.File.WriteAllText(ConfigFile, json_str);
        }
#endif

        foreach (Camera c in TargetCamera)
        {
            if (c == null)
            {
                Debug.LogError("StypeGripClient: Missing camera");
                continue;
            }
        }

        netReader.Connect(config, ringBuffer);
        ringBuffer.ResetDrops();
    }



    void OnDisable()
    {
        netReader.Disconnect();
    }


    public Vector3 p = Vector3.one;

    public Vector3 r = Vector3.one;
    public Vector4 q = Vector4.one;

    public bool applyCCDShift = true;
    public bool applyDistortion = true;
    void Update()
    {
        if (netReader.IsReading)
            UpdateCameras();

        PackSize = netReader.PackageSize;
        PackCount = netReader.TotalCounter;

        if (Input.GetKeyDown(KeyCode.C))
        {
            applyCCDShift = !applyCCDShift;
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            applyDistortion = !applyDistortion;
            Distortion = applyDistortion;
        }
    }




    void UpdateCameras()
    {
        for (int field = 0; field < TargetCamera.Length; ++field)
        {
            Camera cam = TargetCamera[field];

            cam.ResetProjectionMatrix();
            cam.ResetWorldToCameraMatrix();

            //Quaternion rot = netReader.Buffer.GetPacket(field).Rotation;
            Vector3 euler = netReader.Buffer.GetPacket(field).EulerAngles;
            Vector3 pos = netReader.Buffer.GetPacket(field).Position;

            //cam.transform.localRotation = rot;
            cam.transform.localRotation = Quaternion.Euler(-euler.x * r.x, euler.y * r.y, euler.z * r.z);
            cam.transform.localPosition = new Vector3(pos.x * p.x, pos.y * p.y, pos.z * p.z); ;

            cam.aspect = (float)netReader.Buffer.GetPacket(field).AspectRatio;
            cam.fieldOfView = (float)netReader.Buffer.GetPacket(field).FovY;

            if (applyDistortion)
                ApplyDistortion(cam, field); // true = shift in pixels for A5 protocol
        }
        
    }


    

    void ApplyDistortion(Camera cam, int field)
    {
        StypeGripDistortion dist = cam.GetComponent<StypeGripDistortion>();
        if (dist != null)
        {
            var Packet = netReader.Buffer.GetPacket(field);

            dist.PA_w = Packet.ChipWidth;
            dist.AR = Packet.AspectRatio;

            dist.CSX = Packet.CenterX;
            dist.CSY = Packet.CenterY;

            dist.K1 = Packet.K1;
            dist.K2 = Packet.K2;

            dist.Oversize = config.ImageScale;
        }
        
    }

}
