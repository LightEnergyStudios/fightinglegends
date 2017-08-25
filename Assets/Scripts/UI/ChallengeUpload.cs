using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;


namespace FightingLegends
{
	public class ChallengeUpload : MonoBehaviour
	{
		public Image panel;
		private Vector3 panelScale;

		private Image background;
		public Color backgroundColour;	

		public Text walletCoins;
		public Text teamCoins;

		public float fadeTime;

		public Button yesButton;
		public Text yesText;
		public Button noButton;
		public Text noText;

		public Text expiryDate;

		private Action actionOnYes;

		public AudioClip FadeSound;
		public AudioClip CoinSound;
		public AudioClip EntrySound;
		public AudioClip YesSound;
		public AudioClip NoSound;

		public DifficultySelector difficultySelector;

		public Image categoryImage;
		public Text categoryLabel;

		public Sprite diamondSprite;
		public Sprite goldSprite;
		public Sprite silverSprite;
		public Sprite bronzeSprite;
		public Sprite ironSprite;


		public ScrollRect locationViewport;
		public GameObject locationContent;				// horizontal viewport content
//		private List<string> locationList = new List<string>();

		public Button dojoButton;
		public Button hawaiiButton;
		public Button sovietButton;
		public Button ghettoButton;
		public Button chinaButton;
		public Button hongKongButton;
		public Button tokyoButton;
		public Button cubaButton;
		public Button nigeriaButton;
		public Button spaceStationButton;

		public Image dojoGlow;
		public Image hawaiiGlow;
		public Image sovietGlow;
		public Image ghettoGlow;
		public Image chinaGlow;
		public Image hongKongGlow;
		public Image tokyoGlow;
		public Image cubaGlow;
		public Image nigeriaGlow;
		public Image spaceStationGlow;

		public Text locationHeading;
		public Text locationLabel;	// as selected

		public ParticleSystem coinStars;
		private const float starSweepX = 250.0f;				// lerp target position
		private const float starSweepTime = 0.2f;

		private ChallengeData uploadChallenge = null;			// to be uploaded


		public delegate void CancelClickedDelegate(AIDifficulty selectedDifficulty, string selectedLocation);
		public static CancelClickedDelegate OnCancelClicked;


		void Awake()
		{
			panel.gameObject.SetActive(false);

			background = panel.GetComponent<Image>();
			background.color = backgroundColour;
			panelScale = panel.transform.localScale;

			yesText.text = FightManager.Translate("upload", false, true);
			noText.text = FightManager.Translate("cancel");
		}

		private void OnEnable()
		{
			dojoButton.onClick.AddListener(delegate { SetLocation(FightManager.dojo); });
			hawaiiButton.onClick.AddListener(delegate { SetLocation(FightManager.hawaii); });
			sovietButton.onClick.AddListener(delegate { SetLocation(FightManager.soviet); });
			ghettoButton.onClick.AddListener(delegate { SetLocation(FightManager.ghetto); });
			chinaButton.onClick.AddListener(delegate { SetLocation(FightManager.china); });
			hongKongButton.onClick.AddListener(delegate { SetLocation(FightManager.hongKong); });
			tokyoButton.onClick.AddListener(delegate { SetLocation(FightManager.tokyo); });
			cubaButton.onClick.AddListener(delegate { SetLocation(FightManager.cuba); });
			nigeriaButton.onClick.AddListener(delegate { SetLocation(FightManager.nigeria); });
			spaceStationButton.onClick.AddListener(delegate { SetLocation(FightManager.spaceStation); });

			yesButton.onClick.AddListener(YesClicked);
			noButton.onClick.AddListener(NoClicked);

			if (difficultySelector != null)
				difficultySelector.OnDifficultySelected += SetTeamDifficulty;

//			for (int i = 0; i < locationContent.transform.childCount; i++)
//			{
//				Debug.Log("Locations: " + locationContent.transform.GetChild(i).name + " sibling index" + locationContent.transform.GetChild(i).GetSiblingIndex());
//			}

//			SetupLocations();
		}

		private void OnDisable()
		{
//			simpleButton.onClick.RemoveListener(delegate { SetTeamDifficulty(AIDifficulty.Simple); });
//			easyButton.onClick.RemoveListener(delegate { SetTeamDifficulty(AIDifficulty.Easy); });
//			mediumButton.onClick.RemoveListener(delegate { SetTeamDifficulty(AIDifficulty.Medium); });
//			hardButton.onClick.RemoveListener(delegate { SetTeamDifficulty(AIDifficulty.Hard); });
//			brutalButton.onClick.RemoveListener(delegate { SetTeamDifficulty(AIDifficulty.Brutal); });

			dojoButton.onClick.RemoveListener(delegate { SetLocation(FightManager.dojo); });
			hawaiiButton.onClick.RemoveListener(delegate { SetLocation(FightManager.hawaii); });
			sovietButton.onClick.RemoveListener(delegate { SetLocation(FightManager.soviet); });
			ghettoButton.onClick.RemoveListener(delegate { SetLocation(FightManager.ghetto); });
			chinaButton.onClick.RemoveListener(delegate { SetLocation(FightManager.china); });
			hongKongButton.onClick.RemoveListener(delegate { SetLocation(FightManager.hongKong); });
			tokyoButton.onClick.RemoveListener(delegate { SetLocation(FightManager.tokyo); });
			cubaButton.onClick.RemoveListener(delegate { SetLocation(FightManager.cuba); });
			nigeriaButton.onClick.RemoveListener(delegate { SetLocation(FightManager.nigeria); });
			spaceStationButton.onClick.RemoveListener(delegate { SetLocation(FightManager.spaceStation); });

			yesButton.onClick.RemoveListener(YesClicked);
			noButton.onClick.RemoveListener(NoClicked);

			if (difficultySelector != null)
				difficultySelector.OnDifficultySelected -= SetTeamDifficulty;
		}

