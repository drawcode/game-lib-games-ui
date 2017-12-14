using System;
using System.Collections;
using System.Collections.Generic;

using Engine.Networking;

namespace Gameverses {
    public class GameversesServiceMessages {
        public static string GameList = "game-get-list";
        public static string GameSessionList = "game-session-get-list";
        public static string GameSessionGet = "game-session-get";
        public static string GameSessionSet = "game-session-set";
        public static string GameSessionState = "game-session-state";
    }

    public class GameversesServiceActions {
        public static string Game = "game";
        public static string Games = "games";
        public static string GameSession = "game-session";
        public static string GameSessions = "game-sessions";
        public static string GameContent = "game-content";
        public static string GameContents = "game-contents";
        public static string GameProfile = "game-profile";
        public static string GameProfileContent = "game-profile-content";
    }

    public class GameversesService {
        private static volatile GameversesService instance;
        private static System.Object syncRoot = new System.Object();

        //private int randomSeed = 1;

        // PROFILE

        public GameversesProfile currentProfile;
        public GameversesProfileData currentProfileData;

        public delegate void HandleGameProfileCallback(GameversesProfile data,WebRequests.ResponseObject responseObject);

        public HandleGameProfileCallback callbackGameProfile;

        // PROFILE CONTENT

        // PLATFORM

        // SESSION

        public List<GameversesGame> currentGamesList;
        public List<GameversesGameSession> currentGameSessions;
        public GameversesGameSession currentGameSession;
        public GameversesGameSessionData currentGameSessionData;

        public delegate void HandleGameListCallback(List<GameversesGame> data,WebRequests.ResponseObject responseObject);

        public HandleGameListCallback callbackGameList;

        public delegate void HandleGameSessionListCallback(List<GameversesGameSession> data,WebRequests.ResponseObject responseObject);

        public HandleGameSessionListCallback callbackGameSessionList;

        public delegate void HandleGameSessionCallback(GameversesGameSession data,WebRequests.ResponseObject responseObject);

        public HandleGameSessionCallback callbackGameSession;

        public delegate void HandleGameSessionDataCallback(GameversesGameSessionData data,WebRequests.ResponseObject responseObject);

        public HandleGameSessionDataCallback callbackGameSessionData;

        public delegate void HandleGameSessionSetCallback(GameversesGameSession data,WebRequests.ResponseObject responseObject);

        public HandleGameSessionSetCallback callbackGameSessionSet;

        public delegate void HandleGameSessionDataSetCallback(GameversesGameSessionData data,WebRequests.ResponseObject responseObject);

        public HandleGameSessionDataSetCallback callbackGameSessionDataSet;

        public delegate void HandleGameSessionStateCallback(GameversesGameSession data,WebRequests.ResponseObject responseObject);

        public HandleGameSessionStateCallback callbackGameSessionState;

        // CONTENT

        public List<GameversesGameContent> currentGameContents;
        public GameversesGameContent currentGameContent;

        public static GameversesService Instance {
            get {
                if (instance == null) {
                    lock (syncRoot) {
                        if (instance == null)
                            instance = new GameversesService();
                    }
                }

                return instance;
            }
            set {
                instance = value;
            }
        }

        public GameversesService() {
            SetApiInfo();
        }

        public void SetApiInfo() {
            GameversesConfig.apiGameFullPath = GameversesConfig.apiPath + GameversesConfig.apiGamePath + "/" + GameversesConfig.apiGame + "/";
        }

