
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


namespace FightingLegends
{
	[Serializable]
	public class TrainingCombo
	{
		public string ComboName = "";
		public List<ComboStep> ComboSteps;

		private int currentStepIndex = 0;	
		private int currentStepUIIndex = 0;		// used for positioning of vertical flashes

		public bool comboActive { get; private set; }
		private bool comboCompleted = false;

		private int comboFailures = 0;				// failed attempts
//		private int maxComboFailures = 3;			// before info bubble message

		private bool restartOnIdle = false;			// restart (try again) when both fighters return to idle

		private Fighter listeningToFighter = null;

//		private const string LMHComboName = "L-M-H-S-E Combo";
//		private const string ResetComboName = "Reset-Counter Combo";

		public delegate void ComboStartedDelegate(TrainingCombo combo);
		public ComboStartedDelegate OnComboStarted;

		public delegate void ComboUpdatedDelegate(TrainingCombo combo);
		public ComboUpdatedDelegate OnComboUpdated;

		public delegate void ComboAIMoveDelegate(ComboStep step);
		public ComboAIMoveDelegate OnComboAIMove;

		public delegate void ComboStepStartedDelegate(ComboStep step);
		public ComboStepStartedDelegate OnComboStepStarted;

		public delegate void ComboStepActivatedDelegate(ComboStep step);
		public ComboStepActivatedDelegate OnComboStepActivated;

//		public delegate void ComboStepTrafficLightDelegate(TrafficLight colour);
//		public ComboStepTrafficLightDelegate OnComboStepTrafficLight;

//		public delegate void ComboStepVerticalFlashDelegate(ComboStep step, int stepUIIndex, bool enabled);
//		public ComboStepVerticalFlashDelegate OnComboStepVerticalFlash;

		public delegate void ComboStepCompletedDelegate(ComboStep step);
		public ComboStepCompletedDelegate OnComboStepCompleted;

		public delegate void ComboStepExpiredDelegate(ComboStep step);
		public ComboStepExpiredDelegate OnComboStepExpired;

		public delegate void ComboRestartDelegate(TrainingCombo combo, bool reachedMaxFailures);
		public ComboRestartDelegate OnComboRestart;

		public delegate void ComboCompletedDelegate(TrainingCombo combo, bool success);
		public ComboCompletedDelegate OnComboCompleted;

		public ComboStep CurrentComboStep
		{
			get
			{
				if (ComboSteps.Count == 0)
					return null;

				if (currentStepIndex < 0 || currentStepIndex > ComboSteps.Count - 1)
					return null;
				
				return ComboSteps[currentStepIndex];
			}
		}

		public int NonAIStepCount
		{
			get
			{
				int count = 0;

				foreach (var step in ComboSteps)
				{
					if (step.IsAIMove)
						continue;

					count++;
				}
				return count;
			}
		}
	
		public void StartListening(Fighter fighter)
		{
			if (!fighter.InTraining || listeningToFighter == fighter)
				return;

			listeningToFighter = fighter;
			
			fighter.OnStateStarted += TriggerOnState;
			fighter.OnLastHit += TriggerOnLastHit;
			fighter.OnGaugeChanged += TriggerOnGauge;

			fighter.Opponent.OnStateStarted += TriggerOnState;	// AI
			fighter.Opponent.OnLastHit += TriggerOnLastHit;		// AI

//			Debug.Log("TrainingCombo: start listening to " + listeningToFighter.FullName);
		}

		public void StopListening(Fighter fighter)
		{
			if (!fighter.InTraining || listeningToFighter != fighter)
				return;

			listeningToFighter = null;
			
			fighter.OnStateStarted -= TriggerOnState;
			fighter.OnLastHit -= TriggerOnLastHit;
			fighter.OnGaugeChanged -= TriggerOnGauge;

			fighter.Opponent.OnStateStarted -= TriggerOnState;	// AI
			fighter.Opponent.OnLastHit -= TriggerOnLastHit;		// AI

//			Debug.Log("TrainingCombo: stop listening to " + fighter.FullName);
		}

