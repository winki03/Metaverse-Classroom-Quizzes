using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun; // Using Photon PUN

public class CharacterTrigger : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject interactUI;   // 例如："按 E 与角色对话"的提示

    [Header("Dialog System")]
    public TeacherVoiceTrigger dialogSystem;  // 拖拽您的TeacherVoiceTrigger组件
    public KeyCode interactKey = KeyCode.Return;   // 互动按键

    [Header("Settings")]
    public bool oneTimeDialog = false;        // 是否只能触发一次对话
    public float cooldownTime = 2f;           // 对话冷却时间

    private bool playerInRange = false;
    private bool hasTriggered = false;
    private float lastTriggerTime = -99f; // Initialize to a low value

    void Start()
    {
        interactUI?.SetActive(false);

        // 如果没有手动分配，尝试自动查找
        if (dialogSystem == null)
        {
            dialogSystem = FindObjectOfType<TeacherVoiceTrigger>();
        }

        if (dialogSystem == null)
        {
            Debug.LogError("CharacterTrigger: 没有找到TeacherVoiceTrigger组件！请在Inspector中分配。");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // This should only react to the local player's character.
        // Make sure your player's GameObject has a PhotonView component and the "Player" tag.
        PhotonView pv = other.GetComponent<PhotonView>();
        if (other.CompareTag("Player") && pv != null && pv.IsMine)
        {
            playerInRange = true;

            // 检查是否可以显示互动提示
            if (CanShowInteractUI())
            {
                interactUI?.SetActive(true);
                Debug.Log("CharacterTrigger: 玩家进入触发区域");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PhotonView pv = other.GetComponent<PhotonView>();
        if (other.CompareTag("Player") && pv != null && pv.IsMine)
        {
            playerInRange = false;
            interactUI?.SetActive(false);
            Debug.Log("CharacterTrigger: 玩家离开触发区域");
        }
    }

    private void Update()
    {
        // 互动逻辑只在本地玩家身上运行
        if (playerInRange && Input.GetKeyDown(interactKey) && interactUI.activeSelf)
        {
            TriggerDialog();
        }
    }

    private bool CanShowInteractUI()
    {
        if (oneTimeDialog && hasTriggered) return false;
        if (Time.time - lastTriggerTime < cooldownTime) return false;
        // Use the new public property from TeacherVoiceTrigger
        if (dialogSystem != null && dialogSystem.IsPlayingDialogue) return false;
        return true;
    }

    private void TriggerDialog()
    {
        if (dialogSystem == null)
        {
            Debug.LogError("CharacterTrigger: TeacherVoiceTrigger组件未分配！");
            return;
        }

        // Check conditions again before triggering
        if (!CanShowInteractUI())
        {
            Debug.Log("CharacterTrigger: 不满足对话触发条件。");
            return;
        }

        // The Master Client is responsible for starting the dialogue.
        // A non-master client cannot start it directly.
        // This logic assumes that any player can interact, but only the Master Client actually starts the sequence.
        // This is a direct translation of the original script's intent where only the owner could start it.
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("CharacterTrigger: (Master Client) 触发角色对话");
            dialogSystem.StartDialogue();
        }
        else
        {
            // In a more complex setup, you could send an RPC to the Master Client to request the dialogue start.
            // For now, we'll just log it. This prevents non-masters from causing issues.
            Debug.Log("CharacterTrigger: 只有Master Client可以启动对话。此交互不会执行任何操作。");
            // We can still hide the UI locally to give feedback to the player.
        }

        // Hide interact UI locally
        interactUI?.SetActive(false);

        // Update state locally
        hasTriggered = true;
        lastTriggerTime = Time.time;

        // Start a local coroutine to listen for when the dialogue is over, to potentially show the UI again.
        StartCoroutine(WaitForDialogEnd());
    }

    private IEnumerator WaitForDialogEnd()
    {
        if (dialogSystem == null) yield break;

        // Wait until the dialogue system starts playing (based on synchronized state)
        yield return new WaitUntil(() => dialogSystem.IsPlayingDialogue);

        // Wait until the dialogue system stops playing
        yield return new WaitUntil(() => !dialogSystem.IsPlayingDialogue);

        Debug.Log("CharacterTrigger: 对话结束");

        // If the player is still in range and it's not a one-time dialogue, check if we can show the UI again.
        if (playerInRange && !oneTimeDialog)
        {
            yield return new WaitForSeconds(0.5f); // Brief delay

            if (playerInRange && CanShowInteractUI())
            {
                interactUI?.SetActive(true);
            }
        }
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
        lastTriggerTime = -99f;

        if (playerInRange && CanShowInteractUI())
        {
            interactUI?.SetActive(true);
        }
        Debug.Log("CharacterTrigger: 触发状态已重置");
    }

    public void SetDialogSystem(TeacherVoiceTrigger system)
    {
        dialogSystem = system;
    }
}
