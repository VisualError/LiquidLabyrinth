using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LethalSettings.UI;
using LethalSettings.UI.Components;
using LiquidLabyrinth.Patches;
using LiquidLabyrinth.Utilities;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using System.Linq;
using LiquidLabyrinth.ItemHelpers;

namespace LiquidLabyrinth;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("evaisa.lethallib", "0.10.1")]
[BepInDependency("com.willis.lc.lethalsettings", "1.2.2")]
[BepInProcess("Lethal Company.exe")]
internal class Plugin : BaseUnityPlugin
{

    // TODO: USE SaveLocalPlayerValues METHOD FROM GAMENETWORKMANAGER TO SAVE THE BOTTLE NAMES!
    internal static new ManualLogSource Logger;
    internal static Plugin Instance;
    internal ConfigEntry<bool> RevivePlayer;
    internal ConfigEntry<bool> NoGravityInOrbit;
    internal ConfigEntry<bool> IsGrabbableToEnemies;
    internal ConfigEntry<bool> UseSillyNames;
    internal ConfigEntry<bool> SetAsShopItems;
    internal ConfigEntry<int> BottleRarity;
    internal ConfigEntry<bool> spawnRandomEnemy;
    internal Dictionary<string, EnemyType> enemyTypes = new Dictionary<string,EnemyType>();
    internal int SliderValue;
    private readonly Harmony Harmony = new(MyPluginInfo.PLUGIN_GUID);
    internal string[] sillyNames = { "wah", "woh", "yippie", "whau", "wuh", "whuh", "auh", ":3" };
    internal List<HeadItem> headItemList = new List<HeadItem>();
    internal List<PotionBottle> bottleItemList = new List<PotionBottle>();

    private void NetcodeWeaver()
    {
        var types = Assembly.GetExecutingAssembly().GetTypes();
        foreach (var type in types)
        {
            var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (var method in methods)
            {
                var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                if (attributes.Length > 0)
                {
                    method.Invoke(null, null);
                }
            }
        }
    }