        public WebRequests.ResponseObject HandleResponseObject(WebRequests.ResponseObject responseObject) {

            // Manages common response object parsing to get to object

            try {
                if (responseObject != null) {

                    if (responseObject.www != null) {

                        if (string.IsNullOrEmpty(responseObject.www.error)) {

                            if (!string.IsNullOrEmpty(responseObject.www.text)) {
                                
                                Dictionary<string, object> data = 
                                    responseObject.www.text.FromJson<Dictionary<string, object>>();
                                
                                if (data != null && data.Count > 0) {
                                    int error = -1;
                                    try {
                                        error = data.Get<int>("error");
                                    }
                                    catch(Exception e) {
                                        LogUtil.Log("ERROR parsing error key: error:" + e.Message + e.StackTrace);
                                    }

                                    string message = "Problem parsing service container";
                                    try {
                                        message = data.Get<string>("message");
                                    }
                                    catch (Exception e) {
                                        LogUtil.Log("ERROR parsing error key: message:" + e.Message + e.StackTrace);
                                    }

                                    object dataValue = null;
                                    try {
                                        dataValue = data.Get<object>("data");
                                    }
                                    catch (Exception e) {
                                        LogUtil.Log("ERROR parsing error key: data" + e.Message + e.StackTrace);
                                    }

                                    responseObject.error = error;
                                    responseObject.message = message;

                                    LogUtil.Log("STATUS/ERROR:" + error);
                                    LogUtil.Log("STATUS/ERROR MESSAGE:" + message);

                                    if (error == 0) {

                                        //JsonData dataNode = (JsonData)data["data"];
                                        LogUtil.Log("STATUS/DATA NODE:" + dataValue);

                                        if (!string.IsNullOrEmpty(responseObject.www.text))
                                            responseObject.data = responseObject.www.text;////data["data"].ToString(); // contains object
                                        responseObject.dataValue = dataValue; // contains object
                                        responseObject.validResponse = true;
                                    }
                                    else {
                                        LogUtil.LogError("ERROR - Good response but problem with data, see message.");
                                        LogUtil.LogError("RESPONSE:" + responseObject.www.text);
                                        responseObject.validResponse = false;
                                    }
                                }

                                //JsonData data = JsonMapper.ToObject(responseObject.www.text);

                                //if(data.IsObject) {
                                //    int error = -1;
                                //    try {
                                //        if(data["error"] != null) {
                                //            if(data["error"].IsInt) {
                                //                error = (int)data["error"];
                                //            }
                                //        }
                                //    }
                                //    catch(Exception e) {
                                //        LogUtil.Log("ERROR parsing error key: error:" + e.Message + e.StackTrace);
                                //    }

                                //    string message = "Problem parsing service container";
                                //    try {
                                //        if(data["message"] != null) {
                                //            if(data["message"].IsString) {
                                //                message = (string)data["message"];
                                //            }
                                //        }
                                //    }
                                //    catch(Exception e) {
                                //        LogUtil.Log("ERROR parsing error key: message:" + e.Message + e.StackTrace);
                                //    }

                                //    object dataValue = null;
                                //    try {
                                //        if(data["data"] != null) {
                                //            dataValue = data["data"];
                                //        }
                                //    }
                                //    catch(Exception e) {
                                //        LogUtil.Log("ERROR parsing error key: data" + e.Message + e.StackTrace);
                                //    }

                                //    responseObject.error = error;
                                //    responseObject.message = message;

                                //    LogUtil.Log("STATUS/ERROR:" + error);
                                //    LogUtil.Log("STATUS/ERROR MESSAGE:" + message);

                                //    if(error == 0) {

                                //        //JsonData dataNode = (JsonData)data["data"];
                                //        LogUtil.Log("STATUS/DATA NODE:" + dataValue);

                                //        if(!string.IsNullOrEmpty(responseObject.www.text))
                                //            responseObject.data = responseObject.www.text;////data["data"].ToString(); // contains object
                                //        responseObject.dataValue = (JsonData)dataValue; // contains object
                                //        responseObject.validResponse = true;
                                //    }
                                //    else {
                                //        LogUtil.LogError("ERROR - Good response but problem with data, see message.");
                                //        LogUtil.LogError("RESPONSE:" + responseObject.www.text);
                                //        responseObject.validResponse = false;
                                //    }
                                //}
                            }
                            else {
                                LogUtil.LogError("ERROR - NO DATA");
                                responseObject.validResponse = false;
                            }
                        }
                    }
                }
            }
            catch (Exception e) {
                LogUtil.Log(e);
                responseObject.validResponse = false;
            }

            return responseObject;
        }

