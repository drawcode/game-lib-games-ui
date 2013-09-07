using System;

public static class GameCommunityConfig {
	
	public static string appUrlShortCode = "vidari-app";
	public static string appUrlScheme = "vidariapp";
	public static string apiUrlWeb = "http://gamecommunity.vidari.com/api/v1/";
	public static string appUrlWeb = "http://gamecommunity.vidari.com/game/vidari-app/";
	public static string appBundleId = "com.leadingfusion.vidariapp";
	public static string appGameDisplayName = "Vidari";
	
	public static string trackingTestFlightTeamToken = "a2fffce2-0b31-4f8e-bda5-9cb64b1d0d14";
	public static bool trackingTestFlightEnable = true;	
	// ## Authentication
	
	// uses 4.3 of google, will need to switch to AnalyticsPlatform when it 
	// is released away from ga.js and image request fallback
	public static string[] trackingGoogleAccountIds = new string[] {
		"UA-39331816-1", 
		"UA-39331816-2"};
	
	public static string[] trackingGoogleAccountDomains = new string[] {
		"gamecommunity.vidari.com",
		"gamecommunity.vidari.com/game/vidari-app/?webapp=1",
		"gamecommunity.vidari.com/game/vidari-app/?app=1"
	};
	
	public static string socialFacebookAppId = "581122718566925";
	public static string socialFacebookSecret = "970f306f98da889e82810bf2f9572ede"; // needed for scores/leaderboards if not using 
											   // the server hosted leaderboards but only facebook
	
	public static string socialFacebookBrandPage = "https://www.facebook.com/leadingfusion/";
	public static string socialFacebookLikeDefaultUrl = "http://apps.facebook.com/vidariapp/";
	public static string[] socialFacebookPermissionsRead = new string[] { 
		SocialNetworksFacebookPermissions.read_user_games_activity , 
		SocialNetworksFacebookPermissions.read_friends_games_activity, 
		SocialNetworksFacebookPermissions.read_user_about_me, 
		SocialNetworksFacebookPermissions.read_user_birthday, 
		SocialNetworksFacebookPermissions.read_user_location
	}; 
	
	public static string[] socialFacebookPermissionsWrite = new string[] { 
		SocialNetworksFacebookPermissions.write_publish_actions//, 
		//SocialNetworksFacebookPermissions.write_publish_stream // part of publish actions now, if friend posting is needed switch to publish stream instead of actions.
	}; 	

	
	/*
	 ## GAME COMMUNITY
	public static string socialFacebookAppId = "135612503258930";
	public static string socialFacebookSecret = "05f74cfecf34f75e4c36b7ff37529ff9"; 
	
	
	 ## GAME COMMUNITY BASIC
	public static string socialFacebookAppId = "231596810306535";
	public static string socialFacebookSecret = "581bcca5ab4746cb8bb37da7be798e12"; 
	 
	 
	*/	
	
	public static string socialTwitterAppId = "lKaUGNygvT3pjkN47eIa7w";
	public static string socialTwitterSecret = "vS44vIUqvnxcdUL1ablS9gB6tolbdxDVESw9knuUUg";
	
	public static string socialGameCommunityAppId = "63a15c20-294f-11e1-9314-0800200c9a66";
	public static string socialGameCommunityAppCode = "vidari-app";
	public static string socialGameCommunityAppAuth = "vS44vIUqvnxcdUL1ablS9gB6tolbdxDVESw9knuUUg";
	
	// ## Network Messages
	
	[NonSerialized]
	public static string stringSavedPhotoTitleDefault = "Vidari Photo";
	
	[NonSerialized]
	public static string stringTwitterPostMessage = "Vidari! vidari.com";
	[NonSerialized]
	public static string stringTwitterDisabledTitle = "Twitter Disabled";
	[NonSerialized]
	public static string stringTwitterDisabledMessage = "Twitter is not configured on this device. Open the Settings App and enter your Twitter username and password to enable this feature.";
	
	[NonSerialized]
	public static string stringFacebookPostMessage = "I viewed some augmented reality in the Vidari App! vidari.com";	
	[NonSerialized]
	public static string stringFacebookUploadErrorTitle = "An error occurred.";
	[NonSerialized]
	public static string stringFacebookUploadErrorMessage = "The following error occcurred:";
	[NonSerialized]
	public static string stringFacebookUploadSuccessTitle = "Success";
	[NonSerialized]
	public static string stringFacebookUploadSuccessMessage = "The photo has been uploaded to your facebook photo feed.";
	
	[NonSerialized]
	public static string stringPendingCreatingScreenshot = "Creating screenshot...";
	[NonSerialized]
	public static string stringPendingUploadingPost = "Uploading...";
	
	[NonSerialized]
	public static string stringLibraryPhotoSavedTitle = "Photo Saved!";
	[NonSerialized]
	public static string stringLibraryPhotoSavedMessage = "Your picture has been saved to your photo album.";
	
	[NonSerialized]
	public static string socialStatisticForFacebook = "points";
	
	// ## Features
	
	// services
	public static bool featureEnableTwitter = true;
	public static bool featureEnableFacebook = true;
	
	// leaderboards/sync thirdparty
	
	public static bool featureEnableCustomLeaderboards = true; // popar/custom
	public static bool featureEnableFacebookLeaderboards = true;
	public static bool featureEnableGameCenterLeaderboards = false;	// ios
	public static bool featureEnableGameCircleLeaderboards = false;   // amazon
	
}
