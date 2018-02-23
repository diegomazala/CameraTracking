using UnityEngine;
using System.Collections;

//using StypeGripPacket = Tracking.StypeGrip.PacketHF;
using StypeGripPacket = Tracking.StypeGrip.PacketA5;

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
    }


    void OnDisable()
    {
        netReader.Disconnect();
    }

    public bool StypeUpdate = true;
    public Vector3 StypePosition = Vector3.one;
    public Vector3 StypeAngles = Vector3.one;
    public Quaternion StypeQuaternion = Quaternion.identity;

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
            StypeGripPacket Packet = netReader.Buffer.GetPacket(field);

            cam.ResetProjectionMatrix();
            cam.ResetWorldToCameraMatrix();

            if (StypeUpdate)
            {
                StypeAngles = Packet.EulerAngles;
                StypeQuaternion = Quaternion.Euler(StypeAngles);
                StypePosition = Packet.Position;
            }


            transform.localRotation = EulerToQuaternion(
                new Vector3(
                StypeAngles.x * AnglesMultiplier.x,
                StypeAngles.y * AnglesMultiplier.y,
                StypeAngles.z * AnglesMultiplier.z), 
                RotationOrder);

            transform.localPosition = new Vector3(
                transform.localPosition.x * PositionMultiplier.x,
                transform.localPosition.y * PositionMultiplier.y,
                transform.localPosition.z * PositionMultiplier.z
                );
            
            cam.aspect = (float)Packet.AspectRatio;
            cam.fieldOfView = (float)Packet.FovY;

            if (applyDistortion)
                ApplyDistortion(cam, field); // true = shift in pixels for A5 protocol
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


#if false
///////////////////////////////////////
////////////// XCITO.CS ///////////////
    Matrix4x4 modelview = netReader.Buffer.GetPacket(field).Params.Transform;
    Quaternion rot = QuaternionFromMatrix(modelview);
    rot.z *= -1.0f;
    rot.w *= -1.0f;

    Vector3 pos = modelview.GetColumn(3);
    pos.z *= -1.0f;

    cam.transform.localRotation = rot;
    cam.transform.localPosition = pos;


///////////////////////////////////////
///////////////////////////////////////
t.localRotation = new Quaternion(-q.x, -q.z, q.y, -q.w);

static void RotationFix180 (Transform t)
{
    Vector3 localEulerAngles = t.localEulerAngles;		
		
    localEulerAngles.x = -localEulerAngles.x;
    localEulerAngles.z = -localEulerAngles.z;
		
    t.localEulerAngles = localEulerAngles;
    Quaternion q = t.localRotation;		
    t.localRotation = new Quaternion(-q.x, q.y, -q.z, q.w);
}

///////////////////////////////////////
///////////////////////////////////////

rot = Quaternion.Inverse(rot);
{
    // Convert the rotation from Blender to Unity 
	keysX[k].value = -rot.x;
	keysY[k].value = -rot.z;
	keysZ[k].value = rot.y;
	keysW[k].value = -rot.w;
}

///////////////////////////////////////
///////////////////////////////////////
rot = new Quaternion (-rot.x, -rot.z, rot.y, -rot.w);
if (m_zReverse)
    rot = new Quaternion (-rot.x, rot.y, -rot.z, rot.w);

///////////////////////////////////////
///////////////////////////////////////
var inputRot = Vector3( parseFloat(lineEntry[i]), parseFloat(lineEntry[i+1]), parseFloat(lineEntry[i+2]) );
var rot = Quaternion.Euler( inputRot );
var changedRot = Quaternion( rot.x , -rot.y, rot.z , rot.w );

///////////////////////////////////////
///////////////////////////////////////
??
var changedRot = Quaternion( rot.x , -rot.y, rot.z , rot.w );
??
var changedRot = Quaternion( -rot.x , rot.y, rot.z, -rot.w );

///////////////////////////////////////
///////////////////////////////////////
var reOrdered = ZXYtoXYZ( Vector3(recordObj.eulerAngles.x, -recordObj.eulerAngles.y, -recordObj.eulerAngles.z) );
function ZXYtoXYZ(v : Vector3)  
   var qx = Quaternion.AngleAxis(v.x, Vector3.right);
   var qy = Quaternion.AngleAxis(v.y, Vector3.up);
   var qz = Quaternion.AngleAxis(v.z, Vector3.forward);
   var qq = qz * qy * qx;
   return qq.eulerAngles;
}


///////////////////////////////////////
///////////////////////////////////////
var qRot : Quaternion = Quaternion.Euler(mayaRotation); // Convert Vector3 output from Maya to Quaternion
var mirrorQuat : Quaternion = Quaternion(-qRot.x, qRot.y, qRot.z, -qRot.w); // Mirror the quaternion on X  W
var reOrderedEulers : Vector3 = XYZtoZXY(mirrorQuat.eulerAngles); // Reorder XYZ (maya) to ZXY (unity)
theObject.transform.localEulerAngles = Vector3(reOrderedEulers.x, reOrderedEulers.y, reOrderedEulers.z);
theObject.transform.Rotate(new Vector3(0, 180, 0));
 
static function XYZtoZXY(v : Vector3) : Vector3 {
   var qx : Quaternion = Quaternion.AngleAxis(v.x, Vector3.right);
   var qy : Quaternion = Quaternion.AngleAxis(v.y, Vector3.up);
   var qz : Quaternion = Quaternion.AngleAxis(v.z, Vector3.forward);
   var qq : Quaternion = qz * qx * qy;
   return qq.eulerAngles;
}


///////////////////////////////////////
///////////////////////////////////////
static function MayaRotationToUnity(rotation : Vector3) : Quaternion {
   var flippedRotation : Vector3 = Vector3(rotation.x, -rotation.y, -rotation.z); // flip Y and Z axis for right->left handed conversion
   // convert XYZ to ZYX
   var qx : Quaternion = Quaternion.AngleAxis(flippedRotation.x, Vector3.right);
   var qy : Quaternion = Quaternion.AngleAxis(flippedRotation.y, Vector3.up);
   var qz : Quaternion = Quaternion.AngleAxis(flippedRotation.z, Vector3.forward);
   var qq : Quaternion = qz * qy * qx ; // this is the order
   return qq;
}

///////////////////////////////////////
///////////////////////////////////////

For the rotation, I found the following : 
If you have your rotations around each axis, 
and applied in the order : X, Y then Z, 
in a right handed system, then in a left handed system, 
it will be : X, -Y, -Z.


///////////////////////////////////////
///////////////////////////////////////

I believe (having experience with Kinect & Unity3d) that what he wants is 
the same Rotation in a coordinate space where Z is inverted. 
That comes down to mirroring around the XY plane. 
To mirror a quaternion around the XY plane, you need to negate the Z value and the W value.

#endif