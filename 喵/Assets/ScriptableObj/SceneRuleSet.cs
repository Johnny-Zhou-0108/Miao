using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SceneRuleSet", menuName = "Story/Scene RuleSet")]
public class SceneRuleSet : ScriptableObject
{
    public string sceneId;
    public List<string> forbiddenActions = new List<string>();
}