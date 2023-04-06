using UnityEngine;
using System.Globalization;
using Unity.VisualScripting;
using UnityEngine.PlayerLoop;


[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class DistortionOpenCV : MonoBehaviour
{
    [SerializeField] private Shader shader;
    private Material m_Material;

    public bool invertedMode = false;
    public bool debugMode = false;
    

    [SerializeField] private float width = 1920f;
    [SerializeField] private float height = 1200f;
    [SerializeField] private float[] k = new float[5];
    [SerializeField] private float[] distCoeffs = new float[8];
    [SerializeField] private Vector2 scale = Vector2.one;

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        material.SetFloat("_ScaleX", scale.x);
        material.SetFloat("_ScaleY", scale.y);
        material.SetFloat("_Cx", (k[2] - width * 0.5f) / width);
        material.SetFloat("_Cy", (k[3] - height * 0.5f) / height);
        material.SetFloat("_K1", distCoeffs[0]);
        material.SetFloat("_K2", distCoeffs[1]);
        material.SetFloat("_P1", distCoeffs[2]);
        material.SetFloat("_P2", distCoeffs[3]);
        material.SetFloat("_K3", distCoeffs[4]);
        material.SetFloat("_K4", distCoeffs[5]);
        material.SetFloat("_K5", distCoeffs[6]);
        material.SetFloat("_K6", distCoeffs[7]);

        float fx = k[0] / width;
        float fy = k[1] / height;
        float fxd = fx; //711.56335449f / width;
        float fyd = fy; //682.72595215f / height;
        //fx = fy = fxd = fyd = 1f;
        material.SetFloat("_Fx", fx);
        material.SetFloat("_Fy", fy);
        material.SetFloat("_Fdx", fxd);
        material.SetFloat("_Fdy", fyd);
       
       
                                                                         
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
