using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SpaceCraft;
using PluginFramework;

namespace OpenInteriorSpaces_Plugin
{
    public class PodWidget : MonoBehaviour
    {
        public Panel[] panelByLocalDirection;
        public PodCornerWidget[] podCornerByLocalDirection;

        private const string POD_GAME_OBJECT_NAME = "Pod";
        private const float DETECT_DISTANCE = 2.0f;
        private const int POD_SPACING = 80;
        private static Dictionary<Vector3Int, PodWidget> podsByLocation = new Dictionary<Vector3Int, PodWidget>();
        private static Dictionary<int, PodWidget> podsByWorldId = new Dictionary<int, PodWidget>();
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

        private WorldObject associatedWorldObj;
        private Vector3Int position;
        private PodRotation rotation;
        private bool initialized = false;
        private Dictionary<PodDirection, Panel> panelByGlobalDirection = new Dictionary<PodDirection, Panel>();
        private Dictionary<Panel, PodDirection> globalDirectionByPanel = new Dictionary<Panel, PodDirection>();
        private Dictionary<PodDirection, PodWidget> podByGlobalDirection = new Dictionary<PodDirection, PodWidget>();
        private Dictionary<PillarDirection, PillarInfo> pillarsByGlobalDirection = new Dictionary<PillarDirection, PillarInfo>();

        public bool AdjacentWallIsCorridor(PodDirection localDirection)
        {
            PodDirection globalDirection = CalculateRotatedPodDirection(localDirection, rotation);
            Panel adjacentWall = AdjacentWall(globalDirection);
            if (adjacentWall != null)
            {
                return (adjacentWall.subPanelType == DataConfig.BuildPanelSubType.WallCorridor);
            }
            return false;
        }

        public void Initialize()
        {
            // Called after WorldObjectsHandler.InstantiateWorldObject so all components exist and corridors have been determined.
            associatedWorldObj = GetComponent<WorldObjectAssociated>().GetWorldObject();
            position = PositionFloatToInt(transform.position);
            rotation = CalculateRotation(transform.eulerAngles.y);
            podsByLocation[position] = this;
            podsByWorldId[associatedWorldObj.GetId()] = this;
            Plugin.bepInExLogger.LogDebug($"Adding Pod. WorldObject: {associatedWorldObj.GetId()}, Location: {position}, Rotation: {rotation}");

            CalculatePanelDirections();
            DetectAdjacentPods();
            initialized = true;
            GeneratePillarInfo();
        }

        public void Remove()
        {
            podsByLocation.Remove(position);
            podsByWorldId.Remove(associatedWorldObj.GetId());
            foreach (var adjacentPod in podByGlobalDirection)
            {
                if (adjacentPod.Value != null)
                {
                    PodDirection flippedGlobalDirection = CalculateRotatedPodDirection(adjacentPod.Key, PodRotation.Half);
                    adjacentPod.Value.podByGlobalDirection.Remove(flippedGlobalDirection);
                }
            }
            foreach (var pillar in pillarsByGlobalDirection)
            {
                DetatchFromPillar(pillar.Value);
            }
            pillarsByGlobalDirection.Clear();
            panelByGlobalDirection.Clear();
            globalDirectionByPanel.Clear();
            podByGlobalDirection.Clear();
            foreach (var corner in podCornerByLocalDirection)
            {
                corner.SetAssociatedPillar(null);
            }
        }

        private void DetatchFromPillar(PillarInfo pillar)
        {
            pillar.RemoveBorderingPod(this);
            pillar.IsInteriorChanged -= RefreshPodWallsAndCorners;
        }

        public void RefreshPodWallsAndCorners()
        {
            if (!initialized) {return;}
            UpdateWall(PodDirection.PodLeft);
            UpdateWall(PodDirection.PodFront);
            UpdateWall(PodDirection.PodRight);
            UpdateWall(PodDirection.PodBack);
            foreach (var corner in podCornerByLocalDirection)
            {
                corner.UpdateDisplay();
            }
        }

