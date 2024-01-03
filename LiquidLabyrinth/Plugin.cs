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
using LiquidLabyrinth.ItemHelpers;
using System.Globalization;
using System;

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
    internal ConfigEntry<bool> SetAsShopItems;
    internal ConfigEntry<bool> UseCustomNameList;
    internal ConfigEntry<int> BottleRarity;
    internal ConfigEntry<bool> spawnRandomEnemy;
    internal ConfigEntry<string> customNameList;
    internal Dictionary<string, EnemyType> enemyTypes = new();

    internal Dictionary<Type, int> SaveableItemDict = new();
    internal int SliderValue;
    private readonly Harmony Harmony = new(MyPluginInfo.PLUGIN_GUID);
    string nameList = "";
    List<string> scientificNames = [
        "Quasarion Helium",
        "Neutronium Carbon",
        "Protonium Oxygen",
        "Electronium Nitrogen",
        "Photonium Phosphorus",
        "Gravitonium Sulfur",
        "Meteorium Chlorine",
        "Cosmonium Snezium",
        "Quantumium Red",
        "Subatomicium Blue",
        "Interstellarium Green",
        "Galacticium Purple",
        "Cosmicium Yellow",
        "Starlightium Pink",
        "Nebulonium Teal",
        "Cometium Brown",
        "Asteroidium Grey",
        "Planetium Black ",
        "Galaxyon White",
        "Universeon Silver",
        "Multiverseon Gold",
        "Parallelon Bronze",
        "Dimensionon Copper",
        "Timeon Zinc",
        "Spaceon Tin",
        "Realityon Lead",
        "Existenceon Nickel",
        "Infinityon Aluminum",
        "Eternityon Iron",
        "Immortalityon Steel",
        // Periodic Elements
        "Helium", "Neon", "Argon", "Krypton", "Xenon", "Radon",
        "Beryllium", "Magnesium", "Calcium", "Strontium", "Barium",
        "Hafnium", "Tantalum", "Tungsten", "Rhenium", "Osmium",
        "Iridium", "Platinum", "Gold", "Silver", "Cadmium",
        "Indium", "Tin", "Antimony", "Tellurium", "Polonium",
        "Astatine", "Francium", "Radium", "Actinium", "Thorium",
        "Protactinium", "Uranium", "Neptunium", "Plutonium", "Americium",
        "Curium", "Berkelium", "Californium", "Einsteinium", "Fermium",
        "Mendelevium", "Nobelium", "Lawrencium", "Rutherfordium", "Dubnium",
        "Seaborgium", "Bohrium", "Hassium", "Meitnerium", "Ununnilium",
        "Unununium", "Ununvium", "Ununhexium", "Ununseptium", "Ununoctium",
        // Scientific Concepts
        "Quantum Physics", "Relativity", "Newtonian Physics", "Schrodinger Equation",
        "Einstein", "DarkMatter",
        "Dark Energy", "Quantum Entanglement", "Higgs Boson",
        "GravitationalWave", "Wormhole", "Tachyon", "Singularity", "EventHorizon",
        "Neutrino", "Muon", "Lepton", "Quark", "Gluon",
        "Atom", "ElectronShell", "Proton", "Electron", "NuclearFusion",
        "Nuclear Fission", "AtomicBond", "Isotope", "RadioactiveDecay", "Alpha Particle",
        "Beta Particle", "GammaRay", "Microwave", "Radio Wave", "GammaRay", "X-Ray",
        "Ultraviolet Ray", "Visible Light", "Infrared Ray", "Microwave", "RadioWave",
        "Cosmic Ray", "Hubble Constant",
        "Big Crunch", "Heat Death", "Black Hole", "Quasar", "Pulsar",
        "Nebula", "Galaxy", "Star Cluster", "Exoplanet", "Solar Wind",
        "Solar Flare", "Nova", "Supernova", "Pulsar", "White Dwarf",
        "Red Giant", "Blue Giant", "Star System"
    ];
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
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        StringBuilder sb = new();
        sb.AppendLine();
        sb.AppendLine("           [-]  ");
        sb.AppendLine("         .-'-'-.");
        sb.AppendLine("         :-...-:");
        sb.AppendLine("         |;:   |");
        sb.AppendLine("         |;:.._|");
        sb.AppendLine("         `-...-'");
        sb.AppendLine(" Liquid Labyrinth Loaded!");
        Logger.LogWarning(sb.ToString());


        OtherUtils.GenerateLayerMap();
        Harmony.PatchAll(typeof(GameNetworkManagerPatch));
        Harmony.PatchAll(typeof(PlayerControllerBPatch));
        Harmony.PatchAll(typeof(StartOfRoundPatch));
        Harmony.PatchAll(typeof(GrabbableObjectPatch));
        RevivePlayer = Config.Bind("General", "Toggle Bottle Revive", true, "Bottle revive functionality, for testing purposes");
        NoGravityInOrbit = Config.Bind("General", "Toggle Bottle Gravity In Orbit", true, "If Bottle Gravity is enabled/disabled during orbit.");
        IsGrabbableToEnemies = Config.Bind("General", "Toggle Enemy Pickups", false, "if enemies can pick up objects made by the mod");
        SetAsShopItems = Config.Bind("Shop", "Set items as buyable", false, "[host only] all registered items will become available to store.");
        BottleRarity = Config.Bind("Scraps", "Bottle Rarity", 60, "Set bottle rarity [Needs game restart.]");
        spawnRandomEnemy = Config.Bind("Fun", "Spawn random enemy on revive", false, "[alpha only] Allow all enemy types to be spawned when revive fail");
        UseCustomNameList = Config.Bind("Fun", "Use Custom Name List", false, "Set to true if you wan't to use your custom name list for bottles.");
        customNameList = Config.Bind("Fun", "Custom Bottle Name List", "", "Custom name list of your bottles. use (\",\") as a seperator.");
        customNameList.Value = "";
        SliderValue = BottleRarity.Value;
        foreach (var name in scientificNames)
        {
            MarkovChain.TrainMarkovChain(name);
        }
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
                        Value = SetAsShopItems.Value,
                        Text = SetAsShopItems.Description.Description,
                        OnValueChanged = (self, value) =>
                        {
                            SetAsShopItems.Value = value;
                            Logger.LogWarning($"set value: {SetAsShopItems.Value}");
                        }
                    },
                    new ToggleComponent
                    {
                        Value = spawnRandomEnemy.Value, 
                        Text = spawnRandomEnemy.Description.Description,
                        OnValueChanged = (self, value) => spawnRandomEnemy.Value = value
                    },
                    new HorizontalComponent
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
                    },
                    new ToggleComponent
                    {
                        Value = UseCustomNameList.Value,
                        Text = UseCustomNameList.Description.Description,
                        OnValueChanged = (self, value) => UseCustomNameList.Value = value
                    },
                    new HorizontalComponent
                    {
                        ChildAlignment = TextAnchor.MiddleCenter,
                        Children = new MenuComponent[]
                        {
                            new InputComponent
                            {
                                Placeholder="[currently doesn't work]",
                                Value=customNameList.Value,
                                OnValueChanged = (self, value) => nameList = value
                            },
                            new ButtonComponent
                            {
                                Text = "Add Custom Name",
                                OnClick = (self) => customNameList.Value += $",{nameList}" // this might break thiingsssss
                            }
                        }
                    },
                    new VerticalComponent
                    {
                        Children = new MenuComponent[]
                        {
                            new LabelComponent
                            {
                                Text=string.Join("\n",customNameList.Value.Split(","))
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