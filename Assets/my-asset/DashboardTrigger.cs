using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashboardTrigger : MonoBehaviour
{
    public GameObject hintUI;            // 拖入提示面板或Text
    public GameObject dashboardCanvas;   // 拖入你的排行榜 Canvas

    private bool playerInRange = false;

    void Start()
    {
        if (hintUI != null)
            hintUI.SetActive(false);
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("📊 玩家进入排行榜");
            hintUI.SetActive(false);
            dashboardCanvas.SetActive(true);
            // 你也可以加一个 Pause 功能
            // Time.timeScale = 0f;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            if (hintUI != null)
                hintUI.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (hintUI != null)
                hintUI.SetActive(false);
        }
    }
}

