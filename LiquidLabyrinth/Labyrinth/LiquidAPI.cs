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

namespace LiquidLabyrinth.Labyrinth;

// TODO: Add API support.
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
        public virtual void OnContainerBreak(RaycastHit hit)
        {
            if (hit.transform.TryGetComponent(out PlayerControllerB player))
            {
                player.DamagePlayer(10, true, true, CauseOfDeath.Unknown, 0, false, default);
            }
        }
        public virtual void OnContainerBreak() { }
        public virtual void OnUpdate() { }
        public GameObject? Container { get; set; }
    }


    public static Dictionary<string ,Liquid> Registry = new Dictionary<string, Liquid>();

    public static Liquid RandomLiquid
    {
        get => Registry.ElementAt(Random.Range(0, Registry.Count)).Value;
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

    public static Liquid RegisterLiquid(Liquid liquid)
    {
        var callingAssembly = Assembly.GetCallingAssembly();
        var modDLL = callingAssembly.GetName().Name;
        liquid.ModName = modDLL;

        if (Registry.ContainsKey(liquid.ShortID))
        {
            Plugin.Logger.LogError($"Liquid {liquid.Name} from {liquid.ModName} already exists in your modded liquid registry! Try a different name!");
            return liquid;
        }
        Registry.Add(liquid.ShortID, liquid);
        Plugin.Logger.LogWarning($"Registered {liquid.Name} from {liquid.ModName} ({liquid.ShortID})");
        return liquid;
    }
}