using System;

using UnityEngine;

// Piece-by-piece rollout of the community layer (owner, action-bots 2026-10-04: "we can ship it
// but will sprinkle it in"). Additive: `managed` defaults to false, which leaves every game on the
// old rule (the toolkit view follows the legacy CommunityCamera). A game that opts in sets
// managed = true and switches pieces on, ideally from data (Load), and the view then draws
// whenever any piece is on while the legacy camera stays off.
public static class CommunityFeatures {

    public static bool managed = false;

    // The cards UIPanelCommunityShare slides in.
    public static bool appRateBadge = false;    // ContainerActionAppRate, main menu
    public static bool shareCard = false;       // ShareCenter (and any other share item), Results
    public static bool actionBar = false;       // ContainerActions

    // Buttons inside the action bar.
    public static bool actionBarPhoto = false;      // ButtonGameCommunityCameraTakePhoto
    public static bool actionBarBroadcast = false;  // ButtonGameCommunityBroadcastOpen

    public const string cardAppRate = "ContainerActionAppRate";
    public const string cardActionBar = "ContainerActions";
    public const string buttonPhoto = "ButtonGameCommunityCameraTakePhoto";
    public const string buttonBroadcast = "ButtonGameCommunityBroadcastOpen";

    public static bool anyOn {
        get {
            return appRateBadge || shareCard || actionBar;
        }
    }

    // Unmanaged: everything allowed (old behaviour). Managed: the piece's own flag; any card that
    // is not the badge or the action bar is a share item.
    public static bool AllowsCard(string path) {

        if(!managed) {
            return true;
        }

        if(path == cardAppRate) {
            return appRateBadge;
        }

        if(path == cardActionBar) {
            return actionBar;
        }

        return shareCard;
    }

    public static bool AllowsButton(string name) {

        if(!managed) {
            return true;
        }

        if(name == buttonPhoto) {
            return actionBarPhoto;
        }

        if(name == buttonBroadcast) {
            return actionBarBroadcast;
        }

        return true;
    }

    [Serializable]
    public class Data {
        public bool appRateBadge;
        public bool shareCard;
        public bool actionBar;
        public bool actionBarPhoto;
        public bool actionBarBroadcast;
    }

    // Manage the rollout from a JSON TextAsset under Resources ({"appRateBadge":true,...}).
    // A missing or unreadable file leaves every piece OFF but still managed: nothing appears
    // that the data did not ask for.
    public static void Load(string resourcePath) {

        managed = true;

        TextAsset asset = Resources.Load<TextAsset>(resourcePath);

        if(asset == null) {
            Debug.LogWarning("CommunityFeatures: no data at Resources/" + resourcePath + " - all pieces off");
            return;
        }

        Data data = null;

        try {
            data = JsonUtility.FromJson<Data>(asset.text);
        }
        catch(Exception e) {
            Debug.LogError("CommunityFeatures: unreadable " + resourcePath + ": " + e.Message);
        }

        if(data == null) {
            return;
        }

        appRateBadge = data.appRateBadge;
        shareCard = data.shareCard;
        actionBar = data.actionBar;
        actionBarPhoto = data.actionBarPhoto;
        actionBarBroadcast = data.actionBarBroadcast;
    }
}
