
using System;
using UnityEngine;
using System.Collections.Generic;


namespace FightingLegends
{
	// data saved for each fighter
	[Serializable]
	public class SavedProfile
	{
		public DateTime SavedTime;

		public string FighterName = "";
		public string FighterColour = "";

		public float Health = 0;				// decreased by hit and block damage
		public int Level = 1;					// increased when XP reaches threshold
		public float XP = 0;					// increased by successful strikes, blocks, combos - reset when level increased
		public float TotalXP = 0;				// never reset

		public int Gauge = 0;					// 0-4
		public float GaugeDamage = 0;			// reset when enough to trip gauge

		public bool IsLocked = false;

		public PowerUp TriggerPowerUp = PowerUp.None;
		public PowerUp StaticPowerUp = PowerUp.None;

		public int TriggerPowerUpCoolDown = 0;		// 1/100s before triggered power-up reactivates
		public int TriggerPowerUpCost = 0;			// when equipped
		public int StaticPowerUpCost = 0;			// when equipped

		public int FightStartLevel = 1;			// level as at start of fight

		public List<string> CompletedLocations = new List<string>();	// arcade mode only
		public int WorldTourCompletions = 0;							// arcade mode only

		// stats...
		public int RoundsWon { get; set; }			// in total
		public int RoundsLost { get; set; }			// in total

		public int MatchRoundsWon { get; set; }		// rounds won in current match
		public int MatchRoundsLost { get; set; }	// rounds lost in current match

		public int MatchesWon { get; set; }
		public int MatchesLost { get; set; }

		public int SimpleWins { get; set; }			// times AI fighter defeated in arcade mode
		public int SimpleLosses { get; set; }		// losses to AI fighter in arcade mode
		public int EasyWins { get; set; }			// times AI fighter defeated in arcade mode
		public int EasyLosses { get; set; }			// losses to AI fighter in arcade mode
		public int MediumWins { get; set; }			// times AI fighter defeated in arcade mode
		public int MediumLosses { get; set; }		// losses to AI fighter in arcade mode
		public int HardWins { get; set; }			// times AI fighter defeated in arcade mode
		public int HardLosses { get; set; }			// losses to AI fighter in arcade mode
		public int BrutalWins { get; set; }			// times AI fighter defeated in arcade mode
		public int BrutalLosses { get; set; }		// losses to AI fighter in arcade mode

		public int UnlockOrder { get; set; }		// to denote order in which fighters are unlocked (see SavedStatus.FighterUnlockedLevel)
		public int UnlockCoins { get; set; }			// required to unlock
		public int UnlockDefeats { get; set; }			// number of defeats required to unlock
		public AIDifficulty UnlockDifficulty { get; set; }	// difficulty level for unlock defeats

		public bool CanUnlock
		{
			get { return UnlockOrder == FightManager.SavedGameStatus.FighterUnlockedLevel+1; }		// next level to unlock
		}

		public int DeliveredHits { get; set; }		// successful hits delivered
		public int BlockedHits { get; set; }		// unsuccessful hits (blocked)

		public int HitsTaken { get; set; }			// hits taken
		public int HitsBlocked { get; set; }		// hits successfully blocked

		public float DamageInflicted { get; set; }	// health points
		public float DamageSustained { get; set; }	// health points
    }
}
