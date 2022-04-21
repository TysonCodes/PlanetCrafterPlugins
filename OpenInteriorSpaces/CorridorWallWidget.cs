using UnityEngine;
using SpaceCraft;
using PluginFramework;

namespace OpenInteriorSpaces_Plugin
{
    class CorridorWallWidget : MonoBehaviour
    {
        public GameObject[] originalWallAndColliders;
        public GameObject interiorCorridorWallAndColliders;

        private const string GAME_OBJECT_PATH_TO_FLOOR = "Container/4BlocRoom/Common/Floor/P_Floor_Tinny_02_LP";
        private const string GAME_OBJECT_PATH_TO_HALF_WALL = "Container/4BlocRoom/Common/P_Wall_Half_01";

        private static GameObject interiorCorridorWallAndCollidersOnPrefab;

        public void ShowOriginalWall()
        {
            SetOriginalWallAndColliderEnabled(true);
            interiorCorridorWallAndColliders.SetActive(false);
        }

        public void ShowInteriorWall()
        {
            SetOriginalWallAndColliderEnabled(false);
            interiorCorridorWallAndColliders.SetActive(true);
        }

        public static void InjectWidgetIntoCorridorWallPrefab(ref GameObject originalPrefab)
        {
            // Attach ourselves
            CorridorWallWidget widgetOnPrefab = originalPrefab.AddComponent<CorridorWallWidget>();

            // Find all the existing components
            widgetOnPrefab.originalWallAndColliders = GetChildGameObjects(originalPrefab);

            // Create and inject new floor/ceiling for corridors
            CreateNewInteriorCorridorWallAndCollidersOnPrefab();
            interiorCorridorWallAndCollidersOnPrefab.transform.SetParent(originalPrefab.transform);
            widgetOnPrefab.interiorCorridorWallAndColliders = interiorCorridorWallAndCollidersOnPrefab;
        }

        private static GameObject[] GetChildGameObjects(GameObject parent)
        {
            GameObject[] result = new GameObject[parent.transform.childCount];
            for (int i = 0; i < parent.transform.childCount; i++)
            {
                result[i] = parent.transform.GetChild(i).gameObject;
            }
            return result;
        }

        private static void CreateNewInteriorCorridorWallAndCollidersOnPrefab()
        {
            GameObject largeRoomGO = Framework.GameObjectByName["Pod4x"];
            GameObject commonFloorPanelGO = Instantiate(largeRoomGO.transform.Find(GAME_OBJECT_PATH_TO_FLOOR).gameObject, null);
            commonFloorPanelGO.transform.localPosition = Vector3.zero;
            commonFloorPanelGO.transform.localEulerAngles = Vector3.zero;
            commonFloorPanelGO.transform.localScale = Vector3.one;

            // Container for floor and ceiling.
            interiorCorridorWallAndCollidersOnPrefab = new GameObject("NewCeilingAndFloor");

            // Floor
            GameObject floorGO = new GameObject("Floor");
            floorGO.transform.SetParent(interiorCorridorWallAndCollidersOnPrefab.transform, false);
            floorGO.transform.localPosition = new Vector3(-1.0f, 0.0f, 0.0f);
            floorGO.transform.localEulerAngles = new Vector3(0.0f, 90.0f, 0.0f);
            floorGO.transform.localScale = new Vector3(1.0f, 1.0f, 0.5f);
            Instantiate(commonFloorPanelGO, floorGO.transform).name = commonFloorPanelGO.name;

            // Floor Surface (for building)
            GameObject floorSurfaceGO = new GameObject("FloorSurface");
            floorSurfaceGO.transform.SetParent(floorGO.transform, false);
            floorSurfaceGO.transform.localPosition = new Vector3(2.5f, 0.0f, 1.0f);
            floorSurfaceGO.transform.localEulerAngles = new Vector3(0.0f, -90.0f, 0.0f);
            floorSurfaceGO.transform.localScale = new Vector3(2.0f, 1.0f, 1.0f);
            BoxCollider floorSurfaceCollider = floorSurfaceGO.AddComponent<BoxCollider>();
            floorSurfaceCollider.isTrigger = true;
            floorSurfaceCollider.center = new Vector3(0.0f, -0.048232f, 0.0f);
            floorSurfaceCollider.size = new Vector3(1.0f, 0.1681314f, 7.0f);
            floorSurfaceGO.AddComponent<HomemadeTag>().homemadeTag = DataConfig.HomemadeTag.SurfaceFloor;

            // Ceiling
            GameObject ceilingGO = Instantiate(floorGO, interiorCorridorWallAndCollidersOnPrefab.transform);
            ceilingGO.name = "Ceiling";
            ceilingGO.transform.localPosition = new Vector3(0.0f, 4.0f, 0.0f);
            ceilingGO.transform.localEulerAngles = new Vector3(180.0f, 90.0f, 0.0f);
            ceilingGO.transform.localScale = new Vector3(1.0f, 1.0f, 0.5f);
        }

        private void SetOriginalWallAndColliderEnabled(bool enabled)
        {
            foreach (GameObject gameObject in originalWallAndColliders)
            {
                gameObject.SetActive(enabled);
            }
        }
    }
}