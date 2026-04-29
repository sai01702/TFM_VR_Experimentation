using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/*
 * V4 pipeline role: MAIN CONTROLLER.
 *
 * This is the script that coordinates the current V4 navigation-debug pipeline:
 *
 * 1. Ask NavMeshIntersectionOptions to generate raw decision candidates and directions.
 * 3. Calculate the correct NavMesh path from startPoint to goalPoint.
 * 4. Filter raw intersections into decision nodes on the correct route.
 * 5. Mark the single correct option for each decision node.
 * 6. Analyze/debug option counts, path-beginning points, corridors, and branch counts.
 * 7. Build runtime-only debug visuals: green decision nodes, yellow correct route
 *    triangles, gray walkable-area triangles, corridor markers, etc.
 * 8. Call NavigationGuideTextGenerator to produce the route explanation log.
 *
 * This script owns runtime detection/visualization. It should stay on the
 * V4_HumanVerbalGuide object.
 */
public class NavigationDecisionSystem : MonoBehaviour
{
    [Header("Scene References")]
    public Transform startPoint;
    public Transform goalPoint;
    public NavMeshIntersectionOptions optionsSystem;
    public NavigationGuideTextGenerator guideTextGenerator;

    [Header("Tuning")]
    public float pathThreshold = 3.5f;
    public float testDistance = 2f;
    public float lookAheadDistance = 1.5f;
    public float entryOffset = 1.5f; // 🔥 entry node mesafesi

    public float routeTargetLookAhead = 1.25f;
    public float decisionMergeDistance = 2.0f;
    public float decisionMergePathTurnAngle = 35f;
    public float optionMergeAngle = 18f;
    public float optionContinuationDistance = 2.25f;
    public float optionContinuationStep = 0.75f;
    public float goalIgnoreDistance = 3.0f;
    public float startIgnoreDistance = 1.2f;

    [Header("Runtime Visualization")]
    public bool showRuntimeVisuals = true;
    public bool showIdealPathLines = true;
    public bool showStartGoalMarkers = true;
    public bool showDecisionNodes = true;
    public bool showPathBeginningPoints = true;
    public bool showOffRouteNodes = false;
    public float visualHeightOffset = 0.35f;
    public float startGoalSize = 0.65f;
    public float decisionNodeSize = 0.42f;
    public float optionNodeSize = 0.28f;
    public float optionLineWidth = 0.05f;
    public float correctOptionNodeSizeMultiplier = 1.5f;
    public float correctOptionLineWidthMultiplier = 2.0f;
    public bool showRouteAnchors = true;
    public float routeAnchorSpacing = 1.25f;
    public float routeAnchorSize = 0.35f;
    public bool showBranchAnchors = true;
    public float branchAnchorProbeDistance = 4f;
    public float branchAnchorSpacing = 0.75f;
    public float branchAnchorSize = 0.25f;
    public float branchAnchorMergeAngle = 35f;
    public bool showWalkableAreaAnchors = true;
    public float walkableAreaAnchorSpacing = 1.25f;
    public float walkableAreaAnchorSize = 0.22f;
    public float walkableAreaSampleRadius = 0.45f;
    public float correctPathAnchorDistance = 0.8f;
    public int maxWalkableAreaAnchors = 2500;
    public Color startColor = Color.cyan;
    public Color goalColor = Color.magenta;
    public Color correctDecisionColor = Color.green;
    public Color pathBeginningColor = Color.cyan;
    public Color correctOptionColor = Color.yellow;
    public Color wrongOptionColor = new Color(1f, 0.35f, 0.05f);
    public Color offRouteNodeColor = Color.gray;
    public Color walkableAreaAnchorColor = Color.gray;
    public Color idealPathColor = Color.white;

    [Header("Corridor Detection")]
    public int corridorMinRouteAnchorCount = 15;
    public bool logCorridors = true;
    public bool showCorridorAnchors = true;
    public float corridorAnchorSpacing = 1.25f;
    public float corridorAnchorSize = 0.45f;
    public Color corridorAnchorColor = Color.cyan;

    [Header("Path Beginning Detection")]
    public float pathBeginningScanSpacing = 0.75f;
    public float pathBeginningSideProbeDistance = 2.5f;
    public float pathBeginningForwardProbeDistance = 1.25f;
    public float pathBeginningMergeDistance = 2.0f;
    public float pathBeginningIgnoreNearDecisionDistance = 0.8f;
    public float pathBeginningMarkerSize = 0.5f;
    public bool logPathBeginningPoints = true;

    [Header("NavMesh Branch Analyzer")]
    public bool analyzeNavMeshBranchOptions = true;
    public int navMeshBranchProbeRays = 24;
    public float navMeshBranchProbeDistance = 3.5f;
    public float navMeshBranchProbeStep = 0.7f;
    public float navMeshBranchMergeAngle = 30f;
    public float navMeshBranchBackwardRejectDot = 0.65f;

    private List<DecisionNode> decisionNodes = new List<DecisionNode>();
    public List<PathBeginningPoint> pathBeginningPoints = new List<PathBeginningPoint>();
    public List<CorridorSegment> corridorSegments = new List<CorridorSegment>();
    private List<IntersectionWithOptions> offRouteIntersections = new List<IntersectionWithOptions>();
    private NavMeshPath debugPath;
    private Transform visualRoot;
    private Material lineMaterial;

    IEnumerator Start()
    {
        yield return null;
        yield return null;

        RunDecisionPipeline();
    }

    // =========================
    // MAIN PIPELINE
    // =========================
    void RunDecisionPipeline()
    {
        if (optionsSystem == null)
        {
            Debug.LogError("Options system is not assigned!");
            return;
        }

        optionsSystem.GenerateOptions();
        GenerateDecisions();

        BuildRuntimeVisuals();
    }

    public void RefreshRuntimeVisuals()
    {
        BuildRuntimeVisuals();
    }

    public void ClearAllRuntimeVisuals()
    {
        ClearRuntimeVisuals();
    }

    public void SetAllRuntimeVisuals(bool enabled)
    {
        showRuntimeVisuals = enabled;
        showIdealPathLines = enabled;
        showStartGoalMarkers = enabled;
        showDecisionNodes = enabled;
        showPathBeginningPoints = enabled;
        showOffRouteNodes = enabled;
        showRouteAnchors = enabled;
        showBranchAnchors = enabled;
        showWalkableAreaAnchors = enabled;
        showCorridorAnchors = enabled;
    }

