[System.Serializable]
public class NationJson
{
    public int id;
    public string name;
    public string color;
    public string aggression;
    public string description;
}

[System.Serializable]
public class NationListWrapper
{
    public NationJson[] nations;
}