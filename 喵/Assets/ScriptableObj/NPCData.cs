using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NPCData", menuName = "Story/NPC Data")]
public class NPCData : ScriptableObject
{
    public string npcName;
    public string role;
    public string initialAttitude;
    public string currentEmotion;
    public List<string> memory = new List<string>();
    public List<string> personalityTraits = new List<string>();
}