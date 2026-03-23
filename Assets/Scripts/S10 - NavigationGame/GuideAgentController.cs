using UnityEngine;
using UnityEngine.AI;

public class GuideAgentController : MonoBehaviour
{
    [Header("References")]
    public NavigationPathManager pathManager;
    public Transform player;

    [Header("Behavior Settings")]
    public float maxDistance = 3f;
    public float rotationSpeed = 2f;
    public float turnThreshold = 10f;

    private NavMeshAgent agent;
    private Animator animator;

    private Vector3[] pathPoints;
    private int currentIndex = 0;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false; // 🔥 we control rotation manually

        animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogError("Animator NOT found in children!");
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj == null)
        {
            Debug.LogError("No object with tag 'Player' found!");
            return;
        }

        player = playerObj.transform;

        if (pathManager == null)
        {
            Debug.LogError("PathManager is NULL!");
            return;
        }

        pathPoints = pathManager.GetPathCorners();

        if (pathPoints == null || pathPoints.Length == 0)
        {
            Debug.LogError("Path is empty!");
            return;
        }

        MoveToNextPoint();
    }

    void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // 🔴 WAIT if player is too far
        if (distanceToPlayer > maxDistance)
        {
            agent.isStopped = true;

            // 👀 Look at player
            Vector3 direction = player.position - transform.position;
            direction.y = 0;

            float angle = Vector3.Angle(transform.forward, direction);

            if (direction != Vector3.zero && angle > turnThreshold)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    Time.deltaTime * rotationSpeed
                );
            }
        }
        else
        {
            // ✅ IMPORTANT FIX: resume movement
            if (agent.isStopped)
            {
                agent.isStopped = false;

                if (currentIndex < pathPoints.Length)
                {
                    agent.SetDestination(pathPoints[currentIndex]);
                }
            }

            // 🚶 Rotate toward path
            Vector3 direction = agent.steeringTarget - transform.position;
            direction.y = 0;

            if (direction.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    Time.deltaTime * rotationSpeed
                );
            }
        }

        // ➡️ Move along path
        if (!agent.pathPending && agent.remainingDistance < 0.2f)
        {
            currentIndex++;

            if (currentIndex < pathPoints.Length)
            {
                MoveToNextPoint();
            }
        }

        // 🎭 ANIMATION CONTROL
        if (animator != null)
        {
            float speed = agent.velocity.magnitude;

            if (agent.hasPath && !agent.isStopped)
            {
                speed = 1f;
            }

            animator.SetFloat("Speed", speed);
        }
    }

    void MoveToNextPoint()
    {
        agent.SetDestination(pathPoints[currentIndex]);
    }
}