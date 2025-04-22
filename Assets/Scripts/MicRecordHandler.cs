using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MicRecordHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] public GameObject micButtonPrefab;
    private GameObject micButtonInstance;
    private BuiltinWebSocketAudioSender audioSender;

    void Start()
    {
        // Ensure audio sender exists on the scene
        audioSender = FindFirstObjectByType<BuiltinWebSocketAudioSender>();
        if (audioSender == null)
        {
            Debug.LogError("[MicRecordHandler] Could not find BuiltinWebSocketAudioSender in scene.");
            enabled = false;
            return;
        }

        // Setup the mic button
        if (micButtonPrefab == null)
        {
            Debug.LogError("[MicRecordHandler] Mic Button Prefab not assigned.");
            return;
        }

        GameObject canvasObj = new GameObject("MicCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        micButtonInstance = Instantiate(micButtonPrefab, canvas.transform);
        RectTransform rectTransform = micButtonInstance.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0);
        rectTransform.anchorMax = new Vector2(0.5f, 0);
        rectTransform.anchoredPosition = new Vector2(0, 100); // Bottom center

        EventTrigger trigger = micButtonInstance.AddComponent<EventTrigger>();

        // Pointer Down
        EventTrigger.Entry pointerDown = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerDown
        };
        pointerDown.callback.AddListener((data) => { audioSender.StartRecording(); });
        trigger.triggers.Add(pointerDown);

        // Pointer Up
        EventTrigger.Entry pointerUp = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerUp
        };
        pointerUp.callback.AddListener((data) => { audioSender.StopRecording(); });
        trigger.triggers.Add(pointerUp);

        Debug.Log("[MicRecordHandler] Mic button initialized and ready.");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        audioSender.StartRecording();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        audioSender.StopRecording();
    }
}
