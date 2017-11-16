using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using System;

namespace FightingLegends
{
	public class TrainingUI : MonoBehaviour
	{
		// combo steps
		public GameObject comboStepPrefab;

		public Image ComboPanel;		// steps are children
		private List<ComboStepUI> comboUISteps;

		public Text ComboPrompt;			// tap! etc. (child of panel)
		public Text ComboText;				// eg. for testing (child of panel)

		public Button SkipButton;
		public Text SkipText;

		// images for combo steps
		public Sprite tapSprite;
		public Sprite holdSprite;
		public Sprite swipeForwardSprite;
		public Sprite swipeBackSprite;
		public Sprite swipeUpSprite;
		public Sprite swipeDownSprite;
		public Sprite swipeVengeanceSprite;
		public Sprite mashSprite;
		public Sprite resetSprite;

		public AudioClip ComboStepAudio;			// bling

		public Color StepWaitingColour;				// white
		public Color StepSetupColour;				// semi-transparent (when first set up)
		public Color StepCompletedColour;			// semi-transparent (tick enabled)
		public Color StepDefaultColour;				// semi-transparent

		private const float comboSetupPause = 0.075f;	// as each step set up
		private const float comboStepWidth = 64;		// image
		private const float comboStepSpace = 6;			// between images
		private const float comboStepGrowTime = 0.25f; // when waiting for input (activated)
		private const float comboStepGrowScale = 2.0f; 	// when waiting for input (activated)

		private bool syncingComboUI = false;
		private const float cleanupPause = 0.25f;		// pause before UI cleanup on combo completed

		private FightManager fightManager;

		private Trainer trainer = null;					// fighter in training (watching)

		// info message bubble

		public Sprite FightModeSprite;
		public Sprite CrystalSprite;

		public Sprite OkSprite;							// green tick
		public Sprite NotOkSprite;						// red cross

		public Image InfoBubble;
		public Text InfoBubbleHeading;
		public Text InfoBubbleText;
		public Image InfoBubbleImage;
		public Color bubbleColour;
		public AudioClip bubbleSound;

		private long lastInfoBubbleTicks = 0;					// ticks (10,000 milliseconds, 10 million ticks per second)
		private const float minTicksBetweenBubbles = 250000000;	// 2.5 seconds (10 million ticks per second)

		private const float bubbleTime = 0.25f;
		private const float bubbleOverTime = 0.1f;

		public static bool InfoBubbleShowing { get; private set; }
		private InfoBubbleMessage bubbleShowing = InfoBubbleMessage.None;

		public static InfoBubbleMessage CurrentInfoBubbleMessage = InfoBubbleMessage.None;
		private bool animatingBubble = false;
		private float infoBubblePause = 0.75f;		// can't dismiss until after this pause
		private const float menuBubbleDelay = 0.5f;	// before bubble shown for menus


		public delegate void InfoBubbleDelegate(InfoBubbleMessage message, bool isShowing, bool freezeFight);
		public static InfoBubbleDelegate OnInfoBubble;


		private void Awake()
		{
			var fightManagerObject = GameObject.Find("FightManager");
			fightManager = fightManagerObject.GetComponent<FightManager>();
		}

		private void Start()
		{
			GameUI.TrafficLightInfoBubble += TrafficLightInfoBubble;
			GameUI.OnGaugeIncreased += CrystalInfoBubble;
			FightManager.OnAIBlock += ShoveInfoBubble;
//			FightManager.OnNewFight += CombatModeInfoBubble;
//			FightManager.OnMenuChanged += MenuInfoBubble;
			FightManager.OnInfoBubbleRead += OnInfoBubbleRead;
			GestureListener.OnSwipeCount += SwipeCountInfoBubble;

			EnableSkip(false);
			SkipText.text = FightManager.Translate("skip");
		}

		private void OnDestroy()
		{
			GameUI.TrafficLightInfoBubble -= TrafficLightInfoBubble;
			GameUI.OnGaugeIncreased -= CrystalInfoBubble;
			FightManager.OnAIBlock -= ShoveInfoBubble;
//			FightManager.OnNewFight -= CombatModeInfoBubble;
//			FightManager.OnMenuChanged -= MenuInfoBubble;
			FightManager.OnInfoBubbleRead -= OnInfoBubbleRead;
			GestureListener.OnSwipeCount -= SwipeCountInfoBubble;

			StopListening();
		}

	
		public void SetTrainer()
		{
//			Debug.Log("SetTrainer: HasPlayer1 = " + fightManager.HasPlayer1 + ", InTraining = " + fightManager.Player1.InTraining);
			if (fightManager != null && fightManager.HasPlayer1 && fightManager.Player1.InTraining)
			{
				StopListening();		// detach from current trainer, if any
				trainer = fightManager.Player1.Trainer;
				StartListening();
			}
		}

