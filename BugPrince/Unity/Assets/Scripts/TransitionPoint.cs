using UnityEngine;
using UnityEngine.Audio;

public class TransitionPoint : MonoBehaviour
{
    [Header("Door Type Gate Settings")]
    [Space(5f)]
    public bool isADoor;

    public bool dontWalkOutOfDoor;

    [Header("Gate Entry")]
    [Tooltip("The wait time before entering from this gate (not the target gate).")]
    public float entryDelay;

    public Vector2 entryOffset;

    public bool alwaysEnterRight;

    public bool alwaysEnterLeft;

    [Header("Force Hard Land (Top Gates Only)")]
    [Space(5f)]
    public bool hardLandOnExit;

    [Header("Destination Scene")]
    [Space(5f)]
    public string targetScene;

    public string entryPoint;

    [SerializeField]
    private bool alwaysUnloadUnusedAssets;

    [Header("Hazard Respawn")]
    [Space(5f)]
    public bool nonHazardGate;

    public HazardRespawnMarker respawnMarker;

    [Header("Set Audio Snapshots")]
    [Space(5f)]
    public AudioMixerSnapshot atmosSnapshot;

    public AudioMixerSnapshot enviroSnapshot;

    public AudioMixerSnapshot actorSnapshot;

    public AudioMixerSnapshot musicSnapshot;

    private Color myGreen = new Color(0f, 0.8f, 0f, 0.5f);

    public enum SceneLoadVisualizations
    {
        Default = 0,
        Custom = -1,
        Dream = 1,
        Colosseum = 2,
        GrimmDream = 3,
        ContinueFromSave = 4,
        GodsAndGlory = 5
    }
    [Header("Cosmetics")]
    public SceneLoadVisualizations sceneLoadVisualization;

    public bool customFade;

    public bool forceWaitFetch;
}
