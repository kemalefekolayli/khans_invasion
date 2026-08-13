using TMPro;
using UnityEditor;
using UnityEngine;

public static class QuestClaimNotificationMigration
{
    private const string GuiPrefabPath = "Assets/prefabs/GUI.prefab";
    private static readonly Color Gold = new Color(1f, 0.82f, 0.2f, 1f);

    [MenuItem("Tools/Khans Invasion/Setup Quest Claim Notification")]
    public static void SetupQuestClaimNotification()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(GuiPrefabPath);
        if (root == null)
        {
            Debug.LogError($"[QuestClaimNotificationMigration] Could not load {GuiPrefabPath}.");
            return;
        }

        try
        {
            Transform endTurnButton = FindTransform(root.transform, "EndTurnButton");
            Transform questPanelButton = FindTransform(root.transform, "QuestPanelButton");
            TMP_Text nearbyText = root.GetComponentInChildren<TMP_Text>(true);
            if (endTurnButton == null || questPanelButton == null || nearbyText == null)
            {
                Debug.LogError("[QuestClaimNotificationMigration] GUI prefab is missing EndTurnButton, QuestPanelButton, or a source TMP.");
                return;
            }

            QuestClaimNotification notification = EnsureSingleNotification(root);
            TextMeshProUGUI claimText = EnsureText(endTurnButton, "QuestClaimText", nearbyText, out bool claimTextCreated);
            TextMeshProUGUI exclamation = EnsureText(questPanelButton, "QuestClaimExclamation", nearbyText, out bool exclamationCreated);

            if (claimTextCreated) ConfigureClaimTextDefaults(claimText);
            if (exclamationCreated) ConfigureExclamationDefaults(exclamation);
            claimText.text = "Quest complete - claim your prize!";
            exclamation.text = "!";
            claimText.raycastTarget = false;
            exclamation.raycastTarget = false;

            SerializedObject serializedNotification = new SerializedObject(notification);
            serializedNotification.FindProperty("claimText").objectReferenceValue = claimText;
            serializedNotification.FindProperty("claimExclamation").objectReferenceValue = exclamation;
            serializedNotification.ApplyModifiedPropertiesWithoutUndo();

            claimText.gameObject.SetActive(false);
            exclamation.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, GuiPrefabPath);
            Debug.Log("[QuestClaimNotificationMigration] Quest claim notification configured on GUI.prefab.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static QuestClaimNotification EnsureSingleNotification(GameObject root)
    {
        QuestClaimNotification[] notifications = root.GetComponents<QuestClaimNotification>();
        QuestClaimNotification notification = notifications.Length > 0
            ? notifications[0]
            : root.AddComponent<QuestClaimNotification>();

        for (int i = 1; i < notifications.Length; i++)
            Object.DestroyImmediate(notifications[i], true);

        return notification;
    }

    private static TextMeshProUGUI EnsureText(Transform parent, string name, TMP_Text source, out bool created)
    {
        Transform existing = parent.Find(name);
        TextMeshProUGUI text = existing != null ? existing.GetComponent<TextMeshProUGUI>() : null;
        created = text == null;
        if (text == null)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            text = textObject.GetComponent<TextMeshProUGUI>();
        }

        if (created)
        {
            if (source.font != null) text.font = source.font;
            text.fontSharedMaterial = source.fontSharedMaterial;
        }
        text.raycastTarget = false;
        return text;
    }

    private static void ConfigureClaimTextDefaults(TextMeshProUGUI text)
    {
        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -42f);
        rect.sizeDelta = new Vector2(620f, 44f);
        text.text = "Quest complete - claim your prize!";
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 26f;
        text.fontStyle = FontStyles.Bold;
        text.color = Gold;
        text.outlineColor = Color.black;
        text.outlineWidth = 0.2f;
        text.raycastTarget = false;
    }

    private static void ConfigureExclamationDefaults(TextMeshProUGUI text)
    {
        RectTransform rect = text.rectTransform;
        rect.anchorMin = Vector2.one;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(38f, 50f);
        text.text = "!";
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 38f;
        text.fontStyle = FontStyles.Bold;
        text.color = Gold;
        text.outlineColor = Color.black;
        text.outlineWidth = 0.25f;
        text.raycastTarget = false;
    }

    private static Transform FindTransform(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindTransform(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
