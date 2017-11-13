//C# Example
using UnityEditor;
using UnityEngine;
using System.Runtime.InteropServices;

public class MrmcWindow : EditorWindow
{
    private class Plugin
    {
        [DllImport("mrmc_net_command")]
        public static extern bool MrmcConnect(string host_address, ushort port);

        [DllImport("mrmc_net_command")]
        public static extern void MrmcDisconnect();

        [DllImport("mrmc_net_command")]
        public static extern bool MrmcIsConnected();

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
        public static extern void MrmcGotoFrame(int frame);

        [DllImport("mrmc_net_command")]
        public static extern void MrmcGotoPosition(ushort frame);
    }

    string HostIpAddress = "127.0.0.1";
    int Port = 53025;
    int FrameNumber = 0;
    int PositionNumber = 0;


    [MenuItem("Examples/Inspector Titlebar")]
    static void Init()
    {
        var window = GetWindow(typeof(MrmcWindow));
        window.Show();
    }

    void OnDestroy()
    {
        Plugin.MrmcStop();
    }

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
        bool is_connected = Plugin.MrmcIsConnected();

        EditorGUILayout.BeginVertical(GUILayout.Width(200));
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Width(200));
            {
                EditorGUILayout.LabelField("Ip Address", GUILayout.Width(70));
                HostIpAddress = EditorGUILayout.TextField(HostIpAddress, GUILayout.Width(80));
                Port = EditorGUILayout.IntField(Port, GUILayout.Width(50));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Connect", (is_connected ? "box" : "button"), GUILayout.Width(100)))
                {
                    Plugin.MrmcConnect(HostIpAddress, (ushort)Port);
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Disconnect", GUILayout.Width(100)))
                {
                    Plugin.MrmcDisconnect();
                }
            }
            EditorGUILayout.EndHorizontal();

        }
        EditorGUILayout.EndVertical();

        EditorGUI.BeginDisabledGroup(!is_connected);
        EditorGUILayout.BeginVertical(GUILayout.Width(200));
        {
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Shoot", GUILayout.Width(100)))
                {
                    Plugin.MrmcShoot();
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Stop", GUILayout.Width(100)))
                {
                    Plugin.MrmcStop();
                }
            }
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Forward", GUILayout.Width(100)))
                {
                    Plugin.MrmcForward();
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Backward", GUILayout.Width(100)))
                {
                    Plugin.MrmcBackward();
                }
            }
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("GoTo", GUILayout.Width(100)))
                {
                    Plugin.MrmcGoto();
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Clean", GUILayout.Width(100)))
                {
                    Plugin.MrmcCleanGoto();
                }
            }
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Frame Number: ", GUILayout.Width(100));
                FrameNumber = EditorGUILayout.IntField(FrameNumber, GUILayout.Width(40));
                if (GUILayout.Button("GoTo", GUILayout.Width(60)))
                {
                    Plugin.MrmcGotoFrame(FrameNumber);
                }
            }
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Position Number: ", GUILayout.Width(100));
                PositionNumber = EditorGUILayout.IntField(PositionNumber, GUILayout.Width(40));
                if (GUILayout.Button("GoTo", GUILayout.Width(60)))
                {
                    Plugin.MrmcGotoPosition((ushort)PositionNumber);
                }
            }
            EditorGUILayout.EndHorizontal();


        }
        EditorGUILayout.EndVertical();
        EditorGUI.EndDisabledGroup();
    }

}