using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
/*using LethalSettings.UI;
using LethalSettings.UI.Components;*/
using LethalConfig;
using LiquidLabyrinth.Utilities;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using System;
using LiquidLabyrinth.Labyrinth;
using System.Linq;
using LiquidLabyrinth.Labyrinth.Liquids;
using LethalConfig.ConfigItems.Options;
using LethalConfig.ConfigItems;
using LiquidLabyrinth.ItemHelpers;

namespace LiquidLabyrinth;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("evaisa.lethallib", "0.13.0")]
[BepInDependency("ainavt.lc.lethalconfig", "1.3.3")]
[BepInProcess("Lethal Company.exe")]
internal class Plugin : BaseUnityPlugin
{
    // TODO: USE SaveLocalPlayerValues METHOD FROM GAMENETWORKMANAGER TO SAVE THE BOTTLE NAMES!
    internal static new ManualLogSource Logger;
    internal static Plugin Instance;
    internal ConfigEntry<bool> RevivePlayer;
    internal ConfigEntry<bool> NoGravityInOrbit;
    internal ConfigEntry<bool> IsGrabbableToEnemies;
    internal ConfigEntry<int> BottleRarity;
    internal ConfigEntry<bool> spawnRandomEnemy;
    internal Dictionary<Type, EnemyType> enemyTypes = new();
    internal List<GameObject> cursed = new List<GameObject>();

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
        "Helium",
        "Neon",
        "Argon",
        "Krypton",
        "Xenon",
        "Radon",
        "Beryllium",
        "Magnesium",
        "Calcium",
        "Strontium",
        "Barium",
        "Hafnium",
        "Tantalum",
        "Tungsten",
        "Rhenium",
        "Osmium",
        "Iridium",
        "Platinum",
        "Gold",
        "Silver",
        "Cadmium",
        "Indium",
        "Tin",
        "Antimony",
        "Tellurium",
        "Polonium",
        "Astatine",
        "Francium",
        "Radium",
        "Actinium",
        "Thorium",
        "Protactinium",
        "Uranium",
        "Neptunium",
        "Plutonium",
        "Americium",
        "Curium",
        "Berkelium",
        "Californium",
        "Einsteinium",
        "Fermium",
        "Mendelevium",
        "Nobelium",
        "Lawrencium",
        "Rutherfordium",
        "Dubnium",
        "Seaborgium",
        "Bohrium",
        "Hassium",
        "Meitnerium",
        "Ununnilium",
        "Unununium",
        "Ununvium",
        "Ununhexium",
        "Ununseptium",
        "Ununoctium",
        // Scientific Concepts
        "Quantum Physics",
        "Relativity",
        "Newtonian Physics",
        "Schrodinger Equation",
        "Einstein",
        "Dark Matter",
        "Dark Energy",
        "Quantum Entanglement",
        "Higgs Boson",
        "Gravitational Wave",
        "Wormhole",
        "Tachyon",
        "Singularity",
        "Event Horizon",
        "Neutrino",
        "Muon",
        "Lepton",
        "Quark",
        "Gluon",
        "Atom",
        "Electron Shell",
        "Proton",
        "Electron",
        "Nuclear Fusion",
        "Nuclear Fission",
        "Atomic Bond",
        "Isotope",
        "Radioactive Decay",
        "Alpha Particle",
        "Beta Particle",
        "Gamma Ray",
        "Microwave",
        "Radio Wave",
        "X-Ray",
        "Ultraviolet Ray",
        "Visible Light",
        "Infrared Ray",
        "Cosmic Ray",
        "Hubble Constant",
        "Big Crunch",
        "Heat Death",
        "Black Hole",
        "Quasar",
        "Nebula",
        "Galaxy",
        "Star Cluster",
        "Exoplanet",
        "Solar Wind",
        "Solar Flare",
        "Nova",
        "Supernova",
        "Pulsar",
        "White Dwarf",
        "Red Giant",
        "Blue Giant",
        "Star System"
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
        var patchClasses = Assembly.GetExecutingAssembly().GetTypes().Where(type => type.Namespace == "LiquidLabyrinth.Patches");
        var liquidAPIPatches = Assembly.GetExecutingAssembly().GetTypes().Where(type => type.Namespace == "LiquidLabyrinth.Labyrinth.LiquidAPI_Patches");
        foreach (var patchClass in patchClasses)
        {
            Logger.LogInfo($"Patching {patchClass.Name}");
            Harmony.CreateAndPatchAll(patchClass);
            Logger.LogInfo($"Patched {patchClass.Name}");
        }
        foreach(var patch in liquidAPIPatches)
        {
            Logger.LogInfo($"LIQUID API PATCHING {patch.Name}");
            Harmony.CreateAndPatchAll(patch);
            Logger.LogInfo($"LIQUID API PATCHED {patch.Name}");
        }
        RevivePlayer = Config.Bind("General", "Toggle Bottle Revive", true, "Bottle revive functionality, for testing purposes");
        NoGravityInOrbit = Config.Bind("General", "Toggle Bottle Gravity In Orbit", true, "If Bottle Gravity is enabled/disabled during orbit.");
        IsGrabbableToEnemies = Config.Bind("General", "Toggle Enemy Pickups", false, "if enemies can pick up objects made by the mod");
        BottleRarity = Config.Bind("Scraps", "Bottle Rarity", 60, "Set bottle rarity [Needs game restart.]");
        spawnRandomEnemy = Config.Bind("Fun", "Spawn random enemy on revive", false, "[alpha only] Allow all enemy types to be spawned when revive fail");
        SliderValue = BottleRarity.Value;
        foreach (var name in scientificNames)
        {
            MarkovChain.TrainMarkovChain(name);
        }
        // Bundle loader.
        AssetLoader.LoadAssetBundles();

        // Liquid API
        LiquidAPI.RegisterLiquid(new ExplosiveLiquid());
        LiquidAPI.RegisterLiquid(new ReviveLiquid());
        LiquidAPI.RegisterLiquid(new Funny());
        LiquidAPI.RegisterLiquid(new TestLiquid());

        NoGravityInOrbit.SettingChanged += NoGravityInOrbit_SettingChanged;

        // Lethal Config.
        try
        {
            var RaritySlider = new IntSliderConfigItem(BottleRarity, new IntSliderOptions
            {
                Min = 0,
                Max = 10000,
                RequiresRestart = true,
            });
            LethalConfigManager.AddConfigItem(RaritySlider);
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(NoGravityInOrbit, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(IsGrabbableToEnemies, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(RevivePlayer, false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(spawnRandomEnemy, false));
        }
        catch(System.Exception err)
        {
            Logger.LogWarning("Couldn't load LethalConfig for Configs!");
            Logger.LogError(err);
        }
    }
    public void NoGravityInOrbit_SettingChanged(object sender, EventArgs e)
    {
        foreach (GrabbableRigidbody grab in GameObject.FindObjectsByType<GrabbableRigidbody>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!NoGravityInOrbit.Value) return;
            grab.rb.AddForce(Vector3.up*0.1f, ForceMode.Impulse); // float!
        }
    }
}