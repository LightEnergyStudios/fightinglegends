﻿using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


namespace FightingLegends
{
	public class TeamSelect : MenuCanvas
	{
		public Button fightHouseButton;				// select challenge category, then house challenge to take on
		public Button fightPlayerButton;			// select challenge category, then player challenge to take on
		public Button uploadButton;
//		public Button resultButton;
//		public Button powerUpButton;
//		public Text powerUpText;			// shown on power-up button

		public Text teamLabel;
		public Image coin;
		public Text costToField;
		public Text titleText;
		public Text fightHouseText;
		public Text fightPlayerText;
		public Text uploadText;
		public Text resultText;
		public Text statusText;

		public Button leoniButton;		// set in Inspector
		public Button shiroButton;			
		public Button natalyaButton;
		public Button danjumaButton;
		public Button hoiLunButton;
		public Button jacksonButton;
		public Button shiyangButton;
		public Button alazneButton;
		public Button skeletronButton;
		public Button ninjaButton;

		private FighterCard leoniCard;
		private FighterCard shiroCard;
		private FighterCard natalyaCard;
		private FighterCard danjumaCard;
		private FighterCard hoiLunCard;
		private FighterCard jacksonCard;
		private FighterCard shiyangCard;
		private FighterCard alazneCard;
		private FighterCard skeletronCard;
		private FighterCard ninjaCard;

		private const float cardXOffset = 76.0f; // 80.0f;
		private const float firstCardXOffset = -342.0f; // -282.0f; 		// first selected card (in team)
		private const float firstCardYOffset = -95.0f;
		private const float staggeredYOffset = 15.0f;
		private const float cardMoveTime = 0.15f;		// in/out of team
		private const float cardGatherTime = 0.04f;		// each card

		// power-up sprites for card inlay
		// static
		public Sprite ArmourPiercing;		// set in Inspector
		public Sprite Avenger;
		public Sprite PoiseMaster;
		public Sprite PoiseWrecker;
		public Sprite Regenerator;
		// trigger
		public Sprite VengeanceBooster;
		public Sprite Ignite;
		public Sprite HealthBooster;
		public Sprite PowerAttack;
		public Sprite SecondLife;

		public AudioClip moveAudio;
		public AudioClip shuffleAudio;
		public AudioClip addToTeamAudio;
		public AudioClip enterCardsAudio;

		public Image ChallengesOverlay;			// overlay panel (categories)
		public Text challengesTitle;			// diamond, gold, silver etc
		public Text challengesHousePlayer;
		public Color OwnChallengeColour;

		public Text UploadingMessage;

		private bool gettingUserProfile = false;

		private bool upLoadingChallenge = false;
		private bool UpLoadingChallenge
		{
			get { return upLoadingChallenge; }
			set
			{
				upLoadingChallenge = value;
				uploadButton.interactable = CanUploadChallenge; // !upLoadingChallenge;	

				if (upLoadingChallenge)
				{
					UploadingMessage.text = FightManager.Translate("uploadingChallenge") + " ...";
				}
				else
				{
					UploadingMessage.text = "";
					ChallengeUploading = null;
					CategoryUploading = ChallengeCategory.None;
				}
			}
		}

		private bool CanUploadChallenge
		{
			get
			{
//				bool challengeUploaded = FightManager.UserLoginProfile != null && FightManager.UserLoginProfile.ChallengeKey != "";
//				Debug.Log("CanUploadChallenge internetReachable = " + internetReachable + " gettingUserProfile = " + gettingUserProfile + " upLoadingChallenge = " + upLoadingChallenge);
				return internetReachable && selectedTeam.Count > 0 && !upLoadingChallenge && !gettingUserProfile; // && !challengeUploaded;
			}
		}

		private bool internetReachable = false;

		private ChallengeData ChallengeUploading = null;
		private ChallengeCategory CategoryUploading = ChallengeCategory.None;
		private bool challengeUploaded = false; 		// notational only
		private static AIDifficulty defaultDifficulty = AIDifficulty.Medium; 
		private static string defaultLocation = FightManager.dojo; 
		private string selectedLocation = ""; 

		private static int GoldThreshold = 1000;	// prize coins <=
		private static int SilverThreshold = 500;	// prize coins <=
		private static int BronzeThreshold = 200;	// prize coins <=
		private static int IronThreshold = 100;		// prize coins <=

//		private bool diamondFilled = false;		// with challenge buttons 
//		private bool goldFilled = false;		// with challenge buttons 
//		private bool silverFilled = false;		// with challenge buttons 
//		private bool bronzeFilled = false;		// with challenge buttons 
//		private bool ironFilled = false;		// with challenge buttons 

		// challenge category buttons
		public Button diamondButton;
		public Button goldButton;
		public Button silverButton;
		public Button bronzeButton;
		public Button ironButton;

		// challenge category button text
		public Text diamondLabel;
		public Text goldLabel;
		public Text silverLabel;
		public Text bronzeLabel;
		public Text ironLabel;

		// challenge category overlays
		public Image diamondOverlay;
		public Image goldOverlay;
		public Image silverOverlay;
		public Image bronzeOverlay;
		public Image ironOverlay;

		// challenge category viewports
		public ScrollRect diamondViewport;
		public ScrollRect goldViewport;	
		public ScrollRect silverViewport;
		public ScrollRect bronzeViewport;
		public ScrollRect ironViewport;	

		// challenge button backgrounds
		public Sprite diamondSprite;			
		public Sprite goldSprite;
		public Sprite silverSprite;
		public Sprite bronzeSprite;
		public Sprite ironSprite;

		// challenge button fighter portraits
		public Sprite danjumaSprite;			
		public Sprite leoniSprite;
		public Sprite shiroSprite;
		public Sprite natalyaSprite;
		public Sprite hoiLunSprite;
		public Sprite alazneSprite;
		public Sprite shiyangSprite;
		public Sprite jacksonSprite;
		public Sprite skeletronSprite;
		public Sprite ninjaSprite;

		// challenge button locations
		public Sprite hawaiiSprite;
		public Sprite chinaSprite;
		public Sprite tokyoSprite;
		public Sprite ghettoSprite;
		public Sprite cubaSprite;
		public Sprite nigeriaSprite;
		public Sprite sovietSprite;
		public Sprite hongKongSprite;
		public Sprite dojoSprite;
		public Sprite spaceStationSprite;

		// fighter element frames
		public Sprite airFireSprite;			
		public Sprite airWaterSprite;
		public Sprite earthFireSprite;
		public Sprite earthWaterSprite;

		public Sprite skeletronFrameSprite;
		public Sprite ninjaFrameSprite;

		private bool bossInTeam = false;			// must be last in team

		public GameObject challengeButtonPrefab;		// for filling challenge viewports
		public GameObject fighterButtonPrefab;			// for populating challenge buttons

		private const float fighterButtonScale = 0.7f;
		private const float fighterCardXOffset = -1260.0f;	// first card
		private const float fighterCardYOffset = -7.0f;
		private const float fighterCardOddOffset = -5.0f;	// staggered up/down
		private const float fighterCardWidth = 56.0f;		// overlapped slightly

		private const float challengeHeight = 124.0f;	// results in slight overlap of buttons to reduce gap
		private const float challengeOffset = challengeHeight / 2.0f;

		private const float challengesYOffset = -245.0f;
		private const float challengesLeft = 3;

		private TeamChallenge chosenChallenge = null;

		private bool gatheringTeam = false;		// shuffling cards up together
		private bool movingCard = false;		// in / out of team

		private int firstCardIndex;

		private ChallengeCategory selectedCategory = ChallengeCategory.None;
		private int selectedCategoryCount = 0;		// for list title

		private List<FighterCard> fighterDeck;		// in left-right order
		private List<FighterCard> selectedTeam;		// hand-picked
		private List<FighterCard> selectedAITeam;	// according to challenge chosen

		private FightManager fightManager;

		private Animator animator;
		private bool animatingEntry = false;
		private const float animatePauseTime = 0.5f;		// pause before animated card entry

