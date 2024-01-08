using LiquidLabyrinth.Utilities;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using System;

namespace LiquidLabyrinth.ItemHelpers;

[RequireComponent(typeof(Rigidbody))]

// Taken from: https://gist.github.com/EvaisaDev/aaf727b2aeb6733793c89a887f8f8615
class GrabbableRigidbody : SaveableItem
{

    public EnemyAI? enemyCurrentlyHeld;
    private NetworkVariable<bool> net_GrabbableToEnemies = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> net_Placed = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> net_isFloating = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public bool floatWhileOrbiting;
    public float gravity = 9.8f;
    internal Rigidbody rb;
    internal AudioSource itemAudio;
    public float itemMass = 1f;
    public override void Start()
    {
        rb = GetComponent<Rigidbody>();
        itemAudio = GetComponent<AudioSource>();
        if (rb == null || itemAudio == null) return;
        rb.useGravity = false;
        rb.mass = itemMass;
        // force some properties which might be missconfigured
        itemProperties.itemSpawnsOnGround = false;
        base.Start();
    }

    internal Vector3 oldEnemyPosition;

    public override void Update()
    {
        // hax
        fallTime = 1.0f;
        reachedFloorTarget = true;
        var wasHeld = isHeld;
        // hella hax
        isHeld = true;
        base.Update();
        isHeld = wasHeld;
    }

    public void EnableColliders(bool enable)
    {
        for (int i = 0; i < propColliders.Length; i++)
        {
            if (!(propColliders[i] == null) && !propColliders[i].gameObject.CompareTag("InteractTrigger") && !propColliders[i].gameObject.CompareTag("DoNotSet"))
            {
                propColliders[i].enabled = enable;
            }
        }
    }


    public virtual void FixedUpdate()
    {
        // handle gravity if rigidbody is enabled
        if (IsHost || IsServer)
        {
            if (floatWhileOrbiting && StartOfRound.Instance.inShipPhase && !rb.isKinematic && Plugin.Instance.NoGravityInOrbit.Value || net_isFloating.Value)
            {
                rb.AddForce(Vector3.zero, ForceMode.VelocityChange);
                return;
            }
            if (!rb.isKinematic && !isHeld)
            {
                rb.useGravity = false;
                rb.AddForce(Vector3.down * gravity, ForceMode.Acceleration);
            }
            else
            {
                rb.AddForce(Vector3.zero, ForceMode.VelocityChange);
            }
        }
    }

    public override void LateUpdate()
    {
        if (parentObject != null && (isHeld||isHeldByEnemy))
        {
            transform.rotation = parentObject.rotation;
            Vector3 rotationOffset = itemProperties.rotationOffset;
            if (isHeldByEnemy) rotationOffset = rotationOffset + new Vector3(0,90,0);
            transform.Rotate(rotationOffset);
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

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsHost || IsServer)
        {
            net_GrabbableToEnemies.Value = Plugin.Instance.IsGrabbableToEnemies.Value;
        }
        grabbableToEnemies = net_GrabbableToEnemies.Value;
    }

    public virtual void OnCollisionEnter(Collision collision)
    {
        if (IsClient)
        {
            OnCollision_ClientRpc(collision.gameObject.tag, rb.velocity.magnitude);
        }
        else if (IsServer)
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
        if (rb == null || rb.isKinematic) return;
        if (itemAudio == null)
        {
            Plugin.Logger.LogWarning("ITEM AUDIO SOURCE DOESNT EXIST.");
            return;
        }
        if (objectTag == "Player")
        {
            return;
        }
        float pitch = OtherUtils.mapValue(rigidBodyMagnitude, 0.8f, 10f, 0.8f, 1.5f);
        itemAudio.pitch = pitch;
        PlayDropSFX();
    }

    public override void EquipItem()
    {
        base.EquipItem();
        itemAudio.pitch = 1f;
        //set parent to null
        transform.parent = null;
        EnablePhysics(false);
        if (IsOwner) net_Placed.Value = false;
    }

    public override void PocketItem()
    {
        base.PocketItem();
        EnablePhysics(false);
    }

    public override void GrabItemFromEnemy(EnemyAI enemy)
    {
        base.GrabItemFromEnemy(enemy);
        isHeldByEnemy = true;
        itemAudio.pitch = 1f;
        //set parent to null and currentEnemyHeld to the enemy.
        enemyCurrentlyHeld = enemy;
        transform.parent = null;
    }

    public override void DiscardItemFromEnemy()
    {
        base.DiscardItemFromEnemy();
        Plugin.Logger.LogWarning($"drop called by enemy {enemyCurrentlyHeld.name}");
        isHeldByEnemy = false;
        enemyCurrentlyHeld = null;
    }

    public override void DiscardItem()
    {
        if (!net_Placed.Value) EnablePhysics(true);
        base.DiscardItem();
    }

    public override void InteractItem()
    {
        base.InteractItem();
        itemAudio.pitch = 1f;
    }

    public override void OnPlaceObject()
    {
        base.OnPlaceObject();
        EnableColliders(true);
        rb.isKinematic = true;
    }

    public override void FallWithCurve()
    {
        // stub, we do not need this.
    }

    public new void FallToGround(bool randomizePosition = false)
    {
        // stub, we do not need this.
    }
}