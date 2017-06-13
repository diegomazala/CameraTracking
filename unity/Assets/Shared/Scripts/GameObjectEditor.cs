using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System;
using System.IO;


public class GameObjectEditor : MonoBehaviour
{
    private KeyCode keyTrigger = KeyCode.F6;
    private Rect guiWindow;
    private bool m_ShowGUI = false;
    public GameObject[] objectList = null;
    private int objectIndex;

    private int m_WindowId = 104;

    void Awake()  
    {        
        guiWindow = new Rect(10, 10, 520, 600);
    }

    void Update()
    {
        if (Input.GetKeyDown(keyTrigger))																			//show or not the Transform interface
        {
            m_ShowGUI = !m_ShowGUI;
        }
    }

    void OnGUI()
    {

        if (m_ShowGUI == false)
            return;

        guiWindow = GUI.Window(m_WindowId, guiWindow, MyGUI, "Game Object (Meshes) Settings [F6]");
    }


    void MyGUI(int windowID)
    {
        GUILayout.BeginHorizontal();
        ListGUI();
        TransformGUI();
        GUILayout.EndHorizontal();
    }


    void ListGUI()
    {
        bool lButtonChecked = false;
        GUILayout.BeginVertical("box", GUILayout.MinHeight(500));

        GUILayout.Label("           Object List", GUILayout.MinWidth(150), GUILayout.MinHeight(30));
        for (int i = 0; i < objectList.Length; i++)
        {
            lButtonChecked = i == objectIndex ? true : false;

            if (GUILayout.Toggle(lButtonChecked, objectList[i].name))
                objectIndex = i;
        }
        GUILayout.EndVertical();
    }


    void TransformGUI()
    {
        GameObject lObjSelection = null;

        if (objectIndex > -1 && objectIndex < objectList.Length)
            lObjSelection = objectList[objectIndex];

        Vector3 lPos = Vector3.zero,
                    lRot = Vector3.zero,
                    lScale = Vector3.zero;

        String pxStr, pyStr, pzStr,
                rxStr, ryStr, rzStr,
                sxStr, syStr, szStr;




        if (lObjSelection != null)
        {
            lPos = lObjSelection.transform.position * 100;		// converting from meters to centimeters
            lRot = lObjSelection.transform.eulerAngles;
            lScale = lObjSelection.transform.localScale * 100;	// converting to percentual range
        }



        GUILayout.BeginVertical();
        // Drawing the gui for position

        GUILayout.Label("Position (cm)");
        GUILayout.BeginHorizontal();
        pxStr = GUILayout.TextField(lPos.x.ToString(), GUILayout.MinWidth(40));
        pyStr = GUILayout.TextField(lPos.y.ToString(), GUILayout.MinWidth(40));
        pzStr = GUILayout.TextField(lPos.z.ToString(), GUILayout.MinWidth(40));
        GUILayout.EndHorizontal();
        // Drawing the gui for ratation
        GUILayout.Label("Rotation (º)");
        GUILayout.BeginHorizontal();
        rxStr = GUILayout.TextField(lRot.x.ToString(), GUILayout.MinWidth(40));
        ryStr = GUILayout.TextField(lRot.y.ToString(), GUILayout.MinWidth(40));
        rzStr = GUILayout.TextField(lRot.z.ToString(), GUILayout.MinWidth(40));
        GUILayout.EndHorizontal();
        // Drawing the gui for scale
        GUILayout.Label("Scale (%)");
        GUILayout.BeginHorizontal();
        sxStr = GUILayout.TextField(lScale.x.ToString(), GUILayout.MinWidth(40));
        syStr = GUILayout.TextField(lScale.y.ToString(), GUILayout.MinWidth(40));
        szStr = GUILayout.TextField(lScale.z.ToString(), GUILayout.MinWidth(40));
        GUILayout.EndHorizontal();


        GUILayout.BeginHorizontal("box");
        {
            if (lObjSelection != null)
                lObjSelection.SetActive(GUILayout.Toggle(lObjSelection.activeSelf, "Enabled"));
        }
        GUILayout.EndHorizontal();

        GUI.DragWindow();
        GUILayout.EndVertical();


        // Converting position from string to float
        try { lPos.x = float.Parse(pxStr, NumberStyles.Number); }
        catch { lPos.x = 0; }
        try { lPos.y = float.Parse(pyStr, NumberStyles.Number); }
        catch { lPos.y = 0; }
        try { lPos.z = float.Parse(pzStr, NumberStyles.Number); }
        catch { lPos.z = 0; }

        // Converting rotation from string to float
        try { lRot.x = float.Parse(rxStr, NumberStyles.Number); }
        catch { lRot.x = 0; }
        try { lRot.y = float.Parse(ryStr, NumberStyles.Number); }
        catch { lRot.y = 0; }
        try { lRot.z = float.Parse(rzStr, NumberStyles.Number); }
        catch { lRot.z = 0; }

        // Converting scale from string to float
        try { lScale.x = float.Parse(sxStr, NumberStyles.Number); }
        catch { lScale.x = 1; }
        try { lScale.y = float.Parse(syStr, NumberStyles.Number); }
        catch { lScale.y = 1; }
        try { lScale.z = float.Parse(szStr, NumberStyles.Number); }
        catch { lScale.z = 1; }

        if (lObjSelection != null)
        {
            lObjSelection.transform.position = lPos / 100;		// converting back to meters
            lObjSelection.transform.eulerAngles = lRot;
            lObjSelection.transform.localScale = lScale / 100;	// converting back to 0-1 range
        }

    }

}
