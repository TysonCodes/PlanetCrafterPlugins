using System;
using System.Collections.Generic;
using System.Linq;
using SpaceCraft;
using UnityEngine;

namespace OpenInteriorSpaces_Plugin
{
    public enum PodDirection {PodFront, PodRight, PodBack, PodLeft};    // Local +Z, +X, -Z, -X
    public enum PodRotation {None, CW_Quarter, Half, CCW_Quarter};
    public enum PillarDirection { PillarFrontLeft, PillarFrontRight, PillarBackRight, PillarBackLeft};

    public class PodInfo
    {
        public WorldObject associatedWorldObj;
        public GameObject associatedGameObj;
        public Dictionary<PodDirection, Panel> panelByDirection = new Dictionary<PodDirection, Panel>();
        public Dictionary<PodDirection, PodInfo> podByDirection = new Dictionary<PodDirection, PodInfo>();
        public Dictionary<PillarDirection, PillarInfo> pillarsByDirection = new Dictionary<PillarDirection, PillarInfo>();

        public static Dictionary<Panel, PodInfo> podInfoByPanel = new Dictionary<Panel, PodInfo>();
        public static Dictionary<Vector3Int, PodInfo> podsByLocation = new Dictionary<Vector3Int, PodInfo>();

        private const float DETECT_DISTANCE = 2.0f;
        private const int POD_SPACING = 80;

        private Vector3Int position;
        private PodRotation rotation;

        public PodInfo(WorldObject worldObject, GameObject gameObject)
        {
            associatedWorldObj = worldObject;
            associatedGameObj = gameObject;
            position = PositionFloatToInt(worldObject.GetPosition());
            rotation = CaculateRotation(worldObject.GetRotation().eulerAngles.y);
            Plugin.bepInExLogger.LogInfo($"Adding Pod. WorldObject: {worldObject.GetId()}, Location: {worldObject.GetPosition()}, Rotation: {rotation}");
            podsByLocation[position] = this;
            var panels = associatedGameObj.GetComponentsInChildren<Panel>();
            for (int i = 0; i < panels.Length; i++)
            {
                panelByDirection[(PodDirection)i] = panels[i];
                PodInfo.podInfoByPanel[panels[i]] = this;
            }
            GeneratePillarInfo();
            DetectAdjacentPods();
        }

        private Vector3Int PositionFloatToInt(Vector3 position)
        {
            return new Vector3Int(Mathf.RoundToInt(position.x * 10.0f), Mathf.RoundToInt(position.y * 10.0f), Mathf.RoundToInt(position.z * 10.0f));
        }

        private PodRotation CaculateRotation(float y)
        {
            if (Mathf.DeltaAngle(y, 90.0f) < 0.1f)
            {
                return PodRotation.CW_Quarter;
            }
            if (Mathf.DeltaAngle(y, 180.0f) < 0.1f)
            {
                return PodRotation.Half;
            }
            if (Mathf.DeltaAngle(y, 270.0f) < 0.1f)
            {
                return PodRotation.CCW_Quarter;
            }
            return PodRotation.None;
        }

        public void DetectAdjacentPods()
        {
            // Panels are in Local +Z/-Z/+X/-X order (Front, Back, Right, Left - based on local coordinates)
            // Rotation is around Y with +90 meaning the local +Z now faces World +X and local +X now faces World -Z

            // Get pods in each global direction (ignore rotation).
            podsByLocation.TryGetValue(position + (Vector3Int.right * POD_SPACING), out PodInfo nearbyPodRightGlobal);
            podsByLocation.TryGetValue(position + (Vector3Int.left * POD_SPACING), out PodInfo nearbyPodLeftGlobal);
            podsByLocation.TryGetValue(position + (Vector3Int.forward * POD_SPACING), out PodInfo nearbyPodFrontGlobal);
            podsByLocation.TryGetValue(position + (Vector3Int.back * POD_SPACING), out PodInfo nearbyPodBackGlobal);

            // Update adjacency tracking
            UpdateAdjacentPodsIfApplicable(PodDirection.PodRight, nearbyPodRightGlobal);
            UpdateAdjacentPodsIfApplicable(PodDirection.PodLeft, nearbyPodLeftGlobal);
            UpdateAdjacentPodsIfApplicable(PodDirection.PodFront, nearbyPodFrontGlobal);
            UpdateAdjacentPodsIfApplicable(PodDirection.PodBack, nearbyPodBackGlobal);
        }

        private void UpdateAdjacentPodsIfApplicable(PodDirection globalDirection, PodInfo adjacentPod)
        {
            if (adjacentPod != null)
            {
                // Update this pod to point to the adjacent pod
                PodDirection directionToAdjacentPod = CalculatePodDirectionAfterRotation(globalDirection);
                AddAdjacentPod(directionToAdjacentPod, adjacentPod);

                // Update the adjacent pod to point back to this pod
                PodDirection flippedGlobalDirection = CalculateRotatedPodDirection(globalDirection, PodRotation.Half);
                PodDirection directionFromAdjacentPodToThisPod = adjacentPod.CalculatePodDirectionAfterRotation(flippedGlobalDirection);
                adjacentPod.AddAdjacentPod(directionFromAdjacentPodToThisPod, this);

                // Update pillars
                UpdatePillars();
                adjacentPod.UpdatePillars();
            }
        }

