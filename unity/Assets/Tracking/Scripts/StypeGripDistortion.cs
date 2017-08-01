using UnityEngine;
using System.Globalization;


[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class StypeGripDistortion : MonoBehaviour
{
    public Shader shader;
    private Material m_Material;

    public Vector2 distParams = Vector2.zero;
    public Vector2 chipSize = new Vector2(9.59f, 5.41f);
    public Vector2 centerShift = Vector2.zero;
    public Vector2 texCoordScale = Vector2.one;
    public float opacity;



    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
            material.SetVector("distParams", distParams);
            material.SetVector("chipSize", chipSize);
            material.SetVector("centerShift", centerShift);
            material.SetVector("texCoordScale", texCoordScale);
            material.SetFloat("opacity", opacity);

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
