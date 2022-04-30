using BepInEx;
using UnityEngine;
using SpaceCraft;
using HarmonyLib;

namespace FixBeacon_Plugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Planet Crafter.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        private void Awake()
        {
            harmony.PatchAll(typeof(FixBeacon_Plugin.Plugin));

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerLookable), "SetFov")]
        private static void PlayerLookable_SetFov_Postfix(Camera ___camera)
        {
            if (___camera != null)
            {
                Camera childCamera = ___camera.transform.Find("CameraWorldUi").GetComponent<Camera>();
                if (childCamera != null)
                {
                    childCamera.fieldOfView = ___camera.fieldOfView;
                }
            }
        }
    }
}
