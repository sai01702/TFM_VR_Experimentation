using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Goal volume for NavigationScene. Supports child colliders / XR hands (parent chain + XR controllers)
/// and forces a second detection pass after the collider is toggled (Unity does not send OnTriggerEnter
/// for objects already overlapping when a trigger is re-enabled).
/// </summary>
public class GoalTrigger : MonoBehaviour
{
    Collider _col;

    void Awake()
    {
        _col = GetComponent<Collider>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!BelongsToPlayer(other))
            return;
        NotifyGoalFromTrigger();
    }

    static bool BelongsToPlayer(Collider other)
    {
        if (other == null)
            return false;
        for (Transform t = other.transform; t != null; t = t.parent)
        {
            if (t.CompareTag("Player"))
                return true;
        }

        return other.GetComponentInParent<XRBaseController>() != null;
    }

    void NotifyGoalFromTrigger()
    {
        if (NavigationSessionController.Instance != null)
        {
            NavigationSessionController.Instance.NotifyGoalReached();
            return;
        }

        if (NavigationGameManager.Instance != null)
            NavigationGameManager.Instance.PlayerReachedGoal();
    }

    /// <summary>Allow a second OnTriggerEnter after round 1 (player may still be inside the volume).</summary>
    public void ResetTriggerForAnotherPass()
    {
        if (_col == null)
            _col = GetComponent<Collider>();
        if (_col == null)
            return;
        _col.enabled = false;
        CancelInvoke(nameof(ReenableCollider));
        Invoke(nameof(ReenableCollider), 0.15f);
    }

    void ReenableCollider()
    {
        if (_col != null)
            _col.enabled = true;
        RefreshOverlapWithPlayer();
    }

    /// <summary>
    /// After re-enabling the trigger or moving the player, Unity may skip OnTriggerEnter if overlaps already exist.
    /// Call this after round-2 setup so a standing overlap still counts.
    /// </summary>
    public void RefreshOverlapWithPlayer()
    {
        if (_col == null || !_col.enabled)
            return;

        var playerRoot = GameObject.FindGameObjectWithTag("Player");
        if (playerRoot == null)
            return;

        var cc = playerRoot.GetComponentInChildren<CharacterController>();
        if (cc != null && cc.enabled && _col.bounds.Intersects(cc.bounds))
        {
            NotifyGoalFromTrigger();
            return;
        }

        foreach (var c in playerRoot.GetComponentsInChildren<Collider>(true))
        {
            if (c == null || !c.enabled)
                continue;
            if (!_col.bounds.Intersects(c.bounds))
                continue;
            NotifyGoalFromTrigger();
            return;
        }
    }
}
