using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace FightingLegends
{
	// Extra in-combat UI for dojo training (record / playback moves etc)
	//
	public class DojoUI : MonoBehaviour
	{
		public MoveUI TapMove;				// strike
		public MoveUI BothTapMove;			// reset
		public MoveUI SwipeLeftMove;		// counter (taunt) -> counter attack?
		public MoveUI SwipeRightMove;		// special
		public MoveUI SwipeUpMove;			// power-up
		public MoveUI SwipeDownMove;		// shove
		public MoveUI SwipeLeftRightMove;	// vengeance
		public MoveUI HoldMove;				// block
	
		public MoveUI FireExtraMove;		// fire special extra
		public MoveUI WaterExtraMove;		// water special extra

		public AudioClip PulseSound;		// pulse move
		public AudioClip EnterSound;		// entry of prompts

		public Text FollowUpFeedback;		// 'follow up'
		public Text ChainedFeedback;		// 'cued'
		public Text RecordingFeedback;
		public Text PlaybackFeedback;
		public Text ChainDamage;
		public Text ChainDamagePB;

		public Text ProfileLabel;						// fighter level / elements / class

		private bool pulsingFollowUp = false;
		private bool pulsingDamage = false;
		private bool pulsingPB = false;
		private bool pulsingRecord = false;
		private bool pulsingPlayback = false;

		private bool scrollingViewport = false;
		private int recordedMovesHidden = 0;			// number of moves hidden off top

		public ScrollRect MoveChainViewport;
		public GameObject MoveChainContent;				// vertical viewport content
		public Text MoveChainCount;	
		public AudioClip addToChainSound;

		private List<RecordedMove> recordedMoves;		// recorded by fighter for playback by shadow
		private int nextMoveIndex = 0;					// index into moveChain
		private bool playbackCompleted = false;			// true when last recorded move executed
		private int totalRecordedDamage = 0;			// total damage inflicted by recordedMoves

		private const float recordedMoveAlpha = 0.75f;	// decreases along chain
		private const float playbackMoveAlpha = 0.5f;	// dimmed when played back
		private const float dimmedMoveAlpha = 0.25f;	// dimmed to hilight next move
		private const float recordMoveTime = 0.35f;		// image animation when adding to chain
		private const float recordMoveScale = 0.5f;		// image animation when adding to chain
		private const float playbackMoveTime = 0.4f;	// image animation when playing back move
		private const float scrollMovesTime = 0.25f;	// when scrolling recorded moves
		private bool recordingMove = false;

		private const float pauseBeforePlayback = 1.0f; // seconds pause between record and playback
		private bool playbackPause = false;				// during pause before playback

		private bool recordingInProgress = false;		// recording fighter moves for opponent playback
		private bool playbackInProgress = false;		// under attack from 'shadow of self'...  =∫
		private int shadowBlockFramesRemaining = 0;		// mirrors fighter block duration

		private int playbackFrameCount = 0;				// frames since start of current playback
		private int recordingFrameCount = 0;			// frames since start of current recorded combo

		private IEnumerator playbackCoroutine = null;

		private const float recordedMoveSize = 49;		// includes gap

		private bool autoShadowPlayback = true;			// shadow repeats recordedMoves

		private bool fightersListening = false;

		private Fighter fighter = null;					// Player1
		private Fighter shadow = null;					// Player2

		private const float pulseTime = 0.25f;
		private const float pulseScale = 1.7f;
		private const float pulseTextTime = 0.1f;
		private const float pulseTextScale = 1.5f;

		private FightManager fightManager;


		private bool StateFeedbackOnly 			// no recording / playback
		{
			get { return FightManager.SavedGameStatus.NinjaSchoolFight && fightManager.RoundNumber == 1; }	
		}
			
		private void Awake()
		{
			var fightManagerObject = GameObject.Find("FightManager");
			fightManager = fightManagerObject.GetComponent<FightManager>();
		}


		public void OnEnable()
		{
			FightManager.OnReadyToFight += OnReadyToFight;

			StopRecordingFeedback();
			StartCoroutine(StopShadowPlayback(false));
			recordedMoves = new List<RecordedMove>();

			ResetRecordedMoves();
			StartCoroutine(ScrollMovesToTop(true));

			recordingInProgress = false;
			playbackInProgress = false;

//			DisableMoves();
//			GetComponent<Animator>().enabled = true;		// enter prompts

			if (StateFeedbackOnly)							// no recording and playback
			{
				MoveChainViewport.gameObject.SetActive(false);
			}
			else
			{
				ChainDamagePB.text = FightManager.Translate("best") + " " + FightManager.SavedGameStatus.BestDojoDamage;
				MoveChainViewport.gameObject.SetActive(true);
			}
		}

		public void OnDisable()
		{
			StopListening();			// if still listening
			FightManager.OnReadyToFight -= OnReadyToFight;

			scrollingViewport = false;
			recordingMove = false;

//			GetComponent<Animator>().enabled = false;
		}

		private void OnReadyToFight(bool ready, bool changed, FightMode fightMode)
		{
			if (! changed)
				return;
			
			if (ready)
			{
//				SetMoveLabels();
				StartListening();
				SetFighterProfile();
				ResetRecordedMoves();
//				SyncStateMoves(true);

				scrollingViewport = false;
				recordingMove = false;
			}
			else
			{
				StopListening();
//				DisableMoves();
			}
		}
			
		private void StartListening()
		{
			if (fightersListening)
				return;
			
			fightersListening = true;
			
			fighter = fightManager.Player1;
			shadow = fightManager.Player2;

			autoShadowPlayback = !shadow.UnderAI;

			fighter.OnStateStarted += OnStateStarted;
			fighter.OnMoveExecuted += OnMoveExecuted;
			fighter.OnContinuationCued += OnContinuationCued;
			fighter.OnComboTriggered += OnComboTriggered; 			// 'combo' = medium / heavy strike
			fighter.OnChainCounter += OnChainCounter;				// 'chain' = counter from medium/heavy strike
			fighter.OnChainSpecial += OnChainSpecial;				// 'chain' = special from medium/heavy strike
			fighter.OnSpecialExtraTriggered += OnSpecialExtraTriggered;
			fighter.OnDamageInflicted += OnDamageInflicted;
			fighter.OnCanContinue += OnCanContinue;
			fighter.OnRomanCancel += OnRomanCancel;
			fighter.OnPowerUpTriggered += OnPowerUp;
			fighter.OnKnockOut += OnKnockOut;

			if (! shadow.UnderAI)
			{
				shadow.OnStateStarted += ShadowStateStarted;		// for combos, chains and continuations
				shadow.OnHitStun += ShadowHitStun;					// stop executing chain
				shadow.OnShoveStun += ShadowShoveStun;				// stop executing chain
				shadow.OnBlockStun += ShadowBlockStun;				// stop executing chain
				shadow.OnKnockOut += OnKnockOut;
				shadow.OnPowerUpTriggered += ShadowPowerUp;
//				shadow.OnRomanCancel += ShadowRomanCancel;
			}

			FightManager.OnNextRound += OnNextRound;
		}

		private void StopListening()
		{
			if (!fightersListening)
				return;

			fightersListening = false;

			fighter.OnStateStarted -= OnStateStarted;
			fighter.OnMoveExecuted -= OnMoveExecuted;
			fighter.OnContinuationCued -= OnContinuationCued;
			fighter.OnComboTriggered -= OnComboTriggered;
			fighter.OnChainCounter -= OnChainCounter;
			fighter.OnChainSpecial -= OnChainSpecial;
			fighter.OnSpecialExtraTriggered -= OnSpecialExtraTriggered;
			fighter.OnDamageInflicted -= OnDamageInflicted;

			fighter.OnCanContinue -= OnCanContinue;
			fighter.OnRomanCancel -= OnRomanCancel;
			fighter.OnPowerUpTriggered -= OnPowerUp;

			fighter.OnKnockOut -= OnKnockOut;

			if (!shadow.UnderAI)
			{
				shadow.OnStateStarted -= ShadowStateStarted;
				shadow.OnHitStun -= ShadowHitStun;
				shadow.OnShoveStun -= ShadowShoveStun;
				shadow.OnBlockStun -= ShadowBlockStun;	
				shadow.OnKnockOut -= OnKnockOut;
				shadow.OnPowerUpTriggered -= ShadowPowerUp;
//				shadow.OnRomanCancel -= ShadowRomanCancel;
			}

			FightManager.OnNextRound -= OnNextRound;
		}

		// moves executed at the same move frame as fighter
		// shadow block is timed to same duration as fighter
		private void FixedUpdate()
		{
			if (shadow == null || shadow.UnderAI)
				return;

			if (playbackInProgress)
			{
				var shadowBlocking = shadow.IsBlockIdle;

				// countdown shadow block frames
				if (shadowBlocking)
				{
					if (shadowBlockFramesRemaining > 0)
						shadowBlockFramesRemaining--;
				}
				else
					shadowBlockFramesRemaining = 0;

				// cue a continuation the frame before it was executed to ensure the continuation will be executed
				// on completion of the current move (ie. to prevent return to idle at that point)
				// else execute next move if reached the frame when fighter executed it
				bool executeNextMove = NextRecordedMove != null &&
										(playbackFrameCount == NextMoveStartFrame-1 && NextMoveContinued && (shadow.CanContinue || shadow.IsBlocking))
											|| (playbackFrameCount == NextMoveStartFrame);

				if (executeNextMove && ! ShadowExecuteNextMove())		// eg. not enough gauge
				{
					ResetRecordedMoves();
				}

				if (shadowBlocking && shadowBlockFramesRemaining == 0)
					shadow.ReleaseBlock();					// after move execution (for continuation)

//				Debug.Log("playBackInProgress: playbackFrameCount = " + playbackFrameCount + ", shadow state = " + shadow.CurrentState);
				playbackFrameCount++;
			}
			else if (recordingInProgress)
			{
//				Debug.Log("recordingInProgress: recordingFrameCount = " + recordingFrameCount + ", fighter state = " + fighter.CurrentState);
				recordingFrameCount++;	
			}
		}


//		private void DisableMoves()
//		{
//			ActivateMove(TapMove, false, false, true);
//			ActivateMove(BothTapMove, false, false, true);
//			ActivateMove(SwipeLeftMove, false, false, true);
//			ActivateMove(SwipeRightMove, false, false, true);
//			ActivateMove(SwipeUpMove, false, false, true);
//			ActivateMove(SwipeDownMove, false, false, true);
//			ActivateMove(SwipeLeftRightMove, false, false, true);
//			ActivateMove(HoldMove, false, false, true);
//			ActivateMove(FireExtraMove, false, false, true);
//			ActivateMove(WaterExtraMove, false, false, true);
//		}

//		private void SyncStateMoves(bool animate)
//		{
//			var canStrike = (fighter.CanExecuteMove(Move.Strike_Light) || fighter.CanExecuteMove(Move.Strike_Medium) || fighter.CanExecuteMove(Move.Strike_Heavy));
//			ActivateMove(TapMove, canStrike, animate, true);
//
//			var canReset = fighter.CanExecuteMove(Move.Roman_Cancel);
//			ActivateMove(BothTapMove, canReset, animate, true);
//
//			var canCounter = fighter.HasCounter && fighter.CanCounter && (fighter.CanExecuteMove(Move.Counter) || (fighter.IsBlocking && fighter.HasCounterGauge));
//			ActivateMove(SwipeLeftMove, canCounter, animate, fighter.HasCounter);
//
//			var canSpecial = (fighter.CanExecuteMove(Move.Special) && !fighter.CanSpecialExtra) || fighter.IsBlocking;
//			ActivateMove(SwipeRightMove, canSpecial, animate, true);
//
//			var canPowerUp = fighter.CanPowerUp || fighter.IsBlocking;	 // no trigger power-up assigned - dummy only
//			ActivateMove(SwipeUpMove, canPowerUp, animate, true);
//
//			var canShove = fighter.HasShove && (fighter.CanExecuteMove(Move.Shove) || fighter.IsBlocking); 
//			ActivateMove(SwipeDownMove, canShove, animate, fighter.HasShove);
//
//			var canVengeance = (fighter.CanExecuteMove(Move.Vengeance) || (fighter.IsBlocking && fighter.HasVengeanceGauge));
//			ActivateMove(SwipeLeftRightMove, canVengeance, animate, true);
//
//			var canBlock = fighter.HasBlock && fighter.CanExecuteMove(Move.Block); 
//			ActivateMove(HoldMove, canBlock, animate, fighter.HasBlock);
//				
//			ActivateMove(FireExtraMove, fighter.CanSpecialExtraFire, animate, fighter.HasSpecialExtra);
//			ActivateMove(WaterExtraMove, fighter.CanSpecialExtraWater, animate, fighter.HasSpecialExtra);
//
////			Debug.Log("SyncStateMoves: " + "CurrentState = " + player1.CurrentState + ", canStrike = " + canStrike + ", canReset = " + canReset + ", canCounter = " + canCounter + ", canSpecial = " + canSpecial + ", canShove = " + canShove + ", canVengeance = " + canVengeance + ", canBlock = " + canBlock + ", canPowerUp = " + canPowerUp);
//		}

//		private void SetMoveLabels()
//		{
//			TapMove.SetLabel(FightManager.Translate("strike"));
//			SwipeRightMove.SetLabel(FightManager.Translate("special"));
//			SwipeLeftMove.SetLabel(FightManager.Translate("counter"));
//			SwipeLeftRightMove.SetLabel(FightManager.Translate("vengeance"));
//			FireExtraMove.SetLabel(FightManager.Translate("extra"));
//			WaterExtraMove.SetLabel(FightManager.Translate("extra"));
//			BothTapMove.SetLabel(FightManager.Translate("reset"));
//			SwipeDownMove.SetLabel(FightManager.Translate("shove"));
//			HoldMove.SetLabel(FightManager.Translate("block"));
//			SwipeUpMove.SetLabel(FightManager.Translate("powerUp"));
//		}

		private void SetFighterProfile()
		{
			if (!shadow.UnderAI)
			{
				string elementsLabel = fightManager.Player1.ElementsLabel;
				if (elementsLabel == "")
					elementsLabel = fightManager.Player1.FighterName;

				ProfileLabel.text = string.Format("{0} {1} - {2} - {3}", FightManager.Translate("level", false, false, true), fightManager.Player1.Level,
													elementsLabel, fightManager.Player1.ClassLabel);
			}

//			FireExtraMove.MoveImage.gameObject.SetActive(fightManager.Player1.IsFireElement);
//			WaterExtraMove.MoveImage.gameObject.SetActive(fightManager.Player1.IsWaterElement
//				|| fighter.ProfileData.FighterClass == FighterClass.Boss || fightManager.Player1.FighterName == "Ninja");		// TODO: bit clumsy (otherwise both extras not active)
		}

//		private void ActivateMove(MoveUI move, bool enable, bool animate, bool available)
//		{
//			bool enabled = move.Activate(enable, animate, available);
//
//			if (available && enabled && animate)
//				StartCoroutine(PulseMoveText(move));	// if enabled
//
//			StartCoroutine(move.ActivateSize());		// enlarge if active
//		}
			

		// CuedMoveFeedback has priority over CanFollowUpFeedback
		private void CuedMoveFeedback(string feedback)
		{
			// clear follow-up message if a move is cued!
			if (feedback != "")
				FollowUpFeedback.text = "";
			
			ChainedFeedback.text = feedback;
		}
			
		private void ChainFeedback()
		{
			// don't show follow-up prompt if already set
			if (ChainedFeedback.text == "")
			{
				var followUpText = FightManager.Translate("followUp", false, true);

				if (FollowUpFeedback.text != followUpText)
				{
					FollowUpFeedback.text = followUpText;
					StartCoroutine(PulseCanFollowUp());
				}
			}
		}

		private IEnumerator StopChainFeedback()
		{
			while (pulsingFollowUp)
				yield return null;
			
			FollowUpFeedback.text = "";
			ChainedFeedback.text = "";
			yield return null;
		}

		private int RecordedMoveCount
		{
			get
			{
				if (recordedMoves.Count == 0)
					return 0;
				
				return RecordedMovesTerminated ? recordedMoves.Count - 1 : recordedMoves.Count;		// last move is null if chain completed (to record frame number)
			}
		}

		private int LastMoveIndex
		{
			get { return RecordedMoveCount > 0 ? RecordedMoveCount - 1 : 0; }
		}

		private RecordedMove NextMove
		{
			get { return (RecordedMoveCount > 0) ? recordedMoves[nextMoveIndex] : null; }
		}
			
		private MoveUI NextRecordedMove
		{
			get { return (RecordedMoveCount > 0) ? recordedMoves[nextMoveIndex].MoveUI : null; }
		}

		private bool HasNextRecordedMove
		{
			get { return NextRecordedMove != null && NextRecordedMove.Move != Move.None; }
		}

		private Image NextRecordedMoveImage
		{
			get
			{
				if (nextMoveIndex >= MoveChainContent.transform.childCount)			// viewport may have been cleared by a reset
					return null;
				
				var childTransform = MoveChainContent.transform.GetChild(nextMoveIndex);

				if (childTransform != null)
					return childTransform.gameObject.GetComponent<Image>();

				return null;
			}
		}

		private bool NextMoveIsFirst
		{
			get { return nextMoveIndex == 0; }
		}

		private bool NextMoveIsLast
		{
			get { return nextMoveIndex == LastMoveIndex; }
		}

		private int FramesToNextMove
		{
			get
			{
				if (RecordedMoveCount == 0)
					return 0;

				if (NextMoveIsLast)
				{
					if (RecordedMovesTerminated)		// return frames to final return to idle
					{
						var returnToIdle = recordedMoves[recordedMoves.Count - 1];
						return returnToIdle.StartFrameNumber - recordedMoves[nextMoveIndex].StartFrameNumber;
					}
					else
						return 0;
				}
				else
					return recordedMoves[nextMoveIndex + 1].StartFrameNumber - recordedMoves[nextMoveIndex].StartFrameNumber;
			}
		}

		private int CurrentMoveStartFrame
		{
			get
			{
				if (RecordedMoveCount == 0)
					return 0;
				
				return (nextMoveIndex > 0) ? recordedMoves[nextMoveIndex-1].StartFrameNumber : 0;
			}
		}

		private int NextMoveStartFrame
		{
			get
			{
				if (RecordedMoveCount == 0)
					return 0;

				if (nextMoveIndex > LastMoveIndex)		// shouldn't happen
					return 0;

				if (NextMove == null)
					return 0;

				return NextMove.StartFrameNumber; // - CurrentMoveStartFrame;
			}
		}

		private bool RecordedMovesTerminated
		{
			// null last entry denotes the return to idle (frame number is recorded)
			get { return recordedMoves[recordedMoves.Count - 1].MoveUI == null; }
		}

		private bool NextMoveComboed
		{
			get { return RecordedMoveCount > 1 && NextRecordedMove != null && NextMove.Comboed && NextRecordedMove.Move == Move.Strike_Light; }		// ie. Tap (can't combo a chain containing a single move)
		}

		private bool NextMoveChained
		{
			get { return (RecordedMoveCount > 1 && NextRecordedMove != null && NextMove.Chained && (NextRecordedMove.Move == Move.Special || NextRecordedMove.Move == Move.Counter)); }
		}

		private bool NextMoveSpecialExtra
		{
			get { return (RecordedMoveCount > 1 && NextRecordedMove != null && NextMove.SpecialExtraTriggered && (NextRecordedMove.Move == Move.Special || NextRecordedMove.Move == Move.Mash)); }
		}

		private bool NextMoveContinued
		{
			get { return RecordedMoveCount > 1 && NextRecordedMove != null && NextMove.Continued; }
		}


		private void StartRecording()
		{
			StopPlayback();

			StartRecordingFeedback();
			recordingInProgress = true;
			recordingFrameCount = 0;
		}

		private void StartRecordingFeedback()
		{
			var feedback = "[ " + FightManager.Translate("recording") + " ]";
			if (RecordingFeedback.text != feedback)
			{
				RecordingFeedback.text = feedback;
				PlaybackFeedback.text = "";
				StartCoroutine(PulseRecording());
			}
		}

		private void StopRecording()
		{
			// null RecordedMove terminates recordedMoves with the final frame number
			if (! playbackInProgress && !shadow.UnderAI)
				TerminateRecordedMoves();

			StopRecordingFeedback();		// UI text
			recordingInProgress = false;

			StartCoroutine(StopChainFeedback());
			PlaybackFeedback.text = "";
		}

		private void StopRecordingFeedback()
		{
			RecordingFeedback.text = "";
		}


		private void StartPlayback()
		{
//			Debug.Log("StartPlayback");

			StopPlayback();

			playbackCoroutine = StartShadowPlayback();
			StartCoroutine(playbackCoroutine);
		}

		private void StopPlayback()
		{
			if (playbackCoroutine != null)
				StopCoroutine(playbackCoroutine);
		}
			
		private IEnumerator StartShadowPlayback()
		{
			if (playbackInProgress)
				yield break;

			if (fightManager.EitherFighterExpired)
				yield break;

			bool fighterIdle = fighter.IsIdle || fighter.IsBlockIdle;
			
			while (! fighterIdle || ! shadow.IsIdle)		// wait for both to return to idle
				yield return null;

			yield return new WaitForSeconds(pauseBeforePlayback);

//			Debug.Log("StartShadowPlayback");

			playbackInProgress = true;
			nextMoveIndex = 0;
			playbackCompleted = false;
			playbackFrameCount = 0;

			RecordingFeedback.text = "";

			yield return StartCoroutine(ScrollMovesToTop());

			var feedback = "[ " + FightManager.Translate("playback") + " ]";
			if (PlaybackFeedback.text != feedback)
			{
				PlaybackFeedback.text = feedback;
				StartCoroutine(PulsePlayback());
			}

			HilightAllRecordedMoves();
			yield return null;
		}

		private IEnumerator StopShadowPlayback(bool waitForFighter)
		{
//			if (waitForFighter)
//			{
//				while (!fighter.IsIdle)		// wait for fighter to return to idle
//					yield return null;
//			}

//			Debug.Log("StopShadowPlayback");

			playbackInProgress = false;
			nextMoveIndex = 0;
			PlaybackFeedback.text = "";
			ChainDamage.text = "";
			yield return null;
		}
			
		private bool ShadowExecuteNextMove()
		{
			if (!autoShadowPlayback || !playbackInProgress || RecordedMoveCount == 0 || NextRecordedMove == null || NextRecordedMove.Move == Move.None)
				return false;

			if (playbackCompleted)			// shadow has already executed last move in chain
				return false;

			// execute move
			var nextMove = NextRecordedMove.Move;
			bool moveOk = false;

			if (!shadow.CheckGauge(nextMove))
			{
				shadow.ResetMove(false);
				return false;
			}
			
			if (NextMoveSpecialExtra && shadow.CanSpecialExtra)
			{
//				Debug.Log("ShadowExecuteNextMove: TriggerSpecialExtra" + ", frame = " + playbackFrameCount);
				shadow.TriggerSpecialExtra();
				moveOk = true;
			}
			else if (NextMoveComboed && shadow.CanCombo)
			{
//				Debug.Log("ShadowExecuteNextMove: TriggerCombo" + ", frame = " + playbackFrameCount);
				shadow.TriggerCombo();
				moveOk = true;
			}
			else if (nextMove == Move.Counter && shadow.CanChain && !shadow.chainedCounter)
			{
//				Debug.Log("ShadowExecuteNextMove: TriggerChainCounter" + ", frame = " + playbackFrameCount);
				shadow.TriggerChainCounter();
				moveOk = true;
			}
			else if (nextMove == Move.Special && shadow.CanChain && !shadow.chainedSpecial)
			{
//				Debug.Log("ShadowExecuteNextMove: TriggerChainSpecial" + ", frame = " + playbackFrameCount);
				shadow.TriggerChainSpecial();
				moveOk = true;
			}
			// fall through if above triggers fail because of immediate shadow state change caused by EndState() (eg. skeletron has single frame light strike states)
			else if ((shadow.CanContinue || shadow.IsBlocking) && nextMove != Move.Roman_Cancel && nextMove != Move.Power_Up)	
			{
//				Debug.Log("ShadowExecuteNextMove: CueContinuation " + nextMove + ", frame = " + playbackFrameCount);
				shadow.CueContinuation(nextMove);		// to be triggered on completion of current move (instead of returning to idle)
				moveOk = true;
			}
			else if (shadow.CanExecuteMove(nextMove))		// checks gauge
			{
				moveOk = shadow.ExecuteMove(nextMove, false);
//				Debug.Log("ShadowExecuteNextMove: ExecuteMove " + nextMove + ", frame = " + playbackFrameCount + ", moveOk = " + moveOk);
			}

			if (moveOk)
			{
//				Debug.Log("ShadowExecuteNextMove: " + nextMove + ", state = " + shadow.CurrentState + ", continued = " + NextMove.Continued + ", chained = " + NextMove.Chained + ". comboed = " + NextMove.Comboed + ", specialExtraTriggered = " + NextMove.SpecialExtraTriggered + ", frame = " + playbackFrameCount);

				// scroll up to next move (move being executed disappears)
				StartCoroutine(ScrollRecordedMoves());

				if (nextMove == Move.Block) 	// shadow block must repeat same number of frames - start countdown
					shadowBlockFramesRemaining = FramesToNextMove;
					
				// step on to next move in chain for next time round
				IncrementNextMoveIndex();
			}
			else
			{
				shadow.WrongFeedbackFX();		// TODO: remove this!
				Debug.Log("ShadowExecuteNextMove: NOT EXECUTED nextMove = " + nextMove + ", Continued = " + NextMoveContinued + ", Chained = " + NextMoveChained + ". Comboed = " + NextMoveComboed + ", SpecialExtraTriggered = " + NextMoveSpecialExtra + ", frame = " + playbackFrameCount);
				Debug.Log(" --- STATE = " + shadow.CurrentState + ", comboTriggered = " + shadow.comboTriggered + ", chainedCounter = " + shadow.chainedCounter + ", chainedSpecial = " + shadow.chainedSpecial + ", specialExtraTriggered = " + shadow.specialExtraTriggered);

				StopPlayback();
			}

			return moveOk;
		}


		private void IncrementNextMoveIndex()
		{
			if (NextMoveIsLast)			// ie. current move - not incremented yet
				playbackCompleted = true;
			else
				nextMoveIndex++;
		}


		private void OnStateStarted(FighterChangedData state)
		{
			if (state.Fighter != fighter)
				return;

			if (fighter.IsBlockIdle || (fighter.CanContinue && !fighter.IsStunned) || fighter.CanChain || fighter.CanCombo || fighter.CanSpecialExtra)
				ChainFeedback();
			else
				StartCoroutine(StopChainFeedback());		// waits for pulse to finish

			if (fightManager.EitherFighterExpired)
				return;
			
//			SyncStateMoves(true);				// according to current state of fighter

			// return to idle - add a null frame number stamp to end of recordedMoves
			// shadow retaliates with same moves (stop recording - start playback)
			if (state.NewState == State.Idle)
			{
				if (recordingInProgress)
				{
					StopRecording();

					if (shadow.IsIdle && autoShadowPlayback && RecordedMoveCount > 0)			// eg. after block
					{
//						Debug.Log("OnStateStarted: StartPlayback on Idle from " + state.OldState);
						StartPlayback();
					}

					// update damage PB if new PB
					if (!shadow.UnderAI && totalRecordedDamage > FightManager.SavedGameStatus.BestDojoDamage)
					{
						FightManager.SavedGameStatus.BestDojoDamage = totalRecordedDamage;
						ChainDamagePB.text = FightManager.Translate("best") + " " + FightManager.SavedGameStatus.BestDojoDamage;
						StartCoroutine(PulseDamagePB());

						FirebaseManager.PostLeaderboardScore(Leaderboard.DojoDamage, totalRecordedDamage);
					}
				}
			}
			else if (state.NewState == State.Counter_Taunt)				// trigger counter attack!
			{
				if (!playbackInProgress && shadow.CanSpecial) 			// only if at idle
					shadow.CueMove(Move.Special);	
//					shadow.ExecuteMove(Move.Special, false);	
			}
		}

		// record executed move 
		private void OnMoveExecuted(FighterChangedData state, bool continued)
		{
			if (continued || state.Move == Move.Roman_Cancel || state.Move == Move.Power_Up)		// handled by own events
				return;

			if (state.Move == Move.Block)				// no point - prevents blocking playback
				return;

//			Debug.Log("OnMoveExecuted: " + state.Move);
			
			MoveUI moveUI = GetMoveUI(state.Move);

			if (moveUI != null)
			{
//				StartCoroutine(PulseMove(moveUI, pulseScale, false, true));

				if (!playbackInProgress)	// don't record if shadow playing back (playback stopped on roman cancel or when shadow stunned or returns to idle)
					StartCoroutine(RecordMove(moveUI, state.OldState, !continued, continued, false, false, false));
			}
		}
			
		// cued move feedback
		private void OnContinuationCued(Move move)
		{
			MoveUI moveUI = GetMoveUI(move);

			if (moveUI != null)
			{
//				StartCoroutine(PulseMove(moveUI, pulseScale, false, true));

				if (!playbackInProgress)	// don't record if shadow playing back (playback stopped on roman cancel or when shadow stunned or returns to idle)
				{
					// continuation may be first recorded move (eg. from block, which is not recorded)
					if (RecordedMoveCount == 0)
						StartCoroutine(RecordMove(moveUI, fighter.CurrentState, true, false, false, false, false));
					else
						StartCoroutine(RecordMove(moveUI, fighter.CurrentState, false, true, false, false, false));
				}
//					StartCoroutine(RecordMove(moveUI, fighter.CurrentState, false, true, false, false, false));
			}

			string moveLabel = "";

			switch (move)
			{
				case Move.Strike_Light:
					moveLabel = FightManager.Translate("strikeLight", false, false);
					break;
				case Move.Strike_Medium:
					moveLabel = FightManager.Translate("strikeMedium", false, false);
					break;
				case Move.Strike_Heavy:
					moveLabel = FightManager.Translate("strikeHeavy", false, false);
					break;
				case Move.Block:
					moveLabel = FightManager.Translate("block", false, false);
					break;
				case Move.Special:
					moveLabel = FightManager.Translate("special", false, false);
					break;
				case Move.Counter:
					moveLabel = FightManager.Translate("counterAttack", false, false);
					break;
				case Move.Vengeance:
					moveLabel = FightManager.Translate("vengeance", false, false);
					break;
				case Move.Shove:
					moveLabel = FightManager.Translate("shove", false, false);
					break;
				case Move.Power_Up:
					moveLabel = FightManager.Translate("powerUp", false, false);
					break;
				default:
					return;
			}
					
			CuedMoveFeedback(FightManager.Translate("cued") + " " + moveLabel + "!");
		}

		// record as move
		private void OnComboTriggered(FighterChangedData state)
		{
			if (!state.Fighter.CanCombo)			// shouldn't be here!
				return;
			
			string comboLabel = "";

			if (state.Fighter.CanStrikeMedium)
				comboLabel = FightManager.Translate("strikeMedium", false, true);
			else if (state.Fighter.CanStrikeHeavy)
				comboLabel = FightManager.Translate("strikeHeavy", false, true);
			
			CuedMoveFeedback(FightManager.Translate("cued") + " " + comboLabel + "!");

//			StartCoroutine(PulseMove(TapMove, pulseScale, false, true));

			if (!playbackInProgress)	// don't record if shadow playing back (playback stopped on roman cancel or when shadow stunned or returns to idle)
				StartCoroutine(RecordMove(TapMove, state.OldState, false, false, false, true, false));			// TODO: correct?
		}

		// record as move
		private void OnChainCounter(FighterChangedData state)
		{
			CuedMoveFeedback(FightManager.Translate("cued") + " " + FightManager.Translate("counterAttack", false, true));

//			StartCoroutine(PulseMove(SwipeLeftMove, pulseScale, false, true));

			if (!playbackInProgress)	// don't record if shadow playing back (playback stopped on roman cancel or when shadow stunned or returns to idle)
				StartCoroutine(RecordMove(SwipeLeftMove, state.OldState, false, false, true, false, false));
		}

		// record as move
		private void OnChainSpecial(FighterChangedData state)
		{
			CuedMoveFeedback(FightManager.Translate("cued") + " " + FightManager.Translate("special", false, true));

//			StartCoroutine(PulseMove(SwipeRightMove, pulseScale, false, true));

			if (!playbackInProgress)	// don't record if shadow playing back (playback stopped on roman cancel or when shadow stunned or returns to idle)
				StartCoroutine(RecordMove(SwipeRightMove, state.OldState, false, false, true, false, false));
		}
			
		// record as move
		private void OnSpecialExtraTriggered(FighterChangedData state)
		{
			CuedMoveFeedback(FightManager.Translate("cued") + " " + FightManager.Translate("specialExtra", false, true));

			var specialExtraMove = state.Fighter.IsWaterElement ? WaterExtraMove : FireExtraMove;

//			StartCoroutine(PulseMove(specialExtraMove, pulseScale, false, true));

			if (!playbackInProgress)	// don't record if shadow playing back (playback stopped on roman cancel or when shadow stunned or returns to idle)
				StartCoroutine(RecordMove(specialExtraMove, state.OldState, false, false, false, false, true));
		}
			
		// record move
		private void OnRomanCancel(FighterChangedData state)
		{
			if (state.Fighter != fighter)
				return;

//			StartCoroutine(PulseMove(BothTapMove, pulseScale, false, true));

			if (!playbackInProgress)	// don't record if shadow playing back (playback stopped on roman cancel or when shadow stunned or returns to idle)
				StartCoroutine(RecordMove(BothTapMove, state.OldState, false, false, false, false, false));

			// fighter roman cancel stops playback
			if (playbackInProgress)
			{
//				StartCoroutine(StopShadowPlayback(false));
				ResetRecordedMoves();
			}
		}

		// record move
		private void OnPowerUp(Fighter powerUpFighter, PowerUp powerUp, bool fromIdle)
		{
			if (powerUpFighter != fighter)
				return;

//			StartCoroutine(PulseMove(SwipeUpMove, pulseScale, false, true));

			if (!playbackInProgress)	// don't record if shadow playing back (playback stopped on roman cancel or when shadow stunned or returns to idle)
				StartCoroutine(RecordPowerUp(powerUpFighter, fromIdle));
		}

		private IEnumerator RecordPowerUp(Fighter powerUpFighter, bool fromIdle)
		{
			yield return StartCoroutine(RecordMove(SwipeUpMove, powerUpFighter.CurrentState, fromIdle, !fromIdle, false, false, false));

			if (fromIdle)
			{
				StopRecording();
				StartPlayback();				// waits for shadow to return to idle
			}
		}
			
		// follow-up feedback
		private void OnCanContinue(FighterChangedData state)
		{
			if (state.Fighter != fighter)
				return;

			if (fightManager.EitherFighterExpired)
				return;

			if (state.NewCanContinue && !fighter.IsStunned)
				ChainFeedback();

//			SyncStateMoves(false);
		}

		// damage inflicted on shadow by fighter
		private void OnDamageInflicted(float damage)
		{
			totalRecordedDamage += (int)damage;
			if (totalRecordedDamage > 0)		// eg. shove == 0
			{
				ChainDamage.text = FightManager.Translate("damage") + " " + totalRecordedDamage;
				StartCoroutine(PulseDamage());
			}
		}

		private void OnKnockOut(Fighter fighter)
		{
//			Debug.Log("OnKnockOut! " + fighter.FullName);

			scrollingViewport = false;
			recordingMove = false;

			ResetRecordedMoves();
//			DisableMoves();
		}

		private void OnNextRound(int roundNumber)
		{
//			ResetRecordedMoves();
//			DisableMoves();
		}
			
		// on return to idle - stop playback or start playback if moves are recorded
		private void ShadowStateStarted(FighterChangedData state)
		{
			if (state.Fighter != shadow)
				return;

			if (state.NewState == State.Idle || fightManager.EitherFighterExpired)
			{
				if (playbackInProgress)
				{
					StartCoroutine(StopShadowPlayback(true));
					ResetRecordedMoves();
				}
				else if (autoShadowPlayback && RecordedMoveCount > 0)
				{
					StartPlayback();
				}
			}
		}
			
//		private void ShadowRomanCancel(FighterChangedData state)
//		{
//			if (state.Fighter != shadow)
//				return;
//
//			Debug.Log("ShadowRomanCancel: NextRecordedMove = " + NextRecordedMove.Move);
//			ShadowExecuteNextMove();			// cue continuation if there is one (CanContinue not yet set at this point)
//		}

		private void ShadowPowerUp(Fighter powerUpFighter, PowerUp powerUp, bool fromIdle)
		{
			if (powerUpFighter != shadow)
				return;
			
			if (playbackInProgress && NextMoveIsLast)		// stop playback if power-up is last move - hasn't stepped on after move execution yet
			{
				ResetRecordedMoves();
				shadow.ResetMove(false);		// resets powerUpTriggered flag, clears cued moves, returns to idle, etc
			}
			else 
				shadow.ResetPowerUpTrigger();	// resets powerUpTriggered flag
		}

		private void ShadowHitStun(FighterChangedData state)
		{
			if (state.Fighter != shadow)
				return;

			if (playbackInProgress)		// knocked out of playback
			{
				StartCoroutine(StopShadowPlayback(false));
				ResetRecordedMoves();
			}
		}

		private void ShadowShoveStun(FighterChangedData state)
		{
			if (state.Fighter != shadow)
				return;

			if (playbackInProgress)		// shoved out of playback
			{
				StartCoroutine(StopShadowPlayback(false));
				ResetRecordedMoves();
			}
		}
			
		private void ShadowBlockStun(FighterChangedData state)
		{
			if (state.Fighter != shadow)
				return;

			if (playbackInProgress)		// hit while blocking
			{
				StartCoroutine(StopShadowPlayback(false));
				ResetRecordedMoves();
			}
		}


		private MoveUI GetMoveUI(Move move)
		{
			switch (move)
			{
				case Move.Strike_Light:
					return TapMove;

				case Move.Strike_Medium:
					return TapMove;

				case Move.Strike_Heavy:
					return TapMove;

				case Move.Block:
					return HoldMove;

				case Move.Special:
					return SwipeRightMove;

				case Move.Counter:
					return SwipeLeftMove;

				case Move.Vengeance:
					return SwipeLeftRightMove;

				case Move.Shove:
					return SwipeDownMove;

				case Move.Power_Up:
					return SwipeUpMove;

				case Move.Roman_Cancel:
					return BothTapMove;

				default:
					return null;
			}
		}


		private IEnumerator RecordMove(MoveUI moveUI, State fromState, bool startNewChain, bool continued, bool chained, bool comboed, bool specialExtraTriggered)
		{
//			Debug.Log("RecordMove: moveUI = " + (moveUI == null ? "null!" : moveUI.MoveLabel.text) + ", startNewChain = " + startNewChain);
			if (moveUI == null)
				yield break;

			if (fightManager.EitherFighterExpiredHealth)
				yield break;

			if (shadow.UnderAI)
				yield break;

			if (moveUI.Move == Move.Block)	// never record block
				yield break;

			if (playbackInProgress)			// don't record if shadow playing back (playback stopped on roman cancel or when shadow stunned or returns to idle)
				yield break;

			playbackCompleted = false;

			if (startNewChain) 				// start new MoveUI chain
			{
				while (recordingMove)		// wait until animation finished
					yield return null;
				
				ResetRecordedMoves();
				StartCoroutine(ScrollMovesToTop());

				StartRecording();
			}

			recordedMoves.Add(new RecordedMove(recordingFrameCount, moveUI, fromState, continued, chained, comboed, specialExtraTriggered));
//			Debug.Log("RecordMove: " + moveUI.Move + ", STATE = " + fromState + ", continued = " + continued + ", chained = " + chained + ", comboed = " + comboed  + ", specialExtraTriggered = " + specialExtraTriggered + ", frame = " + recordingFrameCount);

			yield return StartCoroutine(AnimateRecordMove(moveUI));		// image animation
			UpdateMoveCountUI();
		}

		private IEnumerator AnimateRecordMove(MoveUI move, bool reverse = false)
		{
			if (RecordedMoveCount == 0)
				yield break;

			while (recordingMove)			// wait until previous animation finished
				yield return null;

			recordingMove = true;

			// create a new image based on the last move button
			var moveImage = move.DuplicateImage;

			moveImage.transform.SetParent(transform);
			moveImage.transform.localScale = Vector3.one;	

//			var startScale = Vector3.one;
//			var targetScale = new Vector3(recordMoveScale, recordMoveScale, recordMoveScale);
//
//			float t = 0;
//			Vector3 startPosition = move.MoveImage.rectTransform.localPosition;						// position of move button image
//			Vector3 targetPosition = ChainMovePosition(reverse ? 1 : RecordedMoveCount, true);		// centre of first / next position in viewport
//			Color startColour = Color.white;
//			Color targetColour = new Color(moveImage.color.r, moveImage.color.g, moveImage.color.b, moveImage.color.a * playbackMoveAlpha);
//
			ScrollViewportIfFull();
//
//			while (t < 1.0f)
//			{
//				t += Time.deltaTime * (Time.timeScale / recordMoveTime);
//
//				moveImage.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
//				moveImage.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
//				moveImage.color = Color.Lerp(startColour, targetColour, t);
//				yield return null;
//			}

			if (addToChainSound != null)
				AudioSource.PlayClipAtPoint(addToChainSound, Vector3.zero, FightManager.SFXVolume);

			// add image to move chain viewport content (which has a content size fitter component)
			moveImage.transform.SetParent(MoveChainContent.transform);
			moveImage.transform.localScale = Vector3.one;	
			moveImage.transform.localPosition = Vector3.zero;	
//			moveImage.color = startColour;

//			AdjustChainAlpha(reverse);

			recordingMove = false;
			yield return null;
		}

		private IEnumerator AnimatePlaybackNextMove()
		{
			if (RecordedMoveCount == 0)
				yield break;

			if (MoveChainContent.transform.childCount == 0)
				yield break;

			if (NextRecordedMove == null)
				yield break;

			if (nextMoveIndex >= MoveChainContent.transform.childCount)			// viewport may have been cleared by a reset
				yield break;
			
			// duplicate next recorded move image from viewport
			var nextMove = MoveChainContent.transform.GetChild(nextMoveIndex);
			var moveImage = NextRecordedMove.DuplicateImage;
			moveImage.transform.SetParent(transform);
			moveImage.transform.localScale = new Vector3(recordMoveScale, recordMoveScale, recordMoveScale);
			moveImage.transform.localPosition = nextMove.transform.localPosition;

			float t = 0;
//			Vector3 startPosition = ChainMovePosition(1, true);										// centre of first in viewport
			Vector3 startPosition = ChainMovePosition(0, false);									// top of viewport
			Vector3 targetPosition = NextRecordedMove.MoveImage.rectTransform.localPosition;		// position of move button image

			Color startColour = Color.white;
			Color targetColour = new Color(moveImage.color.r, moveImage.color.g, moveImage.color.b, moveImage.color.a * playbackMoveAlpha);

			recordedMovesHidden = RecordedMoveCount - ViewportCapacity;
			UpdateMoveCountUI();

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / playbackMoveTime);

				moveImage.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
				moveImage.color = Color.Lerp(startColour, targetColour, t);
				yield return null;
			}

			Destroy(moveImage.gameObject);
			yield return null;
		}


		private int ViewportCapacity
		{
			get
			{
				Rect viewportRect = MoveChainViewport.GetComponent<Image>().rectTransform.rect;
				return (int)(viewportRect.height / recordedMoveSize);
			}
		}

		private void UpdateMoveCountUI()
		{
			if (playbackInProgress)
				MoveChainCount.text = "x " + (RecordedMoveCount - recordedMovesHidden);
			else
				MoveChainCount.text = "x " + RecordedMoveCount;
		}

		// scroll back to top (first move)
		private IEnumerator ScrollMovesToTop(bool init = false)
		{
			while (scrollingViewport)
				yield return null;

			scrollingViewport = true;

			var startPosition = MoveChainContent.transform.localPosition;
			var targetPosition = Vector3.zero;
			float t = 0;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / scrollMovesTime);

				MoveChainContent.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
				yield return null;
			}

			recordedMovesHidden = 0;

			if (! init)
				UpdateMoveCountUI();
			
			scrollingViewport = false;
		}
			
		private void ScrollViewportIfFull()
		{
			var currentOverflow = recordedMovesHidden;
			recordedMovesHidden = RecordedMoveCount - ViewportCapacity;
			UpdateMoveCountUI();

			if (recordedMovesHidden <= 0 || recordedMovesHidden == currentOverflow)		// no need to scroll
				return;

			StartCoroutine(ScrollRecordedMoves(recordedMovesHidden - currentOverflow));
		}
			
		private IEnumerator ScrollRecordedMoves(int numMoves = 1)		// -numMoves to scroll down
		{
			while (scrollingViewport)
				yield return null;

			recordedMovesHidden += numMoves;
			UpdateMoveCountUI();

			scrollingViewport = true;

			var startPosition = MoveChainContent.transform.localPosition;
			var targetPosition = new Vector3(startPosition.x, startPosition.y + (recordedMoveSize * numMoves), startPosition.z);
			float t = 0;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / scrollMovesTime);

				MoveChainContent.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
				yield return null;
			}
				
			UpdateMoveCountUI();
			scrollingViewport = false;
		}

		private void TerminateRecordedMoves()
		{
			// null RecordedMove terminates moveChain with the final frame number (added on fighter return to idle)
			recordedMoves.Add(new RecordedMove(recordingFrameCount, null, State.Void, false, false, false, false));
		}

		private void ResetRecordedMoves()
		{
//			Debug.Log("ResetRecordedMoves");
			recordedMoves.Clear();
			recordedMovesHidden = 0;

			totalRecordedDamage = 0;
			playbackFrameCount = 0;

			// clear move chain viewport
			foreach (Transform move in MoveChainContent.transform)
			{
				Destroy(move.gameObject);
			}

			StartCoroutine(StopChainFeedback());

			ChainDamage.text = "";
			MoveChainCount.text = "";

			StopRecordingFeedback();
			recordingInProgress = false;

			StartCoroutine(StopShadowPlayback(false));
		}

		private Vector3 ChainMovePosition(int moveIndex, bool centre)
		{
			Vector3 viewportPosition = MoveChainViewport.transform.localPosition;
			Rect viewportRect = MoveChainViewport.GetComponent<Image>().rectTransform.rect;
			float viewportSize = viewportRect.height;

			int maxInViewport = (int)(viewportSize / recordedMoveSize);
			var viewportCount = (moveIndex > maxInViewport) ? maxInViewport : moveIndex;
			var viewportStart = viewportPosition.y + (viewportSize / 2.0f);
			float moveOffset = (viewportCount * recordedMoveSize) - (centre ? (recordedMoveSize / 2.0f) : 0);

			return new Vector3(viewportPosition.x, viewportStart - moveOffset, viewportPosition.z);
		}


		private void HilightAllRecordedMoves()
		{
			foreach (Transform child in MoveChainContent.transform)
			{
				var childImage = child.gameObject.GetComponent<Image>();
				childImage.color = Color.white;
			}
		}

		private void DimAllRecordedMoves()
		{
			foreach (Transform child in MoveChainContent.transform)
			{
				var childImage = child.gameObject.GetComponent<Image>();
				childImage.color = new Color(1,1,1, dimmedMoveAlpha);
			}
		}

