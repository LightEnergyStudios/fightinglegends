using UnityEngine;

namespace FightingLegends
{
    public class DeviceDetector
    {
        private DeviceDetector()
        {
        }

        public static bool IsMobile
        {
			get { return IsAndroid() || IsIOS(); }
        }

		public static bool IsAndroid()
        {
			return IsAndroid(Application.platform);
        }

        public static bool IsAndroid(RuntimePlatform platform)
        {
            return platform == RuntimePlatform.Android;
        }

        public static bool IsIOS()
        {
            return IsIOS(Application.platform);
        }
        public static bool IsIOS(RuntimePlatform platform)
        {
            return platform == RuntimePlatform.IPhonePlayer;
        }

        public static bool IsUnity()
        {
            return Application.platform == RuntimePlatform.WindowsEditor;
        }
    }
}