		private void StartListening()
		{
			if (trainer != null)
			{
				// training listeners
				trainer.OnComboStarted += OnComboStarted;
//				trainer.OnComboVerticalFlash += OnComboVerticalFlash;
				trainer.OnComboRestart += OnComboRestart;
				trainer.OnComboUpdated += OnComboUpdated;
				trainer.OnComboCompleted += OnComboCompleted;

				SkipButton.onClick.AddListener(() => { SkipTraining(); });
			}
		}

		private void StopListening()
		{
			if (trainer != null)
			{
				trainer.OnComboStarted -= OnComboStarted;
//				trainer.OnComboVerticalFlash -= OnComboVerticalFlash;
				trainer.OnComboRestart -= OnComboRestart;
				trainer.OnComboUpdated -= OnComboUpdated;
				trainer.OnComboCompleted -= OnComboCompleted;

				SkipButton.onClick.RemoveAllListeners();
			}
		}


		public void EnableSkip(bool enable)
		{
			SkipButton.gameObject.SetActive(enable);
		}


		private void SkipTraining()
		{
			fightManager.BackClicked();
			FightManager.SavedGameStatus.CompletedBasicTraining = true;		// to prevent auto repeat
		}
			
		private void SetComboStepImage(ComboStep step, Image stepImage)
		{
			switch (step.ComboMove)
			{
				case Move.Strike_Light:
				case Move.Strike_Medium:
				case Move.Strike_Heavy:
					stepImage.sprite = tapSprite;
					break;

				case Move.Special:								// also special extra - TODO: fire characters!
					stepImage.sprite = swipeForwardSprite;
					break;

				case Move.Vengeance:
					stepImage.sprite = swipeVengeanceSprite;
					break;

				case Move.Block:
					stepImage.sprite = holdSprite;
					break;

				case Move.Shove:
					stepImage.sprite = swipeDownSprite;
					break;

				case Move.Counter:
					stepImage.sprite = swipeBackSprite;
					break;

				case Move.Roman_Cancel:
					stepImage.sprite = resetSprite;
					break;

				case Move.Power_Up:
					stepImage.sprite = swipeUpSprite;
					break;

				default:
					break;
			}
		}

	
		public Sprite GestureSprite(FeedbackFXType gesture)
		{
			switch (gesture)
			{
				case FeedbackFXType.Mash:
					return mashSprite;

				case FeedbackFXType.Hold:
					return holdSprite;

				case FeedbackFXType.Press:
					return tapSprite;

				case FeedbackFXType.Press_Both:
					return resetSprite;

				case FeedbackFXType.Swipe_Forward:
					return swipeForwardSprite;

				case FeedbackFXType.Swipe_Back:
					return swipeBackSprite;

				case FeedbackFXType.Swipe_Up:
					return swipeUpSprite;

				case FeedbackFXType.Swipe_Down:
					return swipeDownSprite;

				case FeedbackFXType.Swipe_Vengeance:
					return swipeVengeanceSprite;

				default:
					return null;
			}
		}

		private IEnumerator CleanupComboUI()
		{
//			Debug.Log("CleanupComboUI");
//			while (syncingComboUI)
//				yield return null;

			yield return new WaitForSeconds(cleanupPause);		// wait for any animation to stop?

			foreach (var step in comboUISteps)
			{
				step.StopAnimation();
				Destroy(step.gameObject);
			}

			comboUISteps.Clear();		// just in case!
//			ComboVerticalFlash.gameObject.SetActive(false);

			ComboPanel.gameObject.SetActive(false);
			yield return null;
		}

		private void OnComboStarted(TrainingCombo combo)
		{
//			Debug.Log("GameUI: OnComboStarted: " + combo.ComboName + " steps: " + combo.ComboSteps.Count);
			StartCoroutine(SyncComboUI(combo, true));
		}

//		private void OnComboVerticalFlash(ComboStep step, int stepUIIndex, bool enabled)
//		{
//			// animated vertical flash
//			if (enabled)
//			{
////				Debug.Log("GameUI: OnComboVerticalFlash: step = " + step.StepName + " stepUIIndex = " + stepUIIndex + " enabled = " + enabled);
//				ComboStepUI stepUI = comboUISteps[stepUIIndex];
//				ComboVerticalFlash.transform.localPosition = stepUI.transform.localPosition;
//				ComboVerticalFlash.gameObject.SetActive(true);
//			}
//			else
//			{
//				ComboVerticalFlash.gameObject.SetActive(false);
//			}
//		}

