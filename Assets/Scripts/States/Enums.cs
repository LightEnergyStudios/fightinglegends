using UnityEngine;
using System;
using System.Collections;


namespace FightingLegends
{
	public enum SwipeDirection
	{
		None = 0,
		Right = 1,
		Left = 2,
		Down = 3,
		Up = 4,
		LeftRight = 5,
		RightLeft = 6,
		DownUp = 7,
		UpDown = 8,
	}

	public enum Move
	{
		None = 0,
		Idle = 1,				// default state
		Strike_Light = 2,		// tap
		Strike_Medium = 3,		// tap
		Strike_Heavy = 4,		// tap
		Strike_Combo = 5,		// light -> medium -> heavy (for dumb AI)
		Block = 6,				// while held
		ReleaseBlock = 7,		// when hold released
		Special = 8,			// swipe right (also special extra for water fighters)
		Counter = 9,			// taunt or attack (if chained or triggered)
		Vengeance = 10,			// swipe left-right
		Shove = 11,				// swipe down
		Roman_Cancel = 12,		// instant return to idle
		Power_Up = 13,			// swipe up - is this required?
		Tutorial_Punch = 14,	// AI only, in training mode
		Mash = 15,				// fire character special extra
		Power_Attack = 16,		// triggered by power up
	}


	// State enum allows large intervals for frame number in hit frame dictionary key
	// Strings correspond with frame labels in fighter's movie clip
	public enum State
	{
		Idle = 0,				// initial, default state

		Light_Windup = 1000,			
		Light_HitFrame = 2000,			
		Light_Recovery = 3000,			
		Light_Cutoff = 4000,

		Medium_Windup = 5000,			
		Medium_HitFrame = 6000,			
		Medium_Recovery = 7000,			
		Medium_Cutoff = 8000,

		Heavy_Windup = 9000,			
		Heavy_HitFrame = 10000,			
		Heavy_Recovery = 11000,			
		Heavy_Cutoff = 12000,

		Shove = 13000,
		Vengeance = 14000,

		Special_Start = 15000,
		Special = 16000,
		Special_Opportunity = 17000,
		Special_Extra = 18000,
		Special_Recover = 19000,

		Counter_Taunt = 20000,
		Counter_Trigger = 21000,
		Counter_Attack = 22000,
		Counter_Recovery = 23000,	

		Hit_Stun_Straight = 24000,	
		Hit_Stun_Uppercut = 25000,	
		Hit_Stun_Mid = 26000,	
		Hit_Stun_Hook = 27000,	

		Block_Idle = 28000,	
		Block_Stun = 29000,	
		Shove_Stun = 30000,	

		Hit_Straight_Die = 31000,	
		Hit_Uppercut_Die = 32000,	
		Hit_Mid_Die = 33000,	
		Hit_Hook_Die = 34000,	

		Idle_Damaged = 35000,			// skeletron only
		Fall = 36000,					// skeletron only		
		Ready_To_Die = 37000,			// skeletron only
		Die = 38000,					// skeletron only

		Tutorial_Punch_Start = 39000,	// ninja only		
		Tutorial_Punch = 40000,			// ninja only
		Tutorial_Punch_End = 41000,		// ninja only

		Dash = 50000,

		Void = 100000,					// frame label not used in game
		Default = 200000,				// to enable playthrough of all moves
	}

	public enum HealthBarZone
	{
		Yellow = 0,
		Orange = 1,
		Red = 2
	}


//	public enum Priority
//	{
//		Default = 0,				// also shove
//		Windup_Light = 1,			// windup
//		Strike_Light = 2,			// hit frame
//		Windup_Medium = 3,			// windup
//		Strike_Medium = 4,			// hit frame
//		Windup_Heavy = 5,			// windup
//		Strike_Heavy = 6,			// hit frame
//		Special_Start = 7,			// until first hit
//		Special_Hit = 8,			// first hit, until last hit
//		Special_Opportunity = 8,	// entire state
//		Special_Extra = 9,			// until last hit
//		Vengeance = 10,				// until first hit
//		Vengeance_Hit = 11,			// first hit, until last hit
//
//		Counter = 20,				// counter trigger. attack retains existing priority until last hit. recover returns to default
//
//		Tutorial_Punch_Start = 7,	// punch start and punch. punch end returns to default
//		Tutorial_Punch = 8,			// first and only hit frame
//	}

