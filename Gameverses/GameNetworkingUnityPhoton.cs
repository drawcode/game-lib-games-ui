using System;
using System.Collections;
using System.Collections.Generic;
using ExitGames;
using ExitGames.Client;
using ExitGames.Client.Photon;
using ExitGames.Client.Photon.Lite;

using Photon.SocketServer;
using Photon.SocketServer.Security;
using UnityEngine;

using Engine.Data.Json;

namespace Gameverses {

    public class GameNetworkingUnityPhoton : MonoBehaviour {
        public static GameNetworkingUnityPhoton Instance;

        public int maxConnections = 4;

        public bool autoStartServing = false;
        public bool hasTestedNAT = false;
        public bool testedUseNat = false;
        public bool lookForGameSession = false;

        public PhotonPlayer currentPlayer;
        public NetworkPeerType peerType;

        public HostData currentHostData;
        public HostData currentGameSessionHostData;
        public int connectedPlayerCount = 0;

        public HostData[] hostDataMasterServer;
        public HostData hostDataGameSession;

        public PhotonView photonView;

        private float currentTimeBlock = 0.0f;
        private float currentTimeBlockDelayed = 0.0f;
        private float actionInterval = 1.0f;

        public string localDeviceId = "";

        public bool useCloudMasterServer = true;

        public string masterserverGameName = "defaultgame";
        public int defaultServerPort = 50666;
        public int connectTimeoutValue = 30;
        public int connectTestTimeValue = 9;
        public int masterServerPort = 5055;
        public int connectionTesterPort = 25011;
        public int natFacilitatorPort = 25011;
        public string masterserveriPAddressOrDns = "matchup.myserver.com";
        public bool useMasterserverRouting = false;

        public string networkObjectsContainerName = "GameversesNetworkContainers";
        public GameObject networkObjectsContainer;

        public Transform localTransform;
        private List<GameNetworkingPlayerInfo> playerList;

        public string currentPlayerName = "default";

        public bool pseudoMultiplayer = false; // multiplayer faked with AI
        public bool useExistingTransforms = true;
        public bool connectingAsClient = false;

        private string roomToJoin = "";
        private string roomToCreate = "";

        public bool isSessionFilled {
            get {
                return playerList.Count == maxConnections;
            }
        }

        public void Awake() {
            if (Instance != null && this != Instance) {

                //There is already a copy of this script running
                Destroy(this);
                return;
            }

            Instance = this;

            ////FindNetworkView();

            useExistingTransforms = true;

            localDeviceId = UniqueUtil.Instance.currentUniqueId;

            playerList = new List<GameNetworkingPlayerInfo>();

            DontDestroyOnLoad(gameObject);
        }

        private void Start() {
            Init();
        }

        private void Init() {

            // TODO
            // check for invite
            // check for friends
            // else start server

            //if(!isConnected && !isConnecting) {

            //    ConnectToServer();
            //}
        }

        private void FindNetworkView() {
            if (photonView == null) {
                Gameverses.GameversesGameObject gameversesGameObject = ObjectUtil.FindObject<Gameverses.GameversesGameObject>();
                if (gameversesGameObject != null) {
                    gameversesGameObject.SetupNetworkView();

                    foreach (Gameverses.GameNetworkPhotonRPC rpcContainer in ObjectUtil.FindObjects<Gameverses.GameNetworkPhotonRPC>()) {
                        if (rpcContainer.uuid.ToLower() == UniqueUtil.Instance.currentUniqueId.ToLower()) {
                            photonView = rpcContainer.photonView;
                            LogUtil.Log("FindNetworkView::photonView:" + photonView.viewID);//.ID);
                        }
                    }
                }
            }
            /*
            PhotonViewID id1 = PhotonNetwork.AllocateViewID();
            photonView.viewID = id1;
            photonView.observed = gameObject.GetComponent<GameNetworkingUnityPhoton>();
            photonView.synchronization = ViewSynchronization.ReliableDeltaCompressed;
            LogUtil.Log("GameNetworkingUnityPhoton:viewID:" + id1.ID);
             */
        }

        // ##################################################################################################
        // PROPERTIES

        public bool isNetworkRunning {
            get {
                if (isMessageQueueRunning)
                    return true;
                return false;
            }
        }

        public bool isServer {
            get {
                if (PhotonNetwork.isMasterClient) {
                    return true;
                }
                return false;
            }
        }

        public bool isClient {
            get {
                if (!PhotonNetwork.isMasterClient) {
                    return true;
                }
                return false;
            }
        }

        public bool isConnected {
            get {
                if (PhotonNetwork.connected) {
                    return true;
                }
                return false;
            }
        }

        public bool isConnecting {
            get {
                if (PhotonNetwork.connectionState == ConnectionState.Connecting) {
                    return true;
                }
                return false;
            }
        }

        public bool isDisconnected {
            get {
                if (PhotonNetwork.connectionState == ConnectionState.Disconnected) {
                    return true;
                }
                return false;
            }
        }

        public bool isMessageQueueRunning {
            get {
                return PhotonNetwork.isMessageQueueRunning;
            }
        }