        private PodDirection CalculatePodDirectionAfterRotation(PodDirection startDirection)
        {
            return CalculateRotatedPodDirection(startDirection, rotation);
        }

        private PodDirection CalculateRotatedPodDirection(PodDirection startDirection, PodRotation rotationAmount)
        {
            int newPodDirection = ((int)startDirection + (int)rotationAmount) % 4;
            return (PodDirection)newPodDirection;
        }

        public void AddAdjacentPod(PodDirection direction, PodInfo podInfo)
        {
            Plugin.bepInExLogger.LogInfo($"Adding adjacent pod from '{this.associatedWorldObj.GetId()}' to '{podInfo.associatedWorldObj.GetId()}' in direction '{direction}'.");
            podByDirection[direction] = podInfo;
            // TODO: This can be cleaner.
            // TODO: How do I figure out the missing interior pillars once I have one or more of them? The problem is the links aren't there when we check and 
            // don't fully establish until return AddAdjacent pod. Maybe this update should happen after somehow?
            switch (direction)
            {
                case PodDirection.PodFront:
                    pillarsByDirection[PillarDirection.PillarFrontLeft].borderingPods.Add(podInfo);
                    pillarsByDirection[PillarDirection.PillarFrontRight].borderingPods.Add(podInfo);
                    break;
                case PodDirection.PodRight:
                    pillarsByDirection[PillarDirection.PillarBackRight].borderingPods.Add(podInfo);
                    pillarsByDirection[PillarDirection.PillarFrontRight].borderingPods.Add(podInfo);
                    break;
                case PodDirection.PodBack:
                    pillarsByDirection[PillarDirection.PillarBackLeft].borderingPods.Add(podInfo);
                    pillarsByDirection[PillarDirection.PillarBackRight].borderingPods.Add(podInfo);
                    break;
                case PodDirection.PodLeft:
                    pillarsByDirection[PillarDirection.PillarFrontLeft].borderingPods.Add(podInfo);
                    pillarsByDirection[PillarDirection.PillarBackLeft].borderingPods.Add(podInfo);
                    break;
                default:
                    break;
            }
        }

        public void GeneratePillarInfo()
        {
            pillarsByDirection[PillarDirection.PillarFrontLeft] = new PillarInfo(this, PillarDirection.PillarFrontLeft, PodDirection.PodLeft, PodDirection.PodFront);
            pillarsByDirection[PillarDirection.PillarFrontRight] = new PillarInfo(this, PillarDirection.PillarFrontRight, PodDirection.PodFront, PodDirection.PodRight);
            pillarsByDirection[PillarDirection.PillarBackRight] = new PillarInfo(this, PillarDirection.PillarBackRight, PodDirection.PodRight, PodDirection.PodBack);
            pillarsByDirection[PillarDirection.PillarBackLeft] = new PillarInfo(this, PillarDirection.PillarBackLeft, PodDirection.PodBack, PodDirection.PodLeft);
        }

        public void UpdatePillars()
        {
            pillarsByDirection[PillarDirection.PillarFrontLeft].CheckForOppositePod();
            pillarsByDirection[PillarDirection.PillarFrontRight].CheckForOppositePod();
            pillarsByDirection[PillarDirection.PillarBackLeft].CheckForOppositePod();
            pillarsByDirection[PillarDirection.PillarBackRight].CheckForOppositePod();
        }
    }

    public class PillarInfo
    {
        public bool IsInterior {get; private set;} = false;
        public PodInfo interiorPod;
        public List<PodInfo> borderingPods = new List<PodInfo>();
        public PodInfo oppositePod;
        private PillarDirection direction;

        public PillarInfo(PodInfo interiorPod, PillarDirection direction, PodDirection oneBorder, PodDirection otherBorder)
        {
            this.interiorPod = interiorPod;
            this.direction = direction;
            // TODO: I can figure out these directions from the enum.
            if (interiorPod.podByDirection.ContainsKey(oneBorder))
            {
                borderingPods.Add(interiorPod.podByDirection[oneBorder]);
            }
            if (interiorPod.podByDirection.ContainsKey(oneBorder))
            {
                borderingPods.Add(interiorPod.podByDirection[oneBorder]);
            }
            CheckForOppositePod();
        }

        public void CheckForOppositePod()
        {
            if (IsInterior) {return;}   // For now we can't undo making something interior.
            if (borderingPods.Count == 2)
            {
                var firstPodBorderingPods = borderingPods[0].podByDirection;
                var secondPodBorderingPods = borderingPods[1].podByDirection;
                foreach (var pod in firstPodBorderingPods)
                {
                    if (pod.Value != interiorPod && secondPodBorderingPods.ContainsValue(pod.Value))
                    {
                        oppositePod = pod.Value;
                        IsInterior = true;
                        Plugin.bepInExLogger.LogInfo($"Found an interior pillar. Direction: '{direction}', pod: '{interiorPod.associatedWorldObj.GetId()}'");
                        borderingPods[0].UpdatePillars();
                        borderingPods[1].UpdatePillars();
                    }
                }
            }
        }
    }
}
