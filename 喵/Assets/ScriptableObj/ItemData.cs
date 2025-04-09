using UnityEngine;
using System.Collections.Generic;
[CreateAssetMenu(fileName = "ItemData", menuName = "Story/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public string description;
    public string location; // 初始位置
}