//		private void HilightNextRecordedMove()
//		{
//			DimAllRecordedMoves();
//
//			var nextMove = MoveChainContent.transform.GetChild(nextMoveIndex);
//			if (nextMove != null)
//			{
//				var nextMoveImage = nextMove.gameObject.GetComponent<Image>();
//				nextMoveImage.color = Color.white;
//			}
//		}


		// work backwards through chain to set increasing alpha
		// set reverse to true to reorder in viewport so last move is at the top
		private void AdjustChainAlpha(bool reverse)
		{
			// ...first make a list from viewport content children
			var moveList = new List<Image>();
			var alpha = 1.0f;
			int reverseCounter = 0;

			foreach (Transform child in MoveChainContent.transform)
			{
				var childImage = child.gameObject.GetComponent<Image>();
				moveList.Add(childImage);
			}

			for (int i = moveList.Count-1; i >= 0; i--)
			{
				var move = moveList[i];
				var moveColour = move.color;

				reverseCounter++;

				move.color = new Color(moveColour.r, moveColour.g, moveColour.b, 0);		// vanish momentarily!

				if (reverse)  	// reorder viewport contents (in reverse) so that the latest is top of the list
					move.transform.SetSiblingIndex(reverseCounter);

				if (reverseCounter > 1)			// first at full alpha
					alpha *= recordedMoveAlpha;

				move.color = new Color(moveColour.r, moveColour.g, moveColour.b, alpha);
			}
		}

		private IEnumerator PulseCanFollowUp()
		{
			if (pulsingFollowUp)
				yield break;

			pulsingFollowUp = true;
			yield return StartCoroutine(FightManager.PulseText(FollowUpFeedback));
			pulsingFollowUp = false;
			yield return null;
		}

		private IEnumerator PulseDamage()
		{
			if (pulsingDamage)
				yield break;

			pulsingDamage = true;
			yield return StartCoroutine(FightManager.PulseText(ChainDamage));
			pulsingDamage = false;
			yield return null;
		}

		private IEnumerator PulseDamagePB()
		{
			if (pulsingPB)
				yield break;

			pulsingPB = true;
			yield return StartCoroutine(FightManager.PulseText(ChainDamagePB, PulseSound));
			pulsingPB = false;
			yield return null;
		}

		private IEnumerator PulseRecording()
		{
			if (pulsingRecord)
				yield break;

			pulsingRecord = true;
			yield return StartCoroutine(FightManager.PulseText(RecordingFeedback));
			pulsingRecord = false;

			if (!recordingInProgress)
				RecordingFeedback.text = "";

			yield return null;
		}

		private IEnumerator PulsePlayback()
		{
			if (pulsingPlayback)
				yield break;

			pulsingPlayback = true;
			yield return StartCoroutine(FightManager.PulseText(PlaybackFeedback));
			pulsingPlayback = false;

			if (!playbackInProgress)
			{
				PlaybackFeedback.text = "";
			}

			yield return null;
		}
			
