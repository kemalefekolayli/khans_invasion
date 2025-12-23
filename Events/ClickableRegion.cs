using UnityEngine;

public class ClickableRegion : MonoBehaviour
{
    [SerializeField] private string regionId; // optional

    public void OnClicked()
    {
        Debug.Log($"Clicked region: {name} (id={regionId})");
        // TODO: open UI, select province, etc.
    }
}
