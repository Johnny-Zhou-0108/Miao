using UnityEngine;

[CreateAssetMenu(fileName = "ChapterData", menuName = "Story/Chapter Data")]
public class ChapterData : ScriptableObject
{
    public string chapterTitle;
    [TextArea(10, 100)]
    public string fullText;

    [Header("从外部文本文件读取")]
    public TextAsset externalTextFile;

#if UNITY_EDITOR
    [ContextMenu("从外部 txt 导入")]
    public void ImportTextFromExternalFile()
    {
        if (externalTextFile != null)
        {
            fullText = externalTextFile.text;
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log($"成功{externalTextFile.name}导入");
        }
        else
        {
            Debug.LogWarning("没有绑定 externalTextFile 文件");
        }
    }
#endif
}