        // ##################################################################################################
        // EVENTS
        private void OnEnable() {
            GameMessenger<GameNetworkingType>.AddListener(GameNetworkingMessages.ServerInitialized, OnNetworkServerInitializedHandler);
            GameMessenger<GameNetworkingType>.AddListener(GameNetworkingMessages.ConnectedToServer, OnNetworkConnectedToServerHandler);
            GameMessenger<GameNetworkingType>.AddListener(GameNetworkingMessages.DisconnectedFromServer, OnNetworkDisconnectedFromServerHandler);
            GameMessenger<GameNetworkingType>.AddListener(GameNetworkingMessages.FailedToConnect, OnNetworkFailedToConnectHandler);
            GameMessenger<GameNetworkingType>.AddListener(GameNetworkingMessages.NetworkInstantiate, OnNetworkInstantiateHandler);
            GameMessenger<GameNetworkingType>.AddListener(GameNetworkingMessages.PlayerConnected, OnPhotonPlayerConnectedHandler);
            GameMessenger<GameNetworkingType, GameNetworkingType>.AddListener(GameNetworkingMessages.SerializeNetworkView, OnNetworkSerializeNetworkViewHandler);
            GameMessenger.AddListener(GameNetworkingMessages.ConnectionTested, OnNetworkConnectionTestedHandler);
        }

        private void OnDisable() {
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

        private void OnNetworkServerInitializedHandler(GameNetworkingType gameType) {

            //player = (type)player;
            //LogUtil.Log("GameversesGameAPI::OnNetworkServerInitializedHandler: " + player.externalIP);
        }

        private void OnNetworkConnectedToServerHandler(GameNetworkingType gameType) {

            //LogUtil.Log("GameversesGameAPI::OnNetworkConnectedToServerHandler: " + player.externalIP);
        }

        private void OnNetworkDisconnectedFromServerHandler(GameNetworkingType gameType) {

            //LogUtil.Log("GameversesGameAPI::OnNetworkDisconnectedFromServerHandler: " + info);
        }

        private void OnNetworkFailedToConnectHandler(GameNetworkingType gameType) {

            //LogUtil.Log("GameversesGameAPI::OnNetworkFailedToConnectHandler: " + error);
        }

        private void OnNetworkInstantiateHandler(GameNetworkingType gameType) {

            //LogUtil.Log("GameversesGameAPI::OnNetworkInstantiateHandler: " + info);
        }

        private void OnPhotonPlayerConnectedHandler(GameNetworkingType gameType) {

            //LogUtil.Log("GameversesGameAPI::OnPhotonPlayerConnectedHandler: " + player);
        }

        private void OnNetworkSerializeNetworkViewHandler(GameNetworkingType streamObject, GameNetworkingType infoObject) {

            //BitStream stream = (gameType.type)gameType.itemObject;
            //NetworkMessageInfo info = (infoObject.type)gameType.infoObject;
            //LogUtil.Log("GameversesGameAPI::OnNetworkSerializeNetworkViewHandler: " + info);
        }

        private void OnNetworkConnectionTestedHandler() {

            //LogUtil.Log("GameversesGameAPI::OnNetworkConnectionTestedHandler: " + GameNetworking.Instance.hasTestedNAT);
        }

        // ##################################################################################################
        // SERVER

        public void CheckConnected() {
            if(!isConnected) {
            
            }
        }

        public void ConnectToServer() {
            if (!isConnected && !isConnecting) {
                LogUtil.Log("ConnectToServer: ");
                LogUtil.Log("GameNetworkingUnityPhoton:ConnectToServer:");

                if(useCloudMasterServer) {
                    LogUtil.Log("GameNetworkingUnityPhoton:ConnectToServer:useCloudMasterServer:" + useCloudMasterServer);
                    PhotonNetwork.ConnectUsingSettings("1");
                } 
                else {
                    LogUtil.Log("GameNetworkingUnityPhoton:ConnectToServer:useCloudMasterServer:" + useCloudMasterServer);
                    PhotonNetwork.Connect(masterserveriPAddressOrDns, masterServerPort, "Master", "1.0");
                }
            }
        }

        public void ServerStart() {
            ConnectToServer();
            
            LogUtil.Log("GameNetworkingUnityPhoton:ServerStart:");
            if (autoStartServing) {
                connectingAsClient = false;
                if (PhotonNetwork.connected) {
                    string room = UniqueUtil.Instance.currentUniqueId;
                    JoinRoom(room);
                }
            }
        }

        // ##################################################################################################
        // INTERNAL NETWORK EVENTS

        //OnJoinedRoom

        string lastRoomTry = "";

        private void OnPhotonCreateRoomFailed() {
            LogUtil.Log("OnPhotonCreateRoomFailed: ");
            
            if(useCloudMasterServer) {                
                if(lastRoomTry != roomToJoin) {
                    JoinRoom(lastRoomTry);
                }
            }
            else {
                ServerStart();
            }
        }

        private void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
        }

        private void OnCreatedRoom() {
            LogUtil.Log("OnCreatedRoom: ");
        }

