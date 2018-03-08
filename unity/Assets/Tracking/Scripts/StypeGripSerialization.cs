using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;


using StypeGripPacket = Tracking.StypeGrip.PacketHF;
//using StypeGripPacket = Tracking.StypeGrip.PacketA5;


[System.Serializable]
public class StypeGripSerialization
{
    public float X;
    public float Y;
    public float Z;
    public float Pan;
    public float Tilt;
    public float Roll;
    public float FovX;
    public float FovY = 40.0f;
    public float AspectRatio = 1.7778f;
    public float Zoom;
    public float Focus;
    public float K1;
    public float K2;
    public float CenterX;
    public float CenterY;
    public float ChipWidth = 9.59f;
    public float ChipHeight = 5.39f;
    public float ImageWidth = 1920.0f;
    public float ImageHeight = 1080.0f;

    public StypeGripSerialization() { }

    public StypeGripSerialization(StypeGripPacket packet)
    {
        FovX = packet.FovX;
        FovY = packet.FovY;
        Zoom = packet.Zoom;
        Focus = packet.Focus;
        AspectRatio = packet.AspectRatio;
        CenterX = packet.CenterX;
        CenterY = packet.CenterY;
        ChipWidth = packet.ChipWidth;
        ChipHeight = packet.ChipHeight;
        K1 = packet.K1;
        K2 = packet.K2;
        X = packet.Position.x;
        Y = packet.Position.y;
        Z = packet.Position.z;
        Pan = packet.EulerAngles.y;
        Tilt = packet.EulerAngles.x;
        Roll = packet.EulerAngles.z;
    }

    public void ApplyToCamera(Camera cam, bool apply_ccd_shift = false, bool apply_distortion = false)
    {
        cam.ResetProjectionMatrix();
        cam.ResetWorldToCameraMatrix();
        cam.transform.localRotation = Quaternion.Euler(Tilt, Pan, Roll);
        cam.transform.localPosition = new Vector3(X, Y, Z);

        cam.aspect = AspectRatio;
        cam.fieldOfView = FovY;


        bool shift_in_pixels = true; // (Packet.GetType() == typeof(Tracking.StypeGrip.PacketA5));

        if (apply_ccd_shift)
        {
            Matrix4x4 p = cam.projectionMatrix;
            if (shift_in_pixels)
            {
                p[0, 2] = 2.0f * CenterX / ImageWidth;
                p[1, 2] = 2.0f * CenterY / ImageHeight;
            }
            else // shift in mm
            {
                p[0, 2] = 2.0f * CenterX / ChipWidth;
                p[1, 2] = 2.0f * CenterY / ChipHeight;
            }
            cam.projectionMatrix = p;
        }
    }


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


