using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;



//using StypeGripPacket = Tracking.StypeGrip.PacketHF;
using StypeGripPacket = Tracking.StypeGrip.PacketA5;


public class StypeGripCalibrationTest : MonoBehaviour
{
    public float fov_scale = 1.0f;
    public float aspect = 1.77778f;
    public float chipWidth = 9.59f;
    public float chipHeight = 5.39f;
    public StypeCameraParams stypecam = null;

    public float k_multiplier = 1.0f;
    public bool shift_in_pixels = true;
    public bool apply_ccd_shift = true;
    public bool apply_distortion = true;
    public string frame_filename = "0871";

    public Camera[] TargetCamera = { null, null };



    public Tracking.StypeGrip.Config config;

    public RotationAxisOrder RotationOrder = RotationAxisOrder.YXZ;
    public Vector3 AnglesMultiplier = new Vector3(-1, 1, 1);
    public Vector3 PositionMultiplier = new Vector3(1, 1, -1);
    
    
    public bool StypeUpdate = true;
    public Vector3 StypePosition = Vector3.one;
    public Vector3 StypeAngles = Vector3.one;
    public Quaternion StypeQuaternion = Quaternion.identity;


    public string path;
    void Start()
    {
        path = Application.dataPath + "/StypeLog/";
        LoadFrame();
    }

    void Update()
    {
        ApplyFrame();

        if (Input.GetKeyDown(KeyCode.S))
        {
            //SaveFrame();
            string filepng = path + frame_filename + "_no_dist__ccd.png";
            ScreenCapture.CaptureScreenshot(filepng);
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            LoadFrame();
        }
    }


    

    void LoadFrame()
    {
        string filepng = path + frame_filename + ".png";
        string filecam = path + frame_filename + ".stypecam";

        if (stypecam == null)
            stypecam = new StypeCameraParams();
        stypecam.Load(filecam);
        ApplyFrame();
    }

    void ApplyFrame()
    {

        for (int field = 0; field < TargetCamera.Length; ++field)
        {

            Camera cam = TargetCamera[field];

            cam.ResetProjectionMatrix();
            cam.ResetWorldToCameraMatrix();

            cam.transform.localRotation = EulerToQuaternion(
                new Vector3(
                stypecam.tilt * AnglesMultiplier.x,
                stypecam.pan * AnglesMultiplier.y,
                stypecam.roll * AnglesMultiplier.z),
                RotationOrder);

            cam.transform.localPosition = new Vector3(
                stypecam.x * PositionMultiplier.x,
                stypecam.y * PositionMultiplier.y,
                stypecam.z * PositionMultiplier.z
                );

            aspect = cam.aspect = stypecam.fovx / stypecam.fovy;
            cam.fieldOfView = stypecam.fovy * fov_scale;


            // A5 = shif_in_pixels = true
            // HF = shif_in_pixels = false
            // shift_in_pixels = false; // (Packet.GetType() == typeof(Tracking.StypeGrip.PacketA5));


            if (apply_ccd_shift)
            {
                Matrix4x4 p = cam.projectionMatrix;
                if (shift_in_pixels)
                {
                    p[0, 2] = 2.0f * stypecam.centerx / (float)config.ImageWidth;
                    p[1, 2] = 2.0f * stypecam.centery / (float)config.ImageHeight;
                }
                else // shift in mm
                {
                    p[0, 2] = 2.0f * stypecam.centerx / chipWidth;
                    p[1, 2] = 2.0f * stypecam.centery / chipHeight;
                }
                cam.projectionMatrix = p;
            }

            if (apply_distortion)
            { 
                StypeGripDistortion dist = cam.GetComponent<StypeGripDistortion>();
                if (dist != null)
                {
                    dist.PA_w = chipWidth;
                    dist.AR = cam.aspect;

                    dist.K1 = k_multiplier * stypecam.k1;
                    dist.K2 = k_multiplier * stypecam.k2;

                    if (shift_in_pixels)
                    {
                        dist.CSX = stypecam.centerx / (float)config.ImageWidth;
                        dist.CSY = stypecam.centery / (float)config.ImageHeight;
                    }
                    else
                    {
                        dist.CSX = stypecam.centerx;
                        dist.CSY = stypecam.centery;
                    }
                }


                XcitoDistortion xcito_dist = cam.GetComponent<XcitoDistortion>();
                if (xcito_dist != null)
                {
                    xcito_dist.distParams = new Vector2(k_multiplier * stypecam.k1, k_multiplier * stypecam.k2);

                    if (shift_in_pixels)
                    {
                        xcito_dist.centerShift = new Vector2(
                            stypecam.centerx / (float)config.ImageWidth * chipWidth,
                            stypecam.centery / (float)config.ImageHeight * chipHeight);
                    }
                    else    // shift in mm
                    {
                        xcito_dist.centerShift = new Vector2(stypecam.centerx, stypecam.centery);
                    }
                    //dist.texCoordScale = xcito.TexCoordScale;
                }
            }
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
}

