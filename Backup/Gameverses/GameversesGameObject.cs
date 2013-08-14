using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gameverses;
using Gameverses.Photon;

namespace Gameverses {
	public class GameversesGameObject : MonoBehaviour {
		
		public GameversesGameAPI gameverses;
		public GameversesService gameversesService;
		public GameNetworking gameNetworking;
		public ServiceUtil serviceUtil;
				
#if NETWORK_UNITY		
		public NetworkView networkViewObject;
#else		
		public PhotonView networkViewObject;
		public GameObject rpcObject;
#endif
		
		void Start() {
			serviceUtil = gameObject.AddComponent<ServiceUtil>();
#if NETWORK_UNITY
			networkViewObject = gameObject.AddComponent<NetworkView>();
			networkViewObject.stateSynchronization = NetworkStateSynchronization.Unreliable;
			gameNetworking = gameObject.AddComponent<GameNetworking>();
#else
			
			//rpcObject = PhotonNetwork.Instantiate("GameNetworkPhotonRPC", Vector3.zero, Quaternion.identity, 0);
			//networkViewObject = rpcObject.GetComponent<PhotonView>();
			////networkViewObject.viewID = PhotonNetwork.AllocateViewID();
			//networkViewObject.synchronization = ViewSynchronization.ReliableDeltaCompressed;
			gameNetworking = gameObject.AddComponent<GameNetworking>();

#endif			
			gameverses = GameversesGameAPI.Instance;
			gameverses.InitGameverses();
			//gameverses.GetGameList();
			
			DontDestroyOnLoad(gameObject);
		}
		
		
		void OnLeftRoom() {
			rpcObject = null;
			networkViewObject = null;
		}
		
		public void SetupNetworkView() {
			
			if(rpcObject == null) {
				rpcObject = PhotonNetwork.Instantiate("GameNetworkPhotonRPC", Vector3.zero, Quaternion.identity, 0);
				networkViewObject = rpcObject.GetComponent<PhotonView>();
				if(rpcObject != null) {
					GameNetworkPhotonRPC rpc = rpcObject.GetComponent<GameNetworkPhotonRPC>();
					rpc.uuid = UniqueUtil.Instance.currentUniqueId;
				}
				//networkViewObject.viewID = PhotonNetwork.AllocateViewID();
				//networkViewObject.synchronization = ViewSynchronization.ReliableDeltaCompressed;
				//DontDestroyOnLoad(rpcObject);
			}
		}
		
	}
}

