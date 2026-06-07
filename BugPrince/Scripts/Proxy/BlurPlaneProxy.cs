using System.Collections.Generic;
using BugPrince.Scripts.InternalLib;
using UnityEngine;

namespace BugPrince.Scripts.Proxy;

// Fork of SFCore's BlurPlanePatcher.cs
[Shim]
public class BlurPlaneProxy : MonoBehaviour
{
    private static readonly Dictionary<float, Material> materials = [];

    [ShimField("53.7f")]
    public float blurSize = 53.7f;

    private static Material GetBluePlaneMaterial(float size)
    {
        if (materials.TryGetValue(size, out var mat))
            return mat;

        mat = new(Shader.Find("UI/Blur/UIBlur"));
        mat.SetColor(Shader.PropertyToID("_TintColor"), new Color(1.0f, 1.0f, 1.0f, 0.0f));
        mat.SetFloat(Shader.PropertyToID("_Size"), size);
        mat.SetFloat(Shader.PropertyToID("_Vibrancy"), 0.2f);
        mat.SetInt(Shader.PropertyToID("_StencilComp"), 8);
        mat.SetInt(Shader.PropertyToID("_Stencil"), 0);
        mat.SetInt(Shader.PropertyToID("_StencilOp"), 0);
        mat.SetInt(Shader.PropertyToID("_StencilWriteMask"), 255);
        mat.SetInt(Shader.PropertyToID("_StencilReadMask"), 255);

        materials[size] = mat;
        return mat;
    }

    private void Awake()
    {
        var mat = GetBluePlaneMaterial(blurSize);
        var meshRenderer = gameObject.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterials = [mat];

        var blurPlane = gameObject.AddComponent<BlurPlane>();
        blurPlane.SetPlaneVisibility(true);
    }
}
