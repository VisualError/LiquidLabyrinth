using LiquidLabyrinth.Events;
using LiquidLabyrinth.Utilities;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace LiquidLabyrinth.ItemHelpers
{
    [RequireComponent(typeof(Rigidbody))]

    // Taken from: https://gist.github.com/EvaisaDev/aaf727b2aeb6733793c89a887f8f8615
    class GrabbableRigidbody : GrabbableObject
    {

        // EVENTS:
        public UnityEvent OnStart = new UnityEvent();
        public UnityEvent OnUpdate = new UnityEvent();
        public UnityEvent OnDiscardItem = new UnityEvent();
        public UnityEvent OnEquipItem = new UnityEvent();
        public UnityEvent OnCollision = new UnityEvent();
        public UnityEvent OnInteractLocal = new UnityEvent();
        public UnityEvent OnInteractGlobal = new UnityEvent();
        public bool floatWhileOrbiting;
        public float gravity = 9.8f;
        internal Rigidbody rb;
        public AudioSource itemAudio;
        public float itemMass = 1f;
        public override void Start()
        {
            OnStart?.Invoke();
            rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.mass = itemMass;
            // force some properties which might be missconfigured
            itemProperties.itemSpawnsOnGround = false;
            base.Start();
            EnablePhysics(true);
        }

        public new void EnablePhysics(bool enable)
        {
            for (int i = 0; i < propColliders.Length; i++)
            {
                if (!(propColliders[i] == null) && !propColliders[i].gameObject.CompareTag("InteractTrigger") && !propColliders[i].gameObject.CompareTag("DoNotSet"))
                {
                    propColliders[i].enabled = enable;
                }
            }

            // enable rigidbody
            rb.isKinematic = !enable;
        }

        public override void Update()
        {
            OnUpdate?.Invoke();
            // hax
            fallTime = 1.0f;
            reachedFloorTarget = true;
            var wasHeld = isHeld;
            // hella hax
            isHeld = true;
            base.Update();
            isHeld = wasHeld;
        }


        public virtual void FixedUpdate()
        {
            // handle gravity if rigidbody is enabled
            if (IsHost || IsServer)
            {
                if(floatWhileOrbiting && StartOfRound.Instance.inShipPhase && !rb.isKinematic && Plugin.Instance.NoGravityInOrbit.Value)
                {
                    rb.AddForce(Vector3.zero, ForceMode.VelocityChange);
                    return;
                }
                if (!rb.isKinematic && !isHeld)
                {
                    rb.useGravity = false;
                    rb.AddForce(Vector3.down * gravity * rb.mass, ForceMode.Acceleration);
                }
                else
                {
                    rb.AddForce(Vector3.zero, ForceMode.VelocityChange);
                }
            }
        }

        public override void LateUpdate()
        {
            if (parentObject != null && isHeld)
            {
                transform.rotation = parentObject.rotation;
                transform.Rotate(itemProperties.rotationOffset);
                transform.position = parentObject.position;
                Vector3 positionOffset = itemProperties.positionOffset;
                positionOffset = parentObject.rotation * positionOffset;
                transform.position += positionOffset;
            }
            if (radarIcon != null)
            {
                radarIcon.position = transform.position;
            }
        }

        public virtual void OnCollisionEnter(Collision collision)
        {
            if (!IsHost || !IsServer) return;
            if (IsClient)
            {
                OnCollision_ClientRpc(collision.gameObject.tag, rb.velocity.magnitude);
            }
            else
            {
                OnCollision_ServerRpc(collision.gameObject.tag, rb.velocity.magnitude);
            }
        }

        [ServerRpc]
        public void OnCollision_ServerRpc(string objectTag, float rigidBodyMagnitude)
        {
            OnCollision_ClientRpc(objectTag, rigidBodyMagnitude);
        } 

        [ClientRpc]
        public void OnCollision_ClientRpc(string objectTag, float rigidBodyMagnitude)
        {
            OnCollision?.Invoke();
            if (objectTag == "Player")
            {
                return;
            }
            float pitch = OtherUtils.mapValue(rigidBodyMagnitude, 0.8f, 10f, 0.8f, 1.5f);
            itemAudio.pitch = pitch;
            itemAudio.PlayOneShot(itemProperties.dropSFX);
        }

        public override void EquipItem()
        {
            // remove parent object
            OnEquipItem?.Invoke();
            base.EquipItem();
            itemAudio.pitch = 1f;
            rb.isKinematic = true;
            transform.parent = null;
        }

        public override void DiscardItem()
        {
            OnDiscardItem?.Invoke();
            rb.isKinematic = false;
            base.DiscardItem();
        }

        public override void InteractItem()
        {
            OnInteractLocal?.Invoke();
            base.InteractItem();
            itemAudio.pitch = 1f;
        }


        public override void FallWithCurve()
        {
            // stub, we do not need this.
        }

        public void FallToGround()
        {
            // stub, we do not need this.
        }
    }
}
