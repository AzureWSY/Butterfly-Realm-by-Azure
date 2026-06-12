using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class AIManager : MonoBehaviour
{
    [Header("Gemini AI 配置")]
    [Tooltip("填入你的 Gemini API Key")]
    public string apiKey = "AIzaSy_在这里填入你的真实APIKey";

    [Tooltip("Gemini 使用的模型名称 (推荐 gemini-1.5-flash)")]
    public string modelName = "gemini-1.5-flash";

    [Header("系统提示词 (AI的设定)")]
    [TextArea(3, 5)]
    public string systemPrompt = "你是《Butterfly Realm》游戏中的指引精灵。请用神秘、空灵的语气回答玩家，回答尽量简短，不超过50个字。";

    // --- 下面是配合 JsonUtility 必须写的数据结构 (Gemini 专属格式) ---
    [Serializable]
    private class Part { public string text; }

    [Serializable]
    private class Content { public string role; public List<Part> parts; }

    [Serializable]
    private class SystemInstruction { public List<Part> parts; }

    [Serializable]
    private class GeminiRequest
    {
        public SystemInstruction system_instruction;
        public List<Content> contents;
    }

    [Serializable]
    private class GeminiResponse
    {
        public List<Candidate> candidates;
    }

    [Serializable]
    private class Candidate
    {
        public Content content;
    }

    /// <summary>
    /// 对外开放的接口：向 Gemini 发送消息
    /// </summary>
    public void SendMessageToAI(string userMessage, Action<string> onSuccess, Action<string> onFail = null)
    {
        StartCoroutine(RequestGeminiCoroutine(userMessage, onSuccess, onFail));
    }

    private IEnumerator RequestGeminiCoroutine(string userText, Action<string> onSuccess, Action<string> onFail)
    {
        // 1. 组装请求数据 (Gemini 的结构比较特殊)
        GeminiRequest reqData = new GeminiRequest
        {
            // 设置系统设定 (也就是你是谁)
            system_instruction = new SystemInstruction
            {
                parts = new List<Part> { new Part { text = systemPrompt } }
            },

            // 设置玩家说的话
            contents = new List<Content>
            {
                new Content
                {
                    role = "user",
                    parts = new List<Part> { new Part { text = userText } }
                }
            }
        };

        // 把数据转成 JSON
        string jsonBody = JsonUtility.ToJson(reqData);

        // 2. 拼接 Gemini 的特殊 URL (API Key 是写在 URL 里的)
        string geminiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";

        // 3. 配置网络请求
        using (UnityWebRequest request = new UnityWebRequest(geminiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            // 设置请求头
            request.SetRequestHeader("Content-Type", "application/json");

            // 4. 发送请求并等待返回
            yield return request.SendWebRequest();

            // 5. 处理服务器的返回结果
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Gemini 接口报错: " + request.error + "\n详情: " + request.downloadHandler.text);
                onFail?.Invoke("通讯受到未知能量干扰...");
            }
            else
            {
                // 解析返回的 JSON
                string responseJson = request.downloadHandler.text;
                try
                {
                    GeminiResponse resData = JsonUtility.FromJson<GeminiResponse>(responseJson);

                    // 提取 AI 的回复内容
                    if (resData.candidates != null && resData.candidates.Count > 0 && resData.candidates[0].content.parts.Count > 0)
                    {
                        string aiReply = resData.candidates[0].content.parts[0].text;
                        onSuccess?.Invoke(aiReply);
                    }
                    else
                    {
                        onFail?.Invoke("精灵的幻影消散了...");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("解析 Gemini 返回数据失败: " + e.Message);
                    onFail?.Invoke("精灵的低语变得无法理解...");
                }
            }
        }
    }
}