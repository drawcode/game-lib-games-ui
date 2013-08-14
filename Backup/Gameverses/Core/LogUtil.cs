using UnityEngine;
using System;
using System.Collections.Generic;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace Gameverses {

	public class LogUtil {
		
		public static bool enableLogging = true;

		public static void Log(object message) {
			if(enableLogging) {
				Debug.Log(message);
				// Output to Firebug or inspectors as avail in the browser.
				if(Application.platform == RuntimePlatform.OSXWebPlayer
					|| Application.platform == RuntimePlatform.WindowsWebPlayer) {
						if(message.GetType() == typeof(String))
							Application.ExternalCall("if(window.console)console.log", message);
						else
							Application.ExternalCall("if(window.console)console.log", message);
				}
			}
		}

		public static void LogError(object message) {
			if(enableLogging) {
				Debug.LogError(message);
				// Output to Firebug or inspectors as avail in the browser.
				if(Application.platform == RuntimePlatform.OSXWebPlayer
					|| Application.platform == RuntimePlatform.WindowsWebPlayer) {
					if(message.GetType() == typeof(String))
						Application.ExternalCall("if(window.console)console.error", message);
					else
						Application.ExternalCall("if(window.console)console.error", message);
				}
			}
		}
	}
}