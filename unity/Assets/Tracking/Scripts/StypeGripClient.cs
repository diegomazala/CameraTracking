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
    public Tracking.INetReader<StypeGripPacket> netReader = null;

    public RotationAxisOrder RotationOrder = RotationAxisOrder.YXZ;
    public Vector3 AnglesMultiplier = Vector3.one;
    public Vector3 PositionMultiplier = Vector3.one;

    private long lastFrameTimecode = 0;
    public float TimeBetweenFields = 0;
    public float TimeBetweenFrames = 0;

    public UnityEngine.UI.Text ScreenText = null;


    

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
        //netReader = new Tracking.StypeGrip.NetReader<StypeGripPacket>();
        netReader = new Tracking.StypeGrip.NetReaderSync<StypeGripPacket>();
        netReader.Config = config;
        ringBuffer = new Tracking.RingBuffer<StypeGripPacket>(config.Delay);
        netReader.Buffer = ringBuffer;
    }


    void OnEnable()
    {
        // Try to load a configuration file
        // If didn't find a config file, create a default
#if !UNITY_EDITOR
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

        for (int field = 0; field < TargetCamera.Length; ++field)
        {
            Camera cam = TargetCamera[field];
            StypeGripDistortion dist = cam.GetComponent<StypeGripDistortion>();
            if (dist != null)
                dist.Oversize = config.ImageScale;
        }
    }


    void OnDisable()
    {
        netReader.Disconnect();
    }

    public bool StypeUpdate = true;
    public Vector3 StypePosition = Vector3.one;
    public Vector3 StypeAngles = Vector3.one;
    public Quaternion StypeQuaternion = Quaternion.identity;

    void Update()
    {
        if (netReader.IsReading || netReader.ReadNow() > 0)
            UpdateCameras();

        PackSize = netReader.PackageSize;
        PackCount = netReader.TotalCounter;

    }




    void UpdateCameras()
    {
        

        for (int field = 0; field < TargetCamera.Length; ++field)
        {
            Camera cam = TargetCamera[field];
            StypeGripPacket Packet = netReader.Buffer.GetPacket(field);

            cam.ResetProjectionMatrix();
            cam.ResetWorldToCameraMatrix();

            if (StypeUpdate)
            {
                StypeAngles = Packet.EulerAngles;
                StypeQuaternion = Quaternion.Euler(StypeAngles);
                StypePosition = Packet.Position;
            }

            cam.transform.localRotation = EulerToQuaternion(
                new Vector3(
                StypeAngles.x * AnglesMultiplier.x,
                StypeAngles.y * AnglesMultiplier.y,
                StypeAngles.z * AnglesMultiplier.z), 
                RotationOrder);

            cam.transform.localPosition = new Vector3(
                StypePosition.x * PositionMultiplier.x,
                StypePosition.y * PositionMultiplier.y,
                StypePosition.z * PositionMultiplier.z
                );
            
            cam.aspect = (float)Packet.AspectRatio;
            cam.fieldOfView = (float)Packet.FovY;

            // A5 = shif_in_pixels = true
            // HF = shif_in_pixels = false
            bool shift_in_pixels = (Packet.GetType() == typeof(Tracking.StypeGrip.PacketA5));
            ApplyDistortion(cam, field, shift_in_pixels); 
        }

        

        if (TargetCamera.Length > 0)
            TimeBetweenFields = (new System.TimeSpan(netReader.Buffer.GetPacket(1).Timecode - netReader.Buffer.GetPacket(0).Timecode).Milliseconds);

        TimeBetweenFrames = (new System.TimeSpan(netReader.Buffer.GetPacket(0).Timecode - lastFrameTimecode).Milliseconds);

        lastFrameTimecode = netReader.Buffer.GetPacket(1).Timecode;


        // A5 protocol does not have counter 
        long counter_sum = 0;
        long start_counter = netReader.TotalCounter - netReader.Buffer.Length;
        if (netReader.Buffer.Packet.GetType() == typeof(Tracking.StypeGrip.PacketA5))
        {
            var p_0 = netReader.Buffer.GetPacket(0);

            for (int it = 0; it < netReader.Buffer.Length; ++it)
            {
                var p_it = netReader.Buffer.GetPacket(it);
                double elapsedtime_ms = (new System.TimeSpan(p_it.Timecode - p_0.Timecode).TotalMilliseconds);
                double expected_interval_ms = 1000.0 / config.FrameRatePerSecond;

                long id_counter = start_counter + System.Convert.ToInt64(elapsedtime_ms / expected_interval_ms);
                p_it.Counter = System.Convert.ToChar(id_counter);
            }

            for (int it = 1; it < netReader.Buffer.Length; ++it)
                counter_sum += netReader.Buffer.GetPacket(it).Counter - netReader.Buffer.GetPacket(it - 1).Counter;
        }


        string buffer_str = "Buffer: " + netReader.Buffer.Length.ToString();
        for (int it = 0; it < netReader.Buffer.Length; ++it)
            buffer_str += " " + netReader.Buffer.GetPacket(it).Counter.ToString();

        if (ScreenText != null)
        {
            ScreenText.text = TimeBetweenFields.ToString("Fields: 0.000") + " " + TimeBetweenFrames.ToString("Frames: 0.000") + " " + buffer_str + " - " + counter_sum;

            if (counter_sum != netReader.Buffer.Length - 1)
                ScreenText.color = Color.red;
            else
                ScreenText.color = Color.green;
        }
    }


    public enum RotationAxisOrder
    {
        XYZ, XZY, YXZ, YZX, ZXY, ZYX, Unknown
    }


    static public Quaternion EulerToQuaternion(Vector3 eulerAngles, RotationAxisOrder rotationOrder)
    {
        Quaternion x = Quaternion.AngleAxis(eulerAngles.x, Vector3.right);
        Quaternion y = Quaternion.AngleAxis(eulerAngles.y, Vector3.up);
        Quaternion z = Quaternion.AngleAxis(eulerAngles.z, Vector3.forward);

        switch (rotationOrder)
        {
            case RotationAxisOrder.XYZ: return x * y * z;
            case RotationAxisOrder.XZY: return x * z * y;
            case RotationAxisOrder.YXZ: return y * x * z;
            case RotationAxisOrder.YZX: return y * z * x;
            case RotationAxisOrder.ZXY: return z * x * y;
            case RotationAxisOrder.ZYX: return z * y * x;
            default: return Quaternion.identity;
        }
    }


    // A5 = shif_in_pixels = true
    // HF = shif_in_pixels = false
    void ApplyDistortion(Camera cam, int field, bool shift_in_pixels)
    {
        StypeGripDistortion dist = cam.GetComponent<StypeGripDistortion>();
        if (dist != null)
        {
            var Packet = netReader.Buffer.GetPacket(field);

            dist.PA_w = Packet.ChipWidth;
            dist.AR = Packet.AspectRatio;

            dist.K1 = Packet.K1;
            dist.K2 = Packet.K2;

            if (shift_in_pixels)
            {
                dist.CSX = Packet.CenterX / (float)config.ImageWidth;
                dist.CSY = Packet.CenterY / (float)config.ImageHeight;
            }
            else
            {
                dist.CSX = Packet.CenterX;
                dist.CSY = Packet.CenterY;
            }

            
        }
        
    }
    
}

