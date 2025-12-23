using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class RaidButton : MonoBehaviour
{
    public Button button;

    void Start()
    {
            if (button != null)
            button.onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
       // logic to raid province 
    }


}