		private void OnComboRestart(TrainingCombo combo, bool reachedMaxFailures)
		{
//			Debug.Log("OnComboRestart: " + combo.ComboName + ", reachedMaxFailures = " + reachedMaxFailures);
			if (reachedMaxFailures)			// before info bubble message
			{
				ComboFailureInfoBubble(combo);
			}
		}

		private void OnComboUpdated(TrainingCombo combo)
		{
//			Debug.Log("GameUI: OnComboUpdated: " + combo.ComboName + " steps: " + combo.ComboSteps.Count);
			StartCoroutine(SyncComboUI(combo, false));
		}

		private void OnComboCompleted(TrainingCombo combo)
		{
			ComboText.text = "";
			ComboPrompt.text = "";

//			Debug.Log("GameUI: OnComboCompleted " + combo.ComboName);

			StartCoroutine(CleanupComboUI());
//			ComboPanel.gameObject.SetActive(false);
		}


		private void SetupComboUIHorizontal(TrainingCombo combo)
		{
			var numSteps = combo.NonAIStepCount;
			float comboWidth = (numSteps * comboStepWidth) + ((numSteps-1) * comboStepSpace);
			float xOffset = (-comboWidth / 2) + (comboStepWidth / 2);

			comboUISteps = new List<ComboStepUI>();

			foreach (var comboStep in combo.ComboSteps)
			{
				if (comboStep.IsAIMove)
					continue;

				var stepObject = Instantiate(comboStepPrefab, ComboPanel.transform) as GameObject;
				stepObject.transform.localScale = Vector3.one;						// somehow corrupted by instantiate! go figure - crappy
				stepObject.SetActive(false);

				var stepUI = stepObject.GetComponent<ComboStepUI>();

				SetComboStepImage(comboStep, stepUI.StepImage);
				stepUI.StepImage.rectTransform.anchoredPosition3D = Vector3.zero;		// to make sure posZ is zero!! ... go figure again
				stepUI.StepImage.rectTransform.SetAnchor(AnchorPresets.MiddleCentre, xOffset, 0);
				comboUISteps.Add(stepUI);

				stepUI.Init(comboStep);

				xOffset += comboStepWidth + comboStepSpace;
			}
		}

//		private void SetupComboUIVertical(TrainingCombo combo)
//		{
//			var numSteps = combo.NonAIStepCount;
//			float comboHeight = (numSteps * comboStepWidth) + ((numSteps-1) * comboStepSpace);
//			float yOffset = (comboHeight / 2) - (comboStepWidth / 2);
//
//			comboUISteps = new List<ComboStepUI>();
//
//			foreach (var comboStep in combo.ComboSteps)
//			{
//				if (comboStep.IsAIMove)
//					continue;
//
//				var stepObject = Instantiate(comboStepPrefab, ComboPanel.transform) as GameObject;
//				stepObject.transform.localScale = Vector3.one;						// somehow corrupted by instantiate! go figure - crappy
//				stepObject.SetActive(false);
//
//				var stepUI = stepObject.GetComponent<ComboStepUI>();
//
//				SetComboStepImage(comboStep, stepUI.StepImage);
//				stepUI.StepImage.rectTransform.anchoredPosition3D = Vector3.zero;		// to make sure posZ is zero!! ... go figure again
//				stepUI.StepImage.rectTransform.SetAnchor(AnchorPresets.MiddleCentre, 0, yOffset);
//				comboUISteps.Add(stepUI);
//
//				stepUI.Init(comboStep);
//
//				yOffset -= comboStepWidth + comboStepSpace;
//			}
//		}

		private IEnumerator PulseComboUI()
		{
			ComboPanel.gameObject.SetActive(true);

			foreach (var stepUI in comboUISteps)
			{
				stepUI.gameObject.SetActive(true);
				stepUI.StepImage.color = StepSetupColour;
				yield return StartCoroutine(stepUI.Pulse(comboSetupPause, new Vector3(comboStepGrowScale, comboStepGrowScale, comboStepGrowScale), ComboStepAudio, false));
			}
		}