	public enum HitType
	{
		None = 0,
		Straight = 1,
		Uppercut = 2,
		Mid = 3,
		Hook = 4,
		Shove = 5 					// invokes shove stun but no damage inflicted
	}


	public enum HitStrength
	{
		Light = 0,
		Medium = 1,
		Heavy = 2,
		Power = 3				// power attack power-up
	}


	public enum FrameAction
	{
		None = 0,
		Hit = 1,			// stun / damage / freeze
		LastHit = 2,		// for travel / priority
//		CanContinue = 3,	// can be continued to next move (at and until end of state)
		SpecialFX = 4		// eg. whiff, spot effect
	}


	public enum FeedbackFXType
	{
		None = 0,
		One = 1,
		Two = 2,
		Three = 3,
		Four = 4,
		Five = 5, 
		Six = 6,
		Seven = 7,
		Eight = 8,
		Nine = 9,
		Ten = 10,
		Zero = 11, 
		Round = 12,
		KO = 13,
		Fight = 14,
		Mash = 15,
		Hold = 16,
		Press = 17,
		Press_Both = 18,
		Swipe_Forward = 19,
		Swipe_Back = 20,
		Swipe_Up = 21,
		Swipe_Down = 22,
		Swipe_Vengeance = 23,
		Armour_Down = 24,
		Armour_Up = 25,
		On_Fire = 26,
		Health_Up = 27,
		Success = 28,
		OK = 29,
		Boss_Alert = 30,
		Wrong = 31,
//		Void = 100
	}

	public enum SpotFXType
	{
		None = 0,
		Light = 1,
		Medium = 2,
		Heavy = 3,
		Block = 4,
		Miss = 5, 
		Counter = 6,
		Vengeance = 7,
		Chain = 8,
		Shove = 9,
		Roman_Cancel = 10,
		Guard_Crush = 11 
	}
			
	public enum ElementsFXType
	{
		None = 0,
		Fire = 1,
		Water = 2,
		Earth = 3,
		Air_Leoni = 4,
		Air_Natayla = 5,		// NOTE - mispelled!
		Air_Hoi_Lun = 6,
		Air_Shiyang = 7
	}

	public enum SmokeFXType
	{
		None = 0,
		Small = 1,
		Straight = 2,
		Uppercut = 3,
		Mid = 4,
		Hook = 5,
		Counter = 6
	}

	public enum LogoFrameLabel
	{
		None = 0,
		Logo = 1,
		Load = 2,
	}

	public enum FighterClass
	{
		Undefined = 0,
		Speed = 1,
		Power = 2,
		Boss = 3
	}

	public enum FighterElement
	{
		Undefined = 0,
		Fire = 1,
		Earth = 2,
		Air = 3,
		Water = 4
	}
		
	public enum UITheme
	{
		Water = 0,			// default
		Fire = 1,
		Earth = 2,
		Air = 3
	}

	public enum TrafficLight
	{
		None = 0,
		Red = 1,
		Yellow = 2,
		Green = 3,
		Left = 4,
		Right = 5,
	}

	[Flags]
	public enum InfoBubbleMessage
	{
		None = (1 << 0),
		RedLight = (1 << 1),
		YellowLight = (1 << 2),
		GreenLight = (1 << 3),
		FlashingGreenLight = (1 << 4),
		LeftArrow = (1 << 5),
		RightArrow = (1 << 6),
		Tap = (1 << 7),
		SwipeLeft = (1 << 8),
		SwipeRight = (1 << 9),
		TapBoth = (1 << 10),
		SwipeUp = (1 << 11),
		SwipeDown = (1 << 12),
		SwipeVengeance = (1 << 13),
		Hold = (1 << 14),
		Mash = (1 << 15),
		LMHComboTiming = (1 << 16),
		ResetComboTiming = (1 << 17),
		Crystals = (1 << 18),
		DojoCombat = (1 << 19),
		TrainingCombat = (1 << 20),
		ArcadeCombat = (1 << 21),
		SurvivalCombat = (1 << 22),
		ChallengeCombat = (1 << 23),
		ArcadeMenu = (1 << 24),
		SurvivalMenu = (1 << 25),
		ChallengeMenu = (1 << 26),
		DojoMenu = (1 << 27),
		FacebookMenu = (1 << 28),
		LeaderboardsMenu = (1 << 29),
		SwipeOnce = (1 << 30),
		SwipeTwice = (1 << 31),
//		CanContinue = (1 << 32),
	}

