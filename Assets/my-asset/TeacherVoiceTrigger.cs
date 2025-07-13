using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class TeacherVoiceTrigger : MonoBehaviourPunCallbacks, IPunObservable
{
    // --- Public Fields ---
    [Header("PUN Components")]
    public PhotonView photonView;

    [Header("ElevenLabs API")]
    [SerializeField] private string _voiceId;
    [SerializeField] private string _apiKey;
    [SerializeField] private string _apiUrl = "https://api.elevenlabs.io";
    public bool Streaming;
    [Range(0, 4)] public int LatencyOptimization;
    public UnityEvent<AudioClip> AudioReceived;

    [Header("Dialog UI Components")]
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private TextMeshProUGUI dialogText;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button skipButton;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("Camera Control (Host Only)")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera avatarCamera;

    [Header("Dialogue System Settings")]
    public KeyCode nextDialogueKey = KeyCode.Backspace;
    public KeyCode startDialogueKey = KeyCode.Return;
    public KeyCode skipDialogueKey = KeyCode.Escape;
    public float typewriterSpeed = 0.05f;
    public bool autoAdvanceAfterAudio = false;
    public float autoAdvanceDelay = 1f;

    [Header("Dialogue Content")]
    public List<DialogueEntry> dialogueEntries = new List<DialogueEntry>();

    // --- Private Fields ---
    private AudioSource audioSource;
    private Dictionary<int, AudioClip> cachedAudioClips = new Dictionary<int, AudioClip>();
    private Coroutine typewriterCoroutine;

    // --- Synchronized State Variables ---
    private int currentDialogueIndex = -1;
    private bool isPlayingDialogue = false;

    public bool IsPlayingDialogue => isPlayingDialogue;

    [System.Serializable]
    public class DialogueEntry
    {
        public string speakerName;
        public string dialogueText;
        public Sprite characterSprite;
        public Color textColor = Color.white;
        public AudioClip customAudioClip;
    }

    #region Unity & PUN Callbacks

    void Awake()
    {
        if (photonView == null)
        {
            photonView = GetComponent<PhotonView>();
        }
    }

    void Start()
    {
        Debug.Log("=== PUN TeacherVoiceTrigger Start Called ===");
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        AudioReceived.AddListener(OnAudioReceived);
        InitializeUI();
        SetupDefaultDialogue();

        if (PhotonNetwork.IsMasterClient)
        {
            if (mainCamera != null) mainCamera.gameObject.SetActive(true);
            if (avatarCamera != null) avatarCamera.gameObject.SetActive(false);
            Debug.Log("PUN TeacherVoiceTrigger: Master client is ready! Press Enter to start dialogue.");
        }
        else
        {
            Debug.Log("PUN TeacherVoiceTrigger: Client connected and ready for synchronized dialogue.");
        }
    }

    void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (Input.GetKeyDown(startDialogueKey) && !isPlayingDialogue)
        {
            StartDialogue();
        }
        else if (Input.GetKeyDown(nextDialogueKey) && isPlayingDialogue)
        {
            HandleNextInput();
        }
        else if (Input.GetKeyDown(skipDialogueKey) && isPlayingDialogue)
        {
            EndDialogue(); // 直接调用EndDialogue
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(currentDialogueIndex);
            stream.SendNext(isPlayingDialogue);
        }
        else
        {
            int newIndex = (int)stream.ReceiveNext();
            bool newIsPlaying = (bool)stream.ReceiveNext();

            if (currentDialogueIndex != newIndex || isPlayingDialogue != newIsPlaying)
            {
                currentDialogueIndex = newIndex;
                isPlayingDialogue = newIsPlaying;
                HandleStateChangeOnClient();
            }
        }
    }

    #endregion

    #region Dialogue Flow Control (Master Client Only Logic)

    private void HandleNextInput()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (typewriterCoroutine != null)
        {
            CompleteTextDisplay();
        }
        else if (!audioSource.isPlaying)
        {
            PlayNextDialogue();
        }
    }

    public void StartDialogue()
    {
        if (!PhotonNetwork.IsMasterClient || isPlayingDialogue) return;

        photonView.RPC("RPC_SetCursorState", RpcTarget.All, false);

        if (mainCamera != null) mainCamera.gameObject.SetActive(false);
        if (avatarCamera != null) avatarCamera.gameObject.SetActive(true);

        photonView.RPC("RPC_ShowDialogPanel", RpcTarget.All, true);
        currentDialogueIndex = 0;
        isPlayingDialogue = true;
        Debug.Log("PUN TeacherVoiceTrigger: Starting dialogue sequence...");
        PlayCurrentDialogue();
    }

    public void PlayNextDialogue()
    {
        if (!PhotonNetwork.IsMasterClient || !isPlayingDialogue) return;

        int nextIndex = currentDialogueIndex + 1;
        if (nextIndex >= dialogueEntries.Count)
        {
            EndDialogue();
            return;
        }
        currentDialogueIndex = nextIndex;
        PlayCurrentDialogue();
    }

    // 这个方法现在只由Master Client调用
    private void EndDialogue()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        photonView.RPC("RPC_SetCursorState", RpcTarget.All, true);

        if (mainCamera != null) mainCamera.gameObject.SetActive(true);
        if (avatarCamera != null) avatarCamera.gameObject.SetActive(false);

        isPlayingDialogue = false;
        currentDialogueIndex = -1;
        photonView.RPC("RPC_ShowDialogPanel", RpcTarget.All, false);
        Debug.Log("PUN TeacherVoiceTrigger: Dialogue sequence completed!");
    }

    private void PlayCurrentDialogue()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int index = currentDialogueIndex;
        if (index >= 0 && index < dialogueEntries.Count)
        {
            DialogueEntry currentEntry = dialogueEntries[index];
            photonView.RPC("RPC_UpdateDialogUI", RpcTarget.All, currentEntry.speakerName, currentEntry.dialogueText, currentEntry.textColor.r, currentEntry.textColor.g, currentEntry.textColor.b);
            photonView.RPC("RPC_StartTypewriter", RpcTarget.All, currentEntry.dialogueText);
            Debug.Log($"PUN TeacherVoiceTrigger: Playing dialogue {index + 1}/{dialogueEntries.Count}: '{currentEntry.dialogueText}'");

            if (currentEntry.customAudioClip != null)
            {
                OnAudioReceived(currentEntry.customAudioClip);
            }
            else
            {
                GetAudio(currentEntry.dialogueText);
            }
        }
    }

    private void CompleteTextDisplay()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        if (currentDialogueIndex < dialogueEntries.Count)
        {
            photonView.RPC("RPC_UpdateDialogText", RpcTarget.All, dialogueEntries[currentDialogueIndex].dialogueText);
        }
    }

    #endregion

    #region Client-Side State Handling & Requests

    private void HandleStateChangeOnClient()
    {
        Debug.Log($"Client received state change: index={currentDialogueIndex}, isPlaying={isPlayingDialogue}");
    }

    public void RequestPlayNextDialogue()
    {
        
        photonView.RPC("RPC_RequestNext", RpcTarget.MasterClient);
        Debug.Log("Sent request to Master Client to play next dialogue.");
    }

    public void RequestSkipDialogue()
    {
       
        photonView.RPC("RPC_RequestSkip", RpcTarget.MasterClient);
        Debug.Log("Sent request to Master Client to skip dialogue.");
    }

    #endregion

    #region Audio Handling & Networking

    private void OnAudioReceived(AudioClip clip)
    {
        if (clip != null)
        {
            cachedAudioClips[currentDialogueIndex] = clip;
            PlayAudioClip(clip);

            if (PhotonNetwork.IsMasterClient)
            {
                StartCoroutine(NetworkAudioData(clip, currentDialogueIndex));
                StartCoroutine(WaitForAudioComplete(clip));
            }
        }
    }

    private IEnumerator WaitForAudioComplete(AudioClip clip)
    {
        if (!PhotonNetwork.IsMasterClient) yield break;
        yield return new WaitForSeconds(clip.length);
        if (autoAdvanceAfterAudio && currentDialogueIndex + 1 < dialogueEntries.Count)
        {
            yield return new WaitForSeconds(autoAdvanceDelay);
            PlayNextDialogue();
        }
    }

    private void PlayAudioClip(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.Play();
            Debug.Log($"Playing audio clip. Length: {clip.length} seconds");
        }
    }

    private IEnumerator NetworkAudioData(AudioClip clip, int dialogueIndex)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);
        byte[] audioBytes = new byte[samples.Length * 4];
        Buffer.BlockCopy(samples, 0, audioBytes, 0, audioBytes.Length);
        Debug.Log($"Sending audio data for index {dialogueIndex}, size: {audioBytes.Length} bytes");
        photonView.RPC("RPC_ReceiveAudioData", RpcTarget.Others, dialogueIndex, audioBytes, clip.frequency, clip.channels);
        yield return null;
    }

    private IEnumerator CreateAudioFromBytes(byte[] audioData, int dialogueIndex, int frequency, int channels)
    {
        float[] samples = new float[audioData.Length / 4];
        Buffer.BlockCopy(audioData, 0, samples, 0, audioData.Length);
        AudioClip networkClip = AudioClip.Create($"NetworkedDialogue_{dialogueIndex}", samples.Length / channels, channels, frequency, false);
        networkClip.SetData(samples, 0);
        cachedAudioClips[dialogueIndex] = networkClip;

        if (currentDialogueIndex == dialogueIndex)
        {
            PlayAudioClip(networkClip);
        }
        yield return null;
    }

    #endregion

    #region UI & RPC Methods

    [PunRPC]
    private void RPC_RequestNext()
    {
        if (!PhotonNetwork.IsMasterClient) return; // 再次确认只有Master Client执行

        Debug.Log("Master Client received request to play next dialogue.");
        if (isPlayingDialogue)
        {
            HandleNextInput();
        }
    }

    [PunRPC]
    private void RPC_RequestSkip()
    {
        if (!PhotonNetwork.IsMasterClient) return; 

        Debug.Log("Master Client received request to skip dialogue.");
        if (isPlayingDialogue)
        {
            EndDialogue();
        }
    }


    [PunRPC]
    public void RPC_ReceiveAudioData(int dialogueIndex, byte[] audioData, int frequency, int channels)
    {
        Debug.Log($"[Client] Received audio data for index {dialogueIndex}");
        StartCoroutine(CreateAudioFromBytes(audioData, dialogueIndex, frequency, channels));
    }

    [PunRPC]
    public void RPC_ShowDialogPanel(bool show)
    {
        dialogPanel?.SetActive(show);
    }

    [PunRPC]
    public void RPC_UpdateDialogUI(string speakerName, string text, float r, float g, float b)
    {
        if (speakerNameText != null) speakerNameText.text = speakerName;
        if (dialogText != null) dialogText.color = new Color(r, g, b, 1f);
    }

    [PunRPC]
    public void RPC_UpdateDialogText(string text)
    {
        if (dialogText != null)
        {
            dialogText.text = text;
        }
    }

    [PunRPC]
    public void RPC_StartTypewriter(string text)
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }
        typewriterCoroutine = StartCoroutine(TypewriterEffect(text));
    }

    [PunRPC]
    private void RPC_SetCursorState(bool locked)
    {
        if (locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
    private IEnumerator TypewriterEffect(string text)
    {
        if (dialogText == null) yield break;
        dialogText.text = "";
        foreach (char letter in text.ToCharArray())
        {
            dialogText.text += letter;
            yield return new WaitForSeconds(typewriterSpeed);
        }
        typewriterCoroutine = null;
    }

    #endregion

    #region Initialization and Setup
    private void InitializeUI()
    {
        if (dialogPanel == null) dialogPanel = GameObject.Find("Dialog");
        if (dialogText == null && dialogPanel != null) dialogText = dialogPanel.GetComponentInChildren<TextMeshProUGUI>();

        // ★★★ 修改这里 ★★★
        // 移除 if (PhotonNetwork.IsMasterClient) 的判断
        // 让所有玩家的按钮都监听“请求”方法
        nextButton?.onClick.AddListener(RequestPlayNextDialogue);
        skipButton?.onClick.AddListener(RequestSkipDialogue);

        dialogPanel?.SetActive(false);
    }

    private void SetupDefaultDialogue()
    {
        if (dialogueEntries.Count == 0)
        {
            dialogueEntries.Add(new DialogueEntry { speakerName = "Teacher", dialogueText = "Hello! Welcome to our synchronized classroom.", textColor = Color.cyan });
            dialogueEntries.Add(new DialogueEntry { speakerName = "Teacher", dialogueText = "You will be asked to answer the quiz.", textColor = Color.cyan });
            dialogueEntries.Add(new DialogueEntry { speakerName = "Teacher", dialogueText = "Quiz paper is on the table.", textColor = Color.cyan });
            dialogueEntries.Add(new DialogueEntry { speakerName = "Teacher", dialogueText = "Take your time to read the questions.", textColor = Color.cyan });
            dialogueEntries.Add(new DialogueEntry { speakerName = "Teacher", dialogueText = "Good luck!", textColor = Color.cyan });
        }
    }
    #endregion

    #region ElevenLabs API Call
    public void GetAudio(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        StartCoroutine(DoRequest(text));
    }

    IEnumerator DoRequest(string message)
    {
        var postData = new TextToSpeechRequest
        {
            text = message,
            model_id = "eleven_monolingual_v1",
            voice_settings = new VoiceSettings { stability = 0, similarity_boost = 0, style = 0.5f, use_speaker_boost = true }
        };

        string json = JsonConvert.SerializeObject(postData);
        byte[] jsonToSend = Encoding.UTF8.GetBytes(json);
        string stream = (Streaming) ? "/stream" : "";
        string url = $"{_apiUrl}/v1/text-to-speech/{_voiceId}{stream}?optimize_streaming_latency={LatencyOptimization}";

        using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            var downloadHandler = new DownloadHandlerAudioClip(url, AudioType.MPEG);
            if (Streaming)
            {
                downloadHandler.streamAudio = true;
            }
            request.downloadHandler = downloadHandler;
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("xi-api-key", _apiKey);
            request.SetRequestHeader("Accept", "audio/mpeg");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"ElevenLabs API Error: {request.error}");
                yield break;
            }

            AudioClip audioClip = downloadHandler.audioClip;
            if (audioClip != null)
            {
                AudioReceived.Invoke(audioClip);
            }
        }
    }

    [Serializable] public class TextToSpeechRequest { public string text; public string model_id; public VoiceSettings voice_settings; }
    [Serializable] public class VoiceSettings { public int stability; public int similarity_boost; public float style; public bool use_speaker_boost; }

    #endregion
}