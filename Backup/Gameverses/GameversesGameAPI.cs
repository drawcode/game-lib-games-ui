using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Gameverses.Photon;

namespace Gameverses {
	
	public class GameversesKeys {
		public static string ProfileList = "gv-profile-list";
		public static string Profile = "gv-profile";
		public static string ProfileData = "gv-profile-data";
	}
	
	/*
	public class GameversesProfile : DataObject {
		
		public string GetKey(string username) {
			return GameversesKeys.Profile 
				+ "-"  + System.Uri.EscapeUriString(username).ToString();	
		}
		
		public void Load(string username) {			
			if(PlatformContext.Current.isWeb) {
				LoadDataFromPrefs(GetKey(username));
			}
			else {
				//LoadDataFromWeb();
			}
		}
	}
	*/
	
	public class GameversesGameAPI {
		
		private static volatile GameversesGameAPI instance;
		private static System.Object syncRoot = new System.Object();
		
		public GameversesProfile currentProfile;
		public string currentProfileId = "";
		
		public GameversesGameSession currentSession;
		public GameversesGameSessionData currentSessionData;
#if NETWORK_UNITY		
		public NetworkPlayer currentPlayer;
#else
		public PhotonPlayer currentPlayer;
#endif
		public HostData currentHostData;
		
		public HostData currentClientHostData;
		
		public static GameversesGameAPI Instance {
			get {
				if (instance == null) {
					lock (syncRoot) {
						if (instance == null) {
							instance = new GameversesGameAPI();
						}
					}
				}
				return instance;
			}
		}
		
		public GameversesGameAPI () {			
			Reset();
		}
		
