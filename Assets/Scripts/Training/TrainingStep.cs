
using System;
using UnityEngine;
using System.Collections.Generic;


namespace FightingLegends
{
	[Serializable]
	public class TrainingStep
	{		
		public string Title = "";						// step title (internal use)
		public string Header = "";						// panel heading
		public string Narrative = "";					// script narrative
		public Sprite NarrativeSprite = null;			// narrative image

		public float WaitSeconds = 0.0f;				// at start of step

		// ...frozen (or idle) from previous step...
		// ...prompt and loop gesture FX until training step gesture is input...
		public string Prompt = "";
		public FeedbackFXType GestureFX = FeedbackFXType.None;	// optional

		public bool ReleaseBlock = false;				// at start of step - release block and stop block stun

		// ...then unfreeze (executing the AI move, if present, possibly as a continuation)
		public Move AIMove = Move.None;					// on unfreeze

		// ...freeze on a last hit frame or at the start of the specified state
		// last hit frame freeze supercedes state freeze
		public bool FreezeOnHit = false;				// freeze on (last) hit frame --> success! -> next step
		public State FreezeOnState = State.Void;		// freeze on start/end of this state --> success! -> next step
		public bool FreezeAtEnd = false;				// freeze at end of FreezeOnState
		public bool FreezeOnAI = false;					// freeze triggered by AI hit frame / state

		public bool SuccessOnFreeze = true;				// success FX on freeze or straight to next step
		public State NextStepOnState = State.Void;		// go to next step at start of this state

		public bool InputReceived = false;				// to prevent repeated input - once is all that is required

		public TrainingCombo Combo = null;				// list of ComboSteps 'played' at full speed with no freezing

		public bool ActivatesTrafficLights = false;		// traffic lights disabled until explained
		public bool IsTrafficLightStep = false;			// training step to explain traffic lights
		public TrafficLight TrafficLightColour = TrafficLight.None;		// traffic light colour associated with training step
	}
}
