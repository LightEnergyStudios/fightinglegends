using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


namespace FightingLegends
{
	public class FighterSelect : MenuCanvas
	{
		public Button fightButton;				// not always used (eg. store)
		public Text fightLabel;

//		public Button storeButton;				// not always used (eg. arcade mode, store)
//		public Text storeLabel;	
//
//		public Button powerUpButton;			// not always used (eg. arcade mode, store)
//		public Text powerUpLabel;

		public Text titleText;

		public Button shiroButton;
		public Button natalyaButton;
		public Button hoiLunButton;
		public Button leoniButton;
		public Button danjumaButton;
		public Button jacksonButton;
		public Button alazneButton;
		public Button shiyangButton;
		public Button skeletronButton;
		public Button ninjaButton;

		public Text SelectedName;
		public Text SelectedElements;
		public Text SelectedClass;

		public Image ElementsPanel;
		public Sprite WaterEarth;				// selected fighter element
		public Sprite FireEarth;				// selected fighter element
		public Sprite WaterAir;					// selected fighter element
		public Sprite FireAir;					// selected fighter element

		private const float previewX = 1000;   		// offset to current camera position (fighter camera/layer)
		private const float previewY = 550;
		private const float previewZ = 20; //50;

		protected FightManager fightManager;
		public Fighter previewFighter { get; private set; }		// preview

		public AudioClip PreviewAudio;

		public Color hilightColour;

		protected Dictionary<string, FighterCard> fighterCards = new Dictionary<string, FighterCard>();
		private bool fighterCardsLoaded = false;
		private bool listening = false;

		public Sprite airFireFrame;		// set in Inspector	
		public Sprite airWaterFrame;
		public Sprite earthFireFrame;
		public Sprite earthWaterFrame;
		public Sprite skeletronFrame;
		public Sprite ninjaFrame;

		public Image Coin;									// animated
		private float insertCoinTime = 1.5f;

		public Image InsertCoinTextPanel;
		public List<Text> InsertCoinTop;					// animated text x3
		public List<Text> InsertCoinBottom;					// animated text x3
		private float insertCoinTextTime = 2.0f;

		private List<float> insertCoinTopPosition;			// original x position of animated text x3
		private List<float> insertCoinBottomPosition;		// original x position of animated text x3

		private float insertCoinXOffset = 200.0f;			// looping coin entry from right
		private float insertCoinTextWidth = 210.0f;			// cycling text entry from right
		private const int insertCoinTextRepeats = 3;

		private IEnumerator coinCoroutine = null;
		private IEnumerator insertCoinTopCoroutine = null;
		private IEnumerator insertCoinBottomCoroutine = null;

		// animated entry
		private Animator animator;
		private bool animatingCardEntry = false;
		private bool animatedEntry = false;
		private const float animatePauseTime = 0.5f;		// pause before animated card entry

		public delegate void PreviewCreatedDelegate(Fighter previewFighter, bool fighterChanged);
		public static PreviewCreatedDelegate OnPreviewCreated;

		public delegate void FighterSelectedDelegate(Fighter fighter);
		public static FighterSelectedDelegate OnFighterSelected;


		private void Awake()
		{
			Init();
		}

		private void Start()
		{
			if (titleText != null)
				titleText.text = FightManager.Translate("chooseYourFighter");

			if (fightLabel != null)
				fightLabel.text = FightManager.Translate("fight", false, true);

//			if (storeLabel != null)
//				storeLabel.text = FightManager.Translate("dojo");
//
//			if (powerUpLabel != null)
//				powerUpLabel.text = FightManager.Translate("powerUp"); //, true, false);

			animatedEntry = false;
		}

		public void Init()
		{
			if (fightManager == null)
			{
				var fightManagerObject = GameObject.Find("FightManager");
				fightManager = fightManagerObject.GetComponent<FightManager>();
			}

			LoadFighterCards();		// if not already loaded
			StartListening();		// if not already listening

			if (InsertCoinTextPanel != null)
			{
				SaveInsertCoinTextPositions();
				InsertCoinTextPanel.gameObject.SetActive(false);
			}
		}

