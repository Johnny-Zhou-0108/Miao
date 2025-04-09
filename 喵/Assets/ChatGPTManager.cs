using System.Collections.Generic;
using UnityEngine;
using OpenAI;
using UnityEngine.Events;
using TMPro;
using System.Threading.Tasks;
using System.Linq;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ChatGPTManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField inputField;
    public TMP_Text outputText;

    [Header("GPT Settings")]
    public OnResponseEvent OnResponse;

    [System.Serializable]
    public class OnResponseEvent : UnityEvent<string> { }

    private OpenAIApi openAI = new OpenAIApi();
    private List<ChatMessage> messages = new List<ChatMessage>();

    [Header("Prompt Config")]
    [TextArea(10, 30)]
    public string gptPersonalityDescription;

    [Header("Story Data")]
    public ChapterData chapterData;
    public ChapterProgressData playerProgressData;
    public GameStateData gameStateData;

    [Header("Auto Generated Data")]
    public List<NPCData> allNpcData = new List<NPCData>();
    public List<SceneData> allSceneData = new List<SceneData>();
    public List<ItemData> allItemData = new List<ItemData>();

    [Header("Story Progression")]
    public int totalRounds = 10;
    private List<string> playerInputs = new List<string>();
    private List<string> aiResponses = new List<string>();
    private List<string> playerBehaviorSummaries = new List<string>();

    private bool isStoryInitialized = false;

    void Start()
    {
#if UNITY_EDITOR
        InitializeEditorData();
#endif

        inputField.onSubmit.AddListener(OnInputSubmitted);
        inputField.ActivateInputField();

        InitializeStoryStructure();
    }

#if UNITY_EDITOR
    private void InitializeEditorData()
    {
        if (chapterData != null && chapterData.externalTextFile != null)
        {
            string path = AssetDatabase.GetAssetPath(chapterData.externalTextFile);
            chapterData.fullText = File.ReadAllText(path);
            EditorUtility.SetDirty(chapterData);
            AssetDatabase.SaveAssets();
        }

        if (playerProgressData != null)
        {
            playerProgressData.storyContent = "";
            EditorUtility.SetDirty(playerProgressData);
            AssetDatabase.SaveAssets();
        }
    }
