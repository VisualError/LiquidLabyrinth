using GameNetcodeStuff;
using LiquidLabyrinth.Utilities;
using LiquidLabyrinth.Utilities.MonoBehaviours;
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
        private float t = 0f;
        public NetworkVariable<bool> isThrown = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public BoxCollider collider;
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
                t = 0f;
                isThrown.Value = true;
            }
            collider = GetComponent<BoxCollider>();
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            Holding = buttonDown;
            if (!isThrown.Value && LMBToThrow && !buttonDown)
            {
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
            if (isKinematic.Value != rb.isKinematic && !(IsHost || IsServer))
            {
                rb.isKinematic = isKinematic.Value;
            }
            else if ((IsHost || IsServer) && rb.isKinematic != isKinematic.Value)
            {
                isKinematic.Value = rb.isKinematic;
            }
            collider.isTrigger = isKinematic.Value;
            base.Update();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (IsHost || IsServer)
            {
                if (StartOfRound.Instance.timeSinceRoundStarted > 2f)
                {
                    rb.isKinematic = !StartOfRound.Instance.shipHasLanded && !StartOfRound.Instance.inShipPhase;
                }
                isKinematic.Value = rb.isKinematic;
                if (rb.isKinematic && !isHeld)
                {
                    rb.position = gameObject.transform.position;
                    rb.rotation = gameObject.transform.rotation;
                }
                if (isThrown.Value)
                {
                    transform.rotation = oldRotation;
                    if (rb.velocity.magnitude > 0.1f)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(rb.velocity);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
                        oldRotation = transform.rotation;
                    }

                    t += Time.deltaTime;
                    if (t > 5f)
                    {
                        isThrown.Value = false;
                        rb.isKinematic = true;
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
            playerThrownBy.DiscardHeldObject();
            t = 0f;
            rb.isKinematic = false;
            isThrown.Value = true;
            rb.transform.Rotate(new Vector3(0, 90, 0)); // Rotates the bottle 90 degrees around the y-axis

            Plugin.Logger.LogMessage($"Throwing object with velocity: {throwDir * throwForce}");
            rb.AddForce(throwDir * throwForce, ForceMode.Impulse);
            if (IsOwner) playerThrownBy.UpdateSpecialAnimationValue(false, 0, 0f, false);
            playerThrownBy.inSpecialInteractAnimation = false;
            yield break;
        }

        public override void OnCollisionEnter(Collision collision)
        {
            base.OnCollisionEnter(collision);
            isThrown.Value = false;
        }

        public override void EquipItem()
        {
            base.EquipItem();
            isThrown.Value = false;
            t = 0f;
        }

    }
}