		private const int challengeExpiryDays = 30;
		private UserProfile userProfile = null;				// for challenge results

		// 'constructor'
		private void Awake()
		{
			fighterDeck = new List<FighterCard>();
			selectedTeam = new List<FighterCard>();
			selectedAITeam = new List<FighterCard>();
		}

		// initialization
		public void Start()
		{
			var fightManagerObject = GameObject.Find("FightManager");
			fightManager = fightManagerObject.GetComponent<FightManager>();

			titleText.text = FightManager.Translate("pickYourTeam");
//			fightText.text = FightManager.Translate("chooseChallenge");
			fightHouseText.text = FightManager.Translate("houseChallenges", true);
			fightPlayerText.text = FightManager.Translate("playerChallenges", true);
			uploadText.text = FightManager.Translate("upload");
			resultText.text = FightManager.Translate("result");

			diamondLabel.text = FightManager.Translate("diamond");
			goldLabel.text = FightManager.Translate("gold");
			silverLabel.text = FightManager.Translate("silver");
			bronzeLabel.text = FightManager.Translate("bronze");
			ironLabel.text = FightManager.Translate("iron");

//			PopulateFighterCards();
			UpdateTeamCost();
			EnableActionButtons();

			AddListeners();

			StartCoroutine(AnimateCardEntry());
		}

		private void OnEnable()
		{
			//			Debug.Log("TeamSelect.OnEnable");
			CheckInternet();
			LayerTeam();

			uploadText.text = FightManager.Translate("upload");
			resultText.text = FightManager.Translate("result");
			statusText.text = "";

			PopulateFighterCards();

			if (FightManager.SavedGameStatus.UserId != "")
			{
				gettingUserProfile = true;
				FightManager.CheckForChallengeResult();		// OnGetUserProfile callback handles result
			}

			EnableActionButtons();
		}

		private void OnDisable()
		{
			EmptyChallengeButtons(ChallengeCategory.Diamond);
			EmptyChallengeButtons(ChallengeCategory.Gold);
			EmptyChallengeButtons(ChallengeCategory.Silver);
			EmptyChallengeButtons(ChallengeCategory.Bronze);
			EmptyChallengeButtons(ChallengeCategory.Iron);

			RestoreAllCards();			// to original positions

			ChallengeUploading = null;
			challengeUploaded = false;
		}

		public void OnDestroy()
		{
			RemoveListeners();
		}


		private void PopulateFighterCards()
		{
//			Debug.Log("PopulateFighterCards");
			fighterDeck.Add(ninjaCard = new FighterCard(ninjaButton, "Ninja", "P1", null));
			fighterDeck.Add(leoniCard = new FighterCard(leoniButton, "Leoni", "P1", null));
			fighterDeck.Add(shiroCard = new FighterCard(shiroButton, "Shiro", "P1", null));
			fighterDeck.Add(danjumaCard = new FighterCard(danjumaButton, "Danjuma", "P1", null));
			fighterDeck.Add(natalyaCard = new FighterCard(natalyaButton, "Natalya", "P1", null));
			fighterDeck.Add(hoiLunCard = new FighterCard(hoiLunButton, "Hoi Lun", "P1", null));
			fighterDeck.Add(jacksonCard = new FighterCard(jacksonButton, "Jackson", "P1", null));
			fighterDeck.Add(alazneCard = new FighterCard(alazneButton, "Alazne", "P1", null));
			fighterDeck.Add(shiyangCard = new FighterCard(shiyangButton, "Shiyang", "P1", null));
			fighterDeck.Add(skeletronCard = new FighterCard(skeletronButton, "Skeletron", "P1", null));

			firstCardIndex = ninjaCard.CardButton.transform.GetSiblingIndex();

			// lookup level, xp and power-ups from fighter profile (saved) data
			SetDeckProfiles();
			SetCardDifficulties(defaultDifficulty);

			LayerTeam();
		}

		private void SetCardDifficulties(AIDifficulty difficulty)
		{
			leoniCard.SetDifficulty(difficulty);
			shiroCard.SetDifficulty(difficulty);
			natalyaCard.SetDifficulty(difficulty);
			danjumaCard.SetDifficulty(difficulty);
			hoiLunCard.SetDifficulty(difficulty);
			jacksonCard.SetDifficulty(difficulty);
			shiyangCard.SetDifficulty(difficulty);
			alazneCard.SetDifficulty(difficulty);
			skeletronCard.SetDifficulty(difficulty);
			ninjaCard.SetDifficulty(difficulty);
		}

		// lookup lock status, level, xp and power-ups from fighter profile (saved) data
		private void SetDeckProfiles()
		{
			foreach (var card in fighterDeck)
			{
				var profile = Profile.GetFighterProfile(card.FighterName);
				if (profile != null)
				{
//					Debug.Log("SetDeckProfiles: " + card.FighterName + ", StaticPowerUp = " + profile.StaticPowerUp + ", TriggerPowerUp = " + profile.TriggerPowerUp);
					card.SetProfileData(profile.Level, profile.XP, profile.StaticPowerUp, PowerUpSprite(profile.StaticPowerUp), profile.TriggerPowerUp, PowerUpSprite(profile.TriggerPowerUp), CardFrame(card.FighterName),
								profile.IsLocked, profile.CanUnlockOrder, profile.UnlockCoins, profile.UnlockOrder, profile.UnlockDefeats, profile.UnlockDifficulty);
				}
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
			}
		}

		private Sprite CardFrame(string fighterName)
		{
			switch (fighterName)
			{
				case "Shiro":
					return earthFireSprite;
				case "Natalya":
					return airFireSprite;
				case "Hoi Lun":
					return airWaterSprite;
				case "Leoni":
					return airWaterSprite;
				case "Danjuma":
					return earthWaterSprite;
				case "Jackson":
					return earthFireSprite;
				case "Alazne":
					return earthWaterSprite;
				case "Shiyang":
					return airFireSprite;
				case "Skeletron":
					return skeletronFrameSprite;
				case "Ninja":
					return ninjaFrameSprite;
				default:
					return null;
			}
		}

		private void AddListeners()
		{
			FightManager.OnThemeChanged += SetTheme;
			FightManager.OnUserProfileChanged += OnUserProfileChanged;			// check for challenge result

			FirebaseManager.OnGetUserProfile += OnGetUserProfile;
			FirebaseManager.OnChallengeSaved += OnChallengeUploaded;
			FirebaseManager.OnChallengeAccepted += OnChallengeAccepted;
			FirebaseManager.OnChallengeRemoved += OnChallengeRemoved;
			FirebaseManager.OnChallengesDownloaded += OnChallengesDownloaded;
			FirebaseManager.OnUserProfileSaved += OnUserProfileSaved;

//			Debug.Log("TeamSelect.AddListeners");
			OnOverlayHidden += OverlayHidden;

			fightHouseButton.onClick.AddListener(delegate { ShowChallengesOverlay(false); });
			fightPlayerButton.onClick.AddListener(delegate { ShowChallengesOverlay(true); });
			uploadButton.onClick.AddListener(delegate { ConfirmUploadSelectedTeam(); });
//			resultButton.onClick.AddListener(delegate { DummyChallengeRoundResults(); });
//			powerUpButton.onClick.AddListener(delegate { PowerUpFighter(); });

			ChallengeUpload.OnCancelClicked += OnUploadCancelled;

			leoniButton.onClick.AddListener(delegate { CardClicked(leoniCard); });
			shiroButton.onClick.AddListener(delegate { CardClicked(shiroCard); });
			natalyaButton.onClick.AddListener(delegate { CardClicked(natalyaCard); });
			danjumaButton.onClick.AddListener(delegate { CardClicked(danjumaCard); });
			hoiLunButton.onClick.AddListener(delegate { CardClicked(hoiLunCard); });
			jacksonButton.onClick.AddListener(delegate { CardClicked(jacksonCard); });
			shiyangButton.onClick.AddListener(delegate { CardClicked(shiyangCard); });
			alazneButton.onClick.AddListener(delegate { CardClicked(alazneCard); });
			skeletronButton.onClick.AddListener(delegate { CardClicked(skeletronCard); });
			ninjaButton.onClick.AddListener(delegate { CardClicked(ninjaCard); });

			// challenges
			diamondButton.onClick.AddListener(delegate { CategoryChosen(ChallengeCategory.Diamond); });
			goldButton.onClick.AddListener(delegate { CategoryChosen(ChallengeCategory.Gold); });
			silverButton.onClick.AddListener(delegate { CategoryChosen(ChallengeCategory.Silver); });
			bronzeButton.onClick.AddListener(delegate { CategoryChosen(ChallengeCategory.Bronze); });
			ironButton.onClick.AddListener(delegate { CategoryChosen(ChallengeCategory.Iron); });
		}

