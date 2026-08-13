using UnityEngine;
using UnityEngine.UI;

public static class RuntimeScreenCanvasPolicy
{
    public static void Apply(GameObject root, Vector2 referenceResolution, float match)
    {
        if (root == null) return;

        foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
        {
            if (canvas == null || !canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace) continue;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.matchWidthOrHeight = Mathf.Clamp01(match);
        }
    }
}
