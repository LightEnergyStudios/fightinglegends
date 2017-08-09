using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;


namespace FightingLegends
{
	[Serializable]
	public class HealthUI
	{
		public Text FighterName;
		public Text Score;

		public GameObject[] EmptyGauge;			// 4 (set in Inspector)
		public GameObject[] FullGauge;			// 4 (set in Inspector)
		public Animator[] GaugeAnimator;		// 4 (set in Inspector)
		public ParticleSystem[] GaugeStars;		// 4 (set in Inspector)
		public ParticleSystem[] GaugeShatter;	// 4 (set in Inspector)

		public Image HealthBar;
		public Animator HealthAnimator;			// red / orange / yellow glow
		private HealthBarZone inHealthZone = HealthBarZone.Yellow;

		public LevelButton LevelButton;

		private bool gaugeShowing = true;

		private Fighter fighter = null;

		public Fighter Fighter
		{
			get { return fighter; }

			set
			{
				if (value == null)
					return;

				if (fighter != null)
				{
					if (value.GetInstanceID() == fighter.GetInstanceID())	// no change
						return;

					// fighter changed - unsubscribe from current fighter events
					fighter.OnHealthChanged -= HealthChanged;
					fighter.OnGaugeChanged -= GaugeChanged;
//					fighter.OnPowerUpTriggered -= PowerUpTriggered;
//					fighter.OnStaticPowerUpApplied -= StaticPowerUpApplied;		

					fighter.OnScoreChanged -= UpdateScore;
				}
				
				fighter = value;

				FighterName.text = fighter.FighterName.ToUpper();

				// subscribe to new fighter events
				fighter.OnHealthChanged += HealthChanged;
				fighter.OnGaugeChanged += GaugeChanged;
//				fighter.OnPowerUpTriggered += PowerUpTriggered;
//				fighter.OnStaticPowerUpApplied += StaticPowerUpApplied;

				fighter.OnScoreChanged += UpdateScore;

				// initialise health and gauge UI
				var stateChanged = new FighterChangedData(fighter);

				stateChanged.ChangedHealth(fighter.ProfileData.SavedData.Health);
				HealthChanged(stateChanged);

				stateChanged.ChangedGauge(fighter.ProfileData.SavedData.Gauge);
				GaugeChanged(stateChanged);

				// set level button fighter + xp/level changed event listeners
				LevelButton.Fighter = fighter;
			}
		}

		// power-up sprites passed by GameUI parent (so sprites not duplicated in HealthUI each player)
		public void SetTriggerPowerUp(Sprite triggerPowerUp, bool stars)
		{
			LevelButton.SetTriggerPowerUp(triggerPowerUp, stars);
		}

		public void SetStaticPowerUp(Sprite staticPowerUp, bool stars)
		{
			LevelButton.SetStaticPowerUp(staticPowerUp, stars);
		}

		public void SetPowerUps(Sprite triggerPowerUp, Sprite staticPowerUp, bool stars)
		{
			SetTriggerPowerUp(triggerPowerUp, stars);
			SetStaticPowerUp(staticPowerUp, stars);
		}
			

		private void HealthChanged(FighterChangedData state) 		// Fighter.HealthChangedDelegate signature
		{
			var fighter = state.Fighter;

			if (fighter != Fighter)
				return;

			if (state.NewHealth < 0)
				state.NewHealth = 0;
			if (state.NewHealth > fighter.ProfileData.LevelHealth)
				state.NewHealth = fighter.ProfileData.LevelHealth;

			var healthPercent = state.NewHealth / fighter.ProfileData.LevelHealth;	// 0.0 - 1.0
			//			Debug.Log("HealthChanged: healthPercent = " + healthPercent + " scale = " + HealthBar.rectTransform.localScale);

			if (Fighter.IsPlayer1 && fighter.IsPlayer1)  		// lower left anchor varies with health
				HealthBar.rectTransform.anchorMin = new Vector2(1 - healthPercent, 0); // lower left corner (0,0 lower left, 1,1 upper right)
			else if (Fighter.IsPlayer2 && fighter.IsPlayer2)	// upper right anchor varies with health
				HealthBar.rectTransform.anchorMax = new Vector2(healthPercent, 1);		// upper right corner (0,0 lower left, 1,1 upper right)
			else
				return;		// not this fighter!

//			HealthBar.rectTransform.localScale = new Vector3(healthPercent, 1, 1);

			var redHealth = fighter.ProfileData.RedHealth;
			var orangeHealth = fighter.ProfileData.OrangeHealth;
			var newHealth = state.NewHealth;

			if (newHealth < redHealth && inHealthZone != HealthBarZone.Red)
			{
				// below 25%
				inHealthZone = HealthBarZone.Red;
				HealthAnimator.SetTrigger("HealthGlowRed");
			}
			else if (newHealth < orangeHealth && newHealth >= redHealth && inHealthZone != HealthBarZone.Orange)
			{
				// below 50%
				inHealthZone = HealthBarZone.Orange;
				HealthAnimator.SetTrigger("HealthGlowOrange");
			}
			else if (newHealth >= orangeHealth && inHealthZone != HealthBarZone.Yellow)
			{
				inHealthZone = HealthBarZone.Yellow;
				HealthAnimator.SetTrigger("HealthGlowYellow");
			}

//			if (state.NewHealth == fighter.ProfileData.LevelHealth)
//			{
//				HealthAnimator.SetTrigger("HealthGlowYellow");		// default state
//			}
//			else if (state.NewHealth < redHealth && state.OldHealth >= redHealth)
//			{
//				// dropped below 25%
//				HealthAnimator.SetTrigger("HealthGlowRed");
//			}
//			else if (state.NewHealth >= orangeHealth && state.OldHealth < redHealth)
//			{
//				// risen above 50% (eg. on training combo reset)
//				HealthAnimator.SetTrigger("HealthGlowYellow");
//			}
//			else if (state.NewHealth >= redHealth && state.OldHealth < redHealth)
//			{
//				// risen above 25%
//				HealthAnimator.SetTrigger("HealthGlowOrange");
//			}
//			else if (state.NewHealth < orangeHealth && state.OldHealth >= orangeHealth)
//			{
//				// dropped below 50%
//				HealthAnimator.SetTrigger("HealthGlowOrange");
//			}
//			else if (state.NewHealth >= orangeHealth && state.OldHealth < orangeHealth)
//			{
//				// risen above 50%
//				HealthAnimator.SetTrigger("HealthGlowYellow");
//			}
		}


		public void ShowScore(bool show)
		{
			Score.gameObject.SetActive(show);
		}

		public void SetScoreColour(Color colour)
		{
			Score.color = colour;
		}

		private void UpdateScore(int score)
		{
			Score.text = score.ToString();
		}

		private void GaugeChanged(FighterChangedData state, bool stars = true)	// Fighter.GaugeChangedDelegate signature
		{
			if (! gaugeShowing)
				return;

//			Debug.Log("GaugeChanged: OldGauge = " + state.OldGauge + " NewGauge = " + state.NewGauge);

			// empty all gems towards centre
			for (int i = 0; i < FullGauge.Length; i++)
			{
				var gauge = FullGauge.Length - i;

				if (gauge <= state.OldGauge && gauge > state.NewGauge)		// gauge decreased - shatter!
				{
					GaugeShatter[i].Play();
					FullGauge[i].SetActive(false);
				}
				else
					FullGauge[i].SetActive(false);
			}

			if (state.NewGauge <= 0)			// all empty
				return;

			// refill according to new gauge
			for (int i = 0; i < state.NewGauge; i++)
			{
				// fill from centre out
				int gemIndex = (FullGauge.Length-1) - i;

				FullGauge[gemIndex].SetActive(true);

				if (stars)
				{
					if (i > state.OldGauge - 1)		// gauge increased - stars
					{
						GaugeStars[gemIndex].Play();
					}
				}
			}
		}

		public void ShowLevel(bool show)
		{
			LevelButton.gameObject.SetActive(show);
		}

//		private void PowerUpTriggered(PowerUp powerUp)
//		{
//			LevelButton.StartTriggerCoolDown(fighter.ProfileData.SavedData.TriggerPowerUpCoolDown);
//		}
//			
//		private void StaticPowerUpApplied(PowerUp powerUp)
//		{
//			LevelButton.StaticStars.Play();
//		}
	}
}
	
