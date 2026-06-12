using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class ButterflyInteraction : MonoBehaviour
{
    [Header("视觉组件引用")]
    public ButterflyGlowController glowController; // 👈 链接蝴蝶发光脚本

    [Header("UI 引用")]
    public GameObject interactPrompt;
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;

    [Header("对话内容")]
    [TextArea(3, 10)]
    public string[] dialogueLines;

    [Header("打字机设置")]
    [Tooltip("每个字蹦出来的时间间隔")]
    public float typingSpeed = 0.05f;

    [Header("核心事件分发")]
    [Tooltip("对话开始时触发（用来锁住玩家）")]
    public UnityEvent OnDialogueStarted;
    [Tooltip("对话完毕时触发（每次都会触发，用来解锁玩家）")]
    public UnityEvent OnDialogueFinished;

    [Tooltip("【首次】读完对话触发（只触发一次！用来给能量、存档）")]
    public UnityEvent OnFirstDialogueFinished;

    [Header("粒子表现设置")]
    [Tooltip("能量粒子传递的预制体（需挂载 TravellingParticle 脚本）")]
    public GameObject energyParticlePrefab;

    private Queue<string> sentences;
    private bool isPlayerInRange = false;
    private bool isTalking = false;

    private bool hasObtainedEnergy = false;
    private bool isTyping = false;
    private string currentSentence;
    private Coroutine typingCoroutine;

    void Start()
    {
        sentences = new Queue<string>();
        interactPrompt.SetActive(false);
        dialoguePanel.SetActive(false);
        
    }

    void Update()
    {
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (!isTalking)
            {
                StartDialogue();
            }
            else if (isTyping)
            {
                CompleteSentence();
            }
            else
            {
                DisplayNextSentence();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = true;
            interactPrompt.SetActive(true);
            Debug.Log("显示UI");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = false;
            interactPrompt.SetActive(false);
            EndDialogue(); // 👈 玩家离开触发，后续会走蓄力发光逻辑
        }
    }

    public void StartDialogue()
    {
        isTalking = true;
        interactPrompt.SetActive(false);
        dialoguePanel.SetActive(true);
        sentences.Clear();

        if (OnDialogueStarted != null) OnDialogueStarted.Invoke();

        foreach (string sentence in dialogueLines)
        {
            sentences.Enqueue(sentence);
        }
        DisplayNextSentence();
    }

    public void DisplayNextSentence()
    {
        if (sentences.Count == 0)
        {
            EndDialogue();
            return;
        }

        currentSentence = sentences.Dequeue();

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(TypeSentence(currentSentence));
    }

    private IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    private void CompleteSentence()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        dialogueText.text = currentSentence;
        isTyping = false;
    }

    public void EndDialogue()
    {
        isTalking = false;
        dialoguePanel.SetActive(false);
        isTyping = false;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        // --- 触发结束事件 ---
        // 🌟 核心逻辑整合：在这里调用发光脚本
        if (!hasObtainedEnergy)
        {
            hasObtainedEnergy = true;

            if (glowController != null)
            {
                // 先让蝴蝶蓄力发光，在最高点通过回调调用 SpawnEnergyParticleVisual
                glowController.PlayChargeAndFade(SpawnEnergyParticleVisual);
            }
            else
            {
                // 如果没挂脚本，走原本的瞬间发射
                SpawnEnergyParticleVisual();
            }
        }

        if (OnDialogueFinished != null)
        {
            OnDialogueFinished.Invoke();
        }
    }

    private void SpawnEnergyParticleVisual()
    {
        if (energyParticlePrefab == null)
        {
            Debug.LogWarning("未设置能量粒子预制体，将跳过粒子动画直接加能量。");
            if (OnFirstDialogueFinished != null) OnFirstDialogueFinished.Invoke();
            return;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogError("找不到 Tag 为 Player 的物体，粒子无法传递！");
            return;
        }

        GameObject particleInstance = Instantiate(energyParticlePrefab, transform.position, Quaternion.identity);

        EnergyPaticleFollow traveller = particleInstance.GetComponent<EnergyPaticleFollow>();
        if (traveller != null)
        {
            traveller.Setup(playerObj.transform);

            traveller.OnArrival = () =>
            {
                
                if (OnFirstDialogueFinished != null) OnFirstDialogueFinished.Invoke();
                // 2. 触发视觉：玩家发光
                PlayerGlowController glow = playerObj.GetComponent<PlayerGlowController>();
                if (glow != null) glow.StartGlowPulse();
                
            };
        }
    }
}