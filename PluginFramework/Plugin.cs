using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MijuTools;
using SpaceCraft;
using UnityEngine;

namespace PluginFramework
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Planet Crafter.exe")]
    public class Framework : BaseUnityPlugin
    {
        private static ManualLogSource bepInExLogger;

        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        private static List<GroupData> gameGroupData;
        private static Dictionary<string, GroupData> groupDataById = new Dictionary<string, GroupData>();
        private static Dictionary<string, TerraformStage> terraformStageById = new Dictionary<string, TerraformStage>();
        private static Dictionary<string, GameObject> gameObjectById = new Dictionary<string, GameObject>();
        private static Dictionary<string, Sprite> iconById = new Dictionary<string, Sprite>();

        private void Awake()
        {
            bepInExLogger = Logger;

            harmony.PatchAll(typeof(PluginFramework.Framework));

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StaticDataHandler), "LoadStaticData")]
        private static bool StaticDataHandler_LoadStaticData_Prefix(ref List<GroupData> ___groupsData)
        {
            // Save reference to group data.
            gameGroupData = ___groupsData;

            IndexExistingGroupData();
            LoadTerraformStages();

            return true;
        }

        private static void IndexExistingGroupData()
        {
            foreach (var groupData in gameGroupData)
            {
                groupDataById[groupData.id] = groupData;
                if (groupData.associatedGameObject)
                {
                    gameObjectById[groupData.associatedGameObject.name] = groupData.associatedGameObject;
                }
                if (groupData.icon)
                {
                    iconById[groupData.icon.name] = groupData.icon;
                }
            }
            bepInExLogger.LogInfo($"Created index of previous group data. Size = {groupDataById.Count}");
        }

        private static void LoadTerraformStages()
        {
            if (Managers.GetManager<TerraformStagesHandler>())
            {
                List<TerraformStage> terraformStages = Managers.GetManager<TerraformStagesHandler>().GetAllTerraGlobalStages();
                foreach (TerraformStage stage in terraformStages)
                {
                    terraformStageById[stage.GetTerraId()] = stage;
                }
            }
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
}
