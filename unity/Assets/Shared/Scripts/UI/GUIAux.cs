using UnityEngine;
using System.Collections;
using System.Globalization;
using System;

public class GUIAux 
{
    public static bool Switcher(bool flag, string true_label, string false_label, params GUILayoutOption[] options)
    {
        if (GUILayout.Button(true_label, flag ? "box" : "button", options))
            return true;

        if (GUILayout.Button(false_label, flag ? "button" : "box", options))
            return false;

        return flag;
    }


    public static int Int(int value)
    {
        try { return int.Parse(GUILayout.TextField(String.Format("{0:0}", value)), NumberStyles.Number); }
        catch { return 0; }
    }
    public static int Int(int value, GUIStyle guiStyle, params GUILayoutOption[] options)
    {
        try { return int.Parse(GUILayout.TextField(String.Format("{0:0}", value), guiStyle, options), NumberStyles.Number); }
        catch { return 0; }
    }
    


    public static float Float(float value)
    {
        try { return float.Parse(GUILayout.TextField(String.Format("{0:0.00}", value)), NumberStyles.Number); }
        catch { return 0; }
    }
    public static float Float(float value, String stringFormat, GUIStyle guiStyle, params GUILayoutOption[] options)
    {
        try { return float.Parse(GUILayout.TextField(String.Format(stringFormat, value), guiStyle, options), NumberStyles.Number); }
        catch { return 0; }
    }
    public static float Float(float value, GUIStyle guiStyle, params GUILayoutOption[] options)
    {
        try { return float.Parse(GUILayout.TextField(String.Format("{0:0.00}", value), guiStyle, options), NumberStyles.Number); }
        catch { return 0; }
    }


    public static double Double(double value)
    {
        try { return double.Parse(GUILayout.TextField(String.Format("{0:0.00000}", value)), NumberStyles.Number); }
        catch { return 0; }
    }
    public static double Double(double value, String stringFormat, GUIStyle guiStyle, params GUILayoutOption[] options)
    {
        try { return double.Parse(GUILayout.TextField(String.Format(stringFormat, value), guiStyle, options), NumberStyles.Number); }
        catch { return 0; }
    }
    public static double Double(double value, GUIStyle guiStyle, params GUILayoutOption[] options)
    {
        try { return double.Parse(GUILayout.TextField(String.Format("{0:0.00000}", value), guiStyle, options), NumberStyles.Number); }
        catch { return 0; }
    }


    
    public static Vector4 Vector4(Vector4 vec4)
    {
        return Vector4(vec4, "");
    }

    public static Vector4 Vector4(Vector4 vec4, GUIStyle guiStyle, params GUILayoutOption[] options)
    {
        Vector4 lVec = vec4;

        String xStr, yStr, zStr, wStr;


        // Drawing the GUI for position
        GUILayout.BeginHorizontal(guiStyle, options);
        xStr = GUILayout.TextField(String.Format("{0:0.00}", lVec.x), GUILayout.MinWidth(40));
        yStr = GUILayout.TextField(String.Format("{0:0.00}", lVec.y), GUILayout.MinWidth(40));
        zStr = GUILayout.TextField(String.Format("{0:0.00}", lVec.z), GUILayout.MinWidth(40));
        wStr = GUILayout.TextField(String.Format("{0:0.00}", lVec.w), GUILayout.MinWidth(40));
        GUILayout.EndHorizontal();


        // Converting position from string to float
        try { lVec.x = float.Parse(xStr, NumberStyles.Number); }
        catch { lVec.x = 0; }
        try { lVec.y = float.Parse(yStr, NumberStyles.Number); }
        catch { lVec.y = 0; }
        try { lVec.z = float.Parse(zStr, NumberStyles.Number); }
        catch { lVec.z = 0; }
        try { lVec.w = float.Parse(wStr, NumberStyles.Number); }
        catch { lVec.w = 0; }

        return lVec;
    }

    public static Vector3 Vector3(Vector3 vec3)
    {
        return Vector3(vec3, "");
    }

    public static Vector3 Vector3(Vector3 vec3, GUIStyle guiStyle, params GUILayoutOption[] options)
    {
        Vector3 lVec = vec3;

        String xStr, yStr, zStr;


        // Drawing the GUI for position
        GUILayout.BeginHorizontal(guiStyle, options);
        xStr = GUILayout.TextField(String.Format("{0:0.00}", lVec.x), GUILayout.MinWidth(40));
        yStr = GUILayout.TextField(String.Format("{0:0.00}", lVec.y), GUILayout.MinWidth(40));
        zStr = GUILayout.TextField(String.Format("{0:0.00}", lVec.z), GUILayout.MinWidth(40));
        GUILayout.EndHorizontal();


        // Converting position from string to float
        try { lVec.x = float.Parse(xStr, NumberStyles.Number); }
        catch { lVec.x = 0; }
        try { lVec.y = float.Parse(yStr, NumberStyles.Number); }
        catch { lVec.y = 0; }
        try { lVec.z = float.Parse(zStr, NumberStyles.Number); }
        catch { lVec.z = 0; }

        return lVec;
    }

    public static Vector2 Vector2(Vector2 vec2)
    {
        return Vector2(vec2, "{0:0.00}", "");
    }

    public static Vector2 Vector2(Vector2 vec2, GUIStyle guiStyle)
    {
        return Vector2(vec2, "{0:0.00}", guiStyle);
    }

    public static Vector2 Vector2(Vector2 vec2, String stringFormat, GUIStyle guiStyle, params GUILayoutOption[] options)
    {
        Vector2 lVec = vec2;

        String xStr, yStr;

        // Drawing the GUI for position
        GUILayout.BeginHorizontal(guiStyle, options);
        xStr = GUILayout.TextField(String.Format(stringFormat, lVec.x), options);
        yStr = GUILayout.TextField(String.Format(stringFormat, lVec.y), options);
        GUILayout.EndHorizontal();

        // Converting position from string to float
        try { lVec.x = float.Parse(xStr, NumberStyles.Number); }
        catch { lVec.x = 0; }
        try { lVec.y = float.Parse(yStr, NumberStyles.Number); }
        catch { lVec.y = 0; }

        return lVec;
    }


    public static bool Transform(Transform t, string label, bool collapsed)
    {
        GUILayout.BeginVertical("box");
        {
            if (GUILayout.Button((collapsed ? "+ " : "- ") + label, "box"))
                collapsed = !collapsed;

            if (!collapsed)
            {
                GUILayout.BeginHorizontal("box");
                {
                    GUILayout.Label("Position: ");
                    t.localPosition = GUIAux.Vector3(t.localPosition, "box");
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal("box");
                {
                    GUILayout.Label("Rotation: ");
                    t.localEulerAngles = GUIAux.Vector3(t.localEulerAngles, "box");
                }
                GUILayout.EndHorizontal();
            }
        }
        GUILayout.EndVertical();

        return collapsed;
    }
}
