﻿
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Purchasing;


namespace FightingLegends
{
	public class Store : MenuCanvas
	{
		private FightManager fightManager;
		private SurvivalSelect fighterSelect;		// includes level, xp, power-ups etc

		private bool internetReachable = false;

		public Text titleText;
	
		public Button BuyCoinsButton;			// shows overlay
		public Button ResetLevelButton;			// shows overlay (confirmation) 
		public Button LevelUpButton;			// shows overlay (spend coins)
		public Button PowerUpButton;			// shows overlay
		public Button ChallengesButton;			// TeamSelect (player-created challenges)
		public Button FriendsButton;			// Facebook
		public Button LeaderboardsButton;
		public Button TrainingButton;

		public Button NewUserButton;

		public GameObject DojoButtons;			// hidden when power-up overlay showing
		public Image ButtonBackground;

		public Text BuyCoinsLabel;	
		public Text ResetLevelLabel;
		public Text LevelUpLabel;
		public Text PowerUpLabel;
		public Text FriendsLabel;
		public Text ChallengesLabel;
		public Text LeaderboardsLabel;
		public Text TrainingLabel;
		public Text NewUserLabel;

		public Text UserId;

		public Image PurchaseOverlay;			// overlay panel
		public Image SpendOverlay;				// overlay panel
		public Image PowerUpOverlay;			// overlay panel
		public PowerUpController PowerUpController;		// entry / exit animation events

		public Button CancelSpendButton;		// hides overlay
		public Button CancelPurchaseButton;		// hides overlay
		public Text CancelSpendLabel;
		public Text CancelPurchaseLabel;
		public Text TapToConfirmLabel;

		// buttons for coin purchase overlay
		public Text buyTitleText;
		public Button Buy100Button;
		public Button Buy1000Button;
		public Button Buy10000Button;
		public Text BuyFeedback;

		// coin spend overlay
		public Text spendTitleText;
		public Button SpendCoinsButton;
		public Text CoinsToSpend; 

		private int CoinsWaitingSpendConfirm = 0;

		// buttons for power-up overlay
		public Text powerUpTitleText;

		public PowerUpButton ArmourPiercing;		// set in Inspector
		public PowerUpButton Avenger;
		public PowerUpButton Ignite;
		public PowerUpButton HealthBooster;
		public PowerUpButton PoiseMaster;
		public PowerUpButton PoiseWrecker;
		public PowerUpButton PowerAttack;
		public PowerUpButton Regenerator;
		public PowerUpButton SecondLife;
		public PowerUpButton VengeanceBooster;

		// button text (translated)
		public Text ArmourPiercingLabel;			// set in Inspector
		public Text AvengerLabel;
		public Text IgniteLabel;
		public Text HealthBoosterLabel;
		public Text PoiseMasterLabel;
		public Text PoiseWreckerLabel;
		public Text PowerAttackLabel;
		public Text RegeneratorLabel;
		public Text SecondLifeLabel;
		public Text VengeanceBoosterLabel;

		public Text StaticPowerUpHeading;
		public Text TriggerPowerUpHeading;

		// sprites for power-up overlay
		public Image ArmourPiercingImage;			// set in Inspector
		public Image AvengerImage;
		public Image IgniteImage;
		public Image HealthBoosterImage;
		public Image PoiseMasterImage;
		public Image PoiseWreckerImage;
		public Image PowerAttackImage;
		public Image RegeneratorImage;
		public Image SecondLifeImage;
		public Image VengeanceBoosterImage;

		// coin value of each power-up
		private const int ArmourPiercingCoins = 20;
		private const int AvengerCoins = 30;
		private const int PoiseMasterCoins = 40;
		private const int PoiseWreckerCoins = 50;
		private const int RegeneratorCoins = 70;
		private const int VengeanceBoosterCoins = 60;
		private const int IgniteCoins = 70;
		private const int HealthBoosterCoins = 80;
		private const int PowerAttackCoins = 90;
		private const int SecondLifeCoins = 100;

		public const int LevelCoins = 10;					// factored by level
		private const float levelUpCoinsPerXP = 0.5f;

		private const float levelCoinFactor = 0.1f;

		private const float SimpleCoinFactor = 0.25f;
		private const float EasyCoinFactor = 0.5f;
		private const float MediumCoinFactor = 1.0f;
		private const float HardCoinFactor = 2.0f;
		private const float BrutalCoinFactor = 4.0f;

		public Button StaticPowerUpButton;		// activates power-up overlay
		public Button TriggerPowerUpButton;		// activates power-up overlay

		public Text PowerUpQty;					// for selected power-up
		public Text PowerUpCoolDown;			// for selected power-up

		// details of selected power up
		[HideInInspector]
		public PowerUpButton SelectedPowerUpButton;
		public Text powerUpName;
		public Text powerUpDescription;
		public Text powerUpCost;		// formatted
		public Image powerUpCoins;		// panel
		public Image SwipeUpImage;		// for trigger power-ups

		private Fighter PowerUpFighter = null;		// selected fighter
		private List<string> fighterNames = null;	// for preview
		private int selectedFighterIndex = 0;		// for preview

		public Text FighterName; 
		public Image NamePanel;

		public Text FighterHealth; 
		public Image HealthPanel;

		public Text FighterAttackRating; 
		public Image AttackRatingPanel;

		public Sprite WaterEarth;					// selected fighter element
		public Sprite FireEarth;					// selected fighter element
		public Sprite WaterAir;						// selected fighter element
		public Sprite FireAir;						// selected fighter element

		public Button EquippedStatic;				// to dequip fighter
		public Button EquippedTrigger;				// to dequip fighter
		public AudioClip EquipSound;
		public AudioClip DequipSound;

		public Image StaticPowerUp;					// as assigned to selected fighter (on power-up overlay)
		public Image TriggerPowerUp;				// as assigned to selected fighter (on power-up overlay)
		public Image StaticHilight;					// when a static powerup is selected
		public Image TriggerHilight;				// when a trigger powerup is selected
		public ParticleSystem StaticPowerUpStars;	// when equipped / dequipped
		public ParticleSystem TriggerPowerUpStars;	// when equipped / dequipped

		private List<InventoryPowerUp> PowerUpInventory { get { return FightManager.SavedGameStatus.PowerUpInventory; } }

		private static List<PowerUp> StaticPowerUps = new List<PowerUp>();
		private static List<PowerUp> TriggerPowerUps = new List<PowerUp>();

		private const float swipePause = 1.0f;			// before swipe forward / back feedback shown
		private const float feedbackOffsetX = -260;		// swipe forward / back to switch fighter
		private const float feedbackOffsetY = -135; 	
		private const string feedbackLayer = "Curtain";	// so curtain camera picks it up

		private IEnumerator swipeBackForwardCoroutine;

		private Animator animator;


		private void Awake()
		{
			Init();
		}