        //OnPhotonPlayerConnected
        private IEnumerator OnJoinedRoom() {
            FindNetworkView();

            if (isServer) {

                // If this is my room I am the server...

                LogUtil.Log("Server initialized and ready");

                playerList.Clear();

                currentPlayer = PhotonNetwork.player;
                currentHostData = new HostData();

                currentHostData.useNat = false;
                LogUtil.Log("currentHostData.useNat: " + currentHostData.useNat);

                currentHostData.guid = UniqueUtil.Instance.currentUniqueId;
                LogUtil.Log("currentHostData.guid: " + currentHostData.guid);

                currentHostData.comment = ""; // profile uuid
                LogUtil.Log("currentHostData.comment: " + currentHostData.comment);

                currentHostData.connectedPlayers = 1;
                LogUtil.Log("currentHostData.connectedPlayers: " + currentHostData.connectedPlayers);

                currentHostData.gameName = "";
                LogUtil.Log("currentHostData.gameName: " + currentHostData.gameName);

                currentHostData.gameType = "";
                LogUtil.Log("currentHostData.gameType: " + currentHostData.gameType);

                //currentHostData.ip[0] = currentPlayer.ipAddress;
                LogUtil.Log("currentPlayer.ipAddress: " + currentHostData.guid);

                currentHostData.passwordProtected = false;
                LogUtil.Log("currentHostData.passwordProtected: " + currentHostData.passwordProtected);

                currentHostData.playerLimit = 4;
                LogUtil.Log("currentHostData.playerLimit: " + currentHostData.playerLimit);

                currentHostData.port = masterServerPort;
                LogUtil.Log("currentHostData.port: " + currentHostData.port);

                LogUtil.Log("currentPlayer.externalIP: " + currentPlayer.ID.ToString());

                LogUtil.Log("currentPlayer.externalPort: " + currentPlayer.ID.ToString());

                string sessionId = UniqueUtil.Instance.CreateUUID4();

                GameversesGameAPI.Instance.SetupNetworkGameSession(sessionId, currentPlayer, currentHostData);

                GameNetworkingType argType = new GameNetworkingType();
                argType.itemObject = PhotonNetwork.player;
                argType.type = typeof(PhotonPlayer);
                GameMessenger<GameNetworkingType>.Broadcast(GameNetworkingMessages.ServerInitialized, argType);

                //SetPhotonPlayer(PhotonNetwork.player, currentPlayerName, GameNetworkingMessages.PlayerTypeHero);

                LogUtil.Log("ServerStartedExisting:  isServer:" + PhotonNetwork.isMasterClient);

                int id1 = PhotonNetwork.AllocateViewID();

                //SetPhotonPlayer(PhotonNetwork.player, UniqueUtil.Instance.currentUniqueId, currentPlayerName, GameNetworkingPlayerTypeMessages.PlayerTypeHero);
                photonView.RPC("SetPhotonPlayer", PhotonTargets.AllBuffered, PhotonNetwork.player, UniqueUtil.Instance.currentUniqueId, currentPlayerName, GameNetworkingPlayerTypeMessages.PlayerTypeHero);

                //SpawnOnNetwork(Vector3.zero,
                //		Quaternion.identity,
                //		id1,
                //		currentPlayerName,
                //		GameNetworkingPlayerTypeMessages.PlayerTypeHero,
                //		UniqueUtil.Instance.currentUniqueId,
                //		false,
                //		PhotonNetwork.player);

                photonView.RPC("SpawnOnNetwork",
                        PhotonTargets.AllBuffered,
                        Vector3.zero,
                        Quaternion.identity,
                        id1,
                        currentPlayerName,
                        GameNetworkingPlayerTypeMessages.PlayerTypeHero,
                        UniqueUtil.Instance.currentUniqueId,
                        false,
                        PhotonNetwork.player);

                //SetPlayer(PhotonNetwork.player, );
                //photonView.RPC("AddPlayer", PhotonTargets.AllBuffered, PhotonNetwork.player, currentPlayerName);
            }

            if (isClient) {
                LogUtil.Log("Connected to server");

                playerList = new List<GameNetworkingPlayerInfo>();

                int id1 = PhotonNetwork.AllocateViewID();
                photonView.RPC("SetPhotonPlayer", PhotonTargets.AllBuffered, PhotonNetwork.player, UniqueUtil.Instance.currentUniqueId, currentPlayerName, GameNetworkingPlayerTypeMessages.PlayerTypeHero);

                photonView.RPC("SpawnOnNetwork",
                    PhotonTargets.AllBuffered,
                    Vector3.zero,
                    Quaternion.identity,
                    id1,
                    currentPlayerName,
                    GameNetworkingPlayerTypeMessages.PlayerTypeHero,
                    UniqueUtil.Instance.currentUniqueId,
                    false,
                    PhotonNetwork.player);
                yield return 0;

                //SetPlayerTransform(PhotonNetwork.player, localTransform);
                //SetPhotonViewIDs(localTransform.gameObject, id1);

                connectingAsClient = false;

                // Broadcast
                GameNetworkingType argType = new GameNetworkingType();
                argType.itemObject = PhotonNetwork.player;
                argType.type = typeof(PhotonPlayer);
                GameMessenger<GameNetworkingType>.Broadcast(GameNetworkingMessages.ConnectedToServer, argType);
            }
        }

        private IEnumerator OnLeftRoom() {
            if (PhotonNetwork.isMasterClient) {
                LogUtil.Log("Local server connection disconnected");

                SetGameSessionComplete();
            }
            else {
                LogUtil.Log("Successfully disconnected from the server");
            }

            //
            // Remove all players except yourself

            yield return new WaitForSeconds(1);

            List<GameNetworkingPlayerInfo> listCopyClone = GetPlayerListCopy();

            foreach (GameNetworkingPlayerInfo np in listCopyClone) {
                if (np.networkPlayer != PhotonNetwork.player) {
                    RemovePhotonPlayer(np.networkPlayer);
                    if (!PhotonNetwork.isMasterClient) {
                        if(PhotonNetwork.room != null) {
                            if (PhotonNetwork.room.playerCount > 0) {

                                //PhotonNetwork.CloseConnection(np.networkPlayer);
                            }
                        }
                    }
                }
            }

            photonView = null;

            // Broadcast
            GameNetworkingType argType = new GameNetworkingType();
            argType.itemObject = "left room";
            argType.type = typeof(String);
            GameMessenger<GameNetworkingType>.Broadcast(GameNetworkingMessages.DisconnectedFromServer, argType);

            if (!connectingAsClient && !useCloudMasterServer) {
                ServerStart();
            }
            else {
                JoinRoom(roomToJoin);
            }
        }

