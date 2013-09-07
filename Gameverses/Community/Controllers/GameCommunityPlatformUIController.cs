using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class GameCommunityPlatformUIController : MonoBehaviour {
	
	public static GameCommunityPlatformUIController Instance;	
		    
	public void Awake() {
		
        if (Instance != null && this != Instance) {
            //There is already a copy of this script running
            //Destroy(gameObject);
            return;
        }
		
        Instance = this;	
	}
	
	void OnEnable() {
		
	}
	
	void OnDisable() {
		
	}
	
	void Start() {
		
	}
	
	public static void LoadFacebookProfileImageByUsername(
		string username, UITexture textureSpriteProfilePicture, int width, int height, float delay) {
		if(Instance != null) {
			string url = String.Format("http://graph.facebook.com/{0}/picture", username);		
			Instance.loadUITextureImage(
				textureSpriteProfilePicture, url, width, height, delay);
		}
	}
	
	public static void LoadFacebookProfileImage(
		string userId, UITexture textureSpriteProfilePicture, int width, int height, float delay) {
		if(Instance != null) {
			string url = String.Format("http://graph.facebook.com/{0}/picture", userId);		
			Instance.loadUITextureImage(
				textureSpriteProfilePicture, url, width, height, delay);
		}
	}
	
	public static void LoadUITextureImage(
		UITexture textureSprite, string url, int width, int height, float delay) {
		if(Instance != null) {
			Instance.loadUITextureImage(textureSprite, url, width, height, delay);
		}
	}
	
	public void loadUITextureImage(
		UITexture textureSprite, string url, int width, int height, float delay) {
		StartCoroutine(LoadUITextureImageCo(textureSprite, url, width, height, delay));		
	}
	
	public IEnumerator loadUITextureImageCo(
		UITexture textureSprite, string url, int width, int height, float delay) {
		yield return StartCoroutine(LoadUITextureImageCo(textureSprite, url, width, height, delay));		
	}
	
	public static IEnumerator LoadFacebookProfileImageCo(
		string userId, UITexture textureSpriteProfilePicture, int width, int height, float delay) {
		if(Instance != null) {
			string url = String.Format("http://graph.facebook.com/{0}/picture", userId);		
			yield return Instance.StartCoroutine(LoadUITextureImageCo(
				textureSpriteProfilePicture, url, width, height, delay));
		}
	}
	
	public static List<string> urls404 = new List<string>();
	
	public static IEnumerator LoadUITextureImageCo(UITexture textureSprite, string url, int width, int height, float delay) {
				
		if(!string.IsNullOrEmpty(url) && !urls404.Contains(url)) {
			
			urls404.Add(url);
			
			yield return new WaitForSeconds(delay * .5f);
									
			Texture2D tex = null;
			
			if(textureSprite != null) {							
				
				if(tex == null) {
					tex = new Texture2D(48, 48, TextureFormat.RGB24, true );
					
					WWW www = new WWW(url);
					yield return www;
			
					if(www.error != null){
						//Debug.Log("Error loading image:" + www.error);
					}
					else {						
						www.LoadImageIntoTexture(tex);
						if(tex != null) {
							textureSprite.mainTexture = tex;
						}
								
					}
					www.Dispose();
					www = null;
								
				}
				
				if(textureSprite != null) {
					Vector3 imageScale = textureSprite.transform.localScale.WithX(width).WithY(height);
					textureSprite.transform.localScale = imageScale;
				}
			}
		}
	}
	
}
