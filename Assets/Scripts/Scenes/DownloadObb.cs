using UnityEngine;
using System.Collections;

namespace FightingLegends
{
	public class DownloadObb : MonoBehaviour
	{	
//		void Start()
//		{
//			try
//			{
//				if (!GooglePlayDownloader.RunningOnAndroid())
//				{
//	//				GUI.Label(new Rect(10, 10, Screen.width-10, 20), "Use GooglePlayDownloader only on Android device!");
//					return;
//				}
//				
//				string expPath = GooglePlayDownloader.GetExpansionFilePath();
//				if (expPath == null)
//				{
//					Debug.LogError("DownloadObb: External storage is not available!");
//	//					GUI.Label(new Rect(10, 10, Screen.width-10, 20), "External storage is not available!");
//				}
//				else
//				{
//					string mainPath = GooglePlayDownloader.GetMainOBBPath(expPath);
//					string patchPath = GooglePlayDownloader.GetPatchOBBPath(expPath);
//	//				
//	//				GUI.Label(new Rect(10, 10, Screen.width-10, 20), "Main = ..."  + ( mainPath == null ? " NOT AVAILABLE" :  mainPath.Substring(expPath.Length)));
//	//				GUI.Label(new Rect(10, 25, Screen.width-10, 20), "Patch = ..." + (patchPath == null ? " NOT AVAILABLE" : patchPath.Substring(expPath.Length)));
//					if (mainPath == null || patchPath == null)
//	//					if (GUI.Button(new Rect(10, 100, 100, 100), "Fetch OBBs"))
//							GooglePlayDownloader.FetchOBB();
//				}
//			}
//			catch (System.Exception ex)
//			{
//				Debug.LogError("DownloadObb: " + ex.Message);
//			}
//		}
	}
}
