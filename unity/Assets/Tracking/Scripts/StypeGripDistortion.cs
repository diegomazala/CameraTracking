using UnityEngine;
using System.Globalization;


[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class StypeGripDistortion : MonoBehaviour
{
    public Shader shader;
    private Material m_Material;

    public float PA_w = 9.59f; // mm
    public float AR = 1.7778f;
    public float CSX = 0.0f;
    public float CSY = 0.0f;
    public float K1 = 0.0f;
    public float K2 = 0.0f;
    public float Oversize = 1.0f;



    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        material.SetFloat("PA_w", PA_w);
        material.SetFloat("AR", AR);
        material.SetFloat("CSX", CSX);
        material.SetFloat("CSY", CSY);
        material.SetFloat("K1", K1);
        material.SetFloat("K2", K2);
        material.SetFloat("Oversize", Oversize);

        Graphics.Blit(source, destination, material);
    }


    void OnEnable()
    {
        // Disable if we don't support image effects
        if (!SystemInfo.supportsImageEffects)
        {
            enabled = false;
            return;
        }

        // Disable the image effect if the shader can't
        // run on the users graphics card
        if (!shader || !shader.isSupported)
            enabled = false;
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