		private void Init()
		{
			if (titleText != null)
				titleText.text = FightManager.Translate("dojo");

			if (powerUpTitleText != null)
				powerUpTitleText.text = FightManager.Translate("powerUps");

			if (spendTitleText != null)
				spendTitleText.text = FightManager.Translate("spendCoins");

			if (buyTitleText != null)
				buyTitleText.text = FightManager.Translate("buyCoins");

			StaticPowerUpHeading.text = FightManager.Translate("constantlyActive");
			TriggerPowerUpHeading.text = FightManager.Translate("swipeUpToActivate");

			ArmourPiercingLabel.text = FightManager.Translate("armourPiercing");
			AvengerLabel.text = FightManager.Translate("avenger");
			IgniteLabel.text = FightManager.Translate("ignite");
			HealthBoosterLabel.text = FightManager.Translate("healthBooster");
			PoiseMasterLabel.text = FightManager.Translate("poiseMaster");
			PoiseWreckerLabel.text = FightManager.Translate("poiseWrecker");
			PowerAttackLabel.text = FightManager.Translate("powerAttack");
			RegeneratorLabel.text = FightManager.Translate("regenerator");
			SecondLifeLabel.text = FightManager.Translate("secondLife");
			VengeanceBoosterLabel.text = FightManager.Translate("vengeanceBooster");
			
			LevelUpLabel.text = FightManager.Translate("levelUp", false, true);
			ResetLevelLabel.text = FightManager.Translate("resetLevel", true);
			PowerUpLabel.text = FightManager.Translate("powerUp", false, true);
			TrainingLabel.text = FightManager.Translate("train");
			LeaderboardsLabel.text = FightManager.Translate("leaderBoards", true);
			FriendsLabel.text = FightManager.Translate("friends");
			ChallengesLabel.text = FightManager.Translate("challengeArena", true);
			BuyCoinsLabel.text = FightManager.Translate("buy");
			NewUserLabel.text = FightManager.Translate("register");

			CancelSpendLabel.text = FightManager.Translate("cancel");
			CancelPurchaseLabel.text = FightManager.Translate("cancel");
			TapToConfirmLabel.text = FightManager.Translate("tapToConfirm");

//			UserId.text = FightManager.SavedGameStatus.UserId;
			
			if (fighterSelect == null)
			{
				fighterSelect = GetComponent<SurvivalSelect>();
				fighterSelect.ParentCanvas = this;
				fighterSelect.Init();		// load fighter cards and start listening
			}

			if (fightManager == null)
			{
				var fightManagerObject = GameObject.Find("FightManager");
				fightManager = fightManagerObject.GetComponent<FightManager>();
			}

			InitFighterNames();

			animator = GetComponent<Animator>();
		}

		private void Start()
		{
			AddListeners();
		}

		public void OnDestroy()
		{
			RemoveListeners();
		}

		private void OnEnable()
		{	
			internetReachable = (Application.internetReachability != NetworkReachability.NotReachable);

			//			UserId.text = "[ " + FightManager.Translate("playerId") + ": " + FightManager.SavedGameStatus.UserId + " ]";
			UserId.text = FightManager.SavedGameStatus.UserId;

			SetPreviewFighter();
			RefreshInventory();		// for all power-ups

			if (ActivatedOverlayCount == 0)
				StartSwipeFeedback();

//			ChallengesButton.gameObject.SetActive(internetReachable);
			FriendsButton.interactable = internetReachable;
			LeaderboardsButton.interactable = internetReachable;

			BuyCoinsButton.interactable = internetReachable;

			// hide if already registered!
			NewUserButton.gameObject.SetActive(false); // string.IsNullOrEmpty(FightManager.SavedGameStatus.UserId));

			animator.SetTrigger("StoreEntry");
		}

		private void OnDisable()
		{
			StopSwipeFeedback();
		}

		private void SetPreviewFighter()
		{
			CreatePreview(FightManager.SelectedFighterName);
			SetSelectedIndex(FightManager.SelectedFighterName);

			if (fighterSelect.previewFighter != null)
			{
				UpdateFighterCard(fighterSelect.previewFighter, true);

				// can only reset level when fighter is at level 100!
//				ResetLevelButton.gameObject.SetActive(fighterSelect.previewFighter.Level == Fighter.maxLevel);
				ResetLevelButton.interactable = (fighterSelect.previewFighter.Level == Fighter.maxLevel);
			}
		}

		private void CreatePreview(string fighterName)
		{
			fighterSelect.CreatePreview(fighterName, FightManager.SelectedFighterColour, false, false); // don't show FighterUnlock
			if (fighterSelect.previewFighter != null)
				SetFighterElements(fighterSelect.previewFighter);
		}

		public void DestroyPreviewFighter()
		{
			fighterSelect.DestroyPreview();
		}
			
		private void AddListeners()
		{
			FightManager.OnThemeChanged += SetTheme;

			OnOverlayRevealed += OverlayRevealed;
			OnOverlayHiding += OverlayHiding;
			OnOverlayHidden += OverlayHidden;

			FighterSelect.OnPreviewCreated += UpdateFighterCard;

			GestureListener.OnSwipeLeft += PreviewPreviousFighter;
			GestureListener.OnSwipeRight += PreviewNextFighter;

			FightManager.OnPowerUpInventoryChanged += PowerUpInventoryChanged;
			FightManager.OnFeedbackStateEnd += FeedbackEnd;

			TrainingButton.onClick.AddListener(delegate { Train(); });
			BuyCoinsButton.onClick.AddListener(delegate { BuyCoins(); });
			ResetLevelButton.onClick.AddListener(delegate { ConfirmResetLevel(); });
			LevelUpButton.onClick.AddListener(delegate { ConfirmLevelUp(); });
			FriendsButton.onClick.AddListener(delegate { Facebook(); });
//			ChallengesButton.onClick.AddListener(delegate { Challenges(); });
			LeaderboardsButton.onClick.AddListener(delegate { Leaderboards(); });
			PowerUpButton.onClick.AddListener(delegate { ShowPowerUpOverlay(fighterSelect.previewFighter, PowerUp.None, true); });
			StaticPowerUpButton.onClick.AddListener(delegate { ShowPowerUpOverlay(fighterSelect.previewFighter, fighterSelect.previewFighter.ProfileData.SavedData.StaticPowerUp, true); });
			TriggerPowerUpButton.onClick.AddListener(delegate { ShowPowerUpOverlay(fighterSelect.previewFighter, fighterSelect.previewFighter.ProfileData.SavedData.TriggerPowerUp, false); });
			CancelSpendButton.onClick.AddListener(delegate { CancelSpend(); });
			CancelPurchaseButton.onClick.AddListener(delegate { CancelPurchase(); });

			NewUserButton.onClick.AddListener(delegate { RegisterNewUser(); });
			SpendCoinsButton.onClick.AddListener(delegate { SpendCoins(); });

			PowerUpController.OnPowerUpEntry += OnPowerUpEntered;
			PowerUpController.OnPowerUpEntryComplete += OnPowerUpEntryComplete;
			PowerUpController.OnPowerUpExitComplete += OnPowerUpExitComplete;

			EquippedStatic.onClick.AddListener(delegate { OnStaticPowerUpClicked(); });
			EquippedTrigger.onClick.AddListener(delegate { OnTriggerPowerUpClicked(); });

			ArmourPiercing.GetComponent<Button>().onClick.AddListener(delegate { SelectPowerUp(ArmourPiercing); });
			Avenger.GetComponent<Button>().onClick.AddListener(delegate { SelectPowerUp(Avenger); });
			Ignite.GetComponent<Button>().onClick.AddListener(delegate { SelectPowerUp(Ignite); });
			HealthBooster.GetComponent<Button>().onClick.AddListener(delegate { SelectPowerUp(HealthBooster); });
			PoiseMaster.GetComponent<Button>().onClick.AddListener(delegate { SelectPowerUp(PoiseMaster); });
			PoiseWrecker.GetComponent<Button>().onClick.AddListener(delegate { SelectPowerUp(PoiseWrecker); });
			PowerAttack.GetComponent<Button>().onClick.AddListener(delegate { SelectPowerUp(PowerAttack); });
			Regenerator.GetComponent<Button>().onClick.AddListener(delegate { SelectPowerUp(Regenerator); });
			SecondLife.GetComponent<Button>().onClick.AddListener(delegate { SelectPowerUp(SecondLife); });
			VengeanceBooster.GetComponent<Button>().onClick.AddListener(delegate { SelectPowerUp(VengeanceBooster); });
		}
			
