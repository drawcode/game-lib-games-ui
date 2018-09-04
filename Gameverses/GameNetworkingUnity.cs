#define NETWORK_PHOTON_OFF
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if NETWORK_PHOTON

using ExitGames;
using ExitGames.Client;
using ExitGames.Client.Photon;
using ExitGames.Client.Photon.Lite;
using Photon.SocketServer;
using Photon.SocketServer.Security;

#elif NETWORK_USE_UNITY
#endif
#if NETWORK_USE_UNITY && !UNITY_FLASH
namespace Gameverses {
    public class GameNetworkingUnity : GameObjectBehavior {
        public static GameNetworkingUnity Instance;

        public int maxConnections = 4;

        public bool autoStartServing = true;
        public bool hasTestedNAT = false;
        public bool testedUseNat = false;
        public bool lookForGameSession = false;

        public NetworkPlayer currentPlayer;
        public NetworkPeerType peerType;
        public HostData currentHostData;
        public HostData currentGameSessionHostData;
        public int connectedPlayerCount = 0;

        public HostData[] hostDataMasterServer;
        public HostData hostDataGameSession;

        float currentTimeBlock = 0.0f;
        float currentTimeBlockDelayed = 0.0f;
        float actionInterval = 1.0f;

        public string localDeviceId = "";
        
        public bool useCloudMasterServer = GameversesConfig.useCloudMasterServer;
        public string masterserverGameName = GameversesConfig.masterserverGameName;
        public int defaultServerPort = GameversesConfig.defaultServerPort;
        public int connectTimeoutValue = GameversesConfig.connectTimeoutValue;
        public int connectTestTimeValue = GameversesConfig.connectTestTimeValue;
        public int masterServerPort = GameversesConfig.masterServerPort;
        public int connectionTesterPort = GameversesConfig.connectionTesterPort;
        public int natFacilitatorPort = GameversesConfig.masterserveriPAddressOrDns;
        public string masterserveriPAddressOrDns = GameversesConfig.masterserveriPAddressOrDns;
        public bool useMasterserverRouting = GameversesConfig.useMasterserverRouting;

        string networkObjectsContainerName = "GameversesNetworkContainers";
        GameObject networkObjectsContainer;

        private Transform localTransform;
        private List<GameNetworkingPlayerInfo> playerList;

        public string currentPlayerName = "default";

        public bool pseudoMultiplayer = false; // multiplayer faked with AI
        public bool useExistingTransforms = true;
        public bool connectingAsClient = false;

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

            useExistingTransforms = true;

            //localDeviceId = Puid.New();

            if(Application.platform == RuntimePlatform.Android
               || Application.platform == RuntimePlatform.IPhonePlayer) {
                localDeviceId = SystemInfo.deviceUniqueIdentifier;
            }

            playerList = new List<GameNetworkingPlayerInfo>();

            StartCoroutine(TestConnection());
        }

        void Start() {
            Init();
        }

        void Init() {

            // TODO
            // check for invite
            // check for friends
            // else start server

            MasterServer.port = masterServerPort;
            Network.connectionTesterPort = connectionTesterPort;
            Network.natFacilitatorPort = natFacilitatorPort;
            Network.natFacilitatorIP = Network.connectionTesterIP = MasterServer.ipAddress = masterserveriPAddressOrDns;

            //ServerStart();
        }

        // ##################################################################################################
        // PROPERTIES

        public bool isNetworkRunning {
            get {
                if(isMessageQueueRunning)
                    return true;
                return false;
            }
        }

        public bool isServer {
            get {
                if(Network.isServer){
                    return true;
                }
                return false;
            }
        }

        public bool isClient {
            get {
                if(Network.isClient){
                    return true;
                }
                return false;
            }
        }

        public bool isConnected {
            get {
                if(Network.peerType == NetworkPeerType.Client
                    || Network.peerType == NetworkPeerType.Server){
                    return true;
                }
                return false;
            }
        }

        public bool isConnecting {
            get {
                if(Network.peerType == NetworkPeerType.Connecting){
                    return true;
                }
                return false;
            }
        }

