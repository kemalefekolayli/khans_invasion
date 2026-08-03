using TMPro;
using UnityEditor;
using UnityEngine;

public static class VisualPrefabMigration
{
    private const string TribePrefabPath = "Assets/Resources/Tribes/TribePrefab.prefab";
    private const string GuiPrefabPath = "Assets/prefabs/GUI.prefab";

    [MenuItem("Tools/Khan's Invasion/Migrate Tribe and GUI Prefabs")]
    public static void Migrate()
    {
        MigrateTribePrefab();
        MigrateGuiPrefab();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[VisualPrefabMigration] TribePrefab and GUI prefab updated.");
    }

    private static void MigrateTribePrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(TribePrefabPath);
        try
        {
            TribeVisual visual = root.GetComponent<TribeVisual>();
            if (visual == null) visual = root.AddComponent<TribeVisual>();

            SerializedObject serializedVisual = new SerializedObject(visual);
            serializedVisual.FindProperty("characterRenderer").objectReferenceValue = root.GetComponent<SpriteRenderer>();

            SerializedProperty sprites = serializedVisual.FindProperty("characterSprites");
            string[] spritePaths =
            {
                "Assets/KHANS INVASION/ART/KHAN/npc1.png",
                "Assets/KHANS INVASION/ART/KHAN/npc2.png",
                "Assets/KHANS INVASION/ART/KHAN/npc3.png",
                "Assets/KHANS INVASION/ART/KHAN/np4.png",
                "Assets/KHANS INVASION/ART/KHAN/npc5.png"
            };
            sprites.arraySize = spritePaths.Length;
            for (int i = 0; i < spritePaths.Length; i++)
            {
                sprites.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(spritePaths[i]);
            }
            serializedVisual.ApplyModifiedPropertiesWithoutUndo();

            TribeRecruitmentIcon recruitmentIcon = root.GetComponent<TribeRecruitmentIcon>();
            if (recruitmentIcon == null) recruitmentIcon = root.AddComponent<TribeRecruitmentIcon>();
            SerializedObject serializedIcon = new SerializedObject(recruitmentIcon);
            serializedIcon.FindProperty("handshakeSprite").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/KHANS INVASION/ART/gui/handshake.png");
            serializedIcon.ApplyModifiedPropertiesWithoutUndo();

            for (int i = root.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            }

            PrefabUtility.SaveAsPrefabAsset(root, TribePrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void MigrateGuiPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(GuiPrefabPath);
        try
        {
            Transform textRoot = root.transform.Find("Text");
            TextMeshProUGUI lootText = textRoot != null ? textRoot.Find("LootText")?.GetComponent<TextMeshProUGUI>() : null;
            if (lootText == null)
            {
                throw new System.InvalidOperationException("GUI prefab does not contain Text/LootText.");
            }

            TextMeshProUGUI charismaText = textRoot.Find("CharismaText")?.GetComponent<TextMeshProUGUI>();
            if (charismaText == null)
            {
                GameObject label = Object.Instantiate(lootText.gameObject, textRoot);
                label.name = "CharismaText";
                charismaText = label.GetComponent<TextMeshProUGUI>();
                RectTransform labelTransform = charismaText.rectTransform;
                labelTransform.anchoredPosition = lootText.rectTransform.anchoredPosition + new Vector2(125f, -65f);
            }
            charismaText.text = "20";

            PlayerNationGUI gui = root.GetComponent<PlayerNationGUI>();
            SerializedObject serializedGui = new SerializedObject(gui);
            serializedGui.FindProperty("charismaText").objectReferenceValue = charismaText;
            serializedGui.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, GuiPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