		// sync combo UI with TrainingCombo
		private IEnumerator SyncComboUI(TrainingCombo combo, bool setup)
		{
			if (!fightManager.TrainingInProgress) // || syncingComboUI)
				yield break;

			ComboText.text = "";

			if (setup)
			{
				SetupComboUIHorizontal(combo);						// instantiate images
				yield return StartCoroutine(PulseComboUI());		// for effect
			}

			if (comboUISteps.Count == 0)		// unlikely!
				yield break;

			syncingComboUI = true;

//			Debug.Log("SyncComboUI: " + combo.ComboName + " ComboSteps: " + combo.ComboSteps.Count + " comboUISteps: " + comboUISteps.Count);
			bool firstStep = true;
			int remainingStepCount = 0;		// steps yet to come - hilight next in line

			// there may be fewer UI steps (images) than steps in the combo because of 'invisible' AI steps
			// so we have to keep a separate index
			int UIindex = 0;

			for (int i = 0; i < combo.ComboSteps.Count; i++)
			{
				ComboStep comboStep = combo.ComboSteps[i];

				if (comboStep.IsAIMove)
				{
					firstStep = false;
					continue;
				}

				ComboStepUI stepUI = comboUISteps[UIindex];
				Image stepImage = stepUI.StepImage;
				Image stepTick = stepUI.StepTick;

				//				ComboVerticalFlash.gameObject.SetActive(false);

				if (stepTick != null)
					stepTick.enabled = comboStep.Completed;

				if (comboStep.WaitingForInput)
				{
					stepUI.gameObject.SetActive(true);
					stepImage.color = StepWaitingColour;

//					yield return StartCoroutine(stepUI.Pulse(comboStepGrowTime, new Vector3(comboStepGrowScale, comboStepGrowScale, comboStepGrowScale), (firstStep ? null : ComboStepAudio), true, true, null));	// first step silent (at idle - waiting for first input of combo)
					yield return StartCoroutine(stepUI.Pulse(comboStepGrowTime, new Vector3(comboStepGrowScale, comboStepGrowScale, comboStepGrowScale), (firstStep ? null : ComboStepAudio), false, true, null));	// first step silent (at idle - waiting for first input of combo)
				}
				else if (comboStep.Completed)
				{
					stepImage.color = StepCompletedColour;
					StartCoroutine(stepUI.Shrink(comboStepGrowTime, null));				// from current scale back to one
				}
				else
				{
					stepImage.color = remainingStepCount == 0 ? StepCompletedColour : StepDefaultColour;		// next step more visible
					StartCoroutine(stepUI.Shrink(comboStepGrowTime, null));				// from current scale back to one

					remainingStepCount++;
				}

//				if (stepTick != null)
//					stepTick.enabled = comboStep.Completed;

				firstStep = false;
				UIindex++;
			}

			syncingComboUI = false;
			yield return null;
		}

		#region info bubble

		private void OnInfoBubbleRead()
		{
			if (bubbleShowing != InfoBubbleMessage.None)
			{
				FightManager.SetInfoBubbleMessageRead(bubbleShowing);
				StartCoroutine(HideInfoBubble());
				bubbleShowing = InfoBubbleMessage.None;

				lastInfoBubbleTicks = DateTime.Now.Ticks;
			}
		}

