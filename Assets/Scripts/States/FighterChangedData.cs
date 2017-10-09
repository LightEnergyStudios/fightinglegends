using System;
using UnityEngine;
using System.Collections;

namespace FightingLegends
{
	public class FighterChangedData
	{
		public DateTime Time { get; private set; }
		public int AnimationFrame { get; private set; }
		public Fighter Fighter { get; private set; }

		public FighterChangeType ChangeType;

		// value(s) changed on this event for this fighter
		public Move Move = Move.None;

		public State OldState = State.Void;
		public State NewState = State.Void;

		public int OldPriority = Fighter.Default_Priority;
		public int NewPriority = Fighter.Default_Priority;

		public float OldHealth = 0.0f;
		public float NewHealth = 0.0f;

		public int OldGauge = 0;
		public int NewGauge = 0;

		public bool NewCanContinue = false;

		public int FrameNumber = 0;

		public bool CanChain = false;			// special or counter from medium/heavy strike
		public bool CanCombo = false;			// light-medium-heavy
		public bool CanSpecialExtra = false;	// 


		public FighterChangedData(Fighter changedFighter)
		{
			Fighter = changedFighter;
			Time = DateTime.Now;
			AnimationFrame = Fighter.AnimationFrameCount;

			ChangeType = FighterChangeType.None;

			// init to current fighter values - may be overridden by functions below
			SnapshotFighter();
		}

		private void SnapshotFighter()
		{
			Move = Fighter.CurrentMove;
			NewState = OldState = Fighter.CurrentState;
			NewPriority = OldPriority = Fighter.CurrentPriority;
			NewHealth = OldHealth = Fighter.ProfileData.SavedData.Health;
			NewGauge = OldGauge = Fighter.ProfileData.SavedData.Gauge;

			NewCanContinue = Fighter.CanContinue;
			CanChain = Fighter.CanChain;				// special / counter
			CanCombo = Fighter.CanCombo;				// l-m-h
			CanSpecialExtra = Fighter.CanSpecialExtra;	
		}

		public void ExecutedMove(Move moveExecuted, State fromState)
		{
			ChangeType = FighterChangeType.MoveExecuted;
			Move = moveExecuted;
			OldState = fromState;
		}

		public void CompletedMove(Move moveCompleted)
		{
			ChangeType = FighterChangeType.MoveCompleted;
			Move = moveCompleted;
		}

		public void StartedState(State newState, bool canChain, bool canCombo, bool canSpecialExtra)
		{
			ChangeType = FighterChangeType.StartState;
			NewState = newState;
			CanChain = canChain;
			CanCombo = canCombo;
			CanSpecialExtra = canSpecialExtra;
		}

		public void EndedState(State oldState)
		{
			ChangeType = FighterChangeType.EndState;
			NewState = oldState;
		}

		public void ChangedPriority(int newPriority)
		{
			ChangeType = FighterChangeType.Priority;
			NewPriority = newPriority;
		}

		public void ChangedHealth(float newHealth)
		{
			ChangeType = FighterChangeType.Health;
			NewHealth = newHealth;
		}

		public void ChangedGauge(int newGauge)
		{
			ChangeType = FighterChangeType.Gauge;
			NewGauge = newGauge;
		}
			
		public void Hit(State state)
		{
			ChangeType = FighterChangeType.Hit;
			NewState = state;
		}

		public void LastHit(State state)
		{
			ChangeType = FighterChangeType.LastHit;
			NewState = state;
		}
			
		public void Stun(State state)
		{
			ChangeType = FighterChangeType.Stun;
			NewState = state;
		}
			
		public void RomanCancel(State state)
		{
			ChangeType = FighterChangeType.RomanCancel;
			NewState = state;
		}

		public void CanContinue(bool canContinue)
		{
			ChangeType = FighterChangeType.CanContinue;
			NewCanContinue = canContinue;
		}

		public void IdleFrame(int frameNumber)
		{
			ChangeType = FighterChangeType.IdleFrame;
			FrameNumber = frameNumber;
		}

		public void BlockIdleFrame(int frameNumber)
		{
			ChangeType = FighterChangeType.BlockIdleFrame;
			FrameNumber = frameNumber;
		}

		public void CanContinueFrame(int frameNumber)
		{
			ChangeType = FighterChangeType.CanContinueFrame;
			FrameNumber = frameNumber;
		}

		public void VengeanceFrame(int frameNumber)
		{
			ChangeType = FighterChangeType.VengeanceFrame;
			FrameNumber = frameNumber;
		}

		public void GaugeIncreasedFrame(int frameNumber, int gauge)
		{
			ChangeType = FighterChangeType.GaugeIncreaseFrame;
			FrameNumber = frameNumber;
			NewGauge = gauge;
		}
			
		public void StunnedFrame(int frameNumber)
		{
			ChangeType = FighterChangeType.StunnedFrame;
			FrameNumber = frameNumber;
		}

		public void LastHitFrame(int frameNumber)
		{
			ChangeType = FighterChangeType.LastHitFrame;
			FrameNumber = frameNumber;
		}
	}
}

