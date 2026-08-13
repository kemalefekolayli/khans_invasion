using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestCatalog", menuName = "Game/Quest Catalog")]
public class QuestCatalog : ScriptableObject
{
    public List<QuestData> quests = new List<QuestData>();
}
