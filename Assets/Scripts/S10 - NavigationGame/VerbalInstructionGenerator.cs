using UnityEngine;
using UnityEngine.AI;

public class VerbalInstructionGenerator : MonoBehaviour
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
    public AudioClip followThePath;
    public AudioClip wrongWay;
    public AudioClip returnToPath;
    public AudioClip destinationReached;

    [Header("Settings")]
    public float triggerDistance = 1.5f;
    public float offPathThreshold = 2.5f;
    public float offPathTimeLimit = 3f;
    public float repeatDelay = 5f;
    public float forwardReminderDelay = 6f;

    private Vector3[] pathCorners;
    private int currentIndex = 1;

    private float lastInstructionTime = 0f;
    private float lastForwardReminderTime = 0f;

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
            {
                player = playerObj.transform;
                Debug.Log("[VOICE] Player found: " + player.name);
            }
            else
            {
                Debug.LogError("[VOICE] Player NOT found!");
            }
        }

        // 🎥 Camera
        if (playerCamera == null)
        {
            Camera cam = Camera.main;

            if (cam != null)
            {
                playerCamera = cam.transform;
                Debug.Log("[VOICE] Camera found: " + playerCamera.name);
            }
            else
            {
                Debug.LogError("[VOICE] Camera NOT found!");
            }
        }

        // 🔊 Audio
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                Debug.Log("[VOICE] AudioSource auto-added");
            }

            audioSource.spatialBlend = 0f;
            audioSource.playOnAwake = false;
        }

        // 🧭 Path
        if (pathManager == null)
        {
            pathManager = FindObjectOfType<NavigationPathManager>();
        }

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

        // 🚨 OFF PATH
        float pathDistance = GetDistanceFromPath();

        if (pathDistance > offPathThreshold)
        {
            offPathTimer += Time.deltaTime;

            if (!isOffPath && offPathTimer > offPathTimeLimit)
            {
                isOffPath = true;

                Speak(wrongWay, "Wrong way");
                SpeakDirection(GetRelativeDirection(pathCorners[currentIndex]));

                RecalculatePath();
            }

            return;
        }
        else
        {
            offPathTimer = 0f;
            isOffPath = false;
        }

        // 📏 DISTANCE
        if (distance < 3f && !hasSpokenDistance)
        {
            SpeakDistance(distance);
            SpeakDirection(GetRelativeDirection(pathCorners[currentIndex]));
            hasSpokenDistance = true;
        }

        // 🎯 CORNER
        if (distance < triggerDistance)
        {
            SpeakDirection(GetRelativeDirection(pathCorners[currentIndex + 1]));

            currentIndex++;
            hasSpokenDistance = false;
            lastInstructionTime = Time.time;
        }

        // 🔁 FORWARD REMINDER
        if (Time.time - lastForwardReminderTime > forwardReminderDelay)
        {
            if (GetRelativeDirection(pathCorners[currentIndex]) == "forward")
            {
                Speak(keepGoingForward, "Keep going forward");
                lastForwardReminderTime = Time.time;
            }
        }

        // 🔁 REPEAT
        if (Time.time - lastInstructionTime > repeatDelay)
        {
            SpeakDirection(GetRelativeDirection(pathCorners[currentIndex]));
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

    void SpeakDistance(float distance)
    {
        int d = Mathf.RoundToInt(distance);

        if (d <= 1) Speak(in1meter, "In 1 meter");
        else if (d == 2) Speak(in2meters, "In 2 meters");
        else Speak(in3meters, "In 3 meters");
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