		private void OnEnable()
		{
			if (HasActivatedOverlay)
				HideFighter();
			else
				SetPreviewFighter();

			InitFighterCards();		// virtual

			if (!animatedEntry)
				StartCoroutine(AnimateCardEntry());			// virtual (different animators)
			else
			{
				AnimateCoin();
				CycleInsertCoinText();
			}
		}

		protected void SetPreviewFighter()
		{
			if (previewFighter == null || previewFighter.FighterName != FightManager.SelectedFighterName)
			{
				CreatePreview(FightManager.SelectedFighterName, FightManager.SelectedFighterColour, false, true);
				if (previewFighter != null)
					previewFighter.ResetHealth();
			}
			else
				RevealFighter();
		}

		private void OnDisable()
		{
			HideFighter();

			if (InsertCoinTextPanel != null)
				RestoreInsertCoinTextPositions();
			
//			animateInsertCoin = false;
		}

		private void StartListening()
		{
			if (listening)
				return;
			
			if (fightButton != null)
				fightButton.onClick.AddListener(delegate { ShowCombat(); });

//				fightButton.onClick.AddListener(delegate { CombatInsertCoin(); });
//			if (storeButton != null)
//				storeButton.onClick.AddListener(delegate { ShowStore(); });
//			if (powerUpButton != null)
//				powerUpButton.onClick.AddListener(delegate { PowerUpFighter(); });
			
			FightManager.OnThemeChanged += SetTheme;

			if (fightManager.HasPlayer1)
				fightManager.Player1.OnLockedChanged += LockChanged;
			if (fightManager.HasPlayer2)
				fightManager.Player2.OnLockedChanged += LockChanged;
			
			shiroButton.onClick.AddListener(delegate { CreatePreview("Shiro", "P1", true, true); });
			natalyaButton.onClick.AddListener(delegate { CreatePreview("Natalya", "P1", true, true); });
			hoiLunButton.onClick.AddListener(delegate { CreatePreview("Hoi Lun", "P1", true, true); });
			leoniButton.onClick.AddListener(delegate { CreatePreview("Leoni", "P1", true, true); });
			danjumaButton.onClick.AddListener(delegate { CreatePreview("Danjuma", "P1", true, true); });
			jacksonButton.onClick.AddListener(delegate { CreatePreview("Jackson", "P1", true, true); });
			alazneButton.onClick.AddListener(delegate { CreatePreview("Alazne", "P1", true, true); });
			shiyangButton.onClick.AddListener(delegate { CreatePreview("Shiyang", "P1", true, true); });

			if (skeletronButton != null)
				skeletronButton.onClick.AddListener(delegate { CreatePreview("Skeletron", "P1", true, true); });
			if (ninjaButton != null)
				ninjaButton.onClick.AddListener(delegate { CreatePreview("Ninja", "P1", true, true); });

			listening = true;
		}

		private void StopListening()
		{
			if (! listening)
				return;
			
//			Debug.Log("StopListening: " + fightManager.SelectedFighterName + " " + fightManager.SelectedFighterColour);
			FightManager.OnThemeChanged -= SetTheme;

			if (fightManager.HasPlayer1)
				fightManager.Player1.OnLockedChanged -= LockChanged;
			if (fightManager.HasPlayer2)
				fightManager.Player2.OnLockedChanged -= LockChanged;
			
			DestroyPreview();

			if (fightButton != null)
				fightButton.onClick.RemoveListener(delegate { ShowCombat(); });
//			if (storeButton != null)
//				storeButton.onClick.RemoveListener(delegate { ShowStore(); });
//			if (powerUpButton != null)
//				powerUpButton.onClick.RemoveListener(delegate { PowerUpFighter(); });
			
			shiroButton.onClick.RemoveListener(delegate { CreatePreview("Shiro", "P1", true, true); });
			natalyaButton.onClick.RemoveListener(delegate { CreatePreview("Natalya", "P1", true, true); });
			hoiLunButton.onClick.RemoveListener(delegate { CreatePreview("Hoi Lun", "P1", true, true); });
			leoniButton.onClick.RemoveListener(delegate { CreatePreview("Leoni", "P1", true, true); });
			danjumaButton.onClick.RemoveListener(delegate { CreatePreview("Danjuma", "P1", true, true); });
			jacksonButton.onClick.RemoveListener(delegate { CreatePreview("Jackson", "P1", true, true); });
			alazneButton.onClick.RemoveListener(delegate { CreatePreview("Alazne", "P1", true, true); });
			shiyangButton.onClick.RemoveListener(delegate { CreatePreview("Shiyang", "P1", true, true); });

			if (skeletronButton != null)
				skeletronButton.onClick.RemoveListener(delegate { CreatePreview("Skeletron", "P1", true, true); });
			if (ninjaButton != null)
				ninjaButton.onClick.RemoveListener(delegate { CreatePreview("Ninja", "P1", true, true); });

			listening = false;
		}

