using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public class NationLoader : MonoBehaviour
{
    public List<NationModel> allNations = new List<NationModel>();

    void Start()
    {
        LoadNations();
    }

    void LoadNations()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "nations.json");
        string json = File.ReadAllText(path);

        // JSON dictionary → <string, NationJson>
        Dictionary<string, NationJson> dict = 
            JsonConvert.DeserializeObject<Dictionary<string, NationJson>>(json);

        foreach (var entry in dict)
        {
            NationJson nj = entry.Value;

            NationModel model = new NationModel();
            model.nationId = nj.id;
            model.nationName = nj.name;
            model.nationColor = nj.color;
            model.nationAgression = ConvertAgression(nj.aggression);

            allNations.Add(model);
        }

        Debug.Log("Loaded nations: " + allNations.Count);
    }

    nationAgression ConvertAgression(string aggr)
    {
        switch (aggr.ToLower())
        {
            case "low": return nationAgression.lightAgression;
            case "medium": return nationAgression.mediumAgression;
            case "high": return nationAgression.heavyAgression;
            case "player": return nationAgression.lightAgression; // veya özel enum eklersin
            default: return nationAgression.mediumAgression;
        }
    }
}