		private void RemoveListeners()
		{
			FightManager.OnThemeChanged -= SetTheme;

			FightManager.OnPowerUpInventoryChanged -= PowerUpInventoryChanged;
			FightManager.OnFeedbackStateEnd -= FeedbackEnd;

			OnOverlayRevealed -= OverlayRevealed;
			OnOverlayHiding -= OverlayHiding;
			OnOverlayHidden -= OverlayHidden;

			FighterSelect.OnPreviewCreated -= UpdateFighterCard;

			GestureListener.OnSwipeLeft -= PreviewPreviousFighter;	
			GestureListener.OnSwipeRight -= PreviewNextFighter;	

			BuyCoinsButton.onClick.RemoveListener(delegate { BuyCoins(); });
			ResetLevelButton.onClick.RemoveListener(delegate { ConfirmResetLevel(); });
			LevelUpButton.onClick.RemoveListener(delegate { ConfirmLevelUp(); });
			FriendsButton.onClick.RemoveListener(delegate { Facebook(); });
//			ChallengesButton.onClick.RemoveListener(delegate { Challenges(); });
			LeaderboardsButton.onClick.RemoveListener(delegate { Leaderboards(); });
			PowerUpButton.onClick.RemoveListener(delegate { ShowPowerUpOverlay(fighterSelect.previewFighter, PowerUp.None, true); });
			StaticPowerUpButton.onClick.RemoveListener(delegate { ShowPowerUpOverlay(fighterSelect.previewFighter, fighterSelect.previewFighter.ProfileData.SavedData.StaticPowerUp, true); });
			TriggerPowerUpButton.onClick.RemoveListener(delegate { ShowPowerUpOverlay(fighterSelect.previewFighter, fighterSelect.previewFighter.ProfileData.SavedData.TriggerPowerUp, true); });
			CancelSpendButton.onClick.RemoveListener(delegate { CancelSpend(); });
			CancelPurchaseButton.onClick.RemoveListener(delegate { CancelPurchase(); });

			NewUserButton.onClick.RemoveListener(delegate { RegisterNewUser(); });

			SpendCoinsButton.onClick.RemoveListener(delegate { SpendCoins(); });

			PowerUpController.OnPowerUpEntry -= OnPowerUpEntered;
			PowerUpController.OnPowerUpEntryComplete -= OnPowerUpEntryComplete;
			PowerUpController.OnPowerUpExitComplete -= OnPowerUpExitComplete;

			EquippedStatic.onClick.RemoveListener(delegate { OnStaticPowerUpClicked(); });
			EquippedTrigger.onClick.RemoveListener(delegate { OnTriggerPowerUpClicked(); });

			ArmourPiercing.GetComponent<Button>().onClick.RemoveListener(delegate { SelectPowerUp(ArmourPiercing); });
			Avenger.GetComponent<Button>().onClick.RemoveListener(delegate { SelectPowerUp(Avenger); });
			Ignite.GetComponent<Button>().onClick.RemoveListener(delegate { SelectPowerUp(Ignite); });
			HealthBooster.GetComponent<Button>().onClick.RemoveListener(delegate { SelectPowerUp(HealthBooster); });
			PoiseMaster.GetComponent<Button>().onClick.RemoveListener(delegate { SelectPowerUp(PoiseMaster); });
			PoiseWrecker.GetComponent<Button>().onClick.RemoveListener(delegate { SelectPowerUp(PoiseWrecker); });
			PowerAttack.GetComponent<Button>().onClick.RemoveListener(delegate { SelectPowerUp(PowerAttack); });
			Regenerator.GetComponent<Button>().onClick.RemoveListener(delegate { SelectPowerUp(Regenerator); });
			SecondLife.GetComponent<Button>().onClick.RemoveListener(delegate { SelectPowerUp(SecondLife); });
			VengeanceBooster.GetComponent<Button>().onClick.RemoveListener(delegate { SelectPowerUp(VengeanceBooster); });
		}

		// list for looping through fighters by name
		private void InitFighterNames()
		{
			if (fighterNames == null)
			{
				fighterNames = new List<string>();

				fighterNames.Add("Leoni");
				fighterNames.Add("Shiro");
				fighterNames.Add("Danjuma");
				fighterNames.Add("Natalya");
				fighterNames.Add("Hoi Lun");
				fighterNames.Add("Jackson");
				fighterNames.Add("Alazne");
				fighterNames.Add("Shiyang");

				fighterNames.Add("Ninja");			// TODO: remove!  ?
				fighterNames.Add("Skeletron");		// TODO: remove!  ?

				selectedFighterIndex = 0;
			}
		}

		private string PreviousFighterName
		{
			get
			{
				if (selectedFighterIndex == 0)
					selectedFighterIndex = fighterNames.Count - 1;
				else
					selectedFighterIndex--;

				return SelectedFighterName;
			}
		}

		private string NextFighterName
		{
			get
			{
				if (selectedFighterIndex == fighterNames.Count - 1)
					selectedFighterIndex = 0;
				else
					selectedFighterIndex++;

				return SelectedFighterName;
			}
		}
			
		private string SelectedFighterName
		{
			get { return fighterNames[selectedFighterIndex]; }
		}

		private void SetSelectedIndex(string fighterName)
		{
			for (int i = 0; i < fighterNames.Count; i++)
			{
				if (fighterNames[i] == fighterName)
				{
					selectedFighterIndex = i;
					return;
				}
			}
		}

		private void PreviewPreviousFighter()
		{
			if (! fightManager.IsMenuOnTop(MenuType.Dojo))
				return;
			
			if (HasActivatedOverlay)
				return;

			fighterSelect.EnableFighterButton(SelectedFighterName, false);
			CreatePreview(PreviousFighterName);
		}

