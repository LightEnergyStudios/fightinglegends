
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using System.Linq;

namespace FightingLegends
{
	public class AIController : MonoBehaviour
    {
		public AIPersonality Personality { get; protected set; }	// data initialised by InitPersonality

		[HideInInspector]
		public List<AIAttitude> Attitudes = new List<AIAttitude>();
		private int CurrentAttitudeIndex = 0;			// TODO: implement changing attitudes at some point
		public AIAttitude CurrentAttitude { get { return Attitudes [CurrentAttitudeIndex]; }} 
				
		public const int numPropensityChoices = 100;		// from which to choose a strategy to execute
		[HideInInspector]
		public AIPropensity[] PropensityChoices;
		[HideInInspector]
		public int SelectedPropensityIndex = -1;

		private const float SimpleDoNothing = 8.0f;
		private const float EasyDoNothing = 4.0f;				// default difficulty
		private const float MediumDoNothing = 2.0f;				// previous default value
		private const float HardDoNothing = 1.0f;
		private const float BrutalDoNothing = 0.1f;

		public string TriggerUI { get; private set; }		// used by StatusUI to show behaviour triggers
		public string TestStepUI { get; private set; }		// used by StatusUI to show behaviour triggers

		public string StatusUI { get; private set; }		// used by StatusUI to status
		public string ErrorUI { get; private set; }			// used by StatusUI to errors

//		private Queue<Move> moveQueue;				// FIFO (Dumb AI)
//		public int MoveIntervalFrames = 100;		// approx every 7 seconds
//		private int framesToNextMove = 0;			// dumb AI

//		public int BlockFrames = 75;				// approx 5 seconds
//		private int blockFramesRemaining = 0;	

		public bool IsWatching = false;				// listening for opponent changes

		public bool DumbAI { get; private set; }	// dumb cycling through moves - no intelligence
		public bool AISuspended = false;
		public bool MoveCountdownSuspended = false;

		// flags for testing AI strategies
		public bool ReactiveStrategies = false;		// consider reactive strategies
		public bool ProactiveStrategies = false;	// consider proactive strategies
		public bool IterateStrategies = false;		// step through strategies
		public bool IsolateStrategy = false;		// consider only the active strategy if true (else all others triggered by the same condition)

		private AIPropensity lastActivated = null;

		public bool StrategyCued = false;		// to prevent >1 (event) triggered per beat - reset at start of each beat (by fighter)

		public int IterationRepeat = 10;			// number of times to activate each iterated strategy
		private int iterationCount = 0;				// number of times each iterated strategy activated

		public Fighter fighter;
		private FightManager fightManager;


		private AIPropensity SelectedPropensity
		{
			get { return SelectedPropensityIndex == -1 ? PropensityChoices[ SelectedPropensityIndex ] : null; }
		}

		private IEnumerable<AIPropensity> AttitudePropensities(Attitude attitude)
		{
			return Personality.Propensities.Where(x => x.Attitude.Attitude == attitude && OkForTesting(x)).OrderByDescending(x => x.Probability);
		}

		private IEnumerable<AIPropensity> CurrentAttitudePropensities()
		{
			return AttitudePropensities(CurrentAttitude.Attitude);
		}


		private IEnumerable<AIPropensity> BehaviourAttitudes(Strategy behaviourPattern)
		{
			return Personality.Propensities.Where(x => x.Strategy.Strategy == behaviourPattern).OrderByDescending(x => x.Probability);
		}


		// 'constructor'
		private void Awake()
		{
//			fighter = GetComponent<Fighter>();

//			Debug.Log("AIController Awake: " + fighter.FullName);

			var fightManagerObject = GameObject.Find("FightManager");
			fightManager = fightManagerObject.GetComponent<FightManager>();
				
			PropensityChoices = new AIPropensity[ numPropensityChoices ];

			StatusUI = "";
			ErrorUI = "";
		}

			
		public void StartWatching()
		{
//			Debug.Log(fighter.FullName + " StartWatching: fighter.Opponent.InTraining = " + fighter.Opponent.InTraining);

			if (IsWatching)
				return;

			if (fighter == null)
				return;

			if (! fighter.UnderAI)
				return;

			if (DumbAI)
				return;

			if (fighter.Opponent.InTraining)
				return;

			if (fighter.PreviewIdle || fighter.PreviewMoves)
				return;

			// subscribe to fighter changed events
			// watch opponent's moves, states, health and priority
			var opponent = fighter.Opponent;

			if (opponent != null)
			{
				opponent.OnHealthChanged += OnHealthChanged;
				opponent.OnStateStarted += OnStateStarted;
				opponent.OnStateEnded += OnStateEnded;
				opponent.OnPriorityChanged += OnPriorityChanged;

				opponent.OnCanContinue += OnCanContinue;
				opponent.OnLastHit += OnLastHit;
				opponent.OnHitStun += OnHitStun;

				opponent.OnRomanCancel += OnRomanCancel;

				opponent.OnIdleFrame += OnIdleFrame;
				opponent.OnBlockIdleFrame += OnBlockIdleFrame;
				opponent.OnCanContinueFrame += OnCanContinueFrame;
				opponent.OnVengeanceFrame += OnVengeanceFrame;
			}
				
			// also watch own health, gauge, state, etc
			fighter.OnHealthChanged += OnHealthChanged;
			fighter.OnGaugeChanged += OnGaugeChanged;
			fighter.OnStateStarted += OnStateStarted;
			fighter.OnStateEnded += OnStateEnded;

			fighter.OnCanContinue += OnCanContinue;
			fighter.OnLastHit += OnLastHit;
			fighter.OnHitStun += OnHitStun;

			fighter.OnRomanCancel += OnRomanCancel;

			fighter.OnIdleFrame += OnIdleFrame;
			fighter.OnBlockIdleFrame += OnBlockIdleFrame;
			fighter.OnCanContinueFrame += OnCanContinueFrame;
			fighter.OnVengeanceFrame += OnVengeanceFrame;
			fighter.OnGaugeIncreasedFrame += OnGaugeIncreasedFrame;
			fighter.OnStunnedFrame += OnStunnedFrame;
			fighter.OnLastHitFrame += OnLastHitFrame;

			fighter.OnKnockOutFreeze += OnKnockOutFreeze;		// second life opportunity

//			Debug.Log(fighter.FullName + ": StartWatching OK");

			IsWatching = true;
		}

//		private void OnDisable()
//		{
//			StopWatching();
//		}

		private void OnDestroy()
		{
			if (! fighter.PreviewIdle && !fighter.PreviewMoves)
			{
//				Debug.Log(fighter.FullName + ": OnDestroy -> StopWatching");
				StopWatching();
			}
		}

		public void StopWatching()
		{
//			Debug.Log(fighter.FullName + ": StopWatching - IsWatching = " + IsWatching);

			if (! IsWatching)
				return;

			if (fighter == null)
				return;

			if (! fighter.UnderAI)
				return;
			
			if (DumbAI)
				return;

//			if (fighter.Opponent != null && fighter.Opponent.InTraining)
//				return;

			if (fighter.PreviewIdle || fighter.PreviewMoves)
				return;

			var opponent = fighter.Opponent;

			if (opponent != null)
			{
				if (opponent.InTraining)
					return;
				
				opponent.OnHealthChanged -= OnHealthChanged;
				opponent.OnStateStarted -= OnStateStarted;
				opponent.OnStateEnded -= OnStateEnded;
				opponent.OnPriorityChanged -= OnPriorityChanged;

				opponent.OnCanContinue -= OnCanContinue;
				opponent.OnLastHit -= OnLastHit;
				opponent.OnHitStun -= OnHitStun;

				opponent.OnRomanCancel -= OnRomanCancel;

				opponent.OnIdleFrame -= OnIdleFrame;
				opponent.OnBlockIdleFrame -= OnBlockIdleFrame;
				opponent.OnCanContinueFrame -= OnCanContinueFrame;
				opponent.OnVengeanceFrame -= OnVengeanceFrame;
			}

			fighter.OnHealthChanged -= OnHealthChanged;
			fighter.OnGaugeChanged -= OnGaugeChanged;
			fighter.OnStateStarted -= OnStateStarted;
			fighter.OnStateEnded -= OnStateEnded;

			fighter.OnCanContinue -= OnCanContinue;
			fighter.OnLastHit -= OnLastHit;
			fighter.OnHitStun -= OnHitStun;

			fighter.OnRomanCancel -= OnRomanCancel;

			fighter.OnIdleFrame -= OnIdleFrame;
			fighter.OnBlockIdleFrame -= OnBlockIdleFrame;
			fighter.OnCanContinueFrame -= OnCanContinueFrame;
			fighter.OnVengeanceFrame -= OnVengeanceFrame;
			fighter.OnGaugeIncreasedFrame -= OnGaugeIncreasedFrame;
			fighter.OnStunnedFrame -= OnStunnedFrame;
			fighter.OnLastHitFrame -= OnLastHitFrame;

			fighter.OnKnockOutFreeze -= OnKnockOutFreeze;		// second life opportunity

//			Debug.Log(fighter.FullName + ": StopWatching OK");
			IsWatching = false;
		}


