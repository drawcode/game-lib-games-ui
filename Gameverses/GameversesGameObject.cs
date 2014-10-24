using System;
using System.Collections;
using System.Collections.Generic;
using Gameverses;

using UnityEngine;
using Engine.Networking;

namespace Gameverses {
    public class GameversesGameObject : GameObjectBehavior {
        public GameversesGameAPI gameverses;
        public GameversesService gameversesService;
        public GameNetworking gameNetworking;
        public WebRequests requests;

#if NETWORK_UNITY
        public NetworkView networkViewObject;
        #elif NETWORK_PHOTON
        public PhotonView networkViewObject;
        public GameObject rpcObject;
        public GameNetworkPhotonRPC rpc;
#endif

        private void Start() {
            requests = gameObject.AddComponent<WebRequests>();
#if NETWORK_UNITY
            networkViewObject = gameObject.AddComponent<NetworkView>();
            networkViewObject.stateSynchronization = NetworkStateSynchronization.Unreliable;
            gameNetworking = gameObject.AddComponent<GameNetworking>();
            #elif NETWORK_PHOTON

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
#if NETWORK_UNITY || NETWORK_PHOTON
            rpcObject = null;
            networkViewObject = null;
#endif
        }

        public void SetupNetworkView() {
            //if(GameNetworking.Instance.isServer) {
            
            #if NETWORK_PHOTON
            string uid = UniqueUtil.Instance.currentUniqueId;

            if (rpcObject == null) {
                foreach(GameNetworkPhotonRPC rpcItem in UnityObjectUtil.FindObjects<GameNetworkPhotonRPC>()) {
                    if(rpcItem.uniqueId == uid && !string.IsNullOrEmpty(uid)) {
                        rpcObject = rpcItem.gameObject;
                        if(rpc == null) {
                            rpc = rpcObject.GetComponent<GameNetworkPhotonRPC>();
                            rpc.uniqueId = uid;
                        }
                    }
                }
            }

            if (rpcObject == null) {
                string path = System.IO.Path.Combine(ContentPaths.appCacheVersionSharedPrefabNetwork, "GameNetworkPhotonRPC");

                rpcObject = PhotonNetwork.Instantiate(path
                        , Vector3.zero, Quaternion.identity, 0);             

                //networkViewObject.viewID = PhotonNetwork.AllocateViewID();
                //networkViewObject.synchronization = ViewSynchronization.ReliableDeltaCompressed;
                //DontDestroyOnLoad(rpcObject);
            }
            if (rpcObject != null) {

                networkViewObject = rpcObject.GetComponent<PhotonView>();   

                if(rpc == null) {
                    rpc = rpcObject.GetComponent<GameNetworkPhotonRPC>();
                    rpc.uniqueId = uid;
                }
                //rpc.uniqueId = UniqueUtil.Instance.currentUniqueId;
            }
            //}
            #endif
        }
    }
}