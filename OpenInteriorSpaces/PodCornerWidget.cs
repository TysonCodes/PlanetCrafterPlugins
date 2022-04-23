using UnityEngine;
using SpaceCraft;
using PluginFramework;

namespace OpenInteriorSpaces_Plugin
{
    class PodCornerWidget : MonoBehaviour
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

        // TODO: Do I need a default constructor for cloning of this via Instantiate?
        
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
            CreateWallGameObjects();
            CreateWallBlockerGameObjects();
        }

        public void SetAssociatedPillar(PillarInfo pillarInfo)
        {
            associatedPillar = pillarInfo;
        }

        public void UpdateDisplay()
        {
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
                    newCornerType = PodCornerType.PCT_Wall_CCW;
                    if (AdjacentWallIsNotCorridor(directionOfWallCCW))
                    {
                        blockerForWallCCW.SetActive(false);
                    }
                }
                else if (CCWIsCorridor())
                {
                    newCornerType = PodCornerType.PCT_Wall_CW;
                    if (AdjacentWallIsNotCorridor(directionOfWallCW))
                    {
                        blockerForWallCW.SetActive(false);
                    }
                }
            }
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
            // TODO: Ask pod widget if the pod in the adjacent direction exists and if it does if the corresponding panel is a corridor.
            throw new System.NotImplementedException();
            return false;
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

        private void CreateWallGameObjects()
        {
            throw new System.NotImplementedException();
        }

        private void CreateWallBlockerGameObjects()
        {
            throw new System.NotImplementedException();
        }

        private void RepositionAndHideNewGameObject(GameObject gameObject)
        {
            gameObject.transform.SetParent(originalCorner.transform.parent, true);
            gameObject.SetActive(false);
        }
        #endregion
    }
}
