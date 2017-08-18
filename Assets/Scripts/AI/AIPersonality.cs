
using System;
using UnityEngine;
using System.Collections.Generic;


namespace FightingLegends
{
	[Serializable]
	public class AIPersonality
	{
		public string Name;
		public List<AIPropensity> Propensities;		// strategies triggered under different attitudes and conditions, with different probabilities
	}


	// many-to-many attitute <-> strategy mapping
	// facilitates list of attitudes that trigger a strategy
	// facilitates list of strategies triggered by an attitude
	[Serializable]
	public class AIPropensity
	{				
		public AIAttitude Attitude;
		public AIStrategy Strategy;

		public float Probability; 			// likelihood propensity will be chosen from others / doing nothing

		public bool IsActive = true;		// may be deactivated (for testing purposes)
	}
		

	[Serializable]
	public class AIAttitude
	{		
		public Attitude Attitude = Attitude.Even;	// 'key'
		public float DoNothingProbability;	// likelihood of doing nothing (ie. not selecting a strategy to execute)
	}


	// list of behaviours (moves) to execute
	[Serializable]
	public class AIStrategy
	{		
		public Strategy Strategy = Strategy.None;		// 'key'
		public AIBehaviour Behaviour;
		public AICondition TriggerCondition;			// when strategy triggered

		public bool IsProactive { get { return ! TriggerCondition.OpponentTrigger; } }

		public override string ToString()
		{
			string condition = (TriggerCondition.OpponentTrigger ? "P1 " : "P2 ");

			switch (TriggerCondition.Condition)
			{
				case Condition.None:
					break;

				case Condition.Gauge:
					condition += "Gauge " + TriggerCondition.TriggerGauge;
					break;

				case Condition.StateStart:
					condition += "StateStart " + TriggerCondition.TriggerState;
					break;

				case Condition.StateEnd:
					condition += "StateEnd " + TriggerCondition.TriggerState;
					break;

				case Condition.MoveExecuted:
					condition += "MoveExecuted " + TriggerCondition.TriggerMove;
					break;

				case Condition.MoveCompleted:
					condition += "MoveCompleted " + TriggerCondition.TriggerMove;
					break;

				case Condition.RomanCancel:
					condition += "RomanCancel";
					break;

				case Condition.PriorityChanged:
					condition += "PriorityChanged " + TriggerCondition.TriggerPriority;
					break;

				case Condition.HealthChanged:
					condition += "HealthChanged " + TriggerCondition.TriggerHealth;
					break;

				case Condition.CanContinue:
					condition += "CanContinue";
					break;

				case Condition.LastHit:
					condition += "LastHit " + TriggerCondition.TriggerState;
					break;

				case Condition.HitStun:
					condition += "HitStun";
					break;

				case Condition.IdleFrame:
					condition += "IdleFrame " + TriggerCondition.TriggerIdleFrame + (TriggerCondition.FrameRepeat ? " (Repeat)" : "");
					break;

				case Condition.BlockIdleFrame:
					condition += "BlockIdleFrame " + TriggerCondition.TriggerBlockIdleFrame + (TriggerCondition.FrameRepeat ? " (Repeat)" : "");
					break;

				case Condition.CanContinueFrame:
					condition += "CanContinueFrame " + TriggerCondition.TriggerCanContinueFrame + (TriggerCondition.FrameRepeat ? " (Repeat)" : "");
					break;

				case Condition.VengeanceFrame:
					condition += "VengeanceFrame " + TriggerCondition.TriggerVengeanceFrame + (TriggerCondition.FrameRepeat ? " (Repeat)" : "");
					break;

				case Condition.GaugeIncreasedFrame:
					condition += "GaugeIncreasedFrame " + TriggerCondition.TriggerGaugeIncreasedFrame + (TriggerCondition.FrameRepeat ? " (Repeat)" : "");
					break;

				case Condition.StunnedFrame:
					condition += "StunnedFrame " + TriggerCondition.TriggerStunnedFrame + (TriggerCondition.FrameRepeat ? " (Repeat)" : "");
					break;
			}

			return string.Format("{0} [ {1} ]", condition, Strategy.ToString());
		}
	}


	[Serializable]
	public class AIBehaviour
	{		
		public Behaviour Behaviour = Behaviour.None;					// 'key'
		public bool Proactive;											// else reactive
		public bool Mistake;											// nobody's perfect!
		public Move MoveToExecute = Move.None;							// move executed by behaviour

		public AIDifficulty Difficulty = AIDifficulty.Simple;				// move only executed from this difficulty level upwards

		public FighterClass FighterClass = FighterClass.Undefined;		// move executed by fighter of this class only
	}


	[Serializable]
	public class AICondition
	{		
		public Condition Condition = Condition.None;				// 'key'
		public bool OpponentTrigger = false;						// condition triggered by AI (ie. proactive by default)

		public Move TriggerMove = Move.None;						// triggered by move execution/completion
		public State TriggerState = State.Void;						// triggered by state start/end
		public int TriggerPriority = Fighter.Default_Priority;		// triggered by priority change
		public int TriggerGauge = 0;								// triggered by change of gauge gems
		public float TriggerHealth = 0.0f;							// triggered by given health value

		public int TriggerIdleFrame = 0;							// triggered after x frames at idle
		public int TriggerBlockIdleFrame = 0;						// triggered after opponent is x frames at block idle
		public int TriggerCanContinueFrame = 0;						// triggered after x frames at CanContinue
		public int TriggerVengeanceFrame = 0;						// triggered after x frames at vengeance
		public int TriggerGaugeIncreasedFrame = 0;					// triggered after x frames after gauge increase
		public int TriggerStunnedFrame = 0;							// triggered after x frames of hit / block / shove stun
		public int TriggerLastHitFrame = 0;							// triggered after x frames since last hit (until end of state)

		public bool FrameRepeat = false;							// triggered every x frames (as opposed to just once)

		public FighterClass OpponentClass = FighterClass.Undefined;	// condition true if opponent class matches
	}
}
