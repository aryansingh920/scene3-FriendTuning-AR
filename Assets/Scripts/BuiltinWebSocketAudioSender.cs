using UnityEngine;
using System;
using System.Collections;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using UnityEngine.Networking;
using System.Collections.Concurrent;

public class BuiltinWebSocketAudioSender : MonoBehaviour
{
    private ClientWebSocket _webSocket;
    private AudioClip _recordingClip;
    private bool _isRecording = false;
    private const int _sampleRate = 44100;
    // private const string WebSocketServerUrl = "ws://localhost:8080";
    private const string WebSocketServerUrl = "wss://b2e8-2a02-8084-2861-3c80-65ea-a579-dacd-9441.ngrok-free.app";

    private int _lastSamplePos = 0;
    private string _micDevice;

    // Thread-safe queue for messages arriving from the server
    private ConcurrentQueue<object> _inboundMessageQueue = new ConcurrentQueue<object>();

    // For audio playback
    private AudioSource _audioSource;

    private CharacterPlacer characterPlacer;


    async void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        await ConnectToWebSocket();
        // Start a background task to continuously receive messages
        _ = Task.Run(() => ListenForServerMessages());

        // Check if the CharacterPlacer component is available
        characterPlacer = FindFirstObjectByType<CharacterPlacer>();

    }

    void Update()
    {
        // Handle any enqueued messages from the server on the main thread
        while (_inboundMessageQueue.TryDequeue(out object message))
        {
            if (message is string textMessage)
            {
                HandleTextMessage(textMessage);
            }
            else if (message is AudioStreamData audioData)
            {
                HandleAudioData(audioData);
            }
        }

        // For demonstration: press P to start/stop local recording
        if (Input.GetKeyDown(KeyCode.P))
        {
            ToggleRecording();
        }
    }

    void OnDestroy()
    {
        DisconnectWebSocket();
    }

    // Class to hold audio data and content type
    private class AudioStreamData
    {
        public byte[] AudioBytes { get; set; }
        public string ContentType { get; set; }
    }

    private void HandleTextMessage(string message)
    {
        Debug.Log($"[Main Thread] Received text message: {message}");

        // Check if it has our special prefix (keeping legacy support)
        if (message.StartsWith("RESPONSE_READY_ABS"))
        {
            // Example: "RESPONSE_READY_ABS /Users/aryan/....mp3"
            // We can parse out the second part
            string[] parts = message.Split(' ', 2); // split into 2 tokens max
            if (parts.Length == 2)
            {
                string absolutePath = parts[1].Trim();
                // Use a coroutine on the main thread to load and play
                StartCoroutine(PlayAudioFromAbsolutePath(absolutePath));
            }
        }
    }

    private void HandleAudioData(AudioStreamData audioData)
    {
        Debug.Log($"[Main Thread] Processing audio data: {audioData.ContentType}, {audioData.AudioBytes.Length} bytes");

        // For MP3 data, we need to save it to a temporary file first
        if (audioData.ContentType == "audio/mp3")
        {
            StartCoroutine(LoadAndPlayMP3(audioData.AudioBytes));
        }
        // Add handling for other audio formats if needed
    }

    // -------------------------
    // WEBSOCKET CONNECT/DISCONNECT
    // -------------------------
    private async Task ConnectToWebSocket()
    {
        try
        {
            _webSocket = new ClientWebSocket();
            await _webSocket.ConnectAsync(new Uri(WebSocketServerUrl), CancellationToken.None);
            Debug.Log("WebSocket connection established.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"WebSocket connection failed: {ex.Message}");
        }
    }

    private void DisconnectWebSocket()
    {
        if (_webSocket != null)
        {
            StopRecording(); // ensure we stop microphone streaming
            try
            {
                _webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Closing",
                    CancellationToken.None
                ).Wait();
                Debug.Log("WebSocket connection closed.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"WebSocket close error: {ex.Message}");
            }

            _webSocket.Dispose();
            _webSocket = null;
        }
    }

    // -------------------------
    // LISTEN FOR SERVER MESSAGES (Background Thread)
    // -------------------------
    private async Task ListenForServerMessages()
    {
        byte[] buffer = new byte[1024 * 1024]; // Larger buffer for audio data (1 MB)

        while (_webSocket != null && _webSocket.State == WebSocketState.Open)
        {
            try
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Debug.Log("Server closed connection");
                    break;
                }
                else if (result.MessageType == WebSocketMessageType.Text)
                {
                    // Handle text message
                    string serverMsg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    // Enqueue for main-thread handling
                    _inboundMessageQueue.Enqueue(serverMsg);
                }
                else if (result.MessageType == WebSocketMessageType.Binary)
                {
                    // Check if this is an audio message with our prefix
                    string prefix = Encoding.UTF8.GetString(buffer, 0, Math.Min(50, result.Count));

                    if (prefix.StartsWith("AUDIO_DATA:"))
                    {
                        // Extract content type and audio data
                        int secondColonIndex = prefix.IndexOf(':', prefix.IndexOf(':') + 1);

                        if (secondColonIndex != -1)
                        {
                            string contentType = prefix.Substring(11, secondColonIndex - 11);

                            // Copy the actual audio data (skipping the prefix)
                            byte[] audioData = new byte[result.Count - (secondColonIndex + 1)];
                            Array.Copy(buffer, secondColonIndex + 1, audioData, 0, audioData.Length);

                            Debug.Log($"[Background Thread] Received {audioData.Length} bytes of {contentType}");

                            // Enqueue audio data for main thread processing
                            _inboundMessageQueue.Enqueue(new AudioStreamData
                            {
                                AudioBytes = audioData,
                                ContentType = contentType
                            });
                        }
                    }
                    else
                    {
                        Debug.Log($"Received {result.Count} bytes of binary data (not audio)");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error receiving from server: " + e.Message);
                break;
            }
        }
    }

    // -------------------------
    // MICROPHONE CAPTURE & SEND
    // -------------------------
    private void ToggleRecording()
    {
        if (!_isRecording) StartRecording();
        else StopRecording();
    }

    public void StartRecording()
    {
        if (_isRecording) return;

        _micDevice = Microphone.devices.Length > 0 ? Microphone.devices[0] : null;
        if (string.IsNullOrEmpty(_micDevice))
        {
            Debug.LogError("No microphone detected!");
            return;
        }

        _recordingClip = Microphone.Start(_micDevice, true, 10, _sampleRate);
        if (_recordingClip == null)
        {
            Debug.LogError("Failed to start microphone recording!");
            return;
        }

        _isRecording = true;
        Debug.Log("Started audio recording locally.");

        // Signal Python to start
        SendTextCommand("START_RECORDING");
        _lastSamplePos = 0;

        // Continuously read mic data in small chunks
        StartCoroutine(StreamAudioData());
    }

    private IEnumerator StreamAudioData()
    {
        while (_isRecording)
        {
            yield return new WaitForSeconds(0.1f);

            if (_recordingClip != null)
            {
                int currentPosition = Microphone.GetPosition(_micDevice);
                int samplesAvailable = currentPosition - _lastSamplePos;

                if (samplesAvailable < 0)
                {
                    // The microphone position wrapped around the end of the audio buffer
                    samplesAvailable = (_recordingClip.samples - _lastSamplePos) + currentPosition;
                }

                if (samplesAvailable > 0)
                {
                    // Extract the samples
                    float[] floatSamples = new float[samplesAvailable * _recordingClip.channels];
                    _recordingClip.GetData(floatSamples, _lastSamplePos);

                    byte[] pcm16 = FloatArrayToPCM16(floatSamples);

                    if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                    {
                        var sendTask = SendBinaryData(pcm16);
                        // Wait for send to complete
                        yield return new WaitUntil(() => sendTask.IsCompleted);

                        if (sendTask.IsFaulted)
                        {
                            Debug.LogError($"WebSocket send error: {sendTask.Exception}");
                        }
                    }

                    // Update last read position
                    _lastSamplePos = currentPosition;
                }
            }
        }
    }

    public void StopRecording()
    {
        if (!_isRecording) return;

        Microphone.End(_micDevice);
        _isRecording = false;
        _recordingClip = null;
        Debug.Log("Stopped audio recording locally.");

        // Signal Python to stop
        SendTextCommand("STOP_RECORDING");
    }

    private byte[] FloatArrayToPCM16(float[] floatSamples)
    {
        byte[] pcm16 = new byte[floatSamples.Length * 2];
        int outputIndex = 0;

        for (int i = 0; i < floatSamples.Length; i++)
        {
            short sampleInt16 = (short)Mathf.Clamp(floatSamples[i] * 32767f, short.MinValue, short.MaxValue);
            pcm16[outputIndex++] = (byte)(sampleInt16 & 0xFF);
            pcm16[outputIndex++] = (byte)((sampleInt16 >> 8) & 0xFF);
        }

        return pcm16;
    }

    private async Task SendBinaryData(byte[] audioBytes)
    {
        try
        {
            await _webSocket.SendAsync(
                new ArraySegment<byte>(audioBytes),
                WebSocketMessageType.Binary,
                endOfMessage: true,
                cancellationToken: CancellationToken.None
            );
        }
        catch (Exception ex)
        {
            Debug.LogError($"WebSocket send (binary) failed: {ex.Message}");
        }
    }

    private async void SendTextCommand(string command)
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open)
        {
            Debug.LogWarning("WebSocket is not connected; cannot send command.");
            return;
        }

        try
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(command);
            await _webSocket.SendAsync(
                new ArraySegment<byte>(messageBytes),
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken: CancellationToken.None
            );
            Debug.Log($"Sent text command to server: {command}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"WebSocket send (text) failed: {ex.Message}");
        }
    }

    // -------------------------
    // PLAY MP3 DIRECTLY FROM BYTES
    // -------------------------
    private IEnumerator LoadAndPlayMP3(byte[] mp3Data)
    {
        // Save to temporary file (Unity can't load MP3 from memory directly)
        string tempPath = Path.Combine(Application.temporaryCachePath, $"temp_audio_{DateTime.Now.Ticks}.mp3");
        File.WriteAllBytes(tempPath, mp3Data);

        // Create a web request to load the audio
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + tempPath, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                if (clip != null)
                {
                    Debug.Log($"Successfully loaded audio of length {clip.length} seconds");

                    _audioSource.clip = clip;
                    _audioSource.Play();

                    // Notify character to switch to talking animation
                    if (characterPlacer != null)
                        characterPlacer.PlayTalkingAnimation();

                    // Wait for the audio to finish
                    yield return new WaitForSeconds(clip.length);

                    // Switch back to idle
                    if (characterPlacer != null)
                        characterPlacer.PlayIdleAnimation();
                }

            }
            else
            {
                Debug.LogError($"Error loading audio: {www.error}");
            }
        }

        // Try to clean up the temporary file after a delay
        yield return new WaitForSeconds(1.0f);
        try
        {
            File.Delete(tempPath);
            Debug.Log("Deleted temporary MP3 file");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Could not delete temporary file: {e.Message}");
        }
    }

    // -------------------------
    // LEGACY: PLAY THE MP3 (FROM ABSOLUTE PATH)
    // -------------------------
    private IEnumerator PlayAudioFromAbsolutePath(string absolutePath)
    {
        Debug.Log($"[PlayAudioFromAbsolutePath] Checking: {absolutePath}");

        // 1) Ensure file actually exists
        if (!File.Exists(absolutePath))
        {
            Debug.LogError($"File not found on disk: {absolutePath}");
            yield break;
        }

        // 2) Prepend "file://" for UnityWebRequest
        string uri = "file://" + absolutePath;
        Debug.Log("Trying to load audio from: " + uri);

        // 3) Request an AudioClip from that URI
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error loading audio: " + www.error);
            }
            else
            {
                // 4) Convert to an AudioClip
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                if (clip != null)
                {
                    Debug.Log($"Successfully loaded audio. Playing: {absolutePath}");
                    _audioSource.clip = clip;
                    _audioSource.Play();
                }
            }
        }
    }
}