		private void OnDestroy()
		{
			StopListening();
		}

		private void LoadFighterCards()
		{
			if (fighterCardsLoaded)
				return;

			fighterCards.Add("Leoni", new FighterCard(leoniButton, "Leoni", "P1", 1, 0, null, null, CardFrame("Leoni")));
			fighterCards.Add("Shiro", new FighterCard(shiroButton, "Shiro", "P1", 1, 0, null, null, CardFrame("Shiro")));	
			fighterCards.Add("Natalya", new FighterCard(natalyaButton, "Natalya", "P1", 1, 0, null, null, CardFrame("Natalya")));
			fighterCards.Add("Danjuma", new FighterCard(danjumaButton, "Danjuma", "P1", 1, 0, null, null, CardFrame("Danjuma")));
			fighterCards.Add("Hoi Lun", new FighterCard(hoiLunButton, "Hoi Lun", "P1", 1, 0, null, null, CardFrame("Hoi Lun")));
			fighterCards.Add("Jackson", new FighterCard(jacksonButton, "Jackson", "P1", 1, 0, null, null, CardFrame("Jackson")));
			fighterCards.Add("Shiyang", new FighterCard(shiyangButton, "Shiyang", "P1", 1, 0, null, null, CardFrame("Shiyang")));
			fighterCards.Add("Alazne", new FighterCard(alazneButton, "Alazne", "P1", 1, 0, null, null, CardFrame("Alazne")));

			if (ninjaButton != null)
				fighterCards.Add("Ninja", new FighterCard(ninjaButton, "Ninja", "P1", 1, 0f, null, null, CardFrame("Ninja")));
			if (skeletronButton != null)
				fighterCards.Add("Skeletron", new FighterCard(skeletronButton, "Skeletron", "P1", 1, 0f, null, null, CardFrame("Skeletron")));
			
			fighterCardsLoaded = true;
		}

		private void InitFighterCards()
		{
			// eg. SurvivalSelect override loads fighter profiles

			// lookup lock status from fighter profile (saved) data (all fighters)
			// lookup level, xp and power-ups from fighter profile (saved) data (all fighters)
			SetCardProfiles();
		}

		// lookup level, xp and power-ups from each fighter profile (saved) data
		protected virtual void SetCardProfiles()
		{
			foreach (var card in fighterCards)
			{
				var fighterName = card.Key;
				var fighterCard = card.Value;

				var profile = Profile.GetFighterProfile(fighterName);
				if (profile != null)
				{
					fighterCard.SetProfileData(1, 0, null, null, CardFrame(fighterName),
						profile.IsLocked, profile.CanUnlock, profile.UnlockCoins, profile.UnlockOrder, profile.UnlockDefeats, profile.UnlockDifficulty);
				}
			}
		}
	

		public FighterCard GetFighterCard(string fighterName)
		{			
//			Debug.Log("GetFighterCard: " + fighterName);
			LoadFighterCards();		// in case not already loaded

			try
			{
				return fighterCards[fighterName];
			}
			catch
			{
				Debug.Log("GetFighterCard: FighterSelect FighterCard not found!! (" + fighterName + ")");
				return null;
			}
		}
			

