using System;
using System.Collections;
using System.Collections.Generic;

namespace Gameverses {

    // config
    /*
    public class GameversesConfig {
        public static string currentContentUrlRoot = "http://v3.gameverses.com/";
        public static string currentContentUrlCDN = "http://s3.amazonaws.com/";

        public static string apiPath = "http://v3.gameverses.com/api/";
        public static string apiGame = "game-demo-1";
        public static string apiGamePath = "game";
        public static string apiGameFullPath = "";
        public static string apiGameId = "11111111-1111-1111-1111-111111111111";
        public static string apiAuthCode = "11111111-1111-1111-1111-111111111111";

        public static int currentContentIncrement = 1;
        public static string currentContentVersion = "1.0";
        public static string currentContentRootAppCode = "gameverses";
        public static string currentContentAppCode = "drawlabs-game-1";
        public static string currentContentPackCode = "drawlabs-game-1-pack-1";
        public static string currentContentPlatformCode = "ios";
    }
    */

    // base
    public class GameversesBaseEntity {
        public string uuid = "";
        public DateTime date_modified = DateTime.Now;
        public DateTime date_created = DateTime.Now;
        public bool active = true;
        public string status = "";

        public GameversesBaseEntity() {
            Reset();
        }

        public virtual void Reset() {
            uuid = UniqueUtil.Instance.CreateUUID4();
            date_modified = DateTime.Now;
            date_created = DateTime.Now;
            active = true;
            status = "";
        }
    }

    public class GameversesBaseMeta : GameversesBaseEntity {
        public string code = "";
        public string name = "";
        public string display_name = "";
        public string description = "";

        public GameversesBaseMeta() {
            Reset();
        }

        public override void Reset() {
            base.Reset();
            code = "";
            name = "";
            display_name = "";
            description = "";
        }
    }

    public class GameversesBaseResponse {
        public int error = 0;
        public string message = "";
        public string action = "";
        public string status = "";
        public Dictionary<string, string> info = new Dictionary<string, string>();

        public GameversesBaseResponse() {
            Reset();
        }

        public virtual void Reset() {
            error = 0;
            message = "";
            action = "";
            status = "";
            info = new Dictionary<string, string>();
        }
    }

    // profile

    public class GameversesProfile : GameversesBaseEntity {
        public string username = "";

        public GameversesProfile() {
            Reset();
        }

        public override void Reset() {
            base.Reset();
            username = "";
        }
    }

    public class GameversesProfileData : GameversesBaseEntity {
        public string game_id = "";
        public string profile_id = "";
        public string data_code = "";
        public string data = "";

        public GameversesProfileData() {
            Reset();
        }

        public override void Reset() {
            base.Reset();
            game_id = "";
            profile_id = "";
            data_code = "";
            data = "";
        }
    }

    // game

    public class GameversesGame : GameversesBaseMeta {
        public string org_id = "";
        public string app_id = "";

        public GameversesGame() {
            Reset();
        }

        public override void Reset() {
            base.Reset();

            org_id = "";
            app_id = "";
        }
    }

    // game session

    public class GameversesGameSessionListResponse : GameversesBaseResponse {
        public List<GameversesGameSession> data = new List<GameversesGameSession>();

        public GameversesGameSessionListResponse() {
            Reset();
        }

        public override void Reset() {
            base.Reset();
            data = new List<GameversesGameSession>();
        }
    }

    public class GameversesGameListResponse : GameversesBaseResponse {
        public List<GameversesGame> data = new List<GameversesGame>();

        public GameversesGameListResponse() {
            Reset();
        }

        public override void Reset() {
            base.Reset();
            data = new List<GameversesGame>();
        }
    }

    public class GameversesGameSession : GameversesBaseMeta {
        public string profile_network = "";
        public string profile_device = "";
        public string profile_id = "";
        public string profile_username = "";
        public string hash = "";
        public string game_id = "";
        public string game_type = "";
        public string game_state = "";
        public string game_code = "";
        public string game_area = "";
        public string game_level = "";
        public int game_players_connected = 1;
        public int game_players_allowed = 4;
        public double game_player_x = 0;
        public double game_player_z = 0;
        public string network_uuid = "";
        public string network_ip = "";
        public int network_port = 50666;
        public bool network_use_nat = false;
        public string network_external_ip = "";
        public int network_external_port = 50666;