		private void Start()
		{
			DumbAI = false;
			ReactiveStrategies = true;
			ProactiveStrategies = true;
			IterateStrategies = false;
			IsolateStrategy = false;

			if (fighter.UnderAI)
			{
//				var smartAI = PlayerPrefs.GetInt("SmartAI", 0) != 0;
//				DumbAI = ! smartAI;
//
//				// consider proactive strategies
//				ReactiveStrategies = PlayerPrefs.GetInt("ReactiveAI", 0) != 0;
//
//				// consider proactive strategies
//				ProactiveStrategies = PlayerPrefs.GetInt("ProactiveAI", 0) != 0;
//
//				// activate one strategy at a time, stepping on when each condition met
//				IterateStrategies = PlayerPrefs.GetInt("IterateAI", 0) != 0;
//
//				// consider only the active test step strategy 
//				// as opposed to all triggered by the same condition
//				IsolateStrategy = PlayerPrefs.GetInt("IsolateAI", 0) != 0;

//				if (DumbAI)
//				{
//					PopulateMoves();
//					framesToNextMove = MoveIntervalFrames;
//				}
//				else
				{
					InitPropensityChoices();
					InitAttitudes();
					InitPersonality();  		// virtual

//					Debug.Log(fighter.FullName + ": Start - IsWatching = false");
//					IsWatching = false;

					if (IterateStrategies)
					{
						DeactivatePropensities(true);
						IterateToNextStrategy();
					}
					else
					{
						DeactivatePropensities(false);
					}
				}
			}
		}
			

//		private void FixedUpdate()
//		{
//			if (fighter == null)
//				return;
//			
//			if (! fighter.UnderAI)
//				return;
//
//			if (fighter.ExpiredState || fighter.aboutToExpire)
//				return;
//
//			if (AISuspended)
//				return;
//
//			if (fightManager.FightPaused)
//				return;
//
//			if (! FightManager.SavedStatus.CompletedBasicTraining)
//				return;
//
//			if (! fightManager.ReadyToFight)
//				return;
//
//			if (fightManager.EitherFighterExpiredHealth)
//				return;
//
//			if (DumbAI)
//			{
//				if (moveQueue.Count > 0)
//				{
//					fighter.NextMoveUI = moveQueue.Peek().ToString().ToUpper();
//
//					if (! MoveCountdownSuspended && framesToNextMove > 0)
//						fighter.NextMoveUI += " in... [ " + framesToNextMove + " ]";
//
//					MoveCountdown();		// between moves, suspended while executing moves
//				}
//					
//				BlockCountdown();		// if fighter is blocking (DumbAI)
//			}
//		}


//		private void MoveCountdown()
//		{
//			if (! DumbAI)
//				return;
//			
//			if (MoveCountdownSuspended)
//				return;
//			
//			if (framesToNextMove == 0)
//			{
//				fighter.ExecuteMove(moveQueue.Peek(), false);		// suspends countdown. move can fail - doesn't matter
//				moveQueue.Enqueue(moveQueue.Dequeue());		// move to the end of the queue
//
//				// reset counter
//				framesToNextMove = MoveIntervalFrames;
//			}
//			else
//			{
//				framesToNextMove--;
//			}
//  		}


//		public void StartBlockCountdown()
//		{
//			if (! DumbAI)
//				return;
//			
//			if (fighter.IsBlocking)
//				blockFramesRemaining = BlockFrames;
//		}

//		private void BlockCountdown()
//		{
//			if (! fighter.IsBlocking)
//				return;
//			
//			if (fighter.Opponent.InTraining)
//				return;
//
//			if (! DumbAI)
//				return;
//
//			if (blockFramesRemaining == 0)
//			{
//				// end animation (back to idle)
//				fighter.CompleteMove();
////				Debug.Log(fighter.FullName + ": BlockCountdown expired");
//			}
//			else
//			{
//				fighter.StateUI = "[ Blocking... " + blockFramesRemaining + " ]";
//				blockFramesRemaining--;
//			}
//		}

		#region propensity selection

		private float DifficultyDoNothingFactor
		{
			get
			{
				switch (FightManager.SavedGameStatus.Difficulty)
				{
					case AIDifficulty.Simple:
						return SimpleDoNothing;
					case AIDifficulty.Easy:
						return EasyDoNothing;
					case AIDifficulty.Medium:
						return MediumDoNothing;
					case AIDifficulty.Hard:
						return HardDoNothing;
					case AIDifficulty.Brutal:
						return BrutalDoNothing;
				}

				return 1.0f;
			}
		}

		private void InitPropensityChoices()
		{
			for (int i = 0; i < numPropensityChoices; i++)
				PropensityChoices[i] = null;		// do nothing
		}
			
		private float TotalProbability(IEnumerable<AIPropensity> propensities, bool doSomething)
		{
			float totalProbability = propensities.Sum(x => x.Probability);

			try
			{
			if (!doSomething)		// doing nothing is a possibility... 
			{
				// probability of doing nothing decreases with each round lost in a match
				var roundsLost = fighter.ProfileData.SavedData.MatchRoundsLost > 0 ? fighter.ProfileData.SavedData.MatchRoundsLost : 1;
				totalProbability += (CurrentAttitude.DoNothingProbability * fighter.ProfileData.AIDoNothingFactor * DifficultyDoNothingFactor) / roundsLost;
			}
			}
			catch (Exception)
			{
				Debug.Log("TotalProbability - null fighter or ProfileData!!!");
			}

			return totalProbability;
		}

		// return value of null represents 'do nothing'
		private AIPropensity RandomSelectPropensity(IEnumerable<AIPropensity> propensities, bool doSomething = false)
		{
			float totalProbability = TotalProbability(propensities, doSomething);
			int slotNumber = 0;		// index into propensityChoices array

			InitPropensityChoices();		// set all choices to null (do nothing)

			foreach (var propensity in propensities)
			{
				float apportion = propensity.Probability / totalProbability * numPropensityChoices;
				int numSlots = (int)(apportion + 0.5f);		// fastest way to round a float?

				if (numSlots == 0)		// very small probability, but include anyway
					numSlots = 1;

				for (int s = 0; s < numSlots && slotNumber < numPropensityChoices; s++)
				{
					PropensityChoices[slotNumber++] = propensity;
				}
			}
			// ...any remaining slots are null (do nothing)

			// pick a slot at random
			SelectedPropensityIndex = UnityEngine.Random.Range(0, numPropensityChoices-1);
			return PropensityChoices[ SelectedPropensityIndex ];
		}

		#endregion 		// propensity selection


		#region behaviour triggers

		private bool IsIdleFrameTrigger(int frameNumber, AICondition condition)
		{
			return ((condition.FrameRepeat && condition.TriggerIdleFrame > 0 && frameNumber >= condition.TriggerIdleFrame) ?
				(frameNumber % condition.TriggerIdleFrame == 0) :
				(frameNumber == condition.TriggerIdleFrame));
		}


		private bool MatchFighterClasses(AIStrategy strategy)
		{
			var behaviour = strategy.Behaviour;
			var condition = strategy.TriggerCondition;

			// no need to match if either the behaviour or the condition are not class specific
			if (behaviour.FighterClass == FighterClass.Undefined || condition.OpponentClass == FighterClass.Undefined)
				return true;

			// can only match classes of fighter and opponent!
			if (! condition.OpponentTrigger)
				return false;

			var fighterClass = fighter.ProfileData.FighterClass;
			var opponentClass = fighter.Opponent.ProfileData.FighterClass;

			return fighterClass == behaviour.FighterClass && opponentClass == condition.OpponentClass;
		}

		// check if the behaviour move is possible under the current AI difficulty level
		private bool MatchDifficulty(AIStrategy strategy)
		{	
			return strategy.Behaviour.Difficulty <= FightManager.SavedGameStatus.Difficulty;
		}
			

		private bool MatchesActiveCondition(AIStrategy strategy)
		{
			if (DumbAI)
				return true;

			if (! IterateStrategies)
				return true;

			if (IsolateStrategy)
				return false;

			var activeStepCondition = TestStepActivePropensity.Strategy.TriggerCondition;
			var condition = strategy.TriggerCondition;

			if (condition.Condition != activeStepCondition.Condition)
				return false;

			if (condition.OpponentTrigger != activeStepCondition.OpponentTrigger)
				return false;
			
			switch (condition.Condition) 		// both conditions the same
			{
				case Condition.None:
					return false;

				case Condition.Gauge:
					return condition.TriggerGauge == activeStepCondition.TriggerGauge;

				case Condition.StateStart:
					return condition.TriggerState == activeStepCondition.TriggerState;

				case Condition.StateEnd:
					return condition.TriggerState == activeStepCondition.TriggerState;

				case Condition.MoveExecuted:
					return condition.TriggerMove == activeStepCondition.TriggerMove;

				case Condition.MoveCompleted:
					return condition.TriggerMove == activeStepCondition.TriggerMove;

				case Condition.RomanCancel:
					return true;			// no further conditions required

				case Condition.PriorityChanged:
					return condition.TriggerPriority == activeStepCondition.TriggerPriority;

				case Condition.HealthChanged:
					return condition.TriggerHealth == activeStepCondition.TriggerHealth;

				case Condition.CanContinue:
					return true;

				case Condition.LastHit:
					return condition.TriggerState == activeStepCondition.TriggerState;

				case Condition.HitStun:
					return true;

				// frame conditions may not all concide on the same frame,
				// so may not all end up in the final choices
				case Condition.IdleFrame:
				case Condition.BlockIdleFrame:
				case Condition.CanContinueFrame:
				case Condition.VengeanceFrame:
				case Condition.GaugeIncreasedFrame:
				case Condition.StunnedFrame:
				case Condition.LastHitFrame:
					return true;

				default:
					return false;
			}
		}

		private bool OkForTesting(AIPropensity propensity)
		{
			if (!ReactiveStrategies && !propensity.Strategy.Behaviour.Proactive)
				return false;

			if (!ProactiveStrategies && propensity.Strategy.Behaviour.Proactive)
				return false;

			if (IterateStrategies)
			{
				if (IsolateStrategy)  	// active strategy only
					return propensity.IsActive;
				else  					// active strategy and all others that have the same trigger condition 
					return propensity.IsActive || MatchesActiveCondition(propensity.Strategy);
			}

			return true;
		}


		private void ChooseStateStrategy(FighterChangedData triggerState, Condition triggerCondition, bool opponentTrigger)
		{
//			Debug.Log("ChooseStateStrategy: " + triggerState.State + " / " + triggerCondition + ", CanContinue = " + fighter.CanContinue);

			if (StrategyCued)
				return;
			
			var propensities = CurrentAttitudePropensities().Where
					(x => x.Strategy.TriggerCondition.OpponentTrigger == opponentTrigger
					&& x.Strategy.TriggerCondition.TriggerState == triggerState.NewState
					&& x.Strategy.TriggerCondition.Condition == triggerCondition
					&& MatchFighterClasses(x.Strategy) && MatchDifficulty(x.Strategy)
					&& fighter.CanExecuteMove(x.Strategy.Behaviour.MoveToExecute));
			
//			if (opponentTrigger && triggerCondition == Condition.StateStart && triggerState.State != State.Idle && propensities.Count() > 0)
//				Debug.Log("ReactiveStateStrategy: state start = " + triggerState.State);

			if (propensities.Count() == 0)
				return;
			
			// diagnostics
			TriggerUI = opponentTrigger ? "[P1] " : "[P2] ";
			TriggerUI += triggerCondition.ToString();
			TriggerUI += ": " + triggerState.NewState.ToString();
			TriggerUI +=  " [ " + fighter.AnimationFrameCount + " ]";

			var propensity = RandomSelectPropensity(propensities);

			if (propensity != null)
			{
				if (propensity.Strategy != null)		// null == 'do nothing'
				{
					if (!CueStrategy(propensity.Strategy))
					{
						ErrorUI = "ChooseStateStrategy unable to " + propensity.Strategy.Behaviour.MoveToExecute;
						Debug.Log(fighter.FullName + ": ChooseStateStrategy ExecuteStrategy failed - " + propensity.Strategy.Behaviour.MoveToExecute);
//						fighter.ContinueOrIdle();		// TODO: temporary catch all if a strategy fails
					}
				}
//
//				strategySelected = false;
			}
				
			// activate next strategy even if DoNothing was chosen
			if (IterateStrategies)
				IterateToNextStrategy();
		}


