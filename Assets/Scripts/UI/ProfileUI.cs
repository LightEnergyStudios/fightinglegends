
using UnityEngine;
using UnityEngine.UI;
using System.Collections;


namespace FightingLegends
{
	public class ProfileUI : MonoBehaviour
	{
		[HideInInspector]
		public Fighter Fighter;
		
		private FightManager fightManager;
		
		private Text healthText;
		private Text profileText;
		private Text statusText;

		private const int maxTrainingSteps = 6;
		

		// 'Constructor'
		// NOT called when returning from background
		private void Awake()
		{
			var fightManagerObject = GameObject.Find("FightManager");
			fightManager = fightManagerObject.GetComponent<FightManager>();
			
			var children = GetComponentsInChildren<Text>();
			
			foreach (var child in children)
			{
				if (child.name == "Health")
					healthText = child;
				else if (child.name == "Profile")
					profileText = child;
				else if (child.name == "Status")
					statusText = child;
			}
		}

		private void FixedUpdate()
		{
			if (Fighter == null)		// assigned by StatusUI
				return;

			UpdateHealth();
			UpdateProfile();
			UpdateStatus();
		}
	
		private void UpdateHealth()
		{
			healthText.color = Fighter.ExpiredState ? Color.red : (Fighter.isFrozen ? Color.blue : Color.cyan);

			healthText.text = Fighter.FullName.ToUpper();

//			healthText.text += fightManager.FightPaused ? "~ PAUSED ~" : string.Format("\n\n{0:0.##} FPS", fightManager.AnimationFPS);

			healthText.text += string.Format("\nHEALTH: {0:0.##}", Fighter.ProfileData.SavedData.Health);
			healthText.text += string.Format("\nGAUGE: {0} / {1:0.##}", Fighter.ProfileData.SavedData.Gauge, Fighter.ProfileData.SavedData.GaugeDamage);
			healthText.text += string.Format("\nLEVEL: {0} / {1:0.##}", Fighter.ProfileData.SavedData.Level, Fighter.ProfileData.SavedData.XP);

			healthText.text += "\nPRIORITY: " + (int)Fighter.CurrentPriority;
		}
		
