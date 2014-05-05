using System;
using System.Collections;
using System.Collections.Generic;
using Gameverses;

using UnityEngine;

namespace Gameverses {
    public class GameNetworkPhotonRPC : GameObjectBehavior {
        public string uniqueId = "";
        public PhotonView photonView;
        public int viewID = 0;

        private void Update() {
            if (viewID == 0) {
                if (photonView != null) {
                    viewID = photonView.viewID;//.ID;
                }
            }
        }

        private void Awake() {
            photonView = gameObject.GetComponent<PhotonView>();
        }

        private void OnLeftRoom() {
            Destroy(gameObject);
        }

        [RPC]
        public void SpawnOnNetwork(
            Vector3 pos,
            Quaternion rot,
            int id1,
            string playerName,
            string playerType,
            string uuid,
            bool amOwner,
            PhotonPlayer np) {
            GameNetworkingUnityPhoton.Instance.SpawnOnNetwork(pos,
                rot,
                id1,
                playerName,
                playerType,
                uuid,
                amOwner,
                np);
        }

        [RPC]
        public void ActionOnNetwork(
            string actionMessage,
            Vector3 pos,
            Vector3 direction) {
            GameNetworkingUnityPhoton.Instance.ActionOnNetwork(actionMessage, pos, direction);
        }

        [RPC]
        public void ActionOnNetworkView(
            PhotonPlayer np,
            int id1,
            Vector3 pos,
            Quaternion rot,
            string actionMessage,
            Vector3 direction,
            bool amOwner) {
            GameNetworkingUnityPhoton.Instance.ActionOnNetworkView(np, id1, pos, rot, actionMessage, direction, amOwner);
        }

        [RPC]
        private void SetPhotonPlayer(PhotonPlayer networkPlayer, string uuid, string username, string type) {
            GameNetworkingUnityPhoton.Instance.SetPhotonPlayer(networkPlayer, uuid, username, type);
        }

        [RPC]
        private void UpdatePlayerAttributeSync(PhotonPlayer networkPlayer, string key, string keyValue) {
            GameNetworkingUnityPhoton.Instance.UpdatePlayerAttributeSync(networkPlayer, key, keyValue);
        }

        [RPC]
        private void RemovePhotonPlayer(PhotonPlayer networkPlayer) {
            GameNetworkingUnityPhoton.Instance.RemovePhotonPlayer(networkPlayer);
        }

        /*

    /// <summary>Connect automatically? If false you can set this to true later on or call ConnectUsingSettings in your own scripts.</summary>
    public bool AutoConnect = true;

    /// <summary>if we don't want to connect in Start(), we have to "remember" if we called ConnectUsingSettings()</summary>
    private bool ConnectInUpdate = true;

    public virtual void Start()
    {
        PhotonNetwork.autoJoinLobby = false;    // we join randomly. always. no need to join a lobby to get the list of rooms.
    }

    public virtual void Update()
    {
        if (ConnectInUpdate && AutoConnect)
        {
            LogUtil.Log("Update() was called by Unity. Scene is loaded. Let's connect to the Photon Master Server. Calling: PhotonNetwork.ConnectUsingSettings();");

            ConnectInUpdate = false;
            PhotonNetwork.ConnectUsingSettings("1");
        }
    }

    // to react to events "connected" and (expected) error "failed to join random room", we implement some methods. PhotonNetworkingMessage lists all available methods!

    public virtual void OnConnectedToMaster()
    {
        LogUtil.Log("OnConnectedToMaster() was called by PUN. Now this client is connected and could join a room. Calling: PhotonNetwork.JoinRandomRoom();");
        PhotonNetwork.JoinRandomRoom();
    }

    public virtual void OnPhotonRandomJoinFailed()
    {
        LogUtil.Log("OnPhotonRandomJoinFailed() was called by PUN. No random room available, so we create one. Calling: PhotonNetwork.CreateRoom(null, true, true, 4);");
        PhotonNetwork.CreateRoom(null, true, true, 4);
    }

    // the following methods are implemented to give you some context. re-implement them as needed.

    public virtual void OnFailedToConnectToPhoton(DisconnectCause cause)
    {
        LogUtil.LogError("Cause: " + cause);
    }

    public void OnJoinedRoom()
    {
        LogUtil.Log("OnJoinedRoom() called by PUN. Now this client is in a room. From here on, your game would be running. For reference, all callbacks are listed in enum: PhotonNetworkingMessage");
    }

    public virtual void OnJoinedLobby()
    {
        LogUtil.Log("OnJoinedLobby(). Use a GUI to show existing rooms available in PhotonNetwork.GetRoomList().");
    }
         * */
    }
}