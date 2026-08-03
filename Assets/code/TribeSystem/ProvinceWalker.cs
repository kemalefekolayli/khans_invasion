using System.Collections;
using UnityEngine;

/// <summary>Reusable province-to-province movement with no combat or ownership knowledge.</summary>
public class ProvinceWalker : MonoBehaviour
{
    public ProvinceModel CurrentProvince { get; private set; }
    public bool IsMoving => moveRoutine != null;
    private Coroutine moveRoutine;

    public void SetProvince(ProvinceModel province)
    {
        CurrentProvince = province;
    }

    public void MoveTo(ProvinceModel target, float duration)
    {
        if (target == null) return;
        CancelMovement();
        moveRoutine = StartCoroutine(MoveRoutine(target, Mathf.Max(0.05f, duration)));
    }

    public void CancelMovement()
    {
        if (moveRoutine == null) return;
        StopCoroutine(moveRoutine);
        moveRoutine = null;
    }

    private IEnumerator MoveRoutine(ProvinceModel target, float duration)
    {
        Vector3 start = transform.position;
        Vector3 destination = target.transform.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(start, destination, elapsed / duration);
            yield return null;
        }

        transform.position = destination;
        CurrentProvince = target;
        moveRoutine = null;
    }
}