		protected Sprite CardFrame(string fighterName)
		{
			switch (fighterName)
			{
				case "Shiro":
					return earthFireFrame;
				case "Natalya":
					return airFireFrame;
				case "Hoi Lun":
					return airWaterFrame;
				case "Leoni":
					return airWaterFrame;
				case "Danjuma":
					return earthWaterFrame;
				case "Jackson":
					return earthFireFrame;
				case "Alazne":
					return earthWaterFrame;
				case "Shiyang":
					return airFireFrame;
				case "Skeletron":
					return skeletronFrame;
				case "Ninja":
					return ninjaFrame;
				default:
					return null;
			}
		}

//		private void CombatInsertCoin()
//		{
//			if (! Store.CanAfford(1))
//				FightManager.BuyCoinsToPlay(BuyCoins);
//			else
//				FightManager.InsertCoinToPlay(ShowCombat);
//		}
			
		private void ShowCombat()
		{
			if (animatingCardEntry)
				return;
			
//			animateInsertCoin = false;

			StopCoinAnimation();
			StopInsertCoinAnimation();

			if (! Store.CanAfford(1))
				FightManager.BuyCoinsToPlay(BuyCoins);
			else
			{
				FightManager.Coins--;

//				if (! fightManager.MultiPlayerFight)							// new fight started when both fighters and location set
					fightManager.FighterSelectChoice = MenuType.WorldMap;		// triggers fade to black and new menu
			}

			if (OnFighterSelected != null)
				OnFighterSelected(previewFighter);
		}
			
//		private void ShowStore()
//		{
//			fightManager.FighterSelectChoice = MenuType.Dojo;		// triggers fade to black and new menu
//		}
//
//		private void PowerUpFighter()
//		{
//			fightManager.SelectedMenuOverlay = MenuOverlay.PowerUp;
//			fightManager.FighterSelectChoice = MenuType.Dojo;		// triggers fade to black and new menu
//		}
//
		private void BuyCoins()
		{
			FightManager.RequestPurchase();
		}
			
		public void CreatePreview(string name, string colour, bool cycleColour, bool showLocked)
		{			
			var fighterCard = GetFighterCard(name);
			if (fighterCard == null)
				return;

			if (HasActivatedOverlay)
				return;

			if (showLocked && fighterCard.IsLocked)
			{
				FightManager.ShowLockedFighter(fighterCard);
				return;
			}

			uint idleFrameNumber = 0;
			string nextColour = colour; 		// default - will change if looped

			string currentColour = FightManager.SelectedFighterColour;

			if (previewFighter != null)
			{
				bool loopColour = cycleColour && previewFighter.FighterName == name;		// cycle colours if the current fighter selected again
				idleFrameNumber = previewFighter.MovieClipFrame;

//				Debug.Log("CreatePreview: " + name + " " + currentColour + ", previewFighter.FighterName = " + previewFighter.FighterName + ", loopColour = " + loopColour);

				if (loopColour)
				{
					nextColour = FightManager.NextFighterColour(currentColour);
				}
			}

			bool fighterChanged = false;
			var newFighter = fightManager.CreateFighter(name, nextColour, false, false, false);
			if (newFighter != null)
			{
				fighterChanged = previewFighter == null || (newFighter.FighterName != previewFighter.FighterName);
				DestroyPreview();		// existing preview

				previewFighter = newFighter;
				previewFighter.transform.position = PreviewPosition();
				previewFighter.SetPreview(idleFrameNumber);

				FightManager.SelectedFighterName = previewFighter.FighterName; 
				FightManager.SelectedFighterColour = previewFighter.ColourScheme;

				if (SelectedName != null)
					SelectedName.text = previewFighter.FighterName.ToUpper(); 

				if (SelectedElements != null)
				{
					string elementsLabel = previewFighter.ElementsLabel;

					if (elementsLabel == "")
						elementsLabel = FightManager.Translate("na");		// N/A
				
					SelectedElements.text = elementsLabel;

					var element1 = previewFighter.ProfileData.Element1;
					var element2 = previewFighter.ProfileData.Element2;

					if (element1 == FighterElement.Water)
					{
						if (element2 == FighterElement.Air)
							ElementsPanel.sprite = WaterAir;
						else if (element2 == FighterElement.Earth)
							ElementsPanel.sprite = WaterEarth;
					}
					else if (element1 == FighterElement.Fire)
					{
						if (element2 == FighterElement.Air)
							ElementsPanel.sprite = FireAir;
						else if (element2 == FighterElement.Earth)
							ElementsPanel.sprite = FireEarth;
					}
				}

				if (SelectedClass != null)
				{
					var fighterClass = previewFighter.ClassLabel;

					if (fighterClass == "")
						fighterClass = FightManager.Translate("na");		// N/A

//					if (previewFighter.ProfileData.FighterClass != FighterClass.Undefined)
//						fighterClass = FightManager.Translate(previewFighter.ProfileData.FighterClass.ToString().ToLower());
//					else
//						fighterClass = FightManager.Translate("na");		// N/A
					
					SelectedClass.text = FightManager.Translate(fighterClass.ToLower()); 
				}

				if (fighterChanged)
				{
					if (fightButton != null)
						fightButton.interactable = !fighterCard.IsLocked;

//					if (powerUpButton != null)
//						powerUpButton.interactable = !fighterCard.IsLocked;
				}

				if (OnPreviewCreated != null)
					OnPreviewCreated(newFighter, fighterChanged);

				if (PreviewAudio != null)
					AudioSource.PlayClipAtPoint(PreviewAudio, Vector3.zero, FightManager.SFXVolume);
			}

//			Debug.Log("CreatePreview: HasActivatedOverlay = " + HasActivatedOverlay);

			RevealFighter();
			HilightPreviewButton();
		}