        // #############################################################################################
        // GAME LIST

        public void GetGameList() {
            RequestGameList(HandleGameList);
        }

        public void HandleGameList(List<GameversesGame> gameList, WebRequests.ResponseObject responseObject) {
            if (responseObject.validResponse) {

                // messenger
                LogUtil.Log("HandleGameList::success");
                currentGamesList = gameList;

                GameMessenger<List<GameversesGame>>.Broadcast(GameversesServiceMessages.GameList, currentGamesList);
            }
            else {
                LogUtil.Log("HandleGameList::error");
            }
        }

        public void RequestGameList(HandleGameListCallback callback) {
            string url = GameversesConfig.apiGameFullPath + GameversesServiceActions.Games + "/";

            // Since this is a real-time update, ensure cache on the url to bypass Unity's WWW caching.
            //url = serviceUtil.EnsureUrlUniqueByTime(url);

            LogUtil.Log("RequestGameList");
            LogUtil.Log("url:" + url);
            LogUtil.Log("callback:" + callback);
            callbackGameList = callback;

            WebRequests.HandleResponseObjectCallback callbackWebRequests = BaseHandleResponseGameList;
            WebRequests.Instance.Request(url, callbackWebRequests);
        }

        public void BaseHandleResponseGameList(WebRequests.ResponseObject responseObject) {
            responseObject = HandleResponseObject(responseObject);

            if (responseObject.validResponse) {
                LogUtil.Log("SUCCESS retrieved valid response");

                if (!string.IsNullOrEmpty(responseObject.www.text))
                    LogUtil.Log("responseObject.www.text" + responseObject.www.text);

                List<GameversesGame> gameList = ParseGameList(responseObject.data);
                callbackGameList(gameList, responseObject);
            }
            else {
                LogUtil.Log("ERROR retrieving data BaseHandleResponseGameList");
            }
        }

        // #############################################################################################
        // GAME SESSION LIST

        public void GetGameSessionList() {
            LogUtil.Log("GetGameSessionList");
            RequestGameSessionList(HandleGameSessionList);
        }

        public void HandleGameSessionList(List<GameversesGameSession> gameSessionList, WebRequests.ResponseObject responseObject) {
            if (responseObject.validResponse) {

                // messenger
                LogUtil.Log("HandleGameSessionList::success");
                currentGameSessions = gameSessionList;
                GameMessenger<List<GameversesGameSession>>.Broadcast(GameversesServiceMessages.GameSessionList, currentGameSessions);
            }
            else {
                LogUtil.Log("HandleGameSessionList::error");
            }
        }

        public void RequestGameSessionList(HandleGameSessionListCallback callback) {
            string url = GameversesConfig.apiGameFullPath
                + GameversesServiceActions.GameSessions
                + "/all";

            // Since this is a real-time update, ensure cache on the url to bypass Unity's WWW caching.
            //url = serviceUtil.EnsureUrlUniqueByTime(url);

            LogUtil.Log("RequestGameSessionList");
            LogUtil.Log("url:" + url);
            LogUtil.Log("callback:" + callback);
            callbackGameSessionList = callback;

            WebRequests.HandleResponseObjectCallback callbackWebRequests = BaseHandleResponseGameSessionList;
            WebRequests.Instance.Request(url, callbackWebRequests);
        }

