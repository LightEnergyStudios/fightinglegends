using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Prototype.NetworkLobby;

namespace FightingLegends
{
	public class Opening : SceneLoader
	{
		public LogoFX BHS;						// animation
		public Image fightingLegends;			// logo
		public Text loadingText;				// animated dots
		public HitFlash whiteFlash;				// not used

		private LobbyManager lobbyManager;

		private const string loading = "Loading";

		private const float dotInterval = 0.5f;

		private IEnumerator preloadCoroutine = null;


		// initialization
		public void Start()
		{
//			Debug.Log("Opening.Start");

			fightingLegends.enabled = false;
			loadingText.enabled = false;

			whiteFlash.gameObject.SetActive(false);
		}

		private void OnEnable()
		{
//			Debug.Log("Opening.OnEnable");

			// LobbyManager is not destroyed between scenes, but we have to
			// get a new reference to it after scene is switched
			var lobbyManagerObject = GameObject.Find("LobbyManager");
			lobbyManager = lobbyManagerObject.GetComponent<LobbyManager>();

			if (lobbyManager != null)
			{
				if (SceneSettings.ShowLobbyUI)
				{
					lobbyManager.ShowLobbyUI();
//					PreloadCombat();
				}
				else
					lobbyManager.HideLobbyUI();
			}
			else
				Debug.Log("Opening.OnEnable: lobbyManager not found!");

			OnPreloadComplete += PreloadComplete;
			BHS.OnDrums += OnLogoDrums;
		}

		private void OnDisable()
		{
			OnPreloadComplete -= PreloadComplete;
			BHS.OnDrums -= OnLogoDrums;
		}

//		private IEnumerator WhiteFlash()
//		{
//			whiteFlash.gameObject.SetActive(true);
//			yield return StartCoroutine(whiteFlash.PlayHitFlash());
//			yield return StartCoroutine(whiteFlash.PlayHitFlash());
//			whiteFlash.gameObject.SetActive(false);
//		}

		private void OnLogoDrums()
		{
//			Debug.Log("OnLogoDrums");
//			StartCoroutine(WhiteFlash());

			PreloadCombat();	
			fightingLegends.enabled = true;	

			LoadingMessage();

			SceneSettings.OpeningSequencePlayed = true;
		}

		public void PreloadCombat()
		{
			if (preloadCoroutine != null)
				StopCoroutine(preloadCoroutine);

			preloadCoroutine = PreloadSceneAsync(CombatScene);
			StartCoroutine(preloadCoroutine);	
		}

		private IEnumerator AnimateLoadingText()
		{
			int dots = 3;

			while (loadingText.enabled)
			{
//				Debug.Log("AnimateLoadingText: " + dots);
				loadingText.text = loading;

				for (int i = 0; i < dots; i++)
				{
					loadingText.text += ".";
				}
						
				if (dots == 3)
					dots = 0;
				else
					dots++;

				yield return new WaitForSeconds(dotInterval);
			}

			yield return null;
		}

		private void LoadingMessage()
		{
			loadingText.enabled = true;
			StartCoroutine(AnimateLoadingText());
		}


		private void PreloadComplete(string scene)
		{
////			Debug.Log("Opening.PreloadComplete");
			if (! SceneSettings.ShowLobbyUI)
				ActivatePreloadedScene();		// ninja school if not completed else mode select
		}
	}
}
