using UnityEngine;
using System.Collections.Generic;
[CreateAssetMenu(fileName = "GameStateData", menuName = "Story/Game State Data")]
[System.Serializable]
public class GameStateData : ScriptableObject
{
    public string currentLocation;
    public List<string> visitedLocations = new List<string>();
    public List<string> npcInteracted = new List<string>(); 
    public List<string> itemsCollected = new List<string>(); 
}
