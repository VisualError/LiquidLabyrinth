using LiquidLabyrinth.ItemHelpers;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using System.Drawing;
using Color = UnityEngine.Color;

namespace LiquidLabyrinth.Labyrinth;

// TODO: Add API support.
public class LiquidAPI
{
    private static List<Liquid> registeredLiquids = new List<Liquid>();

    public static Liquid RandomLiquid
    {
        get => registeredLiquids[Random.Range(0, registeredLiquids.Count)];
    }

    public static Color CombineColor(List<Liquid> liquids)
    {
        float r = 0, g = 0, b = 0, a = 0;
        foreach (Liquid liquid in liquids)
        {
            r += liquid.Color.r;
            g += liquid.Color.g;
            b += liquid.Color.b;
            a += liquid.Color.a;
        }
        int count = liquids.Count;
        return new Color(r / count, g / count, b / count);
    }

    public static List<Liquid> RandomLiquids(int num)
    {
        List<Liquid> liquids = new List<Liquid>();
        for(int i=0; i < num; i++)
        {
            liquids.Add(RandomLiquid);
        }
        return liquids;
    }

    [Serializable]
    public class Liquid
    {
        public string? Name { get; set; }
        internal string? ModName { set; get; }
        internal string LiquidID { get => $"{ModName}-{Name}"; }
        public Color Color { get; set; }
        public string? Description { get; set; }
        internal GameObject? Container { get; set; }
    }

    public static Liquid GetByID(string ID)
    {
        return registeredLiquids.Find(match => match.LiquidID.Equals(ID));
    }

    public static List<Liquid> GetByIDs(List<string> ID)
    {
        List<Liquid> liquidList = new List<Liquid>();
        foreach(string id in ID) 
        {
            liquidList.Add(GetByID(id));
        }
        return liquidList;
    }

    public static Liquid RegisterLiquid(Liquid liquid)
    {
        var callingAssembly = Assembly.GetCallingAssembly();
        var modDLL = callingAssembly.GetName().Name;
        liquid.ModName = modDLL;

        registeredLiquids.Add(liquid);

        Plugin.Logger.LogWarning($"Registered {liquid.Name} from {liquid.ModName}");
        return liquid;
    }
}