		private void ChooseMoveStrategy(FighterChangedData triggerState, Condition triggerCondition, bool opponentTrigger)
		{
			if (StrategyCued)
				return;

			var propensities = CurrentAttitudePropensities().Where
				(x => x.Strategy.TriggerCondition.OpponentTrigger == opponentTrigger
					&& x.Strategy.TriggerCondition.TriggerMove == triggerState.Move
					&& x.Strategy.TriggerCondition.Condition == triggerCondition
					&& MatchFighterClasses(x.Strategy) && MatchDifficulty(x.Strategy)
					&& fighter.CanExecuteMove(x.Strategy.Behaviour.MoveToExecute));

			if (propensities.Count() == 0)
				return;
			
			// diagnostics
			TriggerUI = opponentTrigger ? "[P1] " : "[P2] ";
			TriggerUI += triggerCondition.ToString();
			TriggerUI += ": " + triggerState.Move.ToString();
			TriggerUI +=  " [ " + fighter.AnimationFrameCount + " ]";

			var propensity = RandomSelectPropensity(propensities);

			if (propensity != null)
			{
				if (propensity.Strategy != null)		// null == 'do nothing'
				{
					if (!CueStrategy(propensity.Strategy))
					{
						ErrorUI = "ChooseMoveStrategy unable to " + propensity.Strategy.Behaviour.MoveToExecute;
						Debug.Log(fighter.FullName + ": ChooseMoveStrategy ExecuteStrategy failed - " + propensity.Strategy.Behaviour.MoveToExecute);
//						fighter.ContinueOrIdle();		// TODO: temporary catch all if a strategy fails
					}
				}

//				strategySelected = false;
			}

			// activate next strategy even if DoNothing was chosen
			if (IterateStrategies)
				IterateToNextStrategy();
		}
			

		private void ChooseCanContinueStrategy(FighterChangedData triggerState, bool opponentTrigger)
		{
			if (StrategyCued)
				return;
			
			var propensities = CurrentAttitudePropensities().Where
				(x => x.Strategy.TriggerCondition.OpponentTrigger == opponentTrigger
					&& x.Strategy.TriggerCondition.Condition == Condition.CanContinue
					&& MatchFighterClasses(x.Strategy) && MatchDifficulty(x.Strategy)
					&& fighter.CanExecuteMove(x.Strategy.Behaviour.MoveToExecute));

			if (propensities.Count() == 0)
				return;

			// diagnostics
			TriggerUI = opponentTrigger ? "[P1] " : "[P2] ";
			TriggerUI += Condition.CanContinue.ToString();
			TriggerUI += ": " + triggerState.Move.ToString();
			TriggerUI +=  " [ " + fighter.AnimationFrameCount + " ]";

			var propensity = RandomSelectPropensity(propensities);

			if (propensity != null)
			{
				if (propensity.Strategy != null)		// null == 'do nothing'
				{
					if (!CueStrategy(propensity.Strategy))
					{
						ErrorUI = "ChooseCanContinueStrategy unable to " + propensity.Strategy.Behaviour.MoveToExecute;
						Debug.Log(fighter.FullName + ": ChooseCanContinueStrategy ExecuteStrategy failed - " + propensity.Strategy.Behaviour.MoveToExecute);
//						fighter.ContinueOrIdle();		// TODO: temporary catch all if a strategy fails
					}
				}

//				strategySelected = false;
			}

			// activate next strategy even if DoNothing was chosen
			if (IterateStrategies)
				IterateToNextStrategy();
		}

		private void ChooseLastHitStrategy(FighterChangedData triggerState, bool opponentTrigger)
		{
			if (StrategyCued)
				return;
			
			var propensities = CurrentAttitudePropensities().Where
				(x => x.Strategy.TriggerCondition.OpponentTrigger == opponentTrigger
					&& x.Strategy.TriggerCondition.Condition == Condition.LastHit
					&& MatchFighterClasses(x.Strategy) && MatchDifficulty(x.Strategy)
					&& fighter.CanExecuteMove(x.Strategy.Behaviour.MoveToExecute));

			if (propensities.Count() == 0)
				return;

			// diagnostics
			TriggerUI = opponentTrigger ? "[P1] " : "[P2] ";
			TriggerUI += Condition.LastHit.ToString();
			TriggerUI += ": " + triggerState.Move.ToString();
			TriggerUI +=  " [ " + fighter.AnimationFrameCount + " ]";

			var propensity = RandomSelectPropensity(propensities);

			if (propensity != null)
			{
				if (propensity.Strategy != null)		// null == 'do nothing'
				{
					if (!CueStrategy(propensity.Strategy))
					{
						ErrorUI = "ChooseLastHitStrategy unable to " + propensity.Strategy.Behaviour.MoveToExecute;
						Debug.Log(fighter.FullName + ": ChooseLastHitStrategy ExecuteStrategy failed - " + propensity.Strategy.Behaviour.MoveToExecute);
//						fighter.ContinueOrIdle();		// TODO: temporary catch all if a strategy fails
					}
				}

//				strategySelected = false;
			}

			// activate next strategy even if DoNothing was chosen
			if (IterateStrategies)
				IterateToNextStrategy();
		}

		private void ChooseHitStunStrategy(FighterChangedData triggerState, bool opponentTrigger)
		{
			if (StrategyCued)
				return;
			
			var propensities = CurrentAttitudePropensities().Where
				(x => x.Strategy.TriggerCondition.OpponentTrigger == opponentTrigger
					&& x.Strategy.TriggerCondition.Condition == Condition.HitStun
					&& MatchFighterClasses(x.Strategy) && MatchDifficulty(x.Strategy)
					&& fighter.CanExecuteMove(x.Strategy.Behaviour.MoveToExecute));

			if (propensities.Count() == 0)
				return;

			// diagnostics
			TriggerUI = opponentTrigger ? "[P1] " : "[P2] ";
			TriggerUI += Condition.HitStun.ToString();
			TriggerUI += ": " + triggerState.Move.ToString();
			TriggerUI +=  " [ " + fighter.AnimationFrameCount + " ]";

			var propensity = RandomSelectPropensity(propensities);

			if (propensity != null)
			{
				if (propensity.Strategy != null)		// null == 'do nothing'
				{
					if (!CueStrategy(propensity.Strategy))
					{
						ErrorUI = "ChooseHitStunStrategy unable to " + propensity.Strategy.Behaviour.MoveToExecute;
						Debug.Log(fighter.FullName + ": ChooseHitStunStrategy ExecuteStrategy failed - " + propensity.Strategy.Behaviour.MoveToExecute);
//						fighter.ContinueOrIdle();		// TODO: temporary catch all if a strategy fails
					}
				}

//				strategySelected = false;
			}

			// activate next strategy even if DoNothing was chosen
			if (IterateStrategies)
				IterateToNextStrategy();
		}

		private void ChooseRomanCancelStrategy(FighterChangedData triggerState, bool opponentTrigger)
		{
			if (StrategyCued)
				return;
			
//			Debug.Log(fighter.FullName + ": ChooseRomanCancelStrategy - CanContinue = " + fighter.CanContinue + ", State = " + fighter.CurrentState + ", Gauge = " + fighter.ProfileData.Gauge);

			var propensities = CurrentAttitudePropensities().Where
				(x => x.Strategy.TriggerCondition.OpponentTrigger == opponentTrigger
					&& x.Strategy.TriggerCondition.Condition == Condition.RomanCancel
					&& MatchFighterClasses(x.Strategy) && MatchDifficulty(x.Strategy)
					&& fighter.CanExecuteMove(x.Strategy.Behaviour.MoveToExecute));

			if (propensities.Count() == 0)
				return;

//			Debug.Log(fighter.FullName + ": ChooseRomanCancelStrategy - choices = " + propensities.Count());

			// diagnostics
			TriggerUI = opponentTrigger ? "[P1] " : "[P2] ";
			TriggerUI += Condition.RomanCancel.ToString();
			TriggerUI += ": " + triggerState.Move.ToString();
			TriggerUI +=  " [ " + fighter.AnimationFrameCount + " ]";

			var propensity = RandomSelectPropensity(propensities);

			if (propensity != null)
			{
				if (propensity.Strategy != null)		// null == 'do nothing'
				{
					if (!CueStrategy(propensity.Strategy))
					{
						ErrorUI = "ChooseRomanCancelStrategy unable to " + propensity.Strategy.Behaviour.MoveToExecute;
						Debug.Log(fighter.FullName + ": ChooseRomanCancelStrategy ExecuteStrategy failed - " + propensity.Strategy.Behaviour.MoveToExecute);
//						fighter.ContinueOrIdle();		// TODO: temporary catch all if a strategy fails
					}
				}

//				strategySelected = false;
			}

			// activate next strategy even if DoNothing was chosen
			if (IterateStrategies)
				IterateToNextStrategy();
		}

		private void ChooseHealthStrategy(FighterChangedData triggerState, bool opponentTrigger)
		{
			if (StrategyCued)
				return;
			
			// trigger only applies when health falls below the strategy condition	// TODO: correct?
			var propensities = CurrentAttitudePropensities().Where
					(x => x.Strategy.TriggerCondition.OpponentTrigger == opponentTrigger
					&& x.Strategy.TriggerCondition.Condition == Condition.HealthChanged
					&& triggerState.NewHealth < x.Strategy.TriggerCondition.TriggerHealth
					&& MatchFighterClasses(x.Strategy) && MatchDifficulty(x.Strategy)
					&& fighter.CanExecuteMove(x.Strategy.Behaviour.MoveToExecute));

			if (propensities.Count() == 0)
				return;
			
			// diagnostics
			TriggerUI = opponentTrigger ? "[P1] " : "[P2] ";
			TriggerUI += "Health: ";
			TriggerUI += triggerState.NewHealth;
			TriggerUI +=  " [ " + fighter.AnimationFrameCount + " ]";

			var propensity = RandomSelectPropensity(propensities);

			if (propensity != null)
			{
				if (propensity.Strategy != null)		// null == 'do nothing'
				{
					if (!CueStrategy(propensity.Strategy))
					{
						ErrorUI = "ChooseHealthStrategy unable to " + propensity.Strategy.Behaviour.MoveToExecute;
						Debug.Log(fighter.FullName + ": ChooseHealthStrategy ExecuteStrategy failed - " + propensity.Strategy.Behaviour.MoveToExecute);
//						fighter.ContinueOrIdle();		// TODO: temporary catch all if a strategy fails
					}
				}

//				strategySelected = false;
			}

			// activate next strategy even if DoNothing was chosen
			if (IterateStrategies)
				IterateToNextStrategy();
		}