//		private IEnumerator PulseMove(MoveUI move, float scale, bool stars, bool tick)
//		{
//			if (move.pulseMoveCoroutine != null)
//				StopCoroutine(move.pulseMoveCoroutine);
//
//			move.pulseMoveCoroutine = move.Pulse(scale, pulseTime, PulseSound, stars, tick);
//			yield return StartCoroutine(move.pulseMoveCoroutine);
//		}
	

//		private IEnumerator PulseMoveText(MoveUI move)
//		{
//			if (! enabled)
//				yield break;
//			
//			if (move.pulseTextCoroutine != null)
//				StopCoroutine(move.pulseTextCoroutine);
//
//			move.pulseTextCoroutine = move.PulseText(pulseTextScale, pulseTextTime);
//			yield return StartCoroutine(move.pulseTextCoroutine);
//		}

		public void EnterPromptSound()
		{
			if (EnterSound != null)
				AudioSource.PlayClipAtPoint(EnterSound, Vector3.zero, FightManager.SFXVolume);
		}
	}


	// class to encapsulate an executed move and its relative animation frame number
	public class RecordedMove
	{
		public int StartFrameNumber { get; private set; }		// offset from record start frame (0)
		public MoveUI MoveUI { get; private set; }

		public State FromState { get; private set; }			// fighter state at time of move execution
		public bool Continued { get; private set; }
		public bool Chained { get; private set; }
		public bool Comboed { get; private set; }
		public bool SpecialExtraTriggered { get; private set; }

		public RecordedMove(int frame, MoveUI move, State fromState, bool continued, bool chained, bool comboed, bool specialExtraTriggered)
		{
			StartFrameNumber = frame;
			MoveUI = move;

			FromState = fromState;

			Continued = continued;
			Chained = chained;
			Comboed = comboed;
			SpecialExtraTriggered = specialExtraTriggered;
		}
	}
}