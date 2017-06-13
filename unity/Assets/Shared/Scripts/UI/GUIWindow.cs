using UnityEngine;
using System.Collections;

public class GUIWindow 
{
    private int m_Id;
    private string m_Label;
    private Rect m_Rect;

    public int id
    {
        get { return m_Id; }
    }

    public string label
    {
        get { return m_Label; }
    }

    public Rect rect
    {
        get { return m_Rect; }
        set { m_Rect = value; }
    }



    public GUIWindow(float left, float top, float width, float height, string label)
    {
        m_Id = Random.Range(101, 9999);
        m_Label = label;
        m_Rect = new Rect(left, top, width, height);
    }

    public void Show(GUI.WindowFunction func)
    {
        this.rect = GUILayout.Window(this.id, this.rect, func, this.label);
    }
}
