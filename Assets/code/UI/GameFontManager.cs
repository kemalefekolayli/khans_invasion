using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFontManager : MonoBehaviour
{
    public static GameFontManager Instance { get; private set; }

    [Header("Font Source")]
    [Tooltip("Optional direct TMP font asset. If assigned, this wins over the Resources font path.")]
    [SerializeField] private TMP_FontAsset overrideFontAsset;

    [Tooltip("Unity Font loaded from Assets/Resources. Default points to Assets/Resources/Fonts/KiwiSoda.ttf")]
    [SerializeField] private string resourcesFontPath = "Fonts/KiwiSoda";

    [Header("City Name Font")]
    [Tooltip("Optional TMP font for province/city names. Leave empty to use TextMesh Pro's default font.")]
    [SerializeField] private TMP_FontAsset cityNameFontAsset;
    [SerializeField] private Color cityNameColor = Color.white;
    [SerializeField] private float cityNameFontSize = 36f;

    [Header("Apply")]
    [SerializeField] private bool applyOnStart = true;
    [SerializeField] private bool scanForNewText = true;
    [SerializeField] private float scanInterval = 0.75f;
    
    [Header("Global Text Color")]
    [SerializeField] private bool overrideTextColor = false;
    [SerializeField] private Color textColor = Color.white;

    private TMP_FontAsset runtimeFontAsset;
    private readonly HashSet<TMP_Text> appliedTexts = new HashSet<TMP_Text>();
    private static readonly HashSet<TMP_Text> cityNameTexts = new HashSet<TMP_Text>();
    private float nextScanTime;

    public TMP_FontAsset ActiveFont => overrideFontAsset != null ? overrideFontAsset : runtimeFontAsset;
    public TMP_FontAsset CityNameFont => cityNameFontAsset != null ? cityNameFontAsset : TMP_Settings.defaultFontAsset;
    public Color CityNameColor => cityNameColor;
    public float CityNameFontSize => cityNameFontSize;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntimeInstance()
    {
        if (Instance != null || FindFirstObjectByType<GameFontManager>() != null) return;

        GameObject managerObject = new GameObject("GameFontManager");
        managerObject.AddComponent<GameFontManager>();
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadRuntimeFont();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Start()
    {
        if (applyOnStart)
        {
            ApplyToAllText();
        }
    }

    private void Update()
    {
        if (!scanForNewText || Time.unscaledTime < nextScanTime) return;

        nextScanTime = Time.unscaledTime + scanInterval;
        ApplyToAllText();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        appliedTexts.Clear();
        LoadRuntimeFont();
        ApplyToAllText();
    }

    private void LoadRuntimeFont()
    {
        if (overrideFontAsset != null || runtimeFontAsset != null) return;

        Font sourceFont = Resources.Load<Font>(resourcesFontPath);
        if (sourceFont == null)
        {
            GameLog.Warning(GameLogCategory.Core, $"[GameFontManager] Font not found at Resources/{resourcesFontPath}. Using TMP defaults.");
            return;
        }

        runtimeFontAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
        runtimeFontAsset.name = $"{sourceFont.name} Runtime TMP";
        GameLog.Log(GameLogCategory.Core, $"[GameFontManager] Loaded font: {sourceFont.name}");
    }

    [ContextMenu("Apply Font To All Text")]
    public void ApplyToAllText()
    {
        TMP_FontAsset fontAsset = ActiveFont;
        if (fontAsset == null)
        {
            LoadRuntimeFont();
            fontAsset = ActiveFont;
            if (fontAsset == null) return;
        }

        TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
        foreach (TMP_Text text in texts)
        {
            if (IsCityNameText(text)) continue;

            ApplyTo(text);
        }
    }

    public static void Apply(TMP_Text text)
    {
        if (text == null) return;

        if (Instance == null)
        {
            GameObject managerObject = new GameObject("GameFontManager");
            managerObject.AddComponent<GameFontManager>();
        }

        Instance.ApplyTo(text);
    }

    public void ApplyTo(TMP_Text text)
    {
        TMP_FontAsset fontAsset = ActiveFont;
        if (text == null || fontAsset == null) return;

        if (appliedTexts.Contains(text) && text.font == fontAsset && (!overrideTextColor || text.color == textColor)) return;

        text.font = fontAsset;
        if (overrideTextColor)
        {
            text.color = textColor;
        }

        appliedTexts.Add(text);
    }

    public static void ApplyCityNameFont(TMP_Text text)
    {
        if (text == null) return;

        if (Instance == null)
        {
            GameObject managerObject = new GameObject("GameFontManager");
            managerObject.AddComponent<GameFontManager>();
        }

        Instance.ApplyToCityName(text);
    }

    public void ApplyToCityName(TMP_Text text)
    {
        if (text == null) return;

        TMP_FontAsset fontAsset = CityNameFont;
        if (fontAsset != null)
        {
            text.font = fontAsset;
        }

        text.enableAutoSizing = false;
        text.fontSize = cityNameFontSize;
        text.color = cityNameColor;
        text.fontStyle = FontStyles.Bold;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.ForceMeshUpdate();
    }

    private bool IsCityNameText(TMP_Text text)
    {
        return text != null && cityNameTexts.Contains(text);
    }

    public static void RegisterCityNameText(TMP_Text text)
    {
        if (text != null) cityNameTexts.Add(text);
    }

    public static void UnregisterCityNameText(TMP_Text text)
    {
        if (text != null) cityNameTexts.Remove(text);
    }

    public void SetOverrideFont(TMP_FontAsset fontAsset)
    {
        overrideFontAsset = fontAsset;
        appliedTexts.Clear();
        ApplyToAllText();
    }

    public void SetResourcesFontPath(string path)
    {
        resourcesFontPath = path;
        runtimeFontAsset = null;
        appliedTexts.Clear();
        LoadRuntimeFont();
        ApplyToAllText();
    }

    public void SetGlobalTextColor(Color color, bool enabled = true)
    {
        textColor = color;
        overrideTextColor = enabled;
        appliedTexts.Clear();
        ApplyToAllText();
    }
}