    public void GenerateDecisions()
    {
        decisionNodes.Clear();
        offRouteIntersections.Clear();

        // The actual route to explain. All candidate decision points are compared
        // against this path so we can distinguish on-route and off-route nodes.
        debugPath = new NavMeshPath();
        NavMesh.CalculatePath(startPoint.position, goalPoint.position, NavMesh.AllAreas, debugPath);

        Vector3[] corners = debugPath.corners;

        if (corners.Length < 2)
        {
            Debug.LogError("Path too short!");
            return;
        }

        // Raw intersection/options generated by NavMeshIntersectionOptions.
        var allIntersections = optionsSystem.results;

        List<IntersectionWithOptions> pathIntersections = new List<IntersectionWithOptions>();

        foreach (var i in allIntersections)
        {
            if (IsOnPath(i.position, corners))
                pathIntersections.Add(i);
            else
                offRouteIntersections.Add(i);
        }

        pathIntersections.Sort((a, b) =>
        {
            float da = DistanceAlongPath(a.position, corners);
            float db = DistanceAlongPath(b.position, corners);
            return da.CompareTo(db);
        });

        // pathIntersections = MergeNearbyIntersections(pathIntersections, corners);
        // We intentionally do not merge decision points now. The user wants to see
        // every raw decision candidate that survives filtering.
        pathIntersections = FilterNonDecisionIntersections(pathIntersections, corners);

        for (int i = 0; i < pathIntersections.Count; i++)
        {
            var inter = pathIntersections[i];
            float routeDistance = DistanceAlongPath(inter.position, corners);
            float routeLookAhead = Mathf.Max(0.5f, routeTargetLookAhead);
            Vector3 routeTarget = GetPointAtDistanceAlongPath(routeDistance + routeLookAhead, corners);

            Vector3 correct = GetCorrectOption(inter.position, inter.options, corners, routeTarget);

            DecisionNode node = new DecisionNode
            {
                position = inter.position,
                options = inter.options,
                correctOption = correct,
                entries = new List<EntryNode>()
            };

            // Create one visible entry marker for each available option.
            foreach (var dir in inter.options)
            {
                if (!TryGetEntryPosition(inter.position, dir, out Vector3 entryPos))
                    continue;

                if (!OptionContinuesFromNode(inter.position, dir))
                    continue;

                EntryNode entry = new EntryNode
                {
                    position = entryPos,
                    direction = dir,
                    isCorrect = false
                };

                node.entries.Add(entry);
            }

            MarkSingleCorrectEntry(node, correct);

            if (!HasCorrectEntry(node) && TryGetEntryPosition(inter.position, correct, out Vector3 correctEntryPos) && OptionContinuesFromNode(inter.position, correct))
            {
                node.entries.Add(new EntryNode
                {
                    position = correctEntryPos,
                    direction = correct,
                    isCorrect = true
                });
            }

            decisionNodes.Add(node);
        }

        Debug.Log("Decision Nodes: " + decisionNodes.Count);
        AnalyzeNavMeshBranchOptions(corners);
        DetectPathBeginningPoints(corners);
        DetectCorridors(corners);

        LogGuideText(corners);
    }

    void AnalyzeNavMeshBranchOptions(Vector3[] corners)
    {
        if (!analyzeNavMeshBranchOptions)
            return;

        // Debug-only comparison. This does not modify decision nodes.
        // It asks: "How many branches does the NavMesh itself seem to offer here?"
        // and compares that number with node.entries.Count.
        Debug.Log("========== V4 NAVMESH BRANCH ANALYSIS ==========");

        for (int i = 0; i < decisionNodes.Count; i++)
        {
            DecisionNode node = decisionNodes[i];
            NavMeshBranchAnalysis analysis = AnalyzeNavMeshBranchOptions(node, corners);
            string prefix = analysis.hasMismatch ? "V4 Branch Mismatch " : "V4 Branch Match ";

            Debug.Log(
                prefix + (i + 1) +
                " | entry options " + analysis.entryOptionCount +
                " | navmesh branches " + analysis.branchCount +
                " | node " + FormatPosition(node.position) +
                " | branches " + FormatBranchAngles(analysis.branchAngles)
            );
        }

        Debug.Log("===============================================");
    }

    NavMeshBranchAnalysis AnalyzeNavMeshBranchOptions(DecisionNode node, Vector3[] corners)
    {
        NavMeshBranchAnalysis analysis = new NavMeshBranchAnalysis
        {
            entryOptionCount = node.entries != null ? node.entries.Count : 0,
            branchAngles = new List<float>()
        };

        if (corners == null || corners.Length < 2)
        {
            analysis.branchCount = 0;
            analysis.hasMismatch = analysis.entryOptionCount != analysis.branchCount;
            return analysis;
        }

        Vector3 routeForward = GetPathDirectionAtPoint(node.position, corners);
        int rays = Mathf.Max(8, navMeshBranchProbeRays);

        for (int i = 0; i < rays; i++)
        {
            float angle = 360f * i / rays;
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            direction = FlattenDirection(direction);

            if (routeForward.sqrMagnitude > Mathf.Epsilon && Vector3.Dot(direction, -routeForward) > navMeshBranchBackwardRejectDot)
                continue;

            if (!CanTravelFromNode(node.position, direction, navMeshBranchProbeDistance, navMeshBranchProbeStep))
                continue;

            float relativeAngle = routeForward.sqrMagnitude > Mathf.Epsilon
                ? Vector3.SignedAngle(routeForward, direction, Vector3.up)
                : angle;

            AddBranchAngle(analysis.branchAngles, relativeAngle);
        }

        analysis.branchCount = analysis.branchAngles.Count;
        analysis.hasMismatch = analysis.entryOptionCount != analysis.branchCount;
        return analysis;
    }

    bool CanTravelFromNode(Vector3 nodePosition, Vector3 direction, float probeDistance, float probeStep)
    {
        Vector3 flatDirection = FlattenDirection(direction);

        if (flatDirection.sqrMagnitude <= Mathf.Epsilon)
            return false;

        if (!NavMesh.SamplePosition(nodePosition, out NavMeshHit previousHit, 1.2f, NavMesh.AllAreas))
            return false;

        float step = Mathf.Max(0.25f, probeStep);
        float maxDistance = Mathf.Max(step, probeDistance);
        Vector3 previous = previousHit.position;
        float travelled = 0f;

        for (float distance = step; distance <= maxDistance; distance += step)
        {
            Vector3 candidate = nodePosition + flatDirection * distance;

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit nextHit, 0.75f, NavMesh.AllAreas))
                break;

            if (NavMesh.Raycast(previous, nextHit.position, out NavMeshHit rayHit, NavMesh.AllAreas))
                break;

