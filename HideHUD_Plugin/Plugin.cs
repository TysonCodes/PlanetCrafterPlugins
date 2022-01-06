using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using MijuTools;
using SpaceCraft;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HideHUD_Plugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Planet Crafter.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private ConfigEntry<Key> configToggleHotkey;
        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        private static bool initialized = false;
        private bool hudEnabled = true;
        private static BaseHudHandler hudHandler;
        private static CanvasEarlyAccess earlyAccessCanvas;
        private static GameObject playerArmor;
        private static List<GameObject> toHideWhenInWindow;

        private void Awake()
        {
            // Plugin startup logic
            configToggleHotkey = Config.Bind("General", "Toggle_HUD_Hotkey", Key.F2, "Pick the key to use to toggle between showing and hiding the HUD");

            // Manually patch WindowsHandler as it doesn't seem to work automatically.
            var original = HarmonyLib.AccessTools.Method(typeof(WindowsHandler), "Start");
            var postfix = HarmonyLib.AccessTools.Method(typeof(HideHUD_Plugin.Plugin), "Init");
            var result = harmony.Patch(original, postfix: new HarmonyMethod(postfix));

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private static void Init()
        {
            toHideWhenInWindow = new List<GameObject>(GameObject.Find("WindowsHandler").GetComponentInChildren<WindowsHandler>(true).toHideWhenInWindow);
			hudHandler = Managers.GetManager<BaseHudHandler>();
			earlyAccessCanvas = Managers.GetManager<CanvasEarlyAccess>();
			playerArmor = GameObject.Find("Player").GetComponentInChildren<PlayerTEMPFIXANIM>().playerArmor;
            initialized = true;
        }

        private void Update()
		{
            if (!initialized)
            {
                return;
            }
            if (Keyboard.current[configToggleHotkey.Value].wasPressedThisFrame)
			{
				hudEnabled = !hudEnabled;
				if (hudEnabled)
				{
					SetHudVisible(hudEnabled);
				}
			}
			if (!hudEnabled)
			{
				SetHudVisible(hudEnabled);
			}
		}
		private void SetHudVisible(bool visible)
		{
			hudHandler.gameObject.SetActive(hudEnabled);
			foreach (GameObject gameObject in toHideWhenInWindow)
			{
				if (gameObject != null)
				{
					gameObject.SetActive(hudEnabled);
				}
			}
			earlyAccessCanvas.gameObject.SetActive(visible);
			if (playerArmor != null)
			{
				playerArmor.SetActive(visible);
			}
		}
        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
}
