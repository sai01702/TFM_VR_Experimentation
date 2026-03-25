using UnityEngine;

public class HeadLookController : MonoBehaviour
{
    public float lookSpeed = 5f;

    private Transform head;
    private Transform player;

    void Start()
    {
        Animator animator = GetComponent<Animator>();
        head = animator.GetBoneTransform(HumanBodyBones.Head);

        if (head == null)
        {
            Debug.LogError("Head bone not found!");
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    void LateUpdate()
    {
        if (head == null || player == null) return;

        Vector3 direction = player.position - head.position;
        direction.y = 0;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        head.rotation = Quaternion.Slerp(
            head.rotation,
            targetRotation,
            Time.deltaTime * lookSpeed
        );
    }
}