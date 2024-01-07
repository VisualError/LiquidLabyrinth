using LiquidLabyrinth.Labyrinth.Monobehaviours;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LiquidLabyrinth.Labyrinth.Liquids
{
    internal class TestLiquid : LiquidAPI.Liquid
    {
        public override Color Color { get => Color.white; }
        public override string? Name { get => "TestLiquid"; }

        public override void OnEnterContainer(Container container)
        {
            throw new NotImplementedException();
        }

        public override void OnEnterLimb(LimbBehaviour limb)
        {
            throw new NotImplementedException();
        }

        public override void OnExitContainer(Container container)
        {
            throw new NotImplementedException();
        }

        public override void OnContainerBreak()
        {
            base.OnContainerBreak();
            if (Container == null) return;
            Landmine.SpawnExplosion(Container.transform.position + Vector3.up, true, 5.7f, 6.4f);
        }
    }
}
