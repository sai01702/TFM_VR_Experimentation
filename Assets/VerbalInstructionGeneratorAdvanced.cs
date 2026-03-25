using UnityEngine;
using UnityEngine.AI;

public class VerbalInstructionGeneratorAdvanced : MonoBehaviour
{
    [Header("References")]
    public NavigationPathManager pathManager;
    public Transform player;
    public Transform playerCamera;

    [Header("Audio")]
    public AudioSource audioSource;

    [Header("Clips")]
    public AudioClip goForward;
    public AudioClip turnLeft;
    public AudioClip turnRight;
    public AudioClip turnAround;

    public AudioClip in1meter;
    public AudioClip in2meters;
    public AudioClip in3meters;

    public AudioClip keepGoingForward;
    public AudioClip wrongWay;
    public AudioClip destinationReached;

    [Header("Settings")]
    public float triggerDistance = 1.5f;
    public float offPathThreshold = 2.5f;
    public float offPathTimeLimit = 3f;
    public float repeatDelay = 5f;

    [Header("Forward Control")]
    public float minForwardDistance = 4f;
    public float forwardCooldown = 5f;

    private float lastForwardTime = -10f;

    private Vector3[] pathCorners;
    private int currentIndex = 1;

    private float lastInstructionTime = 0f;

    private float offPathTimer = 0f;
    private bool isOffPath = false;

    private bool hasSpokenDistance = false;

    void Start()
    {
        Debug.Log("[VOICE] System initializing...");

        // 🔍 Player
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

            if (playerObj != null)
                player = playerObj.transform;
            else
                Debug.LogError("[VOICE] Player NOT found!");
        }

        // 🎥 Camera
        if (playerCamera == null)
        {
            Camera cam = Camera.main;

            if (cam != null)
                playerCamera = cam.transform;
            else
                Debug.LogError("[VOICE] Camera NOT found!");
        }

        // 🔊 Audio
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.spatialBlend = 0f;
            audioSource.playOnAwake = false;
        }

        // 🧭 Path
        if (pathManager == null)
            pathManager = FindObjectOfType<NavigationPathManager>();

        if (pathManager != null)
        {
            pathCorners = pathManager.GetPathCorners();
            Debug.Log("[VOICE] Path corners: " + pathCorners.Length);
        }
        else
        {
            Debug.LogError("[VOICE] PathManager NOT found!");
        }
    }

    void Update()
    {
        if (player == null || pathCorners == null || pathCorners.Length < 2) return;

        if (currentIndex >= pathCorners.Length - 1)
        {
            Speak(destinationReached, "Destination reached");
            return;
        }

        float distance = Vector3.Distance(player.position, pathCorners[currentIndex]);
        string dir = GetRelativeDirection(pathCorners[currentIndex]);

        // 🚨 OFF PATH
        float pathDistance = GetDistanceFromPath();

        if (pathDistance > offPathThreshold)
        {
            offPathTimer += Time.deltaTime;

            if (!isOffPath && offPathTimer > offPathTimeLimit)
            {
                isOffPath = true;

                Speak(wrongWay, "Wrong way");
                SpeakDirection(dir);

                RecalculatePath();
            }

            return;
        }
        else
        {
            offPathTimer = 0f;
            isOffPath = false;
        }

        // 🎯 CORNER
        if (distance < triggerDistance)
        {
            SpeakDirection(GetRelativeDirection(pathCorners[currentIndex + 1]));

            currentIndex++;
            hasSpokenDistance = false;
            lastInstructionTime = Time.time;
            return;
        }

        // 📏 DISTANCE + DIRECTION (combined)
        if (distance < 3f && !hasSpokenDistance)
        {
            SpeakCombined(distance, dir);
            hasSpokenDistance = true;
            return;
        }

        // 🔁 SMART FORWARD (ANTI-SPAM)
        if (dir == "forward"
            && distance > minForwardDistance
            && Time.time - lastForwardTime > forwardCooldown)
        {
            Speak(keepGoingForward, "Keep going forward");
            lastForwardTime = Time.time;
            return;
        }

        // 🔁 REPEAT
        if (Time.time - lastInstructionTime > repeatDelay)
        {
            SpeakDirection(dir);
            lastInstructionTime = Time.time;
        }
    }

    // 🧭 DIRECTION
    string GetRelativeDirection(Vector3 target)
    {
        Vector3 toTarget = (target - player.position).normalized;

        Vector3 forward = playerCamera != null ? playerCamera.forward : player.forward;

        forward.y = 0;
        toTarget.y = 0;

        float angle = Vector3.SignedAngle(forward, toTarget, Vector3.up);

        if (angle > 150f || angle < -150f) return "back";
        if (angle > 30f) return "right";
        if (angle < -30f) return "left";

        return "forward";
    }

    void SpeakDirection(string dir)
    {
        switch (dir)
        {
            case "forward":
                Speak(goForward, "Go forward");
                break;
            case "left":
                Speak(turnLeft, "Turn left");
                break;
            case "right":
                Speak(turnRight, "Turn right");
                break;
            case "back":
                Speak(turnAround, "Turn around");
                break;
        }
    }

    void SpeakCombined(float distance, string dir)
    {
        int d = Mathf.RoundToInt(distance);

        string sentence = d <= 1 ? "In 1 meter, " :
                          d == 2 ? "In 2 meters, " :
                                   "In 3 meters, ";

        switch (dir)
        {
            case "left": sentence += "turn left"; break;
            case "right": sentence += "turn right"; break;
            case "back": sentence += "turn around"; break;
            default: sentence += "go forward"; break;
        }

        Speak(GetClipForDirection(dir), sentence);
    }

    AudioClip GetClipForDirection(string dir)
    {
        switch (dir)
        {
            case "left": return turnLeft;
            case "right": return turnRight;
            case "back": return turnAround;
            default: return goForward;
        }
    }

    void Speak(AudioClip clip, string debugText)
    {
        Debug.Log("[VOICE] " + debugText);

        if (audioSource == null || clip == null) return;

        audioSource.Stop();
        audioSource.PlayOneShot(clip);
    }

    float GetDistanceFromPath()
    {
        float min = Mathf.Infinity;

        foreach (var p in pathCorners)
        {
            float d = Vector3.Distance(player.position, p);
            if (d < min) min = d;
        }

        return min;
    }

    void RecalculatePath()
    {
        NavMeshPath newPath = new NavMeshPath();

        NavMesh.CalculatePath(
            player.position,
            pathCorners[pathCorners.Length - 1],
            NavMesh.AllAreas,
            newPath
        );

        pathCorners = newPath.corners;
        currentIndex = 1;

        lastInstructionTime = Time.time;

        Debug.Log("[VOICE] Path recalculated");
    }
}