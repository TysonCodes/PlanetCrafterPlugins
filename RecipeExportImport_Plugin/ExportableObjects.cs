using System;
using System.Collections.Generic;
using SpaceCraft;
using UnityEngine;

[Serializable]
class ExportableGroupData
{
    public string id;
    public string associatedGameObject;
    public string icon;
    public List<string> recipeIngredients;
    public bool hideInCrafter;
    public DataConfig.WorldUnitType unlockingWorldUnit;
    public float unlockingValue;
    public string terraformStageUnlock;
    public int inventorySize;

    protected ExportableGroupData(GroupData data)
    {
        id = data.id;
        associatedGameObject = data.associatedGameObject ? data.associatedGameObject.name : "None";
        icon = data.icon ? data.icon.name : "None";
        recipeIngredients = new List<string>(data.recipeIngredients.Count);
        foreach (GroupDataItem ingredient in data.recipeIngredients)
        {
            recipeIngredients.Add(ingredient.id);
        }
        hideInCrafter = data.hideInCrafter;
        unlockingWorldUnit = data.unlockingWorldUnit;
        unlockingValue = data.unlockingValue;
        terraformStageUnlock = data.terraformStageUnlock ? data.terraformStageUnlock.GetTerraId() : "None";
        inventorySize = data.inventorySize;
    }
}

[Serializable]
class ExportableGroupDataItem : ExportableGroupData
{
    public int value;
    public List<DataConfig.CraftableIn> craftableInList;
    public DataConfig.EquipableType equipableType;
    public DataConfig.UsableType usableType;
    public DataConfig.ItemCategory itemCategory;
    public string growableGroup;
    public List<string> associatedGroups;
    public bool assignRandomGroupAtSpawn;
    public bool replaceByRandomGroupAtSpawn;
    public float unitMultiplierOxygen;
    public float unitMultiplierPressure;
    public float unitMultiplierHeat;
    public float unitMultiplierEnergy;
    public float unitMultiplierBiomass;

    public ExportableGroupDataItem(GroupDataItem item) : base(item)
    {
        value = item.value;
        craftableInList = item.craftableInList;
        equipableType = item.equipableType;
        usableType = item.usableType;
        itemCategory = item.itemCategory;
        growableGroup = item.growableGroup ? item.growableGroup.id : "None";
        associatedGroups = new List<string>(item.associatedGroups.Count);
        foreach (GroupData data in item.associatedGroups)
        {
            associatedGroups.Add(data.id);
        }
        assignRandomGroupAtSpawn = item.assignRandomGroupAtSpawn;
        replaceByRandomGroupAtSpawn = item.replaceByRandomGroupAtSpawn;
        unitMultiplierOxygen = item.unitMultiplierOxygen;
        unitMultiplierPressure = item.unitMultiplierPressure;
        unitMultiplierHeat = item.unitMultiplierHeat;
        unitMultiplierEnergy = item.unitMultiplierEnergy;
        unitMultiplierBiomass = item.unitMultiplierBiomass;
    }
}

[Serializable]
class ExportableGroupDataConstructible : ExportableGroupData
{
    public float unitGenerationOxygen;
    public float unitGenerationPressure;
    public float unitGenerationHeat;
    public float unitGenerationEnergy;
    public float unitGenerationBiomass;
    public bool rotationFixed;
    public DataConfig.GroupCategory groupCategory;
    public DataConfig.WorldUnitType worlUnitMultiplied;

    public ExportableGroupDataConstructible(GroupDataConstructible constructible) : base(constructible)
    {
        unitGenerationOxygen = constructible.unitGenerationOxygen;
        unitGenerationPressure = constructible.unitGenerationPressure;
        unitGenerationHeat = constructible.unitGenerationHeat;
        unitGenerationEnergy = constructible.unitGenerationEnergy;
        unitGenerationBiomass = constructible.unitGenerationBiomass;
        rotationFixed = constructible.rotationFixed;
        groupCategory = constructible.groupCategory;
        worlUnitMultiplied = constructible.worlUnitMultiplied;
    }
}