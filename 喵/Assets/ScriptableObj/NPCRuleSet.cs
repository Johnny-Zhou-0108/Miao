using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NPCRuleSet", menuName = "Story/NPC RuleSet")]
public class NPCRuleSet : ScriptableObject
{
    [SerializeField] 
    private string _npcId;
    
    public List<PersonalityRule> CorePersonalityRules = new List<PersonalityRule>();
    public List<string> FallbackResponses = new List<string>();

    public string NPCId {
        get => _npcId;
        set => _npcId = value;
    }
}

[System.Serializable]
public class PersonalityRule
{
    public string Description;
    public float Weight;
}