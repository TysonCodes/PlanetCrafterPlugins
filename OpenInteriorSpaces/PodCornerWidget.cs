using UnityEngine;
using SpaceCraft;
using PluginFramework;

namespace OpenInteriorSpaces_Plugin
{
    public class PodCornerWidget : MonoBehaviour
    {
        enum PodCornerType {PCT_InnerAngle, PCT_OuterAngle, PCT_InteriorEmtpy, PCT_InteriorPillar, PCT_Wall_CW, PCT_Wall_CCW};

        // Objects at this corner to turn on/of depending on situation
        public GameObject originalCorner;
        public GameObject invertedCorner;
        public GameObject topBottomCorner;
        public GameObject solidPillarCorner;
        public GameObject wallCW;
        public GameObject wallCCW;
        public GameObject blockerForWallCW;
        public GameObject blockerForWallCCW;

        // Situational awareness for pillar to determine what to turn on/off
        public PodWidget podWidget;
        public Panel wallFromPillarCW;
        public Panel wallFromPillarCCW;
        public PodDirection directionOfWallCW;
        public PodDirection directionOfWallCCW;
        private PillarInfo associatedPillar;

        private const string GAME_OBJECT_TO_GET_FLOOR_FROM = "Biolab";
        private const string GAME_OBJECT_PATH_TO_FLOOR = "Container/4BlocRoom/Common/Floor/P_Floor_Tinny_02_LP";
        private const string GAME_OBJECT_PATH_TO_HALF_WALL = "Container/4BlocRoom/Common/P_Wall_Half_01";
        private const string GAME_OBJECT_PATH_TO_STRUCTURE = "Container/Structure";

        public void Initialize()
        {
            wallFromPillarCW = podWidget.panelByLocalDirection[(int)directionOfWallCW];
            wallFromPillarCCW = podWidget.panelByLocalDirection[(int)directionOfWallCCW];
        }

        public void SetAssociatedPillar(PillarInfo pillarInfo)
        {
            associatedPillar = pillarInfo;
        }

        public void UpdateDisplay()
        {
            // TODO: How do I know if an interior wall changes?
            if (associatedPillar == null)
            {
                return;
            }

            PodCornerType newCornerType = PodCornerType.PCT_InnerAngle;
            blockerForWallCW.SetActive(false);
            blockerForWallCCW.SetActive(false);
            if (associatedPillar.IsInterior)
            {
                if (BothWallsAreCorridors())
                {
                    newCornerType = PodCornerType.PCT_InteriorEmtpy;
                }
                else
                {
                    newCornerType = PodCornerType.PCT_InteriorPillar;
                }
            }
            else
            {
                // Technically for all of these 'are corridors' checks they need to be 'interior corridors' but if they are not then they'll cover
                // up the whole corner making this moot anyways.
                if (BothWallsAreCorridors())
                {
                    newCornerType = PodCornerType.PCT_OuterAngle;
                }
                else if (CWIsCorridor())
                {
                    newCornerType = PodCornerType.PCT_Wall_CW;
                    if (AdjacentWallIsNotCorridor(directionOfWallCW))
                    {
                        blockerForWallCW.SetActive(true);
                    }
                }
                else if (CCWIsCorridor())
                {
                    newCornerType = PodCornerType.PCT_Wall_CCW;
                    if (AdjacentWallIsNotCorridor(directionOfWallCCW))
                    {
                        blockerForWallCCW.SetActive(true);
                    }
                }
            }
            Plugin.bepInExLogger.LogDebug($"Updating pillar at {originalCorner.transform.position} to {newCornerType}");
            SetVisibleCornerType(newCornerType);
        }

        private bool BothWallsAreCorridors()
        {
            return CWIsCorridor() && CCWIsCorridor();
        }

        private bool CWIsCorridor()
        {
            return wallFromPillarCW.subPanelType == DataConfig.BuildPanelSubType.WallCorridor;
        }

        private bool CCWIsCorridor()
        {
            return wallFromPillarCCW.subPanelType == DataConfig.BuildPanelSubType.WallCorridor;
        }

        private bool AdjacentWallIsNotCorridor(PodDirection localDirection)
        {
            return !podWidget.AdjacentWallIsCorridor(localDirection);
        }

        private void SetVisibleCornerType(PodCornerType type)
        {
            HideAllCornerTypes();
            switch(type)
            {
                case PodCornerType.PCT_InnerAngle:
                    originalCorner.SetActive(true);
                    break;
                case PodCornerType.PCT_OuterAngle:
                    invertedCorner.SetActive(true);
                    topBottomCorner.SetActive(true);
                    break;
                case PodCornerType.PCT_InteriorEmtpy:
                    topBottomCorner.SetActive(true);
                    break;
                case PodCornerType.PCT_InteriorPillar:
                    solidPillarCorner.SetActive(true);
                    break;
                case PodCornerType.PCT_Wall_CW:
                    wallCW.SetActive(true);
                    break;
                case PodCornerType.PCT_Wall_CCW:
                    wallCCW.SetActive(true);
                    break;
                default:
                    break;
            }
        }