        public void RefreshAdjacentPodIfApplicable(Panel panelChanged)
        {
            if (globalDirectionByPanel.ContainsKey(panelChanged))
            {
                PodDirection adjacentPodDirection = globalDirectionByPanel[panelChanged];
                Panel adjacentWall = AdjacentWall(adjacentPodDirection);
                if (adjacentWall != null && adjacentWall.subPanelType == DataConfig.BuildPanelSubType.WallCorridor)
                {
                    podByGlobalDirection[adjacentPodDirection].RefreshPodWallsAndCorners();
                }
            }
        }

        private Panel AdjacentWall(PodDirection globalDirection)
        {
            if (podByGlobalDirection.TryGetValue(globalDirection, out PodWidget adjacentPod))
            {
                PodDirection flippedGlobalDirection = CalculateRotatedPodDirection(globalDirection, PodRotation.Half);
                return adjacentPod.panelByGlobalDirection[flippedGlobalDirection];
            }
            return null;
        }

        private void UpdateWall(PodDirection podDirection)
        {
            PillarDirection leftPillarDirection = leftPillarByDirection[podDirection];
            PillarDirection rightPillarDirection = rightPillarByDirection[podDirection];
            bool leftPillarInside = pillarsByGlobalDirection[leftPillarDirection].IsInterior;
            bool rightPillarInside = pillarsByGlobalDirection[rightPillarDirection].IsInterior;
            Panel podDirectionPanel = panelByGlobalDirection[podDirection];
            //Plugin.bepInExLogger.LogDebug($"UpdateWall {podDirection} for pod: {associatedWorldObj.GetId()}");
            if (podDirectionPanel.subPanelType == DataConfig.BuildPanelSubType.WallCorridor)
            {
                var allWidgets = podDirectionPanel.GetComponentsInChildren<CorridorWallWidget>();
                if (allWidgets.Length == 0)
                {
                    Plugin.bepInExLogger.LogError("Unable to get a CorridorWallWidget for a corridor.");
                    return;
                }
                if (leftPillarInside || rightPillarInside)
                {
                    Plugin.bepInExLogger.LogDebug($"\tChanging corridor to interior for pod: {associatedWorldObj.GetId()}, direction: {podDirection}");
                    foreach (var widget in allWidgets)
                    {
                        widget.ShowInteriorWall();
                    }
                }
                else
                {
                    Plugin.bepInExLogger.LogDebug($"\tChanging corridor to original for pod: {associatedWorldObj.GetId()}, direction: {podDirection}");
                    foreach (var widget in allWidgets)
                    {
                        widget.ShowOriginalWall();
                    }
                }
            }
        }

        private Vector3Int PositionFloatToInt(Vector3 position)
        {
            return new Vector3Int(Mathf.RoundToInt(position.x * 10.0f), Mathf.RoundToInt(position.y * 10.0f), Mathf.RoundToInt(position.z * 10.0f));
        }

