//C# Example
using UnityEditor;
using UnityEngine;
using System.Runtime.InteropServices;

public class MrmcWindow : EditorWindow
{
    private class Plugin
    {
        [DllImport("mrmc_net_command")]
        public static extern void MrmcConnect(string host_address, ushort port);

        [DllImport("mrmc_net_command")]
        public static extern void MrmcDisconnect();

        [DllImport("mrmc_net_command")]
        public static extern void MrmcStop();

        [DllImport("mrmc_net_command")]
        public static extern void MrmcShoot();

        [DllImport("mrmc_net_command")]
        public static extern void MrmcForward();

        [DllImport("mrmc_net_command")]
        public static extern void MrmcBackward();

        [DllImport("mrmc_net_command")]
        public static extern void MrmcCleanGoto();

        [DllImport("mrmc_net_command")]
        public static extern void MrmcGoto();

        [DllImport("mrmc_net_command")]
        public static extern void MrmcGotoFrame(float frame);

        [DllImport("mrmc_net_command")]
        public static extern void MrmcGotoPosition(ushort frame);
    }



    GUIStyle guiStyle;
    Texture stopTex;
    Texture playTex;
    Texture nextTex;
    int frameNumber = 0;

    //MrmcTcpClient tcpClient = new MrmcTcpClient();

    [MenuItem("Window/M.R.M.C.")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(MrmcWindow));
    }

    public static EditorWindow GetMainGameView()
    {
        //Creates a game window. Only works if there isn't one already.
        EditorApplication.ExecuteMenuItem("Window/Game");

        System.Type T = System.Type.GetType("UnityEditor.GameView,UnityEditor");
        System.Reflection.MethodInfo GetMainGameView = T.GetMethod("GetMainGameView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        System.Object Res = GetMainGameView.Invoke(null, null);
        return (EditorWindow)Res;
    }


    void OnGUI()
    {
        LoadButtonTextures();

        GUILayoutOption[] layoutOptions = { GUILayout.Width(30), GUILayout.Height(30) };

        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("C", layoutOptions))
            {
                Plugin.MrmcConnect("127.0.0.1", 53025);
            }

            if (GUILayout.Button("D", layoutOptions))
            {
                Plugin.MrmcDisconnect();
            }

            if (GUILayout.Button(stopTex, layoutOptions))
            {
                Plugin.MrmcStop();
            }

            if (GUILayout.Button(playTex, layoutOptions))
            {
                Plugin.MrmcShoot();
            }

            if (GUILayout.Button("F", layoutOptions))
            {
                Plugin.MrmcForward();
            }

            if (GUILayout.Button("B", layoutOptions))
            {
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("G", layoutOptions))
            {
                Plugin.MrmcGoto();
            }

            if (GUILayout.Button("C", layoutOptions))
            {
                Plugin.MrmcCleanGoto();
            }

            frameNumber = EditorGUILayout.IntField(frameNumber, GUILayout.Width(50), GUILayout.Height(30));

            if (GUILayout.Button("GF", layoutOptions))
            {
                Plugin.MrmcGotoFrame((float)frameNumber);
            }

            if (GUILayout.Button("GP", layoutOptions))
            {
                Plugin.MrmcGotoPosition((ushort)frameNumber);
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