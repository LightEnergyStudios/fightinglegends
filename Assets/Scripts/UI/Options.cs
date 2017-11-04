using UnityEngine;
using System.Collections;
using UnityEngine.UI;


namespace FightingLegends
{
	public class Options : MonoBehaviour
	{
		public Button backButton;
		public Button settingsButton;
		public Image coinsPanel;
		public Image coin;

		public Sprite backImage;
		public Sprite pauseImage;
		public Sprite settingsImage;

		public Image kudosPanel;
		public Text Kudos;

		public Text Coins;
		public ParticleSystem CoinStars;

		private const float starSweepDistance = 100.0f;

		private const int numCoinSpins = 4;			// full 360 turns
		private const float coinSpinTime = 0.05f;	// first half-turn
		private const float coinSlowdown = 1.5f;	// increases coinSpinTime on each half-turn
		private bool coinSpinning = false;
		private IEnumerator coinSpinCoroutine;		// so it can be interrupted

		private FightManager fightManager;


		private void Awake()
		{
			var fightManagerObject = GameObject.Find("FightManager");
			fightManager = fightManagerObject.GetComponent<FightManager>();

//			SpeedLabel.text = FightManager.Translate("speed");
		}
	

		private void OnEnable()
		{
			backButton.onClick.AddListener(Back);
			settingsButton.onClick.AddListener(Pause);

			FightManager.OnMenuChanged += MenuChanged;

			coinsPanel.gameObject.SetActive(false);
			FightManager.OnCoinsChanged += CoinsChanged;
			CoinsChanged(FightManager.Coins);				// set current value

			kudosPanel.gameObject.SetActive(false);
			FightManager.OnKudosChanged += KudosChanged;
			KudosChanged(FightManager.Kudos);				// set current value
		}

		private void OnDisable()
		{
			backButton.onClick.RemoveListener(Back);
			settingsButton.onClick.RemoveListener(Pause);

			FightManager.OnMenuChanged -= MenuChanged;
			FightManager.OnCoinsChanged -= CoinsChanged;
			FightManager.OnKudosChanged -= KudosChanged;
		}


		private void MenuChanged(MenuType newMenu, bool canGoBack, bool canSettings, bool showCoins, bool showKudos)
		{
			Debug.Log("MenuChanged: " + newMenu + ", canGoBack = " + canGoBack + ", canSettings = " + canSettings);
			backButton.gameObject.SetActive(canGoBack);
			settingsButton.gameObject.SetActive(canSettings);
			coinsPanel.gameObject.SetActive(showCoins);
			kudosPanel.gameObject.SetActive(showKudos);

//			if (newMenu == MenuType.Combat)
//				backButton.image.sprite = FightManager.IsNetworkFight ? backImage : pauseImage;		// confirm quit fight
//			else
				backButton.image.sprite = backImage;

//			settingsButton.image.sprite = settingsImage;
		}

		private void KudosChanged(float kudos)
		{	
			Kudos.text = string.Format("{0:N0}", kudos);		// thousands separator
		}

		private void CoinsChanged(int coins)
		{	
			Coins.text = string.Format("{0:N0}", coins);		// thousands separator

			if (coinsPanel.gameObject.activeSelf)
			{
				StartCoroutine(CoinStarSweep());	

				fightManager.CoinAudio();

				if (! coinSpinning)
					SpinCoin(numCoinSpins);
			}
		}

		private IEnumerator CoinStarSweep()
		{
			var startPosition = CoinStars.transform.localPosition;
			var targetPosition = new Vector3(startPosition.x + starSweepDistance, startPosition.y, startPosition.z);
			float coinSweepTime = CoinStars.main.duration;
			float t = 0.0f;

			CoinStars.gameObject.SetActive(true);
			CoinStars.Play();

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / coinSweepTime);

				CoinStars.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
				yield return null;
			}

			CoinStars.gameObject.SetActive(false);
			CoinStars.transform.localPosition = startPosition;

			yield return null;
		}


		private void SpinCoin(int numSpins)
		{
			coin.rectTransform.localRotation = Quaternion.Euler(Vector3.zero);

			if (coinSpinCoroutine != null)
				StopCoroutine(coinSpinCoroutine);
			
			coinSpinCoroutine = AnimateSpin(numSpins);
			StartCoroutine(coinSpinCoroutine);
		}


		private IEnumerator AnimateSpin(int numSpins)
		{
			if (numSpins <= 0)
				yield break;

			if (coinSpinning)
				yield break;

			coinSpinning = true;

			var startRotation = Vector3.zero;
			var targetRotation1 = new Vector3(0, 180, 0);
			var targetRotation2 = new Vector3(0, 359, 0);

			numSpins *= 2; 		// 180 deg each
			var spinTime = coinSpinTime;

			while (numSpins > 0)
			{
				float t = 0.0f;

				while (t < 1.0f)
				{
					t += Time.deltaTime * (Time.timeScale / spinTime); 

					if (numSpins % 2 != 0)			// return to zero on even
						coin.rectTransform.localRotation = Quaternion.Lerp(Quaternion.Euler(targetRotation1), Quaternion.Euler(targetRotation2), t);
					else
						coin.rectTransform.localRotation = Quaternion.Lerp(Quaternion.Euler(startRotation), Quaternion.Euler(targetRotation1), t);

					yield return null;
				}

				numSpins--;
				spinTime *= coinSlowdown;
			}

			// reset rotation
			coin.rectTransform.localRotation = Quaternion.Euler(Vector3.zero);

			coinSpinning = false;
			yield return null;
		}


		private void Back()
		{
			fightManager.BackClicked();
		}

		private void Pause()
		{
			fightManager.PauseClicked();
		}
	}
}
