using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BuilderRaycastMigration
{
    private const string BuilderPrefabPath = "Assets/prefabs/Builder.prefab";

    private static readonly string[] DecorativeOverlayProperties =
    {
        "overlay_farm",
        "overlay_barrack",
        "overlay_barrack2",
        "overlay_fort",
        "overlay_fort2",
        "overlay_house",
        "overlay_house2",
        "overlay_trade"
    };

    [MenuItem("Tools/Khans Invasion/Repair Builder Raycasts")]
    public static void RepairBuilderRaycasts()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(BuilderPrefabPath);
        if (root == null)
        {
            Debug.LogError($"[BuilderRaycastMigration] Could not load {BuilderPrefabPath}.");
            return;
        }

        try
        {
            BuilderOverlayController overlayController = root.GetComponent<BuilderOverlayController>();
            Transform panel = root.transform.Find("Image");
            Image panelBackground = panel != null ? panel.GetComponent<Image>() : null;
            if (overlayController == null || panelBackground == null)
            {
                Debug.LogError("[BuilderRaycastMigration] Builder root is missing BuilderOverlayController or direct child Image background.");
                return;
            }

            BuilderClickRouter[] routers = root.GetComponentsInChildren<BuilderClickRouter>(true);
            if (routers.Length == 0)
            {
                root.AddComponent<BuilderClickRouter>();
                routers = root.GetComponentsInChildren<BuilderClickRouter>(true);
            }

            UIPolygonHotspot[] hotspots = root.GetComponentsInChildren<UIPolygonHotspot>(true);
            ValidateInputArchitecture(root, routers, hotspots);

            panelBackground.raycastTarget = true;

            SerializedObject serializedController = new SerializedObject(overlayController);
            foreach (string propertyName in DecorativeOverlayProperties)
            {
                SerializedProperty property = serializedController.FindProperty(propertyName);
                GameObject overlay = property != null ? property.objectReferenceValue as GameObject : null;
                if (overlay == null)
                {
                    Debug.LogError($"[BuilderRaycastMigration] Decorative overlay reference '{propertyName}' is missing.");
                    return;
                }

                foreach (Graphic graphic in overlay.GetComponentsInChildren<Graphic>(true))
                    graphic.raycastTarget = false;
            }

            // The panel background is the only generic click surface; polygons are data only.
            foreach (UIPolygonHotspot hotspot in hotspots)
            {
                foreach (Graphic graphic in hotspot.GetComponentsInChildren<Graphic>(true))
                    graphic.raycastTarget = false;
            }

            PrefabUtility.SaveAsPrefabAsset(root, BuilderPrefabPath);
            Debug.Log("[BuilderRaycastMigration] Builder decorative raycasts repaired.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ValidateInputArchitecture(
        GameObject root,
        BuilderClickRouter[] routers,
        UIPolygonHotspot[] hotspots)
    {
        if (routers.Length != 1 || routers[0].gameObject != root)
            Fail("Builder prefab must contain exactly one BuilderClickRouter on its root.");

        if (hotspots.Length != 5)
            Fail($"Builder prefab must contain exactly five UIPolygonHotspots; found {hotspots.Length}.");

        HashSet<string> ids = new HashSet<string>();
        foreach (UIPolygonHotspot hotspot in hotspots)
        {
            if (hotspot == null
                || string.IsNullOrEmpty(hotspot.BuildingType)
                || !ids.Add(hotspot.regionId))
            {
                Fail("Builder hotspot IDs must be unique values 1 through 5.");
            }
        }

        for (int id = 1; id <= 5; id++)
        {
            if (!ids.Contains(id.ToString()))
                Fail("Builder hotspot IDs must contain each value from 1 through 5.");
        }
    }

    private static void Fail(string message)
    {
        Debug.LogError($"[BuilderRaycastMigration] {message}");
        throw new InvalidOperationException(message);
    }
}
