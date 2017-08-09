using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


namespace FightingLegends
{
	// data for a FighterButton
	[Serializable]
	public class FighterCard
	{
		public Button CardButton { get; private set; }				// may be null

		public string FighterName { get; private set; }
		public string FighterColour { get; private set; }

		public int Level { get; private set; }
		public bool ExpandedLevel = false;							// show Level as opposed to Lv
		public float XP { get; private set; }

		public AIDifficulty Difficulty { get; private set; }

		public PowerUp TriggerPowerUp { get; private set; }
		public PowerUp StaticPowerUp { get; private set; }
		public Sprite StaticPowerUpSprite { get; private set; }		
		public Sprite TriggerPowerUpSprite { get; private set; }	

		public Sprite FrameSprite { get; private set; }

		public bool InTeam = false;									// challenge mode (during team selection)
		public bool IsHidden = false;								// challenge mode (secret AI opposition)
		public bool IsLocked = false;		

		public Vector3 originalPosition { get; private set; }
		public Vector3 currentPosition { get { return CardButton != null ? CardButton.transform.localPosition : Vector3.zero; } }

		private Image Portrait { get { return CardButton != null ? CardButton.transform.Find("Image").GetComponent<Image>() : null; }}

		public FighterCard(Button button, string name, string colour, int level, float xpPercent, Sprite staticPowerUp, Sprite triggerPowerUp, Sprite frame, AIDifficulty difficulty = AIDifficulty.Medium, bool isLocked = false )
		{
			FighterName = name;
			FighterColour = colour;
			Difficulty = difficulty;
			InTeam = false;

			SetButton(button);
			SetProfileData(level, xpPercent, staticPowerUp, triggerPowerUp, frame, isLocked);	// shows level, power-ups and xp
		}


		public void SetProfileData(int level, float xpPercent, Sprite staticPowerUp, Sprite triggerPowerUp, Sprite frame, bool isLocked)
		{
//			Debug.Log("SetProfileData: level = " + level + ", xpPercent = " + xpPercent + ", staticPowerUp = " + staticPowerUp + ", triggerPowerUp = " + triggerPowerUp + ", isLocked = " + isLocked);
			Level = level;
			StaticPowerUpSprite = staticPowerUp;
			TriggerPowerUpSprite = triggerPowerUp;

			if (frame != null)
				FrameSprite = frame;
			
			XP = xpPercent;

			if (XP < 0)
				XP = 0;

			IsLocked = isLocked;

			SetFighterData();
		}

		public void SetButton(Button button)
		{
			CardButton = button;

			if (CardButton != null)
				originalPosition = CardButton.transform.localPosition;
		}

		public void SetFighterData()
		{
			SetButtonPowerUps();
			SetButtonLevel();
			SetButtonXPBar();
			SetButtonFrame();
			SetButtonLocked();
		}


//		public void SetButtonPortrait(Sprite portrait, bool canLock)
//		{
//			if (Portrait != null)
//				Portrait.sprite = portrait;
//
//			if (!canLock)
//				Portrait.enabled = true;
//			else
//				SetButtonLocked();		// portrait disabled if locked
//		}

		private void SetButtonLevel()
		{
			if (CardButton != null)
			{
				var lv = CardButton.transform.Find("Level");

				if (lv != null)
				{
//					Debug.Log("SetButtonLevel: " + Level);
					var levelText = lv.GetComponent<Text>();
					levelText.text = (ExpandedLevel ? "Level " : "Lv") + Level;
				}
			}
		}

		private void SetButtonPowerUps()
		{
			if (CardButton != null)
			{
				var staticPowerUp = CardButton.transform.Find("StaticPowerUp");

				if (staticPowerUp != null)
				{
					if (StaticPowerUpSprite != null)
					{
						var image = staticPowerUp.GetComponent<Image>();
						image.sprite = StaticPowerUpSprite;
						staticPowerUp.gameObject.SetActive(true);
					}
					else
					{
						staticPowerUp.gameObject.SetActive(false);
					}
				}

				var triggerPowerUp = CardButton.transform.Find("TriggerPowerUp");

				if (triggerPowerUp != null)
				{
					if (TriggerPowerUpSprite != null)
					{
						var image = triggerPowerUp.GetComponent<Image>();
						image.sprite = TriggerPowerUpSprite;
						triggerPowerUp.gameObject.SetActive(true);
					}
					else
					{
						triggerPowerUp.gameObject.SetActive(false);
					}
				}
			}
		}

		public void SetDifficulty(AIDifficulty difficulty)
		{
			Difficulty = difficulty;
		}

//		public Button StaticPowerUpButton
//		{
//			get
//			{ 
//				if (CardButton == null)
//					return null;
//
//				var staticPowerUp = CardButton.transform.Find("StaticButton");
//
//				if (staticPowerUp != null)
//					return staticPowerUp.GetComponent<Button>();
//
//				return null;
//			}
//		}
//
//		public Button TriggerPowerUpButton
//		{
//			get
//			{ 
//				if (CardButton == null)
//					return null;
//
//				var triggerPowerUp = CardButton.transform.Find("TriggerButton");
//
//				if (triggerPowerUp != null)
//					return triggerPowerUp.GetComponent<Button>();
//
//				return null;
//			}
//		}

		private void SetButtonXPBar()
		{
			if (CardButton != null)
			{
				var xpBkg = CardButton.transform.Find("XPBackground");

				if (xpBkg != null)
				{
					var xpb = xpBkg.Find("XPBar");				// stretch anchor
					var xpImage = xpb.GetComponent<Image>();
					var xpPercent = XP / Fighter.XPToNextLevel(Level);

					xpImage.rectTransform.anchorMax = new Vector2(xpPercent, 1);		// upper right corner (0,0 lower left, 1,1 upper right)
				}
			}
		}

		private void SetButtonFrame()
		{
			if (CardButton != null)
			{
				var frame = CardButton.transform.Find("Frame");

				if (frame != null)
				{
					var frameImage = frame.GetComponent<Image>();
					frameImage.sprite = FrameSprite;
				}
			}
		}

		private void SetButtonLocked()
		{
			if (CardButton != null)
			{
				var padlock = CardButton.transform.Find("Lock");

				if (padlock != null)
				{
					var lockImage = padlock.GetComponent<Image>();
					lockImage.enabled = IsLocked;
				}

				Portrait.enabled = !IsLocked;
			}
		}
			
		public void RestorePosition()
		{
			CardButton.transform.localPosition = originalPosition;
		}
	}


	[Serializable]
	public class TeamChallenge
	{
		public string ChallengeKey = "";
		public ChallengeCategory ChallengeCategory;
		public string Name = "";
		public string DateCreated = "";
		public string Location = "";
		public List<FighterCard> AITeam;

		public int PrizeCoins = 0;		// winnings

		public string UserId = "";		// "" denotes 'system' challenge
	}
}