		public void Confirm(ChallengeData challenge, Action onConfirm)
		{
			if (challenge == null)
				return;

			uploadChallenge = challenge;

			actionOnYes = onConfirm;
			StartCoroutine(Show());
		}

		private void SetChallengeCategory()
		{
			if (uploadChallenge == null)
				return;
			
			var category = TeamSelect.DetermineCoinCategory(uploadChallenge);
			uploadChallenge.ParentCategory = category.ToString();
			categoryLabel.text = FightManager.Translate(category.ToString().ToLower());

			switch (category)
			{
				case ChallengeCategory.Diamond:
					categoryImage.sprite = diamondSprite;
					break;

				case ChallengeCategory.Gold:
					categoryImage.sprite = goldSprite;
					break;

				case ChallengeCategory.Silver:
					categoryImage.sprite = silverSprite;
					break;

				case ChallengeCategory.Bronze:
					categoryImage.sprite = bronzeSprite;
					break;

				case ChallengeCategory.Iron:
					categoryImage.sprite = ironSprite;
					break;
			}
		}

		private void SetTeamCoins(bool force = false)
		{
			if (uploadChallenge == null)
				return;

			var coins = Store.ChallengeCoinValue(uploadChallenge, true);
			bool changed = uploadChallenge.PrizeCoins != coins;

			if (force || changed)
			{
				uploadChallenge.PrizeCoins = coins;
				teamCoins.text = string.Format("{0:N0}", uploadChallenge.PrizeCoins);

				SetChallengeCategory();			// according to coin value

				StartCoroutine(CoinStarSweep(changed));
			}

			yesButton.interactable = Store.CanAfford(coins);		// TODO: prompt to buy more coins if can't afford
		}

		private void SetLocation(string location)
		{
			if (uploadChallenge == null)
				return;
			
			uploadChallenge.Location = location;
			locationLabel.text = location.ToUpper();

			ResetLocationGlows();

			var glow = GetLocationGlow(location);
			if (glow != null)
				ActivateGlow(glow, true);
		}

		private void ScrollToLocation(string location)
		{
//			for (int i = 0; i < locationContent.transform.childCount; i++)
//			{
//				Debug.Log("Locations: " + locationContent.transform.GetChild(i).name + " sibling index" + locationContent.transform.GetChild(i).GetSiblingIndex());
//			}

//			moveImage.transform.SetParent(locationContent.transform);
//
//			var location = locationContent.transform.FindChild(location);
//			var nextMove = locationContent.transform.GetChild(locationIndex);
//
//
//			var children = locationContent.GetComponentsInChildren<Image>();
//
//			for (int i = 0; i < locationContent.transform.childCount; i++)
//			{
//				Debug.Log("ScrollToLocation: " + locationContent.transform.GetChild(i).name + " sibling index" + locationContent.transform.GetChild(i).GetSiblingIndex);
//			}
//
//			locationContent.transform.chil
		}

		private void ResetLocationGlows()
		{
			ActivateGlow(dojoGlow, false);
			ActivateGlow(hawaiiGlow, false);
			ActivateGlow(sovietGlow, false);
			ActivateGlow(ghettoGlow, false);
			ActivateGlow(chinaGlow, false);
			ActivateGlow(hongKongGlow, false);
			ActivateGlow(tokyoGlow, false);
			ActivateGlow(cubaGlow, false);
			ActivateGlow(nigeriaGlow, false);
			ActivateGlow(spaceStationGlow, false);
		}

		private void ActivateGlow(Image glow, bool activate)
		{
			if (glow == null)
				return;
			
			glow.GetComponent<Image>().enabled = activate;
			glow.GetComponent<Animator>().enabled = activate;
		}
			

		private Image GetLocationGlow(string location)
		{
			switch (location)
			{
				case FightManager.dojo:
					return dojoGlow;

				case FightManager.hawaii:
					return hawaiiGlow;

				case FightManager.soviet:
					return sovietGlow;

				case FightManager.ghetto:
					return ghettoGlow;

				case FightManager.china:
					return chinaGlow;

				case FightManager.hongKong:
					return hongKongGlow;

				case FightManager.tokyo:
					return tokyoGlow;

				case FightManager.cuba:
					return cubaGlow;

				case FightManager.nigeria:
					return nigeriaGlow;

				case FightManager.spaceStation:
					return spaceStationGlow;

				default:
					return null;
			}
		}

