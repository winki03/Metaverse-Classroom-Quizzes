using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class QuizManager : MonoBehaviour
{
    public GameObject quizCanvas;  // 指向规则 UI
    public GameObject actualQuizUI; // 测验 UI（如果你有的话）

    public TMP_InputField playerNameInput; // 玩家姓名输入框

    public Button ExitButton;

    void Start()
    {
        if (quizCanvas != null)
            quizCanvas.SetActive(false); // 初始时隐藏规则 UI
        if (actualQuizUI != null)
            actualQuizUI.SetActive(false); // 初始时隐藏测验 UI
    }

    private void Update()
    {
        ExitButton.onClick.AddListener(() =>
        {
            Debug.Log("退出测验！");
            quizCanvas?.SetActive(false);
            actualQuizUI?.SetActive(false);
            Time.timeScale = 1f; // 恢复游戏
        });
    }

    public void StartQuiz()
    {
        string playerName = playerNameInput.text.Trim();

        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogWarning("⚠️ 玩家姓名未设置！请在测验开始前输入姓名。");
            playerNameInput.text = "Player"; // 设置默认值
        }
        else
        {

            Debug.Log("开始测验！");

            // 隐藏规则页面
            quizCanvas?.SetActive(false);

            // 显示测验页面（可选）
            actualQuizUI?.SetActive(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 1f; // 恢复游戏
        }
    }
}
