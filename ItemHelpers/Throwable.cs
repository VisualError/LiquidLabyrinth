using GameNetcodeStuff;
using LiquidLabyrinth.Utilities;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace LiquidLabyrinth.ItemHelpers
{
    class Throwable : GrabbableRigidbody
    {

        // EVENTS:
        public event UnityAction onThrowItem;

        public PlayerControllerB playerThrownBy;
        public NetworkVariable<bool> isThrown = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public BoxCollider collider;
        public SphereCollider headCollider; // TODO: Implement Throwable head class instead of doing this bs..
        public bool Holding = false;
        public bool LMBToThrow = true;
        public bool QToThrow = false;
        public bool EToThrow = false;
        Quaternion oldRotation;
        private NetworkVariable<Vector3> throwDir = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<bool> isKinematic = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public float throwForce = 10f;

        public override void Start()
        {
            base.Start();

            if (IsHost || IsServer)
            {
                rb.isKinematic = false;
                rb.AddForce(gameObject.transform.forward * 2f, ForceMode.Impulse);
                isThrown.Value = true;
            }
            collider = GetComponent<BoxCollider>();
            headCollider = GetComponent<SphereCollider>();
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            Holding = buttonDown;
            if (!isThrown.Value && LMBToThrow && !buttonDown)
            {
                playerThrownBy = playerHeldBy;
                if (IsOwner)
                {
                    rb.isKinematic = false;
                    throwDir.Value = gameObject.transform.forward;
                    // cast ray forward for 100 units, if it hit something, we take the direction from the object to the hit point
                    RaycastHit hit;
                    if (Physics.Raycast(playerThrownBy.gameplayCamera.transform.position, playerThrownBy.gameplayCamera.transform.forward, out hit, 100f, OtherUtils.MaskForLayer(gameObject.layer), QueryTriggerInteraction.Ignore))
                    {
                        throwDir.Value = (hit.point - gameObject.transform.position).normalized;
                    }
                    Throw_ServerRpc(throwDir.Value);
                }
            }
        }

        [ServerRpc]
        void Throw_ServerRpc(Vector3 throwDir)
        {
            Throw_ClientRpc(throwDir);
        }
        [ClientRpc]
        void Throw_ClientRpc(Vector3 throwDir)
        {
            StartCoroutine(Throw(throwDir));
        }

        public override void Update()
        {
            if (rb == null)
            {
                Plugin.Logger.LogWarning($"Rigidbody for {name} doesn't exist. This shouldn't happen!");
                return;
            }
            if (isKinematic.Value != rb.isKinematic && !(IsHost || IsServer))
            {
                rb.isKinematic = isKinematic.Value;
            }
            else if ((IsHost || IsServer) && rb.isKinematic != isKinematic.Value)
            {
                isKinematic.Value = rb.isKinematic;
            }
            // code wack.
            if(headCollider != null)
            {
                headCollider.isTrigger = isKinematic.Value;
            }
            if (collider != null)
            {
                collider.isTrigger = isKinematic.Value;
            }
            base.Update();
        }

        public override void SetControlTipsForItem()
        {
            base.SetControlTipsForItem();
            string[] allLines;
            allLines = new string[]{
                "Throw [Click]"
            };
            if (IsOwner)
            {
                HUDManager.Instance.ChangeControlTipMultiple(allLines, true, itemProperties);
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (IsHost || IsServer)
            {
                if (StartOfRound.Instance.timeSinceRoundStarted > 2)
                {
                    rb.isKinematic = !StartOfRound.Instance.shipHasLanded && !StartOfRound.Instance.inShipPhase;
                    //rb.isKinematic = !StartOfRound.Instance.shipDoorsEnabled;
                }
                /*else if(!StartOfRound.Instance.inShipPhase && !isHeld)
                {
                    rb.isKinematic = StartOfRound.Instance.inShipPhase;
                }*/
                isKinematic.Value = rb.isKinematic;
                if (isThrown.Value)
                {
                    transform.rotation = oldRotation;
                    if (rb.velocity.magnitude > 0.1f)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(rb.velocity);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
                        oldRotation = transform.rotation;
                    }
                }
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


        public IEnumerator Throw(Vector3 throwDir)
        {
            //TODO: Throwing animation.
            //previouslyHeld.twoHanded = true;
            if (!StartOfRound.Instance.shipHasLanded && !StartOfRound.Instance.inShipPhase) yield break;
            yield return new WaitUntil(() => !Holding);
            if (IsOwner) playerThrownBy.UpdateSpecialAnimationValue(true, 0, 0f, true);
            playerThrownBy.inSpecialInteractAnimation = true;
            playerThrownBy.isClimbingLadder = false;
            playerThrownBy.playerBodyAnimator.ResetTrigger("SA_ChargeItem");
            playerThrownBy.playerBodyAnimator.SetTrigger("SA_ChargeItem");
            yield return new WaitForSeconds(.25f);
            onThrowItem?.Invoke();
            oldRotation = gameObject.transform.rotation;
            if (IsOwner)playerThrownBy.DiscardHeldObject();
            rb.isKinematic = false;
            if(IsOwner) isThrown.Value = true;
            transform.Rotate(itemProperties.rotationOffset); // so it no fucky. it look goody

            Plugin.Logger.LogMessage($"Throwing object with velocity: {throwDir * throwForce}");
            rb.AddForce(throwDir * throwForce, ForceMode.Impulse);
            if (IsOwner) playerThrownBy.UpdateSpecialAnimationValue(false, 0, 0f, false);
            playerThrownBy.inSpecialInteractAnimation = false;
            yield break;
        }

        public override void OnCollisionEnter(Collision collision)
        {
            base.OnCollisionEnter(collision);
            if(IsOwner) isThrown.Value = false;
        }

        public override void EquipItem()
        {
            base.EquipItem();
            if (IsOwner) isThrown.Value = false;
        }

    }
}