        private void CalculatePanelDirections()
        {
            foreach (PodDirection localDirection in Enum.GetValues(typeof(PodDirection)))
            {
                PodDirection globalDirection = CalculateRotatedPodDirection(localDirection, rotation);
                Panel curPanel = panelByLocalDirection[(int)localDirection];
                panelByGlobalDirection[globalDirection] = curPanel;
                globalDirectionByPanel[curPanel] = globalDirection;
            }
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

        private void GeneratePillarInfo()
        {
            foreach (PillarDirection localDirection in Enum.GetValues(typeof(PillarDirection)))
            {
                PillarDirection globalDirection = CalculateGlobalPillarDirection(localDirection);
                PillarInfo curPillar = PillarInfo.GetPillarAtLocation(position, globalDirection);
                podCornerByLocalDirection[(int) localDirection].Initialize();
                podCornerByLocalDirection[(int) localDirection].SetAssociatedPillar(curPillar);
                curPillar.IsInteriorChanged += RefreshPodWallsAndCorners;
                pillarsByGlobalDirection[globalDirection] = curPillar;
            }
            foreach (var pillar in pillarsByGlobalDirection)
            {
                pillar.Value.AddBorderingPod(this, pillar.Key);
            }
        }

        private PillarDirection CalculateGlobalPillarDirection(PillarDirection startDirection)
        {
            int newPillarDirection = ((int)startDirection + (int)rotation) % 4;
            return (PillarDirection)newPillarDirection;
        }

        private void DetectAdjacentPods()
        {
            // Update adjacency tracking
            UpdateAdjacentPodsIfApplicable(PodDirection.PodRight, TryToGetNearbyPod(position + (Vector3Int.right * POD_SPACING)));
            UpdateAdjacentPodsIfApplicable(PodDirection.PodLeft, TryToGetNearbyPod(position + (Vector3Int.left * POD_SPACING)));
            UpdateAdjacentPodsIfApplicable(PodDirection.PodFront, TryToGetNearbyPod(position + (Vector3Int.forward * POD_SPACING)));
            UpdateAdjacentPodsIfApplicable(PodDirection.PodBack, TryToGetNearbyPod(position + (Vector3Int.back * POD_SPACING)));
        }

        private PodWidget TryToGetNearbyPod(Vector3Int locationToCheck, int tolerance = 5)
        {
            var result = podsByLocation.FirstOrDefault(podAtLocation => (podAtLocation.Key - locationToCheck).magnitude < tolerance);
            return result.Value;
        }

        private void UpdateAdjacentPodsIfApplicable(PodDirection globalDirection, PodWidget adjacentPod)
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

        private void AddAdjacentPod(PodDirection globalDirection, PodWidget adjacentPod)
        {
            Plugin.bepInExLogger.LogDebug($"Adding adjacent pod from '{this.associatedWorldObj.GetId()}' to '{adjacentPod.associatedWorldObj.GetId()}' in global direction '{globalDirection}'.");
            podByGlobalDirection[globalDirection] = adjacentPod;
        }

        private PodDirection CalculateRotatedPodDirection(PodDirection startDirection, PodRotation rotationAmount)
        {
            int newPodDirection = ((int)startDirection + (int)rotationAmount) % 4;
            return (PodDirection)newPodDirection;
        }

        #region StaticMethods
        public static void Reset()
        {
            podsByLocation = new Dictionary<Vector3Int, PodWidget>();
            podsByWorldId = new Dictionary<int, PodWidget>();
        }

        public static void InjectWidgetIntoPodPrefab()
        {
            GameObject podPrefab = Framework.GameObjectByName[POD_GAME_OBJECT_NAME];

            // Attach ourselves
            PodWidget widgetOnPrefab = podPrefab.AddComponent<PodWidget>();

            // Get the panel objects into the array
            widgetOnPrefab.panelByLocalDirection = GetReferencesToPanels(podPrefab);

            // Create corners and attach to widget
            widgetOnPrefab.podCornerByLocalDirection = PodCornerWidget.InjectNewObjectsIntoPrefab(ref podPrefab, ref widgetOnPrefab);
        }

        private static Panel[] GetReferencesToPanels(GameObject podPrefab)
        {
            // Panels are in Local +Z/-Z/+X/-X order (Front, Back, Right, Left - based on local coordinates)
            // Rotation is around Y with +90 meaning the local +Z now faces World +X and local +X now faces World -Z
            Panel[] result = new Panel[4];
            Panel[] panelsOnPrefab = podPrefab.GetComponentsInChildren<Panel>();
            result[(int)PodDirection.PodFront] = panelsOnPrefab[0];
            result[(int)PodDirection.PodBack] = panelsOnPrefab[1];
            result[(int)PodDirection.PodRight] = panelsOnPrefab[2];
            result[(int)PodDirection.PodLeft] = panelsOnPrefab[3];
            return result;
        }
        #endregion
    }
}
