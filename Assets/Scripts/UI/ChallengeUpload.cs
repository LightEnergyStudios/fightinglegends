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

		public Text ReplaceMessage;
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

		public Image dojoImage;
		public Image hawaiiImage;
		public Image sovietImage;
		public Image ghettoImage;
		public Image chinaImage;
		public Image hongKongImage;
		public Image tokyoImage;
		public Image cubaImage;
		public Image nigeriaImage;
		public Image spaceStationImage;

		public Image dojoFilmStrip;
		public Image hawaiiFilmStrip;
		public Image sovietFilmStrip;
		public Image ghettoFilmStrip;
		public Image chinaFilmStrip;
		public Image hongKongFilmStrip;
		public Image tokyoFilmStrip;
		public Image cubaFilmStrip;
		public Image nigeriaFilmStrip;
		public Image spaceStationFilmStrip;

		public ParticleSystem dojoStars;
		public ParticleSystem hawaiiStars;
		public ParticleSystem sovietStars;
		public ParticleSystem ghettoStars;
		public ParticleSystem chinaStars;
		public ParticleSystem hongKongStars;
		public ParticleSystem tokyoStars;
		public ParticleSystem cubaStars;
		public ParticleSystem nigeriaStars;
		public ParticleSystem spaceStationStars;

		private int dojoIndex; 			// image behind film strip unless hilighted
		private int hawaiiIndex;
		private int sovietIndex;
		private int ghettoIndex;
		private int chinaIndex;
		private int hongKongIndex;
		private int tokyoIndex;
		private int cubaIndex;
		private int nigeriaIndex;
		private int spaceStationIndex;

		private int dojoStripIndex;		// film strip on top of image unless hilighted
		private int hawaiiStripIndex;
		private int sovietStripIndex;
		private int ghettoStripIndex;
		private int chinaStripIndex;
		private int hongKongStripIndex;
		private int tokyoStripIndex;
		private int cubaStripIndex;
		private int nigeriaStripIndex;
		private int spaceStationStripIndex;

		public Text locationHeading;
		public Text locationLabel;	// as selected

		public Color lolightColour;				// when location not hilighted

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

			ReplaceMessage.text = FightManager.Translate("replaceChallenge", false, true);
			ReplaceMessage.gameObject.SetActive(false);

			InitLocationSiblings();
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

			ResetLocationHilights();
		}

		public void Confirm(ChallengeData challenge, Action onConfirm)
		{
			if (challenge == null)
				return;

			uploadChallenge = challenge;

			ReplaceMessage.gameObject.SetActive(! string.IsNullOrEmpty(FightManager.UserLoginProfile.ChallengeKey));

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

//			ResetLocationGlows();

			HilightLocation(location);

//			var glow = GetLocationImage(location);
//			if (glow != null)
//				ActivateGlow(glow, true);
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
//			ActivateGlow(dojoGlow, false);
//			ActivateGlow(hawaiiGlow, false);
//			ActivateGlow(sovietGlow, false);
//			ActivateGlow(ghettoGlow, false);
//			ActivateGlow(chinaGlow, false);
//			ActivateGlow(hongKongGlow, false);
//			ActivateGlow(tokyoGlow, false);
//			ActivateGlow(cubaGlow, false);
//			ActivateGlow(nigeriaGlow, false);
//			ActivateGlow(spaceStationGlow, false);
		}

		private void ActivateGlow(Image glow, bool activate)
		{
//			if (glow == null)
//				return;
//			
//			glow.GetComponent<Image>().enabled = activate;
//			glow.GetComponent<Animator>().enabled = activate;
		}
			
		private void InitLocationSiblings()
		{
			dojoIndex = dojoImage.transform.GetSiblingIndex();
			hawaiiIndex = hawaiiImage.transform.GetSiblingIndex();
			sovietIndex = sovietImage.transform.GetSiblingIndex();
			ghettoIndex = ghettoImage.transform.GetSiblingIndex();
			chinaIndex = chinaImage.transform.GetSiblingIndex();
			hongKongIndex = hongKongImage.transform.GetSiblingIndex();
			tokyoIndex = tokyoImage.transform.GetSiblingIndex();
			cubaIndex = cubaImage.transform.GetSiblingIndex();
			nigeriaIndex = nigeriaImage.transform.GetSiblingIndex();
			spaceStationIndex = spaceStationImage.transform.GetSiblingIndex();

			dojoStripIndex = dojoFilmStrip.transform.GetSiblingIndex();
			hawaiiStripIndex = hawaiiFilmStrip.transform.GetSiblingIndex();
			sovietStripIndex = sovietFilmStrip.transform.GetSiblingIndex();
			ghettoStripIndex = ghettoFilmStrip.transform.GetSiblingIndex();
			chinaStripIndex = chinaFilmStrip.transform.GetSiblingIndex();
			hongKongStripIndex = hongKongFilmStrip.transform.GetSiblingIndex();
			tokyoStripIndex = tokyoFilmStrip.transform.GetSiblingIndex();
			cubaStripIndex = cubaFilmStrip.transform.GetSiblingIndex();
			nigeriaStripIndex = nigeriaFilmStrip.transform.GetSiblingIndex();
			spaceStationStripIndex = spaceStationFilmStrip.transform.GetSiblingIndex();
		}

		private void ResetLocationHilights()
		{
			dojoImage.transform.SetSiblingIndex(dojoIndex);
			hawaiiImage.transform.SetSiblingIndex(hawaiiIndex);
			sovietImage.transform.SetSiblingIndex(sovietIndex);
			ghettoImage.transform.SetSiblingIndex(ghettoIndex);
			chinaImage.transform.SetSiblingIndex(chinaIndex);
			hongKongImage.transform.SetSiblingIndex(hongKongIndex);
			tokyoImage.transform.SetSiblingIndex(tokyoIndex);
			cubaImage.transform.SetSiblingIndex(cubaIndex);
			nigeriaImage.transform.SetSiblingIndex(nigeriaIndex);
			spaceStationImage.transform.SetSiblingIndex(spaceStationIndex);

			dojoFilmStrip.transform.SetSiblingIndex(dojoStripIndex);
			hawaiiFilmStrip.transform.SetSiblingIndex(hawaiiStripIndex);
			sovietFilmStrip.transform.SetSiblingIndex(sovietStripIndex);
			ghettoFilmStrip.transform.SetSiblingIndex(ghettoStripIndex);
			chinaFilmStrip.transform.SetSiblingIndex(chinaStripIndex);
			hongKongFilmStrip.transform.SetSiblingIndex(hongKongStripIndex);
			tokyoFilmStrip.transform.SetSiblingIndex(tokyoStripIndex);
			cubaFilmStrip.transform.SetSiblingIndex(cubaStripIndex);
			nigeriaFilmStrip.transform.SetSiblingIndex(nigeriaStripIndex);
			spaceStationFilmStrip.transform.SetSiblingIndex(spaceStationStripIndex);

			dojoImage.color = lolightColour;
			hawaiiImage.color = lolightColour;
			sovietImage.color = lolightColour;
			ghettoImage.color = lolightColour;
			chinaImage.color = lolightColour;
			hongKongImage.color = lolightColour;
			tokyoImage.color = lolightColour;
			cubaImage.color = lolightColour;
			nigeriaImage.color = lolightColour;
			spaceStationImage.color = lolightColour;

			dojoStars.Stop();
			hawaiiStars.Stop();
			sovietStars.Stop();
			ghettoStars.Stop();
			chinaStars.Stop();
			hongKongStars.Stop();
			tokyoStars.Stop();
			cubaStars.Stop();
			nigeriaStars.Stop();
			spaceStationStars.Stop();
		}
			
		// pop location image on top of film strip to hilight
		private void HilightLocation(string location)
		{
			ResetLocationHilights();

			switch (location)
			{
				case FightManager.dojo:
					dojoImage.transform.SetSiblingIndex(dojoStripIndex);		// image on top
					dojoFilmStrip.transform.SetSiblingIndex(dojoIndex);			// film strip behind
					dojoStars.Play();
					dojoImage.color = Color.white;
					break;

				case FightManager.hawaii:
					hawaiiImage.transform.SetSiblingIndex(hawaiiStripIndex);		// image on top
					hawaiiFilmStrip.transform.SetSiblingIndex(hawaiiIndex);			// film strip behind
					hawaiiStars.Play();
					hawaiiImage.color = Color.white;
					break;

				case FightManager.soviet:
					sovietImage.transform.SetSiblingIndex(sovietStripIndex);		// image on top
					sovietFilmStrip.transform.SetSiblingIndex(sovietIndex);			// film strip behind
					sovietStars.Play();
					sovietImage.color = Color.white;
					break;

				case FightManager.ghetto:
					ghettoImage.transform.SetSiblingIndex(ghettoStripIndex);		// image on top
					ghettoFilmStrip.transform.SetSiblingIndex(ghettoIndex);			// film strip behind
					ghettoStars.Play();
					ghettoImage.color = Color.white;
					break;

				case FightManager.china:
					chinaImage.transform.SetSiblingIndex(chinaStripIndex);		// image on top
					chinaFilmStrip.transform.SetSiblingIndex(chinaIndex);			// film strip behind
					chinaStars.Play();
					chinaImage.color = Color.white;
					break;

				case FightManager.hongKong:
					hongKongImage.transform.SetSiblingIndex(hongKongStripIndex);		// image on top
					hongKongFilmStrip.transform.SetSiblingIndex(hongKongIndex);			// film strip behind
					hongKongStars.Play();
					hongKongImage.color = Color.white;
					break;

				case FightManager.tokyo:
					tokyoImage.transform.SetSiblingIndex(tokyoStripIndex);		// image on top
					tokyoFilmStrip.transform.SetSiblingIndex(tokyoIndex);			// film strip behind
					tokyoStars.Play();
					tokyoImage.color = Color.white;
					break;

				case FightManager.cuba:
					cubaImage.transform.SetSiblingIndex(cubaStripIndex);		// image on top
					cubaFilmStrip.transform.SetSiblingIndex(cubaIndex);			// film strip behind
					cubaStars.Play();
					cubaImage.color = Color.white;
					break;

				case FightManager.nigeria:
					nigeriaImage.transform.SetSiblingIndex(nigeriaStripIndex);		// image on top
					nigeriaFilmStrip.transform.SetSiblingIndex(nigeriaIndex);			// film strip behind
					nigeriaStars.Play();
					nigeriaImage.color = Color.white;
					break;

				case FightManager.spaceStation:
					spaceStationImage.transform.SetSiblingIndex(spaceStationStripIndex);		// image on top
					spaceStationFilmStrip.transform.SetSiblingIndex(spaceStationIndex);			// film strip behind
					spaceStationStars.Play();
					spaceStationImage.color = Color.white;
					break;

				default:
					break;
			}
		}