        public GameversesGameSession() {
            Reset();
        }

        public override void Reset() {
            base.Reset();

            profile_network = "";
            profile_device = "";
            profile_id = "";
            profile_username = "";
            hash = "";

            game_id = "";
            game_type = "";
            game_state = "";
            game_code = "";
            game_area = "";
            game_level = "";
            game_players_connected = 1;
            game_players_allowed = 4;
            game_player_x = 0;
            game_player_z = 0;

            network_uuid = "";
            network_ip = "";
            network_port = 50666;
            network_use_nat = false;
            network_external_ip = "";
            network_external_port = 50666;
        }
    }

    public class GameversesGameSessionResponse : GameversesBaseResponse {
        public GameversesGameSession data = new GameversesGameSession();

        public GameversesGameSessionResponse() {
            Reset();
        }

        public override void Reset() {
            base.Reset();
            data = new GameversesGameSession();
        }
    }

    public class GameversesGameSessionData : GameversesBaseMeta {
        public string data_layer_enemy = "";
        public string data_layer_actors = "";
        public string data_results = "";
        public string data = "";
        public string data_layer_projectile = "";

        public GameversesGameSessionData() {
            Reset();
        }

        public override void Reset() {
            base.Reset();

            data_layer_enemy = "";
            data_layer_actors = "";
            data_results = "";
            data = "";
            data_layer_projectile = "";
        }
    }

    public class GameversesGameSessionDataResponse : GameversesBaseResponse {
        public GameversesGameSessionData data = new GameversesGameSessionData();

        public GameversesGameSessionDataResponse() {
            Reset();
        }

        public override void Reset() {
            base.Reset();
            data = new GameversesGameSessionData();
        }
    }

    // game content

    public class GameversesGameContent : GameversesBaseMeta {
        public string game_id = "";
        public string data = "";
        public string increment = "";
        public string version = "";
        public string hash = "";
        public string path = "";
        public string filename = "";
        public string extension = "";
        public string platform = "";
        public string source = "";   // unity/ udk etc
        public string content_type = "";   // unity/ udk etc

        public GameversesGameContent() {
            Reset();
        }

        public override void Reset() {
            base.Reset();

            game_id = "";
            data = "";
            increment = "";
            version = "";
            hash = "";
            path = "";
            filename = "";
            extension = "";
            platform = "";
            source = "";
            content_type = "";
        }
    }

    public class GameversesGameContentListResponse : GameversesBaseResponse {
        public List<GameversesGameContent> data = new List<GameversesGameContent>();

        public GameversesGameContentListResponse() {
            Reset();
        }

        public override void Reset() {
            base.Reset();
            data = new List<GameversesGameContent>();
        }
    }

    public class GameversesGameContentResponse : GameversesBaseResponse {
        public GameversesGameContent data = new GameversesGameContent();

        public GameversesGameContentResponse() {
            Reset();
        }

        public override void Reset() {
            base.Reset();
            data = new GameversesGameContent();
        }
    }

    // game profile content

    public class GameversesGameProfileContent : GameversesBaseMeta {
        public string profile_id = "";
        public string game_id = "";
        public string data = "";
        public string increment = "";
        public string version = "";
        public string hash = "";
        public string path = "";
        public string filename = "";
        public string extension = "";
        public string platform = "";
        public string source = "";   // unity/ udk etc

        public GameversesGameProfileContent() {
            Reset();
        }

        public override void Reset() {
            base.Reset();

            profile_id = "";
            game_id = "";
            data = "";
            increment = "";
            version = "";
            hash = "";
            path = "";
            filename = "";
            extension = "";
            platform = "";
            source = "";
        }
    }

    public class GameversesGameProfileContentListResponse : GameversesBaseResponse {
        public List<GameversesGameProfileContent> data = new List<GameversesGameProfileContent>();

        public GameversesGameProfileContentListResponse() {
            Reset();
        }

        public override void Reset() {
            base.Reset();
            data = new List<GameversesGameProfileContent>();
        }
    }

    public class GameversesGameProfileContentResponse : GameversesBaseResponse {
        public GameversesGameProfileContent data = new GameversesGameProfileContent();

        public GameversesGameProfileContentResponse() {
            Reset();
        }

        public override void Reset() {
            base.Reset();
            data = new GameversesGameProfileContent();
        }
    }
}