		private void TrafficLightInfoBubble(TrafficLight colour, bool flashing)
		{
			if (colour == TrafficLight.None)
				return;

			if (! GameUI.TrafficLightVisible)
				return;

			if (!fightManager.ReadyToFight)			// no point if not ready
				return;

			Vector3 bubblePosition = Vector3.zero;
			InfoBubbleMessage bubbleMessage = GameUI.TrafficLightMessage(colour, flashing);

			if (FightManager.WasInfoBubbleMessageRead(bubbleMessage))
				return;

			string bubbleHeading = "";
			string bubbleText = "";
			Sprite bubbleImage = null;

			switch (colour)
			{
				case TrafficLight.Red:
					bubbleHeading = FightManager.Translate("redLightHeading", false, true);
					bubbleText = FightManager.Translate("redLightNarrative");
					bubbleImage = holdSprite;
					break;

				case TrafficLight.Yellow:
					bubbleHeading = FightManager.Translate("yellowLightHeading", false, true);
					bubbleText = FightManager.Translate("yellowLightNarrative");
					bubbleImage = resetSprite;
					break;

				case TrafficLight.Green:
					if (flashing)
					{
						bubbleHeading = FightManager.Translate("flashingGreenLightHeading", false, true);
						bubbleText = FightManager.Translate("flashingGreenLightNarrative");			// combined opponent vulnerable and fire special extra
						bubbleImage = tapSprite;

						// TODO: bubbleText = FightManager.Translate("specialExtraFireNarrative">Mash to follow-up your special attack!</string>
						//							bubbleImage = MashSprite;
					}
					else
					{
						bubbleHeading = FightManager.Translate("greenLightHeading", false, true);
						bubbleText = FightManager.Translate("greenLightNarrative");
						bubbleImage = tapSprite;
					}
					break;

				case TrafficLight.Left:
					bubbleHeading = FightManager.Translate("leftArrowHeading", false, true);
					bubbleText = FightManager.Translate("leftArrowNarrative");
					bubbleImage = swipeBackSprite;
					break;

				case TrafficLight.Right:
					bubbleHeading = FightManager.Translate("rightArrowHeading", false, true);
					bubbleText = FightManager.Translate("specialExtraWaterNarrative");
					bubbleImage = swipeForwardSprite;
					break;

				default:
					break;
			}

			StartCoroutine(ShowInfoBubble(bubblePosition, bubbleMessage, bubbleHeading, bubbleText, bubbleImage, true));
		}

		private void ComboFailureInfoBubble(TrainingCombo combo)
		{
//			Debug.Log("ComboFailureInfoBubble: " + combo.ComboName);

			InfoBubbleMessage message = InfoBubbleMessage.None;
			string bubbleHeading = "";
			string bubbleText = "";
			Sprite bubbleImage = NotOkSprite;

			if (combo.ComboName == Trainer.LMHComboName)
			{
				message = InfoBubbleMessage.LMHComboTiming;
				bubbleHeading = FightManager.Translate("comboTimingInfoHeading");
				bubbleText = FightManager.Translate("LMHComboTimingMessage");
			}
			else if (combo.ComboName == Trainer.ResetComboName)
			{
				message = InfoBubbleMessage.ResetComboTiming;
				bubbleHeading = FightManager.Translate("comboTimingInfoHeading");
				bubbleText = FightManager.Translate("ResetComboTimingMessage");
			}

			if (! FightManager.WasInfoBubbleMessageRead(message))
				StartCoroutine(ShowInfoBubble(Vector3.zero, message, bubbleHeading, bubbleText, bubbleImage, false));
		}

