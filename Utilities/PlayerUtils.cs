using GameNetcodeStuff;
using LiquidLabyrinth.Utilities.MonoBehaviours;
using System.Collections;
using UnityEngine;

namespace LiquidLabyrinth.Utilities
{
	internal class PlayerUtils
    {

		internal static void RotateToObject(PlayerControllerB player, GameObject targetObject)
        {
			CoroutineHandler.Instance.NewCoroutine(RotateToObjectCoroutine(player, targetObject), true);
			//Vector3 directionToTarget = targetObject.transform.position - player.transform.position;
			//Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
			//player.transform.LookAt(targetObject.transform);
			//player.SyncFullRotWithClients();
		}

        internal static IEnumerator RotateToObjectCoroutine(PlayerControllerB player, GameObject targetObject)
        {
			float turnStartTime = Time.time;
			while (true)
            {
                if (player == null || player.transform == null || targetObject == null || targetObject.transform == null) yield break;

                Vector3 directionToTarget = targetObject.transform.position - player.transform.position;
				directionToTarget = new Vector3(directionToTarget.x, 0, directionToTarget.z);
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

                // Get the current rotation of the camera
                Quaternion currentRotation = player.transform.rotation;

				// Calculate the time it takes to turn to the next target
				float turnTime = 1f;

				// Calculate the elapsed time since the start of the rotation
				float elapsedTime = Time.time - turnStartTime;

				// Calculate the interpolation factor
				float t = elapsedTime / turnTime;

				// Interpolate between the current rotation and the target rotation
				player.transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, t);
				// Adjust the character's velocity
				float angleDifference = Quaternion.Angle(currentRotation, targetRotation);
				player.SyncFullRotWithClients();
				// If the difference is small enough, stop the coroutine
				if (angleDifference < 0.01f)
                {
                    yield break;
                }
                yield return null;
            }
        }

        internal static void RevivePlayer(PlayerControllerB player, DeadBodyInfo bodyInfo, Vector3 spawnPosition)
        {
			if (!player.isPlayerDead) return;
			if(player.IsOwner || player.IsServer || player.IsHost)
            {
                Object.Destroy(bodyInfo.gameObject);
            }
            player.ResetPlayerBloodObjects(player.isPlayerDead);
            player.isClimbingLadder = false;
            player.ResetZAndXRotation();
            player.thisController.enabled = true;
            player.health = 100;
            player.disableLookInput = false;
			player.isPlayerDead = false;
			player.isPlayerControlled = true;
			player.isInElevator = true;
			player.isInHangarShipRoom = true;
			player.isInsideFactory = false;
			player.wasInElevatorLastFrame = false;
			StartOfRound.Instance.SetPlayerObjectExtrapolate(false);
			player.TeleportPlayer(spawnPosition, false, 0f, false, true);
			player.setPositionOfDeadPlayer = false;
			player.DisablePlayerModel(StartOfRound.Instance.allPlayerObjects[player.playerClientId], true, true);
			player.helmetLight.enabled = false;
			player.Crouch(false);
			player.criticallyInjured = false;
			if (player.playerBodyAnimator != null)
			{
				player.playerBodyAnimator.SetBool("Limp", false);
			}
			player.bleedingHeavily = false;
			player.activatingItem = false;
			player.twoHanded = false;
			player.inSpecialInteractAnimation = false;
			player.disableSyncInAnimation = false;
			player.inAnimationWithEnemy = null;
			player.holdingWalkieTalkie = false;
			player.speakingToWalkieTalkie = false;
			player.isSinking = false;
			player.isUnderwater = false;
			player.sinkingValue = 0f;
			player.statusEffectAudio.Stop();
			player.DisableJetpackControlsLocally();
			player.health = 100;
			player.mapRadarDotAnimator.SetBool("dead", false);
			if (player.IsOwner)
			{
				HUDManager.Instance.gasHelmetAnimator.SetBool("gasEmitting", false);
				player.hasBegunSpectating = false;
				HUDManager.Instance.RemoveSpectateUI();
				HUDManager.Instance.gameOverAnimator.SetTrigger("revive");
				HUDManager.Instance.HideHUD(false);
				player.hinderedMultiplier = 1f;
				player.isMovementHindered = 0;
				player.sourcesCausingSinking = 0;
				player.reverbPreset = StartOfRound.Instance.shipReverb;
			}
			StartOfRound.Instance.livingPlayers++;
			SoundManager.Instance.earsRingingTimer = 0f;
			player.voiceMuffledByEnemy = false;
			SoundManager.Instance.playerVoicePitchTargets[player.playerClientId] = 1f;
			SoundManager.Instance.SetPlayerPitch(1f, (int)player.playerClientId);
			if (player.currentVoiceChatIngameSettings == null)
			{
				StartOfRound.Instance.RefreshPlayerVoicePlaybackObjects();
			}
			if (player.currentVoiceChatIngameSettings != null)
			{
				if (player.currentVoiceChatIngameSettings.voiceAudio == null)
				{
					player.currentVoiceChatIngameSettings.InitializeComponents();
				}
				if (player.currentVoiceChatIngameSettings.voiceAudio == null)
				{
					return;
				}
				player.currentVoiceChatIngameSettings.voiceAudio.GetComponent<OccludeAudio>().overridingLowPass = false;
			}
		}
    }
}
