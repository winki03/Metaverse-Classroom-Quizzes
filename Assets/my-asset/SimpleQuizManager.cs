using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro; // ✅ 引入 TextMeshPro 命名空间

public class SimpleQuizManager : MonoBehaviour
{
    [System.Serializable]
    public class QuizQuestion
    {
        public string question;
        public string[] options = new string[4];
        public int correctAnswerIndex; // 0 - 3
    }

    [Header("Quiz UI")]
    public TextMeshProUGUI questionText;
    public Button[] optionButtons; // 0 = A, 1 = B, ...
    public QuizQuestion[] questions;

    [Header("Quiz Control")]
    public GameObject quizCanvas; // 用于显示测验 UI
    public GameObject quizEndPanel; // 用于显示测验结束的面板
    public Button exitButton; // 用于退出测验的按钮
    public Button goToDashboard;

    [Header("Score Display")]
    public TextMeshProUGUI score;

    [Header("Firebase Integration")]
    public FirebaseUploaded firebaseManager; // 拖拽 Firebase 管理器到这里

    private int currentQuestionIndex = 0;
    private bool isAnswering = false;
    private int gamePoint = 0;

    public GameObject goToClassroomCanvas;
    public Button BackToClassroom; // 用于返回教室的按钮（如果有的话）

    void Start()
    {
        // 检查 Firebase 管理器引用
        if (firebaseManager == null)
        {
            firebaseManager = FindObjectOfType<FirebaseUploaded>();
            if (firebaseManager == null)
            {
                Debug.LogError("❌ 找不到 FirebaseUploaded 组件！请确保场景中有 Firebase 管理器");
            }
        }

        firebaseManager.CheckIfQuizCompleted((bool completed) =>
        {
            if (completed)
            {
                Debug.Log("🚫 玩家已完成测验，不再显示 Quiz UI");

                quizCanvas.SetActive(false);
                quizEndPanel.SetActive(true);
                questionText.text = "You have already completed the quiz.";

                foreach (var btn in optionButtons)
                    btn.gameObject.SetActive(false);

                if (BackToClassroom != null)
                    BackToClassroom.gameObject.SetActive(true);  // ✅ 显示返回按钮
            }
            else
            {
                Debug.Log("✅ 玩家可以进行测验");
                ShowQuestion();
            }
        });


        if (score != null)
        {
            score.text = gamePoint.ToString();
        }

        ShowQuestion();
        goToClassroomCanvas.SetActive(false); // 确保教室按钮初始时隐藏
        quizEndPanel.SetActive(false); // 确保一开始是隐藏的
        BackToClassroom.onClick.AddListener(backToClassroom);
        exitButton.onClick.AddListener(OnExitClicked);
        goToDashboard.onClick.AddListener(OnDashboardClicked);
    }

    void ShowQuestion()
    {
        isAnswering = false;

        if (currentQuestionIndex >= questions.Length)
        {
            OnQuizCompleted(); // 测验完成时的处理
            return;
        }

        QuizQuestion q = questions[currentQuestionIndex];
        questionText.text = q.question;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i;
            optionButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = q.options[i];
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => OnAnswerSelected(index));
            SetButtonColor(optionButtons[i], Color.white); // Reset color
        }
    }

    void OnAnswerSelected(int selectedIndex)
    {
        if (isAnswering) return;
        isAnswering = true;

        QuizQuestion q = questions[currentQuestionIndex];

        if (selectedIndex == q.correctAnswerIndex)
        {
            SetButtonColor(optionButtons[selectedIndex], Color.green);
            gamePoint += 1;
            score.text = gamePoint.ToString();

            // 实时更新 Firebase 管理器中的分数
            if (firebaseManager != null)
            {
                firebaseManager.SetPlayerScore(gamePoint);
            }

            Debug.Log("✅ 答对了！当前分数: " + gamePoint);
        }
        else
        {
            SetButtonColor(optionButtons[selectedIndex], Color.red);
            SetButtonColor(optionButtons[q.correctAnswerIndex], Color.green);
            Debug.Log("❌ 答错了！当前分数: " + gamePoint);
        }

        StartCoroutine(GoToNextQuestion());
    }

    IEnumerator GoToNextQuestion()
    {
        yield return new WaitForSeconds(1f);
        currentQuestionIndex++;
        ShowQuestion();
    }

    void OnQuizCompleted()
    {
        questionText.text = "Quiz Completed!";
        foreach (var btn in optionButtons) btn.gameObject.SetActive(false);

        // 测验完成时自动上传最终分数
        if (firebaseManager != null)
        {
            Debug.Log("🏁 测验完成！最终分数: " + gamePoint);
            firebaseManager.SetPlayerScore(gamePoint);

            // 可选：自动上传分数到 Firebase
            // firebaseManager.UploadPlayerScore(gamePoint);
        }

        firebaseManager.UploadPlayerCompleted();
        quizEndPanel.SetActive(true); // 显示测验结束面板
    }

    void SetButtonColor(Button btn, Color color)
    {
        var image = btn.GetComponent<Image>();
        if (image != null)
        {
            image.color = color;
        }
    }

    void OnExitClicked()
    {
        Debug.Log("Quitting game...");

        // 退出前确保分数已更新
        if (firebaseManager != null)
        {
            firebaseManager.SetPlayerScore(gamePoint);
        }

        quizEndPanel.SetActive(false); // 隐藏测验结束面板
        quizCanvas.SetActive(false); // 隐藏测验 UI
    }

    void OnDashboardClicked()
    {
        Debug.Log("Loading Dashboard...");

        // 跳转前确保分数已更新
        if (firebaseManager != null)
        {
            firebaseManager.SetPlayerScore(gamePoint);
        }

        //UnityEngine.SceneManagement.SceneManager.LoadScene("DashboardScene"); // 替换为你 Dashboard 的场景名
    }

    // 公共方法：获取当前分数
    public int GetCurrentScore()
    {
        return gamePoint;
    }

    // 公共方法：手动上传分数到 Firebase
    public void UploadScoreToFirebase()
    {
        if (firebaseManager != null)
        {
            firebaseManager.SetPlayerScore(gamePoint);
            firebaseManager.UploadPlayerScore(gamePoint);
            Debug.Log("📤 手动上传分数: " + gamePoint);
        }
        else
        {
            Debug.LogError("❌ Firebase 管理器未设置！");
        }
    }

    public void backToClassroom()
    {
        quizCanvas.SetActive(false); // 隐藏测验 UI
        quizEndPanel.SetActive(false); // 隐藏测验结束面板
    }
}