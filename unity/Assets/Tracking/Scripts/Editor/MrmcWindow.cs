//C# Example
using UnityEditor;
using UnityEngine;
using System.Runtime.InteropServices;

public class MrmcWindow : EditorWindow
{
    private class Plugin
    {
        [DllImport("mrmc_net_command")]
        public static extern void MrmcStop();

        [DllImport("mrmc_net_command")]
        public static extern void MrmcPlay();

        [DllImport("mrmc_net_command")]
        public static extern void MrmcGoto(int frame);
    }



    GUIStyle guiStyle;
    Texture stopTex;
    Texture playTex;
    Texture nextTex;
    int frameNumber = 0;

    MrmcTcpClient tcpClient = new MrmcTcpClient();

    [MenuItem("Window/M.R.M.C.")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(MrmcWindow));
    }


    void OnGUI()
    {
        LoadButtonTextures();

        EditorGUILayout.BeginHorizontal();
        {
            GUILayoutOption[] layoutOptions = { GUILayout.Width(30), GUILayout.Height(30)};

            if (GUILayout.Button(stopTex, layoutOptions))
            {
                Plugin.MrmcStop();
            }

            if (GUILayout.Button(playTex, layoutOptions))
            {
                Plugin.MrmcPlay();
                tcpClient.Send();
            }

            frameNumber = EditorGUILayout.IntField(frameNumber, GUILayout.Width(50), GUILayout.Height(30));

            if (GUILayout.Button(nextTex, layoutOptions))
            {
                Plugin.MrmcGoto(frameNumber);
            }

            GUILayout.FlexibleSpace();
        }
        EditorGUILayout.EndHorizontal();

    }

    void LoadButtonTextures()
    {
        if (guiStyle == null)
        {
            guiStyle = new GUIStyle(GUI.skin.textField);
            guiStyle.alignment = TextAnchor.MiddleCenter;
        }

        if (stopTex == null)
            stopTex = (Texture)Resources.Load("stop");

        if (playTex == null)
            playTex = (Texture)Resources.Load("play");

        if (nextTex == null)
            nextTex = (Texture)Resources.Load("next");
    }

}