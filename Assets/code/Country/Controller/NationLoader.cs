using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class NationLoader : MonoBehaviour
{
    [Header("AI Settings")]
    [SerializeField] private AISettings aiSettings;

    public List<NationModel> allNations = new List<NationModel>();
    public Dictionary<int, NationModel> nationsById = new Dictionary<int, NationModel>();

    void Awake()
    {
        LoadNations();
    }

    void LoadNations()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "nations.json");
        
        if (!File.Exists(path))
        {
            GameLog.Error(GameLogCategory.Core, $"nations.json not found at: {path}");
            return;
        }

        string json = File.ReadAllText(path);

        // Parse JSON using Unity's built-in JsonUtility
        NationListWrapper wrapper = JsonUtility.FromJson<NationListWrapper>(json);

        if (wrapper == null || wrapper.nations == null)
        {
            GameLog.Error(GameLogCategory.Core, "Failed to parse nations.json!");
            return;
        }

        foreach (NationJson nj in wrapper.nations)
        {
            NationModel model = new NationModel();
            model.nationId = nj.id;
            model.nationName = nj.name;
            model.nationColor = nj.color;
            model.nationAgression = ConvertAggression(nj.aggression);
            model.baseAggressionLevel = ConvertAggressionLevel(nj.aggression);
            model.effectiveAggressionLevel = model.baseAggressionLevel;

            allNations.Add(model);
            nationsById[nj.id] = model;
        }


        
        // Fire event - nations are ready
        GameEvents.NationsLoaded();
    }

    nationAgression ConvertAggression(string aggr)
    {
        switch (aggr.ToLower())
        {
            case "low": return nationAgression.lightAgression;
            case "medium": return nationAgression.mediumAgression;
            case "high": return nationAgression.heavyAgression;
            case "player": return nationAgression.lightAgression;
            default: return nationAgression.mediumAgression;
        }
    }

    int ConvertAggressionLevel(string aggr)
    {
        if (string.IsNullOrWhiteSpace(aggr))
            return GenerateAggressionLevel();

        if (int.TryParse(aggr, out int numericLevel))
            return Mathf.Clamp(numericLevel, 1, 6);

        switch (aggr.ToLower())
        {
            case "auto":
            case "random":
                return GenerateAggressionLevel();
            case "low": return 2;
            case "medium": return 3;
            case "high": return 5;
            case "player": return 2;
            default: return GenerateAggressionLevel();
        }
    }

    int GenerateAggressionLevel()
    {
        int min = aiSettings != null ? aiSettings.AggressionMin : 1;
        int max = aiSettings != null ? aiSettings.AggressionMax : 6;
        float mean = aiSettings != null ? aiSettings.AggressionMean : 3.5f;
        float stdDev = aiSettings != null ? aiSettings.AggressionStdDev : 1.1f;

        float u1 = Mathf.Max(0.0001f, Random.value);
        float u2 = Random.value;
        float standardNormal = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
        int level = Mathf.RoundToInt(mean + standardNormal * stdDev);
        return Mathf.Clamp(level, min, max);
    }

    public NationModel GetNationById(int id)
    {
        if (nationsById.ContainsKey(id))
        {
            return nationsById[id];
        }
        GameLog.Warning(GameLogCategory.Core, $"Nation with ID {id} not found!");
        return null;
    }

    public Color GetNationColorById(int id)
    {
        NationModel nation = GetNationById(id);
        if (nation != null)
        {
            return HexToColor(nation.nationColor);
        }
        return Color.gray;
    }

    // Convert hex string to Unity Color
    public static Color HexToColor(string hex)
    {
        // Remove # if present
        if (hex.StartsWith("#"))
        {
            hex = hex.Substring(1);
        }

        // Parse RGB
        if (hex.Length == 6)
        {
            int r = int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            int g = int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            int b = int.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }

        GameLog.Warning(GameLogCategory.Core, $"Invalid hex color: {hex}");
        return Color.gray;
    }
}