		private void ChooseGaugeStrategy(FighterChangedData triggerState, bool opponentTrigger)
		{
			if (StrategyCued)
				return;
			
//			Debug.Log(fighter.FullName + ": ChooseGaugeStrategy - old = " + triggerState.OldGauge + ", new = " + triggerState.NewGauge + ", CanContinue = " + fighter.CanContinue + ", State = " + fighter.CurrentState);
			// trigger only applies when gauge is equal to the strategy condition	// TODO: correct?
			var propensities = CurrentAttitudePropensities().Where
					(x => x.Strategy.TriggerCondition.OpponentTrigger == opponentTrigger
					&& x.Strategy.TriggerCondition.Condition == Condition.Gauge
					&& x.Strategy.TriggerCondition.TriggerGauge == triggerState.NewGauge	
					&& MatchFighterClasses(x.Strategy) && MatchDifficulty(x.Strategy)
					&& fighter.CanExecuteMove(x.Strategy.Behaviour.MoveToExecute));

			if (propensities.Count() == 0)
				return;
			
			// diagnostics
			TriggerUI = opponentTrigger ? "[P1] " : "[P2] ";
			TriggerUI += "Gauge: ";
			TriggerUI += triggerState.NewGauge;
			TriggerUI +=  " [ " + fighter.AnimationFrameCount + " ]";

			var propensity = RandomSelectPropensity(propensities);

			if (propensity != null)
			{
				if (propensity.Strategy != null)		// null == 'do nothing'
				{
					if (!CueStrategy(propensity.Strategy))
					{
						ErrorUI =  "ChooseGaugeStrategy unable to " + propensity.Strategy.Behaviour.MoveToExecute;
						Debug.Log(fighter.FullName + ": ChooseGaugeStrategy ExecuteStrategy failed - " + propensity.Strategy.Behaviour.MoveToExecute);
//						fighter.ContinueOrIdle();		// TODO: temporary catch all if a strategy fails
					}
				}

//				strategySelected = false;
			}

			// activate next strategy even if DoNothing was chosen
			if (IterateStrategies)
				IterateToNextStrategy();
		}


		private void ChoosePriorityStrategy(FighterChangedData triggerState, bool opponentTrigger)
		{
			if (StrategyCued)
				return;
			
			// trigger only applies when priority is equal to the strategy condition
			var propensities = CurrentAttitudePropensities().Where
					(x => x.Strategy.TriggerCondition.OpponentTrigger == opponentTrigger
					&& x.Strategy.TriggerCondition.Condition == Condition.PriorityChanged
					&& x.Strategy.TriggerCondition.TriggerPriority == triggerState.NewPriority
					&& MatchFighterClasses(x.Strategy) && MatchDifficulty(x.Strategy)
					&& fighter.CanExecuteMove(x.Strategy.Behaviour.MoveToExecute));

			if (propensities.Count() == 0)
			{
//				Debug.Log(fighter.FullName + ": ChoosePriorityStrategy: no propensities!");
				return;
			}
			
			// diagnostics
			TriggerUI = opponentTrigger ? "[P1] " : "[P2] ";
			TriggerUI += "Priority: ";
			TriggerUI += triggerState.NewPriority;
			TriggerUI +=  " [ " + fighter.AnimationFrameCount + " ]";

			var propensity = RandomSelectPropensity(propensities);

			if (propensity != null)
			{
				if (propensity.Strategy != null)		// null == 'do nothing'
				{
					if (!CueStrategy(propensity.Strategy))
					{
						ErrorUI = "ChoosePriorityStrategy unable to " + propensity.Strategy.Behaviour.MoveToExecute;
						Debug.Log(fighter.FullName + ": ChoosePriorityStrategy ExecuteStrategy failed - " + propensity.Strategy.Behaviour.MoveToExecute);
//						fighter.ContinueOrIdle();		// TODO: temporary catch all if a strategy fails
					}
				}

//				strategySelected = false;
			}

			// activate next strategy even if DoNothing was chosen
			if (IterateStrategies)
				IterateToNextStrategy();
		}

		private void ChooseIdleFrameStrategy(FighterChangedData triggerState, bool opponentTrigger)
		{
			if (StrategyCued)
				return;
			
			// trigger applies when idle frame is equal to the strategy condition
			var propensities = CurrentAttitudePropensities().Where
				(x => x.Strategy.TriggerCondition.OpponentTrigger == opponentTrigger
					&& x.Strategy.TriggerCondition.Condition == Condition.IdleFrame
					&& IsIdleFrameTrigger(triggerState.FrameNumber, x.Strategy.TriggerCondition)
					&& MatchFighterClasses(x.Strategy) && MatchDifficulty(x.Strategy)
					&& fighter.CanExecuteMove(x.Strategy.Behaviour.MoveToExecute));

			if (propensities.Count() == 0)
				return;
			
			// diagnostics
			TriggerUI = "Idle frame: ";
			TriggerUI += triggerState.FrameNumber;
			TriggerUI +=  " [ " + fighter.AnimationFrameCount + " ]";

			var propensity = RandomSelectPropensity(propensities);

			if (propensity != null)
			{
				if (propensity.Strategy != null)		// null == 'do nothing'
				{
					if (!CueStrategy(propensity.Strategy))
					{
						ErrorUI = "ChooseIdleFrameStrategy unable to " + propensity.Strategy.Behaviour.MoveToExecute;
						Debug.Log(fighter.FullName + ": ChooseIdleFrameStrategy ExecuteStrategy failed - " + propensity.Strategy.Behaviour.MoveToExecute);
//						fighter.ContinueOrIdle();		// TODO: temporary catch all if a strategy fails
					}
				}

//				strategySelected = false;
			}
				
			// activate next strategy even if DoNothing was chosen
			if (IterateStrategies)
				IterateToNextStrategy();
		}

		private void ChooseBlockIdleFrameStrategy(FighterChangedData triggerState, bool opponentTrigger)
		{
			if (StrategyCued)
				return;
			
			// trigger applies when opponents block idle frame is equal to the strategy condition
			var propensities = CurrentAttitudePropensities().Where
				(x => x.Strategy.TriggerCondition.OpponentTrigger == opponentTrigger
					&& x.Strategy.TriggerCondition.Condition == Condition.BlockIdleFrame
					&& x.Strategy.TriggerCondition.TriggerBlockIdleFrame == triggerState.FrameNumber
					&& MatchFighterClasses(x.Strategy) && MatchDifficulty(x.Strategy)
					&& fighter.CanExecuteMove(x.Strategy.Behaviour.MoveToExecute));

			if (propensities.Count() == 0)
				return;

			// diagnostics
			TriggerUI = "Block idle frame: ";
			TriggerUI += triggerState.FrameNumber;
			TriggerUI +=  " [ " + fighter.AnimationFrameCount + " ]";

			var propensity = RandomSelectPropensity(propensities);

			if (propensity != null)
			{
				if (propensity.Strategy != null)		// null == 'do nothing'
				{
					if (!CueStrategy(propensity.Strategy))
					{
						ErrorUI = "ChooseBlockIdleFrameStrategy unable to " + propensity.Strategy.Behaviour.MoveToExecute;
						Debug.Log(fighter.FullName + ": ChooseBlockIdleFrameStrategy ExecuteStrategy failed - " + propensity.Strategy.Behaviour.MoveToExecute);
//						fighter.ContinueOrIdle();		// TODO: temporary catch all if a strategy fails
					}
				}

//				strategySelected = false;
			}
				
			// activate next strategy even if DoNothing was chosen
			if (IterateStrategies)
				IterateToNextStrategy();
		}

		private void ChooseCanContinueFrameStrategy(FighterChangedData triggerState, bool opponentTrigger)
		{
			if (StrategyCued)
				return;
			
			// trigger applies when can continue frame is equal to the strategy condition
			var propensities = CurrentAttitudePropensities().Where
				(x => x.Strategy.TriggerCondition.OpponentTrigger == opponentTrigger
					&& x.Strategy.TriggerCondition.Condition == Condition.CanContinueFrame
					&& x.Strategy.TriggerCondition.TriggerCanContinueFrame == triggerState.FrameNumber
					&& MatchFighterClasses(x.Strategy) && MatchDifficulty(x.Strategy)
					&& fighter.CanExecuteMove(x.Strategy.Behaviour.MoveToExecute));

			if (propensities.Count() == 0)
				return;

			// diagnostics
			TriggerUI = "CanContinue frame: ";
			TriggerUI += triggerState.FrameNumber;
			TriggerUI +=  " [ " + fighter.AnimationFrameCount + " ]";

			var propensity = RandomSelectPropensity(propensities);

			if (propensity != null)
			{
				if (propensity.Strategy != null)		// null == 'do nothing'
				{
					if (!CueStrategy(propensity.Strategy))
					{
						ErrorUI = "ChooseBlockIdleFrameStrategy unable to " + propensity.Strategy.Behaviour.MoveToExecute;
						Debug.Log(fighter.FullName + ": ChooseCanContinueFrameStrategy ExecuteStrategy failed - " + propensity.Strategy.Behaviour.MoveToExecute);
//						fighter.ContinueOrIdle();		// TODO: temporary catch all if a strategy fails
					}
				}

//				strategySelected = false;
			}

			// activate next strategy even if DoNothing was chosen
			if (IterateStrategies)
				IterateToNextStrategy();
		}

		private void ChooseVengeanceFrameStrategy(FighterChangedData triggerState, bool opponentTrigger)
		{
			if (StrategyCued)
				return;
			
			// trigger applies when vengeance frame is equal to the strategy condition
			var propensities = CurrentAttitudePropensities().Where
				(x => x.Strategy.TriggerCondition.OpponentTrigger == opponentTrigger
					&& x.Strategy.TriggerCondition.Condition == Condition.VengeanceFrame
					&& x.Strategy.TriggerCondition.TriggerVengeanceFrame == triggerState.FrameNumber
					&& MatchFighterClasses(x.Strategy) && MatchDifficulty(x.Strategy)
					&& fighter.CanExecuteMove(x.Strategy.Behaviour.MoveToExecute));

			if (propensities.Count() == 0)
				return;			// nothing triggered

			// diagnostics
			TriggerUI = "Vengeance frame: ";
			TriggerUI += triggerState.FrameNumber;
			TriggerUI +=  " [ " + fighter.AnimationFrameCount + " ]";

			var propensity = RandomSelectPropensity(propensities);

			if (propensity != null)
			{
				if (propensity.Strategy != null)		// null == 'do nothing'
				{
					if (!CueStrategy(propensity.Strategy))
					{
						ErrorUI = "ChooseVengeanceFrameStrategy unable to " + propensity.Strategy.Behaviour.MoveToExecute;
						Debug.Log(fighter.FullName + ": ChooseVengeanceFrameStrategy ExecuteStrategy failed - " + propensity.Strategy.Behaviour.MoveToExecute);
//						fighter.ContinueOrIdle();		// TODO: temporary catch all if a strategy fails
					}
				}

//				strategySelected = false;
			}

			// activate next strategy even if DoNothing was chosen
			if (IterateStrategies)
				IterateToNextStrategy();
		}
			
