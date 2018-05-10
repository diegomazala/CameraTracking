using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;



using StypeGripPacket = Tracking.StypeGrip.PacketHF;
//using StypeGripPacket = Tracking.StypeGrip.PacketA5;



public class StypeGripClient : MonoBehaviour
{
    public Camera[] TargetCamera = { null, null };
    private StypeGripPacket[] Packets = { null, null };

    public Tracking.StypeGrip.Config config;
    private Tracking.RingBuffer<StypeGripPacket> ringBuffer;
    public Tracking.INetReader<StypeGripPacket> netReader = null;

    private LogWriter.LogWriter logWriter = null;

    private long lastFrameTimecode = 0;
    public int DropCountBetweenFields = 0;
    public int DropCountBetweenFrames = 0;

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
        netReader.Config = config;
        ringBuffer = new Tracking.RingBuffer<StypeGripPacket>(config.Delay);
        netReader.Buffer = ringBuffer;

        logWriter = new LogWriter.LogWriter("", "Stype_", ".log");
        logWriter.WriteToLog(System.DateTime.Now.ToString("yyyyMMdd_HHmmss", System.Globalization.CultureInfo.InvariantCulture));
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

        if (!config.Enabled)
        {
            enabled = false;
            return;
        }

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

        Packets = new StypeGripPacket[TargetCamera.Length];

