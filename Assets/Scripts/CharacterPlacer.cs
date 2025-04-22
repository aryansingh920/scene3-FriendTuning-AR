using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.Animations;
using UnityEngine.Playables;
using System.Collections.Generic;

[RequireComponent(typeof(ARRaycastManager))]
public class CharacterPlacer : MonoBehaviour
{
    [SerializeField] private string characterPath = "Characters/boy";
    [SerializeField] private string animationPath = "Animations/StandingIdle"; // without .fbx



    private ARRaycastManager raycastManager;
    private ARPlaneManager planeManager;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private GameObject placedCharacter;
    private Camera mainCamera;
    private PlayableGraph playableGraph;

    void Start()
    {
        raycastManager = GetComponent<ARRaycastManager>();
        planeManager = FindFirstObjectByType<ARPlaneManager>();
        mainCamera = Camera.main;

        GameObject prefab = Resources.Load<GameObject>(characterPath);
        if (prefab == null)
        {
            Debug.LogError($"[CharacterPlacer] Could not load prefab at Resources/{characterPath}");
            enabled = false;
            return;
        }

        characterPrefab = prefab;

        AnimationClip idleClip = Resources.Load<AnimationClip>(animationPath);
        if (idleClip == null)
        {
            Debug.LogError($"[CharacterPlacer] Could not load AnimationClip at Resources/{animationPath}");
            enabled = false;
            return;
        }

        standingIdleClip = idleClip;
    }

    private GameObject characterPrefab;
    private AnimationClip standingIdleClip;

    void Update()
    {
        if (Input.touchCount == 0) return;
        var touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Began) return;

        if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
        {
            var hit = hits[0];
            var pose = hit.pose;

            var plane = planeManager.GetPlane(hit.trackableId);
            if (plane == null || !plane.gameObject.activeSelf) return;

            if (plane.alignment == PlaneAlignment.HorizontalUp)
                pose.position += Vector3.up * 0.05f;

            if (placedCharacter == null)
            {
                placedCharacter = Instantiate(characterPrefab, pose.position, Quaternion.identity);
                placedCharacter.name = "ARCharacter";
                placedCharacter.transform.localScale = Vector3.one * 0.75f;

                // Play animation using Playables API
                var animator = placedCharacter.GetComponent<Animator>();
                if (animator == null) animator = placedCharacter.AddComponent<Animator>();

                playableGraph = PlayableGraph.Create("IdleAnimationGraph");
                var playableOutput = AnimationPlayableOutput.Create(playableGraph, "Animation", animator);

                var clipPlayable = AnimationClipPlayable.Create(playableGraph, standingIdleClip);
                clipPlayable.SetApplyFootIK(false);
                clipPlayable.SetDuration(standingIdleClip.length);
                clipPlayable.SetTime(0);
                clipPlayable.SetSpeed(1);

                playableOutput.SetSourcePlayable(clipPlayable);
                playableGraph.Play();
            }
            else
            {
                placedCharacter.transform.position = pose.position;
            }

            Vector3 directionToCamera = mainCamera.transform.position - placedCharacter.transform.position;
            directionToCamera.y = 0;
            placedCharacter.transform.rotation = Quaternion.LookRotation(directionToCamera.normalized);

            Debug.Log($"[CharacterPlacer] Placed/moved ARCharacter at {pose.position}.");
        }
    }

    public void PlayTalkingAnimation()
    {
        if (placedCharacter == null || standingIdleClip == null) return;

        AnimationClip talkingClip = Resources.Load<AnimationClip>("Animations/Talking"); // Put your talking animation here
        if (talkingClip == null)
        {
            Debug.LogError("[CharacterPlacer] Talking animation not found at Resources/Animations/Talking");
            return;
        }

        if (playableGraph.IsValid()) playableGraph.Destroy();

        var animator = placedCharacter.GetComponent<Animator>();
        if (animator == null) animator = placedCharacter.AddComponent<Animator>();

        playableGraph = PlayableGraph.Create("TalkingAnimationGraph");
        var output = AnimationPlayableOutput.Create(playableGraph, "Animation", animator);
        var clipPlayable = AnimationClipPlayable.Create(playableGraph, talkingClip);
        output.SetSourcePlayable(clipPlayable);
        playableGraph.Play();
    }

    public void PlayIdleAnimation()
    {
        if (placedCharacter == null || standingIdleClip == null) return;

        if (playableGraph.IsValid()) playableGraph.Destroy();

        var animator = placedCharacter.GetComponent<Animator>();
        if (animator == null) animator = placedCharacter.AddComponent<Animator>();

        playableGraph = PlayableGraph.Create("IdleAnimationGraph");
        var output = AnimationPlayableOutput.Create(playableGraph, "Animation", animator);
        var clipPlayable = AnimationClipPlayable.Create(playableGraph, standingIdleClip);
        output.SetSourcePlayable(clipPlayable);
        playableGraph.Play();
    }


    private void OnDestroy()
    {
        if (playableGraph.IsValid())
            playableGraph.Destroy();
    }
}
