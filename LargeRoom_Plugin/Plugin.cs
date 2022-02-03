using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MijuTools;
using SpaceCraft;
using UnityEngine;
using UnityEngine.Rendering;

namespace LargeRoom_Plugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Planet Crafter.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private static ManualLogSource bepInExLogger;
        private static Sprite largeRoomIcon;
        private static VolumeProfile podSharedVolumeProfile;
        private static Dictionary<string, GroupData> groupDataById = new Dictionary<string, GroupData>();
        private const string LARGE_ROOM_ICON_FILE_NAME = "LargeRoom_Plugin.LargeRoomIcon-Small.png";

        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        private void Awake()
        {
            bepInExLogger = Logger;

            // Load the embedded icon image and convert to sprite
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream iconFileStream = assembly.GetManifestResourceStream(LARGE_ROOM_ICON_FILE_NAME);
            MemoryStream iconFileMemoryStream = new MemoryStream();
            iconFileStream.CopyTo(iconFileMemoryStream);
            Texture2D iconImageTexture = new Texture2D(0,0);
            iconImageTexture.LoadImage(iconFileMemoryStream.ToArray(), false);
            largeRoomIcon = Sprite.Create(iconImageTexture, new Rect(0.0f, 0.0f, 256.0f, 256.0f), new Vector2(0.5f, 0.5f), 64.0f);

            harmony.PatchAll(typeof(LargeRoom_Plugin.Plugin));

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StaticDataHandler), "LoadStaticData")]
        private static bool StaticDataHandler_LoadStaticData_Prefix(ref List<GroupData> ___groupsData)  
        {
            // Index all of the existing group data
            foreach (var groupData in ___groupsData)
            {
                groupDataById[groupData.id] = groupData;
            }
            bepInExLogger.LogInfo($"Created index of previous group data. Size = {groupDataById.Count}");

            // Copy the Pod GroupDataConstructible and modify it
            GroupData BiolabGD = groupDataById["Biolab"];
            GroupDataConstructible largeRoomGDC = Instantiate<GroupDataConstructible>(groupDataById["pod"] as GroupDataConstructible);
            GameObject podGO = largeRoomGDC.associatedGameObject;
            largeRoomGDC.associatedGameObject = BiolabGD.associatedGameObject;
            largeRoomGDC.icon = largeRoomIcon;
            largeRoomGDC.name = "LargeRoom";
            largeRoomGDC.id = "LargeRoom";
            GroupDataItem iron = groupDataById["Iron"] as GroupDataItem;
            GroupDataItem magnesium = groupDataById["Magnesium"] as GroupDataItem;
            GroupDataItem aluminum = groupDataById["Aluminium"] as GroupDataItem;
            largeRoomGDC.recipeIngredients = new List<GroupDataItem>(){
                    iron, iron, iron, iron, magnesium, magnesium, aluminum, aluminum            
                };

            // Add to the list of groups            
            AddGroupDataToList(ref ___groupsData, largeRoomGDC);

            // Copy the window wall to add the biolab wall
            var panelsResources = Managers.GetManager<PanelsResources>();
            if (panelsResources != null)
            {
                GroupDataConstructible biolabWallGDC = Instantiate<GroupDataConstructible>(groupDataById["window"] as GroupDataConstructible);
                biolabWallGDC.associatedGameObject =panelsResources.GetPanelGameObject(DataConfig.BuildPanelSubType.WallLab);
                biolabWallGDC.name = "BiolabWall";
                biolabWallGDC.id = "BiolabWall";

                // Add to the list of groups
                AddGroupDataToList(ref ___groupsData, biolabWallGDC);

                // Save to PanelsResources
                panelsResources.panelsGroupItems[9] = groupDataById[biolabWallGDC.id] as GroupDataConstructible;
            }

            // Save the pod post processing profile
            Volume podVolume = podGO.GetComponentInChildren<Volume>();
            podSharedVolumeProfile = podVolume.sharedProfile;

            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(WorldObjectsHandler), "InstantiateWorldObject")]
        private static void WorldObjectsHandler_InstantiateWorldObject_Postfix(WorldObject _worldObject, bool _fromDb, ref GameObject __result)
        {
            if (_worldObject.GetGroup() != null && _worldObject.GetGroup().id == "LargeRoom")
            {
                // Hide content
                __result.transform.Find("Container/Content").gameObject.SetActive(false);

                // Add point lights to all the pod centers.
                GameObject lightToAdd = new GameObject("Point Light");
                lightToAdd.transform.localPosition = new Vector3(0.04f, 3.1f, 2.86f);
                Light lightComponent = lightToAdd.AddComponent<Light>();
                lightComponent.range = 8.0f;
                lightComponent.intensity = 5.0f;
                lightComponent.shadows = LightShadows.Soft;
                lightComponent.shadowBias = 0.02f;
                lightComponent.shadowNormalBias = 0.1f;
                lightComponent.shadowNearPlane = 0.1f;
                Transform podsParent = __result.transform.Find("Container/4BlocRoom");
                for (int i = 0; i < podsParent.childCount; i++)
                {
                    if (podsParent.GetChild(i).gameObject.name.Contains("Pod"))
                    {
                        Instantiate<GameObject>(lightToAdd, podsParent.GetChild(i));
                    }
                }
                Destroy(lightToAdd);

                // Set the VolumeProfile to match Pod
                __result.GetComponentInChildren<Volume>().sharedProfile = podSharedVolumeProfile;
            }
        }

        private static void AddGroupDataToList(ref List<GroupData> groupsData, GroupData toAdd)
        {
            bool alreadyExists = groupDataById.ContainsKey(toAdd.id);
            if (!alreadyExists)
            {
                bepInExLogger.LogInfo($"Adding {toAdd.id} to group data.");
                groupsData.Add(toAdd);
                groupDataById[toAdd.id] = toAdd;
            }   
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
    
}
