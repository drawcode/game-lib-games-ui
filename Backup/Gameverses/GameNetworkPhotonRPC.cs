using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gameverses;
using Gameverses.Photon;

namespace Gameverses {
	public class GameNetworkPhotonRPC : MonoBehaviour {
		
		public string uuid = "";
		public Gameverses.Photon.PhotonView photonView;
		
		public int viewID = 0;
		
		
		void Update() {
			if(viewID == 0) {
				if(photonView != null) {
					viewID = photonView.viewID.ID;
				}
			}
		}
			
		void Awake() {
			photonView = gameObject.GetComponent<Gameverses.Photon.PhotonView>();
			uuid = UniqueUtil.Instance.currentUniqueId;
		}
		
		
		void OnLeftRoom() {
			Destroy(gameObject);
		}
		
		[RPC]
	    public void SpawnOnNetwork(Vector3 pos, 
			Quaternion rot, 
			PhotonViewID id1, 
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
			PhotonViewID id1, 
			Vector3 pos, 
			Quaternion rot, 
			string actionMessage,
			Vector3 direction,
			bool amOwner) {
			
			GameNetworkingUnityPhoton.Instance.ActionOnNetworkView(np, id1, pos, rot, actionMessage, direction, amOwner);
		}
		
	    [RPC]
	    void SetPhotonPlayer(PhotonPlayer networkPlayer, string uuid, string username, string type) {
			
			GameNetworkingUnityPhoton.Instance.SetPhotonPlayer(networkPlayer, uuid, username, type);
	    }	
				
		[RPC]
	    void UpdatePlayerAttributeSync(PhotonPlayer networkPlayer, string key, string keyValue) {
			GameNetworkingUnityPhoton.Instance.UpdatePlayerAttributeSync(networkPlayer, key, keyValue);
	    }
	
	    [RPC]
	    void RemovePhotonPlayer(PhotonPlayer networkPlayer) {
			
	        GameNetworkingUnityPhoton.Instance.RemovePhotonPlayer(networkPlayer);
	    }	
	}
}

