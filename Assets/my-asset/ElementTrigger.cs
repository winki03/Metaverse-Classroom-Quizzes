using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElementTrigger : MonoBehaviour
{
    private bool canStartQuiz = false;
    private bool canTalkToAvatar = false;
    private bool canOpenLeaderboard = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("QuizPaper"))
            canStartQuiz = true;
        else if (other.CompareTag("Avatar"))
            canTalkToAvatar = true;
        else if (other.CompareTag("Leaderboard"))
            canOpenLeaderboard = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("QuizPaper"))
            canStartQuiz = false;
        else if (other.CompareTag("Avatar"))
            canTalkToAvatar = false;
        else if (other.CompareTag("Leaderboard"))
            canOpenLeaderboard = false;
    }

    void Update()
    {
        if (canStartQuiz && Input.GetKeyDown(KeyCode.E))
        {
            // 打开测验窗口
        }

        if (canTalkToAvatar && Input.GetKeyDown(KeyCode.Return))
        {
            // 播放对话
        }

        if (canOpenLeaderboard && Input.GetKeyDown(KeyCode.L))
        {
            // 显示排行榜
        }
    }

}