		public void StartCombo(bool restart)
		{
//			Debug.Log("StartCombo: " + ComboName);

			comboActive = true;
			comboCompleted = false;
			restartOnIdle = false;
			currentStepIndex = 0;
			currentStepUIIndex = 0;		// used for positioning of vertical flashes

			// broadcast update to combo (primarily for UI)
			if (! restart)
			{				
				if (OnComboStarted != null)
					OnComboStarted(this);
			}
			else
			{
				if (OnComboUpdated != null)
					OnComboUpdated(this);
			}
				
			ActivateCurrentStep();					// first step activated automatically

			// broadcast start of first combo step
			if (OnComboStepStarted != null)
				OnComboStepStarted(CurrentComboStep);
		}

		public void CompleteCombo(bool success)
		{
			comboActive = false;
			comboCompleted = true;

			ComboSteps.Clear();

			// broadcast completion of combo (primarily for Trainer)
			if (OnComboCompleted != null)
				OnComboCompleted(this, success);
		}


		private void RestartCombo()
		{
//			Debug.Log("TrainingCombo: RestartCombo " + ComboName);
			comboFailures++;

			foreach (var step in ComboSteps)
			{
				step.WaitingForInput = false;
				step.Completed = false;
//				step.Expired = false;
			}

			bool reachedMaxFailures = comboFailures >= Fighter.MaxFailedInputs;

			// broadcast reset of combo (primarily for Trainer to reset health + gauge)
			if (OnComboRestart != null)
				OnComboRestart(this, reachedMaxFailures);

			// reset falures?
//			if (reachedMaxFailures)
//				comboFailures = 0;

			StartCombo(true);
		}
			
		// execute AI move and complete step, or set current step to WaitingForInput 
		private void ActivateCurrentStep()
		{
//			Debug.Log("ActivateCurrentStep " + CurrentComboStep.StepName);

			if (CurrentComboStep.IsAIMove && CurrentComboStep.ComboMove != Move.None)
			{
				// broadcast AI move (to Trainer)
				if (OnComboAIMove != null)
					OnComboAIMove(CurrentComboStep);

				CompleteCurrentStep(false);
			}
			else
			{
				// broadcast (to Trainer)
				if (OnComboStepActivated != null)
					OnComboStepActivated(CurrentComboStep);
				
				CurrentComboStep.WaitingForInput = true;		// waiting for required input

//				if (OnComboStepTrafficLight != null)
//					OnComboStepTrafficLight(CurrentComboStep.TrafficLightColour);

//				if (OnComboStepVerticalFlash != null)
//					OnComboStepVerticalFlash(CurrentComboStep, currentStepUIIndex, true);
			}

			// broadcast update to combo (primarily for UI)
			if (OnComboUpdated != null)
				OnComboUpdated(this);
		}

		// complete the current step and move to next 
		public void CompleteCurrentStep(bool incrementUIStep)
		{
			if (CurrentComboStep == null)
				return;

//			Debug.Log("CompleteCurrentStep " + CurrentComboStep.StepName);

			CurrentComboStep.Completed = true;
			CurrentComboStep.WaitingForInput = false;

//			if (OnComboStepTrafficLight != null)
//				OnComboStepTrafficLight(TrafficLight.None);

//			// deactivate vertical flash
//			if (OnComboStepVerticalFlash != null)
//				OnComboStepVerticalFlash(CurrentComboStep, currentStepUIIndex, false);

			if (incrementUIStep)
				currentStepUIIndex++;

			if (! GoToNextStep())			// reached end of combo!
			{
				CompleteCombo(true);
			}
			else
			{
				// broadcast completion of combo step
				if (OnComboStepCompleted != null)
					OnComboStepCompleted(CurrentComboStep);
			}

			// broadcast update to combo (primarily for UI)
			if (OnComboUpdated != null)
				OnComboUpdated(this);
		}

		private void ExpireCurrentStep(Fighter fighter)
		{
//			Debug.Log("ExpireCurrentStep " + CurrentComboStep.StepName);
			CurrentComboStep.WaitingForInput = false;

//			if (OnComboStepTrafficLight != null)
//				OnComboStepTrafficLight(TrafficLight.None);

//			// deactivate vertical flash
//			if (OnComboStepVerticalFlash != null)
//				OnComboStepVerticalFlash(CurrentComboStep, currentStepUIIndex, false);

			// restart the combo when both fighters return to idle
			if (fighter.CurrentState == State.Idle && fighter.Opponent.CurrentState == State.Idle)
				RestartCombo();
			else
				restartOnIdle = true;

			// broadcast expiry of combo step
			if (OnComboStepExpired != null)
				OnComboStepExpired(CurrentComboStep);

			// broadcast update to combo (primarily for UI)
			if (OnComboUpdated != null)
				OnComboUpdated(this);
		}

