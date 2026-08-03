using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>Shows and handles the handshake interaction when the selected player general reaches an unrecruited tribe.</summary>
public class TribeRecruitmentController : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField, Min(0.01f)] private float contactDistance = 0.75f;

    private CharismaSystem charisma;
    private TribeGroup nearbyTribe;
    private Transform contactTransform;
    private TribeRecruitmentIcon visibleIcon;

    private void Awake()
    {
        charisma = GetComponent<CharismaSystem>();
    }

    private void OnDisable()
    {
        SetVisibleIcon(null);
    }

    private void Update()
    {
        nearbyTribe = FindContactableTribe(out contactTransform);
        SetVisibleIcon(nearbyTribe);
        HandleHandshakeClick();
    }

    private void HandleHandshakeClick()
    {
        if (visibleIcon == null || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (!visibleIcon.ContainsScreenPoint(Camera.main, Mouse.current.position.ReadValue())) return;

        TryRecruitNearbyTribe();
    }

    private TribeGroup FindContactableTribe(out Transform target)
    {
        target = null;
        SelectableGeneral selected = GeneralSelectionManager.Instance?.SelectedGeneral;
        General general = selected != null ? selected.GetComponent<General>() : null;
        if (general == null || general.IsCaptured) return null;

        target = general.transform;
        TribeGroup closest = null;
        float closestDistance = contactDistance;

        foreach (TribeGroup tribe in FindObjectsByType<TribeGroup>(FindObjectsSortMode.None))
        {
            if (tribe == null || tribe.FollowTarget != null) continue;
            float distance = Vector3.Distance(target.position, tribe.transform.position);
            if (distance > closestDistance) continue;
            closestDistance = distance;
            closest = tribe;
        }

        return closest;
    }

    private void SetVisibleIcon(TribeGroup tribe)
    {
        TribeRecruitmentIcon nextIcon = tribe != null ? tribe.GetComponent<TribeRecruitmentIcon>() : null;
        if (visibleIcon == nextIcon) return;

        if (visibleIcon != null) visibleIcon.SetVisible(false);
        visibleIcon = nextIcon;
        if (visibleIcon != null) visibleIcon.SetVisible(true);
    }

    private void TryRecruitNearbyTribe()
    {
        if (nearbyTribe == null || contactTransform == null || charisma == null) return;

        if (nearbyTribe.TryRecruit(contactTransform, charisma))
        {
            SetVisibleIcon(null);
            CenterWarningPopupSpawner.Show($"Tribe joined your host. Charisma {charisma.Current:F0}/{nearbyTribe.RequiredCharisma:F0}");
            return;
        }

        CenterWarningPopupSpawner.Show($"Insufficient charisma: {charisma.Current:F0}/{nearbyTribe.RequiredCharisma:F0} required");
    }
}