    private void Awake()
    {
        // Plugin startup logic
        Instance = this;
        Logger = base.Logger;
        NetcodeWeaver();
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!!!!!!!!");
        StringBuilder sb = new();
        sb.AppendLine();
        sb.AppendLine(" ___      ___   _______  __   __  ___   ______     ___      _______  _______  __   __  ______    ___   __    _  _______  __   __ ");
        sb.AppendLine("|   |    |   | |       ||  | |  ||   | |      |   |   |    |   _   ||  _    ||  | |  ||    _ |  |   | |  |  | ||       ||  | |  |");
        sb.AppendLine("|   |    |   | |   _   ||  | |  ||   | |  _    |  |   |    |  |_|  || |_|   ||  |_|  ||   | ||  |   | |   |_| ||_     _||  |_|  |");
        sb.AppendLine("|   |    |   | |  | |  ||  |_|  ||   | | | |   |  |   |    |       ||       ||       ||   |_||_ |   | |       |  |   |  |       |");
        sb.AppendLine("|   |___ |   | |  |_|  ||       ||   | | |_|   |  |   |___ |       ||  _   | |_     _||    __  ||   | |  _    |  |   |  |       |");
        sb.AppendLine("|       ||   | |      | |       ||   | |       |  |       ||   _   || |_|   |  |   |  |   |  | ||   | | | |   |  |   |  |   _   |");
        sb.AppendLine("|_______||___| |____||_||_______||___| |______|   |_______||__| |__||_______|  |___|  |___|  |_||___| |_|  |__|  |___|  |__| |__|");
        Logger.LogWarning(sb.ToString());


        OtherUtils.GenerateLayerMap();
        Harmony.PatchAll(typeof(GameNetworkManagerPatch));
        Harmony.PatchAll(typeof(PlayerControllerBPatch));
        Harmony.PatchAll(typeof(TerminalPatch));
        Harmony.PatchAll(typeof(StartOfRoundPatch));
        RevivePlayer = Config.Bind("General", "Toggle Bottle Revive", true, "Bottle revive functionality, for testing purposes");
        NoGravityInOrbit = Config.Bind("General", "Toggle Bottle Gravity In Orbit", true, "If Bottle Gravity is enabled/disabled during orbit.");
        IsGrabbableToEnemies = Config.Bind("General", "Toggle Enemy Pickups", true, "if enemies can pick up objects made by the mod");
        UseSillyNames = Config.Bind("Fun", "Use Silly Names", false, "Silly overlaod");
        SetAsShopItems = Config.Bind("Shop", "Set items as buyable", false, "[used for development] all registered items will become available to store.");
        BottleRarity = Config.Bind("Scraps", "Bottle Rarity", 60, "Set bottle rarity");
        spawnRandomEnemy = Config.Bind("Fun", "Spawn random enemy on revive", false, "[alpha only] revive has a chance to spawn enemy, let all enemies be spawned.");
        SliderValue = BottleRarity.Value;
        // Bundle loader.

        AssetLoader.LoadAssetBundles();
        try
        {
            ModMenu.RegisterMod(new ModMenu.ModSettingsConfig
            {
                Name = MyPluginInfo.PLUGIN_NAME,
                Id = MyPluginInfo.PLUGIN_GUID,
                Description = "Liquid Labyrinth: Mysterious liquids",
                MenuComponents = new MenuComponent[]
                {
                    new ToggleComponent
                    {
                        Value = RevivePlayer.Value,
                        Text = RevivePlayer.Description.Description,
                        OnValueChanged = (self, value) => RevivePlayer.Value = value
                    },
                    new ToggleComponent
                    {
                        Value = NoGravityInOrbit.Value,
                        Text = NoGravityInOrbit.Description.Description,
                        OnValueChanged = (self, value) => NoGravityInOrbit.Value = value
                    },
                    new ToggleComponent
                    {
                        Value = IsGrabbableToEnemies.Value,
                        Text = IsGrabbableToEnemies.Description.Description,
                        OnValueChanged = (self, value) => IsGrabbableToEnemies.Value = value
                    },
                    new ToggleComponent
                    {
                        Value = UseSillyNames.Value,
                        Text = UseSillyNames.Description.Description,
                        OnValueChanged = (self, value) => UseSillyNames.Value = value
                    },
                    new ToggleComponent
                    {
                        Value = SetAsShopItems.Value,
                        Text = SetAsShopItems.Description.Description,
                        OnValueChanged = (self, value) =>
                        {
                            SetAsShopItems.Value = value;
                            foreach(KeyValuePair<string, Object> obj in AssetLoader.assetsDictionary)
                            {
                                if(obj.Value is Item item)
                                {
                                    var shopItemExists = LethalLib.Modules.Items.shopItems.Any(si => si.item == item);
                                    if (value && !shopItemExists)
                                    {
                                        // Value is true and the item doesn't exist in the shopItems list, so add it.
                                        LethalLib.Modules.Items.ShopItem shopItem = new LethalLib.Modules.Items.ShopItem(item, null, null, null, -1); // -1 so price isn't 0.
                                        shopItem.modName = Assembly.GetExecutingAssembly().GetName().Name;
                                        LethalLib.Modules.Items.shopItems.Add(shopItem);
                                        Logger.LogWarning($"Adding: {shopItem.item.itemName}");
                                    }
                                    else if (!value && shopItemExists)
                                    {
                                        // Value is false and the item exists in the shopItems list, so remove it.
                                        var shopItemToRemove = LethalLib.Modules.Items.shopItems.FirstOrDefault(si => si.item == item);
                                        if (shopItemToRemove != null)
                                        {
                                            LethalLib.Modules.Items.shopItems.Remove(shopItemToRemove);
                                            Logger.LogWarning($"Removing: {shopItemToRemove.item.itemName}");
                                        }
                                    }
                                }
                            }
                        }
                    },
                    new ToggleComponent
                    {
                        Value = spawnRandomEnemy.Value,
                        Text = spawnRandomEnemy.Description.Description,
                        OnValueChanged = (self, value) => spawnRandomEnemy.Value = value
                    },
                    new VerticalComponent
                    {
                        Children = new MenuComponent[]
                        {
                            new SliderComponent
                            {
                                WholeNumbers = true,
                                MaxValue = 1000,
                                MinValue = 0,
                                Text = BottleRarity.Description.Description,
                                Value = BottleRarity.Value,
                                ShowValue = true,
                                OnValueChanged = (self, value) => BottleRarity.Value = (int)value
                            }
                        }
                    }
                }
            }); // TODO: Dynamically add components using reflection on bepinex configs;
        }
        catch(System.Exception err)
        {
            Logger.LogWarning("Couldn't load LethalSettings for Configs!");
            Logger.LogError(err);
        }
    }
}