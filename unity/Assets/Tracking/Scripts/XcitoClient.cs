using UnityEngine;
using System.Collections;


public class XcitoClient : MonoBehaviour
{
    private long LastPacketCounter = 0;
    private long LastDropCounter = 0;
    public Camera[] TargetCamera = { null, null };

    public Tracking.Xcito.Config config;
    private Tracking.RingBuffer<Tracking.Xcito.Packet> ringBuffer;
    public Tracking.Xcito.NetReader<Tracking.Xcito.Packet> netReader = null;

    private XcitoClientUI xcitoClientUI = null;
    public XcitoClientUI UI
    {
        set
        {
            xcitoClientUI = value;
            xcitoClientUI.netReader = netReader;
        }

        get
        {
            return xcitoClientUI;
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

            return (new System.IO.DirectoryInfo(sb.ToString())).FullName + Tracking.Xcito.Config.FileName;
        }
    }

    public Tracking.Xcito.CameraConstants CameraConsts
    {
        get { return netReader.CameraConsts; }
    }

    void Awake()
    {
        //netReader = new Tracking.Xcito.NetReaderAsync<Tracking.Xcito.Packet>();
        netReader = new Tracking.Xcito.NetReader<Tracking.Xcito.Packet>();
        netReader.Config = config;
        ringBuffer = new Tracking.RingBuffer<Tracking.Xcito.Packet>(config.Delay);
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

        foreach (Camera c in TargetCamera)
        {
            if (c == null)
            {
                Debug.LogError("XcitoClient: Missing camera");
                continue;
            }
        }

        netReader.Connect(config, ringBuffer);
        ringBuffer.ResetDrops();
        LastDropCounter = ringBuffer.Drops;
    }



    void OnDisable()
    {
        netReader.Disconnect();
    }

    public bool IsUpdating()
    {
        return LastPacketCounter != ringBuffer.Packet.Counter;
    }

    public bool IsSynced()
    {
        bool synced = (LastDropCounter == ringBuffer.Drops);
        LastDropCounter = ringBuffer.Drops;
        return synced;
    }

    void Update()
    {
        if (netReader.IsReading)
        {
            UpdateCameras();

            for (int field = 0; field < TargetCamera.Length; ++field)
            {
                LastPacketCounter = ringBuffer.GetPacket(TargetCamera.Length - 1 - field).Counter;
            }
        }

        //System.Text.StringBuilder builder = new System.Text.StringBuilder();
        //for (int i =0; i<ringBuffer.Delay; ++i)
        //{
        //    builder.Append(ringBuffer.GetPacket(i).Counter + "  " );
        //}
        //Debug.Log(builder.ToString());  
    }




    void UpdateCameras()
    {

        for (int field = 0; field < TargetCamera.Length; ++field)
        {
            Camera cam = TargetCamera[field];

            cam.ResetProjectionMatrix();
            cam.ResetWorldToCameraMatrix();

            cam.aspect = (float)netReader.CameraConsts.ImageWidth / (float)netReader.CameraConsts.ImageHeight;
            cam.fieldOfView = (float)netReader.Buffer.GetPacket(field).FovY;

            ApplyCcdShift(cam, field, true);
            ApplyDistortion(cam, field, true);  

            
#if true
            cam.transform.localRotation = netReader.Buffer.GetPacket(field).Rotation;
            cam.transform.localPosition = netReader.Buffer.GetPacket(field).Position;
#else
            Matrix4x4 modelview = Matrix4x4.identity;
            modelview[0] = (float)netReader.Buffer.GetPacket(field).Params.Matrix[0];
            modelview[1] = (float)netReader.Buffer.GetPacket(field).Params.Matrix[1];
            modelview[2] = (float)netReader.Buffer.GetPacket(field).Params.Matrix[2];
            modelview[3] = 0;

            modelview[4] = (float)netReader.Buffer.GetPacket(field).Params.Matrix[3];
            modelview[5] = (float)netReader.Buffer.GetPacket(field).Params.Matrix[4];
            modelview[6] = (float)netReader.Buffer.GetPacket(field).Params.Matrix[5];
            modelview[7] = 0;

            modelview[8] = (float)netReader.Buffer.GetPacket(field).Params.Matrix[6];
            modelview[9] = (float)netReader.Buffer.GetPacket(field).Params.Matrix[7];
            modelview[10] = (float)netReader.Buffer.GetPacket(field).Params.Matrix[8];
            modelview[11] = 0;

            modelview[12] = (float)netReader.Buffer.GetPacket(field).Params.Matrix[9];
            modelview[13] = (float)netReader.Buffer.GetPacket(field).Params.Matrix[10];
            modelview[14] = (float)netReader.Buffer.GetPacket(field).Params.Matrix[11];
            modelview[15] = 1;

            Matrix4x4 mdv = modelview;

            // Math to decompose orad matrix 
            Quaternion r = QuaternionFromMatrix(mdv);
            r.z *= -1.0f;
            r.w *= -1.0f;
            cam.transform.localRotation = r;

            Vector3 pos = mdv.GetColumn(3);
            pos.z *= -1.0f;
            cam.transform.localPosition = pos;
#endif
        }

    }


