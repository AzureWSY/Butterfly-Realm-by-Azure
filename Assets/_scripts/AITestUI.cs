using UnityEngine;
using TMPro; // 引入你最熟悉的 TMP

public class AITestUI : MonoBehaviour
{
    public AIManager aiManager;
    public TextMeshProUGUI dialogText; // 拖入你之前烘焙好中文字体的 TMP 文本框

    public void OnClickTalkToAI()
    {
        dialogText.text = "精灵正在思考...";

        aiManager.SendMessageToAI(
            "我刚刚踩到了一个破碎的方块，差点掉进深渊！",
            (reply) => { dialogText.text = reply; },
            (errorMsg) => { dialogText.text = errorMsg; }
        );
    }
}