		public void Reset() {
			currentProfile = new GameversesProfile();
			
			LoadProfile("default");
			
			try {
				//GetGameList();
			}
			catch (Exception e) {
				LogUtil.Log(e);
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
			GameMessenger<GameNetworkingType>.AddListener(GameNetworkingMessages.PlayerConnected, OnNetworkPlayerConnectedHandler);
			GameMessenger<GameNetworkingType, GameNetworkingType>.AddListener(GameNetworkingMessages.SerializeNetworkView, OnNetworkSerializeNetworkViewHandler);
			GameMessenger.AddListener(GameNetworkingMessages.ConnectionTested, OnNetworkConnectionTestedHandler);
		}
		
		void OnDisable() {
			GameMessenger<GameNetworkingType>.RemoveListener(GameNetworkingMessages.ServerInitialized, OnNetworkServerInitializedHandler);
			GameMessenger<GameNetworkingType>.RemoveListener(GameNetworkingMessages.ConnectedToServer, OnNetworkConnectedToServerHandler);
			GameMessenger<GameNetworkingType>.RemoveListener(GameNetworkingMessages.DisconnectedFromServer, OnNetworkDisconnectedFromServerHandler);
			GameMessenger<GameNetworkingType>.RemoveListener(GameNetworkingMessages.FailedToConnect, OnNetworkFailedToConnectHandler);	
			GameMessenger<GameNetworkingType>.RemoveListener(GameNetworkingMessages.NetworkInstantiate, OnNetworkInstantiateHandler);
			GameMessenger<GameNetworkingType>.RemoveListener(GameNetworkingMessages.PlayerConnected, OnNetworkPlayerConnectedHandler);
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
		
		void OnNetworkPlayerConnectedHandler(GameNetworkingType gameType) {
			//LogUtil.Log("GameversesGameAPI::OnNetworkPlayerConnectedHandler: " + player);
		}
		
		void OnNetworkSerializeNetworkViewHandler(GameNetworkingType streamObject, GameNetworkingType infoObject) {
			//BitStream stream = (gameType.type)gameType.itemObject;
			//NetworkMessageInfo info = (infoObject.type)gameType.infoObject;
			//LogUtil.Log("GameversesGameAPI::OnNetworkSerializeNetworkViewHandler: " + info);
		}
		
		void OnNetworkConnectionTestedHandler() {
			//LogUtil.Log("GameversesGameAPI::OnNetworkConnectionTestedHandler: " + GameNetworking.Instance.hasTestedNAT);
		}
		
		public void InitGameverses() {
			GameversesService.Instance.SetApiInfo();
		}
		
		// ##################################################################################################
		// PROFILES
		
		public void LoadProfile(string username) {
			// TODO look up
			string key = GameversesKeys.Profile 
				+ "-"  + System.Uri.EscapeUriString(username).ToString();
			if(SystemPrefUtil.HasLocalSetting(key)) {
				currentProfileId = SystemPrefUtil.GetLocalSettingString(key);
				currentProfile.uuid = currentProfileId;
			}
		}
		
		// ##################################################################################################
		// GAMES
		
		public void GetGameList() {
			
			LogUtil.Log("GetGameList:" + currentProfileId);
			
			GameversesService.Instance.GetGameList();
			
		}
		
		// ##################################################################################################
		// GAME SESSIONS
		
		public void GetGameSessionList() {
			
			LogUtil.Log("GetGameSessionList:" + currentProfileId);
			
			GameversesService.Instance.GetGameSessionList();
			
		}
		
		public void SetGameSession(GameversesGameSession gameSession) {
						
			currentSession = gameSession;
			
			LogUtil.Log("SetGameSession::currentSession:" + currentSession.uuid);
			
			//currentSession
			
			GameversesService.Instance.SetGameSession(currentSession);
			
		}
		
		public void SyncGameSession() {			
			
			LogUtil.Log("SyncGameSession::currentSession:" + currentSession.uuid);
			GameversesService.Instance.SetGameSession(currentSession);
		}
		
		public void SetGameSessionState(string gameState) {
				currentSession.game_state = gameState;
			
				LogUtil.Log("SetGameSessionState::currentSession:" + gameState);
				
				//currentSession
				
				GameversesService.Instance.SetGameSessionState(currentSession);
			
		}
		
		public void ConnectGameSession(GameversesGameSession gameSession) {
			currentSession = gameSession;
			currentSessionData = new GameversesGameSessionData();
			
			currentClientHostData = new HostData();
			currentClientHostData.connectedPlayers = gameSession.game_players_connected;
			currentClientHostData.gameName = gameSession.uuid;
			currentClientHostData.gameType = gameSession.uuid;
			currentClientHostData.guid = gameSession.network_uuid;
			currentClientHostData.ip = new string[2]{gameSession.network_ip, gameSession.network_external_ip};
			currentClientHostData.passwordProtected = false;
			currentClientHostData.playerLimit = gameSession.game_players_allowed;
			currentClientHostData.port = gameSession.network_external_port;
			currentClientHostData.useNat = gameSession.network_use_nat;
			
			GameNetworking.Instance.ConnectGameSession(currentClientHostData);
		}
		
		public void DisconnectGameSession() {
			GameNetworking.Instance.DisconnectNetwork();
		}
		
		public void SetupNetworkGameSession(string uuid, PhotonPlayer player, HostData hostData) {
			
			currentPlayer = player;
			currentHostData = hostData;
			
			currentSession = new GameversesGameSession();
			currentSession.active = true;
			currentSession.date_created = DateTime.Now;
			currentSession.date_modified = DateTime.Now;
			
			currentSession.network_external_ip = UniqueUtil.Instance.currentUniqueId;
			currentSession.network_external_port = hostData.port;
			
			currentSession.game_players_allowed = 4;
			currentSession.game_players_connected = 1;
			currentSession.game_type = "demogame";
			currentSession.game_level = "chapter-1-1";
			currentSession.game_area = "here";
			currentSession.game_state = "init";
			currentSession.network_ip = uuid;
			currentSession.network_port = hostData.port;
			currentSession.network_uuid = uuid;
			currentSession.network_use_nat = currentHostData.useNat;
			
			currentSession.status = "";
			currentSession.uuid = uuid;
			currentSession.profile_id = UniqueUtil.CreateUUID4();
			currentSession.game_id = "11111111-1111-1111-1111-111111111111";
			
			SetGameSession(currentSession);
			
			LogUtil.Log("GameversesService.Instance: " + uuid);
			
		}
		
		public void SetupNetworkGameSession(string uuid, NetworkPlayer player, HostData hostData) {
#if NETWORK_UNITY
			if(!Network.isServer) 
				return;
		
			currentPlayer = player;
			currentHostData = hostData;
			
			currentSession = new GameversesGameSession();
			currentSession.active = true;
			currentSession.date_created = DateTime.Now;
			currentSession.date_modified = DateTime.Now;
			
			currentSession.network_external_ip = currentPlayer.externalIP;
			currentSession.network_external_port = currentPlayer.externalPort;
			
			currentSession.game_players_allowed = 4;
			currentSession.game_players_connected = 1;
			currentSession.game_type = "demogame";
			currentSession.game_level = "chapter-1-1";
			currentSession.game_area = "here";
			currentSession.game_state = "init";
			currentSession.network_ip = currentPlayer.ipAddress;
			currentSession.network_port = currentPlayer.port;
			currentSession.network_uuid = currentPlayer.guid;
			currentSession.network_use_nat = currentHostData.useNat;
			
			currentSession.status = "";
			currentSession.uuid = uuid;
			currentSession.profile_id = UniqueUtil.CreateUUID4();
			currentSession.game_id = "11111111-1111-1111-1111-111111111111";
			
			SetGameSession(currentSession);
			
			LogUtil.Log("GameversesService.Instance: " + currentPlayer.externalPort);
#endif
		}
		
		public void SendActionMessage(GameNetworkingAction action, Vector3 pos, Vector3 direction) {
			GameNetworking.Instance.SendActionMessage(action, pos, direction);
		}
				
		// ##################################################################################################
		
		
	}
}

