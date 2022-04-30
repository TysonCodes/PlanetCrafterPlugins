using System;
using System.Collections.Generic;
using BepInEx;
using UnityEngine;
using PluginFramework;

namespace FixBeacon_Plugin
{
    [System.Serializable]
    public class BuildingList
    {
        public List<string> buildingGameObjectNames;
    }

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Planet Crafter.exe")]
    [BepInDependency(PluginFramework.PluginInfo.PLUGIN_GUID, PluginFramework.PluginInfo.PLUGIN_VERSION)]    // In BepInEx 5.4.x this ia a minimum version, BepInEx 6.x has range semantics.
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Framework.StaticGroupDataIndexed += OnStaticGroupDataIndexed;

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void OnStaticGroupDataIndexed()
        {
            try
            {
                AdjustBeaconIcon();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Caught exception '{ex.Message}' trying to adjust the beacon icon.");
            }
        }

        private void AdjustBeaconIcon()
        {
            GameObject beacon = Framework.GameObjectByName["Beacon1"];
        }
    }
}
