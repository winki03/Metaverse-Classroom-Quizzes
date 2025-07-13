using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using Firebase.Auth;

public class FirebaseUploaded : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField nameInputField;
    public Button submitNameButton;
    public Button submitScoreButton;

    [Header("Firebase Configuration")]
    [Tooltip("填入你的Firebase数据库URL，格式：https://your-project-default-rtdb.firebaseio.com/")]
    public string databaseURL = "https://your-project-default-rtdb.firebaseio.com/";

    [Header("Player Data")]
    public int playerScore = 0;

    private string playerId;
    private DatabaseReference dbReference;
    private bool isFirebaseInitialized = false;
    private FirebaseApp app;
    private FirebaseAuth auth;
    private FirebaseUser user;

    void Start()
    {
        Debug.Log("🔥 Start 开始执行");

        // Check if UI elements are assigned
        if (nameInputField == null)
            Debug.LogError("❌ nameInputField 未分配到Inspector中！");
        if (submitNameButton == null)
            Debug.LogError("❌ submitNameButton 未分配到Inspector中！");
        if (submitScoreButton == null)
            Debug.LogError("❌ submitScoreButton 未分配到Inspector中！");

        // Check database URL
        if (string.IsNullOrEmpty(databaseURL) || databaseURL.Contains("your-project"))
        {
            Debug.LogError("❌ 请在Inspector中设置正确的数据库URL！");
            Debug.LogError("格式示例：https://your-project-default-rtdb.firebaseio.com/");
            return;
        }

        InitializeFirebase();
    }

    void InitializeFirebase()
    {
        Debug.Log("🚀 开始初始化Firebase...");

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var status = task.Result;
            Debug.Log("📦 Firebase 依赖状态：" + status);

            if (status == DependencyStatus.Available)
            {
                try
                {
                    // 初始化Firebase App
                    app = FirebaseApp.DefaultInstance;
                    Debug.Log("📱 Firebase App 初始化成功");

                    // 创建自定义AppOptions（如果需要）
                    //var options = app.Options;
                    //Debug.Log("🔗 原始数据库URL: " + (options.DatabaseUrl ?? "未设置"));

                    // 方法1：尝试使用指定URL创建数据库实例
                    try
                    {
                        Debug.Log("🎯 尝试连接到数据库: " + databaseURL);
                        dbReference = FirebaseDatabase.GetInstance(app, databaseURL).RootReference;
                        Debug.Log("✅ 使用指定URL连接成功！");
                    }
                    catch (System.Exception urlException)
                    {
                        Debug.LogError("❌ 指定URL连接失败: " + urlException.Message);

                        // 方法2：尝试使用默认实例
                        try
                        {
                            Debug.Log("🔄 尝试使用默认数据库实例...");
                            dbReference = FirebaseDatabase.DefaultInstance.RootReference;
                            Debug.Log("✅ 默认实例连接成功！");
                        }
                        catch (System.Exception defaultException)
                        {
                            Debug.LogError("❌ 默认实例也失败: " + defaultException.Message);
                            Debug.LogError("🆘 可能的解决方案：");
                            Debug.LogError("1. 检查google-services.json文件是否在StreamingAssets文件夹中");
                            Debug.LogError("2. 确认Firebase项目已启用Realtime Database");
                            Debug.LogError("3. 检查数据库URL是否正确");
                            Debug.LogError("4. 重新下载google-services.json文件");
                            return;
                        }
                    }

                    //playerId = SystemInfo.deviceUniqueIdentifier;
                    //Debug.Log("🆔 Player ID: " + playerId);

                    auth= FirebaseAuth.DefaultInstance;
                    auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
                    {
                        if (task.IsCanceled || task.IsFaulted)
                        {
                            Debug.LogError("❌ 匿名登录失败：" + task.Exception);
                            return;
                        }

                        Firebase.Auth.AuthResult result = task.Result;
                        user = result.User; // ✅ 取出 User 对象
                        playerId = user.UserId;
                        Debug.Log("👤 使用Firebase用户ID作为Player ID: " + playerId);

                        // Add button listeners only if buttons exist
                        if (submitNameButton != null)
                            submitNameButton.onClick.AddListener(OnSubmitNameClicked);
                        if (submitScoreButton != null)
                            submitScoreButton.onClick.AddListener(OnSubmitScoreClicked);

                        isFirebaseInitialized = true;
                        Debug.Log("✅ Firebase 完全初始化成功！");

                        // Test connection
                        TestFirebaseConnection();

                        Debug.Log("当前用户 UID: " + FirebaseAuth.DefaultInstance.CurrentUser?.UserId);

                    });
                }
                catch (System.Exception e)
                {
                    Debug.LogError("❌ Firebase 初始化异常: " + e.Message);
                    Debug.LogError("异常堆栈: " + e.StackTrace);
                }
            }
            else
            {
                Debug.LogError("❌ Firebase 依赖检查失败: " + status);
                Debug.LogError("请检查：");
                Debug.LogError("1. Firebase SDK是否正确安装");
                Debug.LogError("2. google-services.json文件是否存在且正确");
                Debug.LogError("3. 网络连接是否正常");
            }
        });
    }

    void TestFirebaseConnection()
    {
        Debug.Log("🧪 测试Firebase连接...");

        dbReference.Child("test").SetValueAsync("connection_test_" + System.DateTime.Now.ToString())
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted)
                {
                    Debug.Log("✅ Firebase连接测试成功！数据已写入数据库");
                }
                else
                {
                    Debug.LogError("❌ Firebase连接测试失败!");
                    if (task.Exception != null)
                    {
                        Debug.LogError("异常详情: " + task.Exception);
                        foreach (var innerException in task.Exception.InnerExceptions)
                        {
                            Debug.LogError("内部异常: " + innerException.Message);
                        }
                    }
                }
            });
    }

    void OnSubmitNameClicked()
    {
        Debug.Log("🖱️ 提交名字按钮被点击");

        if (!isFirebaseInitialized)
        {
            Debug.LogError("❌ Firebase 尚未初始化完成！");
            return;
        }

        if (nameInputField == null)
        {
            Debug.LogError("❌ nameInputField 没绑定");
            return;
        }

        string playerName = nameInputField.text.Trim();
        Debug.Log("📝 输入的玩家名字: '" + playerName + "'");

        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogWarning("⚠️ 玩家名字不能为空！");
            return;
        }

        UploadPlayerName(playerName);
    }

    void OnSubmitScoreClicked()
    {
        Debug.Log("🖱️ 提交分数按钮被点击");

        if (!isFirebaseInitialized)
        {
            Debug.LogError("❌ Firebase 尚未初始化完成！");
            return;
        }

        Debug.Log("🏆 当前玩家分数: " + playerScore);
        UploadPlayerScore(playerScore);
    }

    public void UploadPlayerName(string playerName)
    {
        if (dbReference == null)
        {
            Debug.LogError("❌ 数据库引用为空！");
            return;
        }

        Debug.Log("⬆️ 开始上传玩家名字: " + playerName + " (PlayerID: " + playerId + ")");

        var nameRef = dbReference.Child("players").Child(playerId).Child("name");

        nameRef.SetValueAsync(playerName)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted)
                {
                    Debug.Log("✅ 玩家名字已成功上传: " + playerName);
                }
                else
                {
                    Debug.LogError("❌ 上传名字失败!");
                    if (task.Exception != null)
                    {
                        Debug.LogError("异常详情: " + task.Exception);
                        foreach (var innerException in task.Exception.InnerExceptions)
                        {
                            Debug.LogError("内部异常: " + innerException.Message);
                        }
                    }
                }
            });
    }

    public void UploadPlayerScore(int score)
    {
        if (dbReference == null)
        {
            Debug.LogError("❌ 数据库引用为空！");
            return;
        }

        Debug.Log("⬆️ 开始上传玩家分数: " + score + " (PlayerID: " + playerId + ")");

        var scoreRef = dbReference.Child("players").Child(playerId).Child("score");

        scoreRef.SetValueAsync(score)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted)
                {
                    Debug.Log("✅ 玩家分数已成功上传: " + score);
                }
                else
                {
                    Debug.LogError("❌ 上传分数失败!");
                    if (task.Exception != null)
                    {
                        Debug.LogError("异常详情: " + task.Exception);
                        foreach (var innerException in task.Exception.InnerExceptions)
                        {
                            Debug.LogError("内部异常: " + innerException.Message);
                        }
                    }
                }
            });
    }

    // 添加一个公共方法来手动设置分数
    public void SetPlayerScore(int newScore)
    {
        playerScore = newScore;
        Debug.Log("🏆 玩家分数已设置为: " + playerScore);
    }

    // 添加一个组合上传方法
    public void UploadPlayerData(string playerName, int score)
    {
        Debug.Log("📤 开始上传完整玩家数据...");

        if (dbReference == null)
        {
            Debug.LogError("❌ 数据库引用为空！");
            return;
        }

        var playerRef = dbReference.Child("players").Child(playerId);

        var playerData = new System.Collections.Generic.Dictionary<string, object>
        {
            { "name", playerName },
            { "score", score },
            { "timestamp", ServerValue.Timestamp }
        };

        playerRef.UpdateChildrenAsync(playerData)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted)
                {
                    Debug.Log("✅ 玩家完整数据已成功上传!");
                }
                else
                {
                    Debug.LogError("❌ 上传完整数据失败: " + task.Exception);
                }
            });
    }

    public void UploadPlayerCompleted()
    {
        if (dbReference == null || string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("❌ Firebase 未初始化，无法上传完成状态！");
            return;
        }

        dbReference.Child("players").Child(playerId).Child("completed").SetValueAsync(true)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted)
                {
                    Debug.Log("✅ 玩家已标记为已完成测验");
                }
                else
                {
                    Debug.LogError("❌ 设置玩家完成状态失败：" + task.Exception);
                }
            });
    }

    public void CheckIfQuizCompleted(System.Action<bool> onChecked)
    {
        if (dbReference == null || string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("❌ Firebase 未初始化，或 playerId 为空，无法检查是否完成测验");
            onChecked?.Invoke(false); // 安全 fallback
            return;
        }

        dbReference.Child("players").Child(playerId).Child("completed").GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || !task.IsCompleted)
                {
                    Debug.LogError("❌ 读取测验完成状态失败");
                    onChecked?.Invoke(false); // 默认为未完成
                    return;
                }

                bool completed = task.Result.Exists && task.Result.Value.ToString().ToLower() == "true";
                Debug.Log("📊 玩家测验完成状态: " + completed);
                onChecked?.Invoke(completed);
            });
    }
}