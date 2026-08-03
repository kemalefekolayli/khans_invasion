using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class Army : MonoBehaviour
{
    [SerializeField] private ArmyData data = new ArmyData();

    [Header("Movement (random walk / repositioning)")]
    [Tooltip("How long a slide between two provinces takes, in seconds")]
    [SerializeField] private float provinceMoveDuration = 1.5f;

    [Header("Defeat Aftermath")]
    [SerializeField] private float defeatRetreatDuration = 2.75f;
    [SerializeField] private float captivityFollowSpeed = 4f;
    [SerializeField] private Vector2 captivityOffset = new Vector2(-0.45f, -0.35f);
    [SerializeField] private int regroupTurnsAfterDefeat = 3;

    [Header("Captive Convoy")]
    [SerializeField] private float captiveFollowDistance = 3f;
    [SerializeField] private float captiveFallbackYOffset = -0.35f;
    [SerializeField] private float captiveChainWidth = 3f;
    [SerializeField] private float captiveChainLengthScale = 6f;
    [SerializeField] private bool useChainVisualPivot = true;
    [SerializeField] private Vector2 fallbackChainPivot = new Vector2(0.475f, 0.705f);
    [SerializeField] private int captiveChainSortingOrder = 2;
    [SerializeField] private string captiveChainSpritePath = "BattleSystem/chain_sprite";
    [SerializeField] private Color captiveChainColor = Color.white;

    // The general commanding this army (set by General class)
    private General commandingGeneral;
    private Coroutine moveCoroutine;
    private Renderer[] visualRenderers;
    private readonly List<Army> captiveArmies = new List<Army>();
    private Army captiveFollowTarget;
    private SpriteRenderer captiveChainRenderer;
    private Sprite captiveChainSprite;
    private string loadedChainSpritePath;
    private bool loadedUseChainVisualPivot;
    private Vector2 loadedFallbackChainPivot;

    // Properties
    public ArmyData Data => data;
    public float ArmySize => data.size;
    public float ArmyQuality => data.quality;
    // Single source of truth: an army is a player army iff its owning nation is the player's.
    // Falls back to the isPlayerOwned flag if OwnerNation hasn't been assigned yet (e.g. mid-construction).
    public bool IsPlayerArmy => OwnerNation != null ? OwnerNation.isPlayer : data.isPlayerOwned;
    public NationModel OwnerNation { get; set; } // Track specific nation ownership
    public ProvinceModel CurrentProvince { get; set; } // Logical position on the province graph
    public General CommandingGeneral => commandingGeneral;
    public bool HasGeneral => commandingGeneral != null;
    public bool IsInBattle { get; private set; }
    public bool IsRetreating { get; private set; }
    public bool IsCaptured { get; private set; }
    public Army CaptorArmy { get; private set; }
    public int RegroupTurnsRemaining { get; private set; }
    public bool IsRegrouping => RegroupTurnsRemaining > 0;
    public bool CanReceiveMovementOrders => !IsInBattle && !IsRetreating && !IsCaptured && !IsRegrouping;


    public void Initialize(float size, float quality, bool isPlayer)
    {
        data.size = size;
        data.quality = quality;
        data.isPlayerOwned = isPlayer;
    }

    private void Start()
    {
        if (ArmyManager.Instance != null)
        {
            ArmyManager.Instance.RegisterArmy(this);
        }

        visualRenderers = GetComponentsInChildren<Renderer>(true);

        RefreshArmyText(); // Ensure text is up to date
    }

    private void OnEnable()
    {
        GameEvents.OnTurnEnded += HandleTurnEnded;
    }

    private void OnDisable()
    {
        GameEvents.OnTurnEnded -= HandleTurnEnded;
    }

    private void Update()
    {
        UpdateCaptivityFollow();
        UpdateFogVisibility();
    }

    /// <summary>
    /// Foreign (non-player) armies are only visible while their current province
    /// has actually been discovered by the player - otherwise their sprite would
    /// show through undiscovered, fogged provinces regardless of fog state.
    /// The player's own armies are always visible.
    /// </summary>
    private void UpdateFogVisibility()
    {
        if (visualRenderers == null) return;

        bool visible = IsPlayerArmy
            || CurrentProvince == null
            || FogOfWarManager.Instance == null
            || !FogOfWarManager.Instance.IsFogActive
            || FogOfWarManager.Instance.IsDiscovered(CurrentProvince);

        foreach (var rend in visualRenderers)
        {
            if (rend != null && rend.enabled != visible)
            {
                rend.enabled = visible;
            }
        }
    }


    public void Initialize(ArmyData armyData)
    {
        data = armyData.Clone();
    }
    
    public void SetCommander(General general)
    {
        commandingGeneral = general;
        
        // Notify follower component if present
        ArmyFollower follower = GetComponent<ArmyFollower>();
        if (follower != null)
        {
            follower.SetFollowTarget(general?.transform);
        }
        
        if (general != null)
            GameLog.Log(GameLogCategory.Core, $"[Army] {data.armyName} now commanded by {general.GeneralName}");
        else
            GameLog.Log(GameLogCategory.Core, $"[Army] {data.armyName} has no commander");
    }
    

    public void AddSoldiers(float count)
    {
        data.size = Mathf.Min(data.size + count, data.maxSize);
        RefreshArmyText();
    }
    
 
    public void RemoveSoldiers(float count)
    {
        data.size = Mathf.Max(data.size - count, 0);
        RefreshArmyText();
        
        if (data.size <= 0)
        {
            OnArmyDestroyed();
        }
    }
    
    /// <summary>
    /// Set army size directly (used for siege casualties).
    /// </summary>
    public void SetArmySize(float newSize)
    {
        float oldSize = data.size;
        data.size = Mathf.Clamp(newSize, 0, data.maxSize);
        RefreshArmyText();
        
        if (data.size <= 0 && oldSize > 0)
        {
            OnArmyDestroyed();
        }
    }
    
    /// <summary>
    /// Refresh the army text display to show current size.
    /// </summary>
    private void RefreshArmyText()
    {
        ArmyText armyText = GetComponentInChildren<ArmyText>();
        if (armyText != null)
        {
            armyText.RefreshDisplay();
        }
        
        GameEvents.ArmySizeChanged(this);
    }

    public void GainExperience(float amount)
    {
        data.quality = Mathf.Min(data.quality + amount, 3.0f);
    }

    public float GetEffectiveStrength()
    {
        return data.EffectiveStrength;
    }

    /// <summary>
    /// Smoothly slide this army from its current position to the target province
    /// (e.g. for AI random-walk within owned territory). Cancels any move in progress.
    /// </summary>
    public void MoveToProvince(ProvinceModel target)
    {
        if (target == null) return;
        if (!CanReceiveMovementOrders) return;

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }
        moveCoroutine = StartCoroutine(SlideToProvince(target));
    }

    public void SetBattleState(bool inBattle)
    {
        IsInBattle = inBattle;

        if (inBattle && moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
    }

    public void ResolveDefeatAftermath(Army winner)
    {
        ReleaseCaptives();

        if (this == null || ArmySize <= 0) return;

        StartRegroupingIfAllied();

        ProvinceModel capital = OwnerNation != null ? OwnerNation.capitalProvince : null;
        if (capital != null && capital.provinceOwner == OwnerNation)
        {
            RetreatToCapital(capital);
        }
        else if (winner != null)
        {
            CaptureBy(winner);
        }
    }

    public void ReleaseCaptives()
    {
        for (int i = captiveArmies.Count - 1; i >= 0; i--)
        {
            Army captive = captiveArmies[i];
            if (captive != null)
                captive.ReleaseFromCaptivity();
        }

        captiveArmies.Clear();
        RefreshCaptiveConvoy();
    }

    public void CaptureBy(Army captor)
    {
        if (captor == null || captor == this) return;

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        if (CaptorArmy != null)
        {
            CaptorArmy.captiveArmies.Remove(this);
            CaptorArmy.RefreshCaptiveConvoy();
        }

        IsCaptured = true;
        IsRetreating = false;
        IsInBattle = false;
        CaptorArmy = captor;
        if (!captor.captiveArmies.Contains(this))
            captor.captiveArmies.Add(this);
        captor.RefreshCaptiveConvoy();

        ArmyFollower follower = GetComponent<ArmyFollower>();
        if (follower != null)
            follower.StopFollowing();

        EnsureCaptiveChain();

        if (commandingGeneral != null)
            commandingGeneral.SetCaptured(captor);

        GameLog.Log(GameLogCategory.Core, $"[Army] {data.armyName} captured by {captor.Data.armyName}");
    }

    public void ReleaseFromCaptivity()
    {
        if (!IsCaptured) return;

        Army oldCaptor = CaptorArmy;
        if (oldCaptor != null)
        {
            oldCaptor.captiveArmies.Remove(this);
            oldCaptor.RefreshCaptiveConvoy();
        }

        IsCaptured = false;
        CaptorArmy = null;
        captiveFollowTarget = null;
        DestroyCaptiveChain();

        if (commandingGeneral != null)
            commandingGeneral.SetCaptured(null);

        ProvinceModel capital = OwnerNation != null ? OwnerNation.capitalProvince : null;
        if (capital != null && capital.provinceOwner == OwnerNation)
            RetreatToCapital(capital);
        else
            RestoreGeneralFollower();

        GameLog.Log(GameLogCategory.Core, $"[Army] {data.armyName} released from captivity");
    }

    private void RetreatToCapital(ProvinceModel capital)
    {
        if (capital == null) return;

        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        IsRetreating = true;
        IsCaptured = false;
        CaptorArmy = null;

        ArmyFollower follower = GetComponent<ArmyFollower>();
        if (follower != null)
            follower.StopFollowing();

        if (commandingGeneral != null)
            commandingGeneral.SetForcedMovement(true);

        moveCoroutine = StartCoroutine(SlideArmyAndGeneralToProvince(capital, defeatRetreatDuration));
    }

    private IEnumerator SlideToProvince(ProvinceModel target)
    {
        Vector3 start = transform.position;
        Vector3 end = target.transform.position;
        float elapsed = 0f;

        while (elapsed < provinceMoveDuration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(start, end, elapsed / provinceMoveDuration);
            yield return null;
        }

        transform.position = end;
        CurrentProvince = target;
        moveCoroutine = null;
    }

    private IEnumerator SlideArmyAndGeneralToProvince(ProvinceModel target, float duration)
    {
        Vector3 armyStart = transform.position;
        Vector3 armyEnd = target.transform.position;
        Transform generalTransform = commandingGeneral != null ? commandingGeneral.transform : null;
        Vector3 generalStart = generalTransform != null ? generalTransform.position : armyStart;
        Vector3 generalOffset = generalTransform != null ? generalStart - armyStart : Vector3.zero;
        Vector3 generalEnd = armyEnd + generalOffset;
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.05f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / safeDuration);
            transform.position = Vector3.Lerp(armyStart, armyEnd, t);

            if (generalTransform != null)
                generalTransform.position = Vector3.Lerp(generalStart, generalEnd, t);

            yield return null;
        }

        transform.position = armyEnd;
        if (generalTransform != null)
            generalTransform.position = generalEnd;

        CurrentProvince = target;
        IsRetreating = false;
        moveCoroutine = null;

        if (commandingGeneral != null)
            commandingGeneral.SetForcedMovement(false);

        RestoreGeneralFollower();
    }

    private void RestoreGeneralFollower()
    {
        ArmyFollower follower = GetComponent<ArmyFollower>();
        if (follower != null)
            follower.SetFollowTarget(commandingGeneral != null ? commandingGeneral.transform : null);
    }

    private void UpdateCaptivityFollow()
    {
        if (!IsCaptured || CaptorArmy == null)
            return;

        Army followTarget = captiveFollowTarget != null ? captiveFollowTarget : CaptorArmy;
        Vector3 targetPosition = GetCaptiveFollowPosition(followTarget);
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, captivityFollowSpeed * Time.deltaTime);
        CurrentProvince = CaptorArmy.CurrentProvince;
        UpdateCaptiveChain(followTarget);
    }

    private Vector3 GetCaptiveFollowPosition(Army followTarget)
    {
        if (followTarget == null)
            return transform.position;

        Vector3 delta = transform.position - followTarget.transform.position;
        if (delta.sqrMagnitude < 0.01f)
            delta = new Vector3(captivityOffset.x, captiveFallbackYOffset, 0f);

        Vector3 direction = delta.normalized;
        return followTarget.transform.position + direction * captiveFollowDistance;
    }

    private void RefreshCaptiveConvoy()
    {
        Army previous = this;
        for (int i = 0; i < captiveArmies.Count; i++)
        {
            Army captive = captiveArmies[i];
            if (captive == null)
            {
                captiveArmies.RemoveAt(i);
                i--;
                continue;
            }

            captive.captiveFollowTarget = previous;
            captive.EnsureCaptiveChain();
            previous = captive;
        }
    }

    private void EnsureCaptiveChain()
    {
        if (captiveChainRenderer != null) return;

        Sprite chainSprite = GetCaptiveChainSprite();
        if (chainSprite == null) return;

        GameObject chainObject = new GameObject("CaptiveChain");

        captiveChainRenderer = chainObject.AddComponent<SpriteRenderer>();
        captiveChainRenderer.sprite = chainSprite;
        ApplyCaptiveChainRendererSettings();
    }

    private void UpdateCaptiveChain(Army followTarget)
    {
        if (followTarget == null)
        {
            DestroyCaptiveChain();
            return;
        }

        EnsureCaptiveChain();
        if (captiveChainRenderer == null || captiveChainRenderer.sprite == null) return;
        captiveChainRenderer.sprite = GetCaptiveChainSprite();
        ApplyCaptiveChainRendererSettings();

        Vector3 start = followTarget.transform.position;
        Vector3 end = transform.position;
        Vector3 midpoint = (start + end) * 0.5f;
        Vector3 delta = end - start;
        float distance = delta.magnitude;

        Transform chainTransform = captiveChainRenderer.transform;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
        chainTransform.position = midpoint;
        chainTransform.rotation = rotation;

        float spriteWidth = Mathf.Max(0.01f, captiveChainRenderer.sprite.bounds.size.x);
        float visibleWidth = Mathf.Max(0.01f, captiveChainWidth);
        float visibleLength = Mathf.Max(0.01f, captiveChainLengthScale);
        chainTransform.localScale = new Vector3((distance / spriteWidth) * visibleLength, visibleWidth, 1f);
    }

    private void ApplyCaptiveChainRendererSettings()
    {
        if (captiveChainRenderer == null) return;

        captiveChainRenderer.color = captiveChainColor;
        captiveChainRenderer.sortingLayerID = GetArmySortingLayerId();
        captiveChainRenderer.sortingOrder = captiveChainSortingOrder;
    }

    private int GetArmySortingLayerId()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != null && renderer != captiveChainRenderer)
                return renderer.sortingLayerID;
        }

        return SortingLayer.NameToID("Default");
    }

    private void DestroyCaptiveChain()
    {
        if (captiveChainRenderer == null) return;

        Destroy(captiveChainRenderer.gameObject);
        captiveChainRenderer = null;
    }

    private Sprite GetCaptiveChainSprite()
    {
        if (captiveChainSprite != null
            && loadedChainSpritePath == captiveChainSpritePath
            && loadedUseChainVisualPivot == useChainVisualPivot
            && loadedFallbackChainPivot == fallbackChainPivot)
        {
            return captiveChainSprite;
        }

        Texture2D texture = Resources.Load<Texture2D>(captiveChainSpritePath);
        if (texture == null)
        {
            GameLog.Warning(GameLogCategory.Core, $"[Army] Missing captive chain sprite at Resources/{captiveChainSpritePath}");
            captiveChainSprite = GenerateFallbackChainSprite();
            RememberChainSpriteSettings();
            return captiveChainSprite;
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.anisoLevel = 0;

        Vector2 pivot = useChainVisualPivot ? GetChainVisualPivot(texture) : new Vector2(0.5f, 0.5f);

        captiveChainSprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            pivot,
            Mathf.Max(1f, texture.height));

        RememberChainSpriteSettings();
        return captiveChainSprite;
    }

    private void RememberChainSpriteSettings()
    {
        loadedChainSpritePath = captiveChainSpritePath;
        loadedUseChainVisualPivot = useChainVisualPivot;
        loadedFallbackChainPivot = fallbackChainPivot;

        if (captiveChainRenderer != null)
            captiveChainRenderer.sprite = captiveChainSprite;
    }

    private Vector2 GetChainVisualPivot(Texture2D texture)
    {
        if (texture == null) return fallbackChainPivot;

        try
        {
            Color32[] pixels = texture.GetPixels32();
            int minX = texture.width;
            int minY = texture.height;
            int maxX = -1;
            int maxY = -1;

            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    Color32 pixel = pixels[y * texture.width + x];
                    if (pixel.a <= 10) continue;

                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            if (maxX < minX || maxY < minY)
                return fallbackChainPivot;

            float centerX = (minX + maxX + 1f) * 0.5f;
            float centerY = (minY + maxY + 1f) * 0.5f;
            return new Vector2(centerX / texture.width, centerY / texture.height);
        }
        catch (System.Exception)
        {
            return fallbackChainPivot;
        }
    }

    private Sprite GenerateFallbackChainSprite()
    {
        Texture2D texture = new Texture2D(32, 8, TextureFormat.RGBA32, false);
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color dark = new Color(0.08f, 0.08f, 0.09f, 1f);
        Color metal = new Color(0.75f, 0.75f, 0.78f, 1f);

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
                texture.SetPixel(x, y, clear);
        }

        for (int x = 0; x < texture.width; x++)
        {
            texture.SetPixel(x, 3, dark);
            texture.SetPixel(x, 4, metal);
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.height);
    }

    private void StartRegroupingIfAllied()
    {
        if (!IsPlayerArmy) return;

        RegroupTurnsRemaining = Mathf.Max(RegroupTurnsRemaining, regroupTurnsAfterDefeat);
        GameLog.Log(GameLogCategory.Core, $"[Army] {data.armyName} regrouping for {RegroupTurnsRemaining} turns");
    }

    private void HandleTurnEnded(int turnNumber)
    {
        if (RegroupTurnsRemaining <= 0) return;

        RegroupTurnsRemaining = Mathf.Max(0, RegroupTurnsRemaining - 1);
        GameLog.Log(GameLogCategory.Core, $"[Army] {data.armyName} regroup turns remaining: {RegroupTurnsRemaining}");
    }

    private void OnArmyDestroyed()
    {
        ReleaseCaptives();

        if (CaptorArmy != null)
        {
            CaptorArmy.captiveArmies.Remove(this);
            CaptorArmy.RefreshCaptiveConvoy();
        }

        DestroyCaptiveChain();

        if (commandingGeneral != null)
            commandingGeneral.SetCaptured(null);

        GameLog.Log(GameLogCategory.Core, $"[Army] {data.armyName} destroyed!");
        
        // Notify general
        if (commandingGeneral != null)
        {
            commandingGeneral.OnArmyLost();
        }
        
        // Fire event
        GameEvents.ArmyDestroyed(this);
        
        // Unregister from manager
        if (ArmyManager.Instance != null)
        {
            ArmyManager.Instance.UnregisterArmy(this);
        }
        
        Destroy(gameObject);
    }
}