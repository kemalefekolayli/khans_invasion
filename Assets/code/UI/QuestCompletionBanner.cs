using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Screen-space toast banner shown when a quest completes or its reward is claimed.
/// Fades in, holds ~3 seconds, then fades out and returns itself to its pool.
/// Non-blocking: its CanvasGroup has blocksRaycasts=false and all raycastTargets are off.
/// </summary>
public class QuestCompletionBanner : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float fadeInDuration = 0.25f;
    [SerializeField] private float holdDuration = 2.5f;
    [SerializeField] private float fadeOutDuration = 0.35f;
    [SerializeField] private float startScale = 0.92f;
    [SerializeField] private float dropDistance = 40f;

    private ComponentPool<QuestCompletionBanner> pool;
    private QuestCompletionPopupSpawner owner;
    private CanvasGroup canvasGroup;
    private TextMeshProUGUI messageText;
    private Vector2 restPosition;
    private float elapsed;
    private float totalDuration;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        messageText = GetComponentInChildren<TextMeshProUGUI>();
    }

    /// <summary>
    /// Associates this banner with its pool and spawner so it returns (not destroys)
    /// and notifies the spawner when it is done so the next queued banner can show.
    /// </summary>
    public void BindPool(ComponentPool<QuestCompletionBanner> popupPool, QuestCompletionPopupSpawner spawner)
    {
        pool = popupPool;
        owner = spawner;
    }

    /// <summary>
    /// Starts the banner with the given message from a pooled (inactive) state.
    /// </summary>
    public void Initialize(string message)
    {
        if (messageText != null) messageText.text = message;

        elapsed = 0f;
        totalDuration = fadeInDuration + holdDuration + fadeOutDuration;
        restPosition = ((RectTransform)transform).anchoredPosition;

        if (canvasGroup != null) canvasGroup.alpha = 0f;
        transform.localScale = Vector3.one * startScale;
        transform.localPosition = restPosition + Vector2.up * dropDistance;

        gameObject.SetActive(true);
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        if (elapsed >= totalDuration)
        {
            Finish();
            return;
        }

        if (elapsed < fadeInDuration)
        {
            float t = Mathf.Clamp01(elapsed / fadeInDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            if (canvasGroup != null) canvasGroup.alpha = eased;
            transform.localScale = Vector3.Lerp(Vector3.one * startScale, Vector3.one, eased);
            transform.localPosition = restPosition + Vector2.up * (dropDistance * (1f - eased));
        }
        else if (elapsed < fadeInDuration + holdDuration)
        {
            if (canvasGroup != null) canvasGroup.alpha = 1f;
        }
        else
        {
            float t = Mathf.Clamp01((elapsed - fadeInDuration - holdDuration) / fadeOutDuration);
            if (canvasGroup != null) canvasGroup.alpha = 1f - t;
        }
    }

    private void Finish()
    {
        if (pool != null) pool.Return(this);
        if (owner != null) owner.OnBannerFinished(this);
    }
}
