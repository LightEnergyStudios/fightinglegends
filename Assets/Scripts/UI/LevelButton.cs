using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;


namespace FightingLegends
{
	public class LevelButton : MonoBehaviour
	{
//		public Image Background;
		public Image XP;
		public Image TriggerPowerUp;
		public Image StaticPowerUp;
		public Image Frame;
		public Text Level;

		public ParticleSystem TriggerStars;
		public ParticleSystem StaticStars;

		public Color TriggeredColour;					// icon colour when triggered (during cool down) - semi-transparent
		public Text TriggerPowerUpCoolDown;				// countdown display
		public Image TriggerCoolDown;					// grey box slowly unveils power-up

		public AudioClip TriggeredAudio;

		public AudioClip CountdownAudio;				// last 3 seconds
		private const int beepCountdown = 3;			// beep on last few seconds

		private int triggerCoolDownRemaining = 0;		// hundredths

		private float LevelUpXP = 0;
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
					fighter.OnXPChanged -= SetXP;
					fighter.OnLevelChanged -= SetLevel;

					fighter.OnKnockOut -= OnKnockOut;

					fighter.OnPowerUpTriggered -= PowerUpTriggered;
					fighter.OnStaticPowerUpApplied -= StaticPowerUpApplied;	
				}

				fighter = value;

				// initialise xp / level
				LevelUpXP = fighter.LevelUpXP;		// XP required to get to next level
				SetXP(fighter.XP);
				SetLevel(fighter.Level);

				// subscribe to new fighter events
				fighter.OnXPChanged += SetXP;
				fighter.OnLevelChanged += SetLevel;

				fighter.OnKnockOut += OnKnockOut;

				fighter.OnPowerUpTriggered += PowerUpTriggered;
				fighter.OnStaticPowerUpApplied += StaticPowerUpApplied;	
			}
		}
			

		private void SetXP(float newXP)
		{
			var xpPercent = newXP / LevelUpXP;

//			if (Fighter != null)
//				Debug.Log(Fighter.FullName + ": LevelButton.SetXP " + newXP + " (" + xpPercent + ")");

			if (Fighter.IsPlayer1)  		// upper right anchor varies with xp
				XP.rectTransform.anchorMax = new Vector2(xpPercent, 1);		// upper right corner (0,0 lower left, 1,1 upper right)
			else if (Fighter.IsPlayer2)		// lower left anchor varies with xp
				XP.rectTransform.anchorMin = new Vector2(1 - xpPercent, 0); // lower left corner (0,0 lower left, 1,1 upper right)

//			if (Fighter.IsPlayer1)
//				XP.rectTransform.SetAnchor(AnchorPresets.VStretchLeft, xpPercent, 0);
//			else
//				XP.rectTransform.SetAnchor(AnchorPresets.VStretchRight, xpPercent, 0);
		}

		private void SetLevel(int newLevel)
		{
//			if (Fighter != null)
//				Debug.Log(Fighter.FullName + ": LevelButton.SetLevel " + newLevel);
			
			Level.text = "Lv" + newLevel;
		}
			
		private void OnKnockOut(Fighter fighter)
		{
			ShowTriggerCoolDown(false);
		}

		public void SetTriggerPowerUp(Sprite triggerPowerUp, bool stars)
		{
			if (TriggerPowerUp == null)
				return;

			if (TriggerPowerUp.sprite != triggerPowerUp)
			{
				TriggerPowerUp.sprite = triggerPowerUp;
				TriggerPowerUp.color = triggerPowerUp != null ? Color.white : Color.black;

				if (stars)
					TriggerStars.Play();
			}
		}

		public void SetStaticPowerUp(Sprite staticPowerUp, bool stars)
		{
			if (StaticPowerUp == null)
				return;

			if (StaticPowerUp.sprite != staticPowerUp)
			{
				StaticPowerUp.sprite = staticPowerUp;
				StaticPowerUp.color = staticPowerUp != null ? Color.white : Color.black;

				if (stars)
					StaticStars.Play();
			}
		}

		private void ShowTriggerCoolDown(bool show)
		{
			TriggerCoolDown.gameObject.SetActive(show);
		}


		// display the trigger cool down countdown, updating every hundredth of a second (10 ms)
		private IEnumerator TriggerCoolDownCountdown(bool revealUpwards)
		{
			int coolDownTime = fighter.ProfileData.SavedData.TriggerPowerUpCoolDown;
			if (coolDownTime <= 0)
			{
				// power-up expires immediately if no countdown
				fighter.ProfileData.SavedData.TriggerPowerUp = PowerUp.None;
				yield break;
			}
				
			triggerCoolDownRemaining = coolDownTime;

			fighter.TriggerCoolingDown = true;		// can't trigger again until cool down finished
			ShowTriggerCoolDown(true);

			int lastSeconds = 0;			// to detect a change during countdown

			while (triggerCoolDownRemaining > 0 && !fighter.ExpiredHealth)	
			{
				// counter counts down!
				if (TriggerPowerUpCoolDown.gameObject.activeSelf)
				{
					int seconds = triggerCoolDownRemaining / 100;
					int hundredths = triggerCoolDownRemaining % 100;

					TriggerPowerUpCoolDown.text = string.Format("{0}:{1:00}", seconds, hundredths);

					if (seconds != lastSeconds && seconds < beepCountdown && CountdownAudio != null)
						AudioSource.PlayClipAtPoint(CountdownAudio, Vector3.zero, FightManager.SFXVolume);

					lastSeconds = seconds;
				}

				// grey overlay shrinks downwards during the cool-down time until the power-up is revealed / reactivated
				float coolDownPercent = (float)triggerCoolDownRemaining / (float)coolDownTime;
				if (revealUpwards)
					TriggerCoolDown.rectTransform.anchorMin = new Vector2(0, 1.0f - coolDownPercent);		// blind opens upwards
				else
					TriggerCoolDown.rectTransform.anchorMax = new Vector2(1.0f, coolDownPercent);			// mask empties downwards

				triggerCoolDownRemaining--;	
				yield return null;
			}

			triggerCoolDownRemaining = 0;
			TriggerPowerUp.color = Color.white;
			TriggerStars.Play();

			ShowTriggerCoolDown(false);
			fighter.TriggerCoolingDown = false;
			yield return null;
		}


		private void PowerUpTriggered(Fighter fighter, PowerUp powerUp, bool fromIdle)
		{
			PlayTriggeredAudio();
			TriggerPowerUp.color = TriggeredColour;
			TriggerStars.Play();

			if (gameObject.activeSelf && powerUp != PowerUp.None)	// eg. dojo combat mode
				StartCoroutine(TriggerCoolDownCountdown(true));		// milliseconds
		}

		private void StaticPowerUpApplied(PowerUp powerUp)
		{
//			PlayTriggeredAudio();
//			StaticStars.Play();
		}

		private void PlayTriggeredAudio()
		{
			if (TriggeredAudio != null)
				AudioSource.PlayClipAtPoint(TriggeredAudio, Vector3.zero, FightManager.SFXVolume);
		}
	}
}