		private void PreviewNextFighter()
		{
			if (! fightManager.IsMenuOnTop(MenuType.Dojo))
				return;

			if (HasActivatedOverlay)
				return;

			fighterSelect.EnableFighterButton(SelectedFighterName, false);
			CreatePreview(NextFighterName);
		}


		private void UpdateFighterCard(Fighter previewFighter, bool fighterChanged)
		{
			if (previewFighter == null)
				return;

			if (! fighterChanged)
				return;

			var fighterCard = fighterSelect.GetFighterCard(previewFighter.FighterName);
			if (fighterCard == null)
				return;

			previewFighter.LoadProfile();

			var fighterName = previewFighter.FighterName;
		
			fighterCard.SetProfileData(previewFighter.Level, previewFighter.XP, previewFighter.StaticPowerUp, PowerUpSprite(previewFighter.StaticPowerUp), previewFighter.TriggerPowerUp, PowerUpSprite(previewFighter.TriggerPowerUp), null,
				previewFighter.IsLocked, previewFighter.CanUnlockOrder, previewFighter.UnlockCoins, previewFighter.UnlockOrder, previewFighter.UnlockDefeats, previewFighter.UnlockDifficulty);
			fighterSelect.EnableFighterButton(fighterName, true);

			FighterName.text = fighterName.ToUpper();
			FighterHealth.text = FightManager.Translate("healthPoints") + ": " + ((int)previewFighter.ProfileData.LevelHealth).ToString();
			FighterAttackRating.text = FightManager.Translate("attackRating") + ": " + ((int)previewFighter.ProfileData.LevelAR).ToString();

			TrainingButton.interactable = !previewFighter.IsLocked && previewFighter.FighterName != "Skeletron";
			PowerUpButton.interactable = !previewFighter.IsLocked;
			LevelUpButton.interactable = !previewFighter.IsLocked;
			ResetLevelButton.interactable = !previewFighter.IsLocked;
		}
			
		private void OnStaticPowerUpClicked()
		{
			DequipPowerUp(false, true, false);
		}

		private void OnTriggerPowerUpClicked()
		{
			DequipPowerUp(true, true, false);
		}

		private void SetFighterElements(Fighter previewFighter)
		{
			var element1 = previewFighter.ProfileData.Element1;
			var element2 = previewFighter.ProfileData.Element2;

			if (element1 == FighterElement.Water)
			{
				if (element2 == FighterElement.Air)
				{
					NamePanel.sprite = WaterAir;
					HealthPanel.sprite = WaterAir;
					AttackRatingPanel.sprite = WaterAir;
				}
				else if (element2 == FighterElement.Earth)
				{
					NamePanel.sprite = WaterEarth;
					HealthPanel.sprite = WaterEarth;
					AttackRatingPanel.sprite = WaterEarth;
				}
			}
			else if (element1 == FighterElement.Fire)
			{
				if (element2 == FighterElement.Air)
				{
					NamePanel.sprite = FireAir;
					HealthPanel.sprite = FireAir;
					AttackRatingPanel.sprite = FireAir;
				}
				else if (element2 == FighterElement.Earth)
				{
					NamePanel.sprite = FireEarth;
					HealthPanel.sprite = FireEarth;
					AttackRatingPanel.sprite = FireEarth;
				}
			}
		}
			
		protected override void HideAllOverlays()
		{
			base.HideAllOverlays();
			fighterSelect.EnableFighterButton(SelectedFighterName, false);

			fighterSelect.RevealFighter();
		}

		private void Facebook()
		{
			if (FacebookManager.FacebookOk)
				fightManager.StoreChoice = MenuType.Facebook;			// triggers fade to black and new menu
		}

		private void Leaderboards()
		{
			fightManager.StoreChoice = MenuType.Leaderboards;			// triggers fade to black and new menu
		}

		private void Challenges()
		{
			fightManager.PlayerCreatedChallenges = true;
			fightManager.StoreChoice = MenuType.TeamSelect;				// triggers fade to black and new menu
		}
			
		private void Train()
		{
			FightManager.CombatMode = FightMode.Dojo;
			fightManager.CleanupFighters();
			fightManager.SelectedLocation = FightManager.dojo;
			fightManager.StoreChoice = MenuType.Combat;					// triggers fade to black and new menu
		}
			
		private int LevelUpCoins
		{
			get
			{
				var fighter = fighterSelect.previewFighter;
//				return (int)(fighter.LevelUpXP * levelUpCoinsPerXP);
				return Mathf.RoundToInt(fighter.LevelUpXP * levelUpCoinsPerXP);
			}
		}

		private void ConfirmLevelUp()
		{
			var fighter = fighterSelect.previewFighter;
			if (fighter.Level == Fighter.maxLevel)
				return;
			
			if (CanAfford(LevelUpCoins))			// confirm spend of coins
				FightManager.GetConfirmation(string.Format(FightManager.Translate("confirmLevelUp"), fighter.FighterName), LevelUpCoins, UseCoinsForLevelUp);
			else  									// offer option to purchase more coins
				FightManager.GetConfirmation(string.Format(FightManager.Translate("confirmBuyLevelUpCoins"), fighter.FighterName), LevelUpCoins, BuyCoins);
		}

		private void UseCoinsForLevelUp()
		{
			var fighter = fighterSelect.previewFighter;
			if (fighter.Level == Fighter.maxLevel)
				return;
			
			// purchase a level-up with coins
			FightManager.Coins -= LevelUpCoins;

			// increment fighter level
			fighter.Level++;
			fighter.XP = 0;
			fighter.SaveProfile();
			UpdateFighterCard(fighterSelect.previewFighter, true);
		}

		private void ConfirmResetLevel()
		{
			var fighter = fighterSelect.previewFighter;
			var resetMessage = string.Format(FightManager.Translate("confirmLevelReset"), fighter.FighterName, FightManager.KudosResetLevel * fighter.Level);
			FightManager.GetConfirmation(resetMessage, 0, ResetLevel);
		}

		private void ResetLevel()
		{
//			Debug.Log("ResetLevel");

			// reset fighter to level 1
			var fighter = fighterSelect.previewFighter;

			// kudos!
			fightManager.ResetLevelKudos(fighter.Level);

			fighter.Level = 1;
			fighter.XP = 0;
			fighter.SaveProfile();
			UpdateFighterCard(fighterSelect.previewFighter, true);
		}

		private void RegisterNewUser()
		{
			if (string.IsNullOrEmpty(FightManager.SavedGameStatus.UserId))
				FightManager.RegisterNewUser();
		}
			
		private void OverlayRevealed(Image panel, int overlayCount)
		{
			fighterSelect.HideFighter();
			StopSwipeFeedback();
			DojoButtons.SetActive(false);
			ButtonBackground.gameObject.SetActive(false);
		}

		private void OverlayHiding(Image overlay, int overlayCount)
		{
			if (overlay == PowerUpOverlay)
				PowerUpController.TriggerExitAnimation();		// event fired by animator on completion - see OnPowerUpExitComplete

			StartCoroutine(FadeOverlay(overlay));
		}
			
