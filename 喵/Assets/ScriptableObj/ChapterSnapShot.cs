using UnityEngine;

[CreateAssetMenu(fileName = "ChapterProgressData", menuName = "Story/Chapter Progress Data")]
public class ChapterProgressData : ScriptableObject
{
    public string chapterTitle;
    [TextArea(10, 100)]
    public string storyContent;
}