		private void RemoveListeners()
		{
			FightManager.OnThemeChanged -= SetTheme;
			FightManager.OnUserProfileChanged -= OnUserProfileChanged;	

			FirebaseManager.OnGetUserProfile -= OnGetUserProfile;
			FirebaseManager.OnChallengeSaved -= OnChallengeUploaded;
			FirebaseManager.OnChallengeAccepted -= OnChallengeAccepted;
			FirebaseManager.OnChallengeRemoved -= OnChallengeRemoved;
			FirebaseManager.OnChallengesDownloaded -= OnChallengesDownloaded;
			FirebaseManager.OnUserProfileSaved -= OnUserProfileSaved;

//			Debug.Log("TeamSelect.RemoveListeners");
			OnOverlayHidden -= OverlayHidden;

			fightHouseButton.onClick.RemoveListener(delegate { ShowChallengesOverlay(false); });
			fightPlayerButton.onClick.AddListener(delegate { ShowChallengesOverlay(true); });

			uploadButton.onClick.RemoveListener(delegate { ConfirmUploadSelectedTeam(); });
//			resultButton.onClick.RemoveListener(delegate { DummyChallengeRoundResults(); });

			ChallengeUpload.OnCancelClicked -= OnUploadCancelled;

//			powerUpButton.onClick.RemoveListener(delegate { PowerUpFighter(); });

			leoniButton.onClick.RemoveListener(delegate { CardClicked(leoniCard); });
			shiroButton.onClick.RemoveListener(delegate { CardClicked(shiroCard); });
			natalyaButton.onClick.RemoveListener(delegate { CardClicked(natalyaCard); });
			danjumaButton.onClick.RemoveListener(delegate { CardClicked(danjumaCard); });
			hoiLunButton.onClick.RemoveListener(delegate { CardClicked(hoiLunCard); });
			jacksonButton.onClick.RemoveListener(delegate { CardClicked(jacksonCard); });
			shiyangButton.onClick.RemoveListener(delegate { CardClicked(shiyangCard); });
			alazneButton.onClick.RemoveListener(delegate { CardClicked(alazneCard); });
			skeletronButton.onClick.RemoveListener(delegate { CardClicked(skeletronCard); });
			ninjaButton.onClick.RemoveListener(delegate { CardClicked(ninjaCard); });

			// challenges
			diamondButton.onClick.RemoveListener(delegate { CategoryChosen(ChallengeCategory.Diamond); });
			goldButton.onClick.RemoveListener(delegate { CategoryChosen(ChallengeCategory.Gold); });
			silverButton.onClick.RemoveListener(delegate { CategoryChosen(ChallengeCategory.Silver); });
			bronzeButton.onClick.RemoveListener(delegate { CategoryChosen(ChallengeCategory.Bronze); });
			ironButton.onClick.RemoveListener(delegate { CategoryChosen(ChallengeCategory.Iron); });
		}
			
		private void OverlayHidden(Image panel, int overlayCount)
		{
			// reset title if hiding a challenge category overlay
			if (panel == diamondOverlay || panel == goldOverlay || panel == silverOverlay || panel == bronzeOverlay || panel == ironOverlay)
			{
				selectedCategory = ChallengeCategory.None;
				SetChallengesTitle();
			}
		}

		private void CheckInternet()
		{
			internetReachable = (Application.internetReachability != NetworkReachability.NotReachable);

			if (!internetReachable)
				statusText.text = FightManager.Translate("noInternet");
		}

		private IEnumerator MoveCardTo(FighterCard card, Vector3 targetPosition, float moveTime, AudioClip audio)
		{
			var button = card.CardButton;
			var startPosition = button.transform.localPosition;
			float t = 0.0f;

			if (startPosition == targetPosition)
				yield break;

			if (moveAudio != null)
				AudioSource.PlayClipAtPoint(moveAudio, Vector3.zero, FightManager.SFXVolume);

			movingCard = true;
			
			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / moveTime); 
				button.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
				yield return null;
			}

			if (audio != null)
				AudioSource.PlayClipAtPoint(audio, Vector3.zero, FightManager.SFXVolume);

			movingCard = false;
			yield return null;
		}

		private IEnumerator MoveCardToTeam(FighterCard card)
		{
			yield return StartCoroutine(MoveCardTo(card, NextTeamPosition, cardMoveTime, addToTeamAudio));
		}

		private Vector3 FirstTeamPosition
		{
			get { return new Vector3(firstCardXOffset, firstCardYOffset, 0.0f); }
		}

		private Vector3 NextTeamPosition
		{
			get
			{
				if (selectedTeam.Count == 0)
				{
					return FirstTeamPosition;
				}
				else
				{
					var lastPosition = selectedTeam[selectedTeam.Count - 1].currentPosition;
					return CardPosition(selectedTeam, lastPosition);
				}
			}
		}

		private Vector3 CardPosition(List<FighterCard> team, Vector3 lastPosition)
		{
			return new Vector3(lastPosition.x + cardXOffset, lastPosition.y + (IsOdd(team.Count) ? -staggeredYOffset : staggeredYOffset), lastPosition.z);
		}

		private bool IsOdd(int number)
		{
			return (number % 2 != 0);
		}

		private IEnumerator GatherTeam()
		{
			if (selectedTeam.Count == 0)
				yield break;

			gatheringTeam = true;

			Vector3 lastPosition = Vector3.zero;
			int counter = 0;

			foreach (var teamMember in selectedTeam)
			{
				if (counter == 0)
					yield return StartCoroutine(MoveCardTo(teamMember, FirstTeamPosition, cardGatherTime, shuffleAudio));
				else
				{
					var newPosition = new Vector3(lastPosition.x + cardXOffset, lastPosition.y + (IsOdd(counter) ? -staggeredYOffset : staggeredYOffset), lastPosition.z);
					yield return StartCoroutine(MoveCardTo(teamMember, newPosition, cardGatherTime, shuffleAudio));
				}
				lastPosition = teamMember.currentPosition;
				counter++;
			}

			gatheringTeam = false;
			yield return null;
		}

		private void LayerTeam()
		{
			int index = 1;

			foreach (var teamMember in selectedTeam)
			{
				// layer the cards from left to right
				var button = teamMember.CardButton;

				button.transform.SetSiblingIndex(firstCardIndex + index++);
			}

			// force challenges overlay on top!
			ChallengesOverlay.transform.SetSiblingIndex(firstCardIndex + 100);
		}

		// cards not in team
		private void LayerCards()
		{
			int index = 1;

			foreach (var card in fighterDeck)
			{
				if (card.InTeam)
					continue;
				
				// layer the cards from left to right
				var button = card.CardButton;

				button.transform.SetSiblingIndex(firstCardIndex + index++);
			}

			// force challenges overlay on top!
			ChallengesOverlay.transform.SetSiblingIndex(firstCardIndex + 100);
		}

		private void UpdateTeamCost(bool silent = true)
		{
//			if (selectedTeam.Count == 0)
//			{
//				teamLabel.text = FightManager.Translate("noTeamMembers");
//				costToField.text = "";
//				coin.gameObject.SetActive(false);
//				return;
//			}

//			teamLabel.text = FightManager.Translate("costToFieldTeam") + " ";
			teamLabel.text = FightManager.Translate("teamValue") + " ";
			costToField.text = string.Format("{0:N0}", SelectedTeamPrizeCoins());	// thousands separator, for clarity
			coin.gameObject.SetActive(true);

			if (!silent)
				fightManager.CoinAudio();
		}

		private int SelectedTeamPrizeCoins()
		{
			int teamCost = 0;

			foreach (var teamMember in selectedTeam)
			{
				teamCost += Store.TeamMemberCoinValue(ConvertToTeamMember(teamMember), true); // false);	//TODO: ok?
			}

			return teamCost;
		}

		// move card in / out of selectedTeam
		private void CardClicked(FighterCard card)
		{
			if (card.IsLocked)
				return;
			
			if (gatheringTeam || movingCard || animatingEntry)
				return;

			StartCoroutine(SwitchCard(card));		// in / out of team
		}

