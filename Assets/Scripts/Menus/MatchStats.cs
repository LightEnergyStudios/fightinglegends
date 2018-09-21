using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace FightingLegends
{
	public class MatchStats : MenuCanvas
	{
		public Image Background;				// black semi-transparent
		public Image WinnerNamePanel;			// pink panel
		public Text WinnerName;					// winner's name
		public Image WinnerPhoto;				// place holder for winner sprite
	
		public Text WinQuote;
		public Text Stats;						// damage, hits, etc... (not currently used)
		public Text ChallengeStats;		
		public Text ChallengeScore;		
		public Text ThanksForPlaying;		

		public GameObject WinnerStatsPanel;
		public Text LevelLabel;
		public Text VsVictoriesLabel;
		public Image KudosLabel;
		public Image CoinsLabel;
		public Text LevelUp;
		public Text VsVictoriesUpDown;
		public Text KudosUp;
		public Text CoinsUp;
		public ParticleSystem LevelUpFireworks;
		public ParticleSystem VsVictoriesFireworks;
		public ParticleSystem KudosUpFireworks;
		public ParticleSystem CoinsUpFireworks;

		public ParticleSystem Stars;

		public float kudosUpPause;
		public float levelUpPause;
		public float vsVictoriesPause;
		public float coinsUpPause;
		public int kudosBlingInterval;			// bling sound

		public InsertContinueCoin ContinueCoin;

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
		public ParticleSystem ChallengeFireworks;

		public FighterButton Player1Button;					// challenge results
		public FighterButton Player2Button;					// challenge results
		public ParticleSystem Player1Fireworks;				// challenge results
		public ParticleSystem Player2Fireworks;				// challenge results
		public ParticleSystem Player1Stars;					// challenge results
		public ParticleSystem Player2Stars;					// challenge results

		public AudioClip ChallengeResultStart;				// fighter card entry start
		public AudioClip ChallengeResultEnd;				// fighter card entry end
		public AudioClip ChallengeFlipStart;				// fighter card flipped and replaced
		public AudioClip ChallengeFlipEnd;					// fighter card flipped and replaced

//		private float challengeResultsPause = 2.5f;			// pause between results of each round (FighterCards)
		private float pulseFlipPause = 0.25f;				// pause before flipping loser / pulsing winner after card entry
		private float pulseFighterTime = 0.25f;
		private Vector3 pulseFighterScale = new Vector3(2.5f, 2.5f, 1);

		private Vector3 defaultPlayerScale;					// default at start
		private Vector3 player1Position;					// default at start
		private Vector3 player2Position;					// default at start

		private bool player1WonChallengeRound = false;
		private int challengeResultRound = 0;				// challenge result currently being shown

//		public GameObject InsertCoin;	
		private const float continuePause = 1.5f;			// before countdown starts

		public GameObject InsertCoinTextPanel;	
		public List<Text> InsertCoinText;					// animated text x3
		public Image InsertCoinStrip;
		private const int insertCoinTextWidth = 600;
		private const float insertCoinTextTime = 3.0f;		// 
		private const int insertCoinTextRepeats = 3;
		private IEnumerator insertCoinTextCoroutine = null;
		private List<float> insertCoinTextPosition;			// original x position of animated text x3

		public GameObject WorldTourPanel;
		public Text CongratsText;
		public Text WorldTourText;
		public ParticleSystem WorldTourFireworks;
		public AudioClip WorldTourSound;

		private const float worldTourCongratsTime = 0.25f;

		private List<ChallengeRoundResult> challengeResults;

		private bool worldTourComplete = false;
		private bool worldTourCongratsShowing = false;


		public override bool CanNavigateBack { get { return false; } }


		public void Awake()
		{
			resultsAnimator = GetComponent<Animator>();

			var fightManagerObject = GameObject.Find("FightManager");
			fightManager = fightManagerObject.GetComponent<FightManager>();
		}

		// initialization
		public void Start()
		{
			inputAllowed = false;

			LevelLabel.text = FightManager.Translate("level", false, false, true);
			VsVictoriesLabel.text = FightManager.Translate("victories", false, false, true);
			ThanksForPlaying.text = FightManager.Translate("thanksForPlaying", false, false, true);

			ThanksForPlaying.gameObject.SetActive(false);			// only once beaten Skeletron (Arcade)

			// record default values
			defaultPlayerScale = Player1Button.transform.localScale;
			player1Position = Player1Button.transform.localPosition;
			player2Position = Player2Button.transform.localPosition;
		}

		private void OnEnable()
		{
//			feedbackUI.feedbackFX.OnEndState += FeedbackStateEnd;
			WorldTourPanel.GetComponent<WorldTourPanel>().OnWorldTourCongratsEnd += WorldTourCongratsEnd;

			SaveInsertCoinTextPositions();
			InsertCoinTextPanel.SetActive(false);
			InsertCoinStrip.gameObject.SetActive(false);
			ContinueCoin.gameObject.SetActive(false);

			worldTourCongratsShowing = false;

			Player1Button.gameObject.SetActive(false);
			Player2Button.gameObject.SetActive(false);
		}

		private void OnDisable()
		{
//			feedbackUI.feedbackFX.OnEndState -= FeedbackStateEnd;

			WorldTourPanel.GetComponent<WorldTourPanel>().OnWorldTourCongratsEnd -= WorldTourCongratsEnd;

			RestoreInsertCoinTextPositions();
			InsertCoinTextPanel.SetActive(false);
			InsertCoinStrip.gameObject.SetActive(false);
			ContinueCoin.gameObject.SetActive(false);

			// restore fight card (button) values
			Player1Button.gameObject.SetActive(false);
			Player2Button.gameObject.SetActive(false);
			Player1Button.transform.localScale = defaultPlayerScale;
			Player2Button.transform.localScale = defaultPlayerScale;
			Player1Button.transform.localPosition = player1Position;
			Player2Button.transform.localPosition = player2Position;
			Player1Stars.Stop();
			Player2Stars.Stop();
		}

		private void Update() 
		{
			if (inputAllowed && ((Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began) || Input.GetMouseButtonDown(0)))	// left button
			{
				if (FightManager.IsNetworkFight)
				{
					fightManager.MatchStatsChoice = MenuType.ModeSelect;			// exits match stats
				}
				else if (FightManager.CombatMode == FightMode.Arcade && !FightManager.SavedGameStatus.NinjaSchoolFight)
				{
					if (winner != null)
					{
						if (!winner.UnderAI)							// player won - choose next location
						{
//							Debug.Log("MatchStats: worldTourCongratsShowing = " + worldTourCongratsShowing);
							if (worldTourCongratsShowing)
							{
								worldTourCongratsShowing = false;
								fightManager.MatchStatsChoice = MenuType.ModeSelect;		// exits match stats

								fightManager.ResetWorldTour();
								fightManager.SaveGameStatus();
							}
							else
								fightManager.MatchStatsChoice = MenuType.WorldMap;			// exits match stats
						}
						// else AI won - wait for countdown or insert coin to continue
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
			ContinueCoin.continueButton.gameObject.SetActive(false);
			ContinueCoin.gameObject.SetActive(true);
			ContinueCoin.Countdown(actionOnContinue, actionOnExit, continuePause, message);

			inputAllowed = true;		// tap to exit
		}

		private void ArcadeContinue()
		{
//			Debug.Log("ArcadeContinue");
			ContinueCoin.gameObject.SetActive(false);
			fightManager.MatchStatsChoice = MenuType.Combat;			// to exit match stats (same menu canvas, so does nothing else)
			fightManager.MatchStatsRestartMatch = true;					// same fighters and location
		}

		private void ArcadeExit()
		{
//			Debug.Log("ArcadeExit");
			ContinueCoin.gameObject.SetActive(false);
			StopInsertCoinAnimation();

			fightManager.ResetWorldTour();
			fightManager.CleanupFighters();
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

			if (worldTourComplete)
			{
				WorldTourCongrats(victor);				// animation
				return;
			}
				
			InitStats();
			EnableWinnerStats(true);

			WinnerName.text = winner.FighterName.ToUpper() + " " + FightManager.Translate("wins");

			switch (winner.FighterName)
			{
				case "Shiro":
					WinnerPhoto.sprite = shiroWin;
					break;

				case "Natalya":
					WinnerPhoto.sprite = natalyaWin;
					break;

				case "Hoi Lun":
					WinnerPhoto.sprite = hoiLunWin;
					break;

				case "Leoni":
					WinnerPhoto.sprite = leoniWin;
					break;

				case "Danjuma":
					WinnerPhoto.sprite = danjumaWin;
					break;

				case "Jackson":
					WinnerPhoto.sprite = jacksonWin;
					break;

				case "Alazne":
					WinnerPhoto.sprite = alazneWin;
					break;

				case "Shiyang":
					WinnerPhoto.sprite = shiyangWin;
					break;

				case "Ninja":
					WinnerPhoto.sprite = ninjaWin;
					break;

				case "Skeletron":
					WinnerPhoto.sprite = skeletronWin;
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
//			WinQuote.gameObject.SetActive(true);

			Stats.text = "";

			// P1 and P2 are reversed... (not sure why)
			if (winner.IsPlayer1)
				resultsAnimator.SetTrigger("Player2Winner");		// animate portrait entry from left
			else
				resultsAnimator.SetTrigger("Player1Winner");		// animate portrait entry from right
		}

		private void InitStats()
		{
			EnableWinnerStats(false);
			EnableChallengeStats(false);

			WinQuote.gameObject.SetActive(false);			// fades in after animation

			KudosLabel.gameObject.SetActive(false);
			KudosUp.gameObject.SetActive(false);
			LevelLabel.gameObject.SetActive(false);
			LevelUp.gameObject.SetActive(false);
			CoinsLabel.gameObject.SetActive(false);
			CoinsUp.gameObject.SetActive(false);
			VsVictoriesLabel.gameObject.SetActive(false);
			VsVictoriesUpDown.gameObject.SetActive(false);

			WorldTourPanel.SetActive(false);
		}

		private void EnableWinnerStats(bool enable)
		{
			WinnerStatsPanel.SetActive(enable);
			WinnerNamePanel.gameObject.SetActive(enable);
			WinnerPhoto.gameObject.SetActive(enable);

			EnableChallengeStats(false);
		}

		private void EnableChallengeStats(bool enable)
		{
			ChallengeStats.gameObject.SetActive(enable);
			ChallengeScore.gameObject.SetActive(enable);
		}

		private IEnumerator ShowWinnerStats()
		{
			EnableWinnerStats(true);

			var player = winner.UnderAI ? winner.Opponent : winner;
			int kudosGained = (int)(FightManager.Kudos - FightManager.SavedGameStatus.FightStartKudos);
			int levelGained = player.Level - player.ProfileData.SavedData.FightStartLevel;
			int coinsGained = FightManager.Coins - FightManager.SavedGameStatus.FightStartCoins;

			KudosLabel.gameObject.SetActive(true);
			KudosUp.gameObject.SetActive(true);
			KudosUp.text = string.Format("{0:N0}", FightManager.SavedGameStatus.FightStartKudos);

			yield return StartCoroutine(IncreaseKudos(kudosGained));

			if (FightManager.CombatMode == FightMode.Arcade)
			{
				LevelLabel.gameObject.SetActive(false);
				LevelUp.gameObject.SetActive(false);
				CoinsLabel.gameObject.SetActive(false);
				CoinsUp.gameObject.SetActive(false);
				ChallengeStats.gameObject.SetActive(false);
				ChallengeScore.gameObject.SetActive(false);
			}
			else if (FightManager.CombatMode == FightMode.Survival)
			{
				yield return new WaitForSeconds(levelUpPause);
				LevelLabel.gameObject.SetActive(true);
				LevelUp.gameObject.SetActive(true);
				LevelUp.text = player.ProfileData.SavedData.FightStartLevel.ToString();

				yield return StartCoroutine(IncreaseLevel(player, levelGained));

				yield return new WaitForSeconds(coinsUpPause);
				CoinsLabel.gameObject.SetActive(true);
				CoinsUp.gameObject.SetActive(true);
				CoinsUp.text = string.Format("{0:N0}", FightManager.SavedGameStatus.FightStartCoins);

				yield return StartCoroutine(IncreaseCoins(coinsGained));
			}

			if (FightManager.IsNetworkFight)
			{
				bool newVictory = winner.IsPlayer1;

				VsVictoriesLabel.gameObject.SetActive(true);
				VsVictoriesUpDown.gameObject.SetActive(true);
				VsVictoriesUpDown.text = newVictory ? (FightManager.SavedGameStatus.VSVictoryPoints - 1).ToString() 	// incremented
													: (FightManager.SavedGameStatus.VSVictoryPoints + 1).ToString(); 	// decremented

				yield return new WaitForSeconds(levelUpPause);

				VsVictoriesUpDown.text = FightManager.SavedGameStatus.VSVictoryPoints.ToString();
				fightManager.BlingAudio();

				if (newVictory)						// victories incremented
					VsVictoriesFireworks.Play();

				FightManager.IsNetworkFight = false;
			}

			inputAllowed = true;
			yield return null;
		}

		private IEnumerator IncreaseKudos(int kudosGained)
		{
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

			yield return null;
		}

		private IEnumerator IncreaseCoins(int coinsGained)
		{
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

			yield return null;
		}

		private IEnumerator IncreaseLevel(Fighter player, int levelGained)
		{
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
		}

//		private IEnumerator DisplayWinnerStats()
//		{
//			yield return new WaitForSeconds(statsInterval);
//
//			Stats.text = string.Format("\nMATCHES WON: {0}", winner.ProfileData.SavedData.MatchesWon);
//			StatsAudio();
//			yield return new WaitForSeconds(statsInterval);
//			Stats.text += string.Format("\nMATCHES LOST: {0}", winner.ProfileData.SavedData.MatchesLost);
//			StatsAudio();
//
//			yield return new WaitForSeconds(statsInterval);
//
//			Stats.text += string.Format("\nROUNDS WON: {0}", winner.ProfileData.SavedData.RoundsWon);
//			StatsAudio();
//			yield return new WaitForSeconds(statsInterval);
//			Stats.text += string.Format("\nROUNDS LOST: {0}", winner.ProfileData.SavedData.RoundsLost);
//			StatsAudio();
//
//			yield return new WaitForSeconds(statsInterval);
//
//			Stats.text += string.Format("\nDELIVERED HITS: {0}", winner.ProfileData.SavedData.DeliveredHits);
//			StatsAudio();
//			yield return new WaitForSeconds(statsInterval);
//			Stats.text += string.Format("\nBLOCKED HITS: {0}", winner.ProfileData.SavedData.BlockedHits);
//			StatsAudio();
//
//			yield return new WaitForSeconds(statsInterval);
//
//			Stats.text += string.Format("\nHITS TAKEN: {0}", winner.ProfileData.SavedData.HitsTaken);
//			StatsAudio();
//			yield return new WaitForSeconds(statsInterval);
//			Stats.text += string.Format("\nHITS BLOCKED: {0}", winner.ProfileData.SavedData.HitsBlocked);
//			StatsAudio();
//
//			yield return new WaitForSeconds(statsInterval);
//
//			Stats.text += string.Format("\nDAMAGE INFLICTED: {0}", (int) winner.ProfileData.SavedData.DamageInflicted);
//			StatsAudio();
//			yield return new WaitForSeconds(statsInterval);
//			Stats.text += string.Format("\nDAMAGE SUSTAINED: {0}", (int) winner.ProfileData.SavedData.DamageSustained);
//			StatsAudio();
//
//			yield return new WaitForSeconds(statsInterval);
//
//			Stats.text += string.Format("\nLEVEL: {0} -> {1}", winner.ProfileData.SavedData.FightStartLevel, winner.ProfileData.SavedData.Level);
//			StatsAudio();
//
////			inputAllowed = true;
//			yield return null;
//		}

		private IEnumerator WinQuoteFadeIn()
		{
			if (FightManager.CombatMode == FightMode.Challenge)
				yield break;
			
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

//		private void Reset()
//		{
//			inputAllowed = false;
//
//			Background.gameObject.SetActive(false);
//			WinnerPhoto.gameObject.SetActive(false);
//			WinnerName.gameObject.SetActive(false);
//			WinnerNamePanel.gameObject.SetActive(false);
//			WinQuote.gameObject.SetActive(false);
//			WinnerStatsPanel.SetActive(false);
//			ChallengeStats.gameObject.SetActive(false);
//			ChallengeScore.gameObject.SetActive(false);
//
////			EnableWinnerStats(false);
////			EnableChallengeStats(false);
//
//			KudosLabel.gameObject.SetActive(false);
//			KudosUp.gameObject.SetActive(false);
//			LevelUp.gameObject.SetActive(false);
//			LevelLabel.gameObject.SetActive(false);
//			CoinsUp.gameObject.SetActive(false);
//			CoinsLabel.gameObject.SetActive(false);
//			VsVictoriesLabel.gameObject.SetActive(false);
//			VsVictoriesUpDown.gameObject.SetActive(false);
//
//			InsertCoinTextPanel.SetActive(false);
//			InsertCoinStrip.gameObject.SetActive(false);
//
////			WorldTourPanel.gameObject.SetActive(false);		// TODO: reinstate?
//
//			Stars.Stop();
//		}

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

		public void EntryComplete()		// animation event on last frame of player1 / player2 winner entry
		{
			StartCoroutine(WinQuoteFadeIn());		// not challenge mode

			if (winner != null && winner.UnderAI && Store.CanAfford(1) &&
						FightManager.CombatMode == FightMode.Arcade && !worldTourComplete && !FightManager.SavedGameStatus.NinjaSchoolFight)		// player lost to AI - countdown 'insert coin to continue'
			{
				InsertCoinCountdown(ArcadeContinue, ArcadeExit);
				StartCoroutine(CycleInsertCoinText());
			}
			else
				StartCoroutine(ShowWinnerStats());
		}

		#region challenge results

		private void EnableChallengeResults()
		{
			WinnerStatsPanel.SetActive(false);

			Player1Button.gameObject.SetActive(true);
			Player2Button.gameObject.SetActive(true);

			WinnerName.gameObject.SetActive(false);
			WinnerNamePanel.gameObject.SetActive(false);

			KudosUp.gameObject.SetActive(false);
			KudosLabel.gameObject.SetActive(false);
			CoinsUp.gameObject.SetActive(false);
			CoinsLabel.gameObject.SetActive(false);

			WinnerPhoto.gameObject.SetActive(false);
			WinQuote.gameObject.SetActive(false);

			LevelUp.gameObject.SetActive(false);
			LevelLabel.gameObject.SetActive(false);

			VsVictoriesLabel.gameObject.SetActive(false);
			VsVictoriesUpDown.gameObject.SetActive(false);

			Background.gameObject.SetActive(true);
		}

		public void FirstChallengeResult(List<ChallengeRoundResult> results)
		{
			challengeResults = results;
			challengeResultRound = 0;
			inputAllowed = false;

			EnableChallengeResults();
			NextChallengeResult();		// ie. first
		}

		private bool NextChallengeResult()
		{
			if (challengeResults == null || challengeResults.Count == 0)
				return false;

			if (challengeResultRound >= challengeResults.Count)
				return false;
			
			ChallengeRoundResult result = challengeResults[ challengeResultRound ];
			var winnerSprite = result.Winner.Portrait.sprite;
			var loserSprite = result.Loser.Portrait.sprite;

//			Debug.Log("NextChallengeResult: round " + challengeResultRound + ": winner " + result.Winner.FighterName + ", loser " + result.Loser.FighterName);

			bool prevRoundAIWinner = ! player1WonChallengeRound;
			player1WonChallengeRound = !result.AIWinner; 	// needed to trigger flip animation

			if (challengeResultRound == 0)			// first result
			{
				if (player1WonChallengeRound)
				{
					Player1Button.SetFighterCard(winnerSprite, result.Winner);
					Player2Button.SetFighterCard(loserSprite, result.Loser);
				}
				else
				{
					Player1Button.SetFighterCard(loserSprite, result.Loser);
					Player2Button.SetFighterCard(winnerSprite, result.Winner);
				}

				resultsAnimator.SetTrigger("ChallengeP1P2");				// both enter together
			}
			else
			{
				if (player1WonChallengeRound)
				{
					if (prevRoundAIWinner)		// P1 lost last round, this round won by P1, so replace P1 with winner
					{
						Player1Button.SetFighterCard(winnerSprite, result.Winner);
						resultsAnimator.SetTrigger("ChallengeP1");					// animate fighter card entry from left
					}
					else 						// P2 lost last round, this round won by P1, so replace P2 with loser
					{
						Player2Button.SetFighterCard(loserSprite, result.Loser);
						resultsAnimator.SetTrigger("ChallengeP2");					// animate fighter card entry from right
					}
				}
				else 
				{
					if (prevRoundAIWinner)		// P1 lost last round, this round won by P2, so replace P1 with loser
					{
						Player1Button.SetFighterCard(loserSprite, result.Loser);
						resultsAnimator.SetTrigger("ChallengeP1");					// animate fighter card entry from left
					}
					else 						// P2 lost last round, this round won by P2, so replace P2 with winner
					{
						Player2Button.SetFighterCard(winnerSprite, result.Winner);
						resultsAnimator.SetTrigger("ChallengeP2");					// animate fighter card entry from right
					}
				}
			}
			return true;
		}

		// animation event - last frame of result entry
		public void ChallengeResultEntry()
		{
			if (ChallengeResultEnd != null)
				AudioSource.PlayClipAtPoint(ChallengeResultEnd, Vector3.zero, FightManager.SFXVolume);

			//			ChallengeFireworks.Play();

			StartCoroutine(PulseWinner());
			StartCoroutine(FlipLoser());
		}


		private void ChallengeResultStats()
		{
			EnableChallengeStats();

			int p1Score = 0;
			int p2Score = 0;

			GetChallengeScore(out p1Score, out p2Score);
			ChallengeScore.text = p1Score + " - " + p2Score; 

			// animate stats panel
			if (player1WonChallengeRound)		// last round of challenge
			{
				resultsAnimator.SetTrigger("Player1Stats");		// animate stats panel entry from left
				WinnerName.text = FightManager.Translate("challengeWon", false, true); 
				ChallengeStats.text = FightManager.Translate("congratulations", false, true); 
			}
			else
			{
				resultsAnimator.SetTrigger("Player2Stats");		// animate stats panel entry from right
				WinnerName.text = FightManager.Translate("challengeLost", false, true); 
				ChallengeStats.text = FightManager.Translate("betterLuckNextTime", false, true); 
			}

//			inputAllowed = true;
		}
			
		// animation event - last frame of challenge stats entry
		public void ChallengeStatsEntry()
		{
			StartCoroutine(UpdateChallengeStats());			// kudos, coins, etc.

			if (ChallengeResultEnd != null)
				AudioSource.PlayClipAtPoint(ChallengeResultEnd, Vector3.zero, FightManager.SFXVolume);
		}

		private void EnableChallengeStats()
		{
			WinnerStatsPanel.SetActive(true);
			ChallengeStats.gameObject.SetActive(false);			// fade in below
			ChallengeScore.gameObject.SetActive(true);

			Player1Button.gameObject.SetActive(player1WonChallengeRound);
			Player2Button.gameObject.SetActive(! player1WonChallengeRound);

			WinnerName.gameObject.SetActive(true);
			WinnerNamePanel.gameObject.SetActive(true);

			KudosUp.gameObject.SetActive(true);
			KudosLabel.gameObject.SetActive(true);
			CoinsUp.gameObject.SetActive(player1WonChallengeRound);
			CoinsLabel.gameObject.SetActive(player1WonChallengeRound);

			WinnerPhoto.gameObject.SetActive(false);
			WinQuote.gameObject.SetActive(false);

			LevelUp.gameObject.SetActive(false);
			LevelLabel.gameObject.SetActive(false);

			VsVictoriesLabel.gameObject.SetActive(false);
			VsVictoriesUpDown.gameObject.SetActive(false);

			var stars = player1WonChallengeRound ? Player1Stars : Player2Stars;
			stars.Play();
		}

		private void GetChallengeScore(out int player1, out int player2)
		{
			player1 = 0;
			player2 = 0;

			if (challengeResults == null)
				return;

			foreach (var result in challengeResults)
			{
				if (result.AIWinner)
					player2++;
				else
					player1++;
			}
		}

		private IEnumerator UpdateChallengeStats()
		{
			yield return StartCoroutine(ChallengeStatsFadeIn());

			int kudosGained = (int)(FightManager.Kudos - FightManager.SavedGameStatus.FightStartKudos);
			KudosUp.text = string.Format("{0:N0}", FightManager.SavedGameStatus.FightStartKudos);

			yield return StartCoroutine(IncreaseKudos(kudosGained));

			if (player1WonChallengeRound)
			{
				int coinsGained = FightManager.Coins - FightManager.SavedGameStatus.FightStartCoins;
			
				yield return new WaitForSeconds(coinsUpPause);
				CoinsLabel.gameObject.SetActive(true);
				CoinsUp.gameObject.SetActive(true);
				CoinsUp.text = string.Format("{0:N0}", FightManager.SavedGameStatus.FightStartCoins);

				yield return StartCoroutine(IncreaseCoins(coinsGained));
			}

			StatsAudio();

			inputAllowed = true;
			yield return null;
		}

		private IEnumerator ChallengeStatsFadeIn()
		{
			ChallengeStats.color = Color.clear;
			ChallengeStats.gameObject.SetActive(true);
//			ChallengeScore.color = Color.clear;
//			ChallengeScore.gameObject.SetActive(true);

			float t = 0.0f;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / fadeInTime); 

				ChallengeStats.color = Color.Lerp(Color.clear, Color.white, t);
//				ChallengeScore.color = Color.Lerp(Color.clear, Color.white, t);
				yield return null;
			}

			yield return null;
		}

		private IEnumerator FlipLoser()
		{
			yield return new WaitForSeconds(pulseFlipPause);

			if (player1WonChallengeRound)
			{
				resultsAnimator.SetTrigger("FlipPlayer2");
				Player2Fireworks.Play();
			}
			else
			{
				resultsAnimator.SetTrigger("FlipPlayer1");
				Player1Fireworks.Play();
			}

			FlipAudio();
		}
			
		public void Player1Flipped()
		{
//			Player1Fireworks.Play();

			if (ChallengeFlipEnd != null)
				AudioSource.PlayClipAtPoint(ChallengeFlipEnd, Vector3.zero, FightManager.SFXVolume);

			challengeResultRound++;
			if (! NextChallengeResult())		// no more results
				ChallengeResultStats();
		}

		public void Player2Flipped()
		{
//			Player2Fireworks.Play();

			if (ChallengeFlipEnd != null)
				AudioSource.PlayClipAtPoint(ChallengeFlipEnd, Vector3.zero, FightManager.SFXVolume);

			challengeResultRound++;
			if (! NextChallengeResult())		// no more results
				ChallengeResultStats();
		}

		public void FlipAudio()
		{
			if (ChallengeFlipStart != null)
				AudioSource.PlayClipAtPoint(ChallengeFlipStart, Vector3.zero, FightManager.SFXVolume);
		}

			
		private IEnumerator PulseWinner()
		{
			yield return new WaitForSeconds(pulseFlipPause);

			var player = player1WonChallengeRound ? Player1Button : Player2Button;
			var stars = player1WonChallengeRound ? Player1Stars : Player2Stars;
			Vector3 startScale = player.transform.localScale;
			float t = 0.0f;

			stars.Play();

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / pulseFighterTime); 

				player.transform.localScale = Vector3.Lerp(startScale, pulseFighterScale, t);
				yield return null;
			}
				
			yield return new WaitForSeconds(pulseFighterTime / 2.0f);
			t = 0.0f;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / pulseFighterTime); 

				player.transform.localScale = Vector3.Lerp(pulseFighterScale, startScale, t);
				yield return null;
			}

			stars.Stop();
			yield return null;
		}

		#endregion  // challenge results


		private void WorldTourCongrats(Fighter winner)
		{
			inputAllowed = false;		
			EnableWinnerStats(false);
			EnableChallengeStats(false);
			worldTourCongratsShowing = true;
			WorldTourPanel.SetActive(true);

			WorldTourPanel.GetComponent<Animator>().SetTrigger("WorldTourComplete");
			WorldTourFireworks.Play();

			// move loser (ie. Skellie out of the way)
			var loser = winner.Opponent; 
			var loserPosition = loser.transform.localPosition;
			loser.transform.localPosition = new Vector3(loserPosition.x + loser.ProfileData.ExpiryDistance, loserPosition.y, loserPosition.z);

			fightManager.DestroyCurrentScenery();	// keep music playing
		}

		private void WorldTourCongratsEnd()
		{
			ThanksForPlaying.gameObject.SetActive(true);
			RevealWinner(winner, false);
		}


		private IEnumerator CycleInsertCoinText()
		{
			StopInsertCoinAnimation();

			yield return new WaitForSeconds(continuePause);

			InsertCoinTextPanel.SetActive(true);
			InsertCoinStrip.gameObject.SetActive(true);

			insertCoinTextCoroutine = LoopInsertCoinText();
			StartCoroutine(insertCoinTextCoroutine);

			yield return null;
		}

		protected IEnumerator LoopInsertCoinText()
		{
			float xReturnPoint = InsertCoinText[0].transform.localPosition.x - insertCoinTextWidth;

			while (true)			// loop until coroutine stopped externally
			{
				foreach (var coinText in InsertCoinText)
				{
					StartCoroutine(AnimateCoinText(coinText, xReturnPoint));
				}

				yield return new WaitForSeconds(insertCoinTextTime);

				RestoreInsertCoinTextPositions(); 		// need accurate float values
			}
		}

		protected IEnumerator AnimateCoinText(Text coinText, float xReturnPoint)
		{
			float t = 0;

			var startPosition = coinText.transform.localPosition;
			var targetPosition = new Vector3(startPosition.x - insertCoinTextWidth, startPosition.y, startPosition.z);

			bool returnAtTarget = targetPosition.x <= xReturnPoint;

//			coinText.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, Time.deltaTime * insertCoinTextTime);

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / insertCoinTextTime); 

				coinText.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
				yield return null;
			}

			// return back to start position if beyond return point
			var currentPosition = coinText.transform.localPosition;

			if (returnAtTarget)
				coinText.transform.localPosition = new Vector3(currentPosition.x + (insertCoinTextWidth * insertCoinTextRepeats), currentPosition.y, currentPosition.z);
		}

		private void SaveInsertCoinTextPositions()
		{
			insertCoinTextPosition = new List<float>();

			for (int i = 0; i < insertCoinTextRepeats; i++)
			{
				insertCoinTextPosition.Add(InsertCoinText[i].transform.localPosition.x);
			}
		}

		private void RestoreInsertCoinTextPositions()
		{
			for (int i = 0; i < insertCoinTextRepeats; i++)
			{
				var position = InsertCoinText[i].transform.localPosition;
				InsertCoinText[i].transform.localPosition = new Vector3(insertCoinTextPosition[i], position.y, position.z);
			}
		}

		private void StopInsertCoinAnimation()
		{
			InsertCoinTextPanel.SetActive(false);
			InsertCoinStrip.gameObject.SetActive(false);
			if (insertCoinTextCoroutine != null)
				StopCoroutine(insertCoinTextCoroutine);
		}

//		private void FeedbackStateEnd(AnimationState endingState)
//		{
//			if (endingState.StateLabel == FeedbackFXType.Success.ToString().ToUpper())
//			{
//				StartCoroutine(AnimateWorldTourCongrats());
////				inputAllowed = true;
//			}
//		}
	}
}