		public Vector3 PreviewPosition(float xOffset = 0)
		{
			//TODO: test preview fighter position
			var cameraX = fightManager.CameraPosition.x;
			return new Vector3(cameraX - (Math.Abs(xOffset) > 0 ? xOffset : previewX), previewY, previewZ);
		}

		public void HideFighter()
		{
			if (previewFighter != null)
				previewFighter.Hide();
		}

		public void RevealFighter()
		{
			if (previewFighter != null)
				previewFighter.Reveal();
		}
			
		public void DestroyPreview()
		{
			if (previewFighter != null)
				fightManager.DestroyFighter(previewFighter);
		}

		private Button PreviewButton
		{
			get
			{
				if (previewFighter == null)
					return null;

				return FighterButton(previewFighter.FighterName);
			}
		}

		public Button FighterButton(string fighterName)
		{
			switch (fighterName)
			{
				case "Leoni":
					return leoniButton;
				case "Shiro":
					return shiroButton;
				case "Danjuma":
					return danjumaButton;
				case "Natalya":
					return natalyaButton;
				case "Hoi Lun":
					return hoiLunButton;
				case "Jackson":
					return jacksonButton;
				case "Alazne":
					return alazneButton;
				case "Shiyang":
					return shiyangButton;
				case "Skeletron":
					return skeletronButton;
				case "Ninja":
					return ninjaButton;
				default:
					return null;
			}
		}

		private void LockChanged(Fighter fighter, bool isLocked)
		{

		}
			

		private IEnumerator AnimateCardEntry()
		{
			animator = GetComponent<Animator>();
			if (animator == null)
				yield break;
			
			animatingCardEntry = true;

//			yield return new WaitForSeconds(animatePauseTime);

			animator.enabled = true;
			animator.SetTrigger("SelectEntry");
			yield return null;
		}

		public void EntryComplete()
		{
			animatingCardEntry = false;
			animatedEntry = true;

			AnimateCoin();
			CycleInsertCoinText();
		}

		private void AnimateCoin()
		{
			if (Coin == null)
				return;
			
			StopCoinAnimation();

			coinCoroutine = InsertCoin();
			StartCoroutine(coinCoroutine);
		}

		private void StopCoinAnimation()
		{
			if (coinCoroutine != null)
				StopCoroutine(coinCoroutine);
		}


		private void StopInsertCoinAnimation()
		{
			if (insertCoinTopCoroutine != null)
				StopCoroutine(insertCoinTopCoroutine);

			if (insertCoinBottomCoroutine != null)
				StopCoroutine(insertCoinBottomCoroutine);
		}

		protected IEnumerator InsertCoin()
		{
			float t = 0;

			var targetPosition = fightButton.transform.localPosition;
			var startPosition = new Vector3(targetPosition.x + insertCoinXOffset, targetPosition.y, targetPosition.z);

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / insertCoinTime); 
				Coin.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
				yield return null;
			}