        public void Join(string name) {            
            JoinRoom(name);
        }

        public void Create(string name) {            
            CreateRoom(name);
        }

        public void Leave() {
            LeaveRoom();
        }

        private void JoinRoom(string roomName) {
            LogUtil.Log("GameNetworkingUnityPhoton:JoinRoom:" + roomName);//network_external_ip
            if (!string.IsNullOrEmpty(roomName)) {
                //roomToJoin = roomName;
                roomToJoin = "planet-426";
                ConnectToServer();
                if (isConnected) {
                    LogUtil.Log("GameNetworkingUnityPhoton:JoinRoom:roomToJoin:" + roomToJoin);
                    PhotonNetwork.JoinRoom(roomToJoin);
                }
            }
        }

        private void CreateRoom(string name) {
            LogUtil.Log("GameNetworkingUnityPhoton:CreateRoom:" + name);            
            roomToCreate = name;
            
            //if(useCloudMasterServer) {
            //    lastRoomTry = name;
            //    JoinRoom(name);  
            //}
            //else {
            if (isConnected) {
                LogUtil.Log("GameNetworkingUnityPhoton:CreateRoom:" + true);   
                PhotonNetwork.CreateRoom(name, true, true, 4);
            }
            //}
        }

        private void CreateRoom() {
            string roomToCreate = UniqueUtil.Instance.currentUniqueId;
            roomToCreate = "planet-426";
            CreateRoom(roomToCreate);
        }

        private void LeaveRoom() {
            if(isConnected) {
                if (PhotonNetwork.room != null) {
                    PhotonNetwork.LeaveRoom();
                }
            }
        }

        private void OnConnectedToPhoton() {

        }

        private void HandleConnectedAndReady() {
            
            string uuid = UniqueUtil.Instance.currentUniqueId;
            //LogUtil.Log("PhotonNetwork:OnConnectedToPhoton:" + uuid);
            
            LogUtil.Log("GameNetworkUnityPhoton:HandleConnectedAndReady:" + uuid);

            ServerStart();
            //ServerStart();
            
            //if (!connectingAsClient && !useCloudMasterServer) {
            //    CreateRoom();
            //}
            //else {
            //    JoinRoom(uuid);
            //}
            
        }

        private void OnJoinedLobby() {
            HandleConnectedAndReady();
        }

        private void OnConnectedToMaster() {
            HandleConnectedAndReady();
        }

        public void SetGameSessionComplete() {
            GameversesGameAPI.Instance.SetGameSessionState("complete");
        }

        private int retries = 3;

        private void OnFailedToConnectToPhoton(DisconnectCause error) {
            LogUtil.Log("Could not connect to server: " + error);

            if (retries > 0) {
                ConnectToServer();
                retries--;
            }

            // Broadcast
            GameNetworkingType argType = new GameNetworkingType();
            argType.itemObject = error;
            argType.type = typeof(DisconnectCause);
            GameMessenger<GameNetworkingType>.Broadcast(GameNetworkingMessages.FailedToConnect, argType);
        }

        private void OnPhotonInstantiate(PhotonMessageInfo info) {
            LogUtil.Log("New object instantiated by " + info.sender);

            // Broadcast
            GameNetworkingType argType = new GameNetworkingType();
            argType.itemObject = info;
            argType.type = typeof(PhotonMessageInfo);
            GameMessenger<GameNetworkingType>.Broadcast(GameNetworkingMessages.NetworkInstantiate, argType);
        }

        private void OnPhotonPlayerConnected(PhotonPlayer player) {
            LogUtil.Log("Player " + connectedPlayerCount++ + " connected from " + player.ID);

            // Broadcast
            GameNetworkingType argType = new GameNetworkingType();
            argType.itemObject = player;
            argType.type = typeof(PhotonPlayer);
            GameMessenger<GameNetworkingType>.Broadcast(GameNetworkingMessages.PlayerConnected, argType);

            GameversesGameAPI.Instance.currentSession.game_players_connected = connectedPlayerCount;
            GameversesGameAPI.Instance.SyncGameSession();
        }

        private void OnPhotonPlayerDisconnected(PhotonPlayer player) {
            LogUtil.Log("Clean up after player " + player);

            photonView.RPC("RemovePhotonPlayer", PhotonTargets.All, player);

            PhotonNetwork.RemoveRPCs(player);
            PhotonNetwork.DestroyPlayerObjects(player);

            connectedPlayerCount--;

            // Broadcast
            GameNetworkingType argType = new GameNetworkingType();
            argType.itemObject = player;
            argType.type = typeof(PhotonPlayer);
            GameMessenger<PhotonPlayer>.Broadcast(GameNetworkingMessages.PlayerDisconnected, player);

            if(GameversesGameAPI.Instance != null) {

                if(GameversesGameAPI.Instance.currentSession != null) {
                    GameversesGameAPI.Instance.currentSession.game_players_connected = connectedPlayerCount;
                    GameversesGameAPI.Instance.SyncGameSession();
                }
            }
        }

