
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace FightingLegends
{
	// class to handle the training of a fighter
	// queues training steps and records when they are completed
	public class Trainer : MonoBehaviour
    {
		public Queue<TrainingStep> TrainingScript;		// FIFO
		public TrainingStep CurrentStep { get { return (TrainingScript == null || TrainingScript.Count == 0) ? null : TrainingScript.Peek(); } }
		public bool CurrentStepIsCombo { get { return (CurrentStep != null && CurrentStep.Combo != null && CurrentStep.Combo.CurrentComboStep != null); } }
		public bool CurrentStepIsTrafficLight { get { return (CurrentStep != null && CurrentStep.IsTrafficLightStep); }} // CurrentStep.TrafficLightColour != TrafficLight.None); } }
		public bool CurrentStepIsNarrativeOnly { get { return CurrentStepIsTrafficLight; } }

		private Fighter fighter;
		private FightManager fightManager;
		private FeedbackUI feedbackUI;
		private TrainingUI trainingUI;

		private float narrativePause = 7.5f; // 5.0f;	// to allow time to read
		private float narrativeFadeTime = 0.25f;		// fade out after pause
		private float imagePulseTime = 0.25f;			// narrative image
		private float imagePulseScale = 2.0f;			// narrative image

//		private const string trafficLightRedHeader = "RED = MOVE NOT POSSIBLE";
//		private const string trafficLightRed = "Whilst you are under attack\n or already executing a move.";
//		private const string trafficLightGreenHeader = "GREEN = EXECUTE A MOVE!";
//		private const string trafficLightGreen = "Your opportunity to attack or block!\nA green flash indicates successful input!";
//		private const string trafficLightYellowHeader = "YELLOW = RESET AVAILABLE!";
//		private const string trafficLightYellow = "Escape an attack to strike back or cancel\na move and follow-up to extend a hit combo!";
	
//		private const string ninjaAtTheBeach = "What's a Ninja doing here at the beach!?\nLooks like she's not backing down!";
//		private const string blockNinja = "Take THAT!\nI guess it's your turn!";
//		private const string coolMoves = "Get ready!!\nI'll show you some COOL moves!";
//		private const string tryItAgain = "That was GREAT!\nLet's try it again!";
//		private const string offBalance = "She blocked the whole thing?\nI have to get her off balance!";
//		private const string reallyMad = "Now she looks REALLY mad!";
////		private const string ouchGauge = "Ouch!! I guess when we take damage\nthose gems below our health bar build up\nand give us access to other moves!";
//		private const string ouchGauge = "OUCH!! So when we take damage those\ncrystals light up to enable other moves..!";
//		private const string comesAgain = "Here she comes again!";
//		private const string vengeanceGauge = "I have enough crystals to perform a\nvengeance attack! Let's FINISH THIS!!";

//		private const string specialExtraComboNarrative = "Now practice your TIMING with\na COMBO at full speed!";
//		private const string resetCounterComboNarrative = "Follow-up a RESET with a COUNTER ATTACK\nwhen you have enough crystals!";

//		private const string tapLight = "TAP SCREEN TO ATTACK!";
//		private const string tapMedium = "TAP AGAIN TO FOLLOW UP!";
//		private const string tapHeavy = "TAP ONE MORE TIME\nFOR A HEAVY HIT!";
//		private const string holdBlock = "HOLD DOWN TO BLOCK!";
//		private const string swipeSpecial = "SWIPE FORWARD\nFOR A SPECIAL ATTACK!";
////		private const string trySpecialAgain = "TRY ANOTHER SPECIAL ATTACK!";
//		private const string swipeSpecialExtra = "SWIPE FORWARD AGAIN\nFOR A FINAL SMASH!";
////		private const string swipeShove = "SWIPE DOWN TO SHOVE!";
////		private const string swipeShove = "SWIPE DOWN TO SHOVE\nOPPONENT OUT OF BLOCK!";
//		private const string swipeShove = "SHE BLOCKED IT!\nSWIPE DOWN TO SHOVE!";
//		private const string specialAgain = "ATTACK AGAIN WHILE\nSHE IS OFF BALANCE!";
//		private const string tapLightShoveStun = "TAP TO STRIKE WHILE\nTHE OPPONENT IS OFF BALANCE!";
//		private const string tapMediumShoveStun = "TAP AGAIN FOR A SECOND IMPACT!";
//		private const string tapHeavyShoveStun = "TAP ONE MORE TIME\nFOR A THIRD STRIKE!";
////		private const string swipeChainSpecial = "YOU CAN SWIPE FORWARD HALFWAY THROUGH\nYOUR SECOND OR THIRD STRIKE... TRY IT!";
//		private const string swipeChainSpecial = "YOU CAN SWIPE FORWARD HALFWAY\nTHROUGH YOUR 2nd OR 3rd STRIKE...";
//		private const string swipeCounter = "SWIPE BACK TO COUNTER!";
//		private const string swipeVengeance = "SWIPE BACK AND FORWARD\nFOR A VENGEANCE ATTACK!";

//		private const string tryAgain = "TRY THAT AGAIN!";
			
		public bool IsFrozen { get; private set; }

		private const float successOffset = 120.0f;		// to prevent centred feedback from obscuring fighters
		private const float feedbackYOffset = 0; // -100.0f;	// over fighters' legs
		private const float feedbackPause = 0.75f;		// pause at end of each prompt feedback loop 

		private const float trainingCompletePause = 1.5f;

		private bool narrativeShowing = false;			// tap to hide narrative
		private bool readNarrative = false;				// tap to hide narrative
		private Color narrativeColour;					// narrative panel background
	
		private float comboStartHealth = 0;				// health saved at start of combo - restored if combo reset
		private int comboStartGauge = 0;				// gauge saved at start of combo - restored if combo reset
		private float comboStartAIHealth = 0;			// AI health saved at start of combo - restored if combo reset
		private int comboStartAIGauge = 0;				// AI gauge saved at start of combo - restored if combo reset

		private const int ResetCounterGauge = 3;		// gauge for reset and counter (1 + 2) - should really come from ProfileData

		private bool trafficLightTraining = true;
		private bool shortBasicTraining = true;
		private bool verboseBasicTraining = false;		// original long script
		private bool LMHCombo = true;
		private bool resetCounterCombo = true;

		private const int trafficLightStepFlashes = 3;	// to emphasise traffic light steps

		public const string LMHComboName = "L-M-H-S-E Combo";
		public const string ResetComboName = "Reset-Counter Combo";

		// true at start of step, false on valid input, until start of next step, etc
		// note: also set to true when combo step activated
		private bool promptingForInput = false;
		public bool PromptingForInput				
		{
			get { return promptingForInput; }
			private set
			{
				//				if (promptingForInput == value)
				//					return;

				var valueChanged = promptingForInput != value;
				promptingForInput = value;

				if (CurrentStep == null)
					return;

				bool setTrafficLight = promptingForInput || CurrentStep.GestureFX == FeedbackFXType.None; // || (CurrentStepIsCombo && !CurrentStep.Combo.CurrentComboStep.IsAIMove);
				TrafficLight stepTrafficLight = TrafficLight.None;

				if (CurrentStepIsCombo)
				{
					stepTrafficLight = CurrentStep.Combo.CurrentComboStep.WaitingForInput ? CurrentStep.Combo.CurrentComboStep.TrafficLightColour : CurrentStep.TrafficLightColour;

					// trigger feedback for activated combo step
					FeedbackFXType comboStepGestureFX = CurrentStep.Combo.CurrentComboStep.GestureFX;

					if (promptingForInput && valueChanged && comboStepGestureFX != FeedbackFXType.None)
					{
						fighter.TriggerFeedbackFX(comboStepGestureFX, false, feedbackYOffset);		// loop until valid input
						SplatOrPaintStroke(comboStepGestureFX);
					}
				}
				else
				{
					stepTrafficLight = CurrentStep.TrafficLightColour;
				}
			}
		}	
			
		public delegate void OnFailedInputDelegate(FailedInput failedInput);
		public OnFailedInputDelegate OnFailedInput;
			
		public delegate void ComboStartedDelegate(TrainingCombo combo);
		public ComboStartedDelegate OnComboStarted;			// relay event from TrainingCombo

		public delegate void ComboUpdatedDelegate(TrainingCombo combo);
		public ComboUpdatedDelegate OnComboUpdated;			// relay event from TrainingCombo

		public delegate void ComboCompletedDelegate(TrainingCombo combo);
		public ComboCompletedDelegate OnComboCompleted;		// relay event from TrainingCombo

		public delegate void ComboRestartDelegate(TrainingCombo combo, bool reachedMaxFailures);
		public ComboRestartDelegate OnComboRestart;			// relay event from TrainingCombo

//		public delegate void PromptingForInputDelegate(bool promptingForInput);
//		public PromptingForInputDelegate OnPromptForInput;			// for GameUI traffic lights

//		public delegate void TrafficLightEnabledDelegate();
//		public TrafficLightEnabledDelegate OnTrafficLightEnabled;
//
//		public delegate void TrafficLightDelegate(TrafficLight colour, int flashes = 0, bool stars = false);
//		public TrafficLightDelegate SetTrafficLightColour;

		public delegate void ComboStepVerticalFlashDelegate(ComboStep step, int stepUIIndex, bool enabled);
		public ComboStepVerticalFlashDelegate OnComboVerticalFlash;

		public delegate void SplatDelegate();
		public SplatDelegate OnTriggerSplat;

		public delegate void HideSplatDelegate();
		public HideSplatDelegate OnHideSplat;

		public delegate void PaintStrokeDelegate(bool flip);
		public PaintStrokeDelegate OnTriggerPaintStroke;

		public delegate void HidePaintStrokeDelegate();
		public HidePaintStrokeDelegate OnHidePaintStroke;

//		public delegate void ComboStepExpiredDelegate(ComboStep step);
//		public ComboStepExpiredDelegate OnComboStepExpired;			// relay event from TrainingCombo

		// 'constructor'
		private void Awake()
		{
			fighter = GetComponent<Fighter>();

			var fightManagerObject = GameObject.Find("FightManager");
			fightManager = fightManagerObject.GetComponent<FightManager>();

			var feedbackUIObject = GameObject.Find("FeedbackUI");
			if (feedbackUIObject != null)
				feedbackUI = feedbackUIObject.GetComponent<FeedbackUI>();

			var trainingUIObject = GameObject.Find("TrainingUI");
			if (trainingUIObject != null)
				trainingUI = trainingUIObject.GetComponent<TrainingUI>();

			narrativeColour = new Color(0, 0, 0, 0.5f);
		}


		private void OnEnable()
		{
			if (FightManager.SavedGameStatus.CompletedBasicTraining)
				return;
			
			// subscribe to fighter changed events
			fighter.OnStateStarted += OnStateStart;
			fighter.OnStateEnded += OnStateEnd;
			fighter.OnLastHit += OnLastHit;

			FightManager.OnQuitFight += OnQuitFight;
				
			if (feedbackUI != null)
			{
//				feedbackUI.feedbackFX.OnStartState += FeedbackStateStart;
				feedbackUI.feedbackFX.OnEndState += FeedbackStateEnd;	// start next step
			}
		}

		private void OnDisable()
		{
			if (FightManager.SavedGameStatus.CompletedBasicTraining)
				return;

			fighter.OnStateStarted -= OnStateStart;
			fighter.OnStateEnded -= OnStateEnd;
			fighter.OnLastHit -= OnLastHit;

			FightManager.OnQuitFight -= OnQuitFight;

			if (feedbackUI != null)
			{
//				feedbackUI.feedbackFX.OnStartState -= FeedbackStateStart;
				feedbackUI.feedbackFX.OnEndState -= FeedbackStateEnd;
			}
		}


		private void Update() 
		{
			if (narrativeShowing && !readNarrative && ((Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began) || Input.GetMouseButtonDown(0)))	// left button
			{
				readNarrative = true;			// tap to hide narrative
			}
		}

		public void StartTraining()
		{
			if (fightManager == null)
				return;

			if (FightManager.SavedGameStatus.CompletedBasicTraining)
				return;
			
			if (fighter.UnderAI)
				return;

//			Debug.Log("StartTraining: " + fighter.FullName);

			fighter.InTraining = true;
			trainingUI.EnableSkip(true);

			PopulateScript();
			CombosStartListening();				// any combos in the training script listen to fighter state start events

			StartCoroutine(PromptForInput());
		}


		public void StopTraining()
		{
//			Debug.Log("StopTraining: " + fighter.FullName);

			CleanupCombo();

			if (TrainingScript != null)
				TrainingScript.Clear();

			trainingUI.EnableSkip(false);

//			if (SetTrafficLightColour != null)
//				SetTrafficLightColour(TrafficLight.None);			// GameUI
			
			UnFreezeTraining();	
			CancelPrompt();
			fighter.InTraining = false;

			CombosStopListening();				// any combos in the training script stop listening to fighter state events
		}


		public bool ValidateMove(Move move)
		{
			if (! fighter.InTraining)
				return false;

//			Debug.Log("ValidateMove: " + move + ", PromptingForInput = " + PromptingForInput + ", InputReceived = " + CurrentStep.InputReceived);

			if (! PromptingForInput)
				return false;

			if (CurrentStep == null)
				return false;

			if (CurrentStep.InputReceived)
				return false;

			bool moveOk = false;
				
			if (CurrentStepIsCombo && CurrentStep.Combo.comboActive)
			{
				var combo = CurrentStep.Combo;
				var currentComboStep = combo.CurrentComboStep;
//				Debug.Log("ValidateMove: COMBO " + combo.ComboName + ", move = " + move + ", ComboMove = " + currentComboStep.ComboMove + ", WaitingForInput = " + currentComboStep.WaitingForInput);

				moveOk = currentComboStep.WaitingForInput && move == currentComboStep.ComboMove && fighter.CheckGauge(move);

//				Debug.Log("ValidateMove: COMBO " + combo.ComboName + ", move = " + move + ", moveOk = " + moveOk);

				if (moveOk)
					combo.CompleteCurrentStep(true);
			}
			else
			{
				// check to see if the move matches the gesture for the current step
				switch (CurrentStep.GestureFX)
				{
					case FeedbackFXType.Hold:
						moveOk = (move == Move.Block);
						break;

					case FeedbackFXType.Press:
						moveOk = (move == Move.Strike_Light || move == Move.Strike_Medium || move == Move.Strike_Heavy);
						break;

					case FeedbackFXType.Press_Both:
						moveOk = (move == Move.Roman_Cancel);
						break;

					case FeedbackFXType.Swipe_Forward:
						moveOk = (move == Move.Special);
						break;

					case FeedbackFXType.Swipe_Back:
						moveOk = (move == Move.Counter);
						break;

					case FeedbackFXType.Swipe_Up:
						moveOk = (move == Move.Power_Up);
						break;

					case FeedbackFXType.Swipe_Down:
						moveOk = (move == Move.Shove);
						break;

					case FeedbackFXType.Swipe_Vengeance:
						moveOk = (move == Move.Vengeance);
						break;

					case FeedbackFXType.Mash:
						moveOk = false;				// special case for tap counts (special opportunity)
						break;

					case FeedbackFXType.None:
						moveOk = false;
						break;
				}

//				Debug.Log("ValidateMove: " + move + ", moveOk = " + moveOk);
				if (moveOk)
				{
					CurrentStep.InputReceived = true;
					CancelPrompt();

					if (fighter.IsBlockStunned)
						fighter.StopBlockStun();
				}
				else 	// log failed input
				{
					if (OnFailedInput != null)
						OnFailedInput(new FailedInput{ InputRequired = CurrentStep.GestureFX, InputEntered = move } );
				}
			}

			if (! moveOk)
				fightManager.MoveCuedFeedback(false);

			return moveOk;
		}
			

		private IEnumerator StartNextStep()
		{
			if (FightManager.SavedGameStatus.CompletedBasicTraining)
				yield break;
			
			if (fighter.UnderAI)
				yield break;

			TrainingScript.Dequeue();			// finished with this step, next step becomes CurrentStep

			if (CurrentStep == null)			// no more training steps
			{
//				Debug.Log("StartNextStep: no more steps!");
				ClearPrompt();
				yield break;
			}

//			Debug.Log("StartNextStep: " + fighter.FullName + " - " + CurrentStep.Title);
//			Debug.Log("-->>> \nSTART NEXT STEP: " + fighter.FullName + " '" + CurrentStep.Title + "' [" + CurrentStep.GestureFX +
//				"] CurrentState = " + fighter.CurrentState + ", FreezeState = " + CurrentStep.FreezeOnState +
//				", FreezeAtEnd = " + CurrentStep.FreezeAtEnd + ", AIStateFreeze = " + CurrentStep.FreezeOnAI);

			ClearPrompt();

			yield return StartCoroutine(PromptForInput());

			if (! PromptingForInput)
				UnFreezeTraining();		// as opposed to on valid input (until next step freeze hit frame / state)

			if (CurrentStep.ReleaseBlock)
			{
//				Debug.Log(fighter.FullName + ": ReleaseBlock - IsBlockStunned = " + fighter.IsBlockStunned + ", IsBlocking = " + fighter.IsBlocking);
				fighter.ReleaseBlock();		

				if (fighter.IsBlockStunned)
					fighter.StopBlockStun();
			}

			yield return null;
		}


		private void ClearPrompt()
		{
			StartCoroutine(DisplayNarrative("", "", null));
			fightManager.TrainingPrompt("");

			if (OnHideSplat != null)
				OnHideSplat();

			if (OnHidePaintStroke != null)
				OnHidePaintStroke();
		}

		private void SplatOrPaintStroke(FeedbackFXType feedback)
		{
			switch (feedback)
			{
				// left/right swipes use paint stroke
				case FeedbackFXType.Swipe_Back:
				case FeedbackFXType.Swipe_Forward:
				case FeedbackFXType.Swipe_Vengeance:
					if (OnTriggerPaintStroke != null)
						OnTriggerPaintStroke(feedback == FeedbackFXType.Swipe_Forward || feedback == FeedbackFXType.Swipe_Vengeance);

					if (OnHideSplat != null)
						OnHideSplat();
					break;

				// all others use splat
				default:
					if (OnTriggerSplat != null)
						OnTriggerSplat();

					if (OnHidePaintStroke != null)
						OnHidePaintStroke();
					break;
			}
		}

		private void CancelPrompt()
		{
//			Debug.Log("CancelPrompt: " + fighter.FullName);

			ClearPrompt();
			fightManager.CancelFeedbackFX();

			PromptingForInput = false;
		}
			

		// called at the start of each step
		private IEnumerator PromptForInput()
		{
			if (! fighter.InTraining)
				yield break;

			if (CurrentStep == null)
				yield break;

//			Debug.Log("PromptForInput: CurrentStep.ActivatesTrafficLights = " + CurrentStep.ActivatesTrafficLights);

//			if (OnTrafficLightEnabled != null && CurrentStep.ActivatesTrafficLights)
//				OnTrafficLightEnabled();

			if (CurrentStep.WaitSeconds > 0)
			{
//				if (SetTrafficLightColour != null)
//					SetTrafficLightColour(TrafficLight.None);				// turn lights off
				
				yield return new WaitForSeconds(CurrentStep.WaitSeconds);
			}

			PromptingForInput = false; 

			// show narrative for a few seconds, then fade
			yield return StartCoroutine(DisplayNarrative(CurrentStep.Narrative, CurrentStep.Header, CurrentStep.NarrativeSprite, CurrentStepIsCombo || CurrentStepIsTrafficLight,
												CurrentStepIsTrafficLight ? CurrentStep.TrafficLightColour : TrafficLight.None));
	
			fightManager.TrainingPrompt(CurrentStep.Prompt);
			PromptingForInput = CurrentStep.GestureFX != FeedbackFXType.None;

			if (PromptingForInput)
			{
				fighter.TriggerFeedbackFX(CurrentStep.GestureFX, false, feedbackYOffset);		// loop until valid input
				SplatOrPaintStroke(CurrentStep.GestureFX);
			}

			if (CurrentStepIsCombo)
			{
				CurrentStep.Combo.StartCombo(false);
				UnFreezeTraining();						// in case combo has no prompt	
			}

			if (CurrentStepIsNarrativeOnly) 				// traffic light steps consist of narrative only
				StartCoroutine(StartNextStep());
			
			yield return null;
		}


		private IEnumerator DisplayNarrative(string trainingNarrative, string trainingHeader, Sprite trainingImage, bool force = false, TrafficLight trafficLight = TrafficLight.None)
		{
			if (! FightManager.SavedGameStatus.ShowTrainingNarrative && ! force)
				yield break;
			
			if (trainingNarrative == null || feedbackUI == null)
				yield break;

			if (trainingNarrative == "")
			{
				feedbackUI.TrainingNarrative.text = "";
				feedbackUI.TrainingHeader.text = "";
				feedbackUI.TrainingDetails.text = "";
				feedbackUI.NarrativePanel.gameObject.SetActive(false);
				narrativeShowing = false;
				readNarrative = false;
			}
			else
			{
				var narrative = trainingNarrative.Replace("'", "\'");

				if (!string.IsNullOrEmpty(trainingHeader))
				{
					feedbackUI.TrainingHeader.text = trainingHeader;
					feedbackUI.TrainingDetails.text = narrative;
					feedbackUI.TrainingNarrative.text = "";
				}
				else
				{
					feedbackUI.TrainingNarrative.text = narrative;
					feedbackUI.TrainingHeader.text = "";
					feedbackUI.TrainingDetails.text = "";
				}

				// fade in
				StartCoroutine(FightManager.FadePanel(feedbackUI.NarrativePanel, narrativeFadeTime, false, null, narrativeColour));
				yield return StartCoroutine(FightManager.FadeText(feedbackUI.TrainingNarrative, narrativeFadeTime, false));

				// flash traffic light
//				if (trafficLight != TrafficLight.None && trafficLightStepFlashes > 0 && SetTrafficLightColour != null)
//					SetTrafficLightColour(trafficLight, trafficLightStepFlashes);

				// narrative image
				feedbackUI.TrainingImage.gameObject.SetActive(trainingImage != null);
				feedbackUI.TrainingImage.transform.localScale = Vector3.zero;
				feedbackUI.TrainingImage.sprite = trainingImage;

				if (trainingImage != null)
					yield return StartCoroutine(FightManager.PulseImage(feedbackUI.TrainingImage, imagePulseTime, imagePulseScale, false, fightManager.BlingSound));

				narrativeShowing = true;
				readNarrative = false;		// can tap to interrupt

				// display for narrativePause seconds unless tapped
//				float t = 0.0f;
//				while (t < 1.0f && !readNarrative)
//				{
//					t += Time.deltaTime * (Time.timeScale / narrativePause); 
//					yield return null;
//				}

				// display until tapped
				while (!readNarrative)
				{
					yield return null;
				}

				narrativeShowing = false;
				readNarrative = true;

				// fade out
				feedbackUI.TrainingImage.gameObject.SetActive(false);
				feedbackUI.TrainingImage.sprite = null;

				StartCoroutine(FightManager.FadeText(feedbackUI.TrainingNarrative, narrativeFadeTime, true));
				yield return StartCoroutine(FightManager.FadePanel(feedbackUI.NarrativePanel, narrativeFadeTime, true, null, narrativeColour));
			}

			yield return null;
		}
			

		public void TrainingComplete()
		{
			if (! FightManager.SavedGameStatus.CompletedBasicTraining)
			{
				FightManager.SavedGameStatus.CompletedBasicTraining = true;
				fightManager.TrainingCompleteKudos();
			}

			StopTraining();
		}


		private void FreezeTraining(bool successFX)
		{
			var opponent = fighter.Opponent;
//			var currentStep = fighter.InTraining ? CurrentStep : opponent.Trainer.CurrentStep;
//			Debug.Log("FREEZE! CurrentStep = '" + currentStep.Title + "' FreezeOnState = " + currentStep.FreezeOnState + ", FreezeAtEnd = " + currentStep.FreezeAtEnd + ", CurrentState = " + fighter.CurrentState);

			fightManager.FreezeFight(); 		// until Unfreeze
			IsFrozen = true;

			if (successFX) 		// starts next step at end of success FX
			{
				ClearPrompt();
//				CancelPrompt();
				fightManager.Success(successOffset); 	// next step at end of success fx
			}
			else 				// next step immediately
			{
				if (fighter.InTraining)
					StartCoroutine(StartNextStep());
				else			// AI
					StartCoroutine(opponent.Trainer.StartNextStep());
			}
		}
			
			
		public void UnFreezeTraining()
		{
			if (! fighter.InTraining)
				return;

			if (fighter.romanCancelFrozen)
				return;
			
			if (CurrentStep == null)
				return;

			// combo training steps do not unfreeze
			if (CurrentStepIsCombo && CurrentStep.Combo.comboActive)
				return;
			
			var aiOpponent = fighter.Opponent;

			fightManager.UnfreezeFight();
			IsFrozen = false;

			// return to default fighting distance if attacked
			fighter.RecoilFromAttack();
			aiOpponent.RecoilFromAttack();

			if (CurrentStep == null)
				return;

//			Debug.Log("UnFreezeTraining! CurrentStep = '" + CurrentStep.Title + "' CurrentState = " + fighter.CurrentState + " [" + fighter.AnimationFrameCount + "]");

			if (CurrentStep.AIMove != Move.None)
			{
//				Debug.Log("AIMove: " + CurrentStep.AIMove + ", AI IsIdle = " + aiOpponent.IsIdle);

				if (aiOpponent.IsIdle || CurrentStep.AIMove == Move.Roman_Cancel || CurrentStep.AIMove == Move.ReleaseBlock)
					aiOpponent.CueMove(CurrentStep.AIMove);
				else
					aiOpponent.CueContinuation(CurrentStep.AIMove);
			}
		}
			

		private void OnStateStart(FighterChangedData stateStartData) 				// Fighter.StateStartedDelegate signature
		{
			if (FightManager.SavedGameStatus.CompletedBasicTraining)
				return;

			var fighter = stateStartData.Fighter;
			var opponent = fighter.Opponent;
			var currentStep = fighter.InTraining ? CurrentStep : opponent.Trainer.CurrentStep;

			if (currentStep == null || currentStep.FreezeAtEnd)
				return;

			if (currentStep.FreezeOnHit)		// freeze on last hit supercedes freeze on state
				return;

			if (currentStep.FreezeOnAI != fighter.UnderAI)
				return;

//			Debug.Log("State START: " + fighter.FighterFullName + ", StartState = " + stateStartData.State + ", FreezeState = " + currentStep.FreezeState + ", AIStateFreeze = " + currentStep.AIStateFreeze);

			if (stateStartData.NewState == currentStep.FreezeOnState)
			{
//				Debug.Log("State START --> SUCCESS + FREEZE! " + fighter.FighterFullName + ", FreezeState = " + currentStep.FreezeOnState + ", AIStateFreeze = " + currentStep.AIStateFreeze + ", success = " + success);
				FreezeTraining(currentStep.SuccessOnFreeze);
			}
			else if (stateStartData.NewState == currentStep.NextStepOnState)
			{
//				Debug.Log(fighter.FullName + ": NextStepOnState = " + currentStep.NextStepOnState);
				StartCoroutine(StartNextStep());
			}
		}

		private void OnStateEnd(FighterChangedData stateEndData) 				// Fighter.StateEndedDelegate signature
		{
			if (FightManager.SavedGameStatus.CompletedBasicTraining)
				return;

			var fighter = stateEndData.Fighter;
			var opponent = fighter.Opponent;
			var currentStep = fighter.InTraining ? CurrentStep : opponent.Trainer.CurrentStep;

			if (currentStep == null || !currentStep.FreezeAtEnd)
				return;

			if (currentStep.FreezeOnHit)		// freeze on last hit supercedes freeze on state
				return;

			if (currentStep.FreezeOnAI != fighter.UnderAI)
				return;

			if (stateEndData.NewState == currentStep.FreezeOnState)
			{
//				Debug.Log("State END: " + fighter.FullName + ", StartState = " + stateEndData.State + ", AIStateFreeze = " + currentStep.FreezeOnAI);

//				Debug.Log("State END --> SUCCESS + FREEZE! " + fighter.FighterFullName + ", FreezeState = " + currentStep.FreezeOnState + ", AIStateFreeze = " + currentStep.AIStateFreeze + ", success = " + success);
				FreezeTraining(currentStep.SuccessOnFreeze);
			}
		}
	

		private void OnLastHit(FighterChangedData lastHitData)
		{
			if (FightManager.SavedGameStatus.CompletedBasicTraining)
				return;

			var fighter = lastHitData.Fighter;
			var opponent = fighter.Opponent;
			var currentStep = fighter.InTraining ? CurrentStep : opponent.Trainer.CurrentStep;

			if (currentStep == null)
				return;

			if (currentStep.FreezeOnAI != fighter.UnderAI)
				return;
			
			if (currentStep.FreezeOnState == lastHitData.NewState)
			{
//				Debug.Log("LAST HIT --> SUCCESS + FREEZE! " + fighter.FullName + ", State = " + lastHitData.State);
				FreezeTraining(currentStep.SuccessOnFreeze);
			}
		}
			
		// start next step at end of success FX
		private void FeedbackStateEnd(AnimationState endingState)
		{
			if (FightManager.SavedGameStatus.CompletedBasicTraining || ! fighter.InTraining)
				return;
			
			if (endingState.StateLabel == FeedbackFXType.Success.ToString().ToUpper())
			{
//				Debug.Log("FeedbackStateEnd: " + FeedbackFXType.Success.ToString());
				if (CurrentStep != null && !CurrentStepIsCombo) // ) // TODO: check this!! 
					StartCoroutine(StartNextStep());	// next step in queue (or completion if this is the last step)
			}
			else if (PromptingForInput)
			{
				if (CurrentStepIsCombo)
				{	
					fightManager.TrainingPrompt("");

					var stepGestureFX = CurrentStep.Combo.CurrentComboStep.GestureFX;

					if (stepGestureFX != FeedbackFXType.None)
						fighter.TriggerFeedbackFX(stepGestureFX, false, feedbackYOffset);			// loop until valid input
				}
				else if (CurrentStep != null)
					fighter.TriggerFeedbackFX(CurrentStep.GestureFX, false, feedbackYOffset);		// loop until valid input
			}
		}

		private void OnQuitFight()
		{
			StopTraining();		// cleanup
		}


		#region combos

		// set any combo steps to listen to this fighter for state changes etc.
		private void CombosStartListening()
		{
			if (!fighter.InTraining)
				return;

//			Debug.Log("Trainer: CombosStartListening " + fighter.FullName);

			if (TrainingScript == null)
				return;
			
			foreach (var step in TrainingScript)
			{
				if (step.Combo != null)
				{
					step.Combo.StartListening(fighter);

					step.Combo.OnComboStarted += ComboStarted;
					step.Combo.OnComboUpdated += ComboUpdated;
					step.Combo.OnComboRestart += ComboRestart;
					step.Combo.OnComboAIMove += ComboAIMove;
//					step.Combo.OnComboStepStarted += ComboStepStarted;
					step.Combo.OnComboStepActivated += ComboStepActivated;
//					step.Combo.OnComboStepTrafficLight += ComboStepTrafficLight;
//					step.Combo.OnComboStepVerticalFlash += ComboStepVerticalFlash;
					step.Combo.OnComboStepCompleted += ComboStepCompleted;
					step.Combo.OnComboStepExpired += ComboStepExpired;
					step.Combo.OnComboCompleted += ComboCompleted;
				}
			}
		}

		private void CombosStopListening()
		{
			if (!fighter.InTraining)
				return;
			
//			Debug.Log("Trainer: CombosStopListening " + fighter.FullName);

			if (TrainingScript == null)
				return;
			
			foreach (var step in TrainingScript)
			{
				if (step.Combo != null)
				{
					step.Combo.StopListening(fighter);

					step.Combo.OnComboStarted -= ComboStarted;
					step.Combo.OnComboUpdated -= ComboUpdated;
					step.Combo.OnComboRestart -= ComboRestart;
					step.Combo.OnComboAIMove -= ComboAIMove;
//					step.Combo.OnComboStepStarted -= ComboStepStarted;
					step.Combo.OnComboStepActivated -= ComboStepActivated;
//					step.Combo.OnComboStepTrafficLight -= ComboStepTrafficLight;
//					step.Combo.OnComboStepVerticalFlash -= ComboStepVerticalFlash;
					step.Combo.OnComboStepCompleted -= ComboStepCompleted;
					step.Combo.OnComboStepExpired -= ComboStepExpired;
					step.Combo.OnComboCompleted -= ComboCompleted;
				}
			}
		}

		private void ComboStarted(TrainingCombo combo)
		{
			if (!fighter.InTraining)
				return;

			var opponent = fighter.Opponent;

			// snapshot health and gauge to retore if combo reset
			comboStartHealth = fighter.ProfileData.SavedData.Health;
			comboStartAIHealth = opponent.ProfileData.SavedData.Health;
			comboStartGauge = fighter.ProfileData.SavedData.Gauge;
			comboStartAIGauge = opponent.ProfileData.SavedData.Gauge;

//			Debug.Log("Trainer: ComboStarted " + combo.ComboName);

			// relay event to listeners to this fighter trainer (eg UI)
			if (OnComboStarted != null)
				OnComboStarted(combo);
		}

		private void ComboUpdated(TrainingCombo combo)
		{
			if (!fighter.InTraining)
				return;
			
//			Debug.Log("Trainer: ComboUpdated " + combo.ComboName);

			// relay event to listeners to this fighter trainer (eg UI)
			if (OnComboUpdated != null)
				OnComboUpdated(combo);
		}

		private void ComboRestart(TrainingCombo combo, bool reachedMaxFailures)
		{
			if (!fighter.InTraining)
				return;
			
			PromptingForInput = false;		// ValidateMove will return false

			// restore both fighters' health and gauge as at start of combo
			RestoreFighterHealth(fighter);

			fighter.WrongFeedbackFX();

			// relay event to listeners to this fighter trainer - eg. UI
			if (OnComboRestart != null)
				OnComboRestart(combo, reachedMaxFailures);
		}

		// restore both fighters' health and gauge as at start of combo
		private void RestoreFighterHealth(Fighter fighter)
		{
			var fighterHealthChange = fighter.ProfileData.SavedData.Health - comboStartHealth;

			fighter.UpdateHealth(fighterHealthChange, false);
			fighter.UpdateGauge(comboStartGauge, true);

			// restore AI opponent
			var opponent = fighter.Opponent;
			var opponentHealthChange = opponent.ProfileData.SavedData.Health - comboStartAIHealth;

			if (opponentHealthChange == 0)
				opponent.UpdateGauge(comboStartAIGauge, true);
			else
				opponent.UpdateHealth(opponentHealthChange);
		}


		// make AI opponent execute the combo step move
		private void ComboAIMove(ComboStep step)
		{
			if (!fighter.InTraining)
				return;

			var opponent = fighter.Opponent;
			
			if (opponent.UnderAI && step.IsAIMove && step.ComboMove != Move.None)
			{
//				Debug.Log("Trainer: ComboAIMove " + step.ComboMove + " CanExecuteMove = " + opponent.CanExecuteMove(step.ComboMove) + " State = " + opponent.CurrentState + " CanContinue = " + opponent.CanContinue);

				if (opponent.CanExecuteMove(step.ComboMove))
				{	
					if (opponent.CanContinue)
						opponent.CueContinuation(step.ComboMove);
					else
						opponent.CueMove(step.ComboMove);
				}
				else
					fightManager.TrainingPrompt(opponent.FighterName.ToUpper() + " UNABLE TO " + step.ComboMove.ToString().ToUpper() + " [" + opponent.CurrentState + "]");	
			}
		}
			

		private void ComboStepActivated(ComboStep step)
		{
			if (!fighter.InTraining)
				return;

//			Debug.Log("ComboStepActivated " + step.ComboMove + " IsAIMove = " + step.IsAIMove);
			PromptingForInput = !step.IsAIMove;			// triggers traffic light / combo step feedback if appropriate
		}

//		private void ComboStepTrafficLight(TrafficLight colour)
//		{
//			if (SetTrafficLightColour != null)
//				SetTrafficLightColour(colour);
//		}

//		private void ComboStepVerticalFlash(ComboStep step, int stepUIIndex, bool enabled)
//		{
//			if (OnComboVerticalFlash != null)
//				OnComboVerticalFlash(step, stepUIIndex, enabled);
//		}

		private void ComboStepExpired(ComboStep step)
		{
			if (!fighter.InTraining)
				return;
			
//			PromptingForInput = false;
			CancelPrompt();

//			// relay event to listeners to this fighter trainer (eg UI)
//			if (OnComboStepExpired != null)
//				OnComboStepExpired(step);
		}

		private void ComboStepCompleted(ComboStep step)
		{
			if (!fighter.InTraining)
				return;

			PromptingForInput = false;
			ClearPrompt();			// displayed for first step only
		}

		private void ComboCompleted(TrainingCombo combo, bool success = true)
		{
			if (! fighter.InTraining)
				return;

			// relay event to listeners to this fighter trainer (eg clear UI)
			if (OnComboCompleted != null)
				OnComboCompleted(combo);

			if (success)
			{
				CancelPrompt();
				fightManager.Success(successOffset); 	// StartNextStep at end of success feedback
			}
			else
				StartCoroutine(StartNextStep());
		}
			
//		private void ComboCompleted(TrainingCombo combo, bool success = true)
//		{
//			if (! fighter.InTraining)
//				return;
//
//			StartCoroutine(StartNextStep());		// no more steps, so training complete
//
//			// relay event to listeners to this fighter trainer (eg UI)
//			if (OnComboCompleted != null)
//				OnComboCompleted(combo);
//			
//			// success!
//			if (success)
//				fightManager.Success(successOffset); 
//		}


		private void CleanupCombo()
		{
			// primarily to cleanup UI in case training was interrupted
			if (CurrentStep != null && CurrentStep.Combo != null)
				CurrentStep.Combo.CompleteCombo(false);
		}


		#endregion


		private void PopulateScript()
		{
			TrainingScript = new Queue<TrainingStep>();

			if (FightManager.SavedGameStatus.CompletedBasicTraining)
				return;

			if (fighter.UnderAI)
				return;

			if (verboseBasicTraining)
			{
				ConstructVerboseBasicTraining();		// ends with KO
			}
			else
			{
				ConstructShortBasicTraining();
//				ConstructRedGreenTrafficLightTraining();
				ConstructLMHCombo();
//				ConstructYellowTrafficLightTraining();
				ConstructResetCounterCombo();			// ends with KO
			}
		}


		private void ConstructRedGreenTrafficLightTraining()
		{
			if (! trafficLightTraining)
				return;

			TrainingScript.Enqueue(new TrainingStep {
				Title = "Green Light",
				Header = FightManager.Translate("greenLightHeader"),
//				Narrative = FightManager.Translate("greenLightNarrative"),
				NarrativeSprite = feedbackUI.OkSprite,
				IsTrafficLightStep = true,					// to explain traffic lights (with narrative)
				TrafficLightColour = TrafficLight.Green
			}
			);

			TrainingScript.Enqueue(new TrainingStep {
				Title = "Red Light",
				Header = FightManager.Translate("redLightHeader"),
//				Narrative = FightManager.Translate("redLightNarrative"),
				NarrativeSprite = feedbackUI.NotOkSprite,
				IsTrafficLightStep = true,					// to explain traffic lights (with narrative)
				TrafficLightColour = TrafficLight.Red
			}
			);
		}


		private void ConstructYellowTrafficLightTraining()
		{
			if (! trafficLightTraining)
				return;

			TrainingScript.Enqueue(new TrainingStep {
				Title = "Yellow Light",
				Header = FightManager.Translate("yellowLightHeader"),
//				Narrative = FightManager.Translate("yellowLightNarrative"), 
//				NarrativeSprite = feedbackUI.ResetSprite,
//				WaitSeconds = 2.0f,
				IsTrafficLightStep = true,					// to explain traffic lights (with narrative)
				TrafficLightColour = TrafficLight.Yellow
			}
			);

			TrainingScript.Enqueue(new TrainingStep {
				Title = "Left Arrow",
				Header = FightManager.Translate("leftArrowHeader"),
//				Narrative = FightManager.Translate("leftArrowNarrative"),
				NarrativeSprite = trainingUI.swipeBackSprite,
				IsTrafficLightStep = true,					// to explain traffic lights (with narrative)
				TrafficLightColour = TrafficLight.Left
			}
			);
		}
			
		private void ConstructShortBasicTraining()
		{
			if (! shortBasicTraining)
				return;
			
			// start light strike, freeze on hit frame, prompt to strike
			TrainingScript.Enqueue(new TrainingStep {
				Title = "Light Strike",
//				Narrative = FightManager.Translate("ninjaAtTheBeach"),
				Prompt = FightManager.Translate("tapLight"),
				GestureFX = FeedbackFXType.Press,
				FreezeOnState = State.Light_HitFrame,
			}
			);

			//  prompt to strike (follow up), start medium strike, freeze on hit frame
			TrainingScript.Enqueue(new TrainingStep {
				Title = "Medium Strike",
				Prompt = FightManager.Translate("tapMedium"),
				GestureFX = FeedbackFXType.Press,
				FreezeOnState = State.Medium_HitFrame,
			}
			);

			// prompt to strike (follow up), start heavy strike, freeze on hit frame 
			TrainingScript.Enqueue(new TrainingStep {
				Title = "Heavy Strike",
				Prompt = FightManager.Translate("tapHeavy"),
				GestureFX = FeedbackFXType.Press,
				FreezeOnState = State.Heavy_HitFrame,
			}
			);

			// next step (tutorial punch) triggered on return to idle
			TrainingScript.Enqueue(new TrainingStep {
				Title = "Idle then AI Tutorial Punch",
				NextStepOnState = State.Idle,
			}
			);

			// no prompt, AI starts tutorial punch, freezes at end of punch start
			TrainingScript.Enqueue(new TrainingStep {
				Title = "AI Tutorial Punch",
//				Narrative = FightManager.Translate("blockNinja"),
				AIMove = Move.Tutorial_Punch,
				FreezeOnState = State.Tutorial_Punch_Start,
				FreezeOnAI = true,
				FreezeAtEnd = true,
				SuccessOnFreeze = false,
			}
			);

			// prompt to block, start block idle, freeze on AI tutorial punch hit frame, release block at end
			TrainingScript.Enqueue(new TrainingStep {
				Title = "Block Tutorial Punch",
				Prompt = FightManager.Translate("holdBlock"),
				GestureFX = FeedbackFXType.Hold,
				FreezeOnHit = true,
				FreezeOnState = State.Tutorial_Punch,
				FreezeOnAI = true,
			}
			);

			// next step (special) triggered on return to idle
			TrainingScript.Enqueue(new TrainingStep {
				Title = "Idle then Special",
				ReleaseBlock = true,			// at start of step
				NextStepOnState = State.Idle,
			}
			);

			// prompt for special, start special, freeze on special last hit
			TrainingScript.Enqueue(new TrainingStep {
				Title = "Special",
//				Narrative = FightManager.Translate("coolMoves"),
				Prompt =FightManager.Translate("swipeSpecial"),
				//					Prompt = specialAgain,		// after AI block / shove
				GestureFX = FeedbackFXType.Swipe_Forward,
				FreezeOnHit = true,				// last special hit
				FreezeOnState = State.Special,	
			}
			);

			// advance to special opportunity because a special extra is not possible until then
			// unfrozen as no prompt, freeze at end of special opportunity
			TrainingScript.Enqueue(new TrainingStep {
				Title = "Special Opportunity",
				FreezeOnState = State.Special_Opportunity,
				FreezeAtEnd = true,
				SuccessOnFreeze = false,
			}
			);

			// prompt for special extra, trigger special extra, freeze on special extra last hit
			TrainingScript.Enqueue(new TrainingStep {
				Title = "Special Extra",
				Prompt = FightManager.Translate("swipeSpecialExtra"),
				GestureFX = FeedbackFXType.Swipe_Forward,
				FreezeOnHit = true,
				FreezeOnState = State.Special_Extra,	
			}
			);

			// next step (special) triggered on return to idle
			TrainingScript.Enqueue(new TrainingStep {
				Title = "Return to Idle",
				NextStepOnState = State.Idle,
			}
			);

			// no prompt, with enough gauge AI performs vengeance attack
			TrainingScript.Enqueue(new TrainingStep {
				Title = "AI Vengeance",
//				Narrative = FightManager.Translate("reallyMad"),
				AIMove = Move.Vengeance,
//				ActivatesTrafficLights = true,				// activates traffic light UI
//				TrafficLightColour = TrafficLight.Red,
				NextStepOnState = State.Idle,				// go to next step as soon as AI is idle
			}
			);

			// prompt for vengeance, AI strikes counter taunt and triggers counter attack
			TrainingScript.Enqueue(new TrainingStep {
				Title = "Vengeance",
//				Narrative = FightManager.Translate("vengeanceGauge"),
				Prompt = FightManager.Translate("swipeVengeance"),
				GestureFX = FeedbackFXType.Swipe_Vengeance,
				FreezeOnHit = true,
				FreezeOnState = State.Vengeance,
				SuccessOnFreeze = true,			
//				TrafficLightColour = TrafficLight.Green,		// TODO: animate vengeance left / right
			}
			);

			// next step (combo) triggered on return to idle
			TrainingScript.Enqueue(new TrainingStep {
				Title = "Idle then Combo 1",
				NextStepOnState = State.Idle,
			}
			);
		}

		private void ConstructLMHCombo()
		{
			if (! LMHCombo)
				return;
			
			TrainingScript.Enqueue(new TrainingStep {
				Title = "Special Extra Combo",
//				Narrative = FightManager.Translate("specialExtraComboNarrative"),
				TrafficLightColour = TrafficLight.None,

				Combo = new TrainingCombo {
					ComboName = LMHComboName,

					ComboSteps = new List<ComboStep> {
						new ComboStep {
							StepName = "LIGHT",
							ComboMove = Move.Strike_Light,
							ActivateState = State.Void, 			// first step automatically activated
							ExpiryState = State.Void, 				// no expiry
//							TrafficLightColour = TrafficLight.Green,
							GestureFX = FeedbackFXType.Press
						}, 

						new ComboStep {
							StepName = "MEDIUM",
							ComboMove = Move.Strike_Light,			// single finger tap - NOTE: not Strike_Medium!
							ActivateState = State.Light_HitFrame, 	// on start
							ExpiryState = State.Light_Cutoff, 		// on start
//							TrafficLightColour = TrafficLight.Green,
							GestureFX = FeedbackFXType.Press
						}, 

						new ComboStep {
							StepName = "HEAVY",
							ComboMove = Move.Strike_Light,			// single finger tap - NOTE: not Strike_Heavy!
							ActivateState = State.Medium_HitFrame, 	// on start
							ExpiryState = State.Medium_Cutoff, 		// on start
//							TrafficLightColour = TrafficLight.Green,
							GestureFX = FeedbackFXType.Press
						}, 

						new ComboStep {
							StepName = "SPECIAL",
							ComboMove = Move.Special,				// swipe right
							ActivateState = State.Heavy_HitFrame, 	// on start
							ExpiryState = State.Heavy_Cutoff, 		// on start
//							TrafficLightColour = TrafficLight.Green,
							GestureFX = FeedbackFXType.Swipe_Forward
						}, 

						new ComboStep {
							StepName = "EXTRA",
							ComboMove = Move.Special,					// swipe right
							ActivateState = State.Special_Opportunity, 	// on start
							ExpiryState = State.Special_Recover, 		// on start
//							TrafficLightColour = TrafficLight.Green,	// TODO: flash for fire element mash / right swipe for water
							GestureFX = FeedbackFXType.Swipe_Forward
						}, 
					},
				},
			}
			);
		}
			
		private void ConstructResetCounterCombo()
		{
			if (! resetCounterCombo)
				return;
			
			TrainingScript.Enqueue(new TrainingStep {
				Title = "Reset-Counter Combo",
//				Narrative = FightManager.Translate("resetCounterComboNarrative"),
//				NarrativeSprite = feedbackUI.SwipeLeftSprite,
				TrafficLightColour = TrafficLight.None,

				Combo = new TrainingCombo {
					ComboName = ResetComboName,

					ComboSteps = new List<ComboStep> {
						new ComboStep {
							StepName = "AI VENGEANCE",
							ComboMove = Move.Vengeance,
							IsAIMove = true,
							ActivateState = State.Void,				// first step automatically activated
//							TrafficLightColour = TrafficLight.Red		// forced
						}, 

						new ComboStep { 		// roman cancel activated on change from idle, expires on last hit of (AI) vengeance
							StepName = "RESET",
							ComboMove = Move.Roman_Cancel,			// two finger tap
							ActivateOnGauge = ResetCounterGauge,	// when enough gauge for reset + counter
							ExpiryState = State.Vengeance, 
							ExpiryAIState = true,
							ExpiryLastHit = true,
//							TrafficLightColour = TrafficLight.Yellow,	// forced
							GestureFX = FeedbackFXType.Press_Both
						}, 

						new ComboStep {	// counter activated on dash (during roman cancel freeze), expires at end of dash (freeze)
							StepName = "COUNTER",
							ComboMove = Move.Counter,				// left swipe
							ActivateState = State.Dash, 			// start roman cancel freeze
							ExpiryState = State.Dash,				// end roman cancel freeze
							ExpiryFromState = true,
//							TrafficLightColour = TrafficLight.Left,		// forced
							GestureFX = FeedbackFXType.Swipe_Back
						}, 

						//...  KO !!!
					},
				},
			}
			);
		}			

		#region verbose training

		private void ConstructVerboseBasicTraining()
		{
//			if (! verboseBasicTraining)
//				return;
//
//			// start light strike, freeze on hit frame, prompt to strike
//			TrainingScript.Enqueue(new TrainingStep
//			{
//				Title = "Light Strike",
//				Narrative = FightManager.Translate("ninjaAtTheBeach"),
//				Prompt = FightManager.Translate("tapLight"),
//				GestureFX = FeedbackFXType.Press,
//				FreezeOnState = State.Light_HitFrame,
//			}
//			);
//
//			//  prompt to strike (follow up), start medium strike, freeze on hit frame
//			TrainingScript.Enqueue(new TrainingStep
//			{
//				Title = "Medium Strike",
//				Prompt = FightManager.Translate("tapMedium"),
//				GestureFX = FeedbackFXType.Press,
//				FreezeOnState = State.Medium_HitFrame,
//			}
//			);
//
//			// prompt to strike (follow up), start heavy strike, freeze on hit frame 
//			TrainingScript.Enqueue(new TrainingStep
//			{
//				Title = "Heavy Strike",
//				Prompt = FightManager.Translate("tapHeavy"),
//				GestureFX = FeedbackFXType.Press,
//				FreezeOnState = State.Heavy_HitFrame,
//			}
//			);
//
//			// next step (tutorial punch) triggered on return to idle
//			TrainingScript.Enqueue(new TrainingStep
//			{
//				Title = "Idle then Tutorial Punch",
//				NextStepOnState = State.Idle,
//			}
//			);
//
//			// no prompt, AI starts tutorial punch, freezes at end of punch start
//			TrainingScript.Enqueue(new TrainingStep
//			{
//				Title = "AI Tutorial Punch",
//				Narrative = FightManager.Translate("blockNinja"),
//				AIMove = Move.Tutorial_Punch,
//				FreezeOnState = State.Tutorial_Punch_Start,
//				FreezeOnAI = true,
//				FreezeAtEnd = true,
//				SuccessOnFreeze = false,
//			}
//			);
//
//			// prompt to block, start block idle, freeze on AI tutorial punch hit frame, release block at end
//			TrainingScript.Enqueue(new TrainingStep
//			{
//				Title = "Block Tutorial Punch",
//				Prompt = FightManager.Translate("holdBlock"),
//				GestureFX = FeedbackFXType.Hold,
//				FreezeOnHit = true,
//				FreezeOnState = State.Tutorial_Punch,
//				FreezeOnAI = true,
//			}
//			);
//
//			// next step (special) triggered on return to idle
//			TrainingScript.Enqueue(new TrainingStep
//			{
//				Title = "Idle then Special",
//				ReleaseBlock = true,			// at start of step
//				NextStepOnState = State.Idle,
//			}
//			);
//
//			// prompt for special, start special, freeze on special last hit
//			TrainingScript.Enqueue(new TrainingStep
//			{
//				Title = "Special",
//				Narrative = FightManager.Translate("coolMoves"),
//				Prompt = FightManager.Translate("swipeSpecial"),
//				GestureFX = FeedbackFXType.Swipe_Forward,
//				FreezeOnHit = true,				// last special hit
//				FreezeOnState = State.Special,	
//			}
//			);
//
//			// advance to special opportunity because a special extra is not possible until then
//			// unfrozen as no prompt, freeze at end of special opportunity
//			TrainingScript.Enqueue(new TrainingStep
//			{
//				Title = "Special Opportunity",
//				FreezeOnState = State.Special_Opportunity,
//				FreezeAtEnd = true,
//				SuccessOnFreeze = false,
//			}
//			);
//
//			// prompt for special extra, trigger special extra, freeze on special extra last hit
//			TrainingScript.Enqueue(new TrainingStep
//			{
//				Title = "Special Extra",
//				Prompt = FightManager.Translate("swipeSpecialExtra"),
//				GestureFX = FeedbackFXType.Swipe_Forward,
//				FreezeOnHit = true,
//				FreezeOnState = State.Special_Extra,	
//			}
//			);
//
//			// next step (special) triggered on return to idle
//			TrainingScript.Enqueue(new TrainingStep
//			{
//				Title = "Return to Idle",
//				NextStepOnState = State.Idle,
//			}
//			);
//
//			// prompt again for special, start special, AI blocks, freeze on special last hit
//			TrainingScript.Enqueue(new TrainingStep
//			{
//				Title = "Blocked Special",
//				Narrative = FightManager.Translate("tryItAgain"),
//				Prompt = FightManager.Translate("swipeSpecial"),
//				GestureFX = FeedbackFXType.Swipe_Forward,
//				AIMove = Move.Block,
//				FreezeOnHit = true,				// last special hit
//				FreezeOnState = State.Special,	
//				SuccessOnFreeze = false,
//			}
//			);
//
//			// next step (shove) triggered on return to idle
//			TrainingScript.Enqueue(new TrainingStep
//			{
//				Title = "Return to Idle",
//				NextStepOnState = State.Idle,
//			}
//			);
//
//			// prompt for shove, AI still blocking, freeze on first frame of AI shove stun
//			TrainingScript.Enqueue(new TrainingStep
//			{
//				Title = "Blocked Special",
//				Narrative = FightManager.Translate("offBalance"),
//				Prompt = FightManager.Translate("swipeShove"),
//				GestureFX = FeedbackFXType.Swipe_Down,
//				FreezeOnState = State.Shove_Stun,	
//				FreezeOnAI = true,
//			}
//			);
//
//			// prompt for light strike, AI still shove stunned, freeze on hit frame
//			TrainingScript.Enqueue(new TrainingStep
//			{
//				Title = "Light Strike Shove Stun",
//				Prompt = FightManager.Translate("tapLightShoveStun"),
//				GestureFX = FeedbackFXType.Press,
//				FreezeOnHit = true,	
//				FreezeOnState = State.Light_HitFrame,	
//			}
//			);
//
//			// prompt for medium strike, AI still shove stunned, freeze on hit frame
//			TrainingScript.Enqueue(new TrainingStep
//			{
//				Title = "Medium Strike Shove Stun",
//				Prompt = FightManager.Translate("tapMediumShoveStun"),
//				GestureFX = FeedbackFXType.Press,
//				FreezeOnHit = true,	
//				FreezeOnState = State.Medium_HitFrame,	
//			}
//			);
//
//			// prompt for heavy strike, AI still shove stunned, releases block hold, freeze on hit frame
//			TrainingScript.Enqueue(new TrainingStep
//			{
//				Title = "Heavy Strike Shove Stun",
//				Prompt = FightManager.Translate("tapHeavyShoveStun"),
//				GestureFX = FeedbackFXType.Press,
//				AIMove = Move.ReleaseBlock,
//				FreezeOnHit = true,	
//				FreezeOnState = State.Heavy_HitFrame,	
//			}
//			);
//
//			// prompt for special, start special, freeze on special last hit
//			TrainingScript.Enqueue(new TrainingStep
//			{
//				Title = "Chain Special",
//				Prompt = FightManager.Translate("swipeChainSpecial"),
//				GestureFX = FeedbackFXType.Swipe_Forward,
//				FreezeOnHit = true,				// last special hit
//				FreezeOnState = State.Special,	
//			}
//			);
//
//			// advance to special opportunity because a special extra is not possible until then
//			// unfrozen as no prompt, freeze at end of special opportunity
//			TrainingScript.Enqueue(new TrainingStep
//			{
//				Title = "Special Opportunity",
//				FreezeOnState = State.Special_Opportunity,
//				FreezeAtEnd = true,
//				SuccessOnFreeze = false,
//			}
//			);
//
//			// prompt for special extra, trigger special extra, freeze on special extra last hit
//			TrainingScript.Enqueue(new TrainingStep
//			{
//				Title = "Chain Special Extra",
//				Prompt = FightManager.Translate("swipeSpecialExtra"),
//				GestureFX = FeedbackFXType.Swipe_Forward,
//				FreezeOnHit = true,
//				FreezeOnState = State.Special_Extra,
//			}
//			);
//
//			// next step (AI vengeance) triggered on return to idle
//			TrainingScript.Enqueue(new TrainingStep
//			{
//				Title = "Return to Idle",
//				NextStepOnState = State.Idle,
//			}
//			);
//
//			// no prompt, with enough gauge AI performs vengeance attack
//			TrainingScript.Enqueue(new TrainingStep
//			{
//				Title = "AI Vengeance",
//				Narrative = FightManager.Translate("reallyMad"),
//				AIMove = Move.Vengeance,
//				NextStepOnState = State.Idle,	// go to next step as soon as AI is idle
//			}
//			);
//
//			// no prompt, AI starts tutorial punch, freezes at end of punch start
//			TrainingScript.Enqueue(new TrainingStep
//			{
//				Title = "AI Tutorial Punch",
//				AIMove = Move.Tutorial_Punch,
//				FreezeOnState = State.Tutorial_Punch_Start,
//				FreezeOnAI = true,
//				FreezeAtEnd = true,
//				SuccessOnFreeze = false,
//			}
//			);
//
//			// prompt for counter attack, AI tutorial punch strikes counter taunt and triggers counter attack
//			TrainingScript.Enqueue(new TrainingStep
//			{
//				Title = "Counter Attack",
//				Narrative = FightManager.Translate("ouchGauge"),
//				Prompt = FightManager.Translate("swipeCounter"),
//				GestureFX = FeedbackFXType.Swipe_Back,
//				FreezeOnHit = true,
//				FreezeOnState = State.Counter_Attack,
//			}
//			);
//
//			// no prompt, AI performs a special
//			TrainingScript.Enqueue(new TrainingStep
//			{
//				Title = "AI Special",
//				Narrative = FightManager.Translate("comesAgain"),
//				AIMove = Move.Special,
//				FreezeOnState = State.Special,
//				FreezeOnAI = true,
//				FreezeAtEnd = true,
//				SuccessOnFreeze = false,
//			}
//			);
//
//			// next step triggered on return to idle
//			TrainingScript.Enqueue(new TrainingStep
//			{
//				Title = "Return to Idle",
//				NextStepOnState = State.Idle,
//			}
//			);
//
//			// prompt for vengeance, AI strikes counter taunt and triggers counter attack
//			TrainingScript.Enqueue(new TrainingStep
//			{
//				Title = "Vengeance",
//				Narrative = FightManager.Translate("vengeanceGauge"),
//				Prompt = FightManager.Translate("swipeVengeance"),
//				GestureFX = FeedbackFXType.Swipe_Vengeance,
//				FreezeOnHit = true,
//				FreezeOnState = State.Vengeance,
//				SuccessOnFreeze = false,				// ...KO!!
//			}
//			);
		}

		#endregion
    }

	// used to log failed inut attempts to provide info bubble help
	public class FailedInput
	{
		public FeedbackFXType InputRequired;	// as per training gesture fx
		public Move InputEntered;				// incorrect for InputRequired
	}
}
