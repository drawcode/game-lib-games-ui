using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Gameverses {

    public class GameNetworkPlayerMessages {
        public static string PlayerAdded = "network-container-player-added";
        public static string PlayerRemoved = "network-container-player-removed";
        public static string PlayerInputAxisVertical = "network-container-input-axis-vertical";
        public static string PlayerInputAxisHorizontal = "network-container-input-axis-horizontal";
        public static string PlayerAnimation = "network-container-player-animation";
        public static string PlayerSpeed = "network-container-player-speed";
    }

    public enum GameNetworkAniStates {
        walk = 0,
        idle,
        run,
        kick,
        punch,
        attack,
        attack1,
        attack2,
        skill,
        skill1,
        skill2,
        health,
        health1,
        health2,
        magic,
        magic1,
        magic2,
        death,
        jump,
        jumpfall,
        gotbit,
        gothit,
        walljump,
        deathfall,
        jetpackjump,
        ledgefall,
        buttstomp,
        jumpland
    }

    public class GameNetworkPlayerContainer : MonoBehaviour {
#if NETWORK_UNITY
		public NetworkView networkViewObject;
#else
        public PhotonView networkViewObject;
#endif
        public string uuid = "";

        public GameNetworkAniStates currentAnimation = GameNetworkAniStates.idle;
        public GameNetworkAniStates lastAnimation = GameNetworkAniStates.idle;

        public GameObject gamePlayer;

        public double interpolationBackTime = 0.1;
        public bool running = false;

        public float verticalInputNetwork = 0f;
        public float horizontalInputNetwork = 0f;
        public float currentSpeedNetwork = 0f;

        internal struct State {
            internal double timestamp;
            internal Vector3 pos;
            internal Quaternion rot;
            internal int ani; // char ani = (char)currentAnimation;
            internal float verticalInput;
            internal float horizontalInput;
            internal float currentSpeed;
        }

        // We store twenty states with "playback" information
        private State[] m_BufferedState = new State[20];

        // Keep track of what slots are used
        private int m_TimestampCount;

        private void Start() {

            //running = true;

            // Find gamePlayer controller or create one
            //GameObject gamePlayerControllerObject = GameObject.Find("GamePlayerObject");
            //if(gamePlayerControllerObject != null) {
            //	GamePlayer
            //}
        }

#if NETWORK_UNITY
		public void AddNetworkView(NetworkViewID idl, string uniqueId) {
			networkViewObject = gameObject.AddComponent<NetworkView>();
			networkViewObject.stateSynchronization = NetworkStateSynchronization.ReliableDeltaCompressed;
			networkViewObject.viewID = idl;
			uuid = uniqueId;

			LogUtil.Log("Creating network container:" + uuid);

			GameMessenger<string>.Broadcast(GameNetworkPlayerMessages.PlayerAdded, uniqueId);
		}
#else

        public void AddNetworkView(int idl, string uniqueId) {
            networkViewObject = gameObject.AddComponent<PhotonView>();
            networkViewObject.synchronization = ViewSynchronization.ReliableDeltaCompressed;
            networkViewObject.viewID = idl;
            uuid = uniqueId;

            LogUtil.Log("Creating network container view:idl.ID:" + idl);
           // LogUtil.Log("Creating network container view:idl.isMine:" + idl.isMine);
            //LogUtil.Log("Creating network container view:idl.owner:" + idl.owner);
            LogUtil.Log("Creating network container:uuid:" + uuid);

            GameMessenger<string>.Broadcast(GameNetworkPlayerMessages.PlayerAdded, uniqueId);
        }

#endif

        public void SyncAnimation(string animationValue) {
            currentAnimation = (GameNetworkAniStates)Enum.Parse(typeof(GameNetworkAniStates), animationValue);
        }

#if NETWORK_UNITY
		void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info) {

			// Always send transform (depending on reliability of the network view)

			//Gameverses.LogUtil.Log("OnSerializeNetworkView1:gamePlayer:" + gamePlayer + " uuid:" + uuid);

			if(!running) {
				return;
			}

			if(gamePlayer == null) {
				return;
			}

			//Gameverses.LogUtil.Log("OnSerializeNetworkView:gamePlayer:" + gamePlayer + " uuid:" + uuid);

			if(stream.isWriting) {
				Vector3 pos = gamePlayer.transform.position;
				Quaternion rot = gamePlayer.transform.rotation;
				char ani = (char)currentAnimation;
				float verticalInput = verticalInputNetwork;
				float horizontalInput = horizontalInputNetwork;
				float currentSpeed = currentSpeedNetwork;
				stream.Serialize(ref pos);
				stream.Serialize(ref rot);
				stream.Serialize(ref ani);
				stream.Serialize(ref verticalInput);
				stream.Serialize(ref horizontalInput);
				stream.Serialize(ref currentSpeed);
			}

			// When receiving, buffer the information
			else {

				// Receive latest state information
				Vector3 pos = Vector3.zero;
				Quaternion rot = Quaternion.identity;
				char ani = (char)0;
				float verticalInput = 0f;
				float horizontalInput = 0f;
				currentAnimation = (GameNetworkAniStates)ani;
				float currentSpeed = 0f;
				stream.Serialize(ref pos);
				stream.Serialize(ref rot);
				stream.Serialize(ref ani);
				stream.Serialize(ref verticalInput);
				stream.Serialize(ref horizontalInput);
				stream.Serialize(ref currentSpeed);

				// Shift buffer contents, oldest data erased, 18 becomes 19, ... , 0 becomes 1
				for(int i = m_BufferedState.Length - 1; i >= 1; i--) {
					m_BufferedState[i] = m_BufferedState[i - 1];
				}

				// Save currect received state as 0 in the buffer, safe to overwrite after shifting
				State state;
				state.timestamp = info.timestamp;
				state.pos = pos;
				state.rot = rot;
				state.ani = ani;
				state.verticalInput = verticalInput;
				state.horizontalInput = horizontalInput;
				state.currentSpeed = currentSpeed;
				m_BufferedState[0] = state;

				// Increment state count but never exceed buffer size
				m_TimestampCount = Mathf.Min(m_TimestampCount + 1, m_BufferedState.Length);

				// Check integrity, lowest numbered state in the buffer is newest and so on
				for(int i = 0; i < m_TimestampCount - 1; i++) {
					if(m_BufferedState[i].timestamp < m_BufferedState[i + 1].timestamp)
						Debug.Log("State inconsistent");
				}

				//Debug.Log("stamp: " + info.timestamp + "my time: " + Network.time + "delta: " + (Network.time - info.timestamp));
			}
		}

#else

        private void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {

            // Always send transform (depending on reliability of the network view)

            //Gameverses.LogUtil.Log("OnSerializeNetworkView1:gamePlayer:" + gamePlayer + " uuid:" + uuid);

            if (!running) {
                return;
            }

            if (gamePlayer == null) {
                return;
            }

            //Gameverses.LogUtil.Log("OnSerializeNetworkView:gamePlayer:" + gamePlayer + " uuid:" + uuid);

            if (stream.isWriting) {
                Vector3 pos = gamePlayer.transform.position;
                Quaternion rot = gamePlayer.transform.rotation;
                int ani = (int)currentAnimation;
                float verticalInput = verticalInputNetwork;
                float horizontalInput = horizontalInputNetwork;
                float currentSpeed = currentSpeedNetwork;
                stream.Serialize(ref pos);
                stream.Serialize(ref rot);
                stream.Serialize(ref ani);
                stream.Serialize(ref verticalInput);
                stream.Serialize(ref horizontalInput);
                stream.Serialize(ref currentSpeed);
            }

            // When receiving, buffer the information
            else {

                // Receive latest state information
                Vector3 pos = Vector3.zero;
                Quaternion rot = Quaternion.identity;
                int ani = 0;
                float verticalInput = 0f;
                float horizontalInput = 0f;
                currentAnimation = (GameNetworkAniStates)ani;
                float currentSpeed = 0f;
                stream.Serialize(ref pos);
                stream.Serialize(ref rot);
                stream.Serialize(ref ani);
                stream.Serialize(ref verticalInput);
                stream.Serialize(ref horizontalInput);
                stream.Serialize(ref currentSpeed);

                // Shift buffer contents, oldest data erased, 18 becomes 19, ... , 0 becomes 1
                for (int i = m_BufferedState.Length - 1; i >= 1; i--) {
                    m_BufferedState[i] = m_BufferedState[i - 1];
                }

                // Save currect received state as 0 in the buffer, safe to overwrite after shifting
                State state;
                state.timestamp = info.timestamp;
                state.pos = pos;
                state.rot = rot;
                state.ani = ani;
                state.verticalInput = verticalInput;
                state.horizontalInput = horizontalInput;
                state.currentSpeed = currentSpeed;
                m_BufferedState[0] = state;

                // Increment state count but never exceed buffer size
                m_TimestampCount = Mathf.Min(m_TimestampCount + 1, m_BufferedState.Length);

                // Check integrity, lowest numbered state in the buffer is newest and so on
                for (int i = 0; i < m_TimestampCount - 1; i++) {
                    if (m_BufferedState[i].timestamp < m_BufferedState[i + 1].timestamp)
                        Debug.Log("State inconsistent");
                }

                //Debug.Log("stamp: " + info.timestamp + "my time: " + Network.time + "delta: " + (Network.time - info.timestamp));
            }
        }

#endif

        // This only runs where the component is enabled, which is only on remote peers (server/clients)
        private void Update() {
            double currentTime = Network.time;
            double interpolationTime = currentTime - interpolationBackTime;

            // We have a window of interpolationBackTime where we basically play
            // By having interpolationBackTime the average ping, you will usually use interpolation.
            // And only if no more data arrives we will use extrapolation

            //LogUtil.Log("Update:gamePlayer:" + gamePlayer + " uuid:" + uuid);

            if (!running) {
                return;
            }

            if (gamePlayer == null || uuid == UniqueUtil.Instance.currentUniqueId) {
                return;
            }

            // Use interpolation
            // Check if latest state exceeds interpolation time, if this is the case then
            // it is too old and extrapolation should be used
            if (m_BufferedState[0].timestamp > interpolationTime) {
                for (int i = 0; i < m_TimestampCount; i++) {

                    // Find the state which matches the interpolation time (time+0.1) or use last state
                    if (m_BufferedState[i].timestamp <= interpolationTime || i == m_TimestampCount - 1) {

                        // The state one slot newer (<100ms) than the best playback state
                        State rhs = m_BufferedState[Mathf.Max(i - 1, 0)];

                        // The best playback state (closest to 100 ms old (default time))
                        State lhs = m_BufferedState[i];

                        // Use the time between the two slots to determine if interpolation is necessary
                        double length = rhs.timestamp - lhs.timestamp;
                        float t = 0.0F;

                        // As the time difference gets closer to 100 ms t gets closer to 1 in
                        // which case rhs is only used
                        if (length > 0.0001)
                            t = (float)((interpolationTime - lhs.timestamp) / length);

                        // if t=0 => lhs is used directly
                        gamePlayer.transform.position = Vector3.Lerp(lhs.pos, rhs.pos, t);
                        gamePlayer.transform.rotation = Quaternion.Slerp(lhs.rot, rhs.rot, t);

                        GameMessenger<string, GameNetworkAniStates>.Broadcast(GameNetworkPlayerMessages.PlayerAnimation, uuid, (GameNetworkAniStates)lhs.ani);
                        GameMessenger<string, float>.Broadcast(GameNetworkPlayerMessages.PlayerInputAxisHorizontal, uuid, lhs.horizontalInput);
                        GameMessenger<string, float>.Broadcast(GameNetworkPlayerMessages.PlayerInputAxisVertical, uuid, lhs.verticalInput);
                        GameMessenger<string, float>.Broadcast(GameNetworkPlayerMessages.PlayerSpeed, uuid, lhs.currentSpeed);

                        // Set animation...

                        return;
                    }
                }
            }

            // Use extrapolation. Here we do something really simple and just repeat the last
            // received state. You can do clever stuff with predicting what should happen.
            else {
                State latest = m_BufferedState[0];

                gamePlayer.transform.position = latest.pos;
                gamePlayer.transform.rotation = latest.rot;

                GameMessenger<string, GameNetworkAniStates>.Broadcast(GameNetworkPlayerMessages.PlayerAnimation, uuid, (GameNetworkAniStates)latest.ani);
                GameMessenger<string, float>.Broadcast(GameNetworkPlayerMessages.PlayerInputAxisHorizontal, uuid, latest.horizontalInput);
                GameMessenger<string, float>.Broadcast(GameNetworkPlayerMessages.PlayerInputAxisVertical, uuid, latest.verticalInput);
                GameMessenger<string, float>.Broadcast(GameNetworkPlayerMessages.PlayerSpeed, uuid, latest.currentSpeed);
            }

            if (lastAnimation != currentAnimation) {
                lastAnimation = currentAnimation;

                GameMessenger<string, GameNetworkAniStates>.Broadcast(GameNetworkPlayerMessages.PlayerAnimation, uuid, lastAnimation);

                //if(gamePlayer.animation) {
                //	gamePlayer.animation.CrossFade(System.Enum.GetName(typeof(GameNetworkAniStates), currentAnimation));
                //	gamePlayer.animation["run"].normalizedSpeed = 1.0F;
                //	gamePlayer.animation["walk"].normalizedSpeed = 1.0F;
                //}
            }
        }
    }
}