		private void ChooseGaugeIncreasedFrameStrategy(FighterChangedData triggerState, bool opponentTrigger)
		{
			if (StrategyCued)
				return;
			
			// trigger applies when gauge increased frame is equal to the strategy condition
			var propensities = CurrentAttitudePropensities().Where
				(x => x.Strategy.TriggerCondition.OpponentTrigger == opponentTrigger
					&& x.Strategy.TriggerCondition.Condition == Condition.GaugeIncreasedFrame
					&& x.Strategy.TriggerCondition.TriggerGaugeIncreasedFrame == triggerState.FrameNumber
					&& x.Strategy.TriggerCondition.TriggerGauge == triggerState.NewGauge
					&& MatchFighterClasses(x.Strategy) && MatchDifficulty(x.Strategy)
					&& fighter.CanExecuteMove(x.Strategy.Behaviour.MoveToExecute));

			if (propensities.Count() == 0)
				return;			// nothing triggered

			// diagnostics
			TriggerUI = "Gauge increased frame: ";
			TriggerUI += triggerState.FrameNumber;
			TriggerUI +=  " [ " + fighter.AnimationFrameCount + " ]";

			var propensity = RandomSelectPropensity(propensities);

			if (propensity != null)
			{
				if (propensity.Strategy != null)		// null == 'do nothing'
				{
					if (!CueStrategy(propensity.Strategy))
					{
						ErrorUI = "ChooseGaugeIncreasedStrategy unable to " + propensity.Strategy.Behaviour.MoveToExecute;
						Debug.Log(fighter.FullName + ": ChooseGaugeIncreasedStrategy ExecuteStrategy failed - " + propensity.Strategy.Behaviour.MoveToExecute);
//						fighter.ContinueOrIdle();		// TODO: temporary catch all if a strategy fails
					}
				}

//				strategySelected = false;
			}

			// activate next strategy even if DoNothing was chosen
			if (IterateStrategies)
				IterateToNextStrategy();
		}
			
		private void ChooseStunnedFrameStrategy(FighterChangedData triggerState, bool opponentTrigger)
		{
			if (StrategyCued)
				return;
			
			// trigger applies when stunned frame is equal to the strategy condition
			var propensities = CurrentAttitudePropensities().Where
				(x => x.Strategy.TriggerCondition.OpponentTrigger == opponentTrigger
					&& x.Strategy.TriggerCondition.Condition == Condition.StunnedFrame
					&& x.Strategy.TriggerCondition.TriggerStunnedFrame == triggerState.FrameNumber
					&& MatchFighterClasses(x.Strategy) && MatchDifficulty(x.Strategy)
					&& fighter.CanExecuteMove(x.Strategy.Behaviour.MoveToExecute));

			if (propensities.Count() == 0)
				return;			// nothing triggered

			// diagnostics
			TriggerUI = "Stunned frame: ";
			TriggerUI += triggerState.FrameNumber;
			TriggerUI +=  " [ " + fighter.AnimationFrameCount + " ]";

			var propensity = RandomSelectPropensity(propensities);

			if (propensity != null)
			{
				if (propensity.Strategy != null)		// null == 'do nothing'
				{
					if (!CueStrategy(propensity.Strategy))
					{
						ErrorUI = "ChooseStunnedFrameStrategy unable to " + propensity.Strategy.Behaviour.MoveToExecute;
						Debug.Log(fighter.FullName + ": ChooseStunnedFrameStrategy ExecuteStrategy failed - " + propensity.Strategy.Behaviour.MoveToExecute);
//						fighter.ContinueOrIdle();		// TODO: temporary catch all if a strategy fails
					}
				}

//				strategySelected = false;
			}

			// activate next strategy even if DoNothing was chosen
			if (IterateStrategies)
				IterateToNextStrategy();
		}


		private void ChooseLastHitFrameStrategy(FighterChangedData triggerState, bool opponentTrigger)
		{
//			Debug.Log("ChooseLastHitFrameStrategy: FrameNumber = " + triggerState.FrameNumber + ", StrategyCued = " + StrategyCued);

			if (StrategyCued)
				return;
			
			var propensities = CurrentAttitudePropensities().Where
				(x => x.Strategy.TriggerCondition.OpponentTrigger == opponentTrigger
					&& x.Strategy.TriggerCondition.Condition == Condition.LastHitFrame
					&& x.Strategy.TriggerCondition.TriggerLastHitFrame == triggerState.FrameNumber
					&& MatchFighterClasses(x.Strategy) && MatchDifficulty(x.Strategy)
					&& fighter.CanExecuteMove(x.Strategy.Behaviour.MoveToExecute));

			if (propensities.Count() == 0)
				return;			// nothing triggered

			// diagnostics
			TriggerUI = "Last hit frame: ";
			TriggerUI += triggerState.FrameNumber;
			TriggerUI +=  " [ " + fighter.AnimationFrameCount + " ]";

			var propensity = RandomSelectPropensity(propensities);

			if (propensity != null)
			{
				if (propensity.Strategy != null)		// null == 'do nothing'
				{
					if (!CueStrategy(propensity.Strategy))
					{
						ErrorUI = "ChooseLastHitFrameStrategy unable to " + propensity.Strategy.Behaviour.MoveToExecute;
						Debug.Log(fighter.FullName + ": ChooseLastHitFrameStrategy ExecuteStrategy failed - " + propensity.Strategy.Behaviour.MoveToExecute);
//						fighter.ContinueOrIdle();		// TODO: temporary catch all if a strategy fails
					}
				}

//				strategySelected = false;
			}

			// activate next strategy even if DoNothing was chosen
			if (IterateStrategies)
				IterateToNextStrategy();
		}

		#endregion 		// behaviour triggers


		private bool CueStrategy(AIStrategy strategy)
		{
//			Debug.Log("CueStrategy: StrategyCued = " + StrategyCued + ", MoveToExecute = " + strategy.Behaviour.MoveToExecute);
			
			if (StrategyCued)		// to prevent >1 triggered per beat
				return false;

			if (strategy == null)		// null == 'do nothing'
				return false;

			if (fighter.ExpiredHealth || fighter.ExpiredState || fighter.takenLastFatalHit)
				return false;

			if (fighter.Opponent.ExpiredHealth || fighter.Opponent.ExpiredState || fighter.Opponent.takenLastFatalHit)
				return false;

			var behaviour = strategy.Behaviour;

			if (behaviour != null && behaviour.MoveToExecute != Move.None)
			{
				// need to release block hold, otherwise ExecuteContinuation will simply keep holding block
				if (behaviour.MoveToExecute != Move.ReleaseBlock && fighter.CanReleaseBlock)
					fighter.ReleaseBlock();
				
				// immediate moves cannot be continuations
				var immediate = behaviour.MoveToExecute == Move.Roman_Cancel || behaviour.MoveToExecute == Move.ReleaseBlock;

				if (fighter.CanContinue && !immediate)
				{
					fighter.CueContinuation(behaviour.MoveToExecute);
					StatusUI = "CONTINUATION CUED: " + behaviour.MoveToExecute + " [ " + fighter.AnimationFrameCount + " ]";
				}
				else
				{
					fighter.CueMove(behaviour.MoveToExecute);
					StatusUI = "MOVE CUED: " + behaviour.MoveToExecute + " [ " + fighter.AnimationFrameCount + " ]";
				}

				StrategyCued = true;		// no more moves this beat
				return true;
			}

			return false;
		}


		private AIPropensity TestStepActivePropensity
		{
			get
			{
				if (! IterateStrategies)
					return null;

				foreach (var propensity in Personality.Propensities)
				{
					if (propensity.IsActive)
						return propensity;
				}

				return null;		// none active
			}
		}

		private void DeactivatePropensities(bool deactivate)
		{
			if (! IterateStrategies)
				return;

			foreach (var propensity in Personality.Propensities)
			{
				propensity.IsActive = !deactivate;
			}
		}

		// activates first if all inactive
		// reactive strategies only!
		private void IterateToNextStrategy()
		{
			if (! IterateStrategies)
				return;

			if (Personality.Propensities.Count == 0)
				return;
			
			iterationCount++;

			if (lastActivated != null)
			{
				if (iterationCount >= IterationRepeat)
				{
					iterationCount = 0;
				}
				else
				{
					TestStepUI = lastActivated.Strategy.ToString() + " [ " + (iterationCount+1) + " ] ";
					return;
				}
			}
				
			lastActivated = null;
			bool activateNext = false;

			foreach (var propensity in Personality.Propensities)
			{
				if (activateNext)
				{
					propensity.IsActive = true;
					lastActivated = propensity;
					break;
				}

				if (propensity.IsActive)
				{
					propensity.IsActive = false;
					activateNext = true;
				}
			}

			if (lastActivated == null)
			{
				// end of list - get first
				lastActivated = Personality.Propensities[0];
				lastActivated.IsActive = true;
			}
				
			TestStepUI = lastActivated.Strategy.ToString() + " [ " + (iterationCount+1) + " ] ";
		}

		#region AI strategies

		protected virtual void InitPersonality()
		{
			Personality = new AIPersonality
			{
				Name = "Ninja",

				Propensities = new List<AIPropensity>
				{
					// REACTIVE!

					// Release block when opponent priority drops to default (zero)
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.ReleaseBlockPriorityZero,

							Behaviour = new AIBehaviour
							{
								Proactive = false,
								Behaviour = Behaviour.Block,
								MoveToExecute = Move.ReleaseBlock,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.PriorityChanged,
								OpponentTrigger = true,
								TriggerPriority = Fighter.Default_Priority,
							},
						},

						Probability = 100.0f,		// still 1% chance of doing nothing
					},

					// Release block when opponent priority drops to punishable priority
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.ReleaseBlockPriorityPunishable,

							Behaviour = new AIBehaviour
							{
								Proactive = false,
								Behaviour = Behaviour.Block,
								MoveToExecute = Move.ReleaseBlock,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.PriorityChanged,
								OpponentTrigger = true,
								TriggerPriority = Fighter.Punishable_Priority,
							},
						},

						Probability = 100.0f,		// still 1% chance of doing nothing
					},

					// Light Strike on opponent 30 frames at Idle
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.LightStrikeOpportunist,

