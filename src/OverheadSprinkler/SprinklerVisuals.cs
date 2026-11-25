using UnityEngine;
#if IL2CPP
using Il2CppScheduleOne.EntityFramework;
using Il2CppScheduleOne.Property;
#else
using ScheduleOne.EntityFramework;
using ScheduleOne.Property;
#endif

namespace OverheadSprinkler;

/// <summary>
/// Attaches a procedural pipe manifold, cyan status ring, and brass sprinkler nozzle
/// underneath placed Overhead Sprinkler Heads so they are visually distinct from
/// regular Full Spectrum Grow Lights.
/// </summary>
public static class SprinklerVisuals
{
    private const string NozzleRootName = "OverheadSprinklerNozzle";

    private static readonly Color GunmetalPipeColor = new(0.169f, 0.200f, 0.239f, 1f);
    private static readonly Color BrushedSteelColor = new(0.302f, 0.349f, 0.400f, 1f);
    private static readonly Color BrassNozzleColor = new(0.784f, 0.608f, 0.235f, 1f);
    private static readonly Color HydroCyanColor = new(0.133f, 0.655f, 0.941f, 1f);

    private static Material? _pipeMat;
    private static Material? _steelMat;
    private static Material? _brassMat;
    private static Material? _cyanMat;

    /// <summary>
    /// Attaches the sprinkler pipe and nozzle assembly to a placed head if not already present.
    /// </summary>
    public static void EnsureNozzleAttached(ProceduralGridItem? head)
    {
        if (head == null || head.isGhost)
        {
            return;
        }

        if (head.transform.Find(NozzleRootName) != null)
        {
            return;
        }

        MeshRenderer? templateRenderer = head.GetComponentInChildren<MeshRenderer>();
        Material? templateMat = templateRenderer != null ? templateRenderer.sharedMaterial : null;
        EnsureMaterials(templateMat);

        var root = new GameObject(NozzleRootName);
        root.transform.SetParent(head.transform, false);
        root.transform.localPosition = new Vector3(0f, -0.06f, 0f);
        root.transform.localRotation = Quaternion.identity;

        // Horizontal supply manifold pipe
        CreatePart(
            "ManifoldPipe",
            PrimitiveType.Cylinder,
            root.transform,
            new Vector3(0f, 0.03f, 0f),
            Quaternion.Euler(0f, 0f, 90f),
            new Vector3(0.032f, 0.24f, 0.032f),
            _pipeMat);

        // Vertical drop stem
        CreatePart(
            "DropStem",
            PrimitiveType.Cylinder,
            root.transform,
            new Vector3(0f, -0.015f, 0f),
            Quaternion.identity,
            new Vector3(0.024f, 0.045f, 0.024f),
            _steelMat);

        // Hydro-cyan indicator ring
        CreatePart(
            "IndicatorRing",
            PrimitiveType.Cylinder,
            root.transform,
            new Vector3(0f, -0.035f, 0f),
            Quaternion.identity,
            new Vector3(0.042f, 0.010f, 0.042f),
            _cyanMat);

        // Brass nozzle body
        CreatePart(
            "BrassNozzle",
            PrimitiveType.Cylinder,
            root.transform,
            new Vector3(0f, -0.055f, 0f),
            Quaternion.identity,
            new Vector3(0.036f, 0.022f, 0.036f),
            _brassMat);

        // Brass spray tip
        CreatePart(
            "SprayTip",
            PrimitiveType.Sphere,
            root.transform,
            new Vector3(0f, -0.072f, 0f),
            Quaternion.identity,
            new Vector3(0.028f, 0.018f, 0.028f),
            _brassMat);
    }

    /// <summary>
    /// Attaches nozzle visuals to all placed Overhead Sprinkler Heads in all owned properties.
    /// </summary>
    public static void AttachNozzlesInAllOwnedProperties()
    {
        var owned = Property.OwnedProperties;
        if (owned == null)
        {
            return;
        }

        for (int i = 0; i < owned.Count; i++)
        {
            Property prop = owned[i];
            if (prop == null)
            {
                continue;
            }

            var heads = SprinklerController.FindHeadsInProperty(prop);
            for (int h = 0; h < heads.Count; h++)
            {
                EnsureNozzleAttached(heads[h]);
            }
        }
    }

    private static void CreatePart(
        string name,
        PrimitiveType primitive,
        Transform parent,
        Vector3 localPos,
        Quaternion localRot,
        Vector3 localScale,
        Material? material)
    {
        GameObject part = GameObject.CreatePrimitive(primitive);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPos;
        part.transform.localRotation = localRot;
        part.transform.localScale = localScale;

        Collider? col = part.GetComponent<Collider>();
        if (col != null)
        {
            Object.Destroy(col);
        }

        MeshRenderer? renderer = part.GetComponent<MeshRenderer>();
        if (renderer != null && material != null)
        {
            renderer.sharedMaterial = material;
        }
    }

    private static void EnsureMaterials(Material? template)
    {
        if (_pipeMat != null && _steelMat != null && _brassMat != null && _cyanMat != null)
        {
            return;
        }

        Shader? fallbackShader = Shader.Find("Universal Render Pipeline/Lit")
                                 ?? Shader.Find("Standard")
                                 ?? Shader.Find("Sprites/Default");

        _pipeMat ??= CreateTintedMaterial(template, fallbackShader, GunmetalPipeColor);
        _steelMat ??= CreateTintedMaterial(template, fallbackShader, BrushedSteelColor);
        _brassMat ??= CreateTintedMaterial(template, fallbackShader, BrassNozzleColor);
        _cyanMat ??= CreateTintedMaterial(template, fallbackShader, HydroCyanColor);
    }

    private static Material? CreateTintedMaterial(Material? template, Shader? fallbackShader, Color color)
    {
        Material? mat = null;
        if (template != null)
        {
            mat = new Material(template);
        }
        else if (fallbackShader != null)
        {
            mat = new Material(fallbackShader);
        }

        if (mat == null)
        {
            return null;
        }

        mat.color = color;
        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", color);
        }

        return mat;
    }
}
