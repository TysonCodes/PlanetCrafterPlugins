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
        public Dictionary<PodDirection, Panel> panelByGlobalDirection = new Dictionary<PodDirection, Panel>();
        public Dictionary<PodDirection, PodInfo> podByGlobalDirection = new Dictionary<PodDirection, PodInfo>();
        public Dictionary<PillarDirection, PillarInfo> pillarsByGlobalDirection = new Dictionary<PillarDirection, PillarInfo>();

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
            DeterminePanelsForGlobalRotation();
            GeneratePillarInfo();
            DetectAdjacentPods();
        }

        private Vector3Int PositionFloatToInt(Vector3 position)
        {
            return new Vector3Int(Mathf.RoundToInt(position.x * 10.0f), Mathf.RoundToInt(position.y * 10.0f), Mathf.RoundToInt(position.z * 10.0f));
        }

        private PodRotation CaculateRotation(float y)
        {
            if (Mathf.Abs(Mathf.DeltaAngle(y, 90.0f)) < 0.1f)
            {
                return PodRotation.CW_Quarter;
            }
            if (Mathf.Abs(Mathf.DeltaAngle(y, 180.0f)) < 0.1f)
            {
                return PodRotation.Half;
            }
            if (Mathf.Abs(Mathf.DeltaAngle(y, -90.0f)) < 0.1f)
            {
                return PodRotation.CCW_Quarter;
            }
            return PodRotation.None;
        }

        private void DeterminePanelsForGlobalRotation()
        {
            // Panels are in Local +Z/-Z/+X/-X order (Front, Back, Right, Left - based on local coordinates)
            // Rotation is around Y with +90 meaning the local +Z now faces World +X and local +X now faces World -Z
            Panel[] panels = associatedGameObj.GetComponentsInChildren<Panel>();
            panelByGlobalDirection[CalculatePodDirectionAfterRotation(PodDirection.PodFront)] = panels[0];
            panelByGlobalDirection[CalculatePodDirectionAfterRotation(PodDirection.PodBack)] = panels[1];
            panelByGlobalDirection[CalculatePodDirectionAfterRotation(PodDirection.PodRight)] = panels[2];
            panelByGlobalDirection[CalculatePodDirectionAfterRotation(PodDirection.PodLeft)] = panels[3];            
        }

        public void DetectAdjacentPods()
        {
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
            // TODO: Does direction even matter?
            if (adjacentPod != null)
            {
                // Update this pod to point to the adjacent pod
                AddAdjacentPod(globalDirection, adjacentPod);

                // Update the adjacent pod to point back to this pod
                PodDirection flippedGlobalDirection = CalculateRotatedPodDirection(globalDirection, PodRotation.Half);
                adjacentPod.AddAdjacentPod(flippedGlobalDirection, this);
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
            Plugin.bepInExLogger.LogInfo($"Adding adjacent pod from '{this.associatedWorldObj.GetId()}' to '{podInfo.associatedWorldObj.GetId()}' in global direction '{direction}'.");
            podByGlobalDirection[direction] = podInfo;
        }

        public void GeneratePillarInfo()
        {
            foreach (PillarDirection direction in Enum.GetValues(typeof(PillarDirection)))
            {
                pillarsByGlobalDirection[direction] = PillarInfo.GetPillarAtLocation(position, direction);
                pillarsByGlobalDirection[direction].AddBorderingPod(this, direction);
            }
        }
    }

    public class PillarInfo
    {
        public bool IsInterior {get; private set;} = false;

        private const int PILLAR_OFFSET_FROM_POD_CENTER = 40;

        private static Dictionary<Vector3Int, PillarInfo> pillarInfoByLocation = new Dictionary<Vector3Int, PillarInfo>();

        private Vector3Int position;
        private Dictionary<PillarDirection, PodInfo> borderingPodsByDirectionFromPod = new Dictionary<PillarDirection, PodInfo>();

        public static PillarInfo GetPillarAtLocation(Vector3Int podLocation, PillarDirection globalDirection)
        {
            Vector3Int pillarLocation = podLocation;
            switch (globalDirection)
            {
                case PillarDirection.PillarFrontLeft:
                    pillarLocation += new Vector3Int(-PILLAR_OFFSET_FROM_POD_CENTER, 0, PILLAR_OFFSET_FROM_POD_CENTER);
                    break;
                case PillarDirection.PillarFrontRight:
                    pillarLocation += new Vector3Int(PILLAR_OFFSET_FROM_POD_CENTER, 0, PILLAR_OFFSET_FROM_POD_CENTER);
                    break;
                case PillarDirection.PillarBackLeft:
                    pillarLocation += new Vector3Int(-PILLAR_OFFSET_FROM_POD_CENTER, 0, -PILLAR_OFFSET_FROM_POD_CENTER);
                    break;
                case PillarDirection.PillarBackRight:
                    pillarLocation += new Vector3Int(PILLAR_OFFSET_FROM_POD_CENTER, 0, -PILLAR_OFFSET_FROM_POD_CENTER);
                    break;
                default:
                    break;
            }
            if (!pillarInfoByLocation.TryGetValue(pillarLocation, out PillarInfo pillarInfo))
            {
                pillarInfo = new PillarInfo(pillarLocation);
            }
            return pillarInfo;
        }

        public PillarInfo(Vector3Int position)
        {
            this.position = position;
            pillarInfoByLocation[position] = this;
        }

        public void AddBorderingPod(PodInfo podToAdd, PillarDirection globalDirectionFromPodToPillar)
        {
            borderingPodsByDirectionFromPod[globalDirectionFromPodToPillar] = podToAdd;
            RecalculateInterior();
        }

        public void RemoveBorderingPod(PodInfo podToRemove)
        {
            if (borderingPodsByDirectionFromPod.ContainsValue(podToRemove))
            {
                borderingPodsByDirectionFromPod.Remove(borderingPodsByDirectionFromPod.First(directionalPod => directionalPod.Value == podToRemove).Key);
                RecalculateInterior();
            }
        }

        private void RecalculateInterior()
        {
            IsInterior = (borderingPodsByDirectionFromPod.Count == 4);
            // TODO: Need to have this emit an event to update graphics
            if (IsInterior)
            {
                // Temporary for debugging.
                Plugin.bepInExLogger.LogInfo($"Found an interior pillar at location: '{position}'.");
            }
        }
    }
}