		public void FailedInputBubble(FailedInput failedInput)
		{
//			Debug.Log("FailedInputBubble: " + failedInput);

			InfoBubbleMessage bubbleMessage = InfoBubbleMessage.None;
			string bubbleHeading = "";
			string bubbleText = "";
			Sprite bubbleImage = GestureSprite(failedInput.InputRequired);

			switch (failedInput.InputRequired)
			{
				case FeedbackFXType.Mash:
					bubbleMessage = InfoBubbleMessage.Mash;
					bubbleHeading = FightManager.Translate("mashInfoHeading");
					bubbleText = FightManager.Translate("mashInfoMessage");
					break;

				case FeedbackFXType.Hold:
					bubbleMessage = InfoBubbleMessage.Hold;
					bubbleHeading = FightManager.Translate("holdInfoHeading");
					bubbleText = FightManager.Translate("holdInfoMessage");
					break;

				case FeedbackFXType.Press:
					bubbleMessage = InfoBubbleMessage.Tap;
					bubbleHeading = FightManager.Translate("tapInfoHeading");
					bubbleText = FightManager.Translate("tapInfoMessage");
					break;

				case FeedbackFXType.Press_Both:
					bubbleMessage = InfoBubbleMessage.TapBoth;
					bubbleHeading = FightManager.Translate("bothTapInfoHeading");
					bubbleText = FightManager.Translate("bothTapInfoMessage");
					break;

				case FeedbackFXType.Swipe_Forward:
					bubbleMessage = InfoBubbleMessage.SwipeRight;
					bubbleHeading = FightManager.Translate("swipeRightInfoHeading");
					bubbleText = FightManager.Translate("swipeRightInfoMessage");
					break;

				case FeedbackFXType.Swipe_Back:
					bubbleMessage = InfoBubbleMessage.SwipeLeft;
					bubbleHeading = FightManager.Translate("swipeLeftInfoHeading");
					bubbleText = FightManager.Translate("swipeLeftInfoMessage");
					break;

				case FeedbackFXType.Swipe_Up:
					bubbleMessage = InfoBubbleMessage.SwipeUp;
					bubbleHeading = FightManager.Translate("swipeUpInfoHeading");
					bubbleText = FightManager.Translate("swipeUpInfoMessage");
					break;

				case FeedbackFXType.Swipe_Down:
					bubbleMessage = InfoBubbleMessage.SwipeDown;
					bubbleHeading = FightManager.Translate("swipeDownInfoHeading");
					bubbleText = FightManager.Translate("swipeDownInfoMessage");
					break;

				case FeedbackFXType.Swipe_Vengeance:
					bubbleMessage = InfoBubbleMessage.SwipeVengeance;
					bubbleHeading = FightManager.Translate("swipeVengeanceInfoHeading");
					bubbleText = FightManager.Translate("swipeVengeanceInfoMessage");
					break;

				default:
					bubbleMessage = InfoBubbleMessage.None;
					break;
			}

			if (bubbleMessage != InfoBubbleMessage.None && bubbleHeading != "" && !FightManager.WasInfoBubbleMessageRead(bubbleMessage))
				StartCoroutine(ShowInfoBubble(Vector3.zero, bubbleMessage, bubbleHeading, bubbleText, bubbleImage, false));
		}


//		private void MenuInfoBubble(MenuType newMenu, bool canGoBack, bool canPause, bool coinsVisible, bool kudosVisible)
//		{
//			InfoBubbleMessage bubbleMessage = InfoBubbleMessage.None;
//			string bubbleHeading = "";
//			string bubbleText = "";
//			Sprite bubbleImage = FightModeSprite;
//
//			switch (newMenu)
//			{
//				case MenuType.ArcadeFighterSelect:
//					bubbleMessage = InfoBubbleMessage.ArcadeMenu;
//					bubbleHeading = FightManager.Translate("arcadeMode");
//					bubbleText = FightManager.Translate("arcadeMenuInfoMessage");
//					break;
//
//				case MenuType.SurvivalFighterSelect:
//					bubbleMessage = InfoBubbleMessage.SurvivalMenu;
//					bubbleHeading = FightManager.Translate("survivalMode");
//					bubbleText = FightManager.Translate("survivalMenuInfoMessage");
//					break;
//
//				case MenuType.TeamSelect:
//					bubbleMessage = InfoBubbleMessage.ChallengeMenu;
//					bubbleHeading = FightManager.Translate("challengeMode");
//					bubbleText = FightManager.Translate("challengeMenuInfoMessage");
//					break;
//
//				case MenuType.Dojo:
//					bubbleMessage = InfoBubbleMessage.DojoMenu;
//					bubbleHeading = FightManager.Translate("dojo");
//					bubbleText = FightManager.Translate("dojoMenuInfoMessage");
//					break;
//
//				case MenuType.Facebook:
//					bubbleMessage = InfoBubbleMessage.FacebookMenu;
//					bubbleHeading = FightManager.Translate("friends");
//					bubbleText = FightManager.Translate("facebookMenuInfoMessage");
//					break;
//
//				case MenuType.Leaderboards:
//					bubbleMessage = InfoBubbleMessage.LeaderboardsMenu;
//					bubbleHeading = FightManager.Translate("leaderBoards");
//					bubbleText = FightManager.Translate("leaderboardsMenuInfoMessage");
//					break;
//
//				case MenuType.None:
//				case MenuType.Combat:
//				case MenuType.MatchStats:
//				case MenuType.PauseSettings:
//				case MenuType.ModeSelect:
//				case MenuType.WorldMap:
//				case MenuType.Advert:
//				default:
//					bubbleMessage = InfoBubbleMessage.None;
//					break;
//			}
//
//			if (bubbleMessage != InfoBubbleMessage.None && bubbleHeading != "" && !FightManager.WasInfoBubbleMessageRead(bubbleMessage))
//				StartCoroutine(ShowInfoBubble(Vector3.zero, bubbleMessage, bubbleHeading, bubbleText, bubbleImage, false, menuBubbleDelay));
//		}


//		private void CombatModeInfoBubble(FightMode fightMode)
//		{
////			if (!fightManager.ReadyToFight)			// no point if not ready
////				return;
//			
//			InfoBubbleMessage bubbleMessage = InfoBubbleMessage.None;
//			string bubbleHeading = "";
//			string bubbleText = "";
//			Sprite bubbleImage = FightModeSprite;
//
//			switch (fightMode)
//			{
//				case FightMode.Dojo:
//					bubbleMessage = InfoBubbleMessage.DojoCombat;
//					bubbleHeading = FightManager.Translate("dojo");
//					bubbleText = FightManager.Translate("dojoFightInfoMessage");
//					break;
//
//				case FightMode.Training:
//					bubbleMessage = InfoBubbleMessage.TrainingCombat;
//					bubbleHeading = FightManager.Translate("ninjaSchool");
//					bubbleText = FightManager.Translate("trainingFightInfoMessage");
//					break;
//
//				case FightMode.Arcade:
//					bubbleMessage = InfoBubbleMessage.ArcadeCombat;
//					bubbleHeading = FightManager.Translate("arcadeMode");
//					bubbleText = FightManager.Translate("arcadeFightInfoMessage");
//					break;
//
//				case FightMode.Survival:
//					bubbleMessage = InfoBubbleMessage.SurvivalCombat;
//					bubbleHeading = FightManager.Translate("survivalMode");
//					bubbleText = FightManager.Translate("survivalFightInfoMessage");
//					break;
//
//				case FightMode.Challenge:
//					bubbleMessage = InfoBubbleMessage.ChallengeCombat;
//					bubbleHeading = FightManager.Translate("challengeMode");
//					bubbleText = FightManager.Translate("challengeFightInfoMessage");
//					break;
//
//				default:
//					bubbleMessage = InfoBubbleMessage.None;
//					break;
//			}
//
//			if (bubbleMessage != InfoBubbleMessage.None && bubbleHeading != "" && !FightManager.WasInfoBubbleMessageRead(bubbleMessage))
//				StartCoroutine(ShowInfoBubble(Vector3.zero, bubbleMessage, bubbleHeading, bubbleText, bubbleImage, false));
//		}