#endif

    private async void InitializeStoryStructure()
    {
        if (isStoryInitialized || chapterData == null || string.IsNullOrEmpty(chapterData.fullText))
            return;

        await GenerateSceneData();
        await GenerateNPCData();
        await GenerateItemData();
        InitializeGameStateData();

        Debug.Log("故事结构初始化完成");
        isStoryInitialized = true;
    }

    private async Task GenerateSceneData()
    {
        string prompt = "你是《小王子》故事的场景策划。请列出所有关键场景，每一项单独一行，格式为：场景名：描述：可用动作（用英文逗号分隔）。请严格按照格式输出，确保每一项之间用中文冒号 '：' 分隔。";

        var scenes = await RequestGptList(prompt);
        Debug.Log($"GPT 返回的 SceneData：\n{scenes}");

#if UNITY_EDITOR
        allSceneData.Clear();
        foreach (var line in scenes.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split('：');
            if (parts.Length < 3) continue;

            var scene = ScriptableObject.CreateInstance<SceneData>();
            scene.sceneName = parts[0].Trim();
            scene.description = parts[1].Trim();
            scene.availableActions = parts[2].Split(',').Select(x => x.Trim()).ToList();
            scene.connectedScenes = new List<string>();

            SaveAsset(scene, scene.sceneName);
            allSceneData.Add(scene);
        }
#endif
    }

    private async Task GenerateNPCData()
    {
        string prompt = "你是《小王子》故事的角色策划。请列出所有重要角色，每一项单独一行，格式为：名字：角色：初始态度：初始情绪。请严格按照格式输出，确保每一项之间用中文冒号 '：' 分隔。";

        var npcs = await RequestGptList(prompt);
        Debug.Log($"GPT 返回的 NPCData：\n{npcs}");

#if UNITY_EDITOR
        allNpcData.Clear();
        foreach (var line in npcs.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split('：');
            if (parts.Length < 4) continue;

            var npc = ScriptableObject.CreateInstance<NPCData>();
            npc.npcName = parts[0].Trim();
            npc.role = parts[1].Trim();
            npc.initialAttitude = parts[2].Trim();
            npc.currentEmotion = parts[3].Trim();
            npc.memory = new List<string>();

            SaveAsset(npc, npc.npcName);
            allNpcData.Add(npc);
        }
#endif
    }

    private async Task GenerateItemData()
    {
        string prompt = "你是《小王子》故事的道具策划。请列出所有重要道具，每一项单独一行，格式为：物品名：描述：初始位置。请严格按照格式输出，确保每一项之间用中文冒号 '：' 分隔。";

        var items = await RequestGptList(prompt);
        Debug.Log($"GPT 返回的 ItemData：\n{items}");

#if UNITY_EDITOR
        allItemData.Clear();
        foreach (var line in items.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split('：');
            if (parts.Length < 3) continue;

            var item = ScriptableObject.CreateInstance<ItemData>();
            item.itemName = parts[0].Trim();
            item.description = parts[1].Trim();
            item.location = parts[2].Trim();

            SaveAsset(item, item.itemName);
            allItemData.Add(item);
        }
#endif
    }

    private void InitializeGameStateData()
    {
#if UNITY_EDITOR
        if (gameStateData == null)
        {
            gameStateData = ScriptableObject.CreateInstance<GameStateData>();
            SaveAsset(gameStateData, "GameStateData");
        }

        gameStateData.currentLocation = allSceneData.FirstOrDefault()?.sceneName ?? "未知地点";
        gameStateData.visitedLocations = new List<string> { gameStateData.currentLocation };
        gameStateData.npcInteracted = new List<string>();
        gameStateData.itemsCollected = new List<string>();

        EditorUtility.SetDirty(gameStateData);
        AssetDatabase.SaveAssets();
#endif
    }

    private async Task<string> RequestGptList(string prompt)
    {
        var request = new CreateChatCompletionRequest
        {
            Model = "gpt-3.5-turbo",
            Messages = new List<ChatMessage>
            {
                new ChatMessage { Role = "system", Content = "你是游戏策划专家，擅长把文学作品拆解为游戏设计要素。" },
                new ChatMessage { Role = "user", Content = prompt }
            }
        };

        var response = await openAI.CreateChatCompletion(request);
        return response.Choices?[0].Message.Content.Trim() ?? "";
    }

#if UNITY_EDITOR
    private void SaveAsset(ScriptableObject asset, string assetName = null)
    {
        EnsureSaveFolderExists();
        string folderPath = "Assets/StorySnapshots";
        string path = $"{folderPath}/{(assetName ?? asset.name)}.asset";

        if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(path) != null)
        {
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return;
        }

        AssetDatabase.CreateAsset(asset, path);
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
    }

    private void EnsureSaveFolderExists()
    {
        string folderPath = "Assets/StorySnapshots";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "StorySnapshots");
        }
    }