        public void BaseHandleResponseGameSessionList(WebRequests.ResponseObject responseObject) {
            responseObject = HandleResponseObject(responseObject);

            if (responseObject.validResponse) {
                LogUtil.Log("SUCCESS retrieved valid response");

                if (!string.IsNullOrEmpty(responseObject.www.text))
                    LogUtil.Log("responseObject.www.text" + responseObject.www.text);

                List<GameversesGameSession> gameSessionList = ParseGameSessionList(responseObject.data);
                callbackGameSessionList(gameSessionList, responseObject);
            }
            else {
                LogUtil.Log("ERROR retrieving data: BaseHandleResponseGameSessionList");
            }
        }

        // #############################################################################################
        // GAME SESSION SETS

        public void SetGameSession(GameversesGameSession gameSession) {
            LogUtil.Log("SetGameSession2:" + gameSession.uuid);
            PostGameSession(gameSession, HandleGameSessionSet);
        }

        public void HandleGameSessionSet(GameversesGameSession gameSession, WebRequests.ResponseObject responseObject) {
            if (responseObject.validResponse) {

                // messenger
                LogUtil.Log("HandleGameSessionSet::success");
                currentGameSession = gameSession;
                GameMessenger<GameversesGameSession>.Broadcast(GameversesServiceMessages.GameSessionSet, currentGameSession);
            }
            else {
                LogUtil.Log("HandleGameSessionSet::error: " + responseObject.message);
            }
        }

        public void PostGameSession(GameversesGameSession gameSession, HandleGameSessionSetCallback callback) {
            string url = GameversesConfig.apiGameFullPath
                + GameversesServiceActions.GameSession
                + "/" + gameSession.uuid;
            LogUtil.Log("url:" + url);

            // Since this is a real-time update, ensure cache on the url to bypass Unity's WWW caching.
            //url = serviceUtil.EnsureUrlUniqueByTime(url);

            string dataValue = gameSession.ToJson();

            LogUtil.Log("PostGameSession::dataValue:" + dataValue);

            Dictionary<string, object> paramHash = new Dictionary<string, object>();

            //paramHash.Add("data", System.Uri.EscapeDataString(dataValue));
            paramHash.Add("data", dataValue);

            LogUtil.Log("PostGameSession");
            LogUtil.Log("url:" + url);
            LogUtil.Log("callback:" + callback);
            callbackGameSessionSet = callback;

            WebRequests.HandleResponseObjectCallback callbackWebRequests = BaseHandleResponseGameSessionSet;
            WebRequests.Instance.Request(WebRequests.RequestType.HTTP_POST, url, paramHash, callbackWebRequests);
        }

        public void BaseHandleResponseGameSessionSet(WebRequests.ResponseObject responseObject) {
            responseObject = HandleResponseObject(responseObject);

            if (responseObject.validResponse) {
                LogUtil.Log("SUCCESS retrieved valid response");

                if (!string.IsNullOrEmpty(responseObject.www.text))
                    LogUtil.Log("responseObject.www.text" + responseObject.www.text);

                GameversesGameSession gameSession = ParseGameSession(responseObject.data);
                callbackGameSessionSet(gameSession, responseObject);
            }
            else {
                LogUtil.Log("ERROR retrieving data BaseHandleResponseGameSessionSet");
            }
        }

        // #############################################################################################
        // GAME SESSION SET STATE

        public void SetGameSessionState(GameversesGameSession gameSession) {
            LogUtil.Log("SetGameSessionState:" + gameSession.game_state);
            PostGameSessionState(gameSession, HandleGameSessionState);
        }

        public void HandleGameSessionState(GameversesGameSession gameSession, WebRequests.ResponseObject responseObject) {
            if (responseObject.validResponse) {
                LogUtil.Log("HandleGameSessionState::success");
                currentGameSession = gameSession;
                GameMessenger<GameversesGameSession>.Broadcast(GameversesServiceMessages.GameSessionState, currentGameSession);
            }
            else {
                LogUtil.Log("HandleGameSessionState::error: " + responseObject.message);
            }
        }

