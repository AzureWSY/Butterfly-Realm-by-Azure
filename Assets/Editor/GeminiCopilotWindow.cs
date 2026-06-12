using UnityEngine;
using UnityEditor;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System;
using System.IO; // [新增] 用于读取本地文件

public class GeminiCopilotWindow : EditorWindow
{
    private string apiKey = "AIzaSy_在这里填入你的Key";
    private string userPrompt = "帮我修改 PlayerHealth，让它在扣血时调用无敌时间的协程...";
    private string aiResponse = "等待你的指令...";
    private bool isGenerating = false;
    private Vector2 scrollPosition;

    // [新增] 是否读取全工程代码的开关
    private bool readAllScripts = true;

    [MenuItem("Window/?? Butterfly AI 编程助手 (Pro版)")]
    public static void ShowWindow()
    {
        GetWindow<GeminiCopilotWindow>("AI 编程助手");
    }

    private void OnGUI()
    {
        GUILayout.Label("?? Gemini 1.5 Pro 架构级代码助手", EditorStyles.boldLabel);

        apiKey = EditorGUILayout.TextField("API Key:", apiKey);

        GUILayout.Space(10);

        // [新增] UI 开关
        EditorGUILayout.HelpBox("开启全局扫描后，AI 将理解你所有的互相引用和架构！\n(注：全工程读取会增加几秒钟的网络请求时间)", MessageType.Info);
        readAllScripts = EditorGUILayout.Toggle("?? 读取全工程脚本作为上下文", readAllScripts);

        GUILayout.Space(10);
        GUILayout.Label("请输入你的需求：");
        userPrompt = EditorGUILayout.TextArea(userPrompt, GUILayout.Height(80));

        GUILayout.Space(10);

        GUI.enabled = !isGenerating && !string.IsNullOrEmpty(apiKey);
        if (GUILayout.Button(isGenerating ? "Gemini 正在阅读全工程代码并思考中..." : "?? 生成架构级代码", GUILayout.Height(40)))
        {
            GenerateCodeAsync();
        }
        GUI.enabled = true;

        GUILayout.Space(10);
        GUILayout.Label("AI 返回的代码：");

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        EditorGUILayout.TextArea(aiResponse, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    private async void GenerateCodeAsync()
    {
        isGenerating = true;
        aiResponse = "正在打包你的游戏代码并发往 Google 服务器...\n(因为代码量大，可能需要等待 10-20 秒，请耐心...)";
        Repaint();

        string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-pro:generateContent?key={apiKey}";

        // 构建最终的提示词
        StringBuilder finalPromptBuilder = new StringBuilder();
        finalPromptBuilder.AppendLine("你是一个精通 Unity C# 的资深游戏架构师。");

        // [新增核心逻辑：读取全工程代码]
        if (readAllScripts)
        {
            finalPromptBuilder.AppendLine("以下是我游戏目前的完整代码架构，请仔细阅读，确保你新写的代码能完美兼容它们，不要重写已有的功能：");
            finalPromptBuilder.AppendLine("===========================");

            // 获取 Assets 目录下所有的 .cs 文件
            string[] allScriptPaths = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);

            foreach (string path in allScriptPaths)
            {
                // 排除掉 Editor 文件夹里的代码，防止 AI 把这写编辑器插件代码也算进去
                if (path.Contains("\\Editor\\") || path.Contains("/Editor/")) continue;

                string fileName = Path.GetFileName(path);
                string fileContent = File.ReadAllText(path);

                finalPromptBuilder.AppendLine($"--- 文件名: {fileName} ---");
                finalPromptBuilder.AppendLine(fileContent);
                finalPromptBuilder.AppendLine();
            }
            finalPromptBuilder.AppendLine("===========================");
        }

        finalPromptBuilder.AppendLine("我的最新需求是：\n" + userPrompt);
        finalPromptBuilder.AppendLine("请只返回最优化的 C# 代码，并带上详细的中文注释。如果需要修改现有脚本，请指出修改哪个文件。");

        // 防止 JSON 中的双引号和换行符报错，进行简单的转义
        string safePrompt = finalPromptBuilder.ToString().Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");

        string jsonBody = $@"{{
            ""contents"": [{{
                ""parts"": [{{""text"": ""{safePrompt}""}}]
            }}]
        }}";

        try
        {
            using (HttpClient client = new HttpClient())
            {
                // 设置较长的超时时间，因为包含所有代码的请求会比较大
                client.Timeout = TimeSpan.FromSeconds(120);

                StringContent content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(url, content);
                string responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    int startIndex = responseString.IndexOf("\"text\": \"") + 9;
                    int endIndex = responseString.IndexOf("\"", startIndex);

                    if (startIndex > 8 && endIndex > startIndex)
                    {
                        string extracted = responseString.Substring(startIndex, endIndex - startIndex);
                        aiResponse = extracted.Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\\\", "\\");
                    }
                    else
                    {
                        aiResponse = "生成成功，但解析返回文本失败。原始内容：\n" + responseString;
                    }
                }
                else
                {
                    aiResponse = "请求失败 (可能代码太多导致超出请求体大小，或网络断开)：\n" + responseString;
                }
            }
        }
        catch (Exception e)
        {
            aiResponse = "网络报错 (请检查代理或超时): \n" + e.Message;
        }
        finally
        {
            isGenerating = false;
            Repaint();
        }
    }
}