		private bool GoToNextStep()
		{
			if (CurrentComboStep == null)
				return false;

			if (CurrentStepIsLast)
				return false;

			currentStepIndex++;

//			if (!(CurrentComboStep.IsAIMove && CurrentComboStep.ComboMove != Move.None))
//				currentStepUIIndex++;

//			Debug.Log("GoToNextStep: " + CurrentComboStep.StepName);

			// broadcast start of combo step
			if (OnComboStepStarted != null)
				OnComboStepStarted(CurrentComboStep);

			return true;
		}

		public bool CurrentStepIsLast
		{
			get { return (currentStepIndex == ComboSteps.Count - 1); }
		}
			
		private bool StepActivateMatchesState(FighterChangedData stateData)
		{
			if (CurrentComboStep.WaitingForInput)			// already activated
				return false;

			bool match = false;

			if (CurrentComboStep.ActivateFromState)
				match = (stateData.OldState == CurrentComboStep.ActivateState);
			else
				match = (stateData.NewState == CurrentComboStep.ActivateState);

			if (match)
			{
//				Debug.Log("StepActivateMatchesState: OldState = " + stateData.OldState + " NewState = " + stateData.NewState);
				return stateData.Fighter.UnderAI == CurrentComboStep.ActivateAIState;
			}

			return false;
		}

		private bool StepExpiryMatchesState(FighterChangedData stateData)
		{
			if (! CurrentComboStep.WaitingForInput)			// not activated - can't expire
				return false;

			bool match = false;

			if (CurrentComboStep.ExpiryFromState)
				match = (stateData.OldState == CurrentComboStep.ExpiryState);
			else
				match = (stateData.NewState == CurrentComboStep.ExpiryState);

			if (match)
			{
//				Debug.Log("StepExpiryMatchesState: OldState = " + stateData.OldState + " NewState = " + stateData.NewState);
				return stateData.Fighter.UnderAI == CurrentComboStep.ExpiryAIState;
			}

			return false;
		}


		private void TriggerOnState(FighterChangedData stateData)
		{
			if (! comboActive)
				return;
			
			if (CurrentComboStep == null)
				return;

			// restart combo when both fighters back at idle (InTraining fighter listening to both fighters)
			if (restartOnIdle && stateData.NewState == State.Idle && stateData.Fighter.Opponent.CurrentState == State.Idle)
			{
//				Debug.Log(stateData.Fighter.FullName + ": TriggerOnState - both fighters idle");

				restartOnIdle = false;
				RestartCombo();
				return;
			}

//			Debug.Log("TrainingCombo: TriggerState " + stateData.NewState + ", Step = " + CurrentComboStep.StepName + ", WaitingForInput = " + CurrentComboStep.WaitingForInput);

			// activate the current step on fighter state start
			if (! CurrentComboStep.WaitingForInput && StepActivateMatchesState(stateData) && ! CurrentComboStep.ActivateLastHit)
			{
				ActivateCurrentStep();
			}
			// expire step and reset combo if the expiry state is reached
			else if (CurrentComboStep.WaitingForInput && StepExpiryMatchesState(stateData))
			{
//				Debug.Log("TriggerState: state = " + stateData.NewState + ", step " + CurrentComboStep.StepName + " EXPIRED");
				ExpireCurrentStep(stateData.Fighter);
			}
		}