        public void PostGameSessionState(GameversesGameSession gameSession, HandleGameSessionStateCallback callback) {
            string url = GameversesConfig.apiGameFullPath
                + GameversesServiceActions.GameSession
                + "/"
                + gameSession.uuid
                + "/"
                + gameSession.game_state;
            LogUtil.Log("url:" + url);

            // Since this is a real-time update, ensure cache on the url to bypass Unity's WWW caching.
            //url = serviceUtil.EnsureUrlUniqueByTime(url);

            string dataValue = gameSession.ToJson();

            LogUtil.Log("PostGameSession::dataValue:" + dataValue);

            Dictionary<string, object> paramHash = new Dictionary<string, object>();

            //paramHash.Add("data", System.Uri.EscapeDataString(dataValue));
            paramHash.Add("data", dataValue);

            LogUtil.Log("PostGameSession");
            LogUtil.Log("url:" + url);
            LogUtil.Log("callback:" + callback);
            callbackGameSessionState = callback;

            WebRequests.HandleResponseObjectCallback callbackWebRequests = BaseHandleResponseGameSessionState;
            WebRequests.Instance.Request(WebRequests.RequestType.HTTP_POST, url, paramHash, callbackWebRequests);
        }

        public void BaseHandleResponseGameSessionState(WebRequests.ResponseObject responseObject) {
            responseObject = HandleResponseObject(responseObject);

            if (responseObject.validResponse) {
                LogUtil.Log("SUCCESS retrieved valid response");

                if (!string.IsNullOrEmpty(responseObject.www.text))
                    LogUtil.Log("responseObject.www.text" + responseObject.www.text);

                GameversesGameSession gameSession = ParseGameSession(responseObject.data);
                callbackGameSessionState(gameSession, responseObject);
            }
            else {
                LogUtil.Log("ERROR retrieving data BaseHandleResponseGameSessionState");
            }
        }

        // #############################################################################################
        // Parsing

        public GameversesProfile ParseProfile(string data) {
            GameversesProfile profile = new GameversesProfile();
            if (!string.IsNullOrEmpty(data)) {
                try {
                    profile = data.FromJson<GameversesProfile>();
                }
                catch (Exception e) {
                    LogUtil.Log("ERROR:" + e);
                }
            }
            return profile;
        }

        public List<GameversesGame> ParseGameList(string data) {
            GameversesGameListResponse response = new GameversesGameListResponse();
            if (!string.IsNullOrEmpty(data)) {
                try {
                    response = data.FromJson<GameversesGameListResponse>();
                }
                catch (Exception e) {
                    LogUtil.Log("ERROR:" + e);
                }
            }
            return response.data;
        }

        public List<GameversesGameSession> ParseGameSessionList(string data) {
            GameversesGameSessionListResponse response = new GameversesGameSessionListResponse();
            if (!string.IsNullOrEmpty(data)) {
                try {
                    response = data.FromJson<GameversesGameSessionListResponse>();
                }
                catch (Exception e) {
                    LogUtil.Log("ERROR:" + e);
                }
            }
            return response.data;
        }

        public GameversesGameSession ParseGameSession(string data) {
            GameversesGameSessionResponse response = new GameversesGameSessionResponse();
            if (!string.IsNullOrEmpty(data)) {
                try {
                    response = data.FromJson<GameversesGameSessionResponse>();
                }
                catch (Exception e) {
                    LogUtil.Log("ERROR:" + e);
                }
            }

            //gameSession = response.data;
            return response.data;
        }

        public GameversesGameSessionData ParseGameSessionData(string data) {
            GameversesGameSessionData gameSessionData = new GameversesGameSessionData();
            if (!string.IsNullOrEmpty(data)) {
                try {
                    gameSessionData = data.FromJson<GameversesGameSessionData>();
                }
                catch (Exception e) {
                    LogUtil.Log("ERROR:" + e);
                }
            }
            return gameSessionData;
        }
    }
}