        for (int field = 0; field < TargetCamera.Length; ++field)
        {
            Camera cam = TargetCamera[field];
            StypeGripDistortion dist = cam.GetComponent<StypeGripDistortion>();
            if (dist != null)
                dist.Oversize = config.ImageScale;

            Packets[field] = new StypeGripPacket();
        }

        
    }


    void OnDisable()
    {
        netReader.Disconnect();
    }


    void Update()
    {
        if (netReader.IsReading)
            UpdateCameras();

        //
        // Log to file if a drop happened
        //
        int drop_count_betwenn_fields = CheckDropBetweenFields(); // CheckDropInBuffer();
        int drop_count_between_frames = CheckDropBetweenFrames(); // CheckDropBetweenBuffers();
        if (drop_count_betwenn_fields > 0 || drop_count_between_frames > 0)
        {
            logWriter.WriteToLog("Drop Fields : " + drop_count_betwenn_fields.ToString() + ' ' + drop_count_between_frames);
            LogBuffer();

            DropCountBetweenFields += drop_count_betwenn_fields;
            DropCountBetweenFrames += drop_count_between_frames;
        }
        //
        // Log if config ask to 
        //
        if (config.LogToFile)
            LogBuffer();


        //
        // Save a snapshot
        //
        if (Input.GetKeyDown(KeyCode.S) && Input.GetKey(KeyCode.LeftControl))
        {
            SaveFrame();
        }

        //
        // Get the timecode of the last packet in the buffer
        //
        lastFrameTimecode = Packets[0].Timecode;//netReader.Buffer.LastPacket.Timecode;
    }


    void UpdateCameras()
    {
        for (int field = 0; field < TargetCamera.Length; ++field)
        {
            Camera cam = TargetCamera[field];
            StypeGripPacket Packet = Packets[field] = netReader.Buffer.GetPacket(field);

            cam.ResetProjectionMatrix();
            cam.ResetWorldToCameraMatrix();

            cam.transform.localRotation = Packet.Rotation;
            cam.transform.localPosition = Packet.Position;
            
            cam.aspect = (float)config.ImageWidth / (float)config.ImageHeight;
            cam.fieldOfView = (float)Packet.FovY;

            // A5 = shif_in_pixels = true
            // HF = shif_in_pixels = false
            //bool shift_in_pixels = (Packet.GetType() == typeof(Tracking.StypeGrip.PacketA5));
            //ApplyDistortionStype(cam, field, shift_in_pixels);
            //ApplyCcdShift(cam, field, shift_in_pixels);
            //ApplyDistortionXcito(cam, field, shift_in_pixels);
        }


    }



    // A5 = shif_in_pixels = true
    // HF = shif_in_pixels = false
    void ApplyDistortionStype(Camera cam, int field, bool shift_in_pixels)
    {
        StypeGripDistortion dist = cam.GetComponent<StypeGripDistortion>();
        if (dist != null)
        {
            var Packet = Packets[field];

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
        var Packet = Packets[field];

        if (dist != null)
        {
            dist.distParams = new Vector2(Packet.K1, Packet.K2);

            if (shift_in_pixels)
            {
                dist.centerShift = new Vector2(
                    Packet.CenterX / (float)config.ImageWidth * Packet.ChipWidth,
                    Packet.CenterY / (float)config.ImageHeight * Packet.ChipHeight);
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
        var Packet = Packets[field];
        Matrix4x4 p = cam.projectionMatrix;
        if (shift_in_pixels)
        {
            p[0, 2] = 2.0f * Packet.CenterX / (float)config.ImageWidth;
            p[1, 2] = 2.0f * Packet.CenterY / (float)config.ImageHeight;
        }
        else // shift in mm
        {
            p[0, 2] = 2.0f * Packet.CenterX / Packet.ChipWidth;
            p[1, 2] = 2.0f * Packet.CenterY / Packet.ChipHeight;
        }
        cam.projectionMatrix = p;
    }




    int CheckDropInBuffer()
    {
        int dropCount = 0;

        for (int i = 0; i < netReader.Buffer.Length - 1; ++i)
        {
            var pack_i0 = netReader.Buffer.GetPacket(i);
            var pack_i1 = netReader.Buffer.GetPacket(i + 1);
            double elapsedtime_ms = (new System.TimeSpan(pack_i1.Timecode - pack_i0.Timecode).TotalMilliseconds);
            double expected_interval_ms = 1000.0 / config.FrameRatePerSecond;

            if (elapsedtime_ms > expected_interval_ms * 2)
                dropCount++;
        }
        return dropCount;
    }

    int CheckDropBetweenBuffers()
    {
        double elapsedtime_ms = (new System.TimeSpan(netReader.Buffer.Packet.Timecode - lastFrameTimecode).TotalMilliseconds);
        double expected_interval_ms = 1000.0 / config.FrameRatePerSecond;

        if (elapsedtime_ms > expected_interval_ms * 2)
            return (int)(elapsedtime_ms / expected_interval_ms);
        else
            return 0;
    }

    int CheckDropBetweenFields()
    {
        double elapsedtime_ms = (new System.TimeSpan(Packets[1].Timecode - Packets[0].Timecode).TotalMilliseconds);
        double expected_interval_ms = 1000.0 / config.FrameRatePerSecond;

        if (elapsedtime_ms > expected_interval_ms * 2)
            return (int)(elapsedtime_ms / expected_interval_ms);
        else
            return 0;
    }

    int CheckDropBetweenFrames()
    {
        double elapsedtime_ms = (new System.TimeSpan(Packets[0].Timecode - lastFrameTimecode).TotalMilliseconds);
        double expected_interval_ms = 1000.0 / config.FrameRatePerSecond;

        if (elapsedtime_ms > expected_interval_ms * 2)
            return (int)(elapsedtime_ms / expected_interval_ms);
        else
            return 0;
    }

    void LogBuffer()
    {
        System.Text.StringBuilder str = new System.Text.StringBuilder();
        for (int i = 0; i < netReader.Buffer.Length - 1; ++i)
        {
            //logWriter.WriteToLog(
            //    new System.TimeSpan(netReader.Buffer.GetPacket(i).Timecode).ToString() + ' ' + 
            //    Time.frameCount.ToString() + ' ' + 
            //    Time.deltaTime.ToString());

            str.Append(new System.TimeSpan(netReader.Buffer.GetPacket(i).Timecode).ToString());
            str.Append(" ");
        }
        str.Append(Time.frameCount.ToString());
        str.Append(" ");
        str.Append(Time.deltaTime.ToString());
        logWriter.WriteToLog(str.ToString());
    }


    void SaveFrame()
    {
        // Append filename to folder name (format is '0005 shot.png"')
        string filepng = string.Format("{0:D04}.png", Time.frameCount);
        string filebin = string.Format("{0:D04}.stype.bin", Time.frameCount);
        string filejson = string.Format("{0:D04}.stype.json", Time.frameCount);

        // Capture the screenshot to the specified file.
        ScreenCapture.CaptureScreenshot(filepng);
        netReader.Buffer.Packet.Save(filebin);

        StypeGripSerialization camParams = new StypeGripSerialization(netReader.Buffer.Packet);
        camParams.Save(filejson);
    }


}