							Behaviour = new AIBehaviour
							{
								Proactive = false,
								Behaviour = Behaviour.Strike,
								MoveToExecute = Move.Strike_Light,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.IdleFrame,
								OpponentTrigger = true,
								TriggerIdleFrame = 30,
								FrameRepeat = true,
							},
						},

						Probability = 100.0f,		
					},


					// Block Hold on opponent Light Windup
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.BlockLight,

							Behaviour = new AIBehaviour
							{
								Proactive = false,
								Behaviour = Behaviour.Block,
								MoveToExecute = Move.Block,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								OpponentTrigger = true,
								TriggerState = State.Light_Windup,
							},
						},

						Probability = 100.0f,		
					},


					// Special on opponent Light Windup
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.StrikeCrushingSpecial,	

							Behaviour = new AIBehaviour
							{
								Proactive = false,
								Behaviour = Behaviour.Special,
								MoveToExecute = Move.Special,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								OpponentTrigger = true,
								TriggerState = State.Light_Windup,
							},
						},

						Probability = 100.0f,		
					},


					// Counter Taunt on opponent Light Windup
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.CounterStrike,	

							Behaviour = new AIBehaviour
							{
								Proactive = false,
								Behaviour = Behaviour.Counter,
								MoveToExecute = Move.Counter,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								OpponentTrigger = true,
								TriggerState = State.Light_Windup,
							},
						},

						Probability = 100.0f,		
					},

					// Vengeance on opponent LightWindup
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.StrikeCrushingVengeance,	

							Behaviour = new AIBehaviour
							{
								Proactive = false,
								Behaviour = Behaviour.Vengeance,
								MoveToExecute = Move.Vengeance,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								OpponentTrigger = true,
								TriggerState = State.Light_Windup,
							},
						},

						Probability = 100.0f,		
					},

		
					// Block Hold on opponent Special
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.BlockSpecialA,

							Behaviour = new AIBehaviour
							{
								Proactive = false,
								Behaviour = Behaviour.Block,
								MoveToExecute = Move.Block,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								OpponentTrigger = true,
								TriggerState = State.Special,
							},
						},

						Probability = 100.0f,		
					},
							
					// Vengeance on opponent Special
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.SpecialCrushingVengeanceA,

							Behaviour = new AIBehaviour
							{
								Proactive = false,
								Behaviour = Behaviour.Vengeance,
								MoveToExecute = Move.Vengeance,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								OpponentTrigger = true,
								TriggerState = State.Special,
							},
						},

						Probability = 100.0f,		
					},


					// Counter Taunt on opponent Special ( not Special Start, Special! )
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.CounterSpecial,

							Behaviour = new AIBehaviour
							{
								Proactive = false,
								Behaviour = Behaviour.Counter,
								MoveToExecute = Move.Counter,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								OpponentTrigger = true,
								TriggerState = State.Special,
							},
						},

						Probability = 100.0f,		
					},


					// Block Hold on opponent Special Start
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.BlockSpecialB,

							Behaviour = new AIBehaviour
							{
								Proactive = false,
								Behaviour = Behaviour.Block,
								MoveToExecute = Move.Block,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								OpponentTrigger = true,
								TriggerState = State.Special_Start,
							},
						},

						Probability = 100.0f,		
					},


					// Vengeance on opponent Special Start
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.SpecialCrushingVengeanceB,

							Behaviour = new AIBehaviour
							{
								Proactive = false,
								Behaviour = Behaviour.Vengeance,
								MoveToExecute = Move.Vengeance,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								OpponentTrigger = true,
								TriggerState = State.Special_Start,
							},
						},

						Probability = 100.0f,		
					},


					// Light Strike on opponent Special Start
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.StrikeAgainstSpecial,

							Behaviour = new AIBehaviour
							{
								Proactive = false,
								Mistake = true,
								Behaviour = Behaviour.Strike,
								MoveToExecute = Move.Strike_Light,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								OpponentTrigger = true,
								TriggerState = State.Special_Start,
							},
						},

						Probability = 100.0f,		
					},


					// Block Hold on opponent Vengeance
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.BlockVengeance,

							Behaviour = new AIBehaviour
							{
								Proactive = false,
								Behaviour = Behaviour.Block,
								MoveToExecute = Move.Block,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								OpponentTrigger = true,
								TriggerState = State.Vengeance,
							},
						},

						Probability = 100.0f,		
					},


					// Special on opponent Vengeance
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.SpecialAgainstVengeance,

							Behaviour = new AIBehaviour
							{
								Proactive = false,
								Mistake = true,
								Behaviour = Behaviour.Special,
								MoveToExecute = Move.Special,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								OpponentTrigger = true,
								TriggerState = State.Vengeance,
							},
						},

						Probability = 100.0f,
					},


					// Light Strike on opponent punishable priority
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.PunishWithLightStrike,

							Behaviour = new AIBehaviour
							{
								Proactive = false,
								Behaviour = Behaviour.Strike,
								MoveToExecute = Move.Strike_Light,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.PriorityChanged,
								OpponentTrigger = true,
								TriggerPriority = Fighter.Punishable_Priority,
							},
						},

						Probability = 100.0f,		
					},

					// Special on opponent punishable priority
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.PunishWithSpecial,

							Behaviour = new AIBehaviour
							{
								Proactive = false,
								Behaviour = Behaviour.Special,
								MoveToExecute = Move.Special,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.PriorityChanged,
								OpponentTrigger = true,
								TriggerPriority = Fighter.Punishable_Priority,
							},
						},

						Probability = 100.0f,		
					},


					// Roman Cancel on Hit/Shove/Block Stun frame number ( AI is trying to escape stun lock )
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.RomanCancelOnStun,

							Behaviour = new AIBehaviour
							{
								Proactive = false,
								Behaviour = Behaviour.RomanCancel,
								MoveToExecute = Move.Roman_Cancel,	
								Difficulty = AIDifficulty.Hard
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StunnedFrame, 		// hook, mid, straight or uppercut
								TriggerStunnedFrame = 2,
							},
						},

						Probability = 500.0f,		
					},


					// Block Hold on Reactive Roman Cancel
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.BlockFromRomanCancel,

							Behaviour = new AIBehaviour
							{
								Proactive = false,
								Behaviour = Behaviour.Block,
								MoveToExecute = Move.Block,		
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.RomanCancel,
							},
						},

						Probability = 300.0f,		
					},


					// Counter Taunt on Reactive Roman Cancel
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.CounterTauntFromRomanCancel,

							Behaviour = new AIBehaviour
							{
								Proactive = false,
								Behaviour = Behaviour.Counter,
								MoveToExecute = Move.Counter,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.RomanCancel,
							},
						},

						Probability = 500.0f,		
					},


					// Speed Character Light Strike on Power Character opponent Light Windup
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.SpeedCharInterceptingStrike,

							Behaviour = new AIBehaviour
							{
								Proactive = false,
								Behaviour = Behaviour.Strike,
								MoveToExecute = Move.Strike_Light,	
								FighterClass = FighterClass.Speed,
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								OpponentTrigger = true,
								TriggerState = State.Light_Windup,
								OpponentClass = FighterClass.Power,
							},
						},

						Probability = 100.0f,		
					},


					// Speed Character Special Start on Power Character opponent Special Start
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.SpeedCharInterceptingSpecial,

							Behaviour = new AIBehaviour
							{
								Proactive = false,
								Behaviour = Behaviour.Special,
								MoveToExecute = Move.Special,
								FighterClass = FighterClass.Speed,
							},
										
							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								OpponentTrigger = true,
								TriggerState = State.Special_Start,
								OpponentClass = FighterClass.Power,
							},
						},

						Probability = 100.0f,		
					},


					// Speed Character Vengeance on Power Character opponent Vengeance
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.SpeedCharInterceptingVengeance,

							Behaviour = new AIBehaviour
							{
								Proactive = false,
								Behaviour = Behaviour.Vengeance,
								MoveToExecute = Move.Vengeance,	
								FighterClass = FighterClass.Speed,
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								OpponentTrigger = true,
								TriggerState = State.Vengeance,
								OpponentClass = FighterClass.Power,
							},
						},

						Probability = 100.0f,		
					},


					// Shove on opponent 10 frames at Block Idle
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.ShoveWaiting,

							Behaviour = new AIBehaviour
							{
								Proactive = false,
								Behaviour = Behaviour.Shove,
								MoveToExecute = Move.Shove,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.BlockIdleFrame,
								OpponentTrigger = true,
								TriggerBlockIdleFrame = 10,
							},
						},

						Probability = 100.0f,		
					},


					// Shove on opponent 30 frames at Block Idle
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.ShoveDelayed,

							Behaviour = new AIBehaviour
							{
								Proactive = false,
								Behaviour = Behaviour.Shove,
								MoveToExecute = Move.Shove,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.BlockIdleFrame,
								OpponentTrigger = true,
								TriggerBlockIdleFrame = 30,
							},
						},

						Probability = 100.0f,		
					},


					// Light Strike on opponent Shove Stun
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.ShoveFollowUp,

							Behaviour = new AIBehaviour
							{
								Proactive = false,
								Behaviour = Behaviour.Strike,
								MoveToExecute = Move.Strike_Light,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								OpponentTrigger = true,
								TriggerState = State.Shove_Stun,
							},
						},

						Probability = 100.0f,		
					},


					// Counter Taunt on opponent Vengeance 10 frames in
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.CounterVengeance,

							Behaviour = new AIBehaviour
							{
								Proactive = false,
								Behaviour = Behaviour.Counter,
								MoveToExecute = Move.Counter,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.VengeanceFrame,
								OpponentTrigger = true,
								TriggerVengeanceFrame = 10,
							},
						},

						Probability = 100.0f,		
					},


					// Power Character Counter Taunt on Speed Character opponent Vengeance
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.PowerCharEarlyCounterVengeance,

							Behaviour = new AIBehaviour
							{
								Proactive = false,
								Behaviour = Behaviour.Counter,
								MoveToExecute = Move.Counter,
								FighterClass = FighterClass.Power,
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								OpponentTrigger = true,
								TriggerState = State.Vengeance,
								OpponentClass = FighterClass.Speed
							},
						},

						Probability = 100.0f,		
					},


					// Special Start on Ready_To_Die (Skeletron)
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.FinishReadyToDie,

							Behaviour = new AIBehaviour
							{
								Proactive = false,
								Behaviour = Behaviour.Special,
								MoveToExecute = Move.Special,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								OpponentTrigger = true,
								TriggerState = State.Ready_To_Die,
							},
						},

						Probability = 1000.0f,		
					},
						
			
					// PROACTIVE!

					// Release Block after 120 frames Block Idle
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.ReleaseBlock,

							Behaviour = new AIBehaviour
							{
								Proactive = true,
								Behaviour = Behaviour.Block,
								MoveToExecute = Move.ReleaseBlock,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.BlockIdleFrame,
								TriggerBlockIdleFrame = 120,
							},
						},

						Probability = 100.0f,		
					},
						
					// Shove from Idle
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.ShoveOnIdle,

							Behaviour = new AIBehaviour
							{
								Proactive = true,
								Behaviour = Behaviour.Shove,
								MoveToExecute = Move.Shove,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								TriggerState = State.Idle,
							},
						},

						Probability = 100.0f,		
					},

					// Shove on CanContinue
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.ShoveOnCanContinue,

							Behaviour = new AIBehaviour
							{
								Proactive = true,
								Behaviour = Behaviour.Shove,
								MoveToExecute = Move.Shove,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.CanContinue,
							},
						},

						Probability = 100.0f,		
					},


					// Light Strike from Idle
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.LightStrikeOnIdle,

							Behaviour = new AIBehaviour
							{
								Proactive = true,
								Behaviour = Behaviour.Strike,
								MoveToExecute = Move.Strike_Light,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								TriggerState = State.Idle,
							},
						},

						Probability = 100.0f,		
					},

					// Light Strike on CanContinue
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.LightStrikeOnCanContinue,

							Behaviour = new AIBehaviour
							{
								Proactive = true,
								Behaviour = Behaviour.Strike,
								MoveToExecute = Move.Strike_Light,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.CanContinue,
							},
						},

						Probability = 100.0f,		
					},


					// Special Start from Idle
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.SpecialStartOnIdle,

							Behaviour = new AIBehaviour
							{
								Proactive = true,
								Behaviour = Behaviour.Special,
								MoveToExecute = Move.Special,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								TriggerState = State.Idle,
							},
						},

						Probability = 100.0f,		
					},

					// Special Start on Can Continue
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.SpecialStartOnCanContinue,

							Behaviour = new AIBehaviour
							{
								Proactive = true,
								Behaviour = Behaviour.Special,
								MoveToExecute = Move.Special,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.CanContinue,
							},
						},

						Probability = 100.0f,		
					},

					// Counter from Idle ( for no reason! )
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.FoolsCounterTauntOnIdle,

							Behaviour = new AIBehaviour
							{
								Proactive = true,
								Mistake = true,
								Behaviour = Behaviour.Counter,
								MoveToExecute = Move.Counter,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								TriggerState = State.Idle,
							},
						},

						Probability = 100.0f,		
					},

					// Counter on CanContinue ( for no reason! )
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.FoolsCounterTauntOnCanContinue,

							Behaviour = new AIBehaviour
							{
								Proactive = true,
								Mistake = true,
								Behaviour = Behaviour.Counter,
								MoveToExecute = Move.Counter,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.CanContinue,
							},
						},

						Probability = 100.0f,		
					},

					// Light Strike every 15 frames at Idle
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.LightStrikeIdle,

							Behaviour = new AIBehaviour
							{
								Proactive = true,
								Behaviour = Behaviour.Strike,
								MoveToExecute = Move.Strike_Light,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.IdleFrame,
								TriggerIdleFrame = 15,
								FrameRepeat = true,
							},
						},

						Probability = 100.0f,		
					},

					// Light Strike 15 frames after CanContinue
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.LightStrikeCanContinue,

							Behaviour = new AIBehaviour
							{
								Proactive = true,
								Behaviour = Behaviour.Strike,
								MoveToExecute = Move.Strike_Light,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.CanContinueFrame,
								TriggerCanContinueFrame = 15,
							},
						},

						Probability = 100.0f,		
					},


					// Medium Strike on Light Recovery
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.MediumStrike,

							Behaviour = new AIBehaviour
							{
								Proactive = true,
								Behaviour = Behaviour.Strike,
								MoveToExecute = Move.Strike_Medium,	
							},
										
							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								TriggerState = State.Light_Recovery,
							},
						},

						Probability = 100.0f,		
					},


					// Heavy Strike on Medium Recovery
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.HeavyStrike,

							Behaviour = new AIBehaviour
							{
								Proactive = true,
								Behaviour = Behaviour.Strike,
								MoveToExecute = Move.Strike_Heavy,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								TriggerState = State.Medium_Recovery,
							},
						},

						Probability = 100.0f,		
					},


					// Chain Special on Medium Recovery
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.ChainSpecialFromMedium,

							Behaviour = new AIBehaviour
							{
								Proactive = true,
								Behaviour = Behaviour.Special,
								MoveToExecute = Move.Special,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								TriggerState = State.Medium_Recovery,
							},
						},

						Probability = 100.0f,		
					},


					// Chain Counter Attack on Medium Recovery
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.CounterAttackFromMedium,

							Behaviour = new AIBehaviour
							{
								Proactive = true,
								Behaviour = Behaviour.Counter,
								MoveToExecute = Move.Counter,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								TriggerState = State.Medium_Recovery,
							},
						},

						Probability = 100.0f,		
					},


					// Chain Special on Heavy Recovery
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.ChainSpecialFromHeavy,

							Behaviour = new AIBehaviour
							{
								Proactive = true,
								Behaviour = Behaviour.Special,
								MoveToExecute = Move.Special,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								TriggerState = State.Heavy_Recovery,
							},
						},

						Probability = 100.0f,		
					},


					// Chain Counter Attack on Heavy Recovery
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.CounterAttackFromHeavy,

							Behaviour = new AIBehaviour
							{
								Proactive = true,
								Behaviour = Behaviour.Counter,
								MoveToExecute = Move.Counter,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								TriggerState = State.Heavy_Recovery,
							},
						},

						Probability = 100.0f,		
					},


				
					// Special Extra on Special Opportunity
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.SpecialExtra,

							Behaviour = new AIBehaviour
							{
								Proactive = true,
								Behaviour = Behaviour.Special,
								MoveToExecute = Move.Special,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								TriggerState = State.Special_Opportunity,
							},
						},

						Probability = 200.0f,		
					},


					// Vengeance on Gauge OK
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.VengeanceImmediate,

							Behaviour = new AIBehaviour
							{
								Proactive = true,
								Behaviour = Behaviour.Vengeance,
								MoveToExecute = Move.Vengeance,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								TriggerState = State.Idle,
//								Condition = Condition.Gauge,
//								TriggerGauge = fighter.ProfileData.VengeanceGauge,
							},
						},

						Probability = 100.0f,		
					},


					// Vengeance 30 frames after Gauge OK
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.VengeanceWaiting,

							Behaviour = new AIBehaviour
							{
								Proactive = true,
								Behaviour = Behaviour.Vengeance,
								MoveToExecute = Move.Vengeance,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.IdleFrame,
								TriggerIdleFrame = 30,
//								Condition = Condition.GaugeIncreasedFrame,
//								TriggerGauge = fighter.ProfileData.VengeanceGauge,
//								TriggerGaugeIncreasedFrame = 30,
							},
						},

						Probability = 100.0f,		
					},


					// Vengeance 60 frames after Gauge OK
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.VengeanceDelayed,

							Behaviour = new AIBehaviour
							{
								Proactive = true,
								Behaviour = Behaviour.Vengeance,
								MoveToExecute = Move.Vengeance,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.GaugeIncreasedFrame,
								TriggerGauge = fighter.ProfileData.VengeanceGauge,
								TriggerGaugeIncreasedFrame = 60,
							},
						},

						Probability = 100.0f,		
					},


					// Roman Cancel after last hit Special ( that's the very next frame after the hit )
					// ninja and skeletron only (others last hit is last frame of state)