        public void ConnectGameSession(HostData hostData) {

            //DisconnectNetwork();
            connectingAsClient = true;
            StartCoroutine(ConnectGameSessionDelayed(hostData));//ServerStart()
        }

        private IEnumerator ConnectGameSessionDelayed(HostData hostData) {
            yield return new WaitForSeconds(6f);
            ConnectGameversesMultiuserGameSession(hostData);
        }

        public void ConnectNetwork() {
            if(isConnected && !isConnecting) {
                if (PhotonNetwork.room != null) {
                    PhotonNetwork.LeaveRoom();
                }
                PhotonNetwork.Disconnect();
            }

            ConnectToServer();
        }

        public void ConnectNetwork(HostData hostData) {
            if(isConnected && !isConnecting) {
                if (PhotonNetwork.room != null) {
                    PhotonNetwork.LeaveRoom();
                }
                PhotonNetwork.Disconnect();
            }
            
            ConnectToServer();
            //PhotonNetwork.
            //roomToJoin = hostData.ip[1];
            //JoinRoom(roomToJoin);
        }

        public void DisconnectNetwork() {
            if(isConnected) {
                if (PhotonNetwork.room != null) {
                    PhotonNetwork.LeaveRoom();
                }
                PhotonNetwork.Disconnect();
            }
        }

        public void ConnectGameversesMultiuserGameSession(HostData hostData) {
            hostDataGameSession = hostData;
            ConnectNetwork(hostData);
        }

        private void Update() {
            currentTimeBlock += Time.deltaTime;
            currentTimeBlockDelayed += Time.deltaTime;

            if (currentTimeBlock > actionInterval) {
                currentTimeBlock = 0.0f;
            }

            if (currentTimeBlockDelayed + 25 > actionInterval) {
                currentTimeBlockDelayed = 0.0f;
            }
        }

