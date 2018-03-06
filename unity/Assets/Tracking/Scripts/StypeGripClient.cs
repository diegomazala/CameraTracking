using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;



//using StypeGripPacket = Tracking.StypeGrip.PacketHF;
using StypeGripPacket = Tracking.StypeGrip.PacketA5;

[System.Serializable]
public class StypeCameraParams
{
    public float x;
    public float y;
    public float z;
    public float pan;
    public float tilt;
    public float roll;
    public float fovx;
    public float fovy;
    public float focus;
    public float k1;
    public float k2;
    public float centerx;
    public float centery;


    public void Save(string filename)
    {
        bool prettyPrint = true;
        string json_str = UnityEngine.JsonUtility.ToJson(this, prettyPrint);
        System.IO.File.WriteAllText(filename, json_str);
    }

    public void Load(string filename)
    {
        string json_str = System.IO.File.ReadAllText(filename);
        if (json_str.Length > 0)
            UnityEngine.JsonUtility.FromJsonOverwrite(json_str, this);
    }
}


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
    public string frame_filename = "0871";



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
        netReader = new Tracking.StypeGrip.NetReader<StypeGripPacket>();
        //netReader = new Tracking.StypeGrip.NetReaderSync<StypeGripPacket>();
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

        if (Input.GetKeyDown(KeyCode.S))
        {
            SaveFrame();
        }

    }

    public bool shift_in_pixels = true;


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
            
            cam.aspect = (float)config.ImageWidth / (float)config.ImageHeight;
            cam.fieldOfView = (float)Packet.FovY;

            // A5 = shif_in_pixels = true
            // HF = shif_in_pixels = false
            //shift_in_pixels = (Packet.GetType() == typeof(Tracking.StypeGrip.PacketA5));
            //ApplyDistortionStype(cam, field, shift_in_pixels);

            ApplyCcdShift(cam, field, shift_in_pixels);
            ApplyDistortionXcito(cam, field, shift_in_pixels);
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

                p_it.Counter = start_counter + System.Convert.ToInt64(elapsedtime_ms / expected_interval_ms);
            }

            for (int it = 1; it < netReader.Buffer.Length; ++it)
                counter_sum += netReader.Buffer.GetPacket(it).Counter - netReader.Buffer.GetPacket(it - 1).Counter;
        }


        string buffer_str = "Buffer: " + netReader.Buffer.Length.ToString();
        for (int it = 0; it < netReader.Buffer.Length; ++it)
            buffer_str += " " + netReader.Buffer.GetPacket(it).Counter.ToString();

        if (ScreenText != null)
        {
            ScreenText.text = buffer_str + " - " + counter_sum;

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


    float chipWidth = 9.59f;
    float chipHeight = 5.49f;
    // A5 = shif_in_pixels = true
    // HF = shif_in_pixels = false
    void ApplyDistortionStype(Camera cam, int field, bool shift_in_pixels)
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


    void ApplyDistortionXcito(Camera cam, int field, bool shift_in_pixels)
    {
        XcitoDistortion dist = cam.GetComponent<XcitoDistortion>();
        var Packet = netReader.Buffer.GetPacket(field);

        if (dist != null)
        {
            dist.distParams = new Vector2(Packet.K1, Packet.K2);

            if (shift_in_pixels)
            {
                dist.centerShift = new Vector2(
                    Packet.CenterX / (float)config.ImageWidth * chipWidth,
                    Packet.CenterY / (float)config.ImageHeight * chipHeight);
            }
            else    // shift in mm
            {
                dist.centerShift = new Vector2(
                    Packet.CenterX,
                    Packet.CenterY);
            }
            //dist.texCoordScale = xcito.TexCoordScale;
        }
    }


    void ApplyCcdShift(Camera cam, int field, bool shift_in_pixels)
    {
        Matrix4x4 p = cam.projectionMatrix;
        if (shift_in_pixels)
        {
            p[0, 2] = 2.0f * netReader.Buffer.GetPacket(field).CenterX / (float)config.ImageWidth;
            p[1, 2] = 2.0f * netReader.Buffer.GetPacket(field).CenterY / (float)config.ImageHeight;
        }
        else // shift in mm
        {
            p[0, 2] = 2.0f * netReader.Buffer.GetPacket(field).CenterX / chipWidth;
            p[1, 2] = 2.0f * netReader.Buffer.GetPacket(field).CenterY / chipHeight;
        }
        cam.projectionMatrix = p;
    }


    void SaveFrame()
    {
        StypeCameraParams stype_cam = new StypeCameraParams();
        stype_cam.fovx = netReader.Buffer.Packet.FovX;
        stype_cam.fovy = netReader.Buffer.Packet.FovY;
        stype_cam.centerx = netReader.Buffer.Packet.CenterX;
        stype_cam.centery = netReader.Buffer.Packet.CenterY;
        stype_cam.k1 = netReader.Buffer.Packet.K1;
        stype_cam.k2 = netReader.Buffer.Packet.K2;
        stype_cam.x = StypePosition.x;
        stype_cam.y = StypePosition.y;
        stype_cam.z = StypePosition.z;
        stype_cam.pan = StypeAngles.y;
        stype_cam.tilt = StypeAngles.x;
        stype_cam.roll = StypeAngles.z;

        // Append filename to folder name (format is '0005 shot.png"')
        string filepng = string.Format("{0:D04}.png", Time.frameCount);
        string filecam = string.Format("{0:D04}.stypecam", Time.frameCount);

        // Capture the screenshot to the specified file.
        ScreenCapture.CaptureScreenshot(filepng);
        stype_cam.Save(filecam);
    }


}

