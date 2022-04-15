using System;
using System.Collections.Generic;
using System.Linq;
using SpaceCraft;
using UnityEngine;

namespace OpenInteriorSpaces_Plugin
{
    public enum PodDirection {PodFront, PodRight, PodBack, PodLeft};
    public enum PillarDirection { PillarFrontLeft, PillarFrontRight, PillarBackRight, PillarBackLeft};

    public class PodInfo
    {
        public WorldObject associatedWorldObj;
        public GameObject associatedGameObj;
        public Dictionary<PodDirection, Panel> panelByDirection = new Dictionary<PodDirection, Panel>();
        public Dictionary<PodDirection, PodInfo> podByDirection = new Dictionary<PodDirection, PodInfo>();
        public Dictionary<PillarDirection, PillarInfo> pillarsByDirection = new Dictionary<PillarDirection, PillarInfo>();

        public static Dictionary<Panel, PodInfo> podInfoByPanel = new Dictionary<Panel, PodInfo>();

        private const float DETECT_DISTANCE = 2.0f;

        public void DetectAdjacentPods()
        {
            foreach (var panel in panelByDirection)
            {
                Panel contiguousPanel = null;
                if (contiguousPanel = panel.Value.GetContingousPanels(DETECT_DISTANCE))
                {
                    if (podInfoByPanel.ContainsKey(contiguousPanel))
                    {
                        PodInfo adjacentPod = podInfoByPanel[contiguousPanel];
                        AddAdjacentPod(panel.Key, adjacentPod);
                        PodDirection adjacentPodDirectionToUs = adjacentPod.panelByDirection.FirstOrDefault(x => x.Value == contiguousPanel).Key;
                        adjacentPod.AddAdjacentPod(adjacentPodDirectionToUs, this);
                    }
                    else
                    {
                        Plugin.bepInExLogger.LogInfo($"Found continguous panel that is not on a Pod.");
                    }
                }
            }
        }

        public void AddAdjacentPod(PodDirection direction, PodInfo podInfo)
        {
            Plugin.bepInExLogger.LogInfo($"Adding adjacent pod from '{this.associatedWorldObj.GetId()}' to '{podInfo.associatedWorldObj.GetId()}' in direction '{direction}'.");
            podByDirection[direction] = podInfo;
            // TODO: This can be cleaner.
            switch (direction)
            {
                case PodDirection.PodFront:
                    pillarsByDirection[PillarDirection.PillarFrontLeft].borderingPods.Add(podInfo);
                    pillarsByDirection[PillarDirection.PillarFrontLeft].CheckForOppositePod();
                    pillarsByDirection[PillarDirection.PillarFrontRight].borderingPods.Add(podInfo);
                    pillarsByDirection[PillarDirection.PillarFrontRight].CheckForOppositePod();
                    break;
                case PodDirection.PodRight:
                    pillarsByDirection[PillarDirection.PillarBackRight].borderingPods.Add(podInfo);
                    pillarsByDirection[PillarDirection.PillarBackRight].CheckForOppositePod();
                    pillarsByDirection[PillarDirection.PillarFrontRight].borderingPods.Add(podInfo);
                    pillarsByDirection[PillarDirection.PillarFrontRight].CheckForOppositePod();
                    break;
                case PodDirection.PodBack:
                    pillarsByDirection[PillarDirection.PillarBackLeft].borderingPods.Add(podInfo);
                    pillarsByDirection[PillarDirection.PillarBackLeft].CheckForOppositePod();
                    pillarsByDirection[PillarDirection.PillarBackRight].borderingPods.Add(podInfo);
                    pillarsByDirection[PillarDirection.PillarBackRight].CheckForOppositePod();
                    break;
                case PodDirection.PodLeft:
                    pillarsByDirection[PillarDirection.PillarFrontLeft].borderingPods.Add(podInfo);
                    pillarsByDirection[PillarDirection.PillarFrontLeft].CheckForOppositePod();
                    pillarsByDirection[PillarDirection.PillarBackLeft].borderingPods.Add(podInfo);
                    pillarsByDirection[PillarDirection.PillarBackLeft].CheckForOppositePod();
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
                    }
                }
            }
        }
    }
}
