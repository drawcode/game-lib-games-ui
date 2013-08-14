#define NETWORK_PHOTON

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Gameverses.Photon;

namespace Gameverses {
	
	public class GameNetworkingPlayerTypeMessages {
		public static string PlayerTypeHero = "type-hero";
		public static string PlayerTypeEnemy = "type-enemy";
		public static string PlayerTypeProjectile = "type-projectile";
		public static string PlayerTypeAction = "type-action";
	}
	
	public class GameNetworkingType {
		public System.Object itemObject;
		public System.Type type;
		public string name;
	}
	
	public class GameNetworkingMessages {
		public static string ConnectionTested = "network-connection-tested";
		public static string ServerInitialized = "network-server-initialized";
		public static string ConnectedToServer = "network-connected-to-sever";
		public static string DisconnectedFromServer = "network-disconnected-from-server";
		public static string FailedToConnect = "network-failed-to-connect";
		public static string NetworkInstantiate = "network-instantiate";
		public static string PlayerConnected = "network-player-connected";
		public static string PlayerDisconnected = "network-player-disconnected";
		public static string SerializeNetworkView = "network-serialize-network-view";
		public static string ActionEvent = "network-action-event";
		public static string ActionNetworkViewEvent = "network-action-network-view-event";
	}
	
	public class GameNetworkingAction {
		public string uuid = "";
		public string uuidOwner = "";
		public string code = "";
		public string type = "";
		public Dictionary<string, string> attributes;
		
		public GameNetworkingAction() {
			Reset();
		}
		
		public void Reset() {
            uuid = "";
			uuidOwner = "";
			code = "";
			type = "";
			attributes = new Dictionary<string, string>();
		}
	}
	
	public class GameNetworkingPlayerInfo {
#if NETWORK_UNITY
		public NetworkPlayer networkPlayer;
#else
		public PhotonPlayer networkPlayer;
#endif
		public string name;
		public string uuid;
		public string type;
		public Transform transform;
		public string deviceId;
		public bool isLocal;
		public Dictionary<string, string> attributes;
		private string localDeviceId;
		
		public GameNetworkingPlayerInfo() {
			Reset();
		}
		
		public void Reset() {
#if NETWORK_UNITY
			networkPlayer = Network.player;
#else
			networkPlayer = PhotonNetwork.player;
#endif
            name = "default";
            type = "default";
            transform = null;
            isLocal = true;
			uuid = UniqueUtil.Instance.currentUniqueId;
			localDeviceId = SystemInfo.deviceUniqueIdentifier;			
			deviceId = localDeviceId;
			attributes = new Dictionary<string, string>();
		}

        public GameNetworkingPlayerInfo Clone() {
            GameNetworkingPlayerInfo np = new GameNetworkingPlayerInfo();
            np.networkPlayer = networkPlayer;
            np.name = name;
            np.type = type;
            np.transform = transform;
            np.isLocal = isLocal;
			np.deviceId = deviceId;
			np.attributes = attributes; 
            return np;
        }
    }
	
	public class GameNetworking : MonoBehaviour {
			
		public static GameNetworking Instance;
		
		public HostData currentHostData;
		public HostData currentGameSessionHostData;	
		
		public int connectedPlayerCount = 0;
		
		public HostData[] hostDataMasterServer;
		public HostData hostDataGameSession;
		
		float currentTimeBlock = 0.0f;
		float currentTimeBlockDelayed = 0.0f;
		float actionInterval = 1.0f;	
		
		public string localDeviceId = "";	
				
		public string currentPlayerName = "default";
		
		public bool pseudoMultiplayer = false; // multiplayer faked with AI
		public bool useExistingTransforms = true; 
		public bool connectingAsClient = false;
		
	    public void Awake() {
			
	        if (Instance != null && this != Instance) {
	            //There is already a copy of this script running
	            Destroy(this);
	            return;
	        }
			
	        Instance = this;
					
			useExistingTransforms = true;
			
			//localDeviceId = Puid.New();
			localDeviceId = SystemInfo.deviceUniqueIdentifier;

#if NETWORK_PHOTON
			 gameObject.AddComponent<GameNetworkingUnityPhoton>();
#elif NETWORK_UNITY
			gameObject.AddComponent<GameNetworkingUnity>();
#endif
			DontDestroyOnLoad(gameObject);
		}
		
