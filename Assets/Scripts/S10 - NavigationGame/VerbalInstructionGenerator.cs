using UnityEngine;
using UnityEngine.AI;

public class VerbalInstructionGenerator : MonoBehaviour
{
    [Header("References")]
    public NavigationPathManager pathManager;
    public Transform player;
    public Transform playerCamera;
    public AudioSource audioSource;

    [Header("Audio Clips")]
    public AudioClip goForward;
    public AudioClip turnLeft;
    public AudioClip turnRight;
    public AudioClip turnAround;
    public AudioClip keepGoingForward;
    public AudioClip wrongWay;
    public AudioClip destinationReached;

    [Header("Settings")]
    public float triggerDistance = 1.5f;
    public float offPathTimeLimit = 2f;
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

    // 🔥 NEW (important)
    private float lastDistanceToTarget = Mathf.Infinity;
    
    private bool hasReachedDestination = false;

    void Start()
    {
        Debug.Log("[VOICE] System initializing...");

        // Player
        if (player == null)
        {
            GameObject obj = GameObject.FindGameObjectWithTag("Player");
            if (obj != null) player = obj.transform;
        }

        // Camera
        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.transform;

        // Audio
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Path
        if (pathManager == null)
            pathManager = FindObjectOfType<NavigationPathManager>();

        if (pathManager != null)
        {
            // Get initial path corners, F.e.: start, turn turn destination
            pathCorners = pathManager.GetPathCorners();
            Debug.Log("[VOICE] Path corners: " + pathCorners.Length);
        }
    }

    void Update()
    {
        if (player == null || pathCorners == null || pathCorners.Length < 2) return;

        //checks if we have reached the destination (last corner) and triggers the destination reached instruction only once
        if (currentIndex >= pathCorners.Length - 1)
        {
            if (!hasReachedDestination)
            {
                hasReachedDestination = true;

                Speak(destinationReached, "Destination reached");

                Debug.Log("[VOICE] DESTINATION TRIGGERED ONCE");
            }

            return;
        }
        //how far is player from the target destination (next corner)
        float distance = Vector3.Distance(player.position, pathCorners[currentIndex]);
        //what direction should the player go to reach the next corner (forward, left, right, back)
        string dir = GetDirection(pathCorners[currentIndex]);

        // ✅ BEHAVIOR-BASED WRONG WAY DETECTION
        //is player getting farther from the target? (with a small threshold to avoid noise)
        if (distance > lastDistanceToTarget + 0.2f)
        {
            //if player is getting farther, start counting how long they have been going the wrong way
            offPathTimer += Time.deltaTime;

            if (!isOffPath && offPathTimer > offPathTimeLimit)
            {
                isOffPath = true;

                Speak(wrongWay, "Wrong way");
                SpeakDirection(dir);

                RecalculatePath();
            }
        }
        else
        {
            offPathTimer = 0f;
            isOffPath = false;
        }

        lastDistanceToTarget = distance;

        // 🎯 CORNER
        // eger oyuncu koseye vardiysa
        /*** 1. Oyuncu → 1.8m → corner 
        triggerDistance = 1.5
        henuz gecmedi

        Oyuncu → 1.4m → corner
        triggerDistance = 1.5
        gecti
        sistem tekilendi
    
    currentIndex++;
    SpeakDirection(...)
            ***/

            //triggerDistance = 1 - 2 idealdir, = 3 erken tetiklenir, = 0.2 tam ustune basmadan tetiklenmez ve gecikmeli olur

        if (distance < triggerDistance)
        {
            //yon soyle
            SpeakDirection(GetDirection(pathCorners[currentIndex + 1]));
            //sonraki hedefe gectik, indexi arttir
            currentIndex++;
            lastInstructionTime = Time.time;
            return;
        }

        // 🟢 SMART FORWARD (ONLY LONG DISTANCE, sadece uzun koridorda konus)
        if (dir == "forward"
            && distance > minForwardDistance
            && Time.time - lastForwardTime > forwardCooldown)
        {
            Speak(keepGoingForward, "Keep going forward");
            lastForwardTime = Time.time;
            return;
        }

        // 🔁 REPEAT (if player not undestand the message)
        if (Time.time - lastInstructionTime > repeatDelay)
        {
            SpeakDirection(dir);
            lastInstructionTime = Time.time;
        }
    }

    // 🧭 RELATIVE DIRECTION (CAMERA BASED)
    string GetDirection(Vector3 target)
    {
        //gidilmesi gereken yon
        Vector3 toTarget = (target - player.position).normalized;
        //oyuncunun baktigi yon
        Vector3 forward = playerCamera != null ? playerCamera.forward : player.forward;

        forward.y = 0;
        toTarget.y = 0;
        //aci hesaplama (signed angle) (0 = forward, positive = right, negative = left, +- 180 = back)
        float angle = Vector3.SignedAngle(forward, toTarget, Vector3.up);

        if (angle > 150f || angle < -150f) return "back";
        if (angle > 30f) return "right";
        if (angle < -30f) return "left";
        return "forward";
    }

    void SpeakDirection(string dir)
    {
        if (dir == "left") Speak(turnLeft, "Turn left");
        else if (dir == "right") Speak(turnRight, "Turn right");
        else if (dir == "back") Speak(turnAround, "Turn around");
        else Speak(goForward, "Go forward");
    }

    void Speak(AudioClip clip, string text)
    {
        //debug yaz
        Debug.Log("[VOICE] " + text);

        if (audioSource == null || clip == null) return;
        //ve sesi cal
        audioSource.Stop();
        audioSource.PlayOneShot(clip);
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

        Debug.Log("[VOICE] Path recalculated");
    }
}