	[Flags]
	public enum MiscFlags1
	{
		None = (1 << 0),
	}

	[Flags]
	public enum MiscFlags2
	{
		None = (1 << 0),
	}

	[Flags]
	public enum MiscFlags3
	{
		None = (1 << 0),
	}

	public enum FighterChangeType
	{
		None = 0,
		MoveExecuted = 1,
		MoveCompleted = 2,
		StartState = 3,
		EndState = 4,
		Priority = 5,
		Health = 6,
		Gauge = 7,
		RomanCancel = 8,
		LastHit = 9,
		Stun = 10, 					// hook, mid, straight or uppercut + shove + block
		CanContinue = 11,			// at start
		IdleFrame = 12,				// every frame in idle state
		BlockIdleFrame = 13,		// every frame in block idle state
		CanContinueFrame = 14,		// every frame in CanContinue state
		VengeanceFrame = 15,		// every frame in vengeance state
		GaugeIncreaseFrame = 16,	// every frame since increase in gauge
		StunnedFrame = 17,			// every frame since hit / block / shove stun
		LastHitFrame = 18,			// every frame since last hit until end of state
	}
			
	public enum FightMode
	{
		Arcade = 0,					// beat each opponent in turn, then skeletron to complete game.  also training.
		Survival = 1,				// 'endless runner' - waves of opponents - last as long as possible
		Challenge = 2,				// team vs team - pick a team (pay coins) - win coins by winning challenge
		Training = 3,				// instruction (scripted training) - hawaii beach
		Dojo = 4					// training arena - practice move strings and play back to defend
	}


	public enum MenuType
	{
		None,					// initial value, until selected
		Combat,					// not really a menu - combat is the default with no menu (includes training)
		MatchStats,				// victory / defeat
		PauseSettings,			// pause / settings
		ModeSelect,				// fight mode / dojo
		ArcadeFighterSelect,	// -> world map	
		SurvivalFighterSelect,	// -> combat (random location)
		TeamSelect,				// team select (challenge mode)
		WorldMap,				// choose fight location
		Dojo,					// dojo (store)
		Facebook,				// friends, profile pic etc.
		Leaderboards,			// kudos, dojo damage, etc.
		Advert,					// watch ad for reward
	}

	public enum MenuOverlay
	{
		None,
		SpendCoins,
		BuyCoins,
		PowerUp,
		Challenges,
		Facebook,
	}

	public enum PowerUp
	{
		None,
		ArmourPiercing,		// static
		Avenger,			// static
		PoiseMaster,		// static
		PoiseWrecker,		// static
		Regenerator,		// static
		Ignite,				// trigger
		HealthBooster,		// trigger
		PowerAttack,		// trigger
		SecondLife,			// trigger
		VengeanceBooster	// trigger
	}

	public enum ChallengeCategory
	{
		None,
		Iron,
		Bronze,
		Silver,
		Gold,
		Diamond,
	}

	public enum AIDifficulty
	{
		Simple = 1,
		Easy = 2,
		Medium = 3,
		Hard = 4,
		Brutal = 5
	}

	public enum Behaviour
	{
		None = 0,
		Strike = 1,
		Vengeance = 2,
		Block = 3,
		Special = 4,
		Counter = 5,
		RomanCancel = 6,
		Shove = 7,
		PowerUp = 8,
	}

