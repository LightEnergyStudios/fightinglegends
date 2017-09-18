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

//		public LobbyManager lobbyManager;
//		public Image lobbyPanel;

		private const string loading = "Loading";

		private const float dotInterval = 0.5f;

		// initialization
		public void Start()
		{
			Debug.Log("Opening.Start");

			fightingLegends.enabled = false;
			loadingText.enabled = false;

//			lobbyPanel.gameObject.SetActive(false);

			whiteFlash.gameObject.SetActive(false);
		}

		private void OnEnable()
		{
			Debug.Log("Opening.OnEnable");

//			BHS.OnEndState += LogoEnd;
//			OnPreloadStart += PreloadStart;
			OnPreloadComplete += PreloadComplete;

			BHS.OnDrums += OnLogoDrums;
		}

		private void OnDisable()
		{
//			BHS.OnEndState -= LogoEnd;
//			OnPreloadStart -= PreloadStart;
			OnPreloadComplete -= PreloadComplete;

			BHS.OnDrums -= OnLogoDrums;
		}
			
//		private void Update() 
//		{
			// if tapped / left mouse, finish loading the combat scene
//			if (inputEnabled && ((Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began)
//						|| Input.GetMouseButtonDown(0)))			// left button
//			{
//				tapToPlay.enabled = false;
//					
//				// enable completion of the scene load
//				ActivatePreloadedScene();		// combat/training if not completed else mode select
//			}
//		}


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

			StartCoroutine(PreloadSceneAsync(CombatScene));	
			fightingLegends.enabled = true;	

			LoadingMessage();
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
			Debug.Log("PreloadComplete");

			// enable completion of the scene load
			ActivatePreloadedScene();		// combat/training if not completed else mode select
		}
	}
}