//		private void PowerUpFighter()
//		{
//			if (selectedTeam.Count > 0)
//			{
//				fightManager.TeamSelectChoice = MenuType.Store;									// triggers fade to black and new menu
//				var fighterName = selectedTeam[ selectedTeam.Count-1 ].FighterName;
//				var fighterColour = selectedTeam[ selectedTeam.Count-1 ].FighterColour;
//				fightManager.StorePowerUpOverlay(fighterName, fighterColour, true);				// direct to store power-up overlay
//			}
//		}


		// move in / out of team
		private IEnumerator SwitchCard(FighterCard card)
		{
			if (card.InTeam) 		// already in team so remove
			{
				// complete move before gathering team
				yield return StartCoroutine(MoveCardTo(card, card.originalPosition, cardMoveTime, null));
				selectedTeam.Remove(card);
				card.InTeam = false;

				if (card.FighterName == "Skeletron")
					bossInTeam = false;					// must be last

//				if (selectedTeam.Count > 0)
//					powerUpText.text = "POWER-UP " + selectedTeam[ selectedTeam.Count-1 ].FighterName.ToUpper();	// last added to team
//				else 
//					powerUpText.text = "POWER-UP";

				LayerTeam();
				LayerCards();
				EnableActionButtons();

				StartCoroutine(GatherTeam());
			}
			else 						// not in team so add
			{
//				var teamDifficulty = GetTeamDifficulty(selectedTeam);
				if (bossInTeam)		// can't add any more
				{
					fightManager.WrongAudio();
					yield break;
				}

				StartCoroutine(MoveCardToTeam(card));
				selectedTeam.Add(card);
				card.InTeam = true;

				if (card.FighterName == "Skeletron")
					bossInTeam = true;
				
//				powerUpText.text = "POWER-UP " + card.FighterName.ToUpper();

//				if (addToTeamAudio != null)
//					AudioSource.PlayClipAtPoint(addToTeamAudio, Vector3.zero, FightManager.SFXVolume);
		
				LayerTeam();
				EnableActionButtons();
			}
				
			UpdateTeamCost(false);
			yield return null;
		}

		private void RestoreAllCards()
		{
			foreach (var card in fighterDeck)
			{
				card.RestorePosition();

				if (card.InTeam)
				{
					card.InTeam = false;
					selectedTeam.Remove(card);
				}
			}

			LayerCards();
			UpdateTeamCost();

			EnableActionButtons();
		}
			

		#region challenges

		private void EnableActionButtons()
		{
			bool fightersInTeam = selectedTeam.Count > 0;
			fightHouseButton.interactable = fightersInTeam;
			fightPlayerButton.interactable = fightersInTeam; // CanUploadChallenge;
			uploadButton.interactable = CanUploadChallenge;
//			resultButton.interactable = fightersInTeam;
		}


		private IEnumerator AnimateCardEntry()
		{
			animator = GetComponent<Animator>();
			animatingEntry = true;

//			yield return new WaitForSeconds(animatePauseTime);

			animator.enabled = true;
			animator.SetTrigger("EnterCards");
			yield return null;
		}

		public void EnterCardSound()
		{
			if (enterCardsAudio != null)
				AudioSource.PlayClipAtPoint(enterCardsAudio, Vector3.zero, FightManager.SFXVolume);
		}

		public void CardEntryComplete()
		{
			animator.enabled = false;
			animatingEntry = false;
		}

		private void CategoryChosen(ChallengeCategory category)
		{
			selectedCategory = category;

			Debug.Log("CategoryChosen: selectedCategory = " + selectedCategory.ToString());

			switch (selectedCategory)
			{
				case ChallengeCategory.Diamond:
					StartCoroutine(RevealOverlay(diamondOverlay));
					break;

				case ChallengeCategory.Gold:
					StartCoroutine(RevealOverlay(goldOverlay));
					break;

				case ChallengeCategory.Silver:
					StartCoroutine(RevealOverlay(silverOverlay));
					break;

				case ChallengeCategory.Bronze:
					StartCoroutine(RevealOverlay(bronzeOverlay));
					break;

				case ChallengeCategory.Iron:
					StartCoroutine(RevealOverlay(ironOverlay));
					break;
			}
					
			GetChallenges(selectedCategory); //, false);
		}

		private void SetChallengesTitle()
		{
			challengesHousePlayer.text = "[ " + (fightManager.PlayerCreatedChallenges ? FightManager.Translate("player") : FightManager.Translate("house")) + " ]";

			switch (selectedCategory)
			{
				case ChallengeCategory.Diamond:
					challengesTitle.text = string.Format("{0} [ {1} ]", FightManager.Translate("diamond"), selectedCategoryCount);
					break;

				case ChallengeCategory.Gold:
					challengesTitle.text = string.Format("{0} [ {1} ]", FightManager.Translate("gold"), selectedCategoryCount);
					break;

				case ChallengeCategory.Silver:
					challengesTitle.text = string.Format("{0} [ {1} ]", FightManager.Translate("silver"), selectedCategoryCount);
					break;

				case ChallengeCategory.Bronze:
					challengesTitle.text = string.Format("{0} [ {1} ]", FightManager.Translate("bronze"), selectedCategoryCount);
					break;

				case ChallengeCategory.Iron:
					challengesTitle.text = string.Format("{0} [ {1} ]", FightManager.Translate("iron"), selectedCategoryCount);
					break;

				default:
					challengesTitle.text = FightManager.Translate("chooseCategory");
					break;
			}
		}

		private void ChallengeChosen(ChallengeButton challengeButton)
		{
			chosenChallenge = challengeButton.Challenge;

			if (chosenChallenge == null)
			{
				Debug.Log("ChallengeChosen: no challenge!");
				return;
			}

			// must register as a user before accepting a player challenge!
			if (chosenChallenge.UserId != "" && string.IsNullOrEmpty(FightManager.SavedGameStatus.UserId))
			{
				FightManager.RegisterNewUser();
				return;
			}
					
			if (!OwnChallenge(chosenChallenge))
			{
				selectedCategoryCount = 0;
				HideAllOverlays();
				ConfirmFightChallenge();
			}
		}
			
		private void ConfirmFightChallenge()
		{
			if (chosenChallenge != null)
				FightManager.GetConfirmation(FightManager.Translate("confirmFightChallenge"), SelectedTeamPrizeCoins(), FightChallenge);
		}

		private void FightChallenge()
		{
			if (chosenChallenge == null)
				return;
			
//			Debug.Log("FightChallenge: selectedTeam " + selectedTeam.Count + ", AITeam " + challenge.AITeam.Count);

			// create AI team according to challenge
			selectedAITeam.Clear();

			foreach (var AICard in chosenChallenge.AITeam)
			{
				selectedAITeam.Add(AICard);
			}

			if (chosenChallenge.UserId != "")			// player created challenge (one per user) - flag as accepted (in progress)
				FirebaseManager.AcceptChallenge(chosenChallenge, SelectedTeamPrizeCoins());
			else
				SetupChallenge(chosenChallenge);
		}


