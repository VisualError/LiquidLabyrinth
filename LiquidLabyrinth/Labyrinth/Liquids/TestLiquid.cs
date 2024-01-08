using LiquidLabyrinth.Labyrinth.Monobehaviours;
using System;
using UnityEngine;

namespace LiquidLabyrinth.Labyrinth.Liquids
{
    internal class TestLiquid : LiquidAPI.Liquid
    {
        public override Color Color { get => Color.white; }

        public override void OnEnterContainer(Container container)
        {
            Plugin.Logger.LogWarning($"{Name} Entering container");
        }

        public override void OnEnterLimb(LimbBehaviour limb)
        {
            throw new NotImplementedException();
        }

        public override void OnExitContainer(Container container)
        {
            Plugin.Logger.LogWarning($"{Name} Exiting container");
        }

        public override void OnContainerBreak(Container container, RaycastHit hit)
        {
            base.OnContainerBreak(container, hit);
        }
    }
}
