using UnityEngine;
using UnityEngine.UI;
public class GetArmyButton : MonoBehaviour
{
    [SerializeField] private Button getArmyButton;
    [SerializeField] private float startingArmySize = 100f;
    [SerializeField] private float startingArmyQuality = 1.0f;

    private Horse khanHorse;
    private ProvinceModel currentProvince;

    private void Awake()
    {

        if(getArmyButton != null)
        {
            getArmyButton.onClick.AddListener(OnGetArmyButtonClicked);
        }
    }

    public void OnGetArmyButtonClicked()
    {
        ArmyFactory factory = ArmyFactory.Instance;
        if (factory == null)
        {
            factory = FindFirstObjectByType<ArmyFactory>();
        }
        
        if (factory == null)
        {
            Debug.LogError("[KhanArmySetup] ArmyFactory not found!");
            return;
        }
        
        // Create army data
        ArmyData armyData = new ArmyData(startingArmySize, startingArmyQuality, true);
        armyData.armyName = "Horde Army";
                
        if(khanHorse == null)
        {
        
            khanHorse = FindFirstObjectByType<Horse>();
        }  
        
        Vector3 spawnPos = khanHorse.Position;
        // Create army
        Army army = factory.CreateArmy(spawnPos, armyData); 
        // issues with this code, we can create the army but it has not general, so it just stands there.
        // we dont have a way inside the game to assign generals
        // the army size text isnt being updated for some reason
        // ive also realized that the army created this way isnt linked to the khan in any way, so its just a random army
        // but it is registered as a player army which is good I guess
    }
}