        private void HideAllCornerTypes()
        {
            originalCorner.SetActive(false);
            invertedCorner.SetActive(false);
            topBottomCorner.SetActive(false);
            solidPillarCorner.SetActive(false);
            wallCW.SetActive(false);
            wallCCW.SetActive(false);
        }

        #region CreateGameObjects
        public static PodCornerWidget[] InjectNewObjectsIntoPrefab(ref GameObject podPrefab, ref PodWidget podWidget)
        {
            // Pillars are called 'Wall_Angle_03' inside 'Structure' inside 'Container' in the Pod gameobject.
            // They have capsule colliders on them.
            // They are ordered locally - BackRight, BackLeft, FrontRight, FrontLeft
            Transform structureGOTransform = podPrefab.transform.Find(GAME_OBJECT_PATH_TO_STRUCTURE);
            CapsuleCollider[] pillarStructures = structureGOTransform.GetComponentsInChildren<CapsuleCollider>();
            PodCornerWidget[] result = new PodCornerWidget[4];
            result[(int)PillarDirection.PillarBackRight] = CreatePodCornerWidget(structureGOTransform, pillarStructures[0].gameObject, 
                PodDirection.PodBack, PodDirection.PodRight, ref podWidget);
            result[(int)PillarDirection.PillarBackLeft] = CreatePodCornerWidget(structureGOTransform, pillarStructures[1].gameObject, 
                PodDirection.PodLeft, PodDirection.PodBack, ref podWidget);
            result[(int)PillarDirection.PillarFrontRight] = CreatePodCornerWidget(structureGOTransform, pillarStructures[2].gameObject, 
                PodDirection.PodRight, PodDirection.PodFront, ref podWidget);
            result[(int)PillarDirection.PillarFrontLeft] = CreatePodCornerWidget(structureGOTransform, pillarStructures[3].gameObject, 
                PodDirection.PodFront, PodDirection.PodLeft, ref podWidget);
            return result;
        }

        private static PodCornerWidget CreatePodCornerWidget(Transform structureGOTransform, GameObject originalPillar, PodDirection wallCWDirection, 
            PodDirection wallCCWDirection, ref PodWidget podWidget)
        {
            GameObject podCornerContainer = new GameObject("PodCorner");
            podCornerContainer.transform.SetParent(originalPillar.transform, false);
            PodCornerWidget result = podCornerContainer.AddComponent<PodCornerWidget>();
            result.originalCorner = originalPillar;
            result.directionOfWallCW = wallCWDirection;
            result.directionOfWallCCW = wallCCWDirection;
            result.podWidget = podWidget;
            podCornerContainer.transform.SetParent(structureGOTransform, true);
            originalPillar.transform.SetParent(podCornerContainer.transform, true);
            CreateGameObjectsUnderPodContainer(ref podCornerContainer, ref result);
            return result;
        }

        private static void CreateGameObjectsUnderPodContainer(ref GameObject podCornerContainer, ref PodCornerWidget cornerWidget)
        {
            CreateTopBottomGameObject(podCornerContainer.transform, ref cornerWidget);
            CreateInvertedCornerGameObject(podCornerContainer.transform, ref cornerWidget);
            CreateSolidPillarGameObject(podCornerContainer.transform, ref cornerWidget);
            CreateWallGameObjects(podCornerContainer.transform, ref cornerWidget);
            CreateWallBlockerGameObjects(podCornerContainer.transform, ref cornerWidget);
        }

        private static void CreateTopBottomGameObject(Transform podCornerContainerTransform, ref PodCornerWidget cornerWidget)
        {
            GameObject floorCeilingSourceGO = Framework.GameObjectByName[GAME_OBJECT_TO_GET_FLOOR_FROM];
            GameObject commonFloorPanelGO = Object.Instantiate(floorCeilingSourceGO.transform.Find(GAME_OBJECT_PATH_TO_FLOOR).gameObject, null);
            commonFloorPanelGO.transform.localPosition = Vector3.zero;
            commonFloorPanelGO.transform.localEulerAngles = Vector3.zero;
            commonFloorPanelGO.transform.localScale = Vector3.one;

            // Container for floor and ceiling.
            cornerWidget.topBottomCorner = new GameObject("CeilingAndFloor");
            cornerWidget.topBottomCorner.transform.SetParent(podCornerContainerTransform, false);

            // Floor
            GameObject floorGO = new GameObject("Floor");
            floorGO.transform.SetParent(cornerWidget.topBottomCorner.transform, false);
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
            GameObject ceilingGO = Object.Instantiate(floorGO, cornerWidget.topBottomCorner.transform);
            ceilingGO.name = "Ceiling";
            ceilingGO.transform.localPosition = new Vector3(-1.0f, 1.0f, 4.0f);
            ceilingGO.transform.localEulerAngles = new Vector3(180.0f, -90.0f, 90.0f);
            ceilingGO.transform.localScale = new Vector3(0.169f, 1.0f, 0.5f);
            cornerWidget.topBottomCorner.SetActive(false);
        }