		private void OverlayHidden(Image overlay, int overlayCount)
		{
//			Debug.Log("OverlayHidden: overlayCount = " + overlayCount);
			if (overlayCount == 0) 		// last one hidden
			{
				if (fighterSelect.previewFighter == null)
				{
					SetPreviewFighter();
				}
				else
				{
					fighterSelect.RevealFighter();			// preview idle
					UpdateFighterCard(fighterSelect.previewFighter, true);
				}

				DojoButtons.SetActive(true);
				ButtonBackground.gameObject.SetActive(true);
				StartSwipeFeedback();
			}
				
			if (overlay == SpendOverlay)
			{
				CoinsWaitingSpendConfirm = 0;
				CoinsToSpend.text = "";
			}
		}

		private void OnPowerUpEntered()
		{
			fightManager.BlingAudio();
		}

		private void OnPowerUpEntryComplete()
		{
			
		}

		private void OnPowerUpExitComplete()
		{
			ShowPowerUpButtons(false);
//			StartCoroutine(FadeOverlay(PowerUpOverlay))
		}

		private void BuyCoins()
		{
			// offer option to buy more coins (store)
			FightManager.RequestPurchase();
		}


		private void CancelSpend()
		{
			HideActiveOverlay();
		}

		private void CancelPurchase()
		{
			HideActiveOverlay();
		}

		private void HideBuyOverlay()
		{
			fighterSelect.RevealFighter();
			StartCoroutine(HideOverlay(PurchaseOverlay));
		}

		// confirm or cancel coin spend
		public void ShowSpendOverlay()
		{
			if (CoinsWaitingSpendConfirm <= 0 || ! CanAfford(CoinsWaitingSpendConfirm))
				return;

			CoinsToSpend.text = string.Format("{0:N0}", CoinsWaitingSpendConfirm);		// thousands separator

			fighterSelect.HideFighter();
			StartCoroutine(RevealOverlay(SpendOverlay));
		}

		private void HideSpendOverlay()
		{
			fighterSelect.RevealFighter();
			StartCoroutine(HideOverlay(SpendOverlay));
		}

		public void ShowPowerUpOverlay(string fighterName, string fighterColour)
		{
			SetSelectedIndex(fighterName);
			ShowPowerUpOverlay(fighterSelect.previewFighter, PowerUp.None, true);
		}
			
		private void ShowPowerUpOverlay(Fighter fighter, PowerUp selectedPowerUp, bool isStatic)
		{
			if (fighter == null)
				return;

			fighter.LoadProfile();		// level, power-ups etc
			PowerUpFighter = fighter;

			ShowPowerUpButtons(true);

			if (selectedPowerUp != PowerUp.None)
				SelectedPowerUpButton = GetPowerUpButton(selectedPowerUp);
			else
				SelectedPowerUpButton = null;
			
			SetPowerUpSprites();		// PowerUpFighter may have power-ups already assigned

			fighterSelect.HideFighter();
			StartCoroutine(RevealOverlay(PowerUpOverlay));

			if (SelectedPowerUpButton == null || SelectedPowerUpButton.PowerUp == PowerUp.None)
			{
				if (isStatic)
					SelectPowerUp(ArmourPiercing, false);		// select first static powerup by default
				else
					SelectPowerUp(VengeanceBooster, false);		// select first trigger powerup by default
			}
			else
				SelectPowerUp(SelectedPowerUpButton, false);
		}

		private void ShowPowerUpButtons(bool show)
		{
			ArmourPiercing.gameObject.SetActive(show);
			Avenger.gameObject.SetActive(show);
			Ignite.gameObject.SetActive(show);
			HealthBooster.gameObject.SetActive(show);
			PoiseMaster.gameObject.SetActive(show);
			PoiseWrecker.gameObject.SetActive(show);
			PowerAttack.gameObject.SetActive(show);
			Regenerator.gameObject.SetActive(show);
			SecondLife.gameObject.SetActive(show);
			VengeanceBooster.gameObject.SetActive(show);

			EquippedStatic.gameObject.SetActive(show);
			EquippedTrigger.gameObject.SetActive(show);
		}

		private bool PowerUpAlreadyEquipped
		{
			get
			{
				if (PowerUpFighter == null || SelectedPowerUpButton == null)
					return false;
				
				if (SelectedPowerUpButton.IsTrigger)
					return PowerUpFighter.TriggerPowerUp == SelectedPowerUpButton.PowerUp;
				else
					return PowerUpFighter.StaticPowerUp == SelectedPowerUpButton.PowerUp;
			}
		}

		private void EquipPowerUp()
		{
			if (PowerUpFighter == null || SelectedPowerUpButton == null)
				return;

			if (PowerUpAlreadyEquipped)
				return;

			var hasInventory = fightManager.InventoryQuantity(SelectedPowerUpButton.PowerUp) > 0;

			if (hasInventory)
			{
				AssignPowerUp();

				fightManager.ReduceInventory(SelectedPowerUpButton.PowerUp, 1);
				RefreshInventory();					// for all power-ups

				SetSelectedPowerUpQty();			// new inventory qty
			}
			else  									// get confirmation to use/buy coins + preview of power-up details
			{
				var selectedPowerUp = new PowerUpDetails
				{ 
					PowerUp = SelectedPowerUpButton.PowerUp,
					Name = powerUpName.text,
					Description = powerUpDescription.text,
					IsTrigger = SelectedPowerUpButton.IsTrigger,
					Icon = PowerUpSprite(SelectedPowerUpButton.PowerUp),
					Cost = powerUpCost.text,
					CoinValue = SelectedPowerUpCoins,
					Activation = SelectedPowerUpButton.IsTrigger ? FightManager.Translate("swipeUpToActivate") : FightManager.Translate("constantlyActive"),
					Cooldown = SelectedPowerUpButton.IsTrigger ? PowerUpCoolDown.text : "",
					Confirmation = CanAffordSelectedPowerUp ? FightManager.Translate("confirmUsePowerUpCoins") : FightManager.Translate("confirmBuyPowerUpCoins")
				};

				if (CanAffordSelectedPowerUp)		// confirm spend of coins
					FightManager.GetPowerUpConfirmation(selectedPowerUp, UseCoinsForSelectedPowerUp);
				else 								// offer option to purchase more coins
					FightManager.GetPowerUpConfirmation(selectedPowerUp, BuyCoins);
			}
		}

		private void UseCoinsForSelectedPowerUp()
		{
//			FightManager.Coins -= SelectedPowerUpCoins;
			// coins deducted by AreYouSure
			AssignPowerUp();
		}

		private void AssignPowerUp()
		{
			if (SelectedPowerUpButton.IsTrigger)
			{
				DequipPowerUp(true, false, true);			// if equipped

				PowerUpFighter.TriggerPowerUp = SelectedPowerUpButton.PowerUp;
				PowerUpFighter.ProfileData.SavedData.TriggerPowerUpCoolDown = SelectedPowerUpButton.TriggerCoolDown;
				PowerUpFighter.ProfileData.SavedData.TriggerPowerUpCost = SelectedPowerUpCoins;
			}
			else
			{
				DequipPowerUp(false, false, true);		// if equipped

				PowerUpFighter.StaticPowerUp = SelectedPowerUpButton.PowerUp;
				PowerUpFighter.ProfileData.SavedData.StaticPowerUpCost = SelectedPowerUpCoins;
			}

			if (EquipSound != null)
				AudioSource.PlayClipAtPoint(EquipSound, Vector3.zero, FightManager.SFXVolume);

			PowerUpFighter.SaveProfile();
			SetPowerUpSprites();
		}
			
