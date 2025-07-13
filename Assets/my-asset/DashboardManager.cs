using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;

public class DashboardManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dashboardPanel;
    public GameObject entryPrefab;
    public Transform leaderboardContent;
    public TextMeshProUGUI myScoreText;
    public TextMeshProUGUI myRankText;
    public Button closeButton;

    private DatabaseReference dbRef;
    private FirebaseAuth auth;
    private bool firebaseInitialized = false;

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                InitializeFirebase();
            }
            else
            {
                Debug.LogError("Could not resolve Firebase dependencies: " + task.Result);
            }
        });

        if (closeButton != null)
            closeButton.onClick.AddListener(() =>
            {
                dashboardPanel.SetActive(false);
            });
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            dashboardPanel.SetActive(true);
            LoadLeaderboard();
        }
    }

    void InitializeFirebase()
    {
        try
        {
            auth = FirebaseAuth.DefaultInstance;

            // 替换为你的 Firebase 数据库 URL
            const string dbUrl = "https://rtcg-metaverse-default-rtdb.asia-southeast1.firebasedatabase.app/";
            dbRef = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance, dbUrl).RootReference;

            firebaseInitialized = true;
            Debug.Log("Firebase initialized successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Firebase initialization failed: " + e.Message);
        }
    }

    void LoadLeaderboard()
    {
        if (!firebaseInitialized)
        {
            Debug.LogWarning("Firebase not initialized yet");
            return;
        }

        if (dbRef == null)
        {
            Debug.LogError("Database reference is null");
            return;
        }

        // 获取所有玩家数据
        dbRef.Child("players")
             .GetValueAsync()
             .ContinueWithOnMainThread(task =>
             {
                 if (task.IsFaulted)
                 {
                     Debug.LogError("Load players error: " + task.Exception);
                     return;
                 }

                 if (!task.Result.Exists)
                 {
                     Debug.LogWarning("No players data found in database");
                     return;
                 }

                 // 解析所有玩家数据
                 var allPlayers = new List<PlayerData>();
                 foreach (var snap in task.Result.Children)
                 {
                     string uid = snap.Key;
                     string name = snap.Child("name").Value?.ToString() ?? "Unknown";

                     if (int.TryParse(snap.Child("score").Value?.ToString(), out int score))
                     {
                         allPlayers.Add(new PlayerData
                         {
                             uid = uid,
                             name = name,
                             score = score
                         });
                     }
                 }

                 if (allPlayers.Count == 0)
                 {
                     Debug.LogWarning("No valid player data found");
                     return;
                 }

                 // 按分数降序排序（从高到低）
                 allPlayers = allPlayers.OrderByDescending(p => p.score).ToList();

                 Debug.Log($"Loaded {allPlayers.Count} players, sorted by score:");
                 foreach (var player in allPlayers)
                 {
                     Debug.Log($"  {player.name}: {player.score}");
                 }

                 // 显示排行榜（前5名）
                 DisplayLeaderboard(allPlayers);

                 // 显示个人统计
                 DisplayPersonalStats(allPlayers);
             });
    }

    void DisplayLeaderboard(List<PlayerData> allPlayers)
    {
        Debug.Log($"DisplayLeaderboard called with {allPlayers.Count} players");

        // 先清空旧条目
        foreach (Transform child in leaderboardContent)
            Destroy(child.gameObject);

        // 等待一帧让 Destroy 完成
        StartCoroutine(DisplayLeaderboardCoroutine(allPlayers));
    }

    System.Collections.IEnumerator DisplayLeaderboardCoroutine(List<PlayerData> allPlayers)
    {
        yield return null; // 等待一帧

        // 显示前5名
        int displayCount = Mathf.Min(5, allPlayers.Count);
        Debug.Log($"Displaying top {displayCount} players");

        for (int i = 0; i < displayCount; i++)
        {
            var player = allPlayers[i];
            var go = Instantiate(entryPrefab, leaderboardContent);

            Debug.Log($"Created entry for rank {i + 1}: {player.name} - {player.score}");
            Debug.Log($"Entry prefab children: {go.transform.childCount}");

            // 列出所有子对象的名称
            for (int j = 0; j < go.transform.childCount; j++)
            {
                Debug.Log($"  Child {j}: {go.transform.GetChild(j).name}");
            }

            // 尝试多种方式查找组件
            var rankText = FindTextComponent(go.transform, "RankText", "Rank");
            var nameText = FindTextComponent(go.transform, "NameText", "Name");
            var scoreText = FindTextComponent(go.transform, "ScoreText", "Score");

            if (rankText != null)
            {
                rankText.text = (i + 1).ToString();
                Debug.Log($"Set rank text: {rankText.text}");
            }
            else
            {
                Debug.LogError($"RankText component not found in entry {i + 1}");
            }

            if (nameText != null)
            {
                nameText.text = player.name;
                Debug.Log($"Set name text: {nameText.text}");
            }
            else
            {
                Debug.LogError($"NameText component not found in entry {i + 1}");
            }

            if (scoreText != null)
            {
                scoreText.text = player.score.ToString();
                Debug.Log($"Set score text: {scoreText.text}");
            }
            else
            {
                Debug.LogError($"ScoreText component not found in entry {i + 1}");
            }

            Debug.Log($"Successfully displayed rank {i + 1}: {player.name} - {player.score}");
        }
    }

    TextMeshProUGUI FindTextComponent(Transform parent, params string[] possibleNames)
    {
        foreach (string name in possibleNames)
        {
            var found = parent.Find(name);
            if (found != null)
            {
                var textComponent = found.GetComponent<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    Debug.Log($"Found text component: {name}");
                    return textComponent;
                }
            }
        }

        // 如果直接查找失败，尝试递归查找
        return FindTextComponentRecursive(parent, possibleNames);
    }

    TextMeshProUGUI FindTextComponentRecursive(Transform parent, string[] possibleNames)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            foreach (string name in possibleNames)
            {
                if (child.name.Contains(name))
                {
                    var textComponent = child.GetComponent<TextMeshProUGUI>();
                    if (textComponent != null)
                    {
                        Debug.Log($"Found text component recursively: {child.name}");
                        return textComponent;
                    }
                }
            }

            // 递归查找
            var found = FindTextComponentRecursive(child, possibleNames);
            if (found != null) return found;
        }
        return null;
    }

    void DisplayPersonalStats(List<PlayerData> allPlayers)
    {
        if (auth.CurrentUser == null)
        {
            myScoreText.text = "Your Score: --";
            myRankText.text = "Your Rank: --";
            return;
        }

        string myUid = auth.CurrentUser.UserId;

        // 查找当前用户
        PlayerData myPlayerData = null;
        int myRank = -1;

        for (int i = 0; i < allPlayers.Count; i++)
        {
            if (allPlayers[i].uid == myUid)
            {
                myPlayerData = allPlayers[i];
                myRank = i + 1; // 排名从1开始
                break;
            }
        }

        if (myPlayerData != null)
        {
            myScoreText.text = $"Your Score: {myPlayerData.score}";
            myRankText.text = $"Your Rank: {myRank}";
            Debug.Log($"Personal stats - Score: {myPlayerData.score}, Rank: {myRank}");
        }
        else
        {
            myScoreText.text = "Your Score: 0";
            myRankText.text = "Your Rank: --";
            Debug.Log("Current user not found in leaderboard");
        }
    }

    // 用于添加/更新分数的公共方法
    public void UpdatePlayerScore(string playerName, int score)
    {
        if (!firebaseInitialized)
        {
            Debug.LogWarning("Cannot update score: Firebase not ready");
            return;
        }

        // 如果没有用户，先进行匿名登录
        if (auth.CurrentUser == null)
        {
            auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(signInTask =>
            {
                if (signInTask.IsCompleted && !signInTask.IsFaulted)
                {
                    Debug.Log("Anonymous sign-in successful");
                    SavePlayerScore(playerName, score);
                }
                else
                {
                    Debug.LogError("Anonymous sign-in failed: " + signInTask.Exception);
                }
            });
        }
        else
        {
            SavePlayerScore(playerName, score);
        }
    }

    void SavePlayerScore(string playerName, int score)
    {
        string uid = auth.CurrentUser.UserId;
        var playerData = new Dictionary<string, object>
        {
            ["name"] = playerName,
            ["score"] = score,
            ["completed"] = true,
            ["timestamp"] = System.DateTime.UtcNow.ToString()
        };

        dbRef.Child("players").Child(uid).SetValueAsync(playerData).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log($"Score updated successfully: {playerName} - {score}");
            }
            else
            {
                Debug.LogError("Failed to update score: " + task.Exception);
            }
        });
    }

    // 数据结构类
    [System.Serializable]
    public class PlayerData
    {
        public string uid;
        public string name;
        public int score;
    }
}