            travelled = distance;
            previous = nextHit.position;
        }

        return travelled >= maxDistance;
    }

    void AddBranchAngle(List<float> branchAngles, float angle)
    {
        float mergeAngle = Mathf.Max(5f, navMeshBranchMergeAngle);

        for (int i = 0; i < branchAngles.Count; i++)
        {
            if (Mathf.Abs(Mathf.DeltaAngle(branchAngles[i], angle)) < mergeAngle)
                return;
        }

        branchAngles.Add(angle);
    }

    string FormatBranchAngles(List<float> branchAngles)
    {
        if (branchAngles == null || branchAngles.Count == 0)
            return "none";

        List<string> labels = new List<string>();

        foreach (float angle in branchAngles)
            labels.Add(angle.ToString("F0") + "deg");

        return string.Join(", ", labels);
    }

    void DetectPathBeginningPoints(Vector3[] corners)
    {
        pathBeginningPoints.Clear();

        if (corners == null || corners.Length < 2)
            return;

        // Separate from green decision nodes: scan along the correct route and
        // probe left/right to find where side paths begin. These cyan markers are
        // currently for debugging and can later be promoted into guide instructions.
        float totalLength = GetPathLength(corners);
        float spacing = Mathf.Max(0.25f, pathBeginningScanSpacing);

        for (float distance = spacing; distance < totalLength - spacing; distance += spacing)
        {
            Vector3 position = GetPointAtDistanceAlongPath(distance, corners);
            Vector3 forward = GetPathDirectionAtPoint(position, corners);

            if (forward.sqrMagnitude <= Mathf.Epsilon)
                continue;

            TryAddPathBeginningCandidate(position, forward, distance, Vector3.Cross(Vector3.up, forward).normalized, corners);
            TryAddPathBeginningCandidate(position, forward, distance, Vector3.Cross(forward, Vector3.up).normalized, corners);
        }

        if (!logPathBeginningPoints)
            return;

        Debug.Log("V4 Path Beginning Points: " + pathBeginningPoints.Count);

        for (int i = 0; i < pathBeginningPoints.Count; i++)
        {
            PathBeginningPoint point = pathBeginningPoints[i];
            Debug.Log(
                "V4 Path Beginning " + (i + 1) +
                " | side " + point.side +
                " | route distance " + point.distanceAlongPath.ToString("F1") + "m" +
                " | position " + FormatPosition(point.position)
            );
        }
    }

    void TryAddPathBeginningCandidate(Vector3 routePosition, Vector3 routeForward, float routeDistance, Vector3 sideDirection, Vector3[] corners)
    {
        sideDirection = FlattenDirection(sideDirection);

        if (sideDirection.sqrMagnitude <= Mathf.Epsilon)
            return;

        if (!IsSidePathBeginning(routePosition, routeForward, sideDirection, out Vector3 branchPosition))
            return;

        if (IsNearDecisionNode(branchPosition, pathBeginningIgnoreNearDecisionDistance))
            return;

        if (IsNearExistingPathBeginning(branchPosition, pathBeginningMergeDistance))
            return;

        pathBeginningPoints.Add(new PathBeginningPoint
        {
            position = branchPosition,
            direction = sideDirection,
            distanceAlongPath = routeDistance,
            side = GetSideLabel(routeForward, sideDirection)
        });
    }

    bool IsSidePathBeginning(Vector3 routePosition, Vector3 routeForward, Vector3 sideDirection, out Vector3 branchPosition)
    {
        branchPosition = Vector3.zero;

        if (!NavMesh.SamplePosition(routePosition, out NavMeshHit routeHit, 0.75f, NavMesh.AllAreas))
            return false;

        Vector3 sideTarget = routePosition + sideDirection * pathBeginningSideProbeDistance;

        if (!NavMesh.SamplePosition(sideTarget, out NavMeshHit sideHit, 0.9f, NavMesh.AllAreas))
            return false;

        if (NavMesh.Raycast(routeHit.position, sideHit.position, out NavMeshHit sideRayHit, NavMesh.AllAreas))
            return false;

        Vector3 forwardTarget = sideHit.position + routeForward * pathBeginningForwardProbeDistance;
        Vector3 backwardTarget = sideHit.position - routeForward * pathBeginningForwardProbeDistance;
        bool continuesForward = CanReachOnNavMesh(sideHit.position, forwardTarget, 0.75f);
        bool continuesBackward = CanReachOnNavMesh(sideHit.position, backwardTarget, 0.75f);

        if (!continuesForward && !continuesBackward)
            return false;

        branchPosition = sideHit.position;
        return true;
    }

    bool CanReachOnNavMesh(Vector3 from, Vector3 to, float sampleRadius)
    {
        if (!NavMesh.SamplePosition(from, out NavMeshHit fromHit, sampleRadius, NavMesh.AllAreas))
            return false;

        if (!NavMesh.SamplePosition(to, out NavMeshHit toHit, sampleRadius, NavMesh.AllAreas))
            return false;

        return !NavMesh.Raycast(fromHit.position, toHit.position, out NavMeshHit rayHit, NavMesh.AllAreas);
    }

    bool IsNearDecisionNode(Vector3 position, float distance)
    {
        foreach (var decision in decisionNodes)
        {
            if (Vector3.Distance(position, decision.position) <= distance)
                return true;
        }

        return false;
    }

    bool IsNearExistingPathBeginning(Vector3 position, float distance)
    {
        foreach (var point in pathBeginningPoints)
        {
            if (Vector3.Distance(position, point.position) <= distance)
                return true;
        }

        return false;
    }

    string GetSideLabel(Vector3 routeForward, Vector3 sideDirection)
    {
        float angle = Vector3.SignedAngle(FlattenDirection(routeForward), FlattenDirection(sideDirection), Vector3.up);
        return angle > 0f ? "right" : "left";
    }

    void DetectCorridors(Vector3[] corners)
    {
        corridorSegments.Clear();

        if (corners == null || corners.Length < 2)
            return;

        // A corridor is defined as a long stretch between decision nodes with more
        // than corridorMinRouteAnchorCount route triangles/anchors and no decision
        // point inside it.
        float spacing = Mathf.Max(0.25f, routeAnchorSpacing);
        List<float> decisionDistances = new List<float>();
        decisionDistances.Add(0f);

        foreach (var node in decisionNodes)
            decisionDistances.Add(DistanceAlongPath(node.position, corners));

        decisionDistances.Add(GetPathLength(corners));
        decisionDistances.Sort();

        for (int i = 0; i < decisionDistances.Count - 1; i++)
        {
            float startDistance = decisionDistances[i];
            float endDistance = decisionDistances[i + 1];
            float segmentLength = endDistance - startDistance;
            int routeAnchorCount = Mathf.FloorToInt(segmentLength / spacing);

            if (routeAnchorCount <= corridorMinRouteAnchorCount)
                continue;

            CorridorSegment corridor = new CorridorSegment
            {
                startDistance = startDistance,
                endDistance = endDistance,
                routeAnchorCount = routeAnchorCount,
                startPosition = GetPointAtDistanceAlongPath(startDistance, corners),
                endPosition = GetPointAtDistanceAlongPath(endDistance, corners)
            };

            corridorSegments.Add(corridor);
        }

        if (!logCorridors)
            return;

        Debug.Log("V4 Corridors: " + corridorSegments.Count);

        for (int i = 0; i < corridorSegments.Count; i++)
        {
            CorridorSegment corridor = corridorSegments[i];
            Debug.Log(
                "V4 Corridor " + (i + 1) +
                " | route triangles " + corridor.routeAnchorCount +
                " | distance " + (corridor.endDistance - corridor.startDistance).ToString("F1") + "m" +
                " | from " + FormatPosition(corridor.startPosition) +
                " to " + FormatPosition(corridor.endPosition)
            );
        }
    }

    void LogGuideText(Vector3[] corners)
    {
        if (guideTextGenerator == null)
            guideTextGenerator = GetComponent<NavigationGuideTextGenerator>();

        if (guideTextGenerator == null)
            guideTextGenerator = gameObject.AddComponent<NavigationGuideTextGenerator>();

        if (guideTextGenerator == null || !guideTextGenerator.logGuideText)
            return;

        guideTextGenerator.LogGuideText(decisionNodes, corners);
    }

    string FormatPosition(Vector3 position)
    {
        return "(" + position.x.ToString("F1") + ", " + position.y.ToString("F1") + ", " + position.z.ToString("F1") + ")";
    }

    void MarkSingleCorrectEntry(DecisionNode node, Vector3 correctDirection)
    {
        int bestIndex = -1;
        float bestScore = -999f;
        Vector3 flatCorrect = FlattenDirection(correctDirection);

        for (int i = 0; i < node.entries.Count; i++)
        {
            node.entries[i].isCorrect = false;

            float score = Vector3.Dot(FlattenDirection(node.entries[i].direction), flatCorrect);

            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        if (bestIndex >= 0)
        {
            EntryNode entry = node.entries[bestIndex];
            entry.isCorrect = true;
            node.entries[bestIndex] = entry;
        }
    }

    bool HasCorrectEntry(DecisionNode node)
    {
        foreach (var entry in node.entries)
        {
            if (entry.isCorrect)
                return true;
        }

        return false;
    }

    List<IntersectionWithOptions> FilterNonDecisionIntersections(List<IntersectionWithOptions> intersections, Vector3[] corners)
    {
        List<IntersectionWithOptions> filtered = new List<IntersectionWithOptions>();

        foreach (var intersection in intersections)
        {
            if (Vector3.Distance(intersection.position, goalPoint.position) < goalIgnoreDistance)
                continue;

            if (Vector3.Distance(intersection.position, startPoint.position) < startIgnoreDistance)
                continue;

            if (!HasRealChoice(intersection, corners))
                continue;

            filtered.Add(intersection);
        }

        Debug.Log("Filtered Decision Nodes: " + filtered.Count + " from " + intersections.Count);

        return filtered;
    }

    bool HasRealChoice(IntersectionWithOptions intersection, Vector3[] corners)
    {
        int walkableOptions = 0;

        foreach (var option in intersection.options)
        {
            if (TryGetEntryPosition(intersection.position, option, out Vector3 entryPosition) && OptionContinuesFromNode(intersection.position, option))
                walkableOptions++;
        }

        if (walkableOptions < 2)
            return false;

        PathProjection projection = ProjectPointToPath(intersection.position, corners);
        Vector3 routeForward = FlattenDirection(GetPointAtDistanceAlongPath(projection.distanceAlongPath + lookAheadDistance, corners) - intersection.position);
        Vector3 routeBackward = FlattenDirection(intersection.position - GetPointAtDistanceAlongPath(projection.distanceAlongPath - lookAheadDistance, corners));

        bool hasForward = false;
        bool hasSideChoice = false;

        foreach (var option in intersection.options)
        {
            Vector3 flatOption = FlattenDirection(option);

            if (Vector3.Dot(flatOption, routeForward) > 0.55f)
                hasForward = true;

            if (Vector3.Dot(flatOption, routeForward) < 0.55f && Vector3.Dot(flatOption, routeBackward) < 0.55f)
                hasSideChoice = true;
        }

        return hasForward && hasSideChoice;
    }

    List<IntersectionWithOptions> MergeNearbyIntersections(List<IntersectionWithOptions> intersections, Vector3[] corners)
    {
        List<IntersectionWithOptions> merged = new List<IntersectionWithOptions>();

        if (intersections.Count == 0)
            return merged;

        List<IntersectionWithOptions> group = new List<IntersectionWithOptions>();
        float groupStartDistance = DistanceAlongPath(intersections[0].position, corners);
        int groupStartSegment = ProjectPointToPath(intersections[0].position, corners).segmentIndex;

        foreach (var intersection in intersections)
        {
            float distanceAlongPath = DistanceAlongPath(intersection.position, corners);
            int currentSegment = ProjectPointToPath(intersection.position, corners).segmentIndex;
            bool crossesPathTurn = IsSharpPathTurnBetweenSegments(groupStartSegment, currentSegment, corners);

            if (group.Count > 0 && (distanceAlongPath - groupStartDistance > decisionMergeDistance || crossesPathTurn))
            {
                merged.Add(CreateMergedIntersection(group, corners));
                group.Clear();
                groupStartDistance = distanceAlongPath;
                groupStartSegment = currentSegment;
            }

            group.Add(intersection);
        }

        if (group.Count > 0)
            merged.Add(CreateMergedIntersection(group, corners));

        Debug.Log("Merged Decision Nodes: " + merged.Count + " from " + intersections.Count);

        return merged;
    }

    bool IsSharpPathTurnBetweenSegments(int startSegment, int endSegment, Vector3[] corners)
    {
        if (startSegment == endSegment)
            return false;

        int from = Mathf.Min(startSegment, endSegment);
        int to = Mathf.Max(startSegment, endSegment);
        float turnAngle = Mathf.Max(5f, decisionMergePathTurnAngle);

        for (int i = from; i < to; i++)
        {
            Vector3 first = GetSegmentDirection(i, corners);
            Vector3 second = GetSegmentDirection(i + 1, corners);

            if (first.sqrMagnitude <= Mathf.Epsilon || second.sqrMagnitude <= Mathf.Epsilon)
                continue;

            if (Vector3.Angle(first, second) >= turnAngle)
                return true;
        }

        return false;
    }

    Vector3 GetSegmentDirection(int segmentIndex, Vector3[] corners)
    {
        if (corners == null || corners.Length < 2)
            return Vector3.zero;

        segmentIndex = Mathf.Clamp(segmentIndex, 0, corners.Length - 2);
        return FlattenDirection(corners[segmentIndex + 1] - corners[segmentIndex]);
    }

    IntersectionWithOptions CreateMergedIntersection(List<IntersectionWithOptions> group, Vector3[] corners)
    {
        Vector3 averagePosition = Vector3.zero;
        List<Vector3> mergedOptions = new List<Vector3>();

        foreach (var intersection in group)
        {
            averagePosition += intersection.position;

            foreach (var option in intersection.options)
                AddMergedOption(mergedOptions, option);
        }

        averagePosition /= group.Count;

        if (NavMesh.SamplePosition(averagePosition, out NavMeshHit hit, pathThreshold, NavMesh.AllAreas))
            averagePosition = hit.position;

        return new IntersectionWithOptions
        {
            position = averagePosition,
            options = mergedOptions
        };
    }

    void AddMergedOption(List<Vector3> options, Vector3 candidate)
    {
        Vector3 flatCandidate = FlattenDirection(candidate);

        if (flatCandidate.sqrMagnitude <= Mathf.Epsilon)
            return;

        foreach (var option in options)
        {
            if (Vector3.Angle(FlattenDirection(option), flatCandidate) < optionMergeAngle)
                return;
        }

        options.Add(flatCandidate);
    }

    // =========================
    // PATH HELPERS
    // =========================
    bool IsOnPath(Vector3 point, Vector3[] corners)
    {
        for (int i = 0; i < corners.Length - 1; i++)
        {
            if (DistancePointToSegment(point, corners[i], corners[i + 1]) < pathThreshold)
                return true;
        }
        return false;
    }

    float DistancePointToSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ap = p - a;
        Vector3 ab = b - a;

        float t = GetSegmentT(p, a, b);
        return Vector3.Distance(p, a + ab * t);
    }

    float DistanceAlongPath(Vector3 point, Vector3[] corners)
    {
        return ProjectPointToPath(point, corners).distanceAlongPath;
    }

    float GetSegmentT(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;

        if (ab.sqrMagnitude <= Mathf.Epsilon)
            return 0f;

        return Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
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

    // =========================
    // PATH SEGMENT INDEX
    // =========================
    int GetPathSegmentIndex(Vector3 point, Vector3[] corners)
    {
        return ProjectPointToPath(point, corners).segmentIndex;
    }

    // =========================
    // PATH FLOW TARGET
    // =========================
    Vector3 GetTargetPointOnPath(Vector3 current, Vector3[] corners)
    {
        PathProjection projection = ProjectPointToPath(current, corners);
        return GetPointAtDistanceAlongPath(projection.distanceAlongPath + lookAheadDistance, corners);
    }

    Vector3 GetPathDirectionAtPoint(Vector3 point, Vector3[] corners)
    {
        Vector3 targetPoint = GetTargetPointOnPath(point, corners);
        Vector3 direction = targetPoint - point;
        direction.y = 0f;

        if (direction.sqrMagnitude <= Mathf.Epsilon)
            return Vector3.zero;

        return direction.normalized;
    }

    // =========================
    // FINAL DECISION LOGIC
    // =========================
    Vector3 GetCorrectOption(Vector3 currentNode, List<Vector3> options, Vector3[] corners, Vector3 routeTarget)
    {
        if (Vector3.Distance(currentNode, goalPoint.position) < testDistance)
        {
            return (goalPoint.position - currentNode).normalized;
        }

        float bestScore = -999f;
        Vector3 best = Vector3.zero;

        float currentDist = DistanceAlongPath(currentNode, corners);

        Vector3 targetPoint = GetTargetPointOnPath(currentNode, corners);
        Vector3 toTarget = FlattenDirection(targetPoint - currentNode);
        Vector3 toRouteTarget = FlattenDirection(routeTarget - currentNode);

        Vector3 pathDir = GetPathDirectionAtPoint(currentNode, corners);

        foreach (var option in options)
        {
            if (!TryGetEntryPosition(currentNode, option, out Vector3 optionPoint))
                continue;

            if (!OptionContinuesFromNode(currentNode, option))
                continue;

            Vector3 flatOption = FlattenDirection(option);
            float targetAlign = Vector3.Dot(flatOption, toTarget);
            float routeTargetAlign = Vector3.Dot(flatOption, toRouteTarget);
            float pathAlign = Vector3.Dot(flatOption, pathDir);

            if (targetAlign < 0.1f && pathAlign < 0.1f && routeTargetAlign < 0.1f)
                continue;

            float optionDist = DistanceAlongPath(optionPoint, corners);
            float progress = optionDist - currentDist;

            if (progress <= 0f)
                continue;

            float distToPath = DistanceToPath(optionPoint, corners);
            float distToRouteTarget = Vector3.Distance(optionPoint, routeTarget);

            float score =
                progress * 6f +
                routeTargetAlign * 4f +
                targetAlign * 3f +
                pathAlign * 2f +
                (-distToRouteTarget * 8f) +
                (-distToPath * 4f);

            if (score > bestScore)
            {
                bestScore = score;
                best = option;
            }
        }

        if (best == Vector3.zero && options.Count > 0)
        {
            best = GetClosestEntryOption(options, currentNode, routeTarget);
            Debug.LogWarning("Fallback used at node: " + currentNode);
        }

        return best;
    }

    Vector3 FlattenDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude <= Mathf.Epsilon)
            return Vector3.zero;

        return direction.normalized;
    }

    Vector3 GetMostAlignedOption(List<Vector3> options, Vector3 targetDirection)
    {
        float bestDot = -999f;
        Vector3 best = Vector3.zero;

        foreach (var option in options)
        {
            float dot = Vector3.Dot(FlattenDirection(option), FlattenDirection(targetDirection));

            if (dot > bestDot)
            {
                bestDot = dot;
                best = option;
            }
        }

        return best;
    }

    Vector3 GetClosestEntryOption(List<Vector3> options, Vector3 nodePosition, Vector3 routeTarget)
    {
        float bestDistance = float.MaxValue;
        Vector3 best = Vector3.zero;

        foreach (var option in options)
        {
            if (!TryGetEntryPosition(nodePosition, option, out Vector3 entryPosition))
                continue;

            if (!OptionContinuesFromNode(nodePosition, option))
                continue;

            float distance = Vector3.Distance(entryPosition, routeTarget);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = option;
            }
        }

        if (best == Vector3.zero)
            best = GetMostAlignedOption(options, FlattenDirection(routeTarget - nodePosition));

        return best;
    }

    // =========================
    // NAVMESH CHECK
    // =========================
    bool IsWalkable(Vector3 from, Vector3 to)
    {
        NavMeshHit hit;

        if (!NavMesh.SamplePosition(to, out hit, 1f, NavMesh.AllAreas))
            return false;

        if (NavMesh.Raycast(from, to, out hit, NavMesh.AllAreas))
            return false;

        return true;
    }

    bool TryGetEntryPosition(Vector3 nodePosition, Vector3 direction, out Vector3 entryPosition)
    {
        entryPosition = Vector3.zero;

        Vector3 flatDirection = FlattenDirection(direction);

        if (flatDirection.sqrMagnitude <= Mathf.Epsilon)
            return false;

        if (!NavMesh.SamplePosition(nodePosition, out NavMeshHit nodeHit, 1.2f, NavMesh.AllAreas))
            return false;

        float[] distances =
        {
            entryOffset,
            entryOffset * 0.75f,
            entryOffset * 0.5f,
            entryOffset * 0.35f
        };

        foreach (float distance in distances)
        {
            Vector3 candidate = nodePosition + flatDirection * distance;

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit entryHit, 1.0f, NavMesh.AllAreas))
                continue;

            if (NavMesh.Raycast(nodeHit.position, entryHit.position, out NavMeshHit rayHit, NavMesh.AllAreas))
                continue;

            entryPosition = entryHit.position;
            return true;
        }

        return false;
    }

    bool OptionContinuesFromNode(Vector3 nodePosition, Vector3 direction)
    {
        Vector3 flatDirection = FlattenDirection(direction);

        if (flatDirection.sqrMagnitude <= Mathf.Epsilon)
            return false;

        if (!NavMesh.SamplePosition(nodePosition, out NavMeshHit previousHit, 1.2f, NavMesh.AllAreas))
            return false;

        float step = Mathf.Max(0.25f, optionContinuationStep);
        float maxDistance = Mathf.Max(step, optionContinuationDistance);
        Vector3 previous = previousHit.position;
        float travelled = 0f;

        for (float distance = step; distance <= maxDistance; distance += step)
        {
            Vector3 candidate = nodePosition + flatDirection * distance;

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit nextHit, 0.75f, NavMesh.AllAreas))
                break;

            if (NavMesh.Raycast(previous, nextHit.position, out NavMeshHit rayHit, NavMesh.AllAreas))
                break;

            travelled = distance;
            previous = nextHit.position;
        }

        return travelled >= maxDistance;
    }

    float DistanceToPath(Vector3 point, Vector3[] corners)
    {
        float minDist = float.MaxValue;

        for (int i = 0; i < corners.Length - 1; i++)
        {
            float d = DistancePointToSegment(point, corners[i], corners[i + 1]);
            if (d < minDist)
                minDist = d;
        }

        return minDist;
    }

    void BuildRuntimeVisuals()
    {
        ClearRuntimeVisuals();

        if (!showRuntimeVisuals)
            return;

        visualRoot = new GameObject("V4 Decision Debug Visuals").transform;
        visualRoot.SetParent(transform, false);

        if (debugPath != null && debugPath.corners.Length > 1)
        {
            if (showIdealPathLines)
            {
                for (int i = 0; i < debugPath.corners.Length - 1; i++)
                {
                    CreateLine(
                        "IdealPath_" + i,
                        Lift(debugPath.corners[i]),
                        Lift(debugPath.corners[i + 1]),
                        idealPathColor,
                        optionLineWidth
                    );
                }
            }

            if (showRouteAnchors)
                CreateRouteAnchorMarkers(debugPath.corners);

            if (showWalkableAreaAnchors)
                CreateWalkableAreaAnchorMarkers(debugPath.corners);

            if (showCorridorAnchors)
                CreateCorridorAnchorMarkers(debugPath.corners);
        }

        if (showStartGoalMarkers)
        {
            if (startPoint != null)
                CreateMarker("StartPoint", Lift(startPoint.position), startGoalSize, startColor, PrimitiveType.Sphere);

            if (goalPoint != null)
                CreateMarker("GoalPoint", Lift(goalPoint.position), startGoalSize, goalColor, PrimitiveType.Sphere);
        }

        if (showOffRouteNodes)
        {
            foreach (var intersection in offRouteIntersections)
            {
                Vector3 nodePosition = Lift(intersection.position);
                CreateMarker("OffRouteDecisionNode", nodePosition, decisionNodeSize, offRouteNodeColor, PrimitiveType.Cube);

                foreach (var option in intersection.options)
                {
                    Vector3 optionPosition = Lift(intersection.position + option * entryOffset);
                    CreateMarker("OffRouteOption", optionPosition, optionNodeSize, offRouteNodeColor, PrimitiveType.Cube);
                    CreateLine("OffRouteOptionLine", nodePosition, optionPosition, offRouteNodeColor, optionLineWidth);
                }
            }
        }

        if (showDecisionNodes)
        {
            foreach (var decision in decisionNodes)
            {
                Vector3 decisionPosition = Lift(decision.position);
                CreateMarker("CorrectRouteDecisionNode", decisionPosition, decisionNodeSize, correctDecisionColor, PrimitiveType.Sphere);

                foreach (var entry in decision.entries)
                {
                    Color color = entry.isCorrect ? correctOptionColor : wrongOptionColor;
                    Vector3 entryPosition = Lift(entry.position);
                    float markerSize = entry.isCorrect ? optionNodeSize * correctOptionNodeSizeMultiplier : optionNodeSize;
                    float lineWidth = entry.isCorrect ? optionLineWidth * correctOptionLineWidthMultiplier : optionLineWidth;

                    CreateMarker(entry.isCorrect ? "CorrectOption" : "WrongOption", entryPosition, markerSize, color, PrimitiveType.Cube);
                    CreateLine(entry.isCorrect ? "CorrectOptionLine" : "WrongOptionLine", decisionPosition, entryPosition, color, lineWidth);
                }
            }
        }

        if (showPathBeginningPoints)
        {
            foreach (var point in pathBeginningPoints)
            {
                Vector3 pointPosition = Lift(point.position);
                CreateMarker("PathBeginningPoint", pointPosition, pathBeginningMarkerSize, pathBeginningColor, PrimitiveType.Sphere);
                CreateLine("PathBeginningDirection", pointPosition, pointPosition + FlattenDirection(point.direction) * entryOffset, pathBeginningColor, optionLineWidth * 1.5f);
            }
        }

        if (showBranchAnchors)
        {
            foreach (var decision in decisionNodes)
                CreateDecisionBranchAnchorMarkers(decision);
        }
    }

    void ClearRuntimeVisuals()
    {
        if (visualRoot == null) return;

        if (Application.isPlaying)
            Destroy(visualRoot.gameObject);
        else
            DestroyImmediate(visualRoot.gameObject);
    }

    Vector3 Lift(Vector3 position)
    {
        return position + Vector3.up * visualHeightOffset;
    }

    void CreateRouteAnchorMarkers(Vector3[] corners)
    {
        float totalLength = GetPathLength(corners);
        float spacing = Mathf.Max(0.25f, routeAnchorSpacing);

        for (float distance = spacing; distance < totalLength; distance += spacing)
        {
            Vector3 position = GetPointAtDistanceAlongPath(distance, corners);
            Vector3 next = GetPointAtDistanceAlongPath(distance + Mathf.Min(spacing * 0.5f, 0.75f), corners);
            Vector3 direction = FlattenDirection(next - position);

            if (direction.sqrMagnitude <= Mathf.Epsilon)
                continue;

            CreateTriangleMarker("RouteAnchor", Lift(position), direction, routeAnchorSize, correctOptionColor);
        }
    }

    void CreateCorridorAnchorMarkers(Vector3[] corners)
    {
        if (corridorSegments == null || corridorSegments.Count == 0)
            return;

        float spacing = Mathf.Max(0.25f, corridorAnchorSpacing);

        foreach (var corridor in corridorSegments)
        {
            float startDistance = corridor.startDistance + spacing;
            float endDistance = corridor.endDistance - spacing * 0.25f;

            for (float distance = startDistance; distance < endDistance; distance += spacing)
            {
                Vector3 position = GetPointAtDistanceAlongPath(distance, corners);
                Vector3 next = GetPointAtDistanceAlongPath(distance + Mathf.Min(spacing * 0.5f, 0.75f), corners);
                Vector3 direction = FlattenDirection(next - position);

                if (direction.sqrMagnitude <= Mathf.Epsilon)
                    continue;

                CreateTriangleMarker("CorridorAnchor", Lift(position), direction, corridorAnchorSize, corridorAnchorColor);
            }
        }
    }

    void CreateDecisionBranchAnchorMarkers(DecisionNode decision)
    {
        if (decision.entries == null)
            return;

        List<BranchAnchorPath> paths = new List<BranchAnchorPath>();
        Vector3 forwardDirection = GetPathDirectionAtPoint(decision.position, debugPath.corners);
        Vector3 backwardDirection = -forwardDirection;

        foreach (var entry in decision.entries)
        {
            if (!OptionContinuesFromNode(decision.position, entry.direction))
                continue;

            Vector3 direction = FlattenDirection(entry.direction);

            if (direction.sqrMagnitude <= Mathf.Epsilon)
                continue;

            if (!entry.isCorrect && Vector3.Dot(direction, backwardDirection) > 0.55f)
                continue;

            AddBranchAnchorPath(paths, direction, entry.isCorrect);
        }

        foreach (var path in paths)
        {
            Color color = path.isCorrect ? correctOptionColor : walkableAreaAnchorColor;
            string markerName = path.isCorrect ? "CorrectBranchAnchor" : "WrongBranchAnchor";
            CreateBranchAnchorMarkers(markerName, decision.position, path.direction, branchAnchorProbeDistance, branchAnchorSpacing, branchAnchorSize, color);
        }
    }

    void AddBranchAnchorPath(List<BranchAnchorPath> paths, Vector3 direction, bool isCorrect)
    {
        float mergeAngle = Mathf.Max(5f, branchAnchorMergeAngle);

        for (int i = 0; i < paths.Count; i++)
        {
            if (Vector3.Angle(paths[i].direction, direction) > mergeAngle)
                continue;

            BranchAnchorPath path = paths[i];

            if (isCorrect && !path.isCorrect)
            {
                path.direction = direction;
                path.isCorrect = true;
                paths[i] = path;
            }

            return;
        }

        paths.Add(new BranchAnchorPath
        {
            direction = direction,
            isCorrect = isCorrect
        });
    }

    void CreateWalkableAreaAnchorMarkers(Vector3[] corners)
    {
        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();

        if (triangulation.vertices == null || triangulation.vertices.Length == 0)
            return;

        Bounds bounds = new Bounds(triangulation.vertices[0], Vector3.zero);

        for (int i = 1; i < triangulation.vertices.Length; i++)
            bounds.Encapsulate(triangulation.vertices[i]);

        float spacing = Mathf.Max(0.35f, walkableAreaAnchorSpacing);
        float sampleRadius = Mathf.Max(0.1f, walkableAreaSampleRadius);
        int created = 0;
        HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();

        for (float x = bounds.min.x; x <= bounds.max.x; x += spacing)
        {
            for (float z = bounds.min.z; z <= bounds.max.z; z += spacing)
            {
                if (created >= maxWalkableAreaAnchors)
                    return;

                Vector3 samplePoint = new Vector3(x, bounds.center.y, z);

                if (!NavMesh.SamplePosition(samplePoint, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
                    continue;

                Vector2Int cell = new Vector2Int(
                    Mathf.RoundToInt(hit.position.x / spacing),
                    Mathf.RoundToInt(hit.position.z / spacing)
                );

                if (occupiedCells.Contains(cell))
                    continue;

                occupiedCells.Add(cell);

                bool isOnCorrectRoute = DistanceToPath(hit.position, corners) <= correctPathAnchorDistance;
                Vector3 direction = isOnCorrectRoute
                    ? GetPathDirectionAtPoint(hit.position, corners)
                    : GetWalkableAreaDirection(hit.position, spacing);

                if (direction.sqrMagnitude <= Mathf.Epsilon)
                    continue;

                Color color = isOnCorrectRoute ? correctOptionColor : walkableAreaAnchorColor;
                CreateTriangleMarker(isOnCorrectRoute ? "CorrectWalkableAnchor" : "WalkableAreaAnchor", Lift(hit.position), direction, walkableAreaAnchorSize, color);
                created++;
            }
        }
    }

    Vector3 GetWalkableAreaDirection(Vector3 position, float probeDistance)
    {
        Vector3[] axes =
        {
            Vector3.forward,
            Vector3.right,
            FlattenDirection(Vector3.forward + Vector3.right),
            FlattenDirection(Vector3.forward - Vector3.right)
        };

        float bestScore = -1f;
        Vector3 bestDirection = Vector3.forward;

        foreach (var axis in axes)
        {
            float forwardDistance = GetWalkableProbeDistance(position, axis, probeDistance);
            float backwardDistance = GetWalkableProbeDistance(position, -axis, probeDistance);
            float score = forwardDistance + backwardDistance;

            if (score > bestScore)
            {
                bestScore = score;
                bestDirection = forwardDistance >= backwardDistance ? axis : -axis;
            }
        }

        return FlattenDirection(bestDirection);
    }

    float GetWalkableProbeDistance(Vector3 position, Vector3 direction, float probeDistance)
    {
        Vector3 flatDirection = FlattenDirection(direction);

        if (flatDirection.sqrMagnitude <= Mathf.Epsilon)
            return 0f;

        if (!NavMesh.SamplePosition(position, out NavMeshHit startHit, 0.5f, NavMesh.AllAreas))
            return 0f;

        Vector3 target = position + flatDirection * probeDistance;

        if (!NavMesh.SamplePosition(target, out NavMeshHit targetHit, 0.5f, NavMesh.AllAreas))
            return 0f;

        if (NavMesh.Raycast(startHit.position, targetHit.position, out NavMeshHit rayHit, NavMesh.AllAreas))
            return Vector3.Distance(startHit.position, rayHit.position);

        return Vector3.Distance(startHit.position, targetHit.position);
    }

    void CreateBranchAnchorMarkers(string markerName, Vector3 nodePosition, Vector3 direction, float probeDistance, float anchorSpacing, float anchorSize, Color color)
    {
        Vector3 flatDirection = FlattenDirection(direction);

        if (flatDirection.sqrMagnitude <= Mathf.Epsilon)
            return;

        if (!NavMesh.SamplePosition(nodePosition, out NavMeshHit previousHit, 1.2f, NavMesh.AllAreas))
            return;

        float spacing = Mathf.Max(0.25f, anchorSpacing);
        float maxDistance = Mathf.Max(spacing, probeDistance);
        Vector3 previous = previousHit.position;

        for (float distance = spacing; distance <= maxDistance; distance += spacing)
        {
            Vector3 candidate = nodePosition + flatDirection * distance;

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit branchHit, 0.75f, NavMesh.AllAreas))
                break;

            if (NavMesh.Raycast(previous, branchHit.position, out NavMeshHit rayHit, NavMesh.AllAreas))
                break;

            CreateTriangleMarker(markerName, Lift(branchHit.position), flatDirection, anchorSize, color);
            previous = branchHit.position;
        }
    }

    float GetPathLength(Vector3[] corners)
    {
        float total = 0f;

        for (int i = 0; i < corners.Length - 1; i++)
            total += Vector3.Distance(corners[i], corners[i + 1]);

        return total;
    }

    void CreateTriangleMarker(string markerName, Vector3 position, Vector3 direction, float size, Color color)
    {
        GameObject marker = new GameObject(markerName);
        marker.transform.SetParent(visualRoot, false);
        marker.transform.position = position;

        MeshFilter meshFilter = marker.AddComponent<MeshFilter>();
        MeshRenderer renderer = marker.AddComponent<MeshRenderer>();

        Vector3 forward = FlattenDirection(direction);
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        float halfWidth = size * 0.45f;
        float length = size;

        Mesh mesh = new Mesh();
        mesh.vertices = new[]
        {
            forward * length,
            -forward * length * 0.45f + right * halfWidth,
            -forward * length * 0.45f - right * halfWidth
        };
        mesh.triangles = new[] { 0, 1, 2 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;
        renderer.material = CreateMaterial(color);
    }

    void CreateMarker(string markerName, Vector3 position, float size, Color color, PrimitiveType primitiveType)
    {
        GameObject marker = GameObject.CreatePrimitive(primitiveType);
        marker.name = markerName;
        marker.transform.SetParent(visualRoot, false);
        marker.transform.position = position;
        marker.transform.localScale = Vector3.one * size;

        Collider markerCollider = marker.GetComponent<Collider>();
        if (markerCollider != null)
            Destroy(markerCollider);

        Renderer renderer = marker.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material = CreateMaterial(color);
    }

    void CreateLine(string lineName, Vector3 from, Vector3 to, Color color, float width)
    {
        GameObject lineObject = new GameObject(lineName);
        lineObject.transform.SetParent(visualRoot, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.SetPosition(0, from);
        line.SetPosition(1, to);
        line.startWidth = width;
        line.endWidth = width;
        line.material = GetLineMaterial();
        line.startColor = color;
        line.endColor = color;
    }

    Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
            shader = Shader.Find("Standard");

        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        if (shader == null)
        {
            Debug.LogError("No compatible shader found for V4 debug marker material.");
            return null;
        }

        Material material = new Material(shader);
        material.color = color;
        return material;
    }

    Material GetLineMaterial()
    {
        if (lineMaterial != null)
            return lineMaterial;

        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
        {
            Debug.LogError("No compatible shader found for V4 debug line material.");
            return null;
        }

        lineMaterial = new Material(shader);
        return lineMaterial;
    }
}

// =========================
// DATA
// =========================
[System.Serializable]
public class DecisionNode
{
    public Vector3 position;
    public List<Vector3> options;
    public Vector3 correctOption;
    public List<EntryNode> entries;
}

[System.Serializable]
public class EntryNode
{
    public Vector3 position;
    public Vector3 direction;
    public bool isCorrect;
}

public struct PathProjection
{
    public int segmentIndex;
    public float distanceAlongPath;
    public Vector3 position;
    public float distanceToPath;
}

[System.Serializable]
public class CorridorSegment
{
    public float startDistance;
    public float endDistance;
    public int routeAnchorCount;
    public Vector3 startPosition;
    public Vector3 endPosition;
}

[System.Serializable]
public class PathBeginningPoint
{
    public Vector3 position;
    public Vector3 direction;
    public float distanceAlongPath;
    public string side;
}

public class NavMeshBranchAnalysis
{
    public int entryOptionCount;
    public int branchCount;
    public bool hasMismatch;
    public List<float> branchAngles;
}

public struct BranchAnchorPath
{
    public Vector3 direction;
    public bool isCorrect;
}
