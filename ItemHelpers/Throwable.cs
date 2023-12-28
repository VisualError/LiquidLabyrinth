using LethalLib;
using LiquidLabyrinth.Utilities.MonoBehaviours;
using System.Collections;
using UnityEngine;

namespace LiquidLabyrinth.ItemHelpers
{
    class Throwable : GrabbableObject
    {
        public bool Throwing = false;
        public bool Holding = false;
        public bool LMBToThrow = true;
        public bool QToThrow = false;
        public bool EToThrow = false;
        public AnimationCurve FallCurve;
        public AnimationCurve VerticalFallCurveNoBounce;
        public AnimationCurve VerticalFallCurve;
        public RaycastHit itemHit;

        public override void Start()
        {
            base.Start();
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!IsOwner) return;
            Holding = buttonDown;
            if (!Throwing)
            {
                CoroutineHandler.Instance.NewCoroutine(Throw());
            }
        }

        public override void ItemInteractLeftRight(bool right)
        {
            base.ItemInteractLeftRight(right);
            if (right)
            {
                return;
            }
        }

        public override void Update()
        {
            base.Update();
        }


        public IEnumerator Throw()
        {
            Throwing = true;
            //TODO: Throwing animation.
            yield return new WaitUntil(() => !Holding);
            playerHeldBy.DiscardHeldObject(true, null, GetItemThrowDestination(), true);
            yield break;
        }

        public override void OnHitGround()
        {
            base.OnHitGround();
            Throwing = false;
        }

        public override void EquipItem()
        {
            base.EquipItem();
            Throwing = false;
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
