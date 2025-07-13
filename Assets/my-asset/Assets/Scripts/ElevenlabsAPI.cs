using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class ElevenlabsAPI : MonoBehaviour
{
    [SerializeField]
    private string _voiceId;
    [SerializeField]
    private string _apiKey;
    [SerializeField]
    private string _apiUrl = "https://api.elevenlabs.io";

    private AudioClip _audioClip;
    public bool Streaming; // If true, the audio will be streamed instead of downloaded
    [Range(0, 4)]
    public int LatencyOptimization;
    public UnityEvent<AudioClip> AudioReceived;

    // Constructor - not typically used by Unity for MonoBehaviour
    // public ElevenlabsAPI(string apiKey, string voiceId) { _apiKey = apiKey; _voiceId = voiceId; }

    // Public property to expose _voiceId for easier access (if needed)
    public string VoiceId
    {
        get { return _voiceId; }
        set { _voiceId = value; }
    }

    // Public property to expose _apiKey (if needed)
    public string ApiKey
    {
        get { return _apiKey; }
        set { _apiKey = value; }
    }


    public void GetAudio(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Debug.LogWarning("ElevenlabsAPI: Text input is empty. Not sending request.");
            return;
        }
        StartCoroutine(DoRequest(text));
    }

    IEnumerator DoRequest(string message)
    {
        Debug.Log($"ElevenlabsAPI: Preparing request for text: '{message}' with voice ID: '{_voiceId}'"); // Log what's being sent

        var postData = new TextToSpeechRequest
        {
            text = message,
            model_id = "eleven_monolingual_v1"
        };
        var voiceSetting = new VoiceSettings
        {
            stability = 0,
            similarity_boost = 0,
            style = 0.5f,
            use_speaker_boost = true
        };
        postData.voice_settings = voiceSetting;

        var json = JsonConvert.SerializeObject(postData);
        Debug.Log($"ElevenlabsAPI: Request JSON: {json}"); // Log the full JSON payload

        // Update the UnityWebRequest.Post call as discussed previously to avoid obsolete warning
        byte[] jsonToSend = Encoding.UTF8.GetBytes(json);
        var stream = (Streaming) ? "/stream" : "";
        var url = $"{_apiUrl}/v1/text-to-speech/{_voiceId}{stream}?optimize_streaming_latency={LatencyOptimization}";

        Debug.Log($"ElevenlabsAPI: Request URL: {url}"); // Log the URL
        Debug.Log($"ElevenlabsAPI: Using API Key: '{_apiKey}'"); // Log the API key being used (ensure it's not empty)

        var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
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
            Debug.LogError($"ElevenlabsAPI: Error downloading audio: {request.error}"); // Will still log 401 if it happens
            yield break;
        }

        AudioClip audioClip = downloadHandler.audioClip;

        if (audioClip == null)
        {
            Debug.LogError("ElevenlabsAPI: Downloaded AudioClip is NULL even though request was successful. This can happen if the API returns non-audio data or an empty file.");
            // You might want to log downloadHandler.text here to see what the server actually returned
            // Debug.LogError("ElevenlabsAPI Raw Response: " + request.downloadHandler.text);
            yield break;
        }

        Debug.Log($"ElevenlabsAPI: Successfully received AudioClip. Length: {audioClip.length} seconds."); // Success log
        AudioReceived.Invoke(audioClip);
        request.Dispose();
    }

    [Serializable]
    public class TextToSpeechRequest
    {
        public string text;
        public string model_id; // eleven_monolingual_v1
        public VoiceSettings voice_settings;
    }

    [Serializable]
    public class VoiceSettings
    {
        public int stability; // 0
        public int similarity_boost; // 0
        public float style; // 0.5
        public bool use_speaker_boost; // true
    }
}