#endif

    private async void OnInputSubmitted(string userInput)
    {
        userInput = userInput.Trim();
        if (string.IsNullOrEmpty(userInput)) return;

        playerInputs.Add(userInput);
        if (playerProgressData != null)
        {
            playerProgressData.storyContent += "\n小王子：" + userInput;
#if UNITY_EDITOR
            EditorUtility.SetDirty(playerProgressData);
            AssetDatabase.SaveAssets();
#endif
        }

        await ProcessPlayerInput(userInput);

        inputField.text = "";
        inputField.ActivateInputField();
    }

    private async Task ProcessPlayerInput(string userInput)
    {
        string sceneIntent = await AnalyzePlayerIntentForScene(userInput);
        if (sceneIntent != "无切换" && IsSceneConnected(sceneIntent))
        {
            MovePlayerToScene(sceneIntent);
            outputText.text = $"你来到了：{sceneIntent}\n{GetSceneDescription(sceneIntent)}";
            return;
        }
        
        string npcIntent = await AnalyzePlayerIntentForNPC(userInput);
        if (npcIntent != "无交互")
        {
            await EngageNPCDialogue(npcIntent, userInput);
        }
        else
        {
            string behaviorSummary = await SummarizePlayerBehaviorGPT();
            playerBehaviorSummaries.Add(behaviorSummary);

            string dynamicPrompt = BuildDynamicPrompt(userInput, behaviorSummary);
            var aiReply = await GetGPTResponse(dynamicPrompt, userInput);
            HandleAIResponse(aiReply);
        }
    }

    private async Task<string> GetGPTResponse(string prompt, string userInput)
    {
        if (messages.Count == 0)
        {
            messages.Add(new ChatMessage
            {
                Role = "system",
                Content = @"
你是一个互动故事 AI，引导玩家沉浸式体验《小王子》故事。

- 玩家是“小王子”，你只需要作为旁白和 NPC 回复他的行动与对话。
- 玩家输入代表“小王子”的语言或动作，不要生成“小王子”的台词或内心独白。
- 你要用旁白或 NPC 的方式回应玩家的行动，推动故事发展。
- 务必避免重复解释角色设定，直接进入故事叙述。
- 每一次回复都要推进故事，例如：新角色登场、环境变化、事件推进。

风格要求：
- 回复语言富有诗意，符合《小王子》的童话风格。
- 避免冗长解释，保持故事自然流畅。
- 回复长度控制在 150 字以内。
"
            });
        }
        
        messages.Add(new ChatMessage { Role = "user", Content = userInput });

        var request = new CreateChatCompletionRequest
        {
            Model = "gpt-3.5-turbo",
            Messages = messages
        };

        var response = await openAI.CreateChatCompletion(request);
        var reply = response.Choices?[0].Message.Content.Trim() ?? "未收到回复";
        
        messages.Add(new ChatMessage { Role = "assistant", Content = reply });
        
        if (messages.Count > 40)
        {
            messages.RemoveAt(1); 
            messages.RemoveAt(1); 
        }

        return reply;
    }

    private void HandleAIResponse(string aiReply)
    {
        if (string.IsNullOrWhiteSpace(aiReply) || aiReply == "未收到回复")
        {
            outputText.text += "\n\n[AI 没有正常回复，请重试输入]";
            return;
        }

        bool isStoryEnding = aiReply.Contains("[END]");
        aiReply = aiReply.Replace("[END]", "").Trim();

        aiResponses.Add(aiReply);
        outputText.text = aiReply;

        if (isStoryEnding)
        {
            outputText.text += "\n\n[这就是关于新-小王子故事的全部啦~]";
            inputField.interactable = false;
        }

        UpdateStoryProgress(aiReply);
    }


    private void UpdateStoryProgress(string aiReply)
    {
        if (playerProgressData != null)
        {
            playerProgressData.storyContent += "\nAI：" + aiReply;
#if UNITY_EDITOR
            EditorUtility.SetDirty(playerProgressData);
            AssetDatabase.SaveAssets();
#endif
        }

        var currentNpc = allNpcData.FirstOrDefault();
        if (currentNpc != null)
        {
            currentNpc.memory.Add($"AI：「{aiReply}」");
#if UNITY_EDITOR
            EditorUtility.SetDirty(currentNpc);
            AssetDatabase.SaveAssets();
#endif
        }
    }

    private async Task<string> AnalyzePlayerIntentForScene(string userInput)
    {
        var request = new CreateChatCompletionRequest
        {
            Model = "gpt-3.5-turbo",
            Messages = new List<ChatMessage>
            {
                new ChatMessage { 
                    Role = "system", 
                    Content = $"分析玩家意图，仅返回场景名或'无切换'。可用场景：{string.Join(",", allSceneData.Select(s => s.sceneName))}" 
                },
                new ChatMessage { Role = "user", Content = userInput }
            }
        };

        var response = await openAI.CreateChatCompletion(request);
        return response.Choices?[0].Message.Content.Trim() ?? "无切换";
    }

    private bool IsSceneConnected(string targetScene)
    {
        var currentScene = allSceneData.FirstOrDefault(s => s.sceneName == gameStateData.currentLocation);
        return currentScene?.connectedScenes.Contains(targetScene) ?? false;
    }

    private void MovePlayerToScene(string sceneName)
    {
        gameStateData.currentLocation = sceneName;
        if (!gameStateData.visitedLocations.Contains(sceneName))
        {
            gameStateData.visitedLocations.Add(sceneName);
#if UNITY_EDITOR
            EditorUtility.SetDirty(gameStateData);
            AssetDatabase.SaveAssets();
#endif
        }
    }

    private string GetSceneDescription(string sceneName)
    {
        var scene = allSceneData.FirstOrDefault(s => s.sceneName == sceneName);
        return scene?.description ?? "这个场景没有更多描述";
    }

    private async Task<string> AnalyzePlayerIntentForNPC(string userInput)
    {
        var request = new CreateChatCompletionRequest
        {
            Model = "gpt-3.5-turbo",
            Messages = new List<ChatMessage>
            {
                new ChatMessage { 
                    Role = "system", 
                    Content = $"分析玩家意图，仅返回NPC名称或'无交互'。可用NPC：{string.Join(",", allNpcData.Select(n => n.npcName))}" 
                },
                new ChatMessage { Role = "user", Content = userInput }
            }
        };

        var response = await openAI.CreateChatCompletion(request);
        return response.Choices?[0].Message.Content.Trim() ?? "无交互";
    }

    private async Task EngageNPCDialogue(string npcName, string userInput)
    {
        var npc = allNpcData.FirstOrDefault(n => n.npcName == npcName);
        if (npc == null) return;

        string prompt = $"你是{npc.npcName}，{npc.role}。当前情绪：{npc.currentEmotion}，对小王子的态度：{npc.initialAttitude}。记忆：{string.Join(";", npc.memory)}";
        
        var aiReply = await GetGPTResponse(prompt, userInput);
        HandleAIResponse(aiReply);

        if (!gameStateData.npcInteracted.Contains(npcName))
        {
            gameStateData.npcInteracted.Add(npcName);
#if UNITY_EDITOR
            EditorUtility.SetDirty(gameStateData);
            AssetDatabase.SaveAssets();
#endif
        }
    }

    private async Task<string> SummarizePlayerBehaviorGPT()
    {
        var request = new CreateChatCompletionRequest
        {
            Model = "gpt-3.5-turbo",
            Messages = new List<ChatMessage>
            {
                new ChatMessage { Role = "system", Content = "用20字以内总结玩家行为倾向" },
                new ChatMessage { Role = "user", Content = string.Join("\n", playerInputs) }
            }
        };

        var response = await openAI.CreateChatCompletion(request);
        return response.Choices?[0].Message.Content.Trim() ?? "行为倾向未知";
    }

    private string BuildDynamicPrompt(string userInput, string behaviorSummary)
    {
        var currentScene = allSceneData.FirstOrDefault(s => s.sceneName == gameStateData.currentLocation);
        var currentNpc = allNpcData.FirstOrDefault();

        return $@"
故事背景：
- 当前场景：{currentScene?.sceneName}，描述：{currentScene?.description}
- 可用动作：{string.Join(", ", currentScene?.availableActions ?? new List<string>())}

NPC 信息：
- {currentNpc?.npcName}：{currentNpc?.role}，当前情绪：{currentNpc?.currentEmotion}
- NPC 记忆片段：{string.Join("；", currentNpc?.memory.TakeLast(3) ?? new List<string>())}

玩家输入：
- 小王子的行为或对话：{userInput}

玩家行为总结：
- {behaviorSummary}

你的回复要求：
- 你是旁白或当前场景的 NPC。
- 回复小王子的行为或对话，推进故事进展。
- 允许场景变化、新角色登场、事件发展。
- 不要代替小王子发言。
- 语言风格符合《小王子》，保持 150 字以内。
";
    }

}