using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TestElevenLabsUI : MonoBehaviour
{
    public Button sendButton;
    public TMP_InputField inputField;
    public ElevenlabsAPI tts;
    public AudioSource studentAudioSource; // Added for playing directly on the student

    void Start()
    {
        // Add more robust null checks at Start
        if (tts == null) { Debug.LogError("ElevenlabsAPI (tts) reference missing in TestElevenLabsUI!"); return; }
        if (sendButton == null) { Debug.LogError("Send Button reference missing in TestElevenLabsUI!"); return; }
        if (inputField == null) { Debug.LogError("Input Field reference missing in TestElevenLabsUI!"); return; }
        if (studentAudioSource == null) { Debug.LogError("Student Audio Source reference missing in TestElevenLabsUI!"); return; }

        tts.AudioReceived.AddListener(PlayClip);

        sendButton.onClick.AddListener(() => {
            if (inputField.text.Length > 0) // Only send if there's text
            {
                tts.GetAudio(inputField.text);
                inputField.text = ""; // Clear input field immediately after sending
            }
            else
            {
                Debug.LogWarning("TestElevenLabsUI: Input field is empty. Not sending TTS request.");
            }
        });
    }

    public void PlayClip(AudioClip clip)
    {
        if (studentAudioSource != null)
        {
            if (clip != null)
            {
                studentAudioSource.clip = clip;
                studentAudioSource.Play();
                Debug.Log($"TestElevenLabsUI: Playing audio clip through student's AudioSource. Clip length: {clip.length}s.");
            }
            else
            {
                Debug.LogError("TestElevenLabsUI: Received NULL AudioClip to play!");
            }
        }
        else
        {
            Debug.LogError("TestElevenLabsUI: studentAudioSource is not assigned! Cannot play audio.");
            // Fallback to global playback if studentAudioSource is null (less ideal)
            // if (clip != null) AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position);
        }
    }
}