		private void UpdateProfile()
		{
			if (Fighter.InTraining)
			{
				var trainer = Fighter.Trainer;
				if (trainer.TrainingScript == null)
					return;
				
				var trainingScript = trainer.TrainingScript.ToArray();
				var currentStep = trainer.CurrentStep;

				profileText.color = trainer.PromptingForInput ? Color.green : Color.grey;

				profileText.text = "IN TRAINING [ " + trainingScript.Length + " STEPS ]";

				if (currentStep != null)
				{
					profileText.text += "\nCURRENT GESTURE: " + currentStep.GestureFX.ToString().ToUpper();

					if (trainer.CurrentStep.FreezeOnHit)
						profileText.text += "\nFREEZE ON LAST HIT";
					else
						profileText.text += "\nFREEZE ON STATE: " + currentStep.FreezeOnState.ToString().ToUpper() + (currentStep.FreezeAtEnd ? " [End]" : " [Start]");

					profileText.text += "\nFREEZE AT END: " + currentStep.FreezeAtEnd;
					profileText.text += "\nSUCCESS ON FREEZE: " + currentStep.SuccessOnFreeze;
//					profileText.text += "\nUNFREEZE NEXT STEP: " + currentStep.UnFreezeNextStep;

					profileText.text += "\nAI FREEZE: " + currentStep.FreezeOnAI;
					profileText.text += "\nAI MOVE: " + currentStep.AIMove.ToString().ToUpper();
					profileText.text += "\nINPUT RECEIVED: " + currentStep.InputReceived;
				}
					
				profileText.text += "\n";
				for (int i = 0; i < maxTrainingSteps; i++)
				{
					if (i >= trainingScript.Length)
					{
						profileText.text += "\n";
						continue;
					}
					profileText.text += string.Format("\n{0}: {1} [ {2} ]", i+1,
								trainingScript[i].Title, trainingScript[i].GestureFX.ToString().ToUpper());
				}
			}
			else if (! Fighter.UnderAI || Fighter.AIController.DumbAI)
			{
				profileText.color = Fighter.ExpiredState ? Color.red : Color.cyan;

				var profileData = Fighter.ProfileData;

				profileText.text = string.Format("CLASS: {0}", profileData.FighterClass.ToString());

				var undefinedElements = (profileData.Element1 == FighterElement.Undefined && profileData.Element2 == FighterElement.Undefined);
				profileText.text += undefinedElements ? "\n" : string.Format("\nELEMENTS: {0} & {1}", profileData.Element1.ToString(), profileData.Element2.ToString());
				
//				profileText.text += string.Format("\nWINS: {0}", profileData.SavedData.MatchesWon);
//				profileText.text += string.Format("\nLOSSES: {0}", profileData.SavedData.MatchesLost);

				profileText.text += string.Format("\n\nHITS DELIVERED: {0}", profileData.SavedData.DeliveredHits);
				profileText.text += string.Format("\nHITS BLOCKED: {0}", profileData.SavedData.BlockedHits);
				profileText.text += string.Format("\nHITS TAKEN: {0}", profileData.SavedData.HitsTaken);
				profileText.text += string.Format("\nSUCCESSFUL BLOCKS: {0}", profileData.SavedData.HitsBlocked);
				profileText.text += string.Format("\nDAMAGE INFLICTED: {0:F0}", profileData.SavedData.DamageInflicted);
				profileText.text += string.Format("\nDAMAGE SUSTAINED: {0:F0}", profileData.SavedData.DamageSustained);
			}
			else
			{
				profileText.color = Fighter.ExpiredState ? Color.red : Color.white;

				var aiController = Fighter.AIController;
				var personality = aiController.Personality;

//				profileText.text = string.Format("PERSONALITY: {0}", personality != null ? personality.Name : "Unknown");
//				profileText.text += string.Format("\nATTITUDE: {0}", aiController != null ? aiController.CurrentAttitude.Attitude.ToString() : "Unknown");

//				profileText.text = string.Format("ATTITUDE: {0}", aiController != null ? aiController.CurrentAttitude.Attitude.ToString() : "Unknown");
				profileText.text = string.Format("{0}", aiController.StatusUI);

				if (! string.IsNullOrEmpty(aiController.ErrorUI))
					profileText.text += string.Format("\n!! {0}", aiController.ErrorUI);

				if (aiController.IterateStrategies)
				{
					profileText.text += "\n\n" + "NEXT TRIGGER: " + aiController.TestStepUI;		// currently active strategy
				}

				var triggered = ! string.IsNullOrEmpty(aiController.TriggerUI);
				var trigger = triggered ? "LAST TRIGGER: " + aiController.TriggerUI : ""; // "AWAITING TRIGGER...";
				profileText.text += "\n\n" + trigger + "\n";		// condition that triggered a strategy selection

				if (triggered)
				{
					int counter = 0;
					int propensityCounter = 0;				// number of times a propensity appears in PropensityChoices
					AIPropensity prevPropensity = null;		// for detecting a change while looping
					AIPropensity selectedPropensity = null;	// for flagging which propensity was selected

					foreach (var propensity in aiController.PropensityChoices)
					{
						if (counter == aiController.SelectedPropensityIndex)
							selectedPropensity = propensity;

						counter++;

						bool lastPropensity = (counter == AIController.numPropensityChoices);
						if (lastPropensity)
							propensityCounter++;
						
						// output prevPropensity if the propensity has changed or if it's the last
						if ((counter > 1 && propensity != prevPropensity) || lastPropensity)
						{
							float percentage = ((float)propensityCounter / (float)AIController.numPropensityChoices) * 100.0f;
							bool isSelected = prevPropensity == selectedPropensity;

							if (prevPropensity == null)
								profileText.text += string.Format("\n{0} {1} [{2}%]", (isSelected ? "--> " : ""), "Do Nothing", (int)percentage);
							else
								profileText.text += string.Format("\n{0} {1} {2} [{3}%]", (isSelected ? "--> " : ""), (prevPropensity.Strategy.Behaviour.Mistake ? "X" : ""), prevPropensity.Strategy.Strategy.ToString(), (int)percentage);

							propensityCounter = 0; 		// reset
						}

						propensityCounter++;
						prevPropensity = propensity;
					}
				}
			}
		}


		private void UpdateStatus()
		{
			statusText.color = (Fighter.CanContinue) ? Color.yellow : (Fighter.UnderAI ? Color.cyan : Color.green);
//					(Fighter.CanExecute ? (Fighter.UnderAI ? Color.cyan : Color.green) : Color.red);

			var hasDebug = ! string.IsNullOrEmpty(Fighter.DebugUI);
			var hasAINextMove = !fightManager.EitherFighterExpiredState && ! string.IsNullOrEmpty(Fighter.NextMoveUI);
			var moveFrameCount = " [ " + Fighter.MoveFrameCount.ToString() + " ]";

			if (Fighter.ExpiredState)
				statusText.text = "R.I.P." + moveFrameCount;
			else if (Fighter.IsHitStunned)
				statusText.text = "HIT STUNNED!";
			else if (Fighter.IsShoveStunned)
				statusText.text = "SHOVE STUNNED!";
			else if (Fighter.IsBlockStunned)
				statusText.text = "BLOCK STUNNED!";
			else
				statusText.text = Fighter.CurrentMove.ToString().ToUpper() + moveFrameCount;
			
			statusText.text += "\n" + Fighter.StateUI; 			// CurrentState

			if (Fighter.UnderAI && Fighter.AIController.DumbAI)
				statusText.text += hasAINextMove ? ("\nNext: " + Fighter.NextMoveUI) : "\n";
			else
				statusText.text += "\n" + Fighter.CuedUI;
			
//			statusText.text += hasLastMove ? ("\nLast: " + Fighter.LastMoveUI) : "";
			statusText.text += hasDebug ? ("\n" + Fighter.DebugUI) : "";
		}
	}
}