        private static void CreateInvertedCornerGameObject(Transform podCornerContainerTransform, ref PodCornerWidget cornerWidget)
        {
            cornerWidget.invertedCorner = Object.Instantiate(cornerWidget.originalCorner, podCornerContainerTransform);
            cornerWidget.invertedCorner.transform.localPosition = new Vector3(-1.0f, 1.0f, 0.0f);
            cornerWidget.invertedCorner.transform.localEulerAngles = new Vector3(0.0f, 0.0f, 180.0f);
            cornerWidget.invertedCorner.SetActive(false);
        }

        private static void CreateSolidPillarGameObject(Transform podCornerContainerTransform, ref PodCornerWidget cornerWidget)
        {
            cornerWidget.solidPillarCorner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cornerWidget.solidPillarCorner.transform.SetParent(podCornerContainerTransform, false);
            cornerWidget.solidPillarCorner.transform.localPosition = new Vector3(-0.5f, 0.5f, 2.0f);
            cornerWidget.solidPillarCorner.transform.localEulerAngles = Vector3.zero;
            cornerWidget.solidPillarCorner.transform.localScale = new Vector3(1.0f, 1.0f, 4.0f);
            cornerWidget.solidPillarCorner.SetActive(false);
        }

        private static void CreateWallGameObjects(Transform podCornerContainerTransform, ref PodCornerWidget cornerWidget)
        {
            GameObject halfWallSourceGO = Framework.GameObjectByName[GAME_OBJECT_TO_GET_FLOOR_FROM];
            GameObject commonHalfWallGO = Instantiate(halfWallSourceGO.transform.Find(GAME_OBJECT_PATH_TO_HALF_WALL).gameObject, null);
            commonHalfWallGO.transform.localPosition = Vector3.zero;
            commonHalfWallGO.transform.localEulerAngles = Vector3.zero;
            commonHalfWallGO.transform.localScale = Vector3.one;
            commonHalfWallGO.name = "HalfWall";

            // WallCW
            cornerWidget.wallCW = new GameObject("WallCW");
            cornerWidget.wallCW.transform.SetParent(podCornerContainerTransform, false);
            cornerWidget.wallCW.transform.localPosition = new Vector3(-1.0f, 0.0f, 0.0f);
            cornerWidget.wallCW.transform.localEulerAngles = new Vector3(0.0f, -90.0f, -90.0f);
            cornerWidget.wallCW.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f / 3.0f);
            Instantiate(commonHalfWallGO, cornerWidget.wallCW.transform).name = commonHalfWallGO.name;
            cornerWidget.wallCW.SetActive(false);


            // WallCW
            cornerWidget.wallCCW = new GameObject("WallCCW");
            cornerWidget.wallCCW.transform.SetParent(podCornerContainerTransform, false);
            cornerWidget.wallCCW.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
            cornerWidget.wallCCW.transform.localEulerAngles = new Vector3(90.0f, 0.0f, 0.0f);
            cornerWidget.wallCCW.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f / 3.0f);
            Instantiate(commonHalfWallGO, cornerWidget.wallCCW.transform).name = commonHalfWallGO.name;
            cornerWidget.wallCCW.SetActive(false);
        }

        private static void CreateWallBlockerGameObjects(Transform podCornerContainerTransform, ref PodCornerWidget cornerWidget)
        {
            cornerWidget.blockerForWallCW = GameObject.CreatePrimitive(PrimitiveType.Quad);
            cornerWidget.blockerForWallCW.transform.SetParent(podCornerContainerTransform, false);
            cornerWidget.blockerForWallCW.transform.localPosition = new Vector3(-1.0f, 0.5f, 2.0f);
            cornerWidget.blockerForWallCW.transform.localEulerAngles = new Vector3(0.0f, -90.0f, 0.0f);
            cornerWidget.blockerForWallCW.transform.localScale = new Vector3(4.0f, 1.0f, 1.0f);
            cornerWidget.blockerForWallCW.SetActive(false);

            cornerWidget.blockerForWallCCW = GameObject.CreatePrimitive(PrimitiveType.Quad);
            cornerWidget.blockerForWallCCW.transform.SetParent(podCornerContainerTransform, false);
            cornerWidget.blockerForWallCCW.transform.localPosition = new Vector3(-0.5f, 1.0f, 2.0f);
            cornerWidget.blockerForWallCCW.transform.localEulerAngles = new Vector3(-90.0f, -90.0f, 0.0f);
            cornerWidget.blockerForWallCCW.transform.localScale = new Vector3(4.0f, 1.0f, 1.0f);
            cornerWidget.blockerForWallCCW.SetActive(false);
        }
        #endregion
    }
}
