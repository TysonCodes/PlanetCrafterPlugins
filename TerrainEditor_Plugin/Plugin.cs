﻿using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using MijuTools;
using SpaceCraft;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TerrainEditor_Plugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Planet Crafter.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        private void Awake()
        {
            harmony.PatchAll(typeof(TerrainEditor_Plugin.Plugin));
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
}