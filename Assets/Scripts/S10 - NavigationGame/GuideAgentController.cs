using UnityEngine;
using UnityEngine.AI;

public class GuideAgentController : MonoBehaviour
{
    public NavigationPathManager pathManager;
    public Transform player;

    public float maxDistance = 3f;

    private NavMeshAgent agent;
    private Vector3[] pathPoints;
    private int currentIndex = 0;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // 🔥 Auto-find player
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
            return;
        }
        else
        {
            agent.isStopped = false;
        }

        // Continue path
        if (!agent.pathPending && agent.remainingDistance < 0.2f)
        {
            currentIndex++;

            if (currentIndex < pathPoints.Length)
            {
                MoveToNextPoint();
            }
        }
    }

    void MoveToNextPoint()
    {
        agent.SetDestination(pathPoints[currentIndex]);
    }
}