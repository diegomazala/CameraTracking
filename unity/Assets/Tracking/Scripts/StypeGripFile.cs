using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;


using StypeGripPacket = Tracking.StypeGrip.PacketHF;
//using StypeGripPacket = Tracking.StypeGrip.PacketA5;


public class StypeGripFile : MonoBehaviour
{
    public Camera TargetCamera = null;
    public string Path;
    public string FrameName = "0871";

    public StypeGripSerialization StypeCam = null;

    void Start()
    {
        if (TargetCamera == null)
            TargetCamera = GetComponent<Camera>();
        if (TargetCamera == null)
            TargetCamera = Camera.main;
        if (TargetCamera == null)
        {
            Debug.LogError("Missing Camera");
            enabled = false;
        }


        Path = Application.dataPath + "/StypeLog/";
        LoadFrame();
    }



    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            LoadFrame();
        }
    }

    void LoadFrame()
    {
        string filepng = Path + FrameName + ".png";
        string filebin = Path + FrameName + ".stype.bin";
        string filejson = Path + FrameName + ".stype.json";

        if (StypeCam == null)
            StypeCam = new StypeGripSerialization();
        StypeCam.Load(filejson);
        StypeCam.ApplyToCamera(TargetCamera);
    }


}
