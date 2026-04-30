using System.Collections.Generic;
using System.Text;
using UnityEngine;

/*
 * V4 pipeline role: GUIDE/TTS TEXT GENERATOR.
 *
 * This script receives already-detected DecisionNode data from NavigationDecisionSystem
 * and turns it into human-readable route text in the Console.
 *
 * It does not detect NavMesh intersections and it does not draw debug visuals.
 * Its job is only language generation:
 * - find the correct entry at each decision node,
 * - decide whether the route is straight/left/right,
 * - detect simple split wording such as "the road splits into 2 paths",
 * - print both per-decision debug lines and one full route sentence.
 *
 * Later, a real TTS component can read the same generated sentence instead of
 * only logging it.
 */
public class NavigationGuideTextGenerator : MonoBehaviour
{
    [Header("Guide Logging")]
    public bool logGuideText = true;
    public float guideTurnLookBackDistance = 1.25f;
    public float guideTurnLookAheadDistance = 2.5f;
    public float guideSplitSeparationAngle = 22f;
    public float guideStraightPathAngle = 25f;

    public void LogGuideText(List<DecisionNode> decisionNodes, Vector3[] corners)
    {
        if (corners == null || corners.Length < 2)
            return;

        // Full route text is built alongside per-node debug logs so we can inspect
        // each decision while also seeing the final sentence the user would hear.
        StringBuilder fullGuide = new StringBuilder();
        fullGuide.Append("V4 Guide Route: Start. ");

        if (decisionNodes == null || decisionNodes.Count == 0)
        {
            fullGuide.Append("Follow the route until you reach the destination.");
            Debug.Log(fullGuide.ToString());
            return;
        }

        Debug.Log("========== V4 GUIDE DECISION TEXT ==========");

        for (int i = 0; i < decisionNodes.Count; i++)
        {
            DecisionNode node = decisionNodes[i];
            EntryNode correctEntry;

            if (!TryGetCorrectEntry(node, out correctEntry))
            {
                Debug.LogWarning("V4 Guide Decision " + (i + 1) + ": no correct entry found at " + FormatPosition(node.position));
                continue;
            }

            string turnText = GetGuideInstruction(node, correctEntry, corners);
            float distanceFromStart = DistanceAlongPath(node.position, corners);
            int optionCount = node.entries != null ? node.entries.Count : 0;

            string decisionLog =
                "V4 Guide Decision " + (i + 1) +
                " | distance " + distanceFromStart.ToString("F1") + "m" +
                " | options " + optionCount +
                " | instruction: " + turnText +
                " | node " + FormatPosition(node.position) +
                " | target entry " + FormatPosition(correctEntry.position);

            Debug.Log(decisionLog);

            fullGuide.Append("At decision ");
            fullGuide.Append(i + 1);
            fullGuide.Append(", ");
            fullGuide.Append(turnText.ToLowerInvariant());
            fullGuide.Append(". ");
        }

        fullGuide.Append("Then continue until you reach the destination.");
        Debug.Log(fullGuide.ToString());
        Debug.Log("===========================================");
    }

    bool TryGetCorrectEntry(DecisionNode node, out EntryNode correctEntry)
    {
        correctEntry = null;

        if (node.entries == null)
            return false;

        foreach (var entry in node.entries)
        {
            if (entry.isCorrect)
            {
                correctEntry = entry;
                return true;
            }
        }

        return false;
    }

    string GetGuideInstruction(DecisionNode node, EntryNode correctEntry, Vector3[] corners)
    {
        // Use the upcoming path direction, not only the first yellow square, because
        // some turns happen just after a decision point.
        Vector3 guideDirection = GetGuideOutgoingDirection(node.position, correctEntry.direction, corners);
        string turnText = GetTurnInstruction(node.position, guideDirection, corners);

        int splitPathCount = GetSplitPathCount(node, guideDirection, corners, out string correctPathLabel);

        if (splitPathCount >= 2)
            return "The road splits into " + splitPathCount + " paths; continue with the " + correctPathLabel + " path";

        return turnText;
    }

    Vector3 GetGuideOutgoingDirection(Vector3 nodePosition, Vector3 fallbackDirection, Vector3[] corners)
    {
        float nodeDistance = DistanceAlongPath(nodePosition, corners);
        Vector3 futurePoint = GetPointAtDistanceAlongPath(nodeDistance + guideTurnLookAheadDistance, corners);
        Vector3 pathDirection = FlattenDirection(futurePoint - nodePosition);

        if (pathDirection.sqrMagnitude > Mathf.Epsilon)
            return pathDirection;

        return fallbackDirection;
    }

    int GetSplitPathCount(DecisionNode node, Vector3 guideDirection, Vector3[] corners, out string correctPathLabel)
    {
        correctPathLabel = "straight";

        if (node.entries == null)
            return 0;

        float nodeDistance = DistanceAlongPath(node.position, corners);
        Vector3 previousPoint = GetPointAtDistanceAlongPath(nodeDistance - guideTurnLookBackDistance, corners);
        Vector3 incomingDirection = FlattenDirection(node.position - previousPoint);

        if (incomingDirection.sqrMagnitude <= Mathf.Epsilon)
            incomingDirection = GetPathDirectionAtPoint(node.position, corners);

        // Count visually distinct outgoing path groups by angle. This is intentionally
        // language-focused and may differ from raw entry option count.
        Vector3 correctDirection = FlattenDirection(guideDirection);
        List<float> pathAngles = new List<float>();

        foreach (var entry in node.entries)
        {
            Vector3 direction = FlattenDirection(entry.direction);

            if (direction.sqrMagnitude <= Mathf.Epsilon)
                continue;

            if (Vector3.Dot(direction, -correctDirection) > 0.65f)
                continue;

            AddSplitPathAngle(pathAngles, Vector3.SignedAngle(incomingDirection, direction, Vector3.up));
        }

        correctPathLabel = GetRelativePathLabel(incomingDirection, guideDirection);

        return pathAngles.Count;
    }

