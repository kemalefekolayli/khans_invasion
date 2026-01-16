using UnityEngine;
using UnityEngine.UI;
public class OpenBarracksMenuButton : MonoBehaviour
{
    // [SerializeField] private Button barracksMenuPrefab;
    [SerializeField] private Button barrackButton;

    private void Awake()
    {
        if(barrackButton != null)
        {
            barrackButton.onClick.AddListener(OnBarracksMenuClicked);
        }
    }

    private void OnBarracksMenuClicked()
    {
        GameEvents.BarrackMenuOpened();
    }
}