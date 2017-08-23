
using System;
using UnityEngine;
using System.Collections.Generic;


namespace FightingLegends
{
	// data saved for each fighter
	[Serializable]
	public class ProfileData
	{
		[HideInInspector]
		public SavedProfile SavedData = new SavedProfile();

		public float InitialHealth = 500.0f;

		// LevelFactor increases damage inflicted and initial health (survival, challenge and dojo only)
		private const float levelFactorPercent = 10.0f;
		public float LevelFactor
		{
			get
			{
				if (FightManager.CombatMode == FightMode.Arcade || FightManager.CombatMode == FightMode.Training)	// always level 1
					return 0;
				
				return (float)(SavedData.Level-1) / levelFactorPercent;
			}
		}	

		public float LevelHealth { get { return InitialHealth + (InitialHealth * LevelFactor); } }
		public float LevelAR { get { return AttackRating + (AttackRating * LevelFactor); } }

		public float OrangeHealth { get { return (LevelHealth * 0.5f); } }
		public float RedHealth { get { return (LevelHealth * 0.25f); } }

		public int AttackRating;	// initial value - LevelAR scaled by Level

		public FighterClass FighterClass = FighterClass.Undefined;

		public FighterElement Element1 = FighterElement.Undefined;
		public FighterElement Element2 = FighterElement.Undefined;

//		public int UnlockLevel = 0;				// to denote order in which fighters are unlocked (see SavedStatus.FighterUnlockedLevel)
//		public int UnlockDefeats = 3;			// number of defeats required to unlock
//		public AIDifficulty UnlockDifficulty = AIDifficulty.Easy;	// difficulty level for unlock defeats
//
//		public bool CanUnlock
//		{
//			get { return UnlockLevel == FightManager.SavedGameStatus.FighterUnlockedLevel+1; }
//		}

		public float ArmourDownDamageFactor;	// air - increased damage
		public float ArmourUpDamageFactor;		// earth - reduced damage
		public float OnFireDamagePerTick;		// fire	- stops on a hit	
		public float HealthUpBoost;				// water - single boost

//		[Tooltip("Seconds taken to travel forwards at start of strike")]
		public float StrikeTravelTime;			// time taken to travel in for an attack
		public float RecoilTravelTime;			// time taken to travel back to default position
		public float AttackDistance;			// same for all hits - enough movement to 'make contact'

		public int RomanCancelFreezeFrames;		// both fighters

		public float ExpiryTime;				// seconds until next round
		public float ExpiryDistance;			// travel distance over ExpiryTime

		public float AIDoNothingFactor;			// increase for less aggressive AI behaviour
		public float AIHealthBoostLevel = 250.0f;	// can health boost power-up if health is below this
		public int AIVengeanceBoostLevel = 2;	// can vengeance boost power-up if gauge is below this level

		public float HitDamageFactor;			// increase for more damage

		public int RomanCancelGauge;			// required to execute move
		public int VengeanceGauge;				// required to execute move
		public int CounterGauge;				// required to execute move (from idle)

		public float DamagePerGaugeLozenge;		// damage required to fill a gauge crystal
		public float LevelDamagePerGauge 		{ get { return DamagePerGaugeLozenge + (DamagePerGaugeLozenge * LevelFactor); } }

//		public bool ShowDamageOnLastHit;		// only show total damage on last hit (else every hit)

		// static power-ups
		public float PoiseWreckerFactor = 2.0f;	// opponent hit / block stun duration increased
		public float PoiseMasterFactor = 0.5f;	// hit / block stun duration reduced
		public float AvengerFactor = 2.0f;		// gauge fills faster
		public float RegeneratorFactor = 0.25f;	// health increase per tick
		public float ArmourPiercingFactor = 2.0f; // increased damage through opponents's block

		// trigger power-ups
		public float PowerAttackDamageFactor = 4.0f; // heavy strike - increased damage


		public AudioClip lightWhiff;
		public AudioClip mediumWhiff;
		public AudioClip heavyWhiff;
		public AudioClip specialWhiff;
		public AudioClip counterWhiff;
		public AudioClip vengeanceWhiff;

		public AudioClip missSound;
		public AudioClip counterTriggerSound;
		public AudioClip romanCancelSound;
		public AudioClip blockedHitSound;
		public AudioClip waterSound;
		public AudioClip fireSound;
		public AudioClip airSound;
		public AudioClip earthSound;
    }
}