        public int PlayerCount {
            get {
                if (playerList != null)
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
            foreach (GameNetworkingPlayerInfo playerInfo in playerList) {
                newPlayerList.Add(playerInfo);
            }
            return newPlayerList;
        }

        public void SetPhotonViewIDs(GameObject go, int id1) {
            Component[] nViews = go.GetComponents<PhotonView>();
            (nViews[0] as PhotonView).viewID = id1;

            Component[] nViewsChildren = go.GetComponentsInChildren<PhotonView>();
            (nViewsChildren[0] as PhotonView).viewID = id1;
        }

        public void FindOrCreateNetworkObjectsContainer() {
            networkObjectsContainerName = "GameversesNetworkContainers";
            if (networkObjectsContainer == null) {
                networkObjectsContainer = GameObject.Find(networkObjectsContainerName);
            }
            if (networkObjectsContainer == null) {
                networkObjectsContainer = new GameObject(networkObjectsContainerName);
                DontDestroyOnLoad(networkObjectsContainer);
            }
        }

        public void SpawnOnNetwork(Vector3 pos,
            Quaternion rot,
            int id1,
            string playerName,
            string playerType,
            string uid,
            bool amOwner,
            PhotonPlayer np) {

            if (playerType == "enemy") {
            
            }

            LogUtil.Log("GameNetworking: SpawnOnNetwork: uid:" + 
                        uid + " playerName:" + playerName + " playerType:" + playerType + " id1:" + id1);

            GameNetworkPlayerContainer playerContainer = null;

            foreach(GameNetworkPlayerContainer item 
                    in networkObjectsContainer
                        .GetComponentsInChildren<GameNetworkPlayerContainer>(true)) {
                if(item.uniqueId == uid) {
                    playerContainer = item;
                    break;
                }
            }

            if(playerContainer == null) {

                string prefabPath = System.IO.Path.Combine(
                    ContentPaths.appCacheVersionSharedPrefabNetwork, 
                    "GameNetworkPlayerObject");

                LogUtil.Log("SpawnOnNetwork:" + prefabPath);

                LogUtil.Log("SpawnOnNetwork:networkObjectsContainer:EXISTS:" + networkObjectsContainer != null);

                playerContainer = 
                    (Instantiate(Resources.Load(prefabPath), 
                                 Vector3.zero, Quaternion.identity) 
                        as GameObject).GetComponent<GameNetworkPlayerContainer>();
                            
            }
            LogUtil.Log("SpawnOnNetwork:playerContainer:EXISTS:" + playerContainer != null);

            playerContainer.gameObject.transform.parent = networkObjectsContainer.transform;
            playerContainer.uniqueId = uid;
            playerContainer.name = uid;

            SetPlayerTransform(np, playerContainer.gameObject.transform);
            playerContainer.AddNetworkView(id1, uid);
            SetPhotonViewIDs(playerContainer.gameObject, id1);

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

            photonView.RPC("ActionOnNetwork",
                PhotonTargets.All,
                actionValue,
                pos,
                direction);
        }

        public void ActionOnNetwork(
            string actionMessage,
            Vector3 pos,
            Vector3 direction) {
            GameNetworkingAction action = JsonMapper.ToObject<GameNetworkingAction>(actionMessage);

            if (action.type == GameNetworkingPlayerTypeMessages.PlayerTypeHero) {
            }
            else if (action.type == GameNetworkingPlayerTypeMessages.PlayerTypeEnemy) {
            }
            else if (action.type == GameNetworkingPlayerTypeMessages.PlayerTypeAction) {
            }
            else if (action.type == GameNetworkingPlayerTypeMessages.PlayerTypeProjectile) {
            }

            LogUtil.Log("GameNetworking: ActionOnNetwork: uuid:" + action.uuid + " code:" + action.code + " uuidOwner:" + action.uuidOwner + " type:" + action.type);

            GameMessenger<GameNetworkingAction, Vector3, Vector3>.Broadcast(GameNetworkingMessages.ActionEvent, action, pos, direction);

            /*
            GameNetworkPlayerContainer playerContainer = (Instantiate(Resources.Load("Prefabs/GameNetworkPlayerObject"), Vector3.zero, Quaternion.identity) as GameObject).GetComponent<GameNetworkPlayerContainer>();
            playerContainer.gameObject.transform.parent = networkObjectsContainer.transform;
            playerContainer.uuid = uuid;

            SetPlayerTransform(np, playerContainer.gameObject.transform);
            playerContainer.FindNetworkView(id1, uuid);

            //SetPhotonViewIDs(playerContainer.gameObject, id1);

            if (amOwner) {
                localTransform = playerContainer.gameObject.transform;
            }
            */
        }

        public void ActionOnNetworkView(
            PhotonPlayer np,
            object id1,
            Vector3 pos,
            Quaternion rot,
            string actionMessage,
            Vector3 direction,
            bool amOwner) {
            GameNetworkingAction action = JsonMapper.ToObject<GameNetworkingAction>(actionMessage);

            if (action.type == GameNetworkingPlayerTypeMessages.PlayerTypeHero) {
            }
            else if (action.type == GameNetworkingPlayerTypeMessages.PlayerTypeEnemy) {
            }
            else if (action.type == GameNetworkingPlayerTypeMessages.PlayerTypeAction) {
            }
            else if (action.type == GameNetworkingPlayerTypeMessages.PlayerTypeProjectile) {
            }

            //LogUtil.Log("GameNetworking: ActionOnNetwork: uuid:" + action.uuid  + " code:" + action.code + " uuidOwner:" + action.uuidOwner + " type:" + action.type);

            /*
            GameNetworkPlayerContainer playerContainer = (Instantiate(Resources.Load("Prefabs/GameNetworkPlayerObject"), Vector3.zero, Quaternion.identity) as GameObject).GetComponent<GameNetworkPlayerContainer>();
            playerContainer.gameObject.transform.parent = networkObjectsContainer.transform;
            playerContainer.uuid = uuid;

            SetPlayerTransform(np, playerContainer.gameObject.transform);
            playerContainer.FindNetworkView(id1, uuid);

            //SetPhotonViewIDs(playerContainer.gameObject, id1);

            if (amOwner) {
                localTransform = playerContainer.gameObject.transform;
            }
            */
        }

        public void SetPhotonPlayer(PhotonPlayer networkPlayer, string uuid, string username, string type) {
            LogUtil.Log("GameNetworking: SetPhotonPlayer: uuid:" + uuid + " username:" + username + " type:" + type);

            if (playerList == null) {
                playerList = new List<GameNetworkingPlayerInfo>();
            }

            GameNetworkingPlayerInfo currentPlayer = GetPlayer(networkPlayer);

            LogUtil.Log("GameNetworking: SetPhotonPlayer: uuid:" + uuid + " username:" + username + " type:" + type);

            bool found = false;

            if (currentPlayer == null) {
                currentPlayer = new GameNetworkingPlayerInfo();
            }
            else {
                found = true;
            }

            LogUtil.Log("GameNetworking: SetPhotonPlayer: currentPlayer:" + currentPlayer.name);

            currentPlayer.networkPlayer = networkPlayer;
            currentPlayer.name = username;
            currentPlayer.type = type;
            currentPlayer.uuid = uuid;

            if (PhotonNetwork.player == networkPlayer) { // || PhotonNetwork.player + "" == "-1") {
                currentPlayer.isLocal = true;
            }

            if (found) {
                playerList.Remove(currentPlayer);
            }

            playerList.Add(currentPlayer);

            LogUtil.Log("GameNetworking: SetPhotonPlayer: UPDATED: username:" + username + " type:" + type);

            FindOrCreateNetworkObjectsContainer();
        }

        public void UpdatePlayerAttributeValue(PhotonPlayer networkPlayer, string key, string keyValue) {
            UpdatePlayerAttributeSync(networkPlayer, key, keyValue);
            photonView.RPC("UpdatePlayerAttributeSync", PhotonTargets.Others, networkPlayer, key, keyValue);
        }

        public void UpdatePlayerAttributeSync(PhotonPlayer networkPlayer, string key, string keyValue) {
            GameNetworkingPlayerInfo currentPlayer = GetPlayer(networkPlayer);
            LogUtil.Log("GameversesGameAPI: UpdatePlayerAttribute: key:" + key + " keyValue: " + keyValue);

            if (currentPlayer != null) {
                LogUtil.Log("GameversesGameAPI: UpdatePlayerAttribute: Player already exists! Updating...");

                if (currentPlayer.attributes.ContainsKey(key)) {
                    currentPlayer.attributes[key] = keyValue;
                    LogUtil.Log("GameversesGameAPI: UpdatePlayerAttribute: contained key:" + key + " keyValue: " + keyValue);
                }
                else {
                    currentPlayer.attributes.Add(key, keyValue);
                    LogUtil.Log("GameversesGameAPI: UpdatePlayerAttribute: add key:" + key + " keyValue: " + keyValue);
                }

                SetPlayerAttributes(networkPlayer, currentPlayer);
            }
            else {
                LogUtil.Log("GameversesGameAPI: UpdatePlayerAttribute: Player doesn't exist! Adding...");
                GameNetworkingPlayerInfo np = new GameNetworkingPlayerInfo();
                np.networkPlayer = networkPlayer;

                if (np.attributes.ContainsKey(key)) {
                    np.attributes[key] = keyValue;
                }
                else {
                    np.attributes.Add(key, keyValue);
                }

                if (PhotonNetwork.player == networkPlayer || PhotonNetwork.player + "" == "-1") {
                    np.isLocal = true;
                }

                playerList.Add(np);
            }
        }

        public void SetPlayerTransform(PhotonPlayer networkPlayer, Transform pTransform) {
            if (!pTransform) {
                LogUtil.LogError("GameversesGameAPI: SetPlayersTransform has a NULL playerTransform!");
            }

            GameNetworkingPlayerInfo thePlayer = GetPlayer(networkPlayer);

            if (thePlayer == null) {
                LogUtil.LogError("GameversesGameAPI: SetPlayersPlayerTransform: No player found!");
            }

            thePlayer.transform = pTransform;
        }

        // -------------------------------------------------------------------------------------
        // PLAYER

        public void RemovePhotonPlayer(PhotonPlayer networkPlayer) {
            GameNetworkingPlayerInfo thePlayer = GetPlayer(networkPlayer);

            PhotonNetwork.RemoveRPCs(networkPlayer);

            if (PhotonNetwork.isMasterClient) {
                PhotonNetwork.DestroyPlayerObjects(networkPlayer);
            }

            if(thePlayer != null) {                
                if (thePlayer.transform) {
                    Destroy(thePlayer.transform.gameObject);
                }                
            }

            playerList.Remove(thePlayer);
        }

        public GameNetworkingPlayerInfo GetPlayer(PhotonPlayer networkPlayer) {
            foreach (GameNetworkingPlayerInfo np in playerList) {
                if (np.networkPlayer == networkPlayer) {
                    return np;
                }
            }
            return null;
        }

        public void SetPlayer(PhotonPlayer networkPlayer, GameNetworkingPlayerInfo playerInfo) {
            for (int i = 0; i < playerList.Count; i++) {
                if (playerList[i].networkPlayer == networkPlayer) {
                    playerList[i] = playerInfo;
                }
            }
        }

        // -------------------------------------------------------------------------------------
        // PLAYER ATTRIBUTES

        public void SetPlayerAttributes(PhotonPlayer networkPlayer, GameNetworkingPlayerInfo playerInfo) {
            System.Object syncRoot = new System.Object();
            lock (syncRoot) {
                for (int i = 0; i < playerList.Count; i++) {
                    if (playerList[i].networkPlayer == networkPlayer) {
                        playerList[i].attributes = playerInfo.attributes;
                    }
                }
            }
        }

        public void SetPlayerAttributeValue(PhotonPlayer networkPlayer, string key, string keyValue) {
            UpdatePlayerAttributeValue(networkPlayer, key, keyValue);
        }

        public void SetPlayerAttributeValue(PhotonPlayer networkPlayer, string key, bool keyValue) {
            SetPlayerAttributeValue(networkPlayer, key, System.Convert.ToString(keyValue));
        }

        public void SetPlayerAttributeValue(PhotonPlayer networkPlayer, string key, int keyValue) {
            SetPlayerAttributeValue(networkPlayer, key, System.Convert.ToString(keyValue));
        }

        public void SetPlayerAttributeValue(PhotonPlayer networkPlayer, string key, double keyValue) {
            SetPlayerAttributeValue(networkPlayer, key, System.Convert.ToString(keyValue));
        }

        public int GetPlayerAttributeValueInt(PhotonPlayer networkPlayer, string key) {
            int attValue = 0;

            GameNetworkingPlayerInfo playerInfo = GetPlayer(networkPlayer);

            if (playerInfo.attributes.ContainsKey(key)) {
                string _value = playerInfo.attributes[key];
                if (!string.IsNullOrEmpty(_value)) {
                    int.TryParse(_value, out attValue);
                }
            }
            return attValue;
        }

        public bool GetPlayerAttributeValueBool(PhotonPlayer networkPlayer, string key) {
            bool attValue = false;

            GameNetworkingPlayerInfo playerInfo = GetPlayer(networkPlayer);

            if (playerInfo.attributes.ContainsKey(key)) {
                string _value = playerInfo.attributes[key];
                if (!string.IsNullOrEmpty(_value)) {
                    if (!bool.TryParse(_value, out attValue))
                        attValue = false;
                }
            }
            return attValue;
        }
    }
}

/*

       public enum PhotonNetworkingMessage
{
/// <summary>
/// Called as soon as PhotonNetwork succeeds to connect to the photon server. (This is not called for transitions from the masterserver to game servers, which is hidden for PUN users)
/// Example: void OnConnectedToPhoton(){ ... }
/// </summary>
OnConnectedToPhoton,

/// <summary>
/// Called once the local user left a room.
/// Example: void OnLeftRoom(){ ... }
/// </summary>
OnLeftRoom,

/// <summary>
/// Called -after- switching to a new MasterClient because the previous MC left the room. The last MC will already be removed at this points.
/// Example: void OnMasterClientSwitched(PhotonPlayer newMasterClient){ ... }
/// </summary>
OnMasterClientSwitched,

/// <summary>
/// Called if a CreateRoom() call failed. Most likely because the room name is already in use.
/// Example: void OnPhotonCreateRoomFailed(){ ... }
/// </summary>
OnPhotonCreateRoomFailed,

/// <summary>
/// Called if a JoinRoom() call failed. Most likely because the room does not exist or the room is full.
/// Example: void OnPhotonJoinRoomFailed(){ ... }
/// </summary>
OnPhotonJoinRoomFailed,

/// <summary>
/// Called after a CreateRoom() succeeded creating a room. Note that this implies the local client is the MasterClient. OnJoinedRoom is always called after OnCreatedRoom.
/// Example: void OnCreatedRoom(){ ... }
/// </summary>
OnCreatedRoom,

/// <summary>
/// Called after connecting to the master server. While in the lobby, the roomlist is automatically updated.
/// Example: void OnJoinedLobby(){ ... }
/// </summary>
OnJoinedLobby,

/// <summary>
/// Called after leaving the lobby
/// Example: void OnLeftLobby(){ ... }
/// </summary>
OnLeftLobby,

/// <summary>
/// Called after disconnecting from the Photon server.
/// In some cases, other events are sent before OnDisconnectedFromPhoton is called. Examples: OnConnectionFail and OnFailedToConnectToPhoton.
/// Example: void OnDisconnectedFromPhoton(){ ... }
/// </summary>
OnDisconnectedFromPhoton,

/// <summary>
/// Called when something causes the connection to fail (after it was established), followed by a call to OnDisconnectedFromPhoton.
/// If the server could not be reached in the first place, OnFailedToConnectToPhoton is called instead.
/// The reason for the error is provided as StatusCode.
/// Example: void OnConnectionFail(DisconnectCause cause){ ... }
/// </summary>
OnConnectionFail,

/// <summary>
/// Called if a connect call to the Photon server failed before the connection was established, followed by a call to OnDisconnectedFromPhoton.
/// If the connection was established but then fails, OnConnectionFail is called.
/// Example: void OnFailedToConnectToPhoton(DisconnectCause cause){ ... }
/// </summary>
OnFailedToConnectToPhoton,

/// <summary>
/// Called after receiving the room list for the first time. Only possible in the Lobby state.
/// Example: void OnReceivedRoomList(){ ... }
/// </summary>
OnReceivedRoomList,

/// <summary>
/// Called after receiving a room list update. Only possible in the Lobby state.
/// Example: void OnReceivedRoomListUpdate(){ ... }
/// </summary>
OnReceivedRoomListUpdate,

/// <summary>
/// Called after joining a room. Called on all clients (including the Master Client)
/// Example: void OnJoinedRoom(){ ... }
/// </summary>
OnJoinedRoom,

/// <summary>
/// Called after a remote player connected to the room. This PhotonPlayer is already added to the playerlist at this time.
/// Example: void OnPhotonPlayerConnected(PhotonPlayer newPlayer){ ... }
/// </summary>
OnPhotonPlayerConnected,

/// <summary>
/// Called after a remote player disconnected from the room. This PhotonPlayer is already removed from the playerlist at this time.
/// Example: void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer){ ... }
/// </summary>
OnPhotonPlayerDisconnected,

/// <summary>
/// Called after a JoinRandom() call failed. Most likely all rooms are full or no rooms are available.
/// Example: void OnPhotonRandomJoinFailed(){ ... }
/// </summary>
OnPhotonRandomJoinFailed,

/// <summary>
/// Called after the connection to the master is established and authenticated but only when PhotonNetwork.AutoJoinLobby is false.
/// If AutoJoinLobby is false, the list of available rooms won't become available but you could join (random or by name) and create rooms anyways.
/// Example: void OnConnectedToMaster(){ ... }
/// </summary>
OnConnectedToMaster,

/// <summary>
/// Called every network 'update' if this MonoBehaviour is being observed by a PhotonView.
/// Example: void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info){ ... }
/// </summary>
OnPhotonSerializeView,

/// <summary>
/// Called on all scripts on a GameObject(and it's children) that have been spawned using PhotonNetwork.Instantiate
/// Example: void OnPhotonInstantiate(PhotonMessageInfo info){ ... }
/// </summary>
OnPhotonInstantiate
}

                           *
NoError	 No error occurred.
RSAPublicKeyMismatch	 We presented an RSA public key which does not match what the system we connected to is using.
InvalidPassword	 The server is using a password and has refused our connection because we did not set the correct password.
ConnectionFailed	 Connection attempt failed, possibly because of internal connectivity problems.
TooManyConnectedPlayers	 The server is at full capacity, failed to connect.
ConnectionBanned	 We are banned from the system we attempted to connect to (likely temporarily).
AlreadyConnectedToServer	 We are already connected to this particular server (can happen after fast disconnect/reconnect).
AlreadyConnectedToAnotherServer	 Cannot connect to two servers at once. Close the connection before connecting again.
CreateSocketOrThreadFailure	 Internal error while attempting to initialize network interface. Socket possibly already in use.
IncorrectParameters	 Incorrect parameters given to Connect function.
EmptyConnectTarget	 No host target given in Connect.
InternalDirectConnectFailed	 Client could not connect internally to same network NAT enabled server.
NATTargetNotConnected	 The NAT target we are trying to connect to is not connected to the facilitator server.
NATTargetConnectionLost	 Connection lost while attempting to connect to NAT target.
NATPunchthroughFailed	 NAT punchthrough attempt has failed. The cause could be a too restrictive NAT implementation on either endpoints.

*/