		private void TriggerOnLastHit(FighterChangedData stateData)
		{
			if (! comboActive)
				return;

			if (CurrentComboStep == null)
				return;

			// activate the current step on fighter last hit
			if (! CurrentComboStep.WaitingForInput && StepActivateMatchesState(stateData) && CurrentComboStep.ActivateLastHit)
			{
				ActivateCurrentStep();
			}
			// expire step and reset combo if the fighter expiry last hit is reached
			else if (CurrentComboStep.WaitingForInput && StepExpiryMatchesState(stateData) && CurrentComboStep.ExpiryLastHit)
			{
//				Debug.Log("TriggerLastHit: state = " + stateData.NewState + ", step " + CurrentComboStep.StepName + " EXPIRED");
				ExpireCurrentStep(stateData.Fighter);
			}
		}

//		private void TriggerOnRomanCancel(FighterChangedData stateData)
//		{
//			if (! comboActive)
//				return;
//
//			if (CurrentComboStep == null)
//				return;
//
////			Debug.Log("TriggerRomanCancel: step " + CurrentComboStep.StepName + " ActivateRomanCancel = " + CurrentComboStep.ActivateRomanCancel);
//
//			// activate the current step on roman cancel event
//			if (! CurrentComboStep.WaitingForInput && CurrentComboStep.ActivateRomanCancel)
//			{
//				ActivateCurrentStep();
//			}
//			// expire step and reset combo on roman cancel event
//			else if (CurrentComboStep.WaitingForInput && CurrentComboStep.ExpiryRomanCancel)
//			{
////				Debug.Log("TriggerRomanCancel: step " + CurrentComboStep.StepName + " EXPIRED");
//				ExpireCurrentStep(stateData.Fighter);
//			}
//		}

		private void TriggerOnGauge(FighterChangedData stateData, bool stars)
		{
			if (! comboActive)
				return;

			if (CurrentComboStep == null)
				return;

			bool gaugeIncreased = stateData.NewGauge > stateData.OldGauge;

			if (listeningToFighter == null)
				return;

//			Debug.Log("TriggerOnGauge: step " + CurrentComboStep.StepName + " ActivateOnGauge = " + CurrentComboStep.ActivateOnGauge);

			// activate the current step on sufficient gauge for move
			if (! CurrentComboStep.WaitingForInput && gaugeIncreased && CurrentComboStep.ActivateOnGauge > 0 && stateData.NewGauge >= CurrentComboStep.ActivateOnGauge)
			{
				ActivateCurrentStep();
			}
			// expire current step on insufficient gauge for move
			else if (! CurrentComboStep.WaitingForInput && !gaugeIncreased && CurrentComboStep.ActivateOnGauge >= 0 && stateData.NewGauge < CurrentComboStep.ActivateOnGauge)
			{
//				Debug.Log("TriggerOnGauge: step " + CurrentComboStep.StepName + " EXPIRED");
				ExpireCurrentStep(stateData.Fighter);
			}
		}
	}


	[Serializable]
	public class ComboStep
	{
		public string StepName = "";
		public Move ComboMove = Move.None;			// for Trainer.ValidateMove (to complete step), to determine image in UI and for AI move execution
		public bool IsAIMove = false;				// AI executes ComboMove

		public State ActivateState = State.Void;	// state (start) that activates this step (waiting for input before expiry)
		public bool ActivateFromState = false;		// step activated when state changes from ActivateState
		public bool ActivateLastHit = false;		// step activated on last hit of ActivateState
		public int ActivateOnGauge = -1;			// step activated when gauge sufficient for move
		public bool ActivateRomanCancel = false;	// step activated on roman cancel event
		public bool ActivateAIState = false;		// step activated by change of AI ActivateState

		public State ExpiryState = State.Void;		// state (start) that, if reached, causes the step to expire (reset combo)
		public bool ExpiryFromState = false;		// step expires when state changes from ExpiryState
		public bool ExpiryLastHit = false;			// step expires on last hit of ExpiryState
		public int ExpiryOnGauge = -1;				// step expires when gauge insufficient for move
		public bool ExpiryRomanCancel = false;		// step expires on roman cancel event
		public bool ExpiryAIState = false;			// step expires on change of AI ExpiryState

		public bool WaitingForInput = false;		// waiting for correct input, required (before expiry) to progress to next step
		public bool Completed = false;				// correct input before expiry (tick)

		public TrafficLight TrafficLightColour = TrafficLight.None;
		public FeedbackFXType GestureFX = FeedbackFXType.None;	// optional

//		public bool Expired = false;				// timed-out
	}
}
