﻿using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace FightingLegends
{
	public class MatchStats : MenuCanvas
	{
		public Image Background;				// black semi-transparent
		public Text WinnerName;					// winner's name
		public Image WinnerPhoto;				// place holder for winner sprite
	
		public Text WinQuote;
		public Text Stats;						// damage, hits, etc... (not currently used)

		public GameObject WinnerStatsPanel;
		public Text LevelLabel;
		public Image KudosLabel;
		public Image CoinsLabel;
		public Text LevelUp;
		public Text KudosUp;
		public Text CoinsUp;
		public ParticleSystem LevelUpFireworks;
		public ParticleSystem KudosUpFireworks;
		public ParticleSystem CoinsUpFireworks;

		public ParticleSystem Stars;

		public float kudosUpPause;
		public float levelUpPause;
		public float coinsUpPause;
		public int kudosBlingInterval;			// bling sound

		public InsertContinueCoin insertCoin;

		public Sprite leoniWin;
		public Sprite hoiLunWin;
		public Sprite danjumaWin;
		public Sprite alazneWin;
		public Sprite jacksonWin;
		public Sprite shiroWin;
		public Sprite shiyangWin;
		public Sprite natalyaWin;
		public Sprite ninjaWin;
		public Sprite skeletronWin;

		public AudioClip photoAudio;
		public AudioClip statsAudio;

		private Animator resultsAnimator;

		private bool inputAllowed = false;
		private float statsInterval = 0.1f;		// between each line

		private const float fadeInTime = 0.5f;		// win quote

		private FightManager fightManager;
		private Fighter winner;

		public GameObject ChallengePanel;
		public FighterButton Player1Button;
		public FighterButton Player2Button;
//		public FighterCard Player1Card;
//		public FighterCard Player2Card;

		private FeedbackUI feedbackUI;

		public GameObject WorldTourPanel;
		public Text CongratsText;
		public Text WorldTourText;
		public ParticleSystem WorldTourFireworks;
		public AudioClip WorldTourSound;

		private const float worldTourCongratsTime = 0.1f;

		private List<ChallengeRoundResult> challengeResults;

		private bool worldTourComplete = false;


		public override bool CanNavigateBack { get { return false; } }


		public void Awake()
		{
			resultsAnimator = GetComponent<Animator>();

			var fightManagerObject = GameObject.Find("FightManager");
			fightManager = fightManagerObject.GetComponent<FightManager>();

			var feedbackUIObject = GameObject.Find("FeedbackUI");
			if (feedbackUIObject != null)
				feedbackUI = feedbackUIObject.GetComponent<FeedbackUI>();
		}

		// initialization
		public void Start()
		{
			inputAllowed = false;
		}

		private void OnEnable()
		{
			feedbackUI.feedbackFX.OnEndState += FeedbackStateEnd;
		}

		private void OnDisable()
		{
			feedbackUI.feedbackFX.OnEndState -= FeedbackStateEnd;
		}

		private void Update() 
		{
			if (inputAllowed && ((Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began) || Input.GetMouseButtonDown(0)))	// left button
			{
				if (FightManager.CombatMode == FightMode.Arcade && !FightManager.SavedGameStatus.NinjaSchoolFight)
				{
//					if (winner != null && !winner.UnderAI)							// player won - back to mode select (eg. dojo)
//						fightManager.MatchStatsChoice = MenuType.ModeSelect;		// exits match stats
					if (winner != null)
					{
						if (!winner.UnderAI)							// player won - choose next location
						{
							if (worldTourComplete)
								fightManager.MatchStatsChoice = MenuType.ModeSelect;		// exits match stats
							else
								fightManager.MatchStatsChoice = MenuType.WorldMap;			// exits match stats
						}
						else   // AI won - didn't wait for countdown or insert coin to continue
							fightManager.MatchStatsChoice = MenuType.ModeSelect;
					}
				}
				else
				{
					fightManager.MatchStatsChoice = MenuType.ModeSelect;			// exits match stats
				}
			}
		}

		private void InsertCoinCountdown(Action actionOnContinue, Action actionOnExit, string message = null)
		{
			insertCoin.gameObject.SetActive(true);
			insertCoin.Countdown(actionOnContinue, actionOnExit, message);

			inputAllowed = true;		// tap to exit
		}

		private void ArcadeContinue()
		{
//			Debug.Log("ArcadeContinue");
			insertCoin.gameObject.SetActive(false);
			fightManager.MatchStatsChoice = MenuType.Combat;			// to exit match stats (same menu canvas, so does nothing else)
			fightManager.MatchStatsRestartMatch = true;					// same fighters and location
		}

		private void ArcadeExit()
		{
//			Debug.Log("ArcadeExit");
			insertCoin.gameObject.SetActive(false);
			fightManager.ResetWorldTour();
			fightManager.MatchStatsChoice = MenuType.ModeSelect;		// exits match stats
		}

	
		public void RevealWinner(Fighter victor, bool completedWorldTour)
		{
//			Debug.Log("RevealWinner: winner = " + (victor == null ? " NULL!" : victor.FullName));

			if (victor == null)
				return;
			
			winner = victor;
			var loser = winner.Opponent;

			worldTourComplete = completedWorldTour;
			WorldTourPanel.SetActive(false);				// animated reveal

			WinnerStatsPanel.SetActive(!worldTourComplete);

			Reset();

			WinnerName.text = winner.FighterName.ToUpper() + " " + FightManager.Translate("wins");

			switch (winner.FighterName)
			{
				case "Shiro":
					WinnerPhoto.sprite = shiroWin;
//					WinnerName.text = "SHIRO WINS!";
					break;

				case "Natalya":
					WinnerPhoto.sprite = natalyaWin;
//					WinnerName.text = "NATALYA WINS!";
					break;

				case "Hoi Lun":
					WinnerPhoto.sprite = hoiLunWin;
//					WinnerName.text = "HOI LUN WINS!";
					break;

				case "Leoni":
					WinnerPhoto.sprite = leoniWin;
//					WinnerName.text = "LEONI WINS!";
					break;

				case "Danjuma":
					WinnerPhoto.sprite = danjumaWin;
//					WinnerName.text = "DANJUMA WINS!";
					break;

				case "Jackson":
					WinnerPhoto.sprite = jacksonWin;
//					WinnerName.text = "JACKSON WINS!";
					break;

				case "Alazne":
					WinnerPhoto.sprite = alazneWin;
//					WinnerName.text = "ALAZNE WINS!";
					break;

				case "Shiyang":
					WinnerPhoto.sprite = shiyangWin;
//					WinnerName.text = "SHIYANG WINS!";
					break;

				case "Ninja":
					WinnerPhoto.sprite = ninjaWin;
//					WinnerName.text = "NINJA WINS!";
					break;

				case "Skeletron":
					WinnerPhoto.sprite = skeletronWin;
//					WinnerName.text = "SKELETRON WINS!";
					break;
	
				default:
					break;
			}

			// switch direction of winner photo according to P1 / P2
			if (WinnerPhoto.sprite != null)
				WinnerPhoto.transform.localScale = winner.IsPlayer1 ? new Vector3(-0.5f, 0.5f, 0.5f) : new Vector3(0.5f, 0.5f, 0.5f);

			WinQuote.text = "\"" + winner.WinQuote(loser.FighterName) + "\"";		// virtual
			StatsAudio();

			WinnerPhoto.gameObject.SetActive(true);
			WinnerName.gameObject.SetActive(true);

			Stats.text = "";

			// TODO: why must P1 and P2 be reversed?
			if (winner.IsPlayer1)
				resultsAnimator.SetTrigger("Player2Winner");		// animate portrait entry from left
			else
				resultsAnimator.SetTrigger("Player1Winner");		// animate portrait entry from right
		}


		private IEnumerator WinnerStats()
		{
			WinnerStatsPanel.SetActive(true);

			var player = winner.UnderAI ? winner.Opponent : winner;
			int kudosGained = (int)(FightManager.Kudos - FightManager.SavedGameStatus.FightStartKudos);
			int levelGained = player.Level - player.ProfileData.SavedData.FightStartLevel;
			int coinsGained = FightManager.Coins - FightManager.SavedGameStatus.FightStartCoins;

			KudosLabel.gameObject.SetActive(true);
			KudosUp.gameObject.SetActive(true);
			KudosUp.text = string.Format("{0:N0}", FightManager.SavedGameStatus.FightStartKudos);

			if (kudosGained > 0)
			{
				for (int kudos = (int)FightManager.SavedGameStatus.FightStartKudos+1; kudos <= (int)FightManager.Kudos; kudos++)
				{
//					yield return new WaitForSeconds(kudosUpPause);
					KudosUp.text = string.Format("{0:N0}", kudos);

					if (kudos % kudosBlingInterval == 0)
					{
						yield return new WaitForSeconds(kudosUpPause);
						fightManager.BlingAudio();
					}
				}
				KudosUpFireworks.Play();
			}
			else
			{
				KudosUp.text = string.Format("{0:N0}", FightManager.Kudos);
				fightManager.BlingAudio();
			}

			if (FightManager.CombatMode == FightMode.Survival || FightManager.CombatMode == FightMode.Challenge)
			{
				yield return new WaitForSeconds(levelUpPause);
				LevelLabel.gameObject.SetActive(true);
				LevelUp.gameObject.SetActive(true);
				LevelUp.text = player.ProfileData.SavedData.FightStartLevel.ToString();

				if (levelGained > 0)
				{
					for (int level = player.ProfileData.SavedData.FightStartLevel + 1; level <= player.Level; level++)
					{
						yield return new WaitForSeconds(levelUpPause);
						LevelUp.text = level.ToString();
						fightManager.BlingAudio();
					}
					LevelUpFireworks.Play();
				}
				else
				{
					LevelUp.text = player.Level.ToString();
					fightManager.BlingAudio();
				}

				if (FightManager.CombatMode == FightMode.Survival || FightManager.CombatMode == FightMode.Challenge)
				{
					yield return new WaitForSeconds(coinsUpPause);
					CoinsLabel.gameObject.SetActive(true);
					CoinsUp.gameObject.SetActive(true);
					CoinsUp.text = string.Format("{0:N0}", FightManager.SavedGameStatus.FightStartCoins);

					if (coinsGained > 0)
					{
						for (int coins = FightManager.SavedGameStatus.FightStartCoins + 1; coins <= FightManager.Coins; coins++)
						{
							yield return new WaitForSeconds(coinsUpPause);
							CoinsUp.text = string.Format("{0:N0}", coins);
							fightManager.CoinAudio();
						}
						CoinsUpFireworks.Play();
					}
					else
					{
						CoinsUp.text = string.Format("{0:N0}", FightManager.Coins);
						fightManager.CoinAudio();
					}
				}
			}

			inputAllowed = true;
			yield return null;
		}


		private IEnumerator DisplayWinnerStats()
		{
			yield return new WaitForSeconds(statsInterval);

			Stats.text = string.Format("\nMATCHES WON: {0}", winner.ProfileData.SavedData.MatchesWon);
			StatsAudio();
			yield return new WaitForSeconds(statsInterval);
			Stats.text += string.Format("\nMATCHES LOST: {0}", winner.ProfileData.SavedData.MatchesLost);
			StatsAudio();

			yield return new WaitForSeconds(statsInterval);

			Stats.text += string.Format("\nROUNDS WON: {0}", winner.ProfileData.SavedData.RoundsWon);
			StatsAudio();
			yield return new WaitForSeconds(statsInterval);
			Stats.text += string.Format("\nROUNDS LOST: {0}", winner.ProfileData.SavedData.RoundsLost);
			StatsAudio();

			yield return new WaitForSeconds(statsInterval);

			Stats.text += string.Format("\nDELIVERED HITS: {0}", winner.ProfileData.SavedData.DeliveredHits);
			StatsAudio();
			yield return new WaitForSeconds(statsInterval);
			Stats.text += string.Format("\nBLOCKED HITS: {0}", winner.ProfileData.SavedData.BlockedHits);
			StatsAudio();

			yield return new WaitForSeconds(statsInterval);

			Stats.text += string.Format("\nHITS TAKEN: {0}", winner.ProfileData.SavedData.HitsTaken);
			StatsAudio();
			yield return new WaitForSeconds(statsInterval);
			Stats.text += string.Format("\nHITS BLOCKED: {0}", winner.ProfileData.SavedData.HitsBlocked);
			StatsAudio();

			yield return new WaitForSeconds(statsInterval);

			Stats.text += string.Format("\nDAMAGE INFLICTED: {0}", (int) winner.ProfileData.SavedData.DamageInflicted);
			StatsAudio();
			yield return new WaitForSeconds(statsInterval);
			Stats.text += string.Format("\nDAMAGE SUSTAINED: {0}", (int) winner.ProfileData.SavedData.DamageSustained);
			StatsAudio();

			yield return new WaitForSeconds(statsInterval);

			Stats.text += string.Format("\nLEVEL: {0} -> {1}", winner.ProfileData.SavedData.FightStartLevel, winner.ProfileData.SavedData.Level);
			StatsAudio();

//			inputAllowed = true;
			yield return null;
		}

		private IEnumerator WinQuoteFadeIn()
		{
			WinQuote.color = Color.clear;
			WinQuote.gameObject.SetActive(true);

			float t = 0.0f;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / fadeInTime); 

				WinQuote.color = Color.Lerp(Color.clear, Color.white, t);
				yield return null;
			}
				
			yield return null;
		}

		private void Reset()
		{
			inputAllowed = false;

			Background.gameObject.SetActive(false);
			WinnerPhoto.gameObject.SetActive(false);
			WinnerName.gameObject.SetActive(false);
			WinQuote.gameObject.SetActive(false);

			WinnerStatsPanel.SetActive(false);
			KudosLabel.gameObject.SetActive(false);
			KudosUp.gameObject.SetActive(false);
			LevelUp.gameObject.SetActive(false);
			LevelLabel.gameObject.SetActive(false);
			CoinsUp.gameObject.SetActive(false);
			CoinsLabel.gameObject.SetActive(false);

			WorldTourPanel.gameObject.SetActive(false);
		}

		public void PhotoAudio()
		{
			if (photoAudio != null)
				AudioSource.PlayClipAtPoint(photoAudio, Vector3.zero, FightManager.SFXVolume);
			Background.gameObject.SetActive(true);
		}

		public void StatsAudio()
		{
			if (statsAudio != null)
				AudioSource.PlayClipAtPoint(statsAudio, Vector3.zero, FightManager.SFXVolume);
		}

		public void EntryComplete()		// animation event on last frame
		{
			StartCoroutine(WinQuoteFadeIn());

			if (Store.CanAfford(1) && FightManager.CombatMode == FightMode.Arcade && winner.UnderAI && !FightManager.SavedGameStatus.NinjaSchoolFight)		// player lost - countdown 'insert coin to continue'
				InsertCoinCountdown(ArcadeContinue, ArcadeExit);
			else if (! worldTourComplete)
				StartCoroutine(WinnerStats());

			if (worldTourComplete)
				WorldTourCongrats();
		}


		public IEnumerator ShowChallengeResults(List<ChallengeRoundResult> results)
		{
			challengeResults = results;

			ChallengePanel.SetActive(true);

			foreach (var result in challengeResults)
			{
				var winnerSprite = result.Winner.Portrait.sprite;
				var loserSprite = result.Loser.Portrait.sprite;

				Debug.Log("ShowChallengeResults: winnerSprite = " + winnerSprite + ", loserSprite = " + loserSprite + ", AIWinner = " + result.AIWinner);

				if (result.AIWinner)
				{
					Player2Button.SetFighterCard(winnerSprite, result.Winner);
					Player1Button.SetFighterCard(loserSprite, result.Loser);
				}
				else
				{
					Player1Button.SetFighterCard(winnerSprite, result.Winner);
					Player2Button.SetFighterCard(loserSprite, result.Loser);
				}

				if (result.AIWinner)
					resultsAnimator.SetTrigger("ChallengeP2");		// animate card entry from right
				else
					resultsAnimator.SetTrigger("ChallengeP1");		// animate card entry from left
				
				yield return new WaitForSeconds(1.0f);
			}
		}

		private void WorldTourCongrats()
		{
			WinnerStatsPanel.SetActive(false);
			fightManager.Success(0, "Curtain");		// top layer. AnimateWorldTourCongrats at end
		}

		private IEnumerator AnimateWorldTourCongrats()
		{
			WorldTourPanel.transform.localScale = Vector3.zero;
			WorldTourPanel.SetActive(true);
			CongratsText.text = FightManager.Translate("congratulations", false, true, true) + "!!";
			WorldTourText.text = FightManager.Translate("completedWorldTour", false, true);

			float t = 0.0f;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / worldTourCongratsTime); 

				WorldTourPanel.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
				yield return null;
			}

			WorldTourFireworks.Play();
			if (WorldTourSound != null)
				AudioSource.PlayClipAtPoint(WorldTourSound, Vector3.zero, FightManager.SFXVolume);

			inputAllowed = true;

			yield return null;
		}

		private void FeedbackStateEnd(AnimationState endingState)
		{
			if (endingState.StateLabel == FeedbackFXType.Success.ToString().ToUpper())
			{
				StartCoroutine(AnimateWorldTourCongrats());
			}
		}
	}
}
