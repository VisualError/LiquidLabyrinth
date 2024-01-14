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
        public override Color Color { get => Color.red; }
        public override string? Name { get => "Explosive Liquid"; }

        GameObject? _container;

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

        public override void OnContainerBreak(Container container)
        {
            base.OnContainerBreak(container);
            _container = container.gameObject;
            SpawnExplosion_ClientRpc();
        }

        [ClientRpc]
        void SpawnExplosion_ClientRpc()
        {
            if (_container == null) return;
            Landmine.SpawnExplosion(_container.transform.position + Vector3.up, true, 5.7f, 6.4f);
        }
    }
}