		private void DequipPowerUp(bool isTrigger, bool saveProfile, bool silent)
		{
			if (PowerUpFighter == null) 
				return;

			if (isTrigger)
			{
				if (PowerUpFighter.TriggerPowerUp == PowerUp.None)
					return;
				
				// return to inventory
				fightManager.IncreaseInventory(PowerUpFighter.TriggerPowerUp, 1);

				PowerUpFighter.TriggerPowerUp = PowerUp.None;
				PowerUpFighter.ProfileData.SavedData.TriggerPowerUpCoolDown = 0;
				PowerUpFighter.ProfileData.SavedData.TriggerPowerUpCost = 0;
			}
			else
			{
				if (PowerUpFighter.StaticPowerUp == PowerUp.None)
					return;
				
				// return to inventory
				fightManager.IncreaseInventory(PowerUpFighter.StaticPowerUp, 1);

				PowerUpFighter.StaticPowerUp = PowerUp.None;
				PowerUpFighter.ProfileData.SavedData.StaticPowerUpCost = 0;
			}

			if (!silent && DequipSound != null)
				AudioSource.PlayClipAtPoint(DequipSound, Vector3.zero, FightManager.SFXVolume);
				
			RefreshInventory();					// for all power-ups
			SetSelectedPowerUpQty();			// new inventory qty
			SetPowerUpSprites();

			if (saveProfile)
				PowerUpFighter.SaveProfile();
		}


		private void SetPowerUpSprites()
		{
			if (PowerUpFighter == null)
				return;

//			Debug.Log("SetPowerUpSprites: " + PowerUpFighter.FullName + ", trigger = " + PowerUpFighter.TriggerPowerUp + ", static = " + PowerUpFighter.StaticPowerUp);

			var triggerSprite = PowerUpSprite(PowerUpFighter.TriggerPowerUp);
			bool triggerChanged = TriggerPowerUp.sprite != triggerSprite;

			if (triggerChanged)
			{
				TriggerPowerUp.sprite = triggerSprite;
				TriggerPowerUp.color = TriggerPowerUp.sprite != null ? Color.white : Color.black;
				
				TriggerPowerUpStars.Play();
			}
				
			var staticSprite = PowerUpSprite(PowerUpFighter.StaticPowerUp);
			bool staticChanged = StaticPowerUp.sprite != staticSprite;

			if (staticChanged)
			{
				StaticPowerUp.sprite = staticSprite;
				StaticPowerUp.color = StaticPowerUp.sprite != null ? Color.white : Color.black;

				StaticPowerUpStars.Play();
			}

			// update fighter card to sync power-up sprites
			// (slightly clumsy duplication of power-up sprites)
			if (triggerChanged || staticChanged)
			{
				var fighterCard = fighterSelect.GetFighterCard(PowerUpFighter.FighterName);
				if (fighterCard != null)
					fighterCard.SetPowerUpSprites(PowerUpSprite(PowerUpFighter.StaticPowerUp), PowerUpSprite(PowerUpFighter.TriggerPowerUp));
			}
		}

		private void RefreshInventory()
		{
//			Debug.Log("UpdateInventory");

			ArmourPiercing.InventoryQuantity.text = "x " + fightManager.InventoryQuantity(PowerUp.ArmourPiercing).ToString();
			Avenger.InventoryQuantity.text = "x " + fightManager.InventoryQuantity(PowerUp.Avenger).ToString();
			Ignite.InventoryQuantity.text = "x " + fightManager.InventoryQuantity(PowerUp.Ignite).ToString();
			HealthBooster.InventoryQuantity.text = "x " + fightManager.InventoryQuantity(PowerUp.HealthBooster).ToString();
			PoiseMaster.InventoryQuantity.text = "x " + fightManager.InventoryQuantity(PowerUp.PoiseMaster).ToString();
			PoiseWrecker.InventoryQuantity.text = "x " + fightManager.InventoryQuantity(PowerUp.PoiseWrecker).ToString();
			PowerAttack.InventoryQuantity.text = "x " + fightManager.InventoryQuantity(PowerUp.PowerAttack).ToString();
			Regenerator.InventoryQuantity.text = "x " + fightManager.InventoryQuantity(PowerUp.Regenerator).ToString();
			SecondLife.InventoryQuantity.text = "x " + fightManager.InventoryQuantity(PowerUp.SecondLife).ToString();
			VengeanceBooster.InventoryQuantity.text = "x " + fightManager.InventoryQuantity(PowerUp.VengeanceBooster).ToString();
		}
			
		private void PowerUpInventoryChanged(PowerUp powerUp, int quantity)
		{
			switch (powerUp)
			{
				case PowerUp.ArmourPiercing:
					ArmourPiercing.InventoryQuantity.text = "x " + quantity.ToString();
					break;

				case PowerUp.Avenger:
					Avenger.InventoryQuantity.text = "x " + quantity.ToString();
					break;

				case PowerUp.Ignite:
					Ignite.InventoryQuantity.text = "x " + quantity.ToString();
					break;

				case PowerUp.HealthBooster:
					HealthBooster.InventoryQuantity.text = "x " + quantity.ToString();
					break;

				case PowerUp.PoiseMaster:
					PoiseMaster.InventoryQuantity.text = "x " + quantity.ToString();
					break;

				case PowerUp.PoiseWrecker:
					PoiseWrecker.InventoryQuantity.text = "x " + quantity.ToString();
					break;

				case PowerUp.PowerAttack:
					PowerAttack.InventoryQuantity.text = "x " + quantity.ToString();
					break;

				case PowerUp.Regenerator:
					Regenerator.InventoryQuantity.text = "x " + quantity.ToString();
					break;

				case PowerUp.SecondLife:
					SecondLife.InventoryQuantity.text = "x " + quantity.ToString();
					break;

				case PowerUp.VengeanceBooster:
					VengeanceBooster.InventoryQuantity.text = "x " + quantity.ToString();
					break;

				case PowerUp.None:
				default:
					break;
			}
		}
			
		public static PowerUp RandomStaticPowerUp
		{
			get
			{
				if (StaticPowerUps.Count == 0)
				{
					StaticPowerUps.Add(PowerUp.ArmourPiercing);
					StaticPowerUps.Add(PowerUp.Avenger);
					StaticPowerUps.Add(PowerUp.PoiseMaster);
					StaticPowerUps.Add(PowerUp.PoiseWrecker);
					StaticPowerUps.Add(PowerUp.Regenerator);
					StaticPowerUps.Add(PowerUp.None);
				}

				return StaticPowerUps [ UnityEngine.Random.Range(0, StaticPowerUps.Count-1) ];
			}
		}