//					new AIPropensity
//					{
//						Attitude = Attitudes[0], 		// even
//
//						Strategy = new AIStrategy
//						{
//							Strategy = Strategy.RomanCancelFromSpecial,
//
//							Behaviour = new AIBehaviour
//							{
//								Proactive = true,
//								Behaviour = Behaviour.RomanCancel,
//								MoveToExecute = Move.Roman_Cancel,		
//							},
//
//							TriggerCondition = new AICondition
//							{
//								Condition = Condition.LastHitFrame,
//								TriggerLastHitFrame = 2,
//								TriggerState = State.Special,
//							},
//						},
//
//						Probability = 100.0f,		
//					},


					// Roman Cancel on Special Opportunity
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.RomanCancelFromSpecialOpportunity,

							Behaviour = new AIBehaviour
							{
								Proactive = true,
								Behaviour = Behaviour.RomanCancel,
								MoveToExecute = Move.Roman_Cancel,	
								Difficulty = AIDifficulty.Hard
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								TriggerState = State.Special_Opportunity,
							},
						},

						Probability = 500.0f,		
					},


					// Roman Cancel after last hit Special Extra ( that's the very next frame after the hit )
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.RomanCancelFromSpecialExtra,

							Behaviour = new AIBehaviour
							{
								Proactive = true,
								Behaviour = Behaviour.RomanCancel,
								MoveToExecute = Move.Roman_Cancel,	
								Difficulty = AIDifficulty.Hard
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.LastHitFrame,
								TriggerLastHitFrame = 2,
								TriggerState = State.Special_Extra,
							},
						},

						Probability = 500.0f,		
					},


					// Roman Cancel after last hit Vengeance ( that's the very next frame after the hit )
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.RomanCancelFromVengeance,

							Behaviour = new AIBehaviour
							{
								Proactive = true,
								Behaviour = Behaviour.RomanCancel,
								MoveToExecute = Move.Roman_Cancel,	
								Difficulty = AIDifficulty.Hard
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.LastHitFrame,
								TriggerLastHitFrame = 2,
								TriggerState = State.Vengeance,
							},
						},

						Probability = 500.0f,		
					},


					// Roman Cancel after last hit CounterAttack ( that's the very next frame after the hit )
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.RomanCancelFromCounterAttack,

							Behaviour = new AIBehaviour
							{
								Proactive = true,
								Behaviour = Behaviour.RomanCancel,
								MoveToExecute = Move.Roman_Cancel,	
								Difficulty = AIDifficulty.Hard
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.LastHitFrame,
								TriggerLastHitFrame = 2,
								TriggerState = State.Counter_Attack,
							},
						},

						Probability = 500.0f,		
					},


					// Light Strike on Proactive Roman Cancel
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.FollowUpRomanCancelLight,

							Behaviour = new AIBehaviour
							{
								Proactive = true,
								Behaviour = Behaviour.Strike,
								MoveToExecute = Move.Strike_Light,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.RomanCancel,
							},
						},

						Probability = 500.0f,		
					},


// TODO: reinstate?? perhaps
					// Shove on Proactive Roman Cancel
