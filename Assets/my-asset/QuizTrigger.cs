using UnityEngine;

public class QuizTrigger : MonoBehaviour
{
    public GameObject interactUI;   // 例如：“按 E 查看规则”的提示
    public GameObject quizCanvas;   // Quiz 规则 UI

    private bool playerInRange = false;

    private void Start()
    {
        if (interactUI != null)
            interactUI.SetActive(false);
        if (quizCanvas != null)
            quizCanvas.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            interactUI?.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            interactUI?.SetActive(false);
        }
    }

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            ShowQuizUI();
        }
    }

    public void ShowQuizUI()
    {
        quizCanvas?.SetActive(true);
        interactUI?.SetActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f; // 暂停游戏
    }
}