    void AddSplitPathAngle(List<float> pathAngles, float angle)
    {
        float separation = Mathf.Max(5f, guideSplitSeparationAngle);

        for (int i = 0; i < pathAngles.Count; i++)
        {
            if (Mathf.Abs(Mathf.DeltaAngle(pathAngles[i], angle)) < separation)
                return;
        }

        pathAngles.Add(angle);
    }

    string GetTurnInstruction(Vector3 nodePosition, Vector3 correctDirection, Vector3[] corners)
    {
        float nodeDistance = DistanceAlongPath(nodePosition, corners);
        Vector3 previousPoint = GetPointAtDistanceAlongPath(nodeDistance - guideTurnLookBackDistance, corners);
        Vector3 incomingDirection = FlattenDirection(nodePosition - previousPoint);
        Vector3 targetDirection = FlattenDirection(correctDirection);

        if (incomingDirection.sqrMagnitude <= Mathf.Epsilon)
            incomingDirection = GetPathDirectionAtPoint(nodePosition, corners);

        float signedAngle = Vector3.SignedAngle(incomingDirection, targetDirection, Vector3.up);
        float absAngle = Mathf.Abs(signedAngle);

        if (absAngle < 25f)
            return "Go straight";

        if (absAngle < 60f)
            return signedAngle > 0f ? "Take the right" : "Take the left";

        if (absAngle < 130f)
            return signedAngle > 0f ? "Turn right" : "Turn left";

        return signedAngle > 0f ? "Turn sharply right" : "Turn sharply left";
    }

    string GetRelativePathLabel(Vector3 incomingDirection, Vector3 targetDirection)
    {
        float signedAngle = Vector3.SignedAngle(FlattenDirection(incomingDirection), FlattenDirection(targetDirection), Vector3.up);
        float absAngle = Mathf.Abs(signedAngle);

        if (absAngle < guideStraightPathAngle)
            return "straight";

        return signedAngle > 0f ? "right" : "left";
    }

    Vector3 GetPathDirectionAtPoint(Vector3 point, Vector3[] corners)
    {
        PathProjection projection = ProjectPointToPath(point, corners);
        Vector3 targetPoint = GetPointAtDistanceAlongPath(projection.distanceAlongPath + guideTurnLookAheadDistance, corners);
        Vector3 direction = targetPoint - point;
        direction.y = 0f;

        if (direction.sqrMagnitude <= Mathf.Epsilon)
            return Vector3.zero;

        return direction.normalized;
    }

    float DistanceAlongPath(Vector3 point, Vector3[] corners)
    {
        return ProjectPointToPath(point, corners).distanceAlongPath;
    }

    PathProjection ProjectPointToPath(Vector3 point, Vector3[] corners)
    {
        float total = 0f;
        float bestDistance = float.MaxValue;
        float bestAlongPath = 0f;
        int bestSegmentIndex = 0;
        Vector3 bestPoint = corners[0];

        for (int i = 0; i < corners.Length - 1; i++)
        {
            Vector3 a = corners[i];
            Vector3 b = corners[i + 1];
            float segLength = Vector3.Distance(a, b);
            float t = GetSegmentT(point, a, b);
            Vector3 projected = Vector3.Lerp(a, b, t);
            float distance = Vector3.Distance(point, projected);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestAlongPath = total + segLength * t;
                bestSegmentIndex = i;
                bestPoint = projected;
            }

            total += segLength;
        }

        return new PathProjection
        {
            segmentIndex = bestSegmentIndex,
            distanceAlongPath = bestAlongPath,
            position = bestPoint,
            distanceToPath = bestDistance
        };
    }

    Vector3 GetPointAtDistanceAlongPath(float targetDistance, Vector3[] corners)
    {
        targetDistance = Mathf.Max(0f, targetDistance);
        float total = 0f;

        for (int i = 0; i < corners.Length - 1; i++)
        {
            Vector3 a = corners[i];
            Vector3 b = corners[i + 1];
            float segLength = Vector3.Distance(a, b);

            if (total + segLength >= targetDistance)
            {
                float t = Mathf.Clamp01((targetDistance - total) / segLength);
                return Vector3.Lerp(a, b, t);
            }

            total += segLength;
        }

        return corners[corners.Length - 1];
    }

    float GetSegmentT(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;

        if (ab.sqrMagnitude <= Mathf.Epsilon)
            return 0f;

        return Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
    }

    Vector3 FlattenDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude <= Mathf.Epsilon)
            return Vector3.zero;

        return direction.normalized;
    }

    string FormatPosition(Vector3 position)
    {
        return "(" + position.x.ToString("F1") + ", " + position.y.ToString("F1") + ", " + position.z.ToString("F1") + ")";
    }
}
