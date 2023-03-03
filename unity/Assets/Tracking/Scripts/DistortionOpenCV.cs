using UnityEngine;
using System.Globalization;


[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class DistortionOpenCV : MonoBehaviour
{
    [SerializeField] private Shader shader;
    private Material m_Material;

    public bool invertedMode = false;
    public bool debugMode = false;
    
    public float cx, cy, k1, k2, p1, p2, k3, k4, k5, k6;
    

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        material.SetFloat("_Cx", cx);
        material.SetFloat("_Cy", cy);
        material.SetFloat("_K1", k1);
        material.SetFloat("_K2", k2);
        material.SetFloat("_K3", k3);
        material.SetFloat("_K4", k4);
        material.SetFloat("_K5", k5);
        material.SetFloat("_K6", k6);
        material.SetFloat("_P1", p1);
        material.SetFloat("_P2", p2);
        material.SetFloat("_Inverted", invertedMode ? 1 : 0);
        material.SetFloat("_Debug", debugMode ? 1 : 0);

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