			fightManager.CoinAudio();

			yield return StartCoroutine(InsertCoin());		// keep repeating
		}


		private void CycleInsertCoinText()
		{
			if (InsertCoinTextPanel == null)
				return;
			
			InsertCoinTextPanel.gameObject.SetActive(true);

			StopInsertCoinAnimation();

			insertCoinTopCoroutine = InsertCoinText(InsertCoinTop);
			StartCoroutine(insertCoinTopCoroutine);

			insertCoinBottomCoroutine = InsertCoinText(InsertCoinBottom);
			StartCoroutine(insertCoinBottomCoroutine);
		}

		protected IEnumerator InsertCoinText(List<Text> coinTextList)
		{
			while (true)			// loop until coroutine stopped externally
			{
				foreach (var coinText in coinTextList)
				{
					StartCoroutine(AnimateCoinText(coinText));
				}

				yield return new WaitForSeconds(insertCoinTextTime);
			}
		}

		protected IEnumerator AnimateCoinText(Text coinText)
		{
			float t = 0;

			var startPosition = coinText.transform.localPosition;
			var targetPosition = new Vector3(startPosition.x - insertCoinTextWidth, startPosition.y, startPosition.z);

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / insertCoinTextTime); 

				coinText.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
				yield return null;
			}

			// loop back to start position if beyond return point
			var currentPosition = coinText.transform.localPosition;

			if (currentPosition.x <= -(insertCoinTextWidth * 2))
				coinText.transform.localPosition = new Vector3(currentPosition.x + (insertCoinTextWidth * insertCoinTextRepeats), currentPosition.y, currentPosition.z);
		}


		private void SaveInsertCoinTextPositions()
		{
			if (InsertCoinTextPanel == null)
				return;
			
			insertCoinTopPosition = new List<float>();
			insertCoinBottomPosition = new List<float>();

			for (int i = 0; i < insertCoinTextRepeats; i++)
			{
				insertCoinTopPosition.Add(InsertCoinTop[i].transform.localPosition.x);
				insertCoinBottomPosition.Add(InsertCoinBottom[i].transform.localPosition.x);
			}
		}

		private void RestoreInsertCoinTextPositions()
		{
			if (InsertCoinTextPanel == null)
				return;

			for (int i = 0; i < insertCoinTextRepeats; i++)
			{
				var topPosition = InsertCoinTop[i].transform.localPosition;
				InsertCoinTop[i].transform.localPosition = new Vector3(insertCoinTopPosition[i], topPosition.y, topPosition.z);

				var bottomPosition = InsertCoinBottom[i].transform.localPosition;
				InsertCoinBottom[i].transform.localPosition = new Vector3(insertCoinBottomPosition[i], bottomPosition.y, bottomPosition.z);
			}
		}

		public void EnableFighterButton(string fighterName, bool enable)
		{
			var button = FighterButton(fighterName);
			if (button != null)
				button.gameObject.SetActive(enable);
		}

		private void HilightPreviewButton()
		{
			var button = PreviewButton;

			if (button == null)
				return;

			ResetHilights();
			button.image.color = hilightColour;
		}

		private void ResetHilights()
		{
			shiroButton.image.color = Color.white;
			natalyaButton.image.color = Color.white;
			hoiLunButton.image.color = Color.white;
			leoniButton.image.color = Color.white;
			danjumaButton.image.color = Color.white;
			jacksonButton.image.color = Color.white;
			alazneButton.image.color = Color.white;
			shiyangButton.image.color = Color.white;

			if (skeletronButton != null)
				skeletronButton.image.color = Color.white;
			if (ninjaButton != null)
				ninjaButton.image.color = Color.white;
		}


		// OnApplicationPause(false) is called for a fresh launch and when resumed from background
		public void OnApplicationPause(bool paused)
		{
			// freeze preview fighter if going to background
			if (paused)
			{
				if (previewFighter != null)
					previewFighter.Freeze();
			}
			else
			{
				if (previewFighter != null)
					previewFighter.Unfreeze();
			}
		}
	}
}