    void ApplyCcdShift(Camera cam, int field, bool shift_in_pixels)
    {
        Matrix4x4 p = cam.projectionMatrix;
        if (shift_in_pixels)
        {
            p[0, 2] = 2.0f * netReader.Buffer.GetPacket(field).CenterX / (float)netReader.CameraConsts.ImageWidth;
            p[1, 2] = 2.0f * netReader.Buffer.GetPacket(field).CenterY / (float)netReader.CameraConsts.ImageHeight;
        }
        else // shift in mm
        {
            p[0, 2] = 2.0f * netReader.Buffer.GetPacket(field).CenterX / netReader.CameraConsts.ChipWidth;
            p[1, 2] = 2.0f * netReader.Buffer.GetPacket(field).CenterY / netReader.CameraConsts.ChipHeight;
        }
        cam.projectionMatrix = p;
    }


    void ApplyDistortion(Camera cam, int field, bool shift_in_pixels)
    {
        XcitoDistortion dist = cam.GetComponent<XcitoDistortion>();
        if (dist != null)
        {
            dist.distParams = new Vector2(netReader.Buffer.GetPacket(field).K1, netReader.Buffer.GetPacket(field).K2);
            dist.chipSize = new Vector2(netReader.CameraConsts.ChipWidth, netReader.CameraConsts.ChipHeight);

            if (shift_in_pixels)
            {
                dist.centerShift = new Vector2(
                    netReader.Buffer.GetPacket(field).CenterX / (float)netReader.CameraConsts.ImageWidth * netReader.CameraConsts.ChipWidth,
                    netReader.Buffer.GetPacket(field).CenterY / (float)netReader.CameraConsts.ImageHeight * netReader.CameraConsts.ChipHeight);
            }
            else    // shift in mm
            {
                dist.centerShift = new Vector2(
                    netReader.Buffer.GetPacket(field).CenterX,
                    netReader.Buffer.GetPacket(field).CenterY);
            }
            //dist.texCoordScale = xcito.TexCoordScale;
        }

    }

    public static Quaternion QuaternionFromMatrix(Matrix4x4 m)
    {
        return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
    }



    public bool Distortion
    {
        set
        {
            foreach (Camera c in TargetCamera)
            {
                if (c == null)
                {
                    Debug.LogError("XcitoClient: Missing camera");
                    continue;
                }

                XcitoDistortion dist = c.GetComponent<XcitoDistortion>();
                if (dist != null && dist.enabled != value)
                    dist.enabled = value;
            }
        }

        get
        {
            if (TargetCamera.Length > 0)
            {
                XcitoDistortion dist = TargetCamera[0].GetComponent<XcitoDistortion>();
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

}
