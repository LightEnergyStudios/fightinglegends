using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

namespace FightingLegends
{
	public class SceneLoader : MonoBehaviour
	{
		private AsyncOperation asyncLoadSceneOp;

		protected float percentPreloaded = 0.0f;			// pretty useless... 
		protected bool preloadComplete = false;

		public delegate void PreloadStartedDelegate(string scene);
		public static PreloadStartedDelegate OnPreloadStart;

		public delegate void PreloadCompleteDelegate(string scene);
		public static PreloadCompleteDelegate OnPreloadComplete;

		private const float preloadPercent = 0.9f;		// % of scene loaded async - fixed - cannot change

		public const string OpeningScene = "Scenes/Opening";
		public const string CombatScene = "Scenes/Combat";
		public const string LobbyScene = "Scenes/Lobby";


		// initialization
		protected void SceneLoaderInit()
		{
			SceneManager.sceneLoaded += OnSceneLoaded;					// on 100% load
			SceneManager.sceneUnloaded += OnSceneUnloaded;				// not used
			SceneManager.activeSceneChanged += OnActiveSceneChanged;	// not used
		}

		private void OnDestroy()
		{
			SceneManager.sceneLoaded -= OnSceneLoaded;
			SceneManager.sceneUnloaded -= OnSceneUnloaded;
			SceneManager.activeSceneChanged -= OnActiveSceneChanged;
		}


		// loads 90% of scene
		protected IEnumerator PreloadSceneAsync(string scene)
		{
			if (OnPreloadStart != null)
				OnPreloadStart(scene);

			Debug.Log("PreloadSceneAsync: " + scene);

			preloadComplete = false;

			asyncLoadSceneOp = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
			asyncLoadSceneOp.allowSceneActivation = false;

			while (asyncLoadSceneOp.progress < preloadPercent)
			{
				// loading progress useless for animation - not a smooth progression
//				[0, 0.9] > [0, 1]
				percentPreloaded = Mathf.Clamp01(asyncLoadSceneOp.progress / preloadPercent);
				yield return null;
			}

//			while (!asyncLoadSceneOp.isDone)
//			{
//				yield return null;
//			}

			// Loading completed
			preloadComplete = true;

			if (OnPreloadComplete != null)
				OnPreloadComplete(scene);

			yield return null;
		}

		// complete load of scene and activate it
		public void ActivatePreloadedScene()
		{
			Debug.Log("ActivatePreloadedScene");
			asyncLoadSceneOp.allowSceneActivation = true;
		}

		public IEnumerator ActivateWhenPreloaded()
		{
			while (! preloadComplete)
				yield return null;

			ActivatePreloadedScene();
		}
			
		// non async version
		public static void LoadScene(string scene)
		{
			SceneManager.LoadScene(scene, LoadSceneMode.Single);
		}

		public static void UnloadSceneAsync(string scene)
		{
			SceneManager.UnloadSceneAsync(scene);
		}


		#region SceneManager event handlers

		private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			Debug.Log("OnSceneLoaded: " + scene.name + ", mode: " + mode);
		}

		private void OnSceneUnloaded(Scene scene)
		{
			Debug.Log("OnSceneUnloaded: " + scene.name);
		}

		private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
		{
			Debug.Log("OnActiveSceneChanged: " + oldScene.name + " --> " + newScene.name);
		}

		#endregion
	}
}
