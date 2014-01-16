using System;
using System.Collections;
using System.Collections.Generic;
using Gameverses;

using UnityEngine;

namespace Gameverses {
    public class GameNetworkPhotonRPC : MonoBehaviour {
        public string uuid = "";
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
            uuid = UniqueUtil.Instance.currentUniqueId;
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
    }
}