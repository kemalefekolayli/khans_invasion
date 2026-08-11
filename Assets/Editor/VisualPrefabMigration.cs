using TMPro;
using UnityEditor;
using UnityEngine;

public static class VisualPrefabMigration
{
    private const string TribePrefabPath = "Assets/Resources/Tribes/TribePrefab.prefab";
    private const string GuiPrefabPath = "Assets/prefabs/GUI.prefab";
    private const string ArmyPrefabPath = "Assets/prefabs/Chars/SoldierPrefab.prefab";
    private const string GeneralPrefabPath = "Assets/prefabs/Chars/HorsePrefab.prefab";

    [MenuItem("Tools/Khan's Invasion/Migrate Tribe and GUI Prefabs")]
    public static void Migrate()
    {
        MigrateTribePrefab();
        MigrateGuiPrefab();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[VisualPrefabMigration] TribePrefab and GUI prefab updated.");
    }

    [MenuItem("Tools/Khan's Invasion/Setup Army Supply Text")]
    public static void SetupArmySupplyText()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(ArmyPrefabPath);
        TMP_FontAsset armyFont = null;
        try
        {
            Transform supplyTransform = root.transform.Find("SupplyText");
            TextMeshPro supplyText;
            if (supplyTransform == null)
            {
                GameObject supplyObject = new GameObject("SupplyText", typeof(RectTransform), typeof(TextMeshPro));
                supplyTransform = supplyObject.transform;
                supplyTransform.SetParent(root.transform, false);
                supplyTransform.localPosition = new Vector3(0f, 2.65f, 0f);
                supplyTransform.localScale = Vector3.one;
                supplyText = supplyObject.GetComponent<TextMeshPro>();
            }
            else
            {
                supplyText = supplyTransform.GetComponent<TextMeshPro>();
                if (supplyText == null) supplyText = supplyTransform.gameObject.AddComponent<TextMeshPro>();
            }

            TextMeshProUGUI existingText = root.transform.Find("TextHolder")?.GetComponent<TextMeshProUGUI>();
            if (existingText != null) { supplyText.font = existingText.font; armyFont = existingText.font; }
            supplyText.text = "100/100";
            supplyText.fontSize = 8f;
            supplyText.alignment = TextAlignmentOptions.Center;
            supplyText.color = Color.white;
            supplyText.sortingOrder = 5;

            ArmySupplyWorldText display = root.GetComponent<ArmySupplyWorldText>();
            if (display == null) display = root.AddComponent<ArmySupplyWorldText>();
            SerializedObject serializedDisplay = new SerializedObject(display);
            serializedDisplay.FindProperty("supplyText").objectReferenceValue = supplyText;
            serializedDisplay.ApplyModifiedPropertiesWithoutUndo();

            Transform lootTransform = root.transform.Find("CarriedLootText");
            TextMeshPro lootText;
            if (lootTransform == null)
            {
                GameObject lootObject = new GameObject("CarriedLootText", typeof(RectTransform), typeof(TextMeshPro));
                lootTransform = lootObject.transform;
                lootTransform.SetParent(root.transform, false);
                lootTransform.localPosition = new Vector3(0f, 2.35f, 0f);
                lootTransform.localScale = Vector3.one;
                lootText = lootObject.GetComponent<TextMeshPro>();
            }
            else
            {
                lootText = lootTransform.GetComponent<TextMeshPro>();
                if (lootText == null) lootText = lootTransform.gameObject.AddComponent<TextMeshPro>();
            }
            if (existingText != null) lootText.font = existingText.font;
            lootText.text = "0";
            lootText.fontSize = 8f;
            lootText.alignment = TextAlignmentOptions.Center;
            lootText.color = Color.white;
            lootText.sortingOrder = 5;

            ArmyLootWorldText lootDisplay = root.GetComponent<ArmyLootWorldText>();
            if (lootDisplay == null) lootDisplay = root.AddComponent<ArmyLootWorldText>();
            SerializedObject serializedLootDisplay = new SerializedObject(lootDisplay);
            serializedLootDisplay.FindProperty("lootText").objectReferenceValue = lootText;
            serializedLootDisplay.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, ArmyPrefabPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[VisualPrefabMigration] Army supply text configured on SoldierPrefab.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        MigrateArmyDisplaysToGeneral(armyFont);
    }

    private static void MigrateArmyDisplaysToGeneral(TMP_FontAsset font)
    {
        GameObject armyRoot = PrefabUtility.LoadPrefabContents(ArmyPrefabPath);
        try
        {
            RemoveDisplayComponent<ArmySupplyWorldText>(armyRoot);
            RemoveDisplayComponent<ArmyLootWorldText>(armyRoot);
            RemoveChild(armyRoot, "SupplyText");
            RemoveChild(armyRoot, "CarriedLootText");
            PrefabUtility.SaveAsPrefabAsset(armyRoot, ArmyPrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(armyRoot); }

        GameObject generalRoot = PrefabUtility.LoadPrefabContents(GeneralPrefabPath);
        try
        {
            TextMeshPro supplyText = EnsureWorldText(generalRoot, "SupplyText", new Vector3(0f, 2.65f, 0f), font, "100/100");
            TextMeshPro lootText = EnsureWorldText(generalRoot, "CarriedLootText", new Vector3(0f, 2.35f, 0f), font, "0");

            ArmySupplyWorldText supplyDisplay = generalRoot.GetComponent<ArmySupplyWorldText>();
            if (supplyDisplay == null) supplyDisplay = generalRoot.AddComponent<ArmySupplyWorldText>();
            SerializedObject serializedSupply = new SerializedObject(supplyDisplay);
            serializedSupply.FindProperty("supplyText").objectReferenceValue = supplyText;
            serializedSupply.ApplyModifiedPropertiesWithoutUndo();

            ArmyLootWorldText lootDisplay = generalRoot.GetComponent<ArmyLootWorldText>();
            if (lootDisplay == null) lootDisplay = generalRoot.AddComponent<ArmyLootWorldText>();
            SerializedObject serializedLoot = new SerializedObject(lootDisplay);
            serializedLoot.FindProperty("lootText").objectReferenceValue = lootText;
            serializedLoot.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(generalRoot, GeneralPrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(generalRoot); }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[VisualPrefabMigration] Moved army supply/loot displays to HorsePrefab.");
    }

    private static void RemoveDisplayComponent<T>(GameObject root) where T : Component
    {
        T component = root.GetComponent<T>();
        if (component != null) Object.DestroyImmediate(component, true);
    }

    private static void RemoveChild(GameObject root, string childName)
    {
        Transform child = root.transform.Find(childName);
        if (child != null) Object.DestroyImmediate(child.gameObject, true);
    }

    private static TextMeshPro EnsureWorldText(GameObject root, string name, Vector3 position, TMP_FontAsset font, string initialText)
    {
        Transform textTransform = root.transform.Find(name);
        TextMeshPro text;
        if (textTransform == null)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshPro));
            textTransform = textObject.transform;
            textTransform.SetParent(root.transform, false);
            text = textObject.GetComponent<TextMeshPro>();
        }
        else
        {
            text = textTransform.GetComponent<TextMeshPro>();
            if (text == null) text = textTransform.gameObject.AddComponent<TextMeshPro>();
        }
        float rootScale = Mathf.Max(0.001f, Mathf.Abs(root.transform.localScale.y));
        textTransform.localPosition = new Vector3(position.x / rootScale, position.y / rootScale, position.z);
        textTransform.localScale = Vector3.one / rootScale;
        if (font != null) text.font = font;
        text.text = initialText;
        text.fontSize = 8f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.sortingOrder = 5;
        return text;
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