//		private void ConfirmRemoveChallenge()
//		{
//			if (chosenChallenge != null)
//				FightManager.GetConfirmation(FightManager.Translate("confirmRemoveChallenge"), chosenChallenge.PrizeCoins, RemoveChallenge);
//		}
//
//		private void RemoveChallenge()
//		{
//			if (chosenChallenge == null || ! OwnChallenge(chosenChallenge))
//				return;
//
//			FirebaseManager.RemoveChallenge(chosenChallenge.ChallengeCategory.ToString(), chosenChallenge.ChallengeKey);	// callback below
//		}

		private void OnChallengeRemoved(string category, string challengeKey, bool success)
		{
//			if (success)
//			{
//				FightManager.UserLoginProfile.ChallengeKey = "";
//				FirebaseManager.SaveUserProfile(FightManager.UserLoginProfile);
//			}
			
			if (category == selectedCategory.ToString())
				GetChallenges(selectedCategory);			// refresh challenge buttons
		}


		private void SetupChallenge(TeamChallenge challenge)
		{
			fightManager.SetupChallenge(challenge, selectedTeam, SelectedTeamPrizeCoins(), selectedAITeam);

			FightManager.CombatMode = FightMode.Challenge;
			fightManager.TeamSelectChoice = MenuType.Combat;			// triggers fade to black and new fight

			chosenChallenge = null;
		}

		private void OnChallengeAccepted(TeamChallenge challenge, string challengerId, bool success)
		{
			if (success)
			{
				SetupChallenge(challenge);
				GetChallenges(selectedCategory); 		// reload to exclude challenge newly accepted challenge(s)
			}
			else
				Debug.Log("OnChallengeAccepted: Challenge not currently available");
		}

		public void ShowChallengesOverlay(bool playerChallenges)	// house challenges if false
		{
			fightManager.PlayerCreatedChallenges = playerChallenges;

			SetChallengesTitle();

//			HideAllOverlays();
			Debug.Log("ShowChallengesOverlay: playerChallenges = " + playerChallenges);

			StartCoroutine(RevealOverlay(ChallengesOverlay));		// challenge categories
		}

		private void HideChallengesOverlay()
		{
			// categories = 0;
			StartCoroutine(HideOverlay(ChallengesOverlay));			// challenge categories
		}
			

		public static int ChallengePot(ChallengeData challenge)
		{
			var challengePot = challenge.PrizeCoins + challenge.ChallengerTeamCoins;
			return challengePot - (int)((float)challengePot * FightManager.ChallengeFee / 100.0f);
		}

		// populate the ChallengeButton with a FighterButton for each AI team member
		private void FillChallengeFighterButtons(ChallengeButton challengeButton)
		{
			float lastPositionX = fighterCardXOffset;		// starting position of first card
			int fighterIndex = 0;

			foreach (var fighterCard in challengeButton.Challenge.AITeam)
			{
				var fighterButtonObject = Instantiate(fighterButtonPrefab, challengeButton.Fighters.transform) as GameObject;		// viewport content
				fighterButtonObject.transform.localScale = new Vector3(fighterButtonScale, fighterButtonScale, fighterButtonScale);

				var fighterButton = fighterButtonObject.GetComponent<FighterButton>();
				var fighterRect = fighterButtonObject.GetComponent<RectTransform>();

//				Debug.Log("FillChallengeFighterButtons: " + fighterCard.FighterName + " static = " + fighterCard.StaticPowerUp + " trigger = " + fighterCard.TriggerPowerUp);
				fighterButton.SetFighterCard(FighterSprite(fighterCard.FighterName), fighterCard);

				// currently clicking a fighter button has the same effect as clicking the challenge button
				fighterCard.CardButton.onClick.AddListener(delegate { ChallengeChosen(challengeButton); });
				fighterCard.CardButton.interactable = ! OwnChallenge(challengeButton.Challenge);	// can't fight own challenge!

				// set fighter button position within container challenge button viewport
				var yOffset = fighterCardYOffset - (IsOdd(fighterIndex) ? fighterCardOddOffset : 0);
				fighterRect.SetAnchor(AnchorPresets.MiddleLeft, lastPositionX, yOffset);
				lastPositionX += fighterCardWidth;

				fighterIndex++;
			}
		}


		private Sprite FighterSprite(string fighterName)
		{
			switch (fighterName)
			{
				case "Shiro":
					return shiroSprite;
				case "Natalya":
					return natalyaSprite;
				case "Hoi Lun":
					return hoiLunSprite;
				case "Leoni":
					return leoniSprite;
				case "Danjuma":
					return danjumaSprite;
				case "Jackson":
					return jacksonSprite;
				case "Alazne":
					return alazneSprite;
				case "Shiyang":
					return shiyangSprite;
				case "Ninja":
					return ninjaSprite;
				case "Skeletron":
					return skeletronSprite;
				default:
					return null;
			}
		}

		private void GetChallenges(ChallengeCategory category) //, bool playerCreated)
		{
			if (fightManager.PlayerCreatedChallenges) // playerCreated)
			{
				FirebaseManager.GetCategoryChallenges(category);		// FillChallengeButtons on callback
			}
			else
			{
				switch (category)
				{
					case ChallengeCategory.Diamond:
						FillChallengeButtons(category, DiamondChallenges); //, false);
						break;

					case ChallengeCategory.Gold:
						FillChallengeButtons(category, GoldChallenges); //, false);
						break;

					case ChallengeCategory.Silver:
						FillChallengeButtons(category, SilverChallenges); //, false);
						break;

					case ChallengeCategory.Bronze:
						FillChallengeButtons(category, BronzeChallenges); //, false);
						break;

					case ChallengeCategory.Iron:
						FillChallengeButtons(category, IronChallenges); //, false);
						break;

					default:
						return;
				}
			}
		}


		private void FillChallengeButtons(ChallengeCategory category, List<TeamChallenge> challenges) //, bool playerCreated)
		{
//			Debug.Log("FillChallengeButtons " + category);

			EmptyChallengeButtons(category);

			selectedCategoryCount = challenges.Count;
			SetChallengesTitle();

			RectTransform viewportContent;
			Sprite categorySprite;

			switch (category)
			{
				case ChallengeCategory.Diamond:
//					if (diamondFilled && ! fightManager.PlayerCreatedChallenges)
//						return;
					viewportContent = diamondViewport.content;
					categorySprite = diamondSprite;
//					diamondFilled = true;
					break;

				case ChallengeCategory.Gold:
//					if (goldFilled && ! fightManager.PlayerCreatedChallenges)
//						return;
					viewportContent = goldViewport.content;
					categorySprite = goldSprite;
//					goldFilled = true;
					break;

				case ChallengeCategory.Silver:
//					if (silverFilled && ! fightManager.PlayerCreatedChallenges)
//						return;
					viewportContent = silverViewport.content;
					categorySprite = silverSprite;
//					silverFilled = true;
					break;

				case ChallengeCategory.Bronze:
//					if (bronzeFilled && ! fightManager.PlayerCreatedChallenges)
//						return;
					viewportContent = bronzeViewport.content;
					categorySprite = bronzeSprite;
//					bronzeFilled = true;
					break;

				case ChallengeCategory.Iron:
//					if (ironFilled && ! fightManager.PlayerCreatedChallenges)
//						return;
					viewportContent = ironViewport.content;
					categorySprite = ironSprite;
//					ironFilled = true;
					break;

				default:
					return;
			}

			// calculate prize coins from members of 'system' challenge team
			if (! fightManager.PlayerCreatedChallenges) // playerCreated)
			{
				foreach (var challenge in challenges)
				{
					foreach (var teamMember in challenge.AITeam)
					{
//						Debug.Log("FillChallengeButtons: " + category + ": static = " + teamMember.StaticPowerUp + " trigger = " + teamMember.TriggerPowerUp);
						challenge.PrizeCoins += Store.TeamMemberCoinValue(ConvertToTeamMember(teamMember), true);
					}
				}
			}
					
			int counter = 0;

			foreach (var challenge in challenges)
			{
				var challengeButtonObject = Instantiate(challengeButtonPrefab, viewportContent.transform) as GameObject;
				challengeButtonObject.transform.localScale = Vector3.one;						// somehow corrupted by instantiate! - crappy
				challengeButtonObject.transform.localPosition = Vector3.zero;					// to make sure z is zero!! ...

				var challengeButton = challengeButtonObject.GetComponent<ChallengeButton>();
				var challengeBackground = challengeButtonObject.GetComponent<Image>();

				var locationSprite = LocationSprite(challenge);
				challengeButton.locationImage.sprite = locationSprite;
				challengeButton.locationImage.gameObject.SetActive(locationSprite != null);

				challengeButton.SetChallenge(challenge); 

				challengeBackground.sprite = categorySprite;
				challengeButton.PrizeCoins.text = string.Format("{0:N0}", challenge.PrizeCoins);		// thousands separator, for clarity
				challengeButton.Name.text = challenge.Name;
				challengeButton.Date.text = challenge.DateCreated;

				if (OwnChallenge(challenge))
				{
					challengeButton.Name.color = OwnChallengeColour;
					challengeButton.Date.color = OwnChallengeColour;
//					challengeButton.PrizeCoins.color = OwnChallengeColour;
				}

				var button = challengeButtonObject.GetComponent<Button>();
				button.onClick.AddListener(delegate { ChallengeChosen(challengeButton); });		// ref to challenge data, not data in this loop
				button.interactable = ! OwnChallenge(challenge);

				// populate challenge button fighters viewport by instantiating fighter buttons	
				FillChallengeFighterButtons(challengeButton);		

				counter++;
			}
		}

		private bool OwnChallenge(TeamChallenge challenge)
		{
			return challenge.UserId != "" && challenge.UserId == FightManager.SavedGameStatus.UserId; 	// can't fight own challenge!
		}
			

		private void EmptyChallengeButtons(ChallengeCategory category)
		{
			RectTransform viewportContent;
//			Debug.Log("EmptyChallengeButtons " + category);

			switch (category)
			{
				case ChallengeCategory.Diamond:
					viewportContent = diamondViewport.content;
//					diamondFilled = false;
					break;

				case ChallengeCategory.Gold:
					viewportContent = goldViewport.content;
//					goldFilled = false;
					break;

				case ChallengeCategory.Silver:
					viewportContent = silverViewport.content;
//					silverFilled = false;
					break;

				case ChallengeCategory.Bronze:
					viewportContent = bronzeViewport.content;
//					bronzeFilled = false;
					break;

				case ChallengeCategory.Iron:
					viewportContent = ironViewport.content;
//					ironFilled = false;
					break;

				default:
					return;
			}

			foreach (var challengeButton in viewportContent.GetComponentsInChildren<Button>())
			{
				challengeButton.onClick.RemoveAllListeners();
				Destroy(challengeButton.gameObject);
			}
		}

		private Sprite LocationSprite(TeamChallenge challenge)
		{
			switch (challenge.Location)
			{
				case FightManager.hawaii:
					return hawaiiSprite;

				case FightManager.china:
					return chinaSprite;

				case FightManager.tokyo:
					return tokyoSprite;

				case FightManager.ghetto:
					return ghettoSprite;

				case FightManager.cuba:
					return cubaSprite;

				case FightManager.nigeria:
					return nigeriaSprite;

				case FightManager.soviet:
					return sovietSprite;

				case FightManager.hongKong:
					return hongKongSprite;

				case FightManager.dojo:
					return dojoSprite;

				case FightManager.spaceStation:
					return spaceStationSprite;

				default:
					return null;
			}
		}

