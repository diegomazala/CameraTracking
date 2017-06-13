using UnityEngine;
using System.Collections;


public class MrmcClient : MonoBehaviour
{
    public int PackSize = 0;
    public int PackCount = 0;
    
    public Camera targetCamera = null;

    public Tracking.Mrmc.Config config;
    private Tracking.RingBuffer<Tracking.Mrmc.Packet> ringBuffer;
    public Tracking.NetReader<Tracking.Mrmc.Packet> netReader = null;



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
#if !UNITY_EDITOR
        if (!netReader.Config.Load(ConfigFile))
            netReader.Config.Save(ConfigFile); 
#endif

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

        PackSize = netReader.PackageSize;
        PackCount = netReader.TotalCounter;
    }


    void UpdateCameras()
    {
    	targetCamera.transform.localPosition = netReader.Buffer.Packet.Position;
        Vector3 target = netReader.Buffer.Packet.Target;
        
    }

}
