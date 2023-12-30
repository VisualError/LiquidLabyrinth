using GameNetcodeStuff;
using LiquidLabyrinth.Utilities.MonoBehaviours;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace LiquidLabyrinth.ItemHelpers
{
    class Throwable : GrabbableObject
    {
        public NetworkVariable<bool> Throwing = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public bool Holding = false;
        public bool LMBToThrow = true;
        public bool QToThrow = false;
        public bool EToThrow = false;
        public AnimationCurve FallCurve;
        public AnimationCurve VerticalFallCurveNoBounce;
        public AnimationCurve VerticalFallCurve;
        private PlayerControllerB previouslyHeld;
        public RaycastHit itemHit;

        public override void Start()
        {
            base.Start();
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            Holding = buttonDown;
            if (!IsOwner) return;
            if (!Throwing.Value && LMBToThrow)
            {
                Throw_ServerRpc();
            }
        }

        [ServerRpc]
        void Throw_ServerRpc()
        {
            Throw_ClientRpc();
        }
        [ClientRpc]
        void Throw_ClientRpc()
        {
            CoroutineHandler.Instance.NewCoroutine(Throw());
        }

        public override void Update()
        {
            base.Update();
        }

        public override void ItemInteractLeftRight(bool right)
        {
            base.ItemInteractLeftRight(right);
            if (right)
            {
                return;
            }
        }


        public IEnumerator Throw()
        {
            if (IsOwner)
            {
                Throwing.Value = true;
            }
            //TODO: Throwing animation.
            //previouslyHeld.twoHanded = true;
            yield return new WaitUntil(() => !Holding);
            if (IsOwner) playerHeldBy.UpdateSpecialAnimationValue(true, 0, 0f, true);
            previouslyHeld.inSpecialInteractAnimation = true;
            previouslyHeld.isClimbingLadder = false;
            previouslyHeld.playerBodyAnimator.ResetTrigger("SA_ChargeItem");
            previouslyHeld.playerBodyAnimator.SetTrigger("SA_ChargeItem");
            yield return new WaitForSeconds(.25f);
            if (IsOwner) playerHeldBy.UpdateSpecialAnimationValue(false, 0, 0f, false);
            previouslyHeld.inSpecialInteractAnimation = false;
            //previouslyHeld.twoHanded = false;
            if (IsOwner) playerHeldBy.DiscardHeldObject(true, null, GetItemThrowDestination(), true);
            yield break;
        }

        public override void OnHitGround()
        {
            base.OnHitGround();
            if (IsOwner)
            {
                Throwing.Value = false;
            }
        }

        public override void EquipItem()
        {
            base.EquipItem();
            previouslyHeld = playerHeldBy;
            if (IsOwner)
            {
                Throwing.Value = false;
            }
        }

        public Vector3 GetItemThrowDestination()
        {
            Vector3 vector = transform.position;
            Ray ThrowRay = new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward);
            if (Physics.Raycast(ThrowRay, out itemHit, 12f, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
            {
                vector = ThrowRay.GetPoint(itemHit.distance - 0.05f);
            }
            else
            {
                vector = ThrowRay.GetPoint(10f);
            }
            ThrowRay = new Ray(vector, Vector3.down);
            Vector3 result;
            if (Physics.Raycast(ThrowRay, out itemHit, 30f, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
            {
                result = itemHit.point + Vector3.up * 0.05f;
            }
            else
            {
                result = ThrowRay.GetPoint(30f);
            }
            return result;
        }

        public override void FallWithCurve()
        {
            base.FallWithCurve();
        }

    }
}