	public enum Strategy
	{
		None,
		LightStrikeOpportunist,
		BlockLight,
		StrikeCrushingSpecial,
		CounterStrike,
		StrikeCrushingVengeance,
		BlockSpecialA,
		SpecialCrushingVengeanceA,
		CounterSpecial,
		BlockSpecialB,
		SpecialCrushingVengeanceB,
		StrikeAgainstSpecial,
		BlockVengeance,
		SpecialAgainstVengeance,
		PunishWithLightStrike,
		PunishWithSpecial,
//		PunishSpecialA,
//		PunishSpecialWithSpecialA,
//		PunishSpecialB,
//		PunishSpecialWithSpecialB,
//		PunishCounterAttackWithStrike,
//		PunishCounterAttackWithSpecial,
//		PunishVengeance,
//		PunishVengeanceWithSpecial,
		RomanCancelOnStun,
		RomanCancelOnBlockStun,
		BlockFromRomanCancel,
		CounterTauntFromRomanCancel,
		SpeedCharInterceptingStrike,
		SpeedCharInterceptingSpecial,
		SpeedCharInterceptingVengeance,
		ShoveWaiting,
		ShoveDelayed,
		ShoveFollowUp,
		CounterVengeance,
		PowerCharEarlyCounterVengeance,
		ReleaseBlock,
		ReleaseBlockPriorityZero,
		ReleaseBlockPriorityPunishable,
		ShoveOnIdle,
		ShoveOnCanContinue,
		LightStrikeOnIdle,
		LightStrikeOnCanContinue,
		SpecialStartOnIdle,
		SpecialStartOnCanContinue,
		FoolsCounterTauntOnIdle,
		FoolsCounterTauntOnCanContinue,
		LightStrikeIdle,
		LightStrikeCanContinue,
		MediumStrike,
		HeavyStrike,
		ChainSpecialFromMedium,
		CounterAttackFromMedium,
		RomanCancelFromMedium,
		ChainSpecialFromHeavy,
		CounterAttackFromHeavy,
		RomanCancelFromHeavy,
		SpecialExtra,
		VengeanceImmediate,
		VengeanceWaiting,
		VengeanceDelayed,
		RomanCancelFromSpecial,
		RomanCancelFromSpecialOpportunity,
		RomanCancelFromSpecialExtra,
		RomanCancelFromVengeance,
		RomanCancelFromCounterAttack,
		FollowUpRomanCancelLight,
		FollowUpRomanCancelShove,
		FollowUpRomanCancelSpecialStart,
		TriggerCounter,
		TryToRaceSpecialBothSpeedClass,
		TryToRaceVengeanceBothSpeedClass,
		TryToRaceSpecialBothPowerClass,
		TryToRaceVengeanceBothPowerClass,
		SpeedCharCounterTooEarlyA,
		SpeedCharCounterTooEarlyB,
		CounterTooEarlyBothSpeedClass,
		CounterTooEarlyBothPowerClass,
		ImmediateFoolsCounterTaunt,
		PowerUpIdle,
		PowerUpCanContinue,
		FinishReadyToDie,
	}
		
	public enum Attitude
	{
		Even,
		Angry,
		Timid,
		Bold,
		Desperate,
	}

	public enum Condition
	{
		None = 0,
		Gauge = 1,					// as soon as gauge available
		StateStart = 2,
		StateEnd = 3,
		MoveExecuted = 4,
		MoveCompleted = 5,
		RomanCancel = 6,
		PriorityChanged = 7,
		HealthChanged = 8,
		CanContinue = 9,			// immediate
		LastHit = 10,
		HitStun = 11,				// hook, mid, straight or uppercut
		IdleFrame = 12,				// frame count at idle
		BlockIdleFrame = 13,		// frame count at block idle (opponent)
		CanContinueFrame = 14,		// frame count at CanContinue
		VengeanceFrame = 15,		// frame count at vengeance state
		GaugeIncreasedFrame = 16,	// frame count since increase in gauge
		StunnedFrame = 17,			// frame count since start of hit / block / shove stun
		LastHitFrame = 18,			// frame count since a last hit (until end of state)
	}

	public enum Leaderboard
	{
		None,
		Kudos,
		DojoDamage,
		SurvivalRounds,
		ChallengeWinnings,
	}
}

