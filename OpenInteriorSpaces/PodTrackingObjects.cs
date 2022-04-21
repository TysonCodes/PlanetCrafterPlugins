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
        private WorldObject associatedWorldObj;
        private GameObject associatedGameObj;
        private Dictionary<PodDirection, Panel> panelByGlobalDirection = new Dictionary<PodDirection, Panel>();
        private Dictionary<PodDirection, PodInfo> podByGlobalDirection = new Dictionary<PodDirection, PodInfo>();
        private Dictionary<PillarDirection, PillarInfo> pillarsByGlobalDirection = new Dictionary<PillarDirection, PillarInfo>();
        
        public static Dictionary<Vector3Int, PodInfo> podsByLocation = new Dictionary<Vector3Int, PodInfo>();
        public static Dictionary<int, PodInfo> podsByWorldId = new Dictionary<int, PodInfo>();

        private const float DETECT_DISTANCE = 2.0f;
        private const int POD_SPACING = 80;
        private const string GAME_OBJECT_PATH_TO_STRUCTURE = "Container/Structure";

        private static readonly Dictionary<PodDirection, PillarDirection> leftPillarByDirection = new Dictionary<PodDirection, PillarDirection>() {
            {PodDirection.PodFront, PillarDirection.PillarFrontLeft},
            {PodDirection.PodRight, PillarDirection.PillarFrontRight},
            {PodDirection.PodBack, PillarDirection.PillarBackRight},
            {PodDirection.PodLeft, PillarDirection.PillarBackLeft}
        };

        private static readonly Dictionary<PodDirection, PillarDirection> rightPillarByDirection = new Dictionary<PodDirection, PillarDirection>() {
            {PodDirection.PodFront, PillarDirection.PillarFrontRight},
            {PodDirection.PodRight, PillarDirection.PillarBackRight},
            {PodDirection.PodBack, PillarDirection.PillarBackLeft},
            {PodDirection.PodLeft, PillarDirection.PillarFrontLeft}
        };

        private Vector3Int position;
        private PodRotation rotation;
        private Dictionary<PillarDirection, GameObject> pillarStructureByGlobalDirection = new Dictionary<PillarDirection, GameObject>();
        private List<PodDirection> interiorWalls = new List<PodDirection>();

        public PodInfo(WorldObject worldObject, GameObject gameObject)
        {
            associatedWorldObj = worldObject;
            associatedGameObj = gameObject;
            position = PositionFloatToInt(worldObject.GetPosition());
            rotation = CalculateRotation(worldObject.GetRotation().eulerAngles.y);
            Plugin.bepInExLogger.LogDebug($"Adding Pod. WorldObject: {worldObject.GetId()}, Location: {worldObject.GetPosition()}, Rotation: {rotation}");
            podsByLocation[position] = this;
            podsByWorldId[associatedWorldObj.GetId()] = this;
            DeterminePanelsForGlobalDirections();
            DeterminePillarStructuresForGlobalDirections();
            GeneratePillarInfo();
            DetectAdjacentPods();
        }

        public void Remove()
        {
            podsByLocation.Remove(position);
            podsByWorldId.Remove(associatedWorldObj.GetId());
            foreach(var adjacentPod in podByGlobalDirection)
            {
                if (adjacentPod.Value != null)
                {
                    PodDirection flippedGlobalDirection = CalculateRotatedPodDirection(adjacentPod.Key, PodRotation.Half);
                    adjacentPod.Value.podByGlobalDirection.Remove(flippedGlobalDirection);
                }
            }
            pillarsByGlobalDirection[PillarDirection.PillarFrontLeft].RemoveBorderingPod(this);
            pillarsByGlobalDirection[PillarDirection.PillarFrontRight].RemoveBorderingPod(this);
            pillarsByGlobalDirection[PillarDirection.PillarBackLeft].RemoveBorderingPod(this);
            pillarsByGlobalDirection[PillarDirection.PillarBackRight].RemoveBorderingPod(this);
        }

        public static void Reset()
        {
            podsByLocation = new Dictionary<Vector3Int, PodInfo>();
            podsByWorldId = new Dictionary<int, PodInfo>();
        }

        private Vector3Int PositionFloatToInt(Vector3 position)
        {
            return new Vector3Int(Mathf.RoundToInt(position.x * 10.0f), Mathf.RoundToInt(position.y * 10.0f), Mathf.RoundToInt(position.z * 10.0f));
        }

        private PodRotation CalculateRotation(float y)
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

        private void DeterminePanelsForGlobalDirections()
        {
            // Panels are in Local +Z/-Z/+X/-X order (Front, Back, Right, Left - based on local coordinates)
            // Rotation is around Y with +90 meaning the local +Z now faces World +X and local +X now faces World -Z
            Panel[] panels = associatedGameObj.GetComponentsInChildren<Panel>();
            panelByGlobalDirection[CalculatePodDirectionAfterRotation(PodDirection.PodFront)] = panels[0];
            panelByGlobalDirection[CalculatePodDirectionAfterRotation(PodDirection.PodBack)] = panels[1];
            panelByGlobalDirection[CalculatePodDirectionAfterRotation(PodDirection.PodRight)] = panels[2];
            panelByGlobalDirection[CalculatePodDirectionAfterRotation(PodDirection.PodLeft)] = panels[3];            
        }

        private void DeterminePillarStructuresForGlobalDirections()
        {
            // Pillars are called 'Wall_Angle_03' inside 'Structure' inside 'Container' in the Pod gameobject.
            // They have capsule colliders on them.
            // They are ordered locally - BackRight, BackLeft, FrontRight, FrontLeft
            Transform structureGameObject = associatedGameObj.transform.Find(GAME_OBJECT_PATH_TO_STRUCTURE);
            CapsuleCollider[] pillarStructures = structureGameObject.GetComponentsInChildren<CapsuleCollider>();
            pillarStructureByGlobalDirection[CalculatePillarDirectionAfterRotation(PillarDirection.PillarBackRight)] = pillarStructures[0].gameObject;
            pillarStructureByGlobalDirection[CalculatePillarDirectionAfterRotation(PillarDirection.PillarBackLeft)] = pillarStructures[1].gameObject;
            pillarStructureByGlobalDirection[CalculatePillarDirectionAfterRotation(PillarDirection.PillarFrontRight)] = pillarStructures[2].gameObject;
            pillarStructureByGlobalDirection[CalculatePillarDirectionAfterRotation(PillarDirection.PillarFrontLeft)] = pillarStructures[3].gameObject;
        }

        private void DetectAdjacentPods()
        {
            // Update adjacency tracking
            UpdateAdjacentPodsIfApplicable(PodDirection.PodRight, TryToGetNearbyPod(position + (Vector3Int.right * POD_SPACING)));
            UpdateAdjacentPodsIfApplicable(PodDirection.PodLeft, TryToGetNearbyPod(position + (Vector3Int.left * POD_SPACING)));
            UpdateAdjacentPodsIfApplicable(PodDirection.PodFront, TryToGetNearbyPod(position + (Vector3Int.forward * POD_SPACING)));
            UpdateAdjacentPodsIfApplicable(PodDirection.PodBack, TryToGetNearbyPod(position + (Vector3Int.back * POD_SPACING)));
        }

        private PodInfo TryToGetNearbyPod(Vector3Int locationToCheck, int tolerance = 5)
        {
            var result =  podsByLocation.FirstOrDefault(podAtLocation => (podAtLocation.Key - locationToCheck).magnitude < tolerance);
            return result.Value;
        }

        private void UpdateAdjacentPodsIfApplicable(PodDirection globalDirection, PodInfo adjacentPod)
        {
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

        private PillarDirection CalculatePillarDirectionAfterRotation(PillarDirection startDirection)
        {
            int newPillarDirection = ((int)startDirection + (int)rotation) % 4;
            return (PillarDirection)newPillarDirection;
        }

        private void AddAdjacentPod(PodDirection direction, PodInfo podInfo)
        {
            Plugin.bepInExLogger.LogDebug($"Adding adjacent pod from '{this.associatedWorldObj.GetId()}' to '{podInfo.associatedWorldObj.GetId()}' in global direction '{direction}'.");
            podByGlobalDirection[direction] = podInfo;
        }

        private void GeneratePillarInfo()
        {
            foreach (PillarDirection direction in Enum.GetValues(typeof(PillarDirection)))
            {
                pillarsByGlobalDirection[direction] = PillarInfo.GetPillarAtLocation(position, direction);
            }
            foreach (PillarDirection direction in Enum.GetValues(typeof(PillarDirection)))
            {
                pillarsByGlobalDirection[direction].AddBorderingPod(this, direction);
            }
        }
    
        public void PillarInteriorChanged(PillarDirection direction, bool isInside)
        {
            UpdateWall(PodDirection.PodLeft);
            UpdateWall(PodDirection.PodFront);
            UpdateWall(PodDirection.PodRight);
            UpdateWall(PodDirection.PodBack);
        }

        private void UpdateWall(PodDirection podDirection)
        {
            PillarDirection leftPillarDirection = leftPillarByDirection[podDirection];
            PillarDirection rightPillarDirection = rightPillarByDirection[podDirection];
            bool leftPillarInside = pillarsByGlobalDirection[leftPillarDirection].IsInterior;
            bool rightPillarInside = pillarsByGlobalDirection[rightPillarDirection].IsInterior;
            GameObject leftPillar = pillarStructureByGlobalDirection[leftPillarDirection];
            GameObject rightPillar = pillarStructureByGlobalDirection[rightPillarDirection];
            Panel podDirectionPanel = panelByGlobalDirection[podDirection];
            Plugin.bepInExLogger.LogDebug($"UpdateWall {podDirection} for pod: {associatedWorldObj.GetId()}");
            if (podDirectionPanel.subPanelType == DataConfig.BuildPanelSubType.WallCorridor)
            {
                CorridorWallWidget widget = podDirectionPanel.GetComponentInChildren<CorridorWallWidget>();
                if (widget == null)
                {
                    Plugin.bepInExLogger.LogError("Unable to get a CorridorWallWidget for a corridor.");
                    return;
                }
                if (leftPillarInside || rightPillarInside)
                {
                    Plugin.bepInExLogger.LogDebug($"\tChanging corridor to interior for pod: {associatedWorldObj.GetId()}, direction: {podDirection}");
                    widget.ShowInteriorWall();
                }
                else
                {
                    Plugin.bepInExLogger.LogDebug($"\tChanging corridor to original for pod: {associatedWorldObj.GetId()}, direction: {podDirection}");
                    widget.ShowOriginalWall();
                }
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
            PillarInfo pillarInfo = TryToGetNearbyPillar(pillarLocation);
            if (pillarInfo == null)
            {
                pillarInfo = new PillarInfo(pillarLocation);
            }
            return pillarInfo;
        }

        public static void Reset()
        {
            pillarInfoByLocation = new Dictionary<Vector3Int, PillarInfo>();
        }

        private static PillarInfo TryToGetNearbyPillar(Vector3Int locationToCheck, int tolerance = 5)
        {
            var result = pillarInfoByLocation.FirstOrDefault(pillarAtLocation => (pillarAtLocation.Key - locationToCheck).magnitude < tolerance);
            return result.Value;
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
            bool newInterior = (borderingPodsByDirectionFromPod.Count == 4);

            if (newInterior != IsInterior)
            {
                Plugin.bepInExLogger.LogDebug($"Pillar at location: '{position}' changed to IsInterior = {newInterior}.");
                IsInterior = newInterior;
                
                foreach (var borderPod in borderingPodsByDirectionFromPod)
                {
                    borderPod.Value.PillarInteriorChanged(borderPod.Key, IsInterior);
                }
            }
        }
    }
}
