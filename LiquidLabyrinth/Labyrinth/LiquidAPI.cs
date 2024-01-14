using LiquidLabyrinth.ItemHelpers;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using System.Drawing;
using Color = UnityEngine.Color;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using LiquidLabyrinth.Labyrinth.Monobehaviours;
using GameNetcodeStuff;
using Unity.Netcode;
using LiquidLabyrinth.Labyrinth.NetworkBehaviours;

namespace LiquidLabyrinth.Labyrinth;

// TODO: Add API support.
// TODO: I DONT KNOW WHAT I AM DOING. MAKE SURE YOU KNOW WHAT YOURE DOING AND FIX WHAT YOU KNOW YOU DONT KNOW YOU'RE DOING WHEN YOU TOUCH THIS FILE. THANKS - PAST ME.

// Credits: Most of the code in this API is based off on People Playground's way of handling liquids. Will change if needed.
public class LiquidAPI
{
    [Serializable]
    public abstract class Liquid
    {
        public virtual string? Name { get => GetType().Name; }
        internal string? ModName { set; get; }
        internal string LiquidID { get => HashString($"{ModName}-{Name}"); }
        internal string ShortID { get => LiquidID.Substring(0, 4) + LiquidID.Substring(LiquidID.Length - 4, 4); }
        public abstract Color Color { get; }
        public virtual string? Description { get; }
        public abstract void OnEnterLimb(LimbBehaviour limb);
        public abstract void OnEnterContainer(Container container);
        public abstract void OnExitContainer(Container container);
        public virtual void OnContainerBreak(Container container, RaycastHit hit)
        {
            if (hit.transform.TryGetComponent(out PlayerControllerB player))
            {
                player.DamagePlayer(10, true, true, CauseOfDeath.Unknown, 0, false, default);
            }
        }
        public virtual void OnContainerBreak(Container container) { }
        public virtual void OnUpdate(Container container) { }
    }


    public static readonly Dictionary<string, Liquid> Registry = new Dictionary<string, Liquid>();
    public static readonly HashSet<Liquid> LiquidSet = new HashSet<Liquid>();

    public static Liquid RandomLiquid
    {
        get => Registry.ElementAt(Random.Range(0, Registry.Count)).Value;
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

    public static Liquid? GetByID(string ID)
    {
        Liquid? liquid = Registry.TryGetValue(ID, out Liquid value) ? value : null;
        if(liquid == null)
        {
            Plugin.Logger.LogError($"There is no Liquid registered with the ID: {ID}");
        }
        return liquid;
    }

    public static List<Liquid> GetByIDs(List<string> ID)
    {
        List<Liquid> liquidList = new List<Liquid>();
        foreach(string id in ID) 
        {
            Liquid? liquid = GetByID(id);
            if (liquid == null) continue;
            liquidList.Add(liquid);
        }
        return liquidList;
    }

    private static string HashString(string strng)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(strng));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }

    public static Liquid? RegisterLiquid(Liquid liquid)
    {
        var callingAssembly = Assembly.GetCallingAssembly();
        var modDLL = callingAssembly.GetName().Name;
        liquid.ModName = modDLL;

        if (Registry.ContainsKey(liquid.ShortID))
        {
            Plugin.Logger.LogError($"Liquid Registry Error: Liquid \"{liquid.Name}\" ({liquid.ShortID}) from {liquid.ModName} already exists in your modded liquid registry! Liquid Registry failed!");
            return null;
        }
        Registry.Add(liquid.ShortID, liquid);
        LiquidSet.Add(liquid);
        Plugin.Logger.LogInfo($"Registered {liquid.Name} from {liquid.ModName} ({liquid.ShortID})");
        return liquid;
    }
}