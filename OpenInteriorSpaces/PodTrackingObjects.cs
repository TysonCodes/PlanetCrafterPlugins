using System;
using System.Collections.Generic;
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
                        podByDirection[panel.Key] = adjacentPod;
                        adjacentPod.AddAdjacentPod(contiguousPanel, this);
                    }
                    else
                    {
                        Plugin.bepInExLogger.LogInfo($"Found continguous panel that is not on a Pod.");
                    }
                }
            }
        }

        public void AddAdjacentPod(Panel panel, PodInfo podInfo)
        {
            foreach (var selfPanels in panelByDirection)
            {
                if (selfPanels.Value == panel)
                {
                    podByDirection[selfPanels.Key] = podInfo;
                    // TODO: Update pillar and panel info
                }   
            }
        }

        public void GeneratePillarInfo()
        {
            pillarsByDirection[PillarDirection.PillarFrontLeft] = new PillarInfo(this, PodDirection.PodLeft, PodDirection.PodFront);
            pillarsByDirection[PillarDirection.PillarFrontRight] = new PillarInfo(this, PodDirection.PodFront, PodDirection.PodRight);
            pillarsByDirection[PillarDirection.PillarBackRight] = new PillarInfo(this, PodDirection.PodRight, PodDirection.PodBack);
            pillarsByDirection[PillarDirection.PillarBackLeft] = new PillarInfo(this, PodDirection.PodBack, PodDirection.PodLeft);
        }
    }

    public class PillarInfo
    {
        public bool IsInterior {get; private set;} = false;
        public PodInfo interiorPod;
        public List<PodInfo> borderingPods = new List<PodInfo>();
        public PodInfo oppositePod;

        public PillarInfo(PodInfo interiorPod, PodDirection oneBorder, PodDirection otherBorder)
        {
            this.interiorPod = interiorPod;
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
                    }
                }
            }
        }
    }
}
