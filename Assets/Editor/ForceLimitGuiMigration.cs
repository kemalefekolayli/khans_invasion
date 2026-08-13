using TMPro;
using UnityEditor;
using UnityEngine;

public static class ForceLimitGuiMigration
{
    private const string GuiPrefabPath = "Assets/prefabs/GUI.prefab";

    [MenuItem("Tools/Khans Invasion/Setup Force Limit GUI")]
    public static void SetupForceLimitGui()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(GuiPrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError($"[ForceLimitGuiMigration] Could not load {GuiPrefabPath}.");
            return;
        }

        try
        {
            TextMeshProUGUI cityCountText = FindText(prefabRoot, "CityCountText");
            PlayerNationGUI playerGui = prefabRoot.GetComponentInChildren<PlayerNationGUI>(true);
            if (cityCountText == null || playerGui == null)
            {
                Debug.LogError("[ForceLimitGuiMigration] GUI prefab is missing CityCountText or PlayerNationGUI.");
                return;
            }

            TextMeshProUGUI forceLimitText = FindText(prefabRoot, "ForceLimitText");
            if (forceLimitText == null)
            {
                GameObject duplicate = Object.Instantiate(cityCountText.gameObject, cityCountText.transform.parent);
                duplicate.name = "ForceLimitText";
                forceLimitText = duplicate.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                EditorUtility.CopySerialized(cityCountText, forceLimitText);
            }

            RectTransform cityRect = cityCountText.rectTransform;
            RectTransform forceRect = forceLimitText.rectTransform;
            forceLimitText.gameObject.name = "ForceLimitText";
            forceRect.SetParent(cityRect.parent, false);
            forceRect.anchorMin = cityRect.anchorMin;
            forceRect.anchorMax = cityRect.anchorMax;
            forceRect.pivot = cityRect.pivot;
            forceRect.sizeDelta = cityRect.sizeDelta;
            forceRect.anchoredPosition = new Vector2(244.6f, 196.9f);
            forceRect.localScale = cityRect.localScale;
            forceRect.localRotation = Quaternion.identity;

            forceLimitText.text = "0/0";
            forceLimitText.raycastTarget = false;
            forceLimitText.enableAutoSizing = true;
            forceLimitText.fontSizeMin = 16f;
            forceLimitText.fontSizeMax = 30f;

            SerializedObject serializedGui = new SerializedObject(playerGui);
            SerializedProperty forceLimitProperty = serializedGui.FindProperty("forceLimitText");
            if (forceLimitProperty == null)
            {
                Debug.LogError("[ForceLimitGuiMigration] PlayerNationGUI.forceLimitText was not found.");
                return;
            }

            forceLimitProperty.objectReferenceValue = forceLimitText;
            serializedGui.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, GuiPrefabPath);
            Debug.Log("[ForceLimitGuiMigration] ForceLimitText configured on GUI.prefab.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static TextMeshProUGUI FindText(GameObject root, string objectName)
    {
        foreach (TextMeshProUGUI text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (text.gameObject.name == objectName)
                return text;
        }

        return null;
    }
}
