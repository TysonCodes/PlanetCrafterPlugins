using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using static HarmonyLib.AccessTools;
using MijuTools;
using SpaceCraft;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TerraformLevel_Plugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Planet Crafter.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private static ConfigEntry<float> configOxygenAmount_ppq;
        private static ConfigEntry<float> configHeatAmount_pK;
        private static ConfigEntry<float> configPressureAmount_nPa;
        private static ConfigEntry<float> configBiomassAmount_g;
        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        private void Awake()
        {
            // Get configuration values
            configOxygenAmount_ppq = Config.Bind<float>("World_Units", "Oxygen_Amount_ppq", 0.0f, "Set oxygen amount (parts per quadrillion)");
            configHeatAmount_pK = Config.Bind<float>("World_Units", "Heat_Amount_pK", 0.0f, "Set heat amount (pico Kelvin)");
            configPressureAmount_nPa = Config.Bind<float>("World_Units", "Pressure_Amount_nPa", 0.0f, "Set pressure amount (nano Pascals)");
            configBiomassAmount_g = Config.Bind<float>("World_Units", "Biomass_Amount_g", 0.0f, "Set oxygen amount (grams)");

            harmony.PatchAll(typeof(TerraformLevel_Plugin.Plugin));
            // Manually patch WindowsHandler as it doesn't seem to work automatically.
            /*
            var original = HarmonyLib.AccessTools.Method(typeof(WorldUnitsHandler), "ComputeUnitsTick");
            var postfix = HarmonyLib.AccessTools.Method(typeof(TerraformLevel_Plugin.Plugin), "ComputeUnitsPostfix");
            var result = harmony.Patch(original, postfix: new HarmonyMethod(postfix));
            */

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(WorldUnit), "Compute")]
        private static bool WorldUnit_ComputePostfix(ref float ___currentTotalValue, ref DataConfig.WorldUnitType ___unitType)
        {
            switch(___unitType)
            {
                case DataConfig.WorldUnitType.Oxygen:
                    ___currentTotalValue = Plugin.configOxygenAmount_ppq.Value;
                    break;
                case DataConfig.WorldUnitType.Heat:
                    ___currentTotalValue = Plugin.configHeatAmount_pK.Value;
                    break;
                case DataConfig.WorldUnitType.Pressure:
                    ___currentTotalValue = Plugin.configPressureAmount_nPa.Value;
                    break;
                case DataConfig.WorldUnitType.Biomass:
                    ___currentTotalValue = Plugin.configBiomassAmount_g.Value;
                    break;
            }
            return true;
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
}
