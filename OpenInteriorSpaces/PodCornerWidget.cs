using UnityEngine;
using SpaceCraft;
using PluginFramework;

namespace OpenInteriorSpaces_Plugin
{
    class PodCornerWidget : MonoBehaviour
    {
        // Objects at this corner to turn on/of depending on situation
        public GameObject originalCorner;
        public GameObject invertedCorner;
        public GameObject topBottomCorner;
        public GameObject solidPillarCorner;
        //public GameObject blockerForWallCW;   // TODO: Create this blocker.
        //public GameObject blockerForWallCCW;  // TODO: Create this blocker.

        // Situational awareness for pillar to determine what to turn on/off
        public PodWidget podWidget;
        public Panel wallFromPillarCW;
        public Panel wallFromPillarCCW;
        public PodDirection directionOfWallCW;
        public PodDirection directionOfWallCCW;

        private const string GAME_OBJECT_TO_GET_FLOOR_FROM = "Biolab";
        private const string GAME_OBJECT_PATH_TO_FLOOR = "Container/4BlocRoom/Common/Floor/P_Floor_Tinny_02_LP";

        public PodCornerWidget(GameObject original, PodWidget pod, PodDirection wallDirectionCW, PodDirection wallDirectionCCW)
        {
            originalCorner = original;
            podWidget = pod;
            wallFromPillarCW = pod.panelByLocalDirection[(int)wallDirectionCW];
            wallFromPillarCCW = pod.panelByLocalDirection[(int)wallDirectionCCW];
            directionOfWallCW = wallDirectionCW;
            directionOfWallCCW = wallDirectionCCW;

            // Create new game objects for different situations.
            CreateTopBottomGameObject();
            CreateInvertedCornerGameObject();
            CreateSolidPillarGameObject();

            // TODO: Figure out how to populate the rest and add functions to change the appearance of the pod corner.
            // TODO: Possibly have this calculate the right thing to do.
        }

        private void CreateTopBottomGameObject()
        {
            GameObject largeRoomGO = Framework.GameObjectByName[GAME_OBJECT_TO_GET_FLOOR_FROM];
            GameObject commonFloorPanelGO = Object.Instantiate(largeRoomGO.transform.Find(GAME_OBJECT_PATH_TO_FLOOR).gameObject, null);
            commonFloorPanelGO.transform.localPosition = Vector3.zero;
            commonFloorPanelGO.transform.localEulerAngles = Vector3.zero;
            commonFloorPanelGO.transform.localScale = Vector3.one;

            // Container for floor and ceiling.
            topBottomCorner = new GameObject("CeilingAndFloor");
            topBottomCorner.transform.SetParent(originalCorner.transform);

            // Floor
            GameObject floorGO = new GameObject("Floor");
            floorGO.transform.SetParent(topBottomCorner.transform, false);
            floorGO.transform.localPosition = new Vector3(0.0f, 1.0f, 0.0f);
            floorGO.transform.localEulerAngles = new Vector3(0.0f, -90.0f, -90.0f);
            floorGO.transform.localScale = new Vector3(0.169f, 1.0f, 0.5f);
            Object.Instantiate(commonFloorPanelGO, floorGO.transform).name = commonFloorPanelGO.name;

            // Floor Surface (for building)
            GameObject floorSurfaceGO = new GameObject("FloorSurface");
            floorSurfaceGO.transform.SetParent(floorGO.transform, false);
            floorSurfaceGO.transform.localPosition = new Vector3(3.0f, 0.0f, 1.0f);
            floorSurfaceGO.transform.localEulerAngles = new Vector3(0.0f, -90.0f, 0.0f);
            floorSurfaceGO.transform.localScale = new Vector3(2.0f, 1.0f, 0.85f);
            BoxCollider floorSurfaceCollider = floorSurfaceGO.AddComponent<BoxCollider>();
            floorSurfaceCollider.isTrigger = true;
            floorSurfaceCollider.center = new Vector3(0.0f, -0.048232f, 0.0f);
            floorSurfaceCollider.size = new Vector3(1.0f, 0.1681314f, 7.0f);
            floorSurfaceGO.AddComponent<HomemadeTag>().homemadeTag = DataConfig.HomemadeTag.SurfaceFloor;

            // Ceiling
            GameObject ceilingGO = Object.Instantiate(floorGO, topBottomCorner.transform);
            ceilingGO.name = "Ceiling";
            ceilingGO.transform.localPosition = new Vector3(-1.0f, 1.0f, 4.0f);
            ceilingGO.transform.localEulerAngles = new Vector3(180.0f, -90.0f, 90.0f);
            ceilingGO.transform.localScale = new Vector3(0.169f, 1.0f, 0.5f);

            RepositionAndHideNewGameObject(topBottomCorner);
        }

        private void CreateInvertedCornerGameObject()
        {
            invertedCorner = Object.Instantiate(originalCorner, originalCorner.transform);
            invertedCorner.transform.localPosition = new Vector3(-1.0f, 1.0f, 0.0f);
            invertedCorner.transform.localEulerAngles = new Vector3(0.0f, 0.0f, 180.0f);

            RepositionAndHideNewGameObject(invertedCorner);
        }

        private void CreateSolidPillarGameObject()
        {
            solidPillarCorner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            solidPillarCorner.transform.SetParent(originalCorner.transform, false);
            solidPillarCorner.transform.localPosition = new Vector3(-0.5f, 0.5f, 2.0f);
            solidPillarCorner.transform.localEulerAngles = Vector3.zero;
            solidPillarCorner.transform.localScale = new Vector3(1.0f, 1.0f, 4.0f);

            RepositionAndHideNewGameObject(solidPillarCorner);
        }

        private void RepositionAndHideNewGameObject(GameObject gameObject)
        {
            gameObject.transform.SetParent(originalCorner.transform.parent, true);
            gameObject.SetActive(false);
        }
    }
}