		public static PowerUp RandomTriggerPowerUp
		{
			get
			{
				if (TriggerPowerUps.Count == 0)
				{
					TriggerPowerUps.Add(PowerUp.VengeanceBooster);
					TriggerPowerUps.Add(PowerUp.Ignite);
					TriggerPowerUps.Add(PowerUp.HealthBooster);
					TriggerPowerUps.Add(PowerUp.PowerAttack);
					TriggerPowerUps.Add(PowerUp.SecondLife);
					TriggerPowerUps.Add(PowerUp.None);
				}

				return TriggerPowerUps [ UnityEngine.Random.Range(0, TriggerPowerUps.Count-1) ];
			}
		}
			

		private IEnumerator SwipeForwardFeedback(bool pause)
		{
			if (pause)
				yield return new WaitForSeconds(swipePause);
			
			fightManager.TriggerFeedbackFX(FeedbackFXType.Swipe_Forward, feedbackOffsetX, feedbackOffsetY, feedbackLayer);
			yield return null;
		}

		private IEnumerator SwipeBackFeedback(bool pause)
		{
			if (pause)
				yield return new WaitForSeconds(swipePause);
			
			fightManager.TriggerFeedbackFX(FeedbackFXType.Swipe_Back, feedbackOffsetX, feedbackOffsetY, feedbackLayer);
			yield return null;
		}

		private void StartSwipeFeedback()
		{
			if (ActivatedOverlayCount > 0)
				return;
			
			swipeBackForwardCoroutine = SwipeForwardFeedback(true);
			StartCoroutine(swipeBackForwardCoroutine);
		}

		private void StopSwipeFeedback()
		{
			if (swipeBackForwardCoroutine != null)
				StopCoroutine(swipeBackForwardCoroutine);
			
			fightManager.CancelFeedbackFX();
		}

		private void FeedbackEnd(AnimationState endingState)
		{
			if (!gameObject.activeSelf || ActivatedOverlayCount > 0)
				return;
			
			if (endingState.StateLabel == FeedbackFXType.Swipe_Forward.ToString().ToUpper())
			{
				swipeBackForwardCoroutine = SwipeBackFeedback(true);
				StartCoroutine(swipeBackForwardCoroutine);
			}
			else if (endingState.StateLabel == FeedbackFXType.Swipe_Back.ToString().ToUpper())
			{
				swipeBackForwardCoroutine = SwipeForwardFeedback(true);
				StartCoroutine(swipeBackForwardCoroutine);
			}
		}

			
		private void SelectPowerUp(PowerUpButton powerUp, bool equip = true)
		{
//			Debug.Log("SelectPowerUp: " + powerUp);

			SelectedPowerUpButton = powerUp;
			HilightSelectedPowerUp();

			if (equip)
				EquipPowerUp();
			else
				SetSelectedPowerUpQty();
		}

		private void SetSelectedPowerUpQty()
		{
			PowerUpQty.text = "x " + fightManager.InventoryQuantity(SelectedPowerUpButton.PowerUp).ToString();
			SetPowerUpQty(SelectedPowerUpButton.PowerUp);
		}

		private void SetPowerUpQty(PowerUp powerUp)
		{
			PowerUpQty.text = "x " + fightManager.InventoryQuantity(powerUp).ToString();
		}

		private void SpendCoins() //int coinsSpent)
		{
			HideSpendOverlay();

			if (CoinsWaitingSpendConfirm <= 0 || ! CanAfford(CoinsWaitingSpendConfirm))
				FightManager.Coins -= CoinsWaitingSpendConfirm;
		}
			
		private void HilightSelectedPowerUp()
		{
			if (SelectedPowerUpButton == null)
			{
				powerUpCoins.gameObject.SetActive(false);
				powerUpCost.text = "";
				return;
			}

			powerUpCoins.gameObject.SetActive(true);

			powerUpName.text = SelectedPowerUpButton.Name;
			powerUpDescription.text = SelectedPowerUpDescription;
			powerUpCost.text = string.Format("{0:N0}", SelectedPowerUpCoins);		// thousands separator
//			powerUpCoinValue = SelectedPowerUpCoins;
			// show swipe-up image and cool-down time for trigger power-ups
//			SwipeUpImage.gameObject.SetActive(SelectedPowerUp.IsTrigger);

			if (SelectedPowerUpButton.IsTrigger)
			{
				int coolDownSecs = SelectedPowerUpButton.TriggerCoolDown / 100;				// TriggerCoolDown is 1/100s

				if (coolDownSecs <= 0)			// indicates one-time use
					PowerUpCoolDown.text = FightManager.Translate("oneTimeOnly");
				else
					PowerUpCoolDown.text = string.Format(FightManager.Translate("cooldown"), coolDownSecs.ToString());
			}
			else
				PowerUpCoolDown.text = "";

			ResetPowerUpHilights();
			SelectedPowerUpButton.Hilight.enabled = true;
			SelectedPowerUpButton.GetComponent<Animator>().enabled = true;

			EquippedStatic.animator.enabled = ! SelectedPowerUpButton.IsTrigger;
			EquippedTrigger.animator.enabled = SelectedPowerUpButton.IsTrigger;

			// reset button hilights if not applicable
			if (! SelectedPowerUpButton.IsTrigger)
			{
				var hilight = EquippedTrigger.transform.Find("Hilight");
				hilight.GetComponent<Image>().color = Color.clear;
			}
			else
			{
				var hilight = EquippedStatic.transform.Find("Hilight");
				hilight.GetComponent<Image>().color = Color.clear;
			}
		}


		private string SelectedPowerUpDescription
		{
			get
			{
				switch (SelectedPowerUpButton.PowerUp)
				{
					case PowerUp.ArmourPiercing:
						return FightManager.Translate("armourPiercingDesc");

					case PowerUp.Avenger:
						return FightManager.Translate("avengerDesc");

					case PowerUp.Ignite:
						return FightManager.Translate("igniteDesc");

					case PowerUp.HealthBooster:
						return FightManager.Translate("healthBoosterDesc");

					case PowerUp.PoiseMaster:
						return FightManager.Translate("poiseMasterDesc");

					case PowerUp.PoiseWrecker:
						return FightManager.Translate("poiseWreckerDesc");

					case PowerUp.PowerAttack:
						return FightManager.Translate("powerAttackDesc");

					case PowerUp.Regenerator:
						return FightManager.Translate("regeneratorDesc");

					case PowerUp.SecondLife:
						return FightManager.Translate("secondLifeDesc");

					case PowerUp.VengeanceBooster:
						return FightManager.Translate("vengeanceBoosterDesc");

					case PowerUp.None:
					default:
						return "";
				}
			}
		}