		private void CrystalInfoBubble(int newGauge)
		{
			if (!fightManager.ReadyToFight)			// no point if not ready
				return;

			if (FightManager.CombatMode == FightMode.Training)
				return;
			
			InfoBubbleMessage message = InfoBubbleMessage.Crystals;
			string bubbleHeading = FightManager.Translate("crystalInfoHeading");
			string bubbleText = FightManager.Translate("crystalInfoMessage");
			Sprite bubbleImage = CrystalSprite;

			if (! FightManager.WasInfoBubbleMessageRead(message))
				StartCoroutine(ShowInfoBubble(Vector3.zero, message, bubbleHeading, bubbleText, bubbleImage, true));
		}

		private void ShoveInfoBubble()
		{
			if (!fightManager.ReadyToFight)			// no point if not ready
				return;

			if (FightManager.CombatMode == FightMode.Training)
				return;

			InfoBubbleMessage message = InfoBubbleMessage.SwipeDown;
			string bubbleHeading = FightManager.Translate("swipeDownInfoHeading");
			string bubbleText = FightManager.Translate("swipeDownInfoMessage");
			Sprite bubbleImage = swipeDownSprite;

			if (! FightManager.WasInfoBubbleMessageRead(message))
				StartCoroutine(ShowInfoBubble(Vector3.zero, message, bubbleHeading, bubbleText, bubbleImage, true));
		}


		private void SwipeCountInfoBubble(int swipeCount)
		{
			if (swipeCount <= 1)
				return;

			if (! trainer.PromptingForInput)
				return;
			
			InfoBubbleMessage message = InfoBubbleMessage.None;
			string bubbleHeading = "";
			string bubbleText = "";
			Sprite bubbleImage = GestureSprite(trainer.CurrentStep.GestureFX);

			switch (trainer.CurrentStep.GestureFX)
			{
				case FeedbackFXType.Swipe_Forward:
				case FeedbackFXType.Swipe_Back:
				case FeedbackFXType.Swipe_Up:
				case FeedbackFXType.Swipe_Down:
					if (swipeCount > 1)
					{
						message = InfoBubbleMessage.SwipeOnce;
						bubbleHeading = FightManager.Translate("swipeOnceInfoHeading");
						bubbleText = FightManager.Translate("swipeOnceInfoMessage");
					}
					break;

				case FeedbackFXType.Swipe_Vengeance:
					if (swipeCount > 2)
					{
						message = InfoBubbleMessage.SwipeTwice;
						bubbleHeading = FightManager.Translate("swipeTwiceInfoHeading");
						bubbleText = FightManager.Translate("swipeTwiceInfoMessage");
						bubbleImage = swipeVengeanceSprite;
					}
					break;

				default:
					return;
			}

			if (! FightManager.WasInfoBubbleMessageRead(message))
				StartCoroutine(ShowInfoBubble(Vector3.zero, message, bubbleHeading, bubbleText, bubbleImage, false));
		}

//		private void CanContinueBubble()
//		{
//			InfoBubbleMessage message = InfoBubbleMessage.CanContinue;
//			string bubbleHeading = FightManager.Translate("canContinueInfoHeading");
//			string bubbleText = FightManager.Translate("canContinueInfoMessage");
//			Sprite bubbleImage = OkSprite;
//
//			if (! FightManager.WasInfoBubbleMessageRead(message))
//				StartCoroutine(ShowInfoBubble(Vector3.zero, message, bubbleHeading, bubbleText, bubbleImage, false));
//		}