		// set all memebers of the challenge team to the selected difficulty
		private void SetTeamDifficulty(AIDifficulty difficulty)
		{			
			if (uploadChallenge == null)
				return;

			foreach (var teamMember in uploadChallenge.Team)
			{
				teamMember.Difficulty = difficulty.ToString();
			}

			SetTeamCoins();
		}

		private AIDifficulty GetTeamDifficulty(ChallengeData challenge)
		{
			AIDifficulty? firstDifficulty = null;

			foreach (var teamMember in challenge.Team)
			{
//				Debug.Log("GetTeamDifficulty: " + teamMember.Difficulty);
				var difficulty = (AIDifficulty) Enum.Parse(typeof(AIDifficulty), teamMember.Difficulty);

				if (firstDifficulty == null)					// first team member
					firstDifficulty = difficulty;

				if (difficulty != firstDifficulty)				// not same as first - must be mixed difficulties
					return difficultySelector.DefaultDifficulty;
			}

			return firstDifficulty != null ? firstDifficulty.GetValueOrDefault() : difficultySelector.DefaultDifficulty;
		}

	
		private IEnumerator Show()
		{
			panel.gameObject.SetActive(true);

			background.color = Color.clear;

			walletCoins.text = string.Format("{0:N0}", FightManager.Coins);

//			var created = Convert.ToDateTime(uploadChallenge.DateCreated);
//			var expiry = Convert.ToDateTime(uploadChallenge.ExpiryDate);
//			expiryDate.text = FightManager.Translate("challengeExpires") + ":\n" + expiry.ToShortDateString() + " " + expiry.ToShortTimeString();

			if (string.IsNullOrEmpty(uploadChallenge.Location))
				uploadChallenge.Location = FightManager.dojo;

			SetLocation(uploadChallenge.Location);		// set as per current location
			SetTeamCoins(true);

			var teamDifficulty = GetTeamDifficulty(uploadChallenge);		// default difficulty if mixed	
			SetTeamDifficulty(teamDifficulty);

			if (FadeSound != null)
				AudioSource.PlayClipAtPoint(FadeSound, Vector3.zero, FightManager.SFXVolume);

			GetComponent<Animator>().SetTrigger("Entry");

			float t = 0.0f;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / fadeTime); 

//				panel.transform.localScale = Vector3.Lerp(Vector3.zero, panelScale, t);
				background.color = Color.Lerp(Color.clear, backgroundColour, t);
				yield return null;
			}
				
			yield return null;
		}

		public IEnumerator Hide()
		{
			float t = 0.0f;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / fadeTime); 

				panel.transform.localScale = Vector3.Lerp(panelScale, Vector3.zero, t);
				background.color = Color.Lerp(backgroundColour, Color.clear, t);
				yield return null;
			}

//			background.enabled = false;
			panel.gameObject.SetActive(false);
			panel.transform.localScale = panelScale;

			ResetLocationGlows();
		
			yield return null;
		}

		public void PlayEntrySound()
		{
			if (EntrySound != null)
				AudioSource.PlayClipAtPoint(EntrySound, Vector3.zero, FightManager.SFXVolume);
		}

		private IEnumerator CoinStarSweep(bool coinSound)
		{
			coinStars.gameObject.SetActive(true);
		
			if (coinSound && CoinSound != null)
				AudioSource.PlayClipAtPoint(CoinSound, Vector3.zero, FightManager.SFXVolume);

			Vector3 startPosition = coinStars.transform.localPosition;
			Vector3 targetPosition = new Vector3(starSweepX, startPosition.y, startPosition.z);
			float t = 0.0f;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / starSweepTime); 

				coinStars.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
				yield return null;
			}

			coinStars.gameObject.SetActive(false);
			coinStars.transform.localPosition = startPosition;
			yield return null;
		}

		private void YesClicked()
		{
			walletCoins.text = string.Format("{0:N0}", FightManager.Coins - uploadChallenge.PrizeCoins);		// display only

			// call the passed-in delegate
			if (actionOnYes != null)
			{
				// spend the coins!
				if (uploadChallenge.PrizeCoins > 0)
				{
					FightManager.Coins -= uploadChallenge.PrizeCoins;
//					coinsToUpload = 0;
				}

				actionOnYes();
			}

			if (YesSound != null)
				AudioSource.PlayClipAtPoint(YesSound, Vector3.zero, FightManager.SFXVolume);

			StartCoroutine(Hide());
		}

		private void NoClicked()
		{
			if (NoSound != null)
				AudioSource.PlayClipAtPoint(NoSound, Vector3.zero, FightManager.SFXVolume);

			StartCoroutine(Hide());

			if (OnCancelClicked != null)
				OnCancelClicked(difficultySelector.SelectedDifficulty, uploadChallenge.Location);
		}
	}
}
