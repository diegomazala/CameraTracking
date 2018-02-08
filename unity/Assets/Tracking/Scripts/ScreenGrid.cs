using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("DTPD/Screen Grid")]
public class ScreenGrid : MonoBehaviour
{
    public Shader shader;
    private Material m_Material;

    public Color color = Color.red;
    public int lineCount = 20;
    public float lineWidth = 0.05f;

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        material.SetColor("_Color", color);
        material.SetFloat("_LineCount", lineCount);
        material.SetFloat("_LineWidth", lineWidth);
        Graphics.Blit(source, destination, material);
    }

    protected Material material
    {
        get
        {
            if (m_Material == null)
            {
                m_Material = new Material(shader);
                m_Material.hideFlags = HideFlags.HideAndDontSave;
            }
            return m_Material;
        }
    }

    protected virtual void OnDisable()
    {
        if (m_Material)
        {
            DestroyImmediate(m_Material);
        }
    }
}
