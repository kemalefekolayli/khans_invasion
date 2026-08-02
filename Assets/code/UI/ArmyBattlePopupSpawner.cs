using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ArmyBattlePopupSpawner : MonoBehaviour
{
    public static ArmyBattlePopupSpawner Instance { get; private set; }

    [Header("Display")]
    [SerializeField] private bool useRecommendedRuntimeDefaults = true;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.6f, 0f);
    [SerializeField] private float panelScale = 0.58f;
    [SerializeField] private float panelFollowSmoothness = 12f;
    [SerializeField] private int baseSortingOrder = 5000;
    [SerializeField] private Color fallbackLeftColor = new Color(0.1f, 0.8f, 0.25f, 1f);
    [SerializeField] private Color fallbackRightColor = new Color(0.9f, 0.15f, 0.15f, 1f);
    [SerializeField] private Color borderColor = new Color(0.45f, 0.55f, 0.08f, 1f);

    [Header("Bar")]
    [SerializeField] private float barWidth = 6.75f;
    [SerializeField] private float barHeight = 0.82f;
    [SerializeField] private float borderThickness = 0.08f;
    [SerializeField] private Vector3 barOffset = new Vector3(0f, -0.04f, -0.04f);

    [Header("Text")]
    [SerializeField] private float soldierFontSize = 2.35f;
    [SerializeField] private float ratioFontSize = 1.2f;
    [SerializeField] private Color textColor = Color.white;

    [Header("Markers")]
    [SerializeField] private float horseScale = 1.75f;
    [SerializeField] private float horseYOffset = 0.84f;
    [SerializeField] private float markerPushOffset = 0.34f;

    [Header("Dice")]
    [SerializeField] private bool useGeneratedDiceSprites = true;
    [SerializeField] private float diceScale = 0.64f;
    [SerializeField] private float diceXOffset = 0.76f;
    [SerializeField] private float diceYOffset = 0.62f;

    [Header("Resources")]
    [SerializeField] private string panelFillPath = "BattleSystem/panel_fill";
    [SerializeField] private string panelFramePath = "BattleSystem/panel_rectangle";
    [SerializeField] private string leftHorsePath = "BattleSystem/east";
    [SerializeField] private string rightHorsePath = "BattleSystem/west";
    [SerializeField] private string diceSheetPath = "BattleSystem/six_sided_die";

    private readonly Dictionary<int, BattlePanel> panels = new Dictionary<int, BattlePanel>();
    private readonly Dictionary<Army, int> panelByArmy = new Dictionary<Army, int>();

    private Sprite panelFillSprite;
    private Sprite panelFrameSprite;
    private Sprite leftHorseSprite;
    private Sprite rightHorseSprite;
    private Sprite[] diceSprites;
    private Sprite whiteSprite;

    public static bool HasActivePanels => Instance != null && Instance.panels.Count > 0;

    private class BattlePanel
    {
        public GameObject root;
        public Army leftArmy;
        public Army rightArmy;
        public SpriteRenderer leftFill;
        public SpriteRenderer rightFill;
        public SpriteRenderer leftHorse;
        public SpriteRenderer rightHorse;
        public SpriteRenderer leftDice;
        public SpriteRenderer rightDice;
        public TextMeshPro leftSoldiers;
        public TextMeshPro rightSoldiers;
        public TextMeshPro ratioText;
        public Color leftColor;
        public Color rightColor;
        public float targetRatio = 0.5f;
        public float currentRatio = 0.5f;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntimeInstance()
    {
        if (Instance != null || FindFirstObjectByType<ArmyBattlePopupSpawner>() != null) return;

        GameObject popupObject = new GameObject("ArmyBattlePopupSpawner");
        popupObject.AddComponent<ArmyBattlePopupSpawner>();
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            ApplyRecommendedRuntimeDefaults();
            LoadAssets();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        GameEvents.OnArmyBattleStarted += HandleBattleStarted;
        GameEvents.OnArmyBattleTick += HandleBattleTick;
        GameEvents.OnArmyBattleEnded += HandleBattleEnded;
    }

    private void OnDisable()
    {
        GameEvents.OnArmyBattleStarted -= HandleBattleStarted;
        GameEvents.OnArmyBattleTick -= HandleBattleTick;
        GameEvents.OnArmyBattleEnded -= HandleBattleEnded;
    }

    private void LateUpdate()
    {
        foreach (BattlePanel panel in panels.Values)
        {
            if (panel?.root == null) continue;

            panel.root.transform.position = Vector3.Lerp(
                panel.root.transform.position,
                GetPopupPosition(panel.leftArmy, panel.rightArmy),
                Time.deltaTime * panelFollowSmoothness);

            panel.currentRatio = Mathf.Lerp(panel.currentRatio, panel.targetRatio, Time.deltaTime * panelFollowSmoothness);
            UpdateBar(panel);
        }
    }

    private void LoadAssets()
    {
        panelFillSprite = LoadSprite(panelFillPath, 100f);
        panelFrameSprite = LoadSprite(panelFramePath, 100f);
        leftHorseSprite = LoadSprite(leftHorsePath, 56f);
        rightHorseSprite = LoadSprite(rightHorsePath, 56f);
        diceSprites = LoadDiceSprites();
        whiteSprite = CreateWhiteSprite();
    }

    private void ApplyRecommendedRuntimeDefaults()
    {
        if (!useRecommendedRuntimeDefaults) return;

        worldOffset = new Vector3(0f, 1.85f, 0f);
        panelScale = 0.72f;
        baseSortingOrder = 5000;
        barWidth = 6.75f;
        barHeight = 0.82f;
        borderThickness = 0.08f;
        barOffset = new Vector3(0f, -0.04f, -0.04f);
        soldierFontSize = 2.35f;
        ratioFontSize = 1.2f;
        horseScale = 1.75f;
        horseYOffset = 0.84f;
        markerPushOffset = 0.42f;
        useGeneratedDiceSprites = true;
        diceScale = 0.64f;
        diceXOffset = 0.76f;
        diceYOffset = 0.62f;
    }

    private Sprite LoadSprite(string path, float pixelsPerUnit)
    {
        Texture2D texture = Resources.Load<Texture2D>(path);
        if (texture == null)
        {
            GameLog.Warning(GameLogCategory.Core, $"[ArmyBattlePopupSpawner] Missing texture at Resources/{path}");
            return null;
        }

        ConfigurePixelTexture(texture);

        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }

    private Sprite[] LoadDiceSprites()
    {
        if (useGeneratedDiceSprites)
            return GenerateDiceSprites();

        Texture2D texture = Resources.Load<Texture2D>(diceSheetPath);
        if (texture == null)
        {
            GameLog.Warning(GameLogCategory.Core, $"[ArmyBattlePopupSpawner] Missing dice sheet at Resources/{diceSheetPath}");
            return GenerateDiceSprites();
        }

        ConfigurePixelTexture(texture);

        Sprite[] sprites = new Sprite[6];
        const int cellSize = 16;
        int topRowY = texture.height - cellSize;

        for (int i = 0; i < 6; i++)
        {
            Rect rect = new Rect(i * cellSize, topRowY, cellSize, cellSize);
            sprites[i] = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), cellSize);
        }

        return sprites;
    }

    private Sprite[] GenerateDiceSprites()
    {
        Sprite[] sprites = new Sprite[6];
        for (int roll = 1; roll <= 6; roll++)
        {
            Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color shadow = new Color(0.35f, 0.35f, 0.42f, 1f);
            Color outline = new Color(0.12f, 0.12f, 0.16f, 1f);
            Color rim = new Color(0.72f, 0.72f, 0.78f, 1f);
            Color face = new Color(0.96f, 0.95f, 0.98f, 1f);
            Color highlight = Color.white;
            Color pip = new Color(0.08f, 0.08f, 0.1f, 1f);

            Fill(texture, clear);
            FillRect(texture, 5, 7, 23, 21, shadow);
            FillRoundedRect(texture, 3, 4, 25, 23, 4, outline);
            FillRoundedRect(texture, 5, 6, 21, 19, 3, rim);
            FillRoundedRect(texture, 6, 7, 19, 17, 2, face);
            FillRect(texture, 8, 21, 12, 2, highlight);
            DrawPips(texture, roll, pip);

            texture.Apply();
            ConfigurePixelTexture(texture);
            sprites[roll - 1] = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
        }

        return sprites;
    }

    private void Fill(Texture2D texture, Color color)
    {
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
                texture.SetPixel(x, y, color);
        }
    }

    private void FillRect(Texture2D texture, int x, int y, int width, int height, Color color)
    {
        for (int py = y; py < y + height; py++)
        {
            for (int px = x; px < x + width; px++)
            {
                if (px >= 0 && px < texture.width && py >= 0 && py < texture.height)
                    texture.SetPixel(px, py, color);
            }
        }
    }

    private void FillRoundedRect(Texture2D texture, int x, int y, int width, int height, int radius, Color color)
    {
        for (int py = y; py < y + height; py++)
        {
            for (int px = x; px < x + width; px++)
            {
                int left = px - x;
                int right = x + width - 1 - px;
                int bottom = py - y;
                int top = y + height - 1 - py;
                int cornerX = Mathf.Min(left, right);
                int cornerY = Mathf.Min(bottom, top);

                if (cornerX >= radius || cornerY >= radius || cornerX * cornerX + cornerY * cornerY >= radius)
                    texture.SetPixel(px, py, color);
            }
        }
    }

    private void DrawPips(Texture2D texture, int roll, Color color)
    {
        Vector2Int center = new Vector2Int(16, 16);
        Vector2Int topLeft = new Vector2Int(11, 20);
        Vector2Int topRight = new Vector2Int(21, 20);
        Vector2Int midLeft = new Vector2Int(11, 16);
        Vector2Int midRight = new Vector2Int(21, 16);
        Vector2Int bottomLeft = new Vector2Int(11, 11);
        Vector2Int bottomRight = new Vector2Int(21, 11);

        if (roll == 1 || roll == 3 || roll == 5) DrawPip(texture, center, color);
        if (roll >= 2)
        {
            DrawPip(texture, topLeft, color);
            DrawPip(texture, bottomRight, color);
        }
        if (roll >= 4)
        {
            DrawPip(texture, topRight, color);
            DrawPip(texture, bottomLeft, color);
        }
        if (roll == 6)
        {
            DrawPip(texture, midLeft, color);
            DrawPip(texture, midRight, color);
        }
    }

    private void DrawPip(Texture2D texture, Vector2Int center, Color color)
    {
        FillRect(texture, center.x - 1, center.y - 1, 3, 3, color);
    }

    private Sprite CreateWhiteSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        ConfigurePixelTexture(texture);
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    private void ConfigurePixelTexture(Texture2D texture)
    {
        if (texture == null) return;

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.anisoLevel = 0;
    }

    private void HandleBattleStarted(Army armyA, Army armyB)
    {
        if (!ShouldShowBattle(armyA, armyB)) return;

        int key = GetBattleId(armyA, armyB);
        if (panels.ContainsKey(key)) return;

        Army leftArmy = PickLeftArmy(armyA, armyB);
        Army rightArmy = leftArmy == armyA ? armyB : armyA;
        BattlePanel panel = CreatePanel(key, leftArmy, rightArmy);

        panels.Add(key, panel);
        panelByArmy[leftArmy] = key;
        panelByArmy[rightArmy] = key;
        RefreshPanel(panel, 0, 0);
    }

    private Army PickLeftArmy(Army armyA, Army armyB)
    {
        if (armyA != null && armyA.IsPlayerArmy) return armyA;
        if (armyB != null && armyB.IsPlayerArmy) return armyB;
        return armyA;
    }

    private BattlePanel CreatePanel(int key, Army leftArmy, Army rightArmy)
    {
        GameObject root = new GameObject($"BattlePanel_{key}");
        root.transform.position = GetPopupPosition(leftArmy, rightArmy);

        BattlePanel panel = new BattlePanel
        {
            root = root,
            leftArmy = leftArmy,
            rightArmy = rightArmy,
            leftColor = GetArmyColor(leftArmy, fallbackLeftColor),
            rightColor = GetArmyColor(rightArmy, fallbackRightColor)
        };

        panel.leftFill = CreateSprite(root.transform, "LeftAdvantageFill", whiteSprite, panel.leftColor, baseSortingOrder + 20);
        panel.rightFill = CreateSprite(root.transform, "RightAdvantageFill", whiteSprite, panel.rightColor, baseSortingOrder + 20);
        CreatePanelBorder(root.transform);

        panel.leftHorse = CreateSprite(root.transform, "LeftHorseMarker", leftHorseSprite, Color.white, baseSortingOrder + 40);
        panel.rightHorse = CreateSprite(root.transform, "RightHorseMarker", rightHorseSprite, Color.white, baseSortingOrder + 40);
        if (panel.leftHorse != null) panel.leftHorse.transform.localScale = Vector3.one * horseScale;
        if (panel.rightHorse != null) panel.rightHorse.transform.localScale = Vector3.one * horseScale;

        panel.leftDice = CreateSprite(root.transform, "LeftDice", GetDiceSprite(1), Color.white, baseSortingOrder + 41);
        panel.rightDice = CreateSprite(root.transform, "RightDice", GetDiceSprite(1), Color.white, baseSortingOrder + 41);
        if (panel.leftDice != null)
        {
            panel.leftDice.transform.localPosition = new Vector3(-diceXOffset, diceYOffset, -0.08f);
            panel.leftDice.transform.localScale = Vector3.one * diceScale;
        }
        if (panel.rightDice != null)
        {
            panel.rightDice.transform.localPosition = new Vector3(diceXOffset, diceYOffset, -0.08f);
            panel.rightDice.transform.localScale = Vector3.one * diceScale;
        }

        panel.leftSoldiers = CreateText(root.transform, "LeftSoldiers", new Vector3(-2.35f, 0.5f, -0.09f), soldierFontSize);
        panel.rightSoldiers = CreateText(root.transform, "RightSoldiers", new Vector3(2.35f, 0.5f, -0.09f), soldierFontSize);
        panel.ratioText = CreateText(root.transform, "RatioText", new Vector3(0f, -0.5f, -0.09f), ratioFontSize);

        return panel;
    }

    private void CreatePanelBorder(Transform parent)
    {
        float outerWidth = barWidth + borderThickness * 2f;
        float outerHeight = barHeight + borderThickness * 2f;

        SpriteRenderer top = CreateSprite(parent, "PanelBorderTop", whiteSprite, borderColor, baseSortingOrder + 30);
        top.transform.localPosition = barOffset + new Vector3(0f, outerHeight * 0.5f - borderThickness * 0.5f, -0.02f);
        top.transform.localScale = new Vector3(outerWidth, borderThickness, 1f);

        SpriteRenderer bottom = CreateSprite(parent, "PanelBorderBottom", whiteSprite, borderColor, baseSortingOrder + 30);
        bottom.transform.localPosition = barOffset + new Vector3(0f, -outerHeight * 0.5f + borderThickness * 0.5f, -0.02f);
        bottom.transform.localScale = new Vector3(outerWidth, borderThickness, 1f);

        SpriteRenderer left = CreateSprite(parent, "PanelBorderLeft", whiteSprite, borderColor, baseSortingOrder + 30);
        left.transform.localPosition = barOffset + new Vector3(-outerWidth * 0.5f + borderThickness * 0.5f, 0f, -0.02f);
        left.transform.localScale = new Vector3(borderThickness, outerHeight, 1f);

        SpriteRenderer right = CreateSprite(parent, "PanelBorderRight", whiteSprite, borderColor, baseSortingOrder + 30);
        right.transform.localPosition = barOffset + new Vector3(outerWidth * 0.5f - borderThickness * 0.5f, 0f, -0.02f);
        right.transform.localScale = new Vector3(borderThickness, outerHeight, 1f);
    }

    private SpriteRenderer CreateSprite(Transform parent, string name, Sprite sprite, Color color, int sortingOrder)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private TextMeshPro CreateText(Transform parent, string name, Vector3 localPosition, float size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPosition;
        obj.transform.localRotation = Quaternion.identity;

        TextMeshPro text = obj.AddComponent<TextMeshPro>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = size;
        text.color = textColor;
        text.sortingOrder = baseSortingOrder + 42;
        GameFontManager.Apply(text);
        return text;
    }

    private void HandleBattleTick(Army armyA, Army armyB, float armyALoss, float armyBLoss, int armyARoll, int armyBRoll, int turn)
    {
        int key = GetBattleId(armyA, armyB);
        if (!panels.TryGetValue(key, out BattlePanel panel)) return;

        int leftRoll = panel.leftArmy == armyA ? armyARoll : armyBRoll;
        int rightRoll = panel.rightArmy == armyA ? armyARoll : armyBRoll;
        RefreshPanel(panel, leftRoll, rightRoll);
    }

    private void RefreshPanel(BattlePanel panel, int leftRoll, int rightRoll)
    {
        if (panel == null) return;

        float leftSize = panel.leftArmy != null ? panel.leftArmy.ArmySize : 0f;
        float rightSize = panel.rightArmy != null ? panel.rightArmy.ArmySize : 0f;
        float total = Mathf.Max(1f, leftSize + rightSize);
        panel.targetRatio = Mathf.Clamp01(leftSize / total);

        if (panel.leftSoldiers != null)
            panel.leftSoldiers.text = leftSize.ToString("F0");

        if (panel.rightSoldiers != null)
            panel.rightSoldiers.text = rightSize.ToString("F0");

        if (panel.ratioText != null)
        {
            float ratio = rightSize > 0.01f ? leftSize / rightSize : leftSize;
            panel.ratioText.text = $"{ratio:F2}:1";
        }

        if (leftRoll > 0 && panel.leftDice != null)
            panel.leftDice.sprite = GetDiceSprite(leftRoll);

        if (rightRoll > 0 && panel.rightDice != null)
            panel.rightDice.sprite = GetDiceSprite(rightRoll);
    }

    private void UpdateBar(BattlePanel panel)
    {
        float leftWidth = barWidth * panel.currentRatio;
        float rightWidth = barWidth - leftWidth;
        float leftCenter = -barWidth * 0.5f + leftWidth * 0.5f;
        float rightCenter = barWidth * 0.5f - rightWidth * 0.5f;

        if (panel.leftFill != null)
        {
            panel.leftFill.transform.localPosition = barOffset + new Vector3(leftCenter, 0f, 0f);
            panel.leftFill.transform.localScale = new Vector3(Mathf.Max(0.01f, leftWidth), barHeight, 1f);
        }

        if (panel.rightFill != null)
        {
            panel.rightFill.transform.localPosition = barOffset + new Vector3(rightCenter, 0f, 0f);
            panel.rightFill.transform.localScale = new Vector3(Mathf.Max(0.01f, rightWidth), barHeight, 1f);
        }

        float boundary = Mathf.Lerp(-barWidth * 0.5f, barWidth * 0.5f, panel.currentRatio);
        if (panel.leftHorse != null)
            panel.leftHorse.transform.localPosition = barOffset + new Vector3(Mathf.Clamp(boundary - markerPushOffset, -barWidth * 0.5f, barWidth * 0.5f), horseYOffset, -0.06f);

        if (panel.rightHorse != null)
            panel.rightHorse.transform.localPosition = barOffset + new Vector3(Mathf.Clamp(boundary + markerPushOffset, -barWidth * 0.5f, barWidth * 0.5f), horseYOffset, -0.06f);
    }

    private void HandleBattleEnded(Army winner, Army loser, ArmyBattleEndReason reason)
    {
        int key = FindPanelKey(winner, loser);
        if (key == 0) return;
        if (!panels.TryGetValue(key, out BattlePanel panel)) return;

        panels.Remove(key);
        RemoveArmyMappingsForPanel(key);

        if (panel.root == null) return;

        if (reason != ArmyBattleEndReason.Retreated && winner != null)
        {
            panel.targetRatio = winner == panel.leftArmy ? 1f : 0f;
            panel.currentRatio = panel.targetRatio;
            UpdateBar(panel);
            if (panel.ratioText != null)
                panel.ratioText.text = "Victory";
        }
        else if (panel.ratioText != null)
        {
            panel.ratioText.text = "Retreat";
        }

        Destroy(panel.root, 1.5f);
    }

    private Sprite GetDiceSprite(int roll)
    {
        if (diceSprites == null || diceSprites.Length == 0) return null;
        return diceSprites[Mathf.Clamp(roll, 1, 6) - 1];
    }

    private Color GetArmyColor(Army army, Color fallback)
    {
        if (army?.OwnerNation == null || string.IsNullOrEmpty(army.OwnerNation.nationColor))
            return fallback;

        return ColorUtility.TryParseHtmlString(army.OwnerNation.nationColor, out Color color) ? color : fallback;
    }

    private bool ShouldShowBattle(Army armyA, Army armyB)
    {
        if (armyA == null || armyB == null) return false;
        if (armyA.IsPlayerArmy || armyB.IsPlayerArmy) return true;

        return IsArmyVisible(armyA) || IsArmyVisible(armyB);
    }

    private bool IsArmyVisible(Army army)
    {
        return army != null
            && (army.CurrentProvince == null
                || FogOfWarManager.Instance == null
                || FogOfWarManager.Instance.IsDiscovered(army.CurrentProvince));
    }

    private Vector3 GetPopupPosition(Army armyA, Army armyB)
    {
        if (armyA == null && armyB == null) return worldOffset;
        if (armyA == null) return armyB.transform.position + worldOffset;
        if (armyB == null) return armyA.transform.position + worldOffset;

        return (armyA.transform.position + armyB.transform.position) * 0.5f + worldOffset;
    }

    private int FindPanelKey(Army winner, Army loser)
    {
        if (winner != null && loser != null)
        {
            int key = GetBattleId(winner, loser);
            return panels.ContainsKey(key) ? key : 0;
        }

        if (winner != null && panelByArmy.TryGetValue(winner, out int winnerKey))
            return winnerKey;

        if (loser != null && panelByArmy.TryGetValue(loser, out int loserKey))
            return loserKey;

        foreach (int key in panels.Keys)
            return key;

        return 0;
    }

    private int GetBattleId(Army armyA, Army armyB)
    {
        int idA = armyA != null ? armyA.GetInstanceID() : 0;
        int idB = armyB != null ? armyB.GetInstanceID() : 0;
        if (idA > idB)
        {
            int temp = idA;
            idA = idB;
            idB = temp;
        }

        unchecked
        {
            return (idA * 397) ^ idB;
        }
    }

    private void RemoveArmyMappingsForPanel(int key)
    {
        List<Army> armiesToRemove = new List<Army>();
        foreach (var entry in panelByArmy)
        {
            if (entry.Value == key)
                armiesToRemove.Add(entry.Key);
        }

        foreach (Army army in armiesToRemove)
            panelByArmy.Remove(army);
    }
}