		private PowerUpButton GetPowerUpButton(PowerUp powerUp)
		{
			switch (powerUp)
			{
				case PowerUp.ArmourPiercing:
					return ArmourPiercing;

				case PowerUp.Avenger:
					return Avenger;

				case PowerUp.Ignite:
					return Ignite;

				case PowerUp.HealthBooster:
					return HealthBooster;

				case PowerUp.PoiseMaster:
					return PoiseMaster;

				case PowerUp.PoiseWrecker:
					return PoiseWrecker;

				case PowerUp.PowerAttack:
					return PowerAttack;

				case PowerUp.Regenerator:
					return Regenerator;

				case PowerUp.SecondLife:
					return SecondLife;

				case PowerUp.VengeanceBooster:
					return VengeanceBooster;

				case PowerUp.None:
				default:
					return null;
			}
		}

		public int GetPowerUpCooldown(PowerUp powerUp)
		{
			switch (powerUp)
			{
				case PowerUp.ArmourPiercing:
					return ArmourPiercing.TriggerCoolDown;

				case PowerUp.Avenger:
					return Avenger.TriggerCoolDown;

				case PowerUp.Ignite:
					return Ignite.TriggerCoolDown;

				case PowerUp.HealthBooster:
					return HealthBooster.TriggerCoolDown;

				case PowerUp.PoiseMaster:
					return PoiseMaster.TriggerCoolDown;

				case PowerUp.PoiseWrecker:
					return PoiseWrecker.TriggerCoolDown;

				case PowerUp.PowerAttack:
					return PowerAttack.TriggerCoolDown;

				case PowerUp.Regenerator:
					return Regenerator.TriggerCoolDown;

				case PowerUp.SecondLife:
					return SecondLife.TriggerCoolDown;

				case PowerUp.VengeanceBooster:
					return VengeanceBooster.TriggerCoolDown;

				case PowerUp.None:
				default:
					return 0;
			}
		}


		private int SelectedPowerUpCoins
		{
			get { return PowerUpCoins(SelectedPowerUpButton.PowerUp); }
		}

		// coin difficulty factors apply to AI team members only
		private static float DifficultyCoinFactor(AIDifficulty difficulty)
		{
			switch (difficulty)
			{
				case AIDifficulty.Simple:
					return SimpleCoinFactor;

				case AIDifficulty.Easy:
					return EasyCoinFactor;

				case AIDifficulty.Medium:
				default:
					return MediumCoinFactor;

				case AIDifficulty.Hard:
					return HardCoinFactor;

				case AIDifficulty.Brutal:
					return BrutalCoinFactor;
			}
		}

		public static int TeamMemberCoinValue(ChallengeTeamMember teamMember, bool isAI)
		{
			float LevelFactor = (float)(teamMember.Level - 1) * levelCoinFactor;
			float levelCoins = (float)LevelCoins + ((float)LevelCoins * LevelFactor);
			int powerUpCoins = 0;

			if (isAI)
			{
				var difficulty = (AIDifficulty) Enum.Parse(typeof(AIDifficulty), teamMember.Difficulty);
				var staticPowerUp = (PowerUp) Enum.Parse(typeof(PowerUp), teamMember.StaticPowerUp);
				var triggerPowerUp = (PowerUp) Enum.Parse(typeof(PowerUp), teamMember.TriggerPowerUp);

				levelCoins *= DifficultyCoinFactor(difficulty);
				powerUpCoins = PowerUpCoins(staticPowerUp) + PowerUpCoins(triggerPowerUp);
			}

			return (int)levelCoins + powerUpCoins;
		}

		public static int ChallengeCoinValue(ChallengeData challenge, bool isAI)
		{
			int challengeCoins = 0;

			foreach (var teamMember in challenge.Team)
			{
				challengeCoins += TeamMemberCoinValue(teamMember, isAI);
			}

			return challengeCoins;
		}

		public static int PowerUpCoins(PowerUp powerUp)
		{
			switch (powerUp)
			{
				case PowerUp.None:
				default:
					return 0;

				case PowerUp.ArmourPiercing:
					return ArmourPiercingCoins;

				case PowerUp.Avenger:
					return AvengerCoins;

				case PowerUp.Ignite:
					return IgniteCoins;

				case PowerUp.HealthBooster:
					return HealthBoosterCoins;

				case PowerUp.PoiseMaster:
					return PoiseMasterCoins;

				case PowerUp.PoiseWrecker:
					return PoiseWreckerCoins;

				case PowerUp.PowerAttack:
					return PowerAttackCoins;

				case PowerUp.Regenerator:
					return RegeneratorCoins;

				case PowerUp.SecondLife:
					return SecondLifeCoins;

				case PowerUp.VengeanceBooster:
					return VengeanceBoosterCoins;
			}
		}


		private Sprite PowerUpSprite(PowerUp powerUp)
		{
			switch (powerUp)
			{
				case PowerUp.None:
				default:
					return null;

				case PowerUp.ArmourPiercing:
					return ArmourPiercingImage.sprite;

				case PowerUp.Avenger:
					return AvengerImage.sprite;

				case PowerUp.Ignite:
					return IgniteImage.sprite;

				case PowerUp.HealthBooster:
					return HealthBoosterImage.sprite;

				case PowerUp.PoiseMaster:
					return PoiseMasterImage.sprite;

				case PowerUp.PoiseWrecker:
					return PoiseWreckerImage.sprite;

				case PowerUp.PowerAttack:
					return PowerAttackImage.sprite;

				case PowerUp.Regenerator:
					return RegeneratorImage.sprite;

				case PowerUp.SecondLife:
					return SecondLifeImage.sprite;

				case PowerUp.VengeanceBooster:
					return VengeanceBoosterImage.sprite;
			}
		}
					

		private void ResetPowerUpHilights()
		{
			ArmourPiercing.Hilight.enabled = false;
			ArmourPiercing.GetComponent<Animator>().enabled = false;

			Avenger.Hilight.enabled = false;
			Avenger.GetComponent<Animator>().enabled = false;

			Ignite.Hilight.enabled = false;
			Ignite.GetComponent<Animator>().enabled = false;

			HealthBooster.Hilight.enabled = false;
			HealthBooster.GetComponent<Animator>().enabled = false;

			PoiseMaster.Hilight.enabled = false;
			PoiseMaster.GetComponent<Animator>().enabled = false;

			PoiseWrecker.Hilight.enabled = false;
			PoiseWrecker.GetComponent<Animator>().enabled = false;

			PowerAttack.Hilight.enabled = false;
			PowerAttack.GetComponent<Animator>().enabled = false;

			Regenerator.Hilight.enabled = false;
			Regenerator.GetComponent<Animator>().enabled = false;

			SecondLife.Hilight.enabled = false;
			SecondLife.GetComponent<Animator>().enabled = false;

			VengeanceBooster.Hilight.enabled = false;
			VengeanceBooster.GetComponent<Animator>().enabled = false;
		}

		private bool CanAffordSelectedPowerUp
		{
			get
			{
				if (PowerUpFighter == null || SelectedPowerUpButton == null)
					return false;

//				Debug.Log("CanAffordSelectedPowerUp: " + SelectedPowerUp.PowerUp + " Cost = " + SelectedPowerUp.Cost + " Coins = " + FightManager.Coins);
				return CanAfford(SelectedPowerUpCoins);
			}
		}

		public static bool CanAfford(int coins)
		{
			return (coins <= FightManager.Coins);
		}
	}
}