        public bool isDisconnected {
            get {
                if(Network.peerType == NetworkPeerType.Disconnected){
                    return true;
                }
                return false;
            }
        }

        public bool isMessageQueueRunning {
            get {
                return Network.isMessageQueueRunning;
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
            if(isDisconnected && autoStartServing) {
                connectingAsClient = false;
                bool useNat = !Network.HavePublicAddress();
                LogUtil.Log("useNat::" + useNat);
                Network.InitializeSecurity();
                Network.InitializeServer(32, defaultServerPort, useNat);
                LogUtil.Log("useNat::" + useNat);
            }
        }

        // ##################################################################################################
        // CLIENT

        // ##################################################################################################
        // INTERNAL NETWORK EVENTS

        void OnServerInitialized() {
            LogUtil.Log("Server initialized and ready");

            playerList.Clear();

            currentPlayer = Network.player;
            currentHostData = new HostData();

            currentHostData.useNat = !Network.HavePublicAddress();
            LogUtil.Log("currentHostData.useNat: " + currentHostData.useNat);

            currentHostData.guid = currentPlayer.guid;
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
            LogUtil.Log("currentPlayer.ipAddress: " + currentPlayer.ipAddress);

            currentHostData.passwordProtected = false;
            LogUtil.Log("currentHostData.passwordProtected: " + currentHostData.passwordProtected);

            currentHostData.playerLimit = 4;
            LogUtil.Log("currentHostData.playerLimit: " + currentHostData.playerLimit);

            currentHostData.port = 50666;
            LogUtil.Log("currentHostData.port: " + currentHostData.port);

            LogUtil.Log("currentPlayer.externalIP: " + currentPlayer.externalIP);

            LogUtil.Log("currentPlayer.externalPort: " + currentPlayer.externalPort);

            string sessionId = UniqueUtil.CreateUUID4();

            if(useMasterserverRouting) {
                MasterServer.RegisterHost(sessionId, sessionId, sessionId);
            }

            // post this shit up

            GameversesGameAPI.Instance.SetupNetworkGameSession(sessionId, currentPlayer, currentHostData);

            GameMessenger<NetworkPlayer>.Broadcast(GameNetworkingMessages.ServerInitialized, Network.player);

            //SetNetworkPlayer(Network.player, currentPlayerName, GameNetworkingMessages.PlayerTypeHero);

            LogUtil.Log("MatchupGame::ServerStartedExisting:  isServer:" + Network.isServer);

            networkView.RPC("SetNetworkPlayer", RPCMode.AllBuffered, Network.player, currentPlayerName, GameNetworkingPlayerTypeMessages.PlayerTypeHero);
            NetworkViewID id1 = Network.AllocateViewID();

            networkView.RPC("SpawnOnNetwork",
                    RPCMode.AllBuffered,
                    Vector3.zero,
                    Quaternion.identity,
                    id1,
                    currentPlayerName,
                    GameNetworkingPlayerTypeMessages.PlayerTypeHero,
                    UniqueUtil.Instance.currentUniqueId,
                    false,
                    Network.player);

            //SetPlayer(Network.player, );
            //networkView.RPC("AddPlayer", RPCMode.AllBuffered, Network.player, currentPlayerName);
        }

        IEnumerator OnConnectedToServer() {
            LogUtil.Log("Connected to server");

            playerList = new List<GameNetworkingPlayerInfo>();

            networkView.RPC("SetNetworkPlayer", RPCMode.AllBuffered, Network.player, currentPlayerName, GameNetworkingPlayerTypeMessages.PlayerTypeHero);

            NetworkViewID id1 = Network.AllocateViewID();
            networkView.RPC("SpawnOnNetwork",
                RPCMode.AllBuffered,
                Vector3.zero,
                Quaternion.identity,
                id1,
                currentPlayerName,
                GameNetworkingPlayerTypeMessages.PlayerTypeHero,
                UniqueUtil.Instance.currentUniqueId,
                false,
                Network.player);
                yield return 0;

                //SetPlayerTransform(Network.player, localTransform);
                //SetNetworkViewIDs(localTransform.gameObject, id1);

            connectingAsClient = false;

            // Broadcast
            GameMessenger<NetworkPlayer>.Broadcast(GameNetworkingMessages.ConnectedToServer, Network.player);
        }

        IEnumerator OnDisconnectedFromServer(NetworkDisconnection info) {
            if (Network.isServer) {
                LogUtil.Log("Local server connection disconnected");

                SetGameSessionComplete();
            }
            else {
                if (info == NetworkDisconnection.LostConnection) {
                    LogUtil.Log("Lost connection to the server");
                }
                else {
                    LogUtil.Log("Successfully disconnected from the server");
                }
            }

            //
            // Remove all players except yourself

            yield return new WaitForSeconds(1);

            List<GameNetworkingPlayerInfo> listCopyClone = GetPlayerListCopy();

            foreach (GameNetworkingPlayerInfo np in listCopyClone) {
                if (np.networkPlayer != Network.player)
                {
                    RemoveNetworkPlayer(np.networkPlayer);
                    if(!Network.isServer) {
                        if(Network.connections.Length > 0) {
                            Network.CloseConnection(np.networkPlayer, false);
                        }
                    }
                }
            }

            // Broadcast
            GameMessenger<NetworkDisconnection>.Broadcast(GameNetworkingMessages.DisconnectedFromServer, info);

            if(!connectingAsClient) {
                ServerStart();
            }
        }

        void SetGameSessionComplete() {
            GameversesGameAPI.Instance.SetGameSessionState("complete");
            if(useMasterserverRouting) {
                MasterServer.UnregisterHost();
            }
        }

        /*
        NoError  No error occurred.
        RSAPublicKeyMismatch     We presented an RSA public key which does not match what the system we connected to is using.
        InvalidPassword  The server is using a password and has refused our connection because we did not set the correct password.
        ConnectionFailed     Connection attempt failed, possibly because of internal connectivity problems.
        TooManyConnectedPlayers  The server is at full capacity, failed to connect.
        ConnectionBanned     We are banned from the system we attempted to connect to (likely temporarily).
        AlreadyConnectedToServer     We are already connected to this particular server (can happen after fast disconnect/reconnect).
        AlreadyConnectedToAnotherServer  Cannot connect to two servers at once. Close the connection before connecting again.
        CreateSocketOrThreadFailure  Internal error while attempting to initialize network interface. Socket possibly already in use.
        IncorrectParameters  Incorrect parameters given to Connect function.
        EmptyConnectTarget   No host target given in Connect.
        InternalDirectConnectFailed  Client could not connect internally to same network NAT enabled server.
        NATTargetNotConnected    The NAT target we are trying to connect to is not connected to the facilitator server.
        NATTargetConnectionLost  Connection lost while attempting to connect to NAT target.
        NATPunchthroughFailed    NAT punchthrough attempt has failed. The cause could be a too restrictive NAT implementation on either endpoints.

        */

         void OnFailedToConnect(NetworkConnectionError error) {
            LogUtil.Log("Could not connect to server: " + error);

            ServerStart();

            // Broadcast
            GameMessenger<NetworkConnectionError>.Broadcast(GameNetworkingMessages.FailedToConnect, error);
        }

        void OnNetworkInstantiate(NetworkMessageInfo info) {
            LogUtil.Log("New object instantiated by " + info.sender);

            // Broadcast
            GameMessenger<NetworkMessageInfo>.Broadcast(GameNetworkingMessages.NetworkInstantiate, info);
        }

        void OnPlayerConnected(NetworkPlayer player) {
            LogUtil.Log("Player " + connectedPlayerCount++ + " connected from " + player.ipAddress + ":" + player.port);

            // Broadcast
            GameMessenger<NetworkPlayer>.Broadcast(GameNetworkingMessages.PlayerConnected, player);

            GameversesGameAPI.Instance.currentSession.game_players_connected = connectedPlayerCount;
            GameversesGameAPI.Instance.SyncGameSession();
        }

        void OnPlayerDisconnected(NetworkPlayer player) {
            LogUtil.Log("Clean up after player " + player);

            networkView.RPC("RemoveNetworkPlayer", RPCMode.All, player);

            Network.RemoveRPCs(player);
            Network.DestroyPlayerObjects(player);

            connectedPlayerCount--;

            // Broadcast
            GameMessenger<NetworkPlayer>.Broadcast(GameNetworkingMessages.PlayerDisconnected, player);

            GameversesGameAPI.Instance.currentSession.game_players_connected = connectedPlayerCount;
            GameversesGameAPI.Instance.SyncGameSession();
        }

        void OnFailedToConnectToMasterServer(NetworkConnectionError info) {
            LogUtil.Log("Could not connect to master server: " + info);
            lookForGameSession = false;
        }

        void OnMasterServerEvent(MasterServerEvent msEvent) {
            if (msEvent == MasterServerEvent.RegistrationSucceeded) {
                LogUtil.Log("Server registered");
            }
        }

        public void ConnectGameSession(HostData hostData) {
            DisconnectNetwork();
            connectingAsClient = true;
            StartCoroutine(ConnectGameSessionDelayed(hostData));//ServerStart()
        }

        IEnumerator ConnectGameSessionDelayed(HostData hostData) {
            yield return new WaitForSeconds(6f);
            if(useMasterserverRouting) {
                ConnectMasterServerGameSession(hostData);
            }
            else {
                ConnectGameversesMultiuserGameSession(hostData);
            }
        }

        public void ConnectNetwork(HostData hostData) {
            Network.Connect(hostData);
        }

        public void DisconnectNetwork() {

            //if(Network.isServer)
            //  return;

            Network.Disconnect(1);
        }

        public void ConnectGameversesMultiuserGameSession(HostData hostData) {
            hostDataGameSession = hostData;
            ConnectNetwork(hostData);
        }

        public void ConnectMasterServerGameSession(HostData hostData) {
            MasterServer.ClearHostList();
            hostDataMasterServer = null;
            hostDataGameSession = null;
            lookForGameSession = true;
            MasterServer.RequestHostList(hostData.gameName);
        }

        void PollMasterServerGameSessionUpdate() {
            if((hostDataMasterServer == null
                || hostDataGameSession == null)
                && lookForGameSession) {
                if (MasterServer.PollHostList().Length != 0) {
                    hostDataMasterServer = MasterServer.PollHostList();
                    int i = 0;
                    while (i < hostDataMasterServer.Length) {
                        LogUtil.Log("Game name: " + hostDataMasterServer[i].gameName);
                        hostDataGameSession = hostDataMasterServer[i];
                        currentHostData = hostDataGameSession;
                        lookForGameSession = false;

                        ConnectGameSession(hostDataGameSession);
                        i++;
                        break;
                    }
                    MasterServer.ClearHostList();
                }
            }
        }

        IEnumerator TestConnection() {
            if (hasTestedNAT)
                yield break;

            testedUseNat = !Network.HavePublicAddress();

            ConnectionTesterStatus connectionTestResult = ConnectionTesterStatus.Undetermined;
            float timeoutAt = Time.realtimeSinceStartup + connectTestTimeValue;
            float timer = 0;
            bool probingPublicIP = false;
            string testMessage = "";

            while (!hasTestedNAT) {
                yield return 0;
                if (Time.realtimeSinceStartup >= timeoutAt) {
                    LogUtil.Log("TestConnect NAT test aborted; timeout");
                    break;
                }
                connectionTestResult = Network.TestConnection();
                switch (connectionTestResult) {
                    case ConnectionTesterStatus.Error:
                        testMessage = "Problem determining NAT capabilities";
                        hasTestedNAT = false;
                        break;

                    case ConnectionTesterStatus.Undetermined:
                        testMessage = "Undetermined NAT capabilities";
                        hasTestedNAT = false;
                        break;

                    case ConnectionTesterStatus.PublicIPIsConnectable:
                        testMessage = "Directly connectable public IP address.";
                        testedUseNat = false;
                        hasTestedNAT = true;
                        break;

                    // This case is a bit special as we now need to check if we can
                    // circumvent the blocking by using NAT punchthrough
                    case ConnectionTesterStatus.PublicIPPortBlocked:
                        testMessage = "Non-connectble public IP address (port " + defaultServerPort + " blocked), running a server is impossible.";
                        hasTestedNAT = false;

                        // If no NAT punchthrough test has been performed on this public IP, force a test
                        if (!probingPublicIP) {
                            LogUtil.Log("Testing if firewall can be circumvented");
                            connectionTestResult = Network.TestConnectionNAT();
                            probingPublicIP = true;
                            timer = Time.time + 10;
                        }

                        // NAT punchthrough test was performed but we still get blocked
                        else if (Time.time > timer) {
                            probingPublicIP = false;        // reset
                            testedUseNat = true;
                            hasTestedNAT = true;
                        }
                        break;

                    case ConnectionTesterStatus.PublicIPNoServerStarted:
                        testMessage = "Public IP address but server not initialized, it must be started to check server accessibility. Restart connection test when ready.";
                        break;

                    case ConnectionTesterStatus.LimitedNATPunchthroughPortRestricted:
                        testMessage = "Limited NAT punchthrough capabilities. Cannot connect to all types of NAT servers. Running a server is ill adviced as not everyone can connect.";
                        testedUseNat = true;
                        hasTestedNAT = true;
                        break;

                    case ConnectionTesterStatus.LimitedNATPunchthroughSymmetric:
                        testMessage = "Limited NAT punchthrough capabilities. Cannot connect to all types of NAT servers. Running a server is ill adviced as not everyone can connect.";
                        testedUseNat = true;
                        hasTestedNAT = true;
                        break;

                    case ConnectionTesterStatus.NATpunchthroughAddressRestrictedCone:
                    case ConnectionTesterStatus.NATpunchthroughFullCone:
                        testMessage = "NAT punchthrough capable. Can connect to all servers and receive connections from all clients. Enabling NAT punchthrough functionality.";
                        testedUseNat = true;
                        hasTestedNAT = true;
                        break;

                    default:
                        testMessage = "Error in test routine, got " + connectionTestResult;
                        break;
                }
            }
            hasTestedNAT = true;

            //Messenger<bool, UnityEngine.ConnectionTesterStatus, bool, bool, string>.Broadcast(NetworkMessages.NetworkNatTestComplete, testedUseNat, connectionTestResult, probingPublicIP, hasTestedNAT, testMessage);
            LogUtil.Log("TestConnection result: testedUseNat=" + testedUseNat + " connectionTestResult=" + connectionTestResult + " probingPublicIP=" + probingPublicIP + " hasTestedNAT=" + hasTestedNAT + " testMessage=" + testMessage);

            GameMessenger.Broadcast(GameNetworkingMessages.ConnectionTested);

            ServerStart();
        }

        void Update() {
            currentTimeBlock += Time.deltaTime;
            currentTimeBlockDelayed += Time.deltaTime;

            if(currentTimeBlock > actionInterval) {
                currentTimeBlock = 0.0f;
                if(useMasterserverRouting) {
                    PollMasterServerGameSessionUpdate();
                }
                peerType = Network.peerType;
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

            /*
            GameNetworkPlayerContainer playerContainer = (Instantiate(Resources.Load("Prefabs/GameNetworkPlayerObject"), Vector3.zero, Quaternion.identity) as GameObject).GetComponent<GameNetworkPlayerContainer>();
            playerContainer.gameObject.transform.parent = networkObjectsContainer.transform;
            playerContainer.uuid = uuid;

            SetPlayerTransform(np, playerContainer.gameObject.transform);
            playerContainer.AddNetworkView(id1, uuid);

            //SetNetworkViewIDs(playerContainer.gameObject, id1);

            if (amOwner) {
                localTransform = playerContainer.gameObject.transform;
            }
            */
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

            /*
            GameNetworkPlayerContainer playerContainer = (Instantiate(Resources.Load("Prefabs/GameNetworkPlayerObject"), Vector3.zero, Quaternion.identity) as GameObject).GetComponent<GameNetworkPlayerContainer>();
            playerContainer.gameObject.transform.parent = networkObjectsContainer.transform;
            playerContainer.uuid = uuid;

            SetPlayerTransform(np, playerContainer.gameObject.transform);
            playerContainer.AddNetworkView(id1, uuid);

            //SetNetworkViewIDs(playerContainer.gameObject, id1);

            if (amOwner) {
                localTransform = playerContainer.gameObject.transform;
            }
            */
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

        public void UpdatePlayerAttributeValue(NetworkPlayer networkPlayer, string key, string keyValue) {
            UpdatePlayerAttributeSync(networkPlayer, key, keyValue);
            networkView.RPC("UpdatePlayerAttributeSync", RPCMode.Others, networkPlayer, key, keyValue);
        }

        [RPC]
        void UpdatePlayerAttributeSync(NetworkPlayer networkPlayer, string key, string keyValue) {
            GameNetworkingPlayerInfo currentPlayer = GetPlayer(networkPlayer);
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

                SetPlayerAttributes(networkPlayer, currentPlayer);
            }
            else {
                Gameverses.LogUtil.Log("GameversesGameAPI: UpdatePlayerAttribute: Player doesn't exist! Adding...");
                GameNetworkingPlayerInfo np = new GameNetworkingPlayerInfo();
                np.networkPlayer = networkPlayer;

                if(np.attributes.ContainsKey(key)) {
                    np.attributes[key] = keyValue;
                }
                else {
                    np.attributes.Add(key, keyValue);
                }

                if (Network.player == networkPlayer || Network.player + "" == "-1") {
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

        public void SetPlayerAttributes(NetworkPlayer networkPlayer, GameNetworkingPlayerInfo playerInfo) {
            System.Object syncRoot = new System.Object();
            lock (syncRoot) {
                for (int i = 0; i < playerList.Count; i++) {
                    if (playerList[i].networkPlayer == networkPlayer) {
                        playerList[i].attributes = playerInfo.attributes;
                    }
                }
            }
        }

        public void SetPlayerAttributeValue(NetworkPlayer networkPlayer, string key, string keyValue) {
            UpdatePlayerAttributeValue(networkPlayer, key, keyValue);
        }

        public void SetPlayerAttributeValue(NetworkPlayer networkPlayer, string key, bool keyValue) {
            SetPlayerAttributeValue(networkPlayer, key, System.Convert.ToString(keyValue));
        }

        public void SetPlayerAttributeValue(NetworkPlayer networkPlayer, string key, int keyValue) {
            SetPlayerAttributeValue(networkPlayer, key, System.Convert.ToString(keyValue));
        }

        public void SetPlayerAttributeValue(NetworkPlayer networkPlayer, string key, double keyValue) {
            SetPlayerAttributeValue(networkPlayer, key, System.Convert.ToString(keyValue));
        }

        public int GetPlayerAttributeValueInt(NetworkPlayer networkPlayer, string key) {
            int attValue = 0;

            GameNetworkingPlayerInfo playerInfo = GetPlayer(networkPlayer);

            if(playerInfo.attributes.ContainsKey(key)) {
                string _value = playerInfo.attributes[key];
                if(!string.IsNullOrEmpty(_value)) {
                    int.TryParse(_value, out attValue);
                }
            }
            return attValue;
        }

        public bool GetPlayerAttributeValueBool(NetworkPlayer networkPlayer, string key) {
            bool attValue = false;

            GameNetworkingPlayerInfo playerInfo = GetPlayer(networkPlayer);

            if(playerInfo.attributes.ContainsKey(key)) {
                string _value = playerInfo.attributes[key];
                if(!string.IsNullOrEmpty(_value)) {
                    if(!bool.TryParse(_value, out attValue))
                        attValue = false;
                }
            }
            return attValue;
        }
    }
}
#endif