		void Start() {
			Init();	
		}
		
		void Init() {
			// TODO
			// check for invite
			// check for friends
			// else start server
			#if NETWORK_PHOTON
				//GameNetworkingUnityPhoton.Instance.ServerStart();
			#elif NETWORK_UNITY
				//GameNetworkingUnity.Instance.ServerStart();
			#endif
		}
		
		// ##################################################################################################
		// PROPERTIES
		
		public bool isNetworkRunning {
			get {
			#if NETWORK_PHOTON
				return GameNetworkingUnityPhoton.Instance.isNetworkRunning;
			#elif NETWORK_UNITY
				return GameNetworkingUnity.Instance.isNetworkRunning;
			#endif
			}
		}
		
		public bool isServer {
			get {
			#if NETWORK_PHOTON
				return GameNetworkingUnityPhoton.Instance.isServer;
			#elif NETWORK_UNITY
				return GameNetworkingUnity.Instance.isServer;
			#endif
			}
		}
		
		public bool isClient {
			get {
			#if NETWORK_PHOTON
				return GameNetworkingUnityPhoton.Instance.isClient;
			#elif NETWORK_UNITY
				return GameNetworkingUnity.Instance.isClient;
			#endif
			}
		}
		
		public bool isConnected {
			get {
			#if NETWORK_PHOTON
				return GameNetworkingUnityPhoton.Instance.isConnected;
			#elif NETWORK_UNITY
				return GameNetworkingUnity.Instance.isConnected;
			#endif
			}
		}
		
		public bool isConnecting {
			get {
			#if NETWORK_PHOTON
				return GameNetworkingUnityPhoton.Instance.isConnecting;
			#elif NETWORK_UNITY
				return GameNetworkingUnity.Instance.isConnecting;
			#endif
			}
		}
		
		public bool isDisconnected {
			get {
			#if NETWORK_PHOTON
				return GameNetworkingUnityPhoton.Instance.isDisconnected;
			#elif NETWORK_UNITY
				return GameNetworkingUnity.Instance.isDisconnected;
			#endif
			}
		}
				
		public bool isMessageQueueRunning {
			get {
			#if NETWORK_PHOTON
				return GameNetworkingUnityPhoton.Instance.isMessageQueueRunning;
			#elif NETWORK_UNITY
				return GameNetworkingUnity.Instance.isMessageQueueRunning;
			#endif
			}
		}
		
		// ##################################################################################################
		// EVENTS
			
		void OnEnable() {
			GameMessenger<GameNetworkingType>.AddListener(GameNetworkingMessages.ServerInitialized, OnNetworkServerInitializedHandler);
			GameMessenger<GameNetworkingType>.AddListener(GameNetworkingMessages.ConnectedToServer, OnNetworkConnectedToServerHandler);
			GameMessenger<GameNetworkingType>.AddListener(GameNetworkingMessages.DisconnectedFromServer, OnNetworkDisconnectedFromServerHandler);
			GameMessenger<GameNetworkingType>.AddListener(GameNetworkingMessages.FailedToConnect, OnNetworkFailedToConnectHandler);	
			GameMessenger<GameNetworkingType>.AddListener(GameNetworkingMessages.NetworkInstantiate, OnNetworkInstantiateHandler);
			GameMessenger<GameNetworkingType>.AddListener(GameNetworkingMessages.PlayerConnected, OnPhotonPlayerConnectedHandler);
			GameMessenger<GameNetworkingType, GameNetworkingType>.AddListener(GameNetworkingMessages.SerializeNetworkView, OnNetworkSerializeNetworkViewHandler);
			GameMessenger.AddListener(GameNetworkingMessages.ConnectionTested, OnNetworkConnectionTestedHandler);
		}
		
		void OnDisable() {
			GameMessenger<GameNetworkingType>.RemoveListener(GameNetworkingMessages.ServerInitialized, OnNetworkServerInitializedHandler);
			GameMessenger<GameNetworkingType>.RemoveListener(GameNetworkingMessages.ConnectedToServer, OnNetworkConnectedToServerHandler);
			GameMessenger<GameNetworkingType>.RemoveListener(GameNetworkingMessages.DisconnectedFromServer, OnNetworkDisconnectedFromServerHandler);
			GameMessenger<GameNetworkingType>.RemoveListener(GameNetworkingMessages.FailedToConnect, OnNetworkFailedToConnectHandler);	
			GameMessenger<GameNetworkingType>.RemoveListener(GameNetworkingMessages.NetworkInstantiate, OnNetworkInstantiateHandler);
			GameMessenger<GameNetworkingType>.RemoveListener(GameNetworkingMessages.PlayerConnected, OnPhotonPlayerConnectedHandler);
			GameMessenger<GameNetworkingType, GameNetworkingType>.RemoveListener(GameNetworkingMessages.SerializeNetworkView, OnNetworkSerializeNetworkViewHandler);
			GameMessenger.RemoveListener(GameNetworkingMessages.ConnectionTested, OnNetworkConnectionTestedHandler);
		}
		
		// ##################################################################################################
		// HANDLERS

		void OnNetworkServerInitializedHandler(GameNetworkingType gameType) {
			//player = (type)player;
			//LogUtil.Log("GameversesGameAPI::OnNetworkServerInitializedHandler: " + player.externalIP);
		}
		
		void OnNetworkConnectedToServerHandler(GameNetworkingType gameType) {
			//LogUtil.Log("GameversesGameAPI::OnNetworkConnectedToServerHandler: " + player.externalIP);
		}
		
		void OnNetworkDisconnectedFromServerHandler(GameNetworkingType gameType) {
			//LogUtil.Log("GameversesGameAPI::OnNetworkDisconnectedFromServerHandler: " + info);
		}
		
		void OnNetworkFailedToConnectHandler(GameNetworkingType gameType) {
			//LogUtil.Log("GameversesGameAPI::OnNetworkFailedToConnectHandler: " + error);
		}
		
		void OnNetworkInstantiateHandler(GameNetworkingType gameType) {
			//LogUtil.Log("GameversesGameAPI::OnNetworkInstantiateHandler: " + info);
		}
		
		void OnPhotonPlayerConnectedHandler(GameNetworkingType gameType) {
			//LogUtil.Log("GameversesGameAPI::OnPhotonPlayerConnectedHandler: " + player);
		}
		
		void OnNetworkSerializeNetworkViewHandler(GameNetworkingType streamObject, GameNetworkingType infoObject) {
			//BitStream stream = (gameType.type)gameType.itemObject;
			//NetworkMessageInfo info = (infoObject.type)gameType.infoObject;
			//LogUtil.Log("GameversesGameAPI::OnNetworkSerializeNetworkViewHandler: " + info);
		}
		
		void OnNetworkConnectionTestedHandler() {
			//LogUtil.Log("GameversesGameAPI::OnNetworkConnectionTestedHandler: " + GameNetworking.Instance.hasTestedNAT);
		}
		
		// ##################################################################################################
		// SERVER
		
		public void ServerStart() {	
			
		#if NETWORK_PHOTON
			GameNetworkingUnityPhoton.Instance.ServerStart();
		#elif NETWORK_UNITY
			GameNetworkingUnity.Instance.ServerStart();
		#endif
		}
		
		// ##################################################################################################
		// CLIENT
		
		
		void SetGameSessionComplete() {
			GameversesGameAPI.Instance.SetGameSessionState("complete");
			#if NETWORK_PHOTON
			GameNetworkingUnityPhoton.Instance.SetGameSessionComplete();
			#elif NETWORK_UNITY
			GameNetworkingUnity.Instance.SetGameSessionComplete();
			#endif
		}
		
		public void ConnectGameSession(HostData hostData) {
			DisconnectNetwork();	
			#if NETWORK_PHOTON
			GameNetworkingUnityPhoton.Instance.ConnectGameSession(hostData);
			#elif NETWORK_UNITY
			GameNetworkingUnity.Instance.ConnectGameSession(hostData);
			#endif
		}
		
		public void ConnectNetwork(HostData hostData) {
			#if NETWORK_PHOTON
			GameNetworkingUnityPhoton.Instance.ConnectNetwork(hostData);
			#elif NETWORK_UNITY
			GameNetworkingUnity.Instance.ConnectNetwork(hostData);
			#endif
		}
		
		public void DisconnectNetwork() {
			#if NETWORK_PHOTON
			GameNetworkingUnityPhoton.Instance.DisconnectNetwork();
			#elif NETWORK_UNITY
			GameNetworkingUnity.Instance.DisconnectNetwork();
			#endif
		}
		
		public void ConnectGameversesMultiuserGameSession(HostData hostData) {
			hostDataGameSession = hostData;
			ConnectNetwork(hostData);		
		}
		
		public void ConnectMasterServerGameSession(HostData hostData) {
	        #if NETWORK_PHOTON
			GameNetworkingUnityPhoton.Instance.DisconnectNetwork();
			#elif NETWORK_UNITY
			GameNetworkingUnity.Instance.DisconnectNetwork();
			#endif
	    }
		
		public void SendActionMessage(GameNetworkingAction action, Vector3 pos, Vector3 direction) {
			#if NETWORK_PHOTON
			GameNetworkingUnityPhoton.Instance.SendActionMessage(action, pos, direction);
			#elif NETWORK_UNITY
			GameNetworkingUnity.Instance.SendActionMessage(action, pos, direction);
			#endif
		}
				
		void Update() {
			currentTimeBlock += Time.deltaTime;
			currentTimeBlockDelayed += Time.deltaTime;
			
			
			if(currentTimeBlock > actionInterval) {
				currentTimeBlock = 0.0f;				
			}
			
			if(currentTimeBlockDelayed + 25 > actionInterval) {
				currentTimeBlockDelayed = 0.0f;
				/*
				 * 
				LogUtil.Log("isNetworkRunning:" + isNetworkRunning);
				LogUtil.Log("isServer:" + isServer);
				LogUtil.Log("isClient:" + isClient);
				LogUtil.Log("isConnected:" + isConnected);
				LogUtil.Log("isConnecting:" + isConnecting);
				LogUtil.Log("isDisconnected:" + isDisconnected);
				
			
				if (Network.peerType == NetworkPeerType.Disconnected) {
	            	LogUtil.Log("Not Connected");
				}
	        	else {
	            	if (Network.peerType == NetworkPeerType.Connecting) {
	                	LogUtil.Log("Connecting");
					}
	            	else {
	                	LogUtil.Log("Network started");
					}
				}
				*/
			}
		}
		
		//
		//
		//
		//
		
			/*	
		public int PlayerCount {
	        get { 
				if(playerList != null)
					return playerList.Count; 
				else
					return 0;
			}
	    }
			
	    public List<GameNetworkingPlayerInfo> GetPlayerList() {
	        return playerList;
	    }
	
	    public List<GameNetworkingPlayerInfo> GetPlayerListCopy() {
			List<GameNetworkingPlayerInfo> newPlayerList = new List<GameNetworkingPlayerInfo>();
			foreach(GameNetworkingPlayerInfo playerInfo in playerList) {
				newPlayerList.Add(playerInfo);
			}
	        return newPlayerList;
	    }
		
		
	    void SetNetworkViewIDs(GameObject go, NetworkViewID id1) {
	        Component[] nViews = go.GetComponentsInChildren<NetworkView>();
			(nViews[0] as NetworkView).viewID = id1;
	    }	
		
		void FindOrCreateNetworkObjectsContainer() {
			networkObjectsContainerName = "GameversesNetworkContainers";
			if(networkObjectsContainer == null) {
				networkObjectsContainer = GameObject.Find(networkObjectsContainerName);
			}
			if(networkObjectsContainer == null) {
				networkObjectsContainer = new GameObject(networkObjectsContainerName);
				DontDestroyOnLoad(networkObjectsContainer);
			}
		}
			
	    [RPC]
	    public void SpawnOnNetwork(Vector3 pos, 
			Quaternion rot, 
			NetworkViewID id1, 
			string playerName, 
			string playerType, 
			string uuid, 
			bool amOwner, 
			NetworkPlayer np) {
			
			if(playerType == "enemy") {
			
			}
			
			Gameverses.LogUtil.Log("GameNetworking: SpawnOnNetwork: uuid:" + uuid + " playerName:" + playerName + " playerType:" + playerType + " id1:" + id1);
						
			GameNetworkPlayerContainer playerContainer = (Instantiate(Resources.Load("Prefabs/GameNetworkPlayerObject"), Vector3.zero, Quaternion.identity) as GameObject).GetComponent<GameNetworkPlayerContainer>();
			playerContainer.gameObject.transform.parent = networkObjectsContainer.transform;
			playerContainer.uuid = uuid;

	        SetPlayerTransform(np, playerContainer.gameObject.transform);
			playerContainer.AddNetworkView(id1, uuid);
	        //SetNetworkViewIDs(playerContainer.gameObject, id1);
	
	        if (amOwner) {
	            localTransform = playerContainer.gameObject.transform;
	        }
		}
		
		public void SendActionMessage(GameNetworkingAction action, Vector3 pos, Vector3 direction) {
			//ActionOnNetwork(
			//string actionMessage,
			//Vector3 pos, 
			//Vector3 direction
			
			string actionValue = JsonMapper.ToJson(action);
				
        	networkView.RPC("ActionOnNetwork", 
				RPCMode.All, 
				actionValue, 
				pos, 
				direction);
		}
		
		[RPC]
	    public void ActionOnNetwork(
			string actionMessage,
			Vector3 pos, 
			Vector3 direction) {
			
			GameNetworkingAction action = JsonMapper.ToObject<GameNetworkingAction>(actionMessage);
			
			if(action.type == GameNetworkingPlayerTypeMessages.PlayerTypeHero) {
			
			}
			else if(action.type == GameNetworkingPlayerTypeMessages.PlayerTypeEnemy) {
			
			}
			else if(action.type == GameNetworkingPlayerTypeMessages.PlayerTypeAction) {
				
			}
			else if(action.type == GameNetworkingPlayerTypeMessages.PlayerTypeProjectile) {
			
			}
			
			Gameverses.LogUtil.Log("GameNetworking: ActionOnNetwork: uuid:" + action.uuid  + " code:" + action.code + " uuidOwner:" + action.uuidOwner + " type:" + action.type);
			
			GameMessenger<GameNetworkingAction, Vector3, Vector3>.Broadcast(GameNetworkingMessages.ActionEvent, action, pos, direction);
			
			/////////GameNetworkPlayerContainer playerContainer = (Instantiate(Resources.Load("Prefabs/GameNetworkPlayerObject"), Vector3.zero, Quaternion.identity) as GameObject).GetComponent<GameNetworkPlayerContainer>();
			playerContainer.gameObject.transform.parent = networkObjectsContainer.transform;
			playerContainer.uuid = uuid;

	        SetPlayerTransform(np, playerContainer.gameObject.transform);
			playerContainer.AddNetworkView(id1, uuid);
	        //SetNetworkViewIDs(playerContainer.gameObject, id1);
	
	        if (amOwner) {
	            localTransform = playerContainer.gameObject.transform;
	        }
		}
		
		[RPC]
	    public void ActionOnNetworkView(
			NetworkPlayer np,
			NetworkViewID id1, 
			Vector3 pos, 
			Quaternion rot, 
			string actionMessage,
			Vector3 direction,
			bool amOwner) {
			
			GameNetworkingAction action = JsonMapper.ToObject<GameNetworkingAction>(actionMessage);
			
			if(action.type == GameNetworkingPlayerTypeMessages.PlayerTypeHero) {
			
			}
			else if(action.type == GameNetworkingPlayerTypeMessages.PlayerTypeEnemy) {
			
			}
			else if(action.type == GameNetworkingPlayerTypeMessages.PlayerTypeAction) {
			
			}
			else if(action.type == GameNetworkingPlayerTypeMessages.PlayerTypeProjectile) {
			
			}
			
			//Gameverses.LogUtil.Log("GameNetworking: ActionOnNetwork: uuid:" + action.uuid  + " code:" + action.code + " uuidOwner:" + action.uuidOwner + " type:" + action.type);
			
			
			////////////////////
			GameNetworkPlayerContainer playerContainer = (Instantiate(Resources.Load("Prefabs/GameNetworkPlayerObject"), Vector3.zero, Quaternion.identity) as GameObject).GetComponent<GameNetworkPlayerContainer>();
			playerContainer.gameObject.transform.parent = networkObjectsContainer.transform;
			playerContainer.uuid = uuid;

	        SetPlayerTransform(np, playerContainer.gameObject.transform);
			playerContainer.AddNetworkView(id1, uuid);
	        //SetNetworkViewIDs(playerContainer.gameObject, id1);
	
	        if (amOwner) {
	            localTransform = playerContainer.gameObject.transform;
	        }
		}
		
	    [RPC]
	    void SetNetworkPlayer(NetworkPlayer networkPlayer, string username, string type) {
			
			Gameverses.LogUtil.Log("GameNetworking: SetNetworkPlayer: username:" + username + " type:" + type);
			
			Gameverses.LogUtil.Log("GameNetworking: SetNetworkPlayer: networkPlayer:" + networkPlayer.guid);
			
			if(playerList == null) {
				playerList = new List<GameNetworkingPlayerInfo>();
			}
			
			GameNetworkingPlayerInfo currentPlayer = GetPlayer(networkPlayer);
			
			Gameverses.LogUtil.Log("GameNetworking: SetNetworkPlayer: username:" + username + " type:" + type);
	        
			bool found = false;
			
			if (currentPlayer == null) {				
				currentPlayer = new GameNetworkingPlayerInfo();
			}
			else {
				found = true;				
			}
				
			Gameverses.LogUtil.Log("GameNetworking: SetNetworkPlayer: currentPlayer:" + currentPlayer.name);
            
            currentPlayer.networkPlayer = networkPlayer;
			currentPlayer.name = username;
			currentPlayer.type = type;
				
			if (Network.player == networkPlayer || Network.player + "" == "-1") {
	            currentPlayer.isLocal = true;
	        }
			
			if(found) {				
				playerList.Remove(currentPlayer);
			}
			
			playerList.Add(currentPlayer);
			
			Gameverses.LogUtil.Log("GameNetworking: SetNetworkPlayer: UPDATED: username:" + username + " type:" + type);
				
			FindOrCreateNetworkObjectsContainer();
	    }		
		
	    public void UpdatePlayerAttributeValue(GameNetworkingPlayerInfo networkPlayer, string key, string keyValue) {
			UpdatePlayerAttributeSync(networkPlayer, key, keyValue);
	        networkView.RPC("UpdatePlayerAttributeSync", RPCMode.Others, networkPlayer.networkPlayer, key, keyValue);
		}
				
		[RPC]
	    void UpdatePlayerAttributeSync(GameNetworkingPlayerInfo currentPlayer, string key, string keyValue) {
	        Gameverses.LogUtil.Log("GameversesGameAPI: UpdatePlayerAttribute: key:" + key + " keyValue: " + keyValue);
			
	        if (currentPlayer != null) {
	            Gameverses.LogUtil.Log("GameversesGameAPI: UpdatePlayerAttribute: Player already exists! Updating...");

				if(currentPlayer.attributes.ContainsKey(key)) {
					currentPlayer.attributes[key] = keyValue;
	            	Gameverses.LogUtil.Log("GameversesGameAPI: UpdatePlayerAttribute: contained key:" + key + " keyValue: " + keyValue);
				}
				else {
					currentPlayer.attributes.Add(key, keyValue);
	            	Gameverses.LogUtil.Log("GameversesGameAPI: UpdatePlayerAttribute: add key:" + key + " keyValue: " + keyValue);
				}
				
				
				
				SetPlayerAttributes(currentPlayer.uuid, currentPlayer);
	        }
			else {
	            Gameverses.LogUtil.Log("GameversesGameAPI: UpdatePlayerAttribute: Player doesn't exist! Adding...");
		        GameNetworkingPlayerInfo np = new GameNetworkingPlayerInfo();
		        np.networkPlayer = currentPlayer.networkPlayer;
				
				if(np.attributes.ContainsKey(key)) {
					np.attributes[key] = keyValue;
				}
				else {
					np.attributes.Add(key, keyValue);
				}		
				
				if (Network.player == currentPlayer.networkPlayer || Network.player + "" == "-1") {
		            np.isLocal = true;
		        }
		        
				playerList.Add(np);
			}
	    }
	
	    void SetPlayerTransform(NetworkPlayer networkPlayer, Transform pTransform) {
	        
			if (!pTransform) {
	            Gameverses.LogUtil.LogError("GameversesGameAPI: SetPlayersTransform has a NULL playerTransform!");
	        }
	        
			GameNetworkingPlayerInfo thePlayer = GetPlayer(networkPlayer);
	        
			if (thePlayer == null) {
	            Gameverses.LogUtil.LogError("GameversesGameAPI: SetPlayersPlayerTransform: No player found!");
	        }
			
	        thePlayer.transform = pTransform;
	    }		
		
		// -------------------------------------------------------------------------------------
		// PLAYER 	
	
	    [RPC]
	    void RemoveNetworkPlayer(NetworkPlayer networkPlayer) {
			
	        GameNetworkingPlayerInfo thePlayer = GetPlayer(networkPlayer);
	
	        Network.RemoveRPCs(networkPlayer);
			
	        if (Network.isServer) {
	            Network.DestroyPlayerObjects(networkPlayer);
	        }
			
	        if (thePlayer.transform) {
	            Destroy(thePlayer.transform.gameObject);
	        }
			
	        playerList.Remove(thePlayer);
	    }	
	
	    public GameNetworkingPlayerInfo GetPlayer(NetworkPlayer networkPlayer) {
	        foreach (GameNetworkingPlayerInfo np in playerList) {
	            if (np.networkPlayer == networkPlayer) {
	                return np;
	            }
	        }
	        return null;
	    }
		
		public void SetPlayer(NetworkPlayer networkPlayer, GameNetworkingPlayerInfo playerInfo) {
	        for (int i = 0; i < playerList.Count; i++) {
	            if (playerList[i].networkPlayer == networkPlayer) {
	                playerList[i] = playerInfo;
	            }
	        }
	    }
		
		// -------------------------------------------------------------------------------------
		// PLAYER ATTRIBUTES
		
		public void SetPlayerAttributes(string uuid, GameNetworkingPlayerInfo playerInfo) {
			System.Object syncRoot = new System.Object();
			lock (syncRoot) {
		        for (int i = 0; i < playerList.Count; i++) {						
		            if (playerList[i].uuid.ToLower() == uuid.ToLower()) {
						playerList[i].attributes = playerInfo.attributes;
		            }
				}
	        }
	    }
		
		public void SetPlayerAttributeValue(GameNetworkingPlayerInfo playerInfo, string key, string keyValue) {
			UpdatePlayerAttributeValue(playerInfo, key, keyValue);
		}
		
		public void SetPlayerAttributeValue(GameNetworkingPlayerInfo playerInfo, string key, bool keyValue) {
			SetPlayerAttributeValue(playerInfo, key, System.Convert.ToString(keyValue));
		}
		
		public void SetPlayerAttributeValue(GameNetworkingPlayerInfo playerInfo, string key, int keyValue) {
			SetPlayerAttributeValue(playerInfo, key, System.Convert.ToString(keyValue));
		}
		
		public void SetPlayerAttributeValue(GameNetworkingPlayerInfo playerInfo, string key, double keyValue) {
			SetPlayerAttributeValue(playerInfo, key, System.Convert.ToString(keyValue));
		}
		
		public int GetPlayerAttributeValueInt(GameNetworkingPlayerInfo playerInfo, string key) {
			int attValue = 0; 
			
			if(playerInfo.attributes.ContainsKey(key)) {
				string _value = playerInfo.attributes[key];
				if(!string.IsNullOrEmpty(_value)) {
					int.TryParse(_value, out attValue);					
				}
			}
			return attValue;
		}
		
		public bool GetPlayerAttributeValueBool(GameNetworkingPlayerInfo playerInfo, string key) {
			bool attValue = false;
			
			if(playerInfo.attributes.ContainsKey(key)) {
				string _value = playerInfo.attributes[key];
				if(!string.IsNullOrEmpty(_value)) {
					if(!bool.TryParse(_value, out attValue))
						attValue = false;					
				}
			}
			return attValue;
		}
		*/
	}
}
