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
using Unity.Netcode;
using UnityEngine;
using System.Linq;
using LiquidLabyrinth.ItemHelpers;

namespace LiquidLabyrinth
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("evaisa.lethallib", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.willis.lc.lethalsettings", BepInDependency.DependencyFlags.HardDependency)]
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
        private readonly Harmony Harmony = new(PluginInfo.PLUGIN_GUID);
        internal string[] sillyNames = { "wah", "woh", "yippie", "whau", "wuh", "whuh", "auh", ":3" };
        List<string> scientificNames = new List<string>()
        {
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
   "Einstein","DarkMatter",
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
        };
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
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!!!!!!!!");
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
            //Harmony.PatchAll(typeof(TerminalPatch));
            Harmony.PatchAll(typeof(StartOfRoundPatch));
            RevivePlayer = Config.Bind("General", "Toggle Bottle Revive", true, "Bottle revive functionality, for testing purposes");
            NoGravityInOrbit = Config.Bind("General", "Toggle Bottle Gravity In Orbit", true, "If Bottle Gravity is enabled/disabled during orbit.");
            IsGrabbableToEnemies = Config.Bind("General", "Toggle Enemy Pickups", true, "if enemies can pick up objects made by the mod");
            UseSillyNames = Config.Bind("Fun", "Use Silly Names", false, "Silly overlaod");
            SetAsShopItems = Config.Bind("Shop", "Set items as buyable", false, "[used for development] all registered items will become available to store.");
            BottleRarity = Config.Bind("Scraps", "Bottle Rarity", 60, "Set bottle rarity");
            spawnRandomEnemy = Config.Bind("Fun", "Spawn random enemy on revive", false, "[alpha only] revive has a chance to spawn enemy, let all enemies be spawned.");
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
                    Name = PluginInfo.PLUGIN_NAME,
                    Id = PluginInfo.PLUGIN_GUID,
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
}
