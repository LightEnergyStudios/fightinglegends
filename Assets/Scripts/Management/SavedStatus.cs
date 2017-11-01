
using System;
using UnityEngine;
using System.Collections.Generic;


namespace FightingLegends
{
	// data saved for player data and also a fight in progress
	[Serializable]
	public class SavedStatus
	{
		public string VersionNumber = "1.0";
		public int LimitedVersion = 0;					// without extra assets? free version?

		public int PlayCount = 0;						// times game started

		public DateTime SavedTime;

		public string UserId = "";						// via user registration

		// player status

		public float Kudos = 0;							// to allow very large numbers!
		public int Coins = 0;

		public int BestDojoDamage = 0;					// PB damage from chain in Dojo mode
		public int BestSurvivalEndurance = 0;			// PB rounds won in survival mode
		public int TotalChallengeWinnings = 0;			// total challenge mode coins won 
		public int WorldTourCompletions = 0;			// arcade mode - for all fighters
		public int VSVictoryPoints = 0;					// network fight

		public bool CompletedBasicTraining = false;	

		// settings
		public float SFXVolume = 0.5f;
		public float MusicVolume = 0.5f;

		public bool ShowTrainingNarrative = false;			// show narrative panel during training
		public bool ShowStateFeedback = true;				// show state feedback + fireworks
		public bool ShowHud = true;							// show HUD
		public bool ShowInfoBubbles = true;					// once only - uness reset
		public bool ShowDojoUI = false;						// moves / animation

		public AIDifficulty Difficulty = AIDifficulty.Easy;
		public UITheme Theme = UITheme.Water;

		// power up inventory
		public List<InventoryPowerUp> PowerUpInventory = new List<InventoryPowerUp>();

		// fight status
		public bool FightInProgress = false;
		public bool NinjaSchoolFight = false;		// first fight after training
		public float FightStartKudos = 0;			// kudos as at start of fight
		public int FightStartCoins = 0;				// coins as at start of fight (can gain in survival mode)

		public FightMode CombatMode;
		public string FightLocation = "";

		public string SelectedFighterName = "Leoni";			// selected in fighter select menu
		public string SelectedFighterColour = "P1";

		// info bubble message flags
		public InfoBubbleMessage InfoMessagesRead = InfoBubbleMessage.None;

		public MiscFlags1 Flags1 = MiscFlags1.None;
		public MiscFlags2 Flags2 = MiscFlags2.None;
		public MiscFlags3 Flags3 = MiscFlags3.None;

		public SavedProfile Player1;
		public SavedProfile Player2;				// AI - in order to restore fight

		public SavedProfile[] playerTeam;
		public SavedProfile[] AITeam;

		public int FighterUnlockedLevel = 0;		// see UnlockLevel for fighters

		// player stats...

		public int SimpleArcadeWins { get; set; }		// times AI fighter defeated in arcade mode
		public int SimpleArcadeLosses { get; set; }		// losses to AI fighter in arcade mode
		public int EasyArcadeWins { get; set; }			// times AI fighter defeated in arcade mode
		public int EasyArcadeLosses { get; set; }		// losses to AI fighter in arcade mode
		public int MediumArcadeWins { get; set; }		// times AI fighter defeated in arcade mode
		public int MediumArcadeLosses { get; set; }		// losses to AI fighter in arcade mode
		public int HardArcadeWins { get; set; }			// times AI fighter defeated in arcade mode
		public int HardArcadeLosses { get; set; }		// losses to AI fighter in arcade mode
		public int BrutalArcadeWins { get; set; }		// times AI fighter defeated in arcade mode
		public int BrutalArcadeLosses { get; set; }		// losses to AI fighter in arcade mode

		public int TotalArcadeWins { get { return SimpleArcadeWins + EasyArcadeWins + MediumArcadeWins + HardArcadeWins + BrutalArcadeWins; }  }
		public int TotalArcadeLosses { get { return SimpleArcadeLosses + EasyArcadeLosses + MediumArcadeLosses + HardArcadeLosses + BrutalArcadeLosses; }  }

		public int RoundsWon { get; set; }			// in total
		public int RoundsLost { get; set; }			// in total

		public int MatchesWon { get; set; }
		public int MatchesLost { get; set; }

		public int SuccessfulHits { get; set; }		// successful hits delivered
		public int BlockedHits { get; set; }		// unsuccessful hits (blocked)

		public int HitsTaken { get; set; }			// hits taken
		public int HitsBlocked { get; set; }		// hits successfully blocked

		public float DamageInflicted { get; set; }	// health points
		public float DamageSustained { get; set; }	// health points
    }


	[Serializable]
	// represents a power-up 'in stock'
	public class InventoryPowerUp
	{
		public PowerUp PowerUp { get; set; }
		public int Quantity { get; set; }			// quantity 'in stock'
		public DateTime TimeCreated { get; set; }
		public DateTime TimeUpdated { get; set; }

//		public float Lifetime = 0;					// 
//		public float LifeReamining = 0;
	}
}