//		private FighterCard CreateChallengeCard(string name, string colour, int level, float xpPercent, AIDifficulty difficulty, Sprite staticPowerUpSprite, Sprite triggerPowerUpSprite)
		private FighterCard CreateChallengeCard(string name, string colour, int level, float xpPercent, AIDifficulty difficulty, PowerUp staticPowerUp, PowerUp triggerPowerUp)
		{
			// AI fighter colour is determined by Player1, so not relevant here
			return new FighterCard(null, name, colour, level, xpPercent, staticPowerUp, PowerUpSprite(staticPowerUp), triggerPowerUp, PowerUpSprite(triggerPowerUp), CardFrame(name), difficulty);
		}


		private void ConfirmUploadSelectedTeam()
		{
			// must register as a user before uploading!
			if (string.IsNullOrEmpty(FightManager.SavedGameStatus.UserId))
			{
				FightManager.RegisterNewUser();
				return;
			}
//			else if (FightManager.UserLoginProfile.ChallengeKey != "")
//			{
//				FightManager.GetOkConfirmation(FightManager.Translate("challengeAlreadyUploaded", false, true), 0);
//				return;
//			}

			if (upLoadingChallenge)
				return;

			if (selectedTeam.Count == 0)
				return;

			if (selectedLocation == "")
				selectedLocation = defaultLocation;

			ChallengeUploading = new ChallengeData {

				Name = FightManager.SavedGameStatus.UserId,		// future use? user assigned challenge name?
				DateCreated = DateTime.Now.ToString(),
				ExpiryDate = DateTime.Now.AddDays(challengeExpiryDays).ToString(),
				Location = selectedLocation,
				Team = new List<ChallengeTeamMember>(),		// filled below
				PrizeCoins = 0,								// set below according to team members
				ParentCategory = "",						// set according to PrizeCoins below

				UserId = FightManager.SavedGameStatus.UserId,
			};
				
			foreach (var teamMember in selectedTeam)
			{
				var newTeamMember = ConvertToTeamMember(teamMember);
				ChallengeUploading.Team.Add(newTeamMember);

				ChallengeUploading.PrizeCoins += Store.TeamMemberCoinValue(newTeamMember, true);
			}

			CategoryUploading = DetermineCoinCategory(ChallengeUploading);
			ChallengeUploading.ParentCategory = CategoryUploading.ToString();

//			FightManager.GetConfirmation(FightManager.Translate("confirmUploadChallenge"), ChallengeUploading.PrizeCoins, UploadChallenge);

			if (FightManager.UserLoginProfile.ChallengeKey != "")
			{
				ChallengeUploading.Key = FightManager.UserLoginProfile.ChallengeKey;			// reuse same key (saves deleting old challenge)
				FightManager.ConfirmChallengeUpload(ChallengeUploading, ReplaceChallenge);
			}
			else
				FightManager.ConfirmChallengeUpload(ChallengeUploading, UploadChallenge);
		}


		private void UploadChallenge()
		{
//			if (challengeUploaded)
//				return;

			if (ChallengeUploading == null)
				return;
			
			if (CategoryUploading == ChallengeCategory.None)
				return;
			
			UpLoadingChallenge = true;
			FirebaseManager.SaveChallenge(ChallengeUploading, CategoryUploading, true); 
		}

		private void ReplaceChallenge()
		{
//			if (challengeUploaded)
//				return;

			if (ChallengeUploading == null)
				return;

			if (CategoryUploading == ChallengeCategory.None)
				return;

			UpLoadingChallenge = true;
			FirebaseManager.SaveChallenge(ChallengeUploading, CategoryUploading, false); 
		}

		private void OnUploadCancelled(AIDifficulty difficulty, string location)
		{
			// set all cards to the selected difficulty in case more team members are added
			SetCardDifficulties(difficulty);

			selectedLocation = location;
		}


		private void OnUserProfileChanged(string userId, UserProfile profile)
		{
			if (!internetReachable)
				return;
			
			if (FightManager.SavedGameStatus.UserId == "")		// not registered
			{
//				uploadButton.interactable = true;				// will prompt for user registration
				return;
			}
			
			userProfile = profile;

			if (profile != null && userId == FightManager.SavedGameStatus.UserId)	
			{
				uploadButton.interactable = CanUploadChallenge; 		// one at a time

				if (profile.ChallengeKey == "")			// no challenge uploaded
				{
					statusText.text = "";
					return;
				}
					
				challengeUploaded = true;
				statusText.text = FightManager.Translate("challengeUploaded");
					
//				if (profile.ChallengeResult == "Won")
//					resultText.text = FightManager.Translate("youWon", false, true);
//				else if (profile.ChallengeResult == "Lost")
//					resultText.text = FightManager.Translate("youLost", false, true);
//				else 		// challenge not yet accepted / completed
//					resultText.text = "";	
//				
//				profile.ChallengeResult = "";
//				profile.CoinsToCollect = 0;
//				profile.ChallengeKey = "";
//
////				FirebaseManager.SaveUserProfile(profile);		// callback enables/disables upload button according to challenge status

//				ShowChallengeResult();
			}
		}

		private void OnUserProfileSaved(string userId, UserProfile profile, bool success)
		{
			if (FightManager.SavedGameStatus.UserId == "")		// not registered
				return;
			
			if (success && profile != null && userId == FightManager.SavedGameStatus.UserId)
			{
				FightManager.UserLoginProfile = profile;  		// -> OnUserProfileChanged

//				uploadButton.interactable = CanUpload; // profile.ChallengeKey == "";		// one at a time
//
//				if (profile.ChallengeKey != "")
//					statusText.text = FightManager.Translate("challengeUploaded");
//				else
//					statusText.text = "";
			}
		}

		private void ShowChallengeResult()
		{
			if (userProfile == null)
				return;
			
			if (userProfile.ChallengeResult == "Won")
			{
				// payout coins and congratulations
				FightManager.ShowChallengeResult(userProfile.CoinsToCollect, true, "", null);		// TODO: challengerId?
			}
			else if (userProfile.ChallengeResult == "Lost")
			{
				// commiserations
				FightManager.ShowChallengeResult(0, false, "", null);
			}
		}


		public static ChallengeCategory DetermineCoinCategory(ChallengeData challenge)
		{
			if (challenge.PrizeCoins <= IronThreshold)
				return ChallengeCategory.Iron;

			if (challenge.PrizeCoins <= BronzeThreshold)
				return ChallengeCategory.Bronze;

			if (challenge.PrizeCoins <= SilverThreshold)
				return ChallengeCategory.Silver;

			if (challenge.PrizeCoins <= GoldThreshold)
				return ChallengeCategory.Gold;

			return ChallengeCategory.Diamond;
		}

