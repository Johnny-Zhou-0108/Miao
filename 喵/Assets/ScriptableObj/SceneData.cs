using UnityEngine;
using System.Collections.Generic;
[CreateAssetMenu(fileName = "SceneData", menuName = "Story/Scene Data")]
public class SceneData : ScriptableObject
{
    public string sceneName;
    public string description;
    public List<string> connectedScenes; 
    public List<string> npcsInScene;     
    public List<string> itemsInScene;    
    public List<string> availableActions;
}
