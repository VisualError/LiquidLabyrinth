using GameNetcodeStuff;
using LiquidLabyrinth.Labyrinth.Monobehaviours;
using LiquidLabyrinth.Utilities;
using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LiquidLabyrinth.Labyrinth.Liquids
{
    internal class ReviveLiquid : LiquidAPI.Liquid
    {
        public override Color Color { get => Random.ColorHSV(); }

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

        public override void OnContainerBreak(RaycastHit hit)
        {
            base.OnContainerBreak(hit);
            if (hit.transform.TryGetComponent(out DeadBodyInfo deadBodyInfo))
            {
                PlayerControllerB player = deadBodyInfo.playerScript;
                RevivePlayer(player, hit.transform.position);
                return;
            }
        }

        void RevivePlayer(PlayerControllerB player, Vector3 position)
        {
            if (!(NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)) return;
            if (player.deadBody == null) return;


            if (25 >= Random.Range(1, 100)) // currently hard coded because im pissy as fuck.
            {
                Vector3 navMeshPosition = RoundManager.Instance.GetNavMeshPosition(position, default, 10f);
                ReviveAsEnemy(player, navMeshPosition);
                return;
            }

            Revive_ClientRpc(player, position);
        }

        EnemyType SelectEnemyType()
        {
            if (!Plugin.Instance.spawnRandomEnemy.Value)
                return Plugin.Instance.enemyTypes["Masked"];

            return Plugin.Instance.enemyTypes.ElementAt(Random.Range(0, Plugin.Instance.enemyTypes.Count)).Value;
        }

        void ReviveAsEnemy(PlayerControllerB player, Vector3 navMeshPosition)
        {
            NetworkObjectReference netObjectRef = RoundManager.Instance.SpawnEnemyGameObject(navMeshPosition, player.transform.eulerAngles.y, -1, SelectEnemyType());
            if (!netObjectRef.TryGet(out NetworkObject networkObject))
            {
                Plugin.Logger.LogWarning("Tried to spawn an enemy, but failed to get spawned enemy game object.");
                return;
            }
            DeactivateBody_ClientRpc(player);

            var ai = networkObject.GetComponent<EnemyAI>();
            if (ai == null) return;
            ai.isOutside = !player.isInsideFactory;
            ai.allAINodes = GameObject.FindGameObjectsWithTag(ai.isOutside ? "OutsideAINode" : "AINode");
            player.redirectToEnemy = ai;
        }

        [ClientRpc]
        void Revive_ClientRpc(NetworkBehaviourReference player, Vector3 position)
        {
            if (!player.TryGet(out PlayerControllerB plr)) return;
            if (plr.deadBody == null) return;

            PlayerUtils.RevivePlayer(plr, plr.deadBody, position);
            plr.deadBody.DeactivateBody(false);
        }

        [ClientRpc]
        void DeactivateBody_ClientRpc(NetworkBehaviourReference player)
        {
            if (!player.TryGet(out PlayerControllerB plr)) return;
            if (plr.deadBody == null) return;

            plr.deadBody.DeactivateBody(false);
        }
    }
}
