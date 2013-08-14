using System.Collections;
using UnityEngine;

namespace Gameverses {

    public class PlatformContext {

        // -------------------------------------------------------------------
        // Singleton access

        private static PlatformContext _current;

        public PlatformContext() {
        }

        public static PlatformContext Current {
            get {
                if (_current == null) {
                    _current = new PlatformContext();
                }
                return _current;
            }
        }

        public bool isWeb {
            get {
                return isWebMac || isWebWindows;
            }
        }

        public bool isWebMac {
            get {
                return Application.platform == RuntimePlatform.OSXWebPlayer;
            }
        }

        public bool isWebWindows {
            get {
                return Application.platform == RuntimePlatform.WindowsWebPlayer;
            }
        }

        public bool isMobile {
            get {
                return isMobileiOS || isMobileAndroid;
            }
        }

        public bool isMobileAndroid {
            get {
                return Application.platform == RuntimePlatform.Android;
            }
        }

        public bool isMobileiOS {
            get {
                return Application.platform == RuntimePlatform.IPhonePlayer;
            }
        }

        public bool isEditor {
            get {
                return isEditorMac || isEditorWindows;
            }
        }

        public bool isEditorMac {
            get {
                return Application.platform == RuntimePlatform.OSXEditor;
            }
        }

        public bool isEditorWindows {
            get {
                return Application.platform == RuntimePlatform.WindowsEditor;
            }
        }

        public bool isGoogleNativeClient {
            get {
                return Application.platform == RuntimePlatform.NaCl;
            }
        }

        public bool isDesktop {
            get {
                return isDesktopMac || isDesktopWindows || isDesktopLinux;
            }
        }

        public bool isDesktopMac {
            get {
                return Application.platform == RuntimePlatform.OSXPlayer;
            }
        }

        public bool isDesktopWindows {
            get {
                return Application.platform == RuntimePlatform.WindowsPlayer;
            }
        }

        public bool isDesktopLinux {
            get {
                return Application.platform == RuntimePlatform.LinuxPlayer;
            }
        }

        public bool isConsole {
            get {
                return isConsolePS3 || isConsoleXbox360 || isConsoleWii;
            }
        }

        public bool isConsolePS3 {
            get {
                return Application.platform == RuntimePlatform.PS3;
            }
        }

        public bool isConsoleXbox360 {
            get {
                return Application.platform == RuntimePlatform.XBOX360;
            }
        }

        public bool isConsoleWii {
            get {
                return Application.platform == RuntimePlatform.WiiPlayer;
            }
        }
    }
}