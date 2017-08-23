using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


namespace FightingLegends
{
	// extended FighterSelect for non-arcade mode (via ModeSelect, TeamSelect and Store)
	// incorporating level, xp and power-ups loaded from saved fighter profiles
	//
	public class SurvivalSelect : FighterSelect
	{
		// power-up sprites for card inlays
		public Sprite ArmourPiercing;		// set in Inspector
		public Sprite Avenger;
		public Sprite Ignite;
		public Sprite HealthBooster;
		public Sprite PoiseMaster;
		public Sprite PoiseWrecker;
		public Sprite PowerAttack;
		public Sprite Regenerator;
		public Sprite SecondLife;
		public Sprite VengeanceBooster;
	

//		// virtual method called OnEnable
//		protected override void InitFighterCards()
//		{
//			base.InitFighterCards();
//
//			// lookup level, xp and power-ups from fighter profile (saved) data (all fighters)
//			SetCardProfiles();
//		}

		// lookup level, xp and power-ups from each fighter profile (saved) data
		protected override void SetCardProfiles()
		{
//			Debug.Log("SetCardProfiles");

			foreach (var card in fighterCards)
			{
				var fighterName = card.Key;
				var fighterCard = card.Value;

				var profile = Profile.GetFighterProfile(fighterName);
				if (profile != null)
				{
//					fighterCard.SetProfileData(fighterCard.Level, fighterCard.XP, PowerUpSprite(fighterCard.StaticPowerUp), PowerUpSprite(fighterCard.TriggerPowerUp), CardFrame(fighterName));
					fighterCard.SetProfileData(profile.Level, profile.XP, PowerUpSprite(profile.StaticPowerUp), PowerUpSprite(profile.TriggerPowerUp), CardFrame(fighterName),
										profile.IsLocked, profile.CanUnlock, profile.UnlockOrder, profile.UnlockDefeats, profile.UnlockDifficulty);
//					Debug.Log("SetProfileData: " + fighterName + " - static = " + profile.StaticPowerUp + ", trigger = " + profile.TriggerPowerUp);
				}
			}
		}
			
		private Sprite PowerUpSprite(PowerUp powerUp)
		{
			switch (powerUp)
			{
				case PowerUp.None:
				default:
					return null;

				case PowerUp.ArmourPiercing:
					return ArmourPiercing;

				case PowerUp.Avenger:
					return Avenger;

				case PowerUp.Ignite:
					return Ignite;

				case PowerUp.HealthBooster:
					return HealthBooster;

				case PowerUp.PoiseMaster:
					return PoiseMaster;

				case PowerUp.PoiseWrecker:
					return PoiseWrecker;

				case PowerUp.PowerAttack:
					return PowerAttack;

				case PowerUp.Regenerator:
					return Regenerator;

				case PowerUp.SecondLife:
					return SecondLife;

				case PowerUp.VengeanceBooster:
					return VengeanceBooster;
			}
		}
	}
}
