using LiquidLabyrinth.Labyrinth.Monobehaviours;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace LiquidLabyrinth.Labyrinth.Liquids
{
    internal class ExplosiveLiquid : LiquidAPI.Liquid
    {
        public override Color Color { get => Color.white; }
        public override string? Name { get => "ExplosiveLiquid"; }

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
            SpawnExplosion_ClientRpc();
        }

        [ClientRpc]
        void SpawnExplosion_ClientRpc()
        {
            Plugin.Logger.LogWarning("client 1");
            if (Container == null) return;
            Plugin.Logger.LogWarning("client 2");
            Landmine.SpawnExplosion(Container.transform.position + Vector3.up, true, 5.7f, 6.4f);
        }
    }
}