		private IEnumerator ShowInfoBubble(Vector3 position, InfoBubbleMessage message, string bubbleHeading, string bubbleText, Sprite bubbleSprite, bool freezeFight, float delayTime = 0)
		{
			if (!FightManager.SavedGameStatus.ShowInfoBubbles)
				yield break;

			if (FightManager.IsNetworkFight)
				yield break;

			if (message != InfoBubbleMessage.None && message == CurrentInfoBubbleMessage)		// no change
				yield break;

			if (fightManager.BlockInfoBubble)
				yield break;

			// wait at least minTicksBetweenBubbles
			var nowTicks = DateTime.Now.Ticks;
			if (nowTicks - lastInfoBubbleTicks < minTicksBetweenBubbles)
				yield break;

			if (animatingBubble || InfoBubbleShowing)				// busy - will have to wait till next time!
				yield break;

			CurrentInfoBubbleMessage = message;

			if (position != Vector3.zero)		// default position
				InfoBubble.transform.localPosition = position;

			float t = 0;
			Vector3 startScale = Vector3.zero;
			Vector3 targetScale = Vector3.one;
			Vector3 overScale = new Vector3(1.15f, 1.15f, 1.15f);

			InfoBubble.transform.localScale = startScale;

			if (OnInfoBubble != null)
				OnInfoBubble(message, true, freezeFight);			// freeze fight

			InfoBubbleHeading.text = bubbleHeading;
			InfoBubbleText.text = bubbleText;
			InfoBubbleImage.sprite = bubbleSprite;
			InfoBubbleImage.gameObject.SetActive(bubbleSprite != null);

			animatingBubble = true;			// stops further bubbles

			// pause before showing bubble
			if (delayTime > 0)
				yield return new WaitForSeconds(delayTime);

			InfoBubble.gameObject.SetActive(true);

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / bubbleTime); 
				InfoBubble.transform.localScale = Vector3.Lerp(startScale, overScale, t);
				InfoBubble.color = Color.Lerp(Color.clear, bubbleColour, t);
				InfoBubbleImage.color = Color.Lerp(Color.clear, Color.white, t);
				yield return null;
			}

			if (bubbleSound != null)
				AudioSource.PlayClipAtPoint(bubbleSound, Vector3.zero, FightManager.SFXVolume);

			t = 0;
			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / bubbleOverTime); 
				InfoBubble.transform.localScale = Vector3.Lerp(overScale, targetScale, t);
				yield return null;
			}
				
			// pause to allow message to be seen without accidentally tapping it away!
			yield return new WaitForSeconds(infoBubblePause);

			animatingBubble = false;
			InfoBubbleShowing = true;

			bubbleShowing = message;
			yield return null;
		}

		private IEnumerator HideInfoBubble()
		{
			if (!InfoBubbleShowing)
				yield break;

			while (animatingBubble)
				yield return null;

			animatingBubble = true;

			float t = 0;
			Vector3 startScale = Vector3.one;
			Vector3 targetScale = Vector3.zero;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / bubbleTime); 
//				bubble.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
				InfoBubble.color = Color.Lerp(bubbleColour, Color.clear, t);
				InfoBubbleImage.color = Color.Lerp(Color.white, Color.clear, t);
				yield return null;
			}

			InfoBubbleHeading.text = "";
			InfoBubbleText.text = "";
			InfoBubbleImage.sprite = null;
			InfoBubble.gameObject.SetActive(false);

			InfoBubbleShowing = false;
			animatingBubble = false;

			if (OnInfoBubble != null)
				OnInfoBubble(CurrentInfoBubbleMessage, false, false);			// unfreeze fight

			CurrentInfoBubbleMessage = InfoBubbleMessage.None;
			yield return null;
		}

		#endregion
	}
}
