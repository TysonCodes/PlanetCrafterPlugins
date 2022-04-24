using System;
using System.Collections.Generic;
using System.Linq;
using SpaceCraft;
using UnityEngine;

namespace OpenInteriorSpaces_Plugin
{
    public enum PodDirection {PodFront, PodRight, PodBack, PodLeft};    // Local +Z, +X, -Z, -X
    public enum PillarDirection { PillarFrontLeft, PillarFrontRight, PillarBackRight, PillarBackLeft};
    public delegate void Trigger();

    public class PillarInfo
    {
        public bool IsInterior {get; private set;} = false;

        public event Trigger IsInteriorChanged;

        private const int PILLAR_OFFSET_FROM_POD_CENTER = 40;

        private static Dictionary<Vector3Int, PillarInfo> pillarInfoByLocation = new Dictionary<Vector3Int, PillarInfo>();

        private Vector3Int position;
        private Dictionary<PillarDirection, PodWidget> borderingPodsByDirectionFromPod = new Dictionary<PillarDirection, PodWidget>();

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

        public void AddBorderingPod(PodWidget podToAdd, PillarDirection globalDirectionFromPodToPillar)
        {
            borderingPodsByDirectionFromPod[globalDirectionFromPodToPillar] = podToAdd;
            RecalculateInterior();
        }

        public void RemoveBorderingPod(PodWidget podToRemove)
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
                IsInteriorChanged?.Invoke();
            }
        }
    }
}