//					new AIPropensity
//					{
//						Attitude = Attitudes[0], 		// even
//
//						Strategy = new AIStrategy
//						{
//							Strategy = Strategy.FollowUpRomanCancelShove,
//
//							Behaviour = new AIBehaviour
//							{
//								Proactive = true,
//								Behaviour = Behaviour.Shove,
//								MoveToExecute = Move.Shove,	
//							},
//
//							TriggerCondition = new AICondition
//							{
//								Condition = Condition.RomanCancel,
//							},
//						},
//
//						Probability = 100.0f,		
//					},


					// Special Start on Proactive Roman Cancel
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.FollowUpRomanCancelSpecialStart,

							Behaviour = new AIBehaviour
							{
								Proactive = true,
								Behaviour = Behaviour.Special,
								MoveToExecute = Move.Special,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.RomanCancel,
							},
						},

						Probability = 500.0f,		
					},


					// REACTIVE MISTAKES!

					// Strike on opponent counter taunt
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.TriggerCounter,

							Behaviour = new AIBehaviour
							{
								Proactive = false,
								Mistake = true,
								Behaviour = Behaviour.Strike,
								MoveToExecute = Move.Strike_Light,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								OpponentTrigger = true,
								TriggerState = State.Counter_Taunt,
							},
						},

						Probability = 100.0f,
					},


					// Special on opponent Special Start if both Speed Class 
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.TryToRaceSpecialBothSpeedClass,

							Behaviour = new AIBehaviour
							{
								Proactive = false,
								Mistake = true,
								Behaviour = Behaviour.Special,
								MoveToExecute = Move.Special,
								FighterClass = FighterClass.Speed,
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								OpponentTrigger = true,
								TriggerState = State.Special_Start,
								OpponentClass = FighterClass.Speed,
							},
						},

						Probability = 100.0f,		
					},


					// Vengeance on opponent Vengeance if both Speed Class
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.TryToRaceVengeanceBothSpeedClass,

							Behaviour = new AIBehaviour
							{
								Proactive = false,
								Mistake = true,
								Behaviour = Behaviour.Vengeance,
								MoveToExecute = Move.Vengeance,	
								FighterClass = FighterClass.Speed,
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								OpponentTrigger = true,
								TriggerState = State.Vengeance,
								OpponentClass = FighterClass.Speed,
							},
						},

						Probability = 100.0f,		
					},


					// Special on opponent Special Start if both Power Class 
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.TryToRaceSpecialBothPowerClass,

							Behaviour = new AIBehaviour
							{
								Proactive = false,
								Mistake = true,
								Behaviour = Behaviour.Special,
								MoveToExecute = Move.Special,
								FighterClass = FighterClass.Power,
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								OpponentTrigger = true,
								TriggerState = State.Special_Start,
								OpponentClass = FighterClass.Power,
							},
						},

						Probability = 100.0f,		
					},


					// Vengeance on opponent Vengeance if both Power Class
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.TryToRaceVengeanceBothPowerClass,

							Behaviour = new AIBehaviour
							{
								Proactive = false,
								Mistake = true,
								Behaviour = Behaviour.Vengeance,
								MoveToExecute = Move.Vengeance,	
								FighterClass = FighterClass.Power,
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								OpponentTrigger = true,
								TriggerState = State.Vengeance,
								OpponentClass = FighterClass.Power,
							},
						},

						Probability = 100.0f,		
					},

					// Speed Character Counter Taunt on Power Character opponent Vengeance
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.SpeedCharCounterTooEarlyB,

							Behaviour = new AIBehaviour
							{
								Proactive = false,
								Mistake = true,
								Behaviour = Behaviour.Counter,
								MoveToExecute = Move.Counter,
								FighterClass = FighterClass.Speed,
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								OpponentTrigger = true,
								TriggerState = State.Vengeance,
								OpponentClass = FighterClass.Power,
							},
						},

						Probability = 100.0f,
					},


					// POWER-UPS (proactive)

					// Power-up on 5 frames at Idle (also according to health / gauge - see CanTriggerPowerUp)
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.PowerUpIdle,

							Behaviour = new AIBehaviour
							{
								Proactive = true,
								Behaviour = Behaviour.PowerUp,
								MoveToExecute = Move.Power_Up,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.IdleFrame,
								TriggerIdleFrame = 5,
							},
						},

						Probability = 100.0f,		
					},

					// Power-up on 5 frames after CanContinue (also according to health / gauge - see CanTriggerPowerUp)
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.PowerUpCanContinue,

							Behaviour = new AIBehaviour
							{
								Proactive = true,
								Behaviour = Behaviour.PowerUp,
								MoveToExecute = Move.Power_Up,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.CanContinueFrame,
								TriggerCanContinueFrame = 5,
							},
						},

						Probability = 100.0f,		
					},


					// PROACTIVE MISTAKES!

					// Counter on Gauge OK
					new AIPropensity
					{
						Attitude = Attitudes[0], 		// even

						Strategy = new AIStrategy
						{
							Strategy = Strategy.ImmediateFoolsCounterTaunt,

							Behaviour = new AIBehaviour
							{
								Proactive = true,
								Mistake = true,
								Behaviour = Behaviour.Counter,
								MoveToExecute = Move.Counter,	
							},

							TriggerCondition = new AICondition
							{
								Condition = Condition.StateStart,
								TriggerState = State.Idle,
//								Condition = Condition.Gauge,
//								TriggerGauge =  fighter.ProfileData.CounterGauge,
							},
						},

						Probability = 100.0f,		
					}
				}
			};
		}

		#endregion


		private void InitAttitudes()
		{
			Attitudes = new List<AIAttitude>
			{
				new AIAttitude
				{
					Attitude = Attitude.Even,
					DoNothingProbability = 100.0f,
				}, 

				new AIAttitude
				{
					Attitude = Attitude.Angry,
					DoNothingProbability = 100.0f,
				}, 

				new AIAttitude
				{
					Attitude = Attitude.Timid,
					DoNothingProbability = 100.0f,
				}, 

				new AIAttitude
				{
					Attitude = Attitude.Bold,
					DoNothingProbability = 100.0f,
				}, 

				new AIAttitude
				{
					Attitude = Attitude.Desperate,
					DoNothingProbability = 100.0f,
				}, 
			};
		}


//		private void PopulateMoves()
//		{
//			if (! DumbAI)
//				return;
//
//			moveQueue = new Queue<Move>();
//
//			moveQueue.Enqueue(Move.Shove);
//			moveQueue.Enqueue(Move.Strike_Light);
//			moveQueue.Enqueue(Move.Special);		// includes special extra
//			moveQueue.Enqueue(Move.Shove);
//			moveQueue.Enqueue(Move.Block);			// start block, will unblock after BlockFrames
//			moveQueue.Enqueue(Move.Strike_Combo);	// light - medium - heavy combo
//			moveQueue.Enqueue(Move.Shove);
////			moveQueue.Enqueue(Move.Counter);		// taunt
//			moveQueue.Enqueue(Move.Vengeance);
//		}


		#region event handlers

		protected void OnHealthChanged(FighterChangedData newState) 		// Fighter.HealthChangedDelegate signature
		{
			if (! CanExecuteStrategy())
				return;
			
			var opponent = ! newState.Fighter.UnderAI;

			ChooseHealthStrategy(newState, opponent);
		}

		public void OnGaugeChanged(FighterChangedData newState, bool stars)			// Fighter.GaugeChangedDelegate signature
		{
			if (! CanExecuteStrategy())
				return;
			
			bool opponent = ! newState.Fighter.UnderAI;
			
			// only watching self
			if (! opponent) 
			{
				// only trigger a strategy if gauge has increased!
				if (newState.NewGauge > newState.OldGauge)
					ChooseGaugeStrategy(newState, opponent);
			}
		}

		protected void OnMoveExecuted(FighterChangedData newState)
		{
			if (! CanExecuteStrategy())
				return;

			bool opponent = ! newState.Fighter.UnderAI;

			// only watching opponent (not self)
			if (opponent) 
			{
				ChooseMoveStrategy(newState, Condition.MoveExecuted, opponent);
			}
		}

		protected void OnMoveCompleted(FighterChangedData newState)
		{	
			if (! CanExecuteStrategy())
				return;

			bool opponent = ! newState.Fighter.UnderAI;

			// only watching opponent (not self)
			if (opponent)
			{
				ChooseMoveStrategy(newState, Condition.MoveCompleted, opponent);
			}
		}

		protected void OnStateStarted(FighterChangedData newState) 				// Fighter.StateStartedDelegate signature
		{
			if (! CanExecuteStrategy())
				return;
			
			bool opponent = ! newState.Fighter.UnderAI;

//			Debug.Log("OnStateStarted: " + newState.State);
			ChooseStateStrategy(newState, Condition.StateStart, opponent);
		}

		protected void OnStateEnded(FighterChangedData oldState) 				// Fighter.StateEndedDelegate signature
		{
			if (! CanExecuteStrategy())
				return;

			bool opponent = ! oldState.Fighter.UnderAI;

			ChooseStateStrategy(oldState, Condition.StateEnd, opponent);
		}
			
		public void OnPriorityChanged(FighterChangedData newState)			// Fighter.PriorityChangedDelegate signature
		{
			if (! CanExecuteStrategy())
				return;

			bool opponent = ! newState.Fighter.UnderAI;

			// only watching oponent
			if (opponent)
			{
				ChoosePriorityStrategy(newState, opponent);
			}
		}


		public void OnCanContinue(FighterChangedData newState)			// Fighter.CanContinueDelegate signature
		{
			if (! CanExecuteStrategy())
				return;

			if (newState.NewCanContinue)
			{
				bool opponent = !newState.Fighter.UnderAI;
				ChooseCanContinueStrategy(newState, opponent);
			}
		}


		public void OnLastHit(FighterChangedData newState)			// Fighter.LastHitDelegate signature
		{
			if (! CanExecuteStrategy())
				return;

			bool opponent = ! newState.Fighter.UnderAI;

			ChooseLastHitStrategy(newState, opponent);
		}

		public void OnHitStun(FighterChangedData newState)			// Fighter.HitStunDelegate signature
		{
			if (! CanExecuteStrategy())
				return;

			bool opponent = ! newState.Fighter.UnderAI;

			ChooseHitStunStrategy(newState, opponent);
		}


		public void OnRomanCancel(FighterChangedData newState)			// Fighter.RomanCancelDelegate signature
		{
			if (! CanExecuteStrategy())
				return;

			bool opponent = ! newState.Fighter.UnderAI;

			ChooseRomanCancelStrategy(newState, opponent);
		}

		public void OnIdleFrame(FighterChangedData newState)			// Fighter.IdleFrameDelegate signature
		{
			if (! CanExecuteStrategy())
				return;

			bool opponent = ! newState.Fighter.UnderAI;

			ChooseIdleFrameStrategy(newState, opponent);
		}

		public void OnBlockIdleFrame(FighterChangedData newState)			// Fighter.BlockIdleFrameDelegate signature
		{
			if (! CanExecuteStrategy())
				return;

			bool opponent = ! newState.Fighter.UnderAI;

			ChooseBlockIdleFrameStrategy(newState, opponent);
		}

		public void OnCanContinueFrame(FighterChangedData newState)			// Fighter.CanContinueFrameDelegate signature
		{
			if (! CanExecuteStrategy())
				return;

			bool opponent = ! newState.Fighter.UnderAI;

			ChooseCanContinueFrameStrategy(newState, opponent);
		}

		public void OnVengeanceFrame(FighterChangedData newState)			// Fighter.VengeanceFrameDelegate signature
		{
			if (! CanExecuteStrategy())
				return;

			bool opponent = ! newState.Fighter.UnderAI;

			ChooseVengeanceFrameStrategy(newState, opponent);
		}


		public void OnGaugeIncreasedFrame(FighterChangedData newState)		// Fighter.GaugeIncreasedFrameDelegate signature
		{
			if (! CanExecuteStrategy())
				return;

			bool opponent = ! newState.Fighter.UnderAI;

			ChooseGaugeIncreasedFrameStrategy(newState, opponent);
		}

		public void OnStunnedFrame(FighterChangedData newState)		// Fighter.StunnedFrameDelegate signature
		{
			if (! CanExecuteStrategy())
				return;

			bool opponent = ! newState.Fighter.UnderAI;

			ChooseStunnedFrameStrategy(newState, opponent);
		}


		public void OnLastHitFrame(FighterChangedData newState)		// Fighter.LastHitFrameDelegate signature
		{
			if (! CanExecuteStrategy())
				return;

//			Debug.Log("OnLastHitFrame: FrameNumber = " + newState.FrameNumber);

			bool opponent = ! newState.Fighter.UnderAI;

			ChooseLastHitFrameStrategy(newState, opponent);
		}

		public void OnKnockOutFreeze(Fighter koFighter)
		{
			// only AI can automatically trigger second life!!
			if (! (fighter.UnderAI && koFighter.UnderAI))
				return;

			// no need to go through strategy selection - simply trigger second life!
			if (fighter.TriggerPowerUp == PowerUp.SecondLife)
				fighter.PowerUp();
		}


		private bool CanExecuteStrategy()
		{
			if (StrategyCued)		// to prevent >1 triggered per beat
				return false;
			
			if (!fightManager.ReadyToFight)
				return false;

			if (fightManager.EitherFighterExpiredHealth)
				return false;

			if (!fighter.UnderAI)		// only AI has triggered behaviours
				return false;

			if (DumbAI)
				return false;

			return true;
		}

		#endregion

    }
}
