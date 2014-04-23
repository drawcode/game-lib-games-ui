using System;
using System.Collections;
using System.Collections.Generic;
using Gameverses;

using UnityEngine;
using Engine.Networking;

namespace Gameverses {

    public class GameversesGameObject : MonoBehaviour {
        public GameversesGameAPI gameverses;
        public GameversesService gameversesService;
        public GameNetworking gameNetworking;
        public WebRequests requests;

#if NETWORK_UNITY
		public NetworkView networkViewObject;
#else
        public PhotonView networkViewObject;
        public GameObject rpcObject;
#endif

        private void Start() {
            requests = gameObject.AddComponent<WebRequests>();
#if NETWORK_UNITY
			networkViewObject = gameObject.AddComponent<NetworkView>();
			networkViewObject.stateSynchronization = NetworkStateSynchronization.Unreliable;
			gameNetworking = gameObject.AddComponent<GameNetworking>();
#else

            //rpcObject = PhotonNetwork.Instantiate("GameNetworkPhotonRPC", Vector3.zero, Quaternion.identity, 0);
            //networkViewObject = rpcObject.GetComponent<PhotonView>();
            //networkViewObject.viewID = PhotonNetwork.AllocateViewID();
            //networkViewObject.synchronization = ViewSynchronization.ReliableDeltaCompressed;
            gameNetworking = gameObject.AddComponent<GameNetworking>();

#endif
            gameverses = GameversesGameAPI.Instance;
            gameverses.InitGameverses();

            //gameverses.GetGameList();

            DontDestroyOnLoad(gameObject);
        }

        private void OnLeftRoom() {
            rpcObject = null;
            networkViewObject = null;
        }

        public void SetupNetworkView() {
            if (rpcObject == null) {
                rpcObject = PhotonNetwork.Instantiate(
                    System.IO.Path.Combine(ContentPaths.appCacheVersionSharedPrefabNetwork, "GameNetworkPhotonRPC")
                    , Vector3.zero, Quaternion.identity, 0);
                networkViewObject = rpcObject.GetComponent<PhotonView>();


                //networkViewObject.viewID = PhotonNetwork.AllocateViewID();
                //networkViewObject.synchronization = ViewSynchronization.ReliableDeltaCompressed;
                //DontDestroyOnLoad(rpcObject);
            }
            if (rpcObject != null) {
                GameNetworkPhotonRPC rpc = rpcObject.GetComponent<GameNetworkPhotonRPC>();
                rpc.uuid = UniqueUtil.Instance.currentUniqueId;
            }
        }
    }
}