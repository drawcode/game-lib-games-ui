using UnityEngine;
using System;
using System.Collections.Generic;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace Gameverses {

	public class UnityUtil {

		public static string GetPlatformAppDataFolder() {
			return Application.dataPath;
		}

		public static string GetPlatformAppPersistenceFolder() {
			if(Application.platform == RuntimePlatform.IPhonePlayer) {
				return GetDeviceDocumentsFolder();
			}
			else {
				return Application.persistentDataPath;
			}
		}

		public static string GetPlatformAppStorageLocalFolder() {
			if(Application.platform == RuntimePlatform.IPhonePlayer) {
				return GetPlatformAppFolder();
			}
			else {
				return GetPlatformAppDataFolder();
			}
		}

		public static string GetPlatformAppFolder() {
			// Your game has read+write access to /var/mobile/Applications/XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX/Documents 
			// Application.dataPath returns              
			// /var/mobile/Applications/XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX/myappname.app/Data 
			// Strip "/Data" from path 
			string path = GetPlatformAppDataFolder().Substring(0, Application.dataPath.Length - 5);
			return path;
		}

		public static string GetPlatformAppRootFolder() {
			// Your game has read+write access to /var/mobile/Applications/XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX/Documents 
			// Application.dataPath returns              
			// /var/mobile/Applications/XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX/myappname.app/Data 
			// Strip "/Data" from path 
			string path = GetPlatformAppDataFolder().Substring(0, Application.dataPath.Length - 5);
			// Strip application name 
			path = path.Substring(0, path.LastIndexOf('/'));
			return path;
		}

		public static string GetDeviceDocumentsFolder() {
			// Your game has read+write access to /var/mobile/Applications/XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX/Documents 
			// Application.dataPath returns              
			// /var/mobile/Applications/XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX/myappname.app/Data 
			// Strip "/Data" from path 
			string path = GetPlatformAppRootFolder();
			return path + "/Documents";			
		}

		public static void SetLocalSettingString(string key, string value) {
			PlayerPrefs.SetString(key, value);
		}

		public static string GetLocalSettingString(string key) {
			string value = PlayerPrefs.GetString(key);
			if(string.IsNullOrEmpty(value))
				return "";
			return value;
		}

		public static void SetLocalSettingFloat(string key, float value) {
			PlayerPrefs.SetFloat(key, value);
		}

		public static float GetLocalSettingFloat(string key) {
			float value = PlayerPrefs.GetFloat(key);
			return value;
		}

		public static void SetLocalSettingInt(string key, int value) {
			PlayerPrefs.SetInt(key, value);
		}

		public static int GetLocalSettingInt(string key) {
			int value = PlayerPrefs.GetInt(key);
			return value;
		}

		public static void SetLocalSettingDateTime(string key, DateTime value) {
			string dateTimeString = Convert.ToString(value.ToUniversalTime());
			PlayerPrefs.SetString(key, dateTimeString);
		}

		public static DateTime GetLocalSettingDateTime(string key) {
			string value = PlayerPrefs.GetString(key);
			if(string.IsNullOrEmpty(value)) {
				return DateTime.MinValue;
			}
			DateTime dt = DateTime.MinValue;
			bool validDate = DateTime.TryParse(value, out dt);
			if(!validDate) {
				dt = DateTime.MinValue;
			}
			return dt;
		}

	}
}