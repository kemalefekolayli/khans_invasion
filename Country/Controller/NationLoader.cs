using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class NationLoader : MonoBehaviour
{
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
            Debug.LogError($"nations.json not found at: {path}");
            return;
        }

        string json = File.ReadAllText(path);

        // Parse JSON using Unity's built-in JsonUtility
        NationListWrapper wrapper = JsonUtility.FromJson<NationListWrapper>(json);

        if (wrapper == null || wrapper.nations == null)
        {
            Debug.LogError("Failed to parse nations.json!");
            return;
        }

        foreach (NationJson nj in wrapper.nations)
        {
            NationModel model = new NationModel();
            model.nationId = nj.id;
            model.nationName = nj.name;
            model.nationColor = nj.color;
            model.nationAgression = ConvertAggression(nj.aggression);

            allNations.Add(model);
            nationsById[nj.id] = model;
        }

        Debug.Log($"✓ Loaded {allNations.Count} nations from JSON");
        
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

    public NationModel GetNationById(int id)
    {
        if (nationsById.ContainsKey(id))
        {
            return nationsById[id];
        }
        Debug.LogWarning($"Nation with ID {id} not found!");
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

        Debug.LogWarning($"Invalid hex color: {hex}");
        return Color.gray;
    }
}