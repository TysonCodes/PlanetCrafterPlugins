using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using UnityEngine;
using UnityEngine.UI;
using PluginFramework;

namespace Vehicle_Plugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Planet Crafter.exe")]
    [BepInDependency(PluginFramework.PluginInfo.PLUGIN_GUID, PluginFramework.PluginInfo.PLUGIN_VERSION)]    // In BepInEx 5.4.x this ia a minimum version, BepInEx 6.x has range semantics.
    public class Plugin : BaseUnityPlugin
    {
        private const string NAME_GO_ENTER_TRIGGER = "TriggerEnter";
        private static ManualLogSource bepInExLogger;
        private AssetBundle vehicleAssetBundle;
        private List<GameObject> assetBundleGameObjects = new List<GameObject>();
        private static List<GroupDataConstructible> assetBundleGroupDataConstructibles = new List<GroupDataConstructible>();
        private static Dictionary<string, GroupData> groupDataById = new Dictionary<string, GroupData>();

        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        private void Awake()
        {
            bepInExLogger = Logger;

            // Load vehicle AssetBundle
            vehicleAssetBundle = AssetBundle.LoadFromFile(Path.Combine(Paths.PluginPath, "spacecraft"));
            assetBundleGameObjects = new List<GameObject>(vehicleAssetBundle.LoadAllAssets<GameObject>());
            assetBundleGroupDataConstructibles = new List<GroupDataConstructible>(vehicleAssetBundle.LoadAllAssets<GroupDataConstructible>());

            // Modify the space craft to add scripts we need to add
            GameObject spaceCraftGO = assetBundleGameObjects.Find((GameObject go) => go.name == "SpaceCraft");
            if (spaceCraftGO != null)
            {
                spaceCraftGO.transform.Find(NAME_GO_ENTER_TRIGGER).gameObject.AddComponent<ActionEnterVehicle>();
            }

            harmony.PatchAll(typeof(Vehicle_Plugin.Plugin));

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

            foreach(var constructible in assetBundleGroupDataConstructibles)
            {
                AddGroupDataToList(ref ___groupsData, constructible);
            }

            return true;
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
            assetBundleGroupDataConstructibles = null;
            assetBundleGameObjects = null;
            if (vehicleAssetBundle != null)
            {
                vehicleAssetBundle.Unload(true);
            }            
            vehicleAssetBundle = null;
            harmony.UnpatchSelf();
        }
    }
    
}
