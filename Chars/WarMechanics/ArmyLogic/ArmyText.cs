using UnityEngine;
using TMPro;

public class ArmyText : MonoBehaviour
{
    public TextMeshPro armySizeText;    // Army size
    public TextMeshPro armyStrText;     // Army strength dont need it for now but ill keep this here just in case

    private void OnEnable()
    {
        GameEvents.OnArmySpawned += UpdateArmyText;
    }
    
    private void OnDisable()
    {
        GameEvents.OnArmySpawned -= UpdateArmyText;
    }

    public void UpdateArmyText(Army army, General general)
    {
        if (army == null) return;
        float size = army.ArmySize;
        UpdateArmyText((int)size);
    }
    public void UpdateArmyText(int size)
    {
        if (armySizeText != null)
        {
            armySizeText.text = size.ToString();
        }

    }
}