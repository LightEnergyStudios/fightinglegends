using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;


namespace FightingLegends
{
	public class FighterUnlock : MonoBehaviour
	{
		public Image panel;
		public Color panelColour;	
		private Vector3 panelScale;

		private Image background;
		public Color backgroundColour;	

		public Text FighterName;	
		public Image FighterPortrait;	

		public Text UnlockStatus;
		public Text DefeatCount;
		public Text DefeatDifficulty;
		public Text UnlockCoins;

		public ParticleSystem FighterStars;		// when unlocked
		public ParticleSystem LockStars;		// when unlocked
		public ParticleSystem Fireworks;		// when unlocked

		public Image Lock;

		public Button UnlockButton;			// with coins
		public Image CoinPanel;

		public Button OkButton;

		public Sprite leoniPortrait;
		public Sprite hoiLunPortrait;
		public Sprite danjumaPortrait;
		public Sprite alaznePortrait;
		public Sprite jacksonPortrait;
		public Sprite shiroPortrait;
		public Sprite shiyangPortrait;
		public Sprite natalyaPortrait;
		public Sprite ninjaPortrait;
		public Sprite skeletronPortrait;

		public AudioClip ShowSound;
		public AudioClip UnlockSound;
		public AudioClip OkSound;

		public float fadeTime;

		private const float unlockPauseTime = 0.75f;

		private Fighter unlockedFighter;
		private FighterCard fighterCard;
		bool unlocked = false;

//		private Action actionOnBuy;
//		private Action actionOnOk;


		void Awake()
		{
			background = GetComponent<Image>();
			background.enabled = false;
			background.color = backgroundColour;

			panel.gameObject.SetActive(false);
			panel.color = panelColour;
			panelScale = panel.transform.localScale;
		}

		private void OnEnable()
		{
			UnlockButton.onClick.AddListener(UnlockClicked);
			OkButton.onClick.AddListener(OkClicked);

			//			SetWalletValue(FightManager.Coins, false);				// set current value
		}

		private void OnDestroy()
		{
			UnlockButton.onClick.RemoveListener(UnlockClicked);
			OkButton.onClick.RemoveListener(OkClicked);
		}

//		public bool CanUnlock(bool isLocked, int unlockOrder)
//		{
//			return isLocked && unlockOrder == FightManager.SavedGameStatus.FighterUnlockedLevel+1;
//		}

		// public entry points
		public void UnlockFighter(Fighter fighter)
		{
			unlockedFighter = fighter;
			unlocked = true;

			ShowLockedFighter(unlockedFighter.FighterName, true, true);
		}
			
		public void ShowLockedFighter(FighterCard fighter)
		{
			fighterCard = fighter;
			unlocked = false;

			ShowLockedFighter(fighterCard.FighterName, fighterCard.IsLocked,
				fighterCard.CanUnlock, fighterCard.UnlockDefeats, fighterCard.UnlockDifficulty);
		}

		private void ShowLockedFighter(string fighterName, bool isLocked, bool canUnlock, int defeatCount = 0, AIDifficulty difficulty = AIDifficulty.Simple)
		{
			FighterName.text = fighterName.ToUpper();

			if (unlocked)
			{
				FighterName.text += (" " + FightManager.Translate("unlocked", false, true));
				UnlockStatus.text = FightManager.Translate("congratulations", false, true, true);

				DefeatCount.text = "";
				DefeatDifficulty.text = "";
			}
			else if (canUnlock)
			{
				UnlockStatus.text = "";

				DefeatCount.text = FightManager.Translate("defeat") + " x" + defeatCount;
				DefeatDifficulty.text = FightManager.Translate(difficulty.ToString().ToLower());
			}
			else
			{
				UnlockStatus.text = "???";
				DefeatCount.text = "";
				DefeatDifficulty.text = "";
			}

			switch (fighterName)
			{
				case "Shiro":
					FighterPortrait.sprite = shiroPortrait;
					break;

				case "Natalya":
					FighterPortrait.sprite = natalyaPortrait;
					break;

				case "Hoi Lun":
					FighterPortrait.sprite = hoiLunPortrait;
					break;

				case "Leoni":
					FighterPortrait.sprite = leoniPortrait;
					break;

				case "Danjuma":
					FighterPortrait.sprite = danjumaPortrait;
					break;

				case "Jackson":
					FighterPortrait.sprite = jacksonPortrait;
					break;

				case "Alazne":
					FighterPortrait.sprite = alaznePortrait;
					break;

				case "Shiyang":
					FighterPortrait.sprite = shiyangPortrait;
					break;

				case "Ninja":
					FighterPortrait.sprite = ninjaPortrait;
					break;

				case "Skeletron":
					FighterPortrait.sprite = skeletronPortrait;
					break;

				default:
					break;
			}
					
			// can't (pay to) unlock some fighters until others have been unlocked (UnlockLevel)
			UnlockButton.gameObject.SetActive(! unlocked && canUnlock);
			CoinPanel.gameObject.SetActive(!unlocked && canUnlock);

			Lock.gameObject.SetActive(fighterCard.IsLocked);		// should always be locked - wouldn't be here otherwise!

			StartCoroutine(Show());
		}


		private IEnumerator Show()
		{
			background.enabled = true;
			panel.gameObject.SetActive(true);

			panel.transform.localScale = Vector3.zero;
			background.color = Color.clear;

			float t = 0.0f;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / fadeTime); 

				panel.transform.localScale = Vector3.Lerp(Vector3.zero, panelScale, t);
				background.color = Color.Lerp(Color.clear, backgroundColour, t);
				yield return null;
			}
				
			if (unlocked)
			{
				yield return new WaitForSeconds(unlockPauseTime);

				Lock.gameObject.SetActive(false);
				LockStars.Play();

				FighterStars.Play();

//				Fireworks.Play();

				if (UnlockSound != null)
					AudioSource.PlayClipAtPoint(UnlockSound, Vector3.zero, FightManager.SFXVolume);
			}
			else
			{
				if (ShowSound != null)
					AudioSource.PlayClipAtPoint(ShowSound, Vector3.zero, FightManager.SFXVolume);
			}
				
			yield return null;
		}

		private IEnumerator Hide()
		{
			//			if (FadeSound != null)
			//				AudioSource.PlayClipAtPoint(FadeSound, Vector3.zero, FightManager.SFXVolume);

			float t = 0.0f;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / fadeTime); 

				panel.transform.localScale = Vector3.Lerp(panelScale, Vector3.zero, t);
				background.color = Color.Lerp(backgroundColour, Color.clear, t);
				yield return null;
			}

			background.enabled = false;
			panel.gameObject.SetActive(false);
			panel.transform.localScale = panelScale;

			yield return null;
		}

		private void UnlockClicked()
		{
//			// call the passed-in delegate
//			if (actionOnBuy != null)
//			{
//				actionOnBuy();
//			}

			//			if (BuySound != null)
			//				AudioSource.PlayClipAtPoint(BuySound, Vector3.zero, FightManager.SFXVolume);


			StartCoroutine(Hide());
		}

		private void OkClicked()
		{
			if (OkSound != null)
				AudioSource.PlayClipAtPoint(OkSound, Vector3.zero, FightManager.SFXVolume);

			StartCoroutine(Hide());
		}
	}


}