//		private Image GetLocationImage(string location)
//		{
//			switch (location)
//			{
//				case FightManager.dojo:
//					return dojoImage;
//
//				case FightManager.hawaii:
//					return hawaiiImage;
//
//				case FightManager.soviet:
//					return sovietImage;
//
//				case FightManager.ghetto:
//					return ghettoImage;
//
//				case FightManager.china:
//					return chinaImage;
//
//				case FightManager.hongKong:
//					return hongKongImage;
//
//				case FightManager.tokyo:
//					return tokyoImage;
//
//				case FightManager.cuba:
//					return cubaImage;
//
//				case FightManager.nigeria:
//					return nigeriaImage;
//
//				case FightManager.spaceStation:
//					return spaceStationImage;
//
//				default:
//					return null;
//			}
//		}
//
//		private Image GetLocationFilmStrip(string location)
//		{
//			switch (location)
//			{
//				case FightManager.dojo:
//					return dojoFilmStrip;
//
//				case FightManager.hawaii:
//					return hawaiiFilmStrip;
//
//				case FightManager.soviet:
//					return sovietFilmStrip;
//
//				case FightManager.ghetto:
//					return ghettoFilmStrip;
//
//				case FightManager.china:
//					return chinaFilmStrip;
//
//				case FightManager.hongKong:
//					return hongKongFilmStrip;
//
//				case FightManager.tokyo:
//					return tokyoFilmStrip;
//
//				case FightManager.cuba:
//					return cubaFilmStrip;
//
//				case FightManager.nigeria:
//					return nigeriaFilmStrip;
//
//				case FightManager.spaceStation:
//					return spaceStationFilmStrip;
//
//				default:
//					return null;
//			}
//		}

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
				uploadChallenge.Location = FightManager.hawaii;

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
