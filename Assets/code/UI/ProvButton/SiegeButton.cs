using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class SiegeButton : MonoBehaviour
{
    public Button button;

    void Start()
    {
            if (button != null)
            button.onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
       // logic to siege province 
    }


}