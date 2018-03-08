using UnityEngine;
using System.Collections;


public class MrmcClient : MonoBehaviour
{
    public int PackSize = 0;
    public int PackCount = 0;

    public Camera mainCamera = null;

    public Tracking.Mrmc.Config config;
    private Tracking.RingBuffer<Tracking.Mrmc.Packet> ringBuffer;
    public Tracking.NetReader<Tracking.Mrmc.Packet> netReader = null;


    public float Zoom;
    public float Focus;

    public Vector3 Pos;
    public Vector3 Target;

    public bool isValid;

    public Transform target;

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

            return (new System.IO.DirectoryInfo(sb.ToString())).FullName + Tracking.Mrmc.Config.FileName;
        }
    }


    void Awake()
    {
        netReader = new Tracking.NetReader<Tracking.Mrmc.Packet>();
        netReader.Config = config;
        ringBuffer = new Tracking.RingBuffer<Tracking.Mrmc.Packet>(config.Delay);
        netReader.Buffer = ringBuffer;
    }


    void OnEnable()
    {

        // Try to load a configuration file
        // If didn't find a config file, create a default
#if true //!UNITY_EDITOR
        if (!netReader.Config.Load(ConfigFile))
            netReader.Config.Save(ConfigFile); 
#endif

        if (mainCamera == null)
            mainCamera = Camera.main;

        netReader.Connect(config, ringBuffer);
        ringBuffer.ResetDrops();
    }



    void OnDisable()
    {
        netReader.Disconnect();
    }


    void Update()
    {
        if (netReader.IsReading)
            UpdateCameras();

    }


    void UpdateCameras()
    { 
        float pos_x = netReader.Buffer.Packet.Position.x;
        float pos_y = netReader.Buffer.Packet.Position.y;
        float pos_z = netReader.Buffer.Packet.Position.z;

        float t_x = netReader.Buffer.Packet.Target.x;
        float t_y = netReader.Buffer.Packet.Target.y;
        float t_z = netReader.Buffer.Packet.Target.z;


        // BOLT
        //mainCamera.transform.localPosition = new Vector3(pos_y, pos_z, pos_x);

        // MILO
        mainCamera.transform.localPosition = new Vector3(pos_x, pos_z, pos_y);
        target.position = new Vector3(t_x, t_z, t_y);
        mainCamera.transform.LookAt(new Vector3(t_x, t_z, t_y));

        


        Zoom = netReader.Buffer.Packet.Zoom;
        Focus = netReader.Buffer.Packet.Focus;

        Pos = netReader.Buffer.Packet.Position;
        Target = netReader.Buffer.Packet.Target;

        isValid = netReader.Buffer.Packet.IsValid;
    }

}