//		public static AIDifficulty? GetTeamDifficulty(TeamChallenge challenge)
//		{
//			AIDifficulty? difficulty = null;
//
//			foreach (var teamMember in challenge.AITeam)
//			{
//				if (difficulty == null)					// first team member
//					difficulty = teamMember.Difficulty;
//
//				if (teamMember.Difficulty != difficulty)	// not same as first - must be mixed difficulties
//					return null;
//			}
//
//			return difficulty;
//		}

		// callback from Firebase
		private void OnGetUserProfile(string userId, UserProfile profile, bool success)
		{
			if (userId == FightManager.SavedGameStatus.UserId)
			{
				if (success)
				{
					if (profile != null && !string.IsNullOrEmpty(profile.ChallengeKey))
					{
						challengeUploaded = true;
						statusText.text = FightManager.Translate("challengeUploaded");
					}
					else
					{
						statusText.text = "";
					}

					resultText.text = "";	

					if (profile.ChallengeResult == "Won" || profile.ChallengeResult == "Lost")
					{
						if (profile.ChallengeResult == "Won")
							resultText.text = FightManager.Translate("youWon", false, true);
						else 
							resultText.text = FightManager.Translate("youLost", false, true);

						ShowChallengeResult();	// TODO: slight risk that SaveUserProfile below fails - possible double payout ... OnUserProfileChanged??

						// clear user profile challenge result
						profile.ChallengeResult = "";
						profile.CoinsToCollect = 0;
						profile.ChallengeKey = "";

						FirebaseManager.SaveUserProfile(profile);		// callback enables/disables upload button according to challenge status
					}
				}

				gettingUserProfile = false;
				EnableActionButtons();
			}
		}


		// callback from Firebase
		private void OnChallengeUploaded(ChallengeCategory category, ChallengeData challenge, bool success)
		{
			if (success)
			{
				if (UpLoadingChallenge)
				{
					UpLoadingChallenge = false;

					challengeUploaded = true;
					statusText.text = FightManager.Translate("challengeUploaded");

//					uploadText.text = FightManager.Translate("uploaded");
//					uploadButton.interactable = false;
				}
				else 		// uploaded by someone else - refresh list
				{
					if (category == selectedCategory)
						GetChallenges(category);//, true);			// refresh challenge buttons
				}

//				Debug.Log("OnChallengeUploaded: " + challenge.TimeCreated);
//				fightManager.TeamSelectChoice = MenuType.ModeSelect;
			}
		}

		// callback from Firebase
		private void OnChallengesDownloaded(ChallengeCategory category, List<ChallengeData> challenges, bool success)
		{
			UpLoadingChallenge = false;

			if (success)
			{
				List<TeamChallenge> teamChallenges = new List<TeamChallenge>();

				foreach (var challenge in challenges)
				{
					teamChallenges.Add(ConvertToTeamChallenge(challenge));
				}

				FillChallengeButtons(category, teamChallenges); //, true);
//				Debug.Log("OnChallengesDownloaded: " + category);
			}
		}

		private TeamChallenge ConvertToTeamChallenge(ChallengeData challenge)
		{
			var teamChallenge = new TeamChallenge
			{
				ChallengeKey = challenge.Key,
				ChallengeCategory = (ChallengeCategory) Enum.Parse(typeof(ChallengeCategory), challenge.ParentCategory),
				Location = challenge.Location,
				PrizeCoins = challenge.PrizeCoins,
				Name = challenge.Name,
				DateCreated = challenge.DateCreated,
				UserId = challenge.UserId,

				AITeam = new List<FighterCard>(),
			};

			foreach (var teamMember in challenge.Team)
			{
				var difficulty = (AIDifficulty) Enum.Parse(typeof(AIDifficulty), teamMember.Difficulty);
				var staticPowerUp = (PowerUp) Enum.Parse(typeof(PowerUp), teamMember.StaticPowerUp);
				var triggerPowerUp = (PowerUp) Enum.Parse(typeof(PowerUp), teamMember.TriggerPowerUp);
//				Debug.Log("ConvertToTeamChallenge: " + teamMember.FighterName + " Static " + staticPowerUp + " Trigger " + triggerPowerUp);

				teamChallenge.AITeam.Add(CreateChallengeCard(teamMember.FighterName, teamMember.FighterColour, teamMember.Level, (float)teamMember.XP, difficulty, staticPowerUp, triggerPowerUp));
			}

			return teamChallenge;
		}

		private ChallengeTeamMember ConvertToTeamMember(FighterCard fighterCard)
		{
			return new ChallengeTeamMember
			{
				FighterName = fighterCard.FighterName,
				FighterColour = fighterCard.FighterColour,
				Level = fighterCard.Level,
				XP = (int)fighterCard.XP,
				Difficulty = fighterCard.Difficulty.ToString(),
				StaticPowerUp = fighterCard.StaticPowerUp.ToString(),
				TriggerPowerUp = fighterCard.TriggerPowerUp.ToString(),
			};
		}
			

		private List<TeamChallenge> DiamondChallenges
		{
			get
			{
				return new List<TeamChallenge>
				{
					// DANCE DANCE RETRIBUTION ( Prize = 1,275)

					new TeamChallenge
					{
						ChallengeCategory = ChallengeCategory.Diamond,
						Location = FightManager.hawaii,
						AITeam = new List<FighterCard>
						{
							CreateChallengeCard("Leoni", "P1", 50, 50.0f, AIDifficulty.Medium, PowerUp.ArmourPiercing, PowerUp.HealthBooster),
							CreateChallengeCard("Leoni", "P2", 61, 61.0f, AIDifficulty.Hard, PowerUp.Regenerator, PowerUp.PowerAttack),
							CreateChallengeCard("Hoi Lun", "P3", 33, 33.0f, AIDifficulty.Medium, PowerUp.PoiseMaster, PowerUp.Ignite),
							CreateChallengeCard("Danjuma", "P1", 45, 45.0f, AIDifficulty.Hard, PowerUp.PoiseWrecker, PowerUp.VengeanceBooster),
							CreateChallengeCard("Leoni", "P3", 70, 70.0f, AIDifficulty.Brutal, PowerUp.Avenger, PowerUp.SecondLife),
						}
					},

					// NINETY NINE NINJAS ( Prize = 2,615 )

					new TeamChallenge
					{
						ChallengeCategory = ChallengeCategory.Diamond,
						Location = FightManager.dojo,
						AITeam = new List<FighterCard>
						{
							CreateChallengeCard("Ninja", "P1", 99, 99.0f, AIDifficulty.Simple, PowerUp.Regenerator, PowerUp.HealthBooster),
							CreateChallengeCard("Ninja", "P2", 99, 99.0f, AIDifficulty.Easy, PowerUp.Avenger, PowerUp.VengeanceBooster),
							CreateChallengeCard("Ninja", "P3", 99, 99.0f, AIDifficulty.Medium, PowerUp.ArmourPiercing, PowerUp.PowerAttack),
							CreateChallengeCard("Ninja", "P1", 99, 99.0f, AIDifficulty.Medium, PowerUp.PoiseWrecker, PowerUp.SecondLife),
							CreateChallengeCard("Ninja", "P2", 99, 99.0f, AIDifficulty.Medium, PowerUp.PoiseMaster, PowerUp.Ignite),
							CreateChallengeCard("Ninja", "P3", 99, 99.0f, AIDifficulty.Hard, PowerUp.Avenger, PowerUp.PowerAttack),
							CreateChallengeCard("Ninja", "P1", 99, 99.0f, AIDifficulty.Hard, PowerUp.ArmourPiercing, PowerUp.HealthBooster),
							CreateChallengeCard("Ninja", "P2", 99, 99.0f, AIDifficulty.Hard, PowerUp.Regenerator, PowerUp.PowerAttack),
							CreateChallengeCard("Ninja", "P3", 99, 99.0f, AIDifficulty.Brutal, PowerUp.PoiseMaster, PowerUp.SecondLife),

						}
					},

					// FIGHTING LEGENDS ( Prize = 3,598!! )

					new TeamChallenge
					{
						ChallengeCategory = ChallengeCategory.Diamond,
						Location = FightManager.dojo,
						AITeam = new List<FighterCard>
						{
							CreateChallengeCard("Leoni", "P1", 100, 25.0f, AIDifficulty.Hard, PowerUp.Regenerator, PowerUp.HealthBooster),
							CreateChallengeCard("Shiro", "P1", 100, 80.0f, AIDifficulty.Hard, PowerUp.ArmourPiercing, PowerUp.Ignite),
							CreateChallengeCard("Danjuma", "P1", 100, 10.0f, AIDifficulty.Hard, PowerUp.PoiseWrecker, PowerUp.PowerAttack),
							CreateChallengeCard("Natalya", "P1", 100, 80.0f, AIDifficulty.Hard, PowerUp.Regenerator, PowerUp.Ignite),
							CreateChallengeCard("Hoi Lun", "P1", 100, 25.0f, AIDifficulty.Hard, PowerUp.PoiseMaster, PowerUp.PowerAttack),
							CreateChallengeCard("Jackson", "P1", 100, 80.0f, AIDifficulty.Hard, PowerUp.PoiseWrecker, PowerUp.Ignite),
							CreateChallengeCard("Alazne", "P1", 100, 10.0f, AIDifficulty.Hard, PowerUp.Avenger, PowerUp.VengeanceBooster),
							CreateChallengeCard("Shiyang", "P1", 100, 80.0f, AIDifficulty.Hard, PowerUp.ArmourPiercing, PowerUp.Ignite),
							CreateChallengeCard("Ninja", "P1", 100, 10.0f, AIDifficulty.Brutal, PowerUp.Avenger, PowerUp.SecondLife),
							CreateChallengeCard("Skeletron", "P1", 100, 90.0f, AIDifficulty.Hard, PowerUp.ArmourPiercing, PowerUp.SecondLife),
						}
					},
				};
			}
		}
			
		private List<TeamChallenge> GoldChallenges
		{
			get
			{
				return new List<TeamChallenge>
				{
					// I, SKELETRON ( Prize = 606 )

					new TeamChallenge
					{
						ChallengeCategory = ChallengeCategory.Gold,
						Location = FightManager.spaceStation,
						AITeam = new List<FighterCard>
						{
							CreateChallengeCard("Skeletron", "P3", 100, 78.0f, AIDifficulty.Brutal, PowerUp.Regenerator, PowerUp.SecondLife),
						}
					},

					// TRIAL BY FIRE ( Prize = 810 )

					new TeamChallenge
					{
						ChallengeCategory = ChallengeCategory.Gold,
						Location = FightManager.china,
						AITeam = new List<FighterCard>
						{
							CreateChallengeCard("Shiro", "P1", 20, 27.0f, AIDifficulty.Hard, PowerUp.ArmourPiercing, PowerUp.Ignite),
							CreateChallengeCard("Jackson", "P3", 44, 76.0f, AIDifficulty.Hard, PowerUp.Regenerator, PowerUp.Ignite),
							CreateChallengeCard("Natalya", "P1", 38, 11.0f, AIDifficulty.Hard, PowerUp.Avenger, PowerUp.Ignite),
							CreateChallengeCard("Shiyang", "P2", 47, 89.0f, AIDifficulty.Hard, PowerUp.PoiseMaster, PowerUp.Ignite),
						}
					},
				};
			}
		}

		private List<TeamChallenge> SilverChallenges
		{
			get
			{
				return new List<TeamChallenge>
				{
					// AIR RAID ( Prize = ? )

					new TeamChallenge
					{
						ChallengeCategory = ChallengeCategory.Silver,
						Location = FightManager.soviet,
						AITeam = new List<FighterCard>
						{
							CreateChallengeCard("Natalya", "P3", 27, 64.0f, AIDifficulty.Medium, PowerUp.None, PowerUp.None),
							CreateChallengeCard("Leoni", "P2", 14, 19.0f, AIDifficulty.Medium, PowerUp.None, PowerUp.None),
							CreateChallengeCard("Shiyang", "P3", 67, 80.0f, AIDifficulty.Medium, PowerUp.None, PowerUp.None),
							CreateChallengeCard("Hoi Lun", "P3", 80, 77.0f, AIDifficulty.Medium, PowerUp.None, PowerUp.PowerAttack),
						}
					},
				};
			}
		}

		private List<TeamChallenge> BronzeChallenges
		{
			get
			{
				return new List<TeamChallenge>
				{

				};
			}
		}

		private List<TeamChallenge> IronChallenges
		{
			get
			{
				return new List<TeamChallenge>
				{
					new TeamChallenge
					{
						ChallengeCategory = ChallengeCategory.Iron,
						Location = FightManager.hawaii,
						AITeam = new List<FighterCard>
						{
							CreateChallengeCard("Leoni", "P1", 10, 67.0f, AIDifficulty.Easy, PowerUp.None, PowerUp.None),
							CreateChallengeCard("Danjuma", "P2", 11, 11.0f, AIDifficulty.Easy, PowerUp.None, PowerUp.None),
							CreateChallengeCard("Alazne", "P1", 12, 90.0f, AIDifficulty.Medium, PowerUp.None, PowerUp.None),
							CreateChallengeCard("Hoi Lun", "P2", 13, 41.0f, AIDifficulty.Medium, PowerUp.ArmourPiercing, PowerUp.None),
						}
					},
				};
			}
		}

		#endregion 		// challenges
	}

	public class ChallengeRoundResult
	{
		public FighterCard Winner { get; private set; }
		public FighterCard Loser { get; private set; }
		public bool AIWinner { get; private set; }

		public ChallengeRoundResult(FighterCard winnerCard, FighterCard loserCard, bool aiWinner)
		{
			Winner = winnerCard;
			Loser = loserCard;
			AIWinner = aiWinner;
		}
	}
}
