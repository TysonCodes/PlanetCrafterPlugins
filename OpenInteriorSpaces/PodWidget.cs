using System.Collections.Generic;
using UnityEngine;
using SpaceCraft;
using PluginFramework;

namespace OpenInteriorSpaces_Plugin
{
    class PodWidget : MonoBehaviour
    {
        public Panel[] panelByLocalDirection;
        public PodCornerWidget[] podCornerByLocalDirection;

        private const string POD_GAME_OBJECT_NAME = "Pod";
        private const string GAME_OBJECT_PATH_TO_STRUCTURE = "Container/Structure";

        public static void InjectWidgetIntoPodPrefab()
        {
            GameObject podPrefab = Framework.GameObjectByName[POD_GAME_OBJECT_NAME];

            // Attach ourselves
            PodWidget widgetOnPrefab = podPrefab.AddComponent<PodWidget>();

            // Get the panel objects into the array
            widgetOnPrefab.panelByLocalDirection = GetReferencesToPanels(podPrefab);

            // Create corners and attach to widget
            widgetOnPrefab.podCornerByLocalDirection = CreatePodCornerWidgets(ref podPrefab, widgetOnPrefab);
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

        private static PodCornerWidget[] CreatePodCornerWidgets(ref GameObject podPrefab, PodWidget prefabWidget)
        {
            // Pillars are called 'Wall_Angle_03' inside 'Structure' inside 'Container' in the Pod gameobject.
            // They have capsule colliders on them.
            // They are ordered locally - BackRight, BackLeft, FrontRight, FrontLeft
            Transform structureGameObject = podPrefab.transform.Find(GAME_OBJECT_PATH_TO_STRUCTURE);
            CapsuleCollider[] pillarStructures = structureGameObject.GetComponentsInChildren<CapsuleCollider>();
            PodCornerWidget[] result = new PodCornerWidget[4];
            result[(int)PillarDirection.PillarBackRight] = new PodCornerWidget(pillarStructures[0].gameObject, prefabWidget, PodDirection.PodBack, PodDirection.PodRight);
            result[(int)PillarDirection.PillarBackLeft] = new PodCornerWidget(pillarStructures[1].gameObject, prefabWidget, PodDirection.PodLeft, PodDirection.PodBack);
            result[(int)PillarDirection.PillarFrontRight] = new PodCornerWidget(pillarStructures[2].gameObject, prefabWidget, PodDirection.PodRight, PodDirection.PodFront);
            result[(int)PillarDirection.PillarFrontLeft] = new PodCornerWidget(pillarStructures[3].gameObject, prefabWidget, PodDirection.PodFront, PodDirection.PodLeft);
            return result;
        }
    }
}
