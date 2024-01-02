using System.Reflection;
using UnityEngine;

namespace LiquidLabyrinth.Labyrinth;

// TODO: Add API support.
public class Liquids
{
    public class LiquidConfiguration
    {
        public string Name { get; set; }
        internal string ModName { set; get; }
        public Color color { get; set; }
        public string Description { get; set; }
        public object[] CustomArguments { get; set; }
    }

    public static void RegisterLiquid(LiquidConfiguration liquid)
    {
        var callingAssembly = Assembly.GetCallingAssembly();
        var modDLL = callingAssembly.GetName().Name;
        liquid.ModName = modDLL;
    }
}