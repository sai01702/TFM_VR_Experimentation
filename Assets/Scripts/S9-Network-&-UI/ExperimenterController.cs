using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class ExperimenterController : NetworkBehaviour
{
    [Header("Camera Settings")]
    public Camera experimenterCamera;
    public float moveSpeed = 10f;
    public float lookSpeed = 2f;
    public float sprintMultiplier = 2f;

    [Header("Follow Settings")]
    public bool isFollowingPlayer = false;
    public Vector3 followOffset = new Vector3(0, 2, -3);

    [Header("UI")]
    public GameObject experimenterUI;
    public Text participantIDText;
    public Text currentSceneText;
    public Toggle followToggle;

    private NetworkedPlayer targetPlayer;
    private float rotationX = 0f;

    void Start()
    {
        if (!isLocalPlayer)
        {
            // Disable camera for non-local experimenters
            experimenterCamera.enabled = false;
            experimenterUI.SetActive(false);
            return;
        }

        // Setup UI
        experimenterUI.SetActive(true);
        followToggle.onValueChanged.AddListener(OnFollowToggleChanged);

        // Find player to observe
        FindTargetPlayer();
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        UpdateUI();

        if (isFollowingPlayer && targetPlayer != null)
        {
            FollowPlayer();
        }
        else
        {
            FreeFlyMovement();
        }
    }

    void FreeFlyMovement()
    {
        // WASD movement
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        float upDown = 0f;

        if (Input.GetKey(KeyCode.E)) upDown = 1f;
        if (Input.GetKey(KeyCode.Q)) upDown = -1f;

        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift)) speed *= sprintMultiplier;

        Vector3 move = transform.right * h + transform.forward * v + Vector3.up * upDown;
        transform.position += move * speed * Time.deltaTime;

        // Mouse look
        if (Input.GetMouseButton(1)) // Right-click to look
        {
            float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;

            rotationX -= mouseY;
            rotationX = Mathf.Clamp(rotationX, -90f, 90f);

            transform.localRotation = Quaternion.Euler(rotationX, transform.localEulerAngles.y + mouseX, 0f);
        }
    }

    void FollowPlayer()
    {
        if (targetPlayer == null || targetPlayer.cameraTransform == null) return;

        // Follow behind player camera
        Vector3 targetPos = targetPlayer.cameraTransform.position +
                           targetPlayer.cameraTransform.TransformDirection(followOffset);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 5f);

        // Look at player
        transform.LookAt(targetPlayer.cameraTransform);
    }

    void FindTargetPlayer()
    {
        // Find the host player (connectionId 0)
        var players = FindObjectsOfType<NetworkedPlayer>();
        foreach (var player in players)
        {
            if (player.isLocalPlayer && player.netId != this.netId)
            {
                targetPlayer = player;
                Debug.Log("Found target player to observe");
                break;
            }
        }
    }

    void UpdateUI()
    {
        if (targetPlayer != null)
        {
            participantIDText.text = $"Participant: {targetPlayer.participantID}";
            currentSceneText.text = $"Scene: {targetPlayer.currentScene}";
        }
    }

    void OnFollowToggleChanged(bool value)
    {
        isFollowingPlayer = value;
    }
}