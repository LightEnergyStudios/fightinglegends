using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace FightingLegends
{
	public class LeaderboardManager : MenuCanvas
	{
		public Text UserId;

		public Button kudosLeaderboardButton;	
		public Button damageLeaderboardButton;	
		public Button survivalLeaderboardButton;	
		public Button challengeLeaderboardButton;	
		public Button worldTourLeaderboardButton;	

		public Button refreshButton;		
//		public Button postTestButton;		
		public Button newUserButton;		

		public Text titleLabel;		
		public Text refreshLabel;		
		public Text newUserLabel;		
		public Text currentLeaderboardTitle;	
		public Text kudosLeaderboardLabel;	
		public Text damageLeaderboardLabel;	
		public Text survivalLeaderboardLabel;	
		public Text challengeLeaderboardLabel;	
		public Text worldTourLeaderboardLabel;	

		public GameObject ScoresContent;		// content of ScrollRect
		public GameObject ScoreEntryPrefab;		// panel to be instantiated in OnGetLeaderboard

		public Text LoadingScores;

		private bool gettingScores = false;
		private bool GettingScores
		{
			get { return gettingScores; }
			set
			{
				gettingScores = value;
				LoadingScores.gameObject.SetActive(gettingScores);

				refreshButton.interactable = !gettingScores;	
				kudosLeaderboardButton.interactable = !gettingScores;
				damageLeaderboardButton.interactable = !gettingScores;	
				survivalLeaderboardButton.interactable = !gettingScores;	
				challengeLeaderboardButton.interactable = !gettingScores;	
				worldTourLeaderboardButton.interactable = !gettingScores;	
//				postTestButton.interactable = !gettingScores;	
			}
		}

		public Sprite diamondSprite;			
		public Sprite goldSprite;
		public Sprite silverSprite;
		public Sprite bronzeSprite;
		public Sprite ironSprite;

		public Text ErrorMessage;

		private Leaderboard currentLeaderboard = Leaderboard.None;

	
		private void Start()
		{
			FightManager.OnThemeChanged += SetTheme;

			titleLabel.text = FightManager.Translate("leaderBoards");
			refreshLabel.text = FightManager.Translate("refresh");
			newUserLabel.text = FightManager.Translate("postScores", true);
			kudosLeaderboardLabel.text = FightManager.Translate("kudos");
			damageLeaderboardLabel.text = FightManager.Translate("dojoDamage", true);
			survivalLeaderboardLabel.text = FightManager.Translate("survivalEndurance", true);
			challengeLeaderboardLabel.text = FightManager.Translate("challengeWinnings", true);
			worldTourLeaderboardLabel.text = FightManager.Translate("worldTours", true);
			LoadingScores.text = FightManager.Translate("loadingScores") + " ...";

//			UserId.text = FightManager.SavedGameStatus.UserId;
		}

		private void OnDestroy()
		{
			FightManager.OnThemeChanged -= SetTheme;
		}
			

		private void OnEnable()
		{
			refreshButton.onClick.AddListener(RefreshScores);
			kudosLeaderboardButton.onClick.AddListener(delegate { GetLeaderboard(Leaderboard.Kudos); });
			damageLeaderboardButton.onClick.AddListener(delegate { GetLeaderboard(Leaderboard.DojoDamage); });
			survivalLeaderboardButton.onClick.AddListener(delegate { GetLeaderboard(Leaderboard.SurvivalRounds); });
			challengeLeaderboardButton.onClick.AddListener(delegate { GetLeaderboard(Leaderboard.ChallengeWinnings); });
			worldTourLeaderboardButton.onClick.AddListener(delegate { GetLeaderboard(Leaderboard.WorldTours); });

//			postTestButton.onClick.AddListener(delegate { PostTest(); });

			UserId.text = FightManager.SavedGameStatus.UserId;

			bool userRegistered = !string.IsNullOrEmpty(FightManager.SavedGameStatus.UserId);
			newUserButton.gameObject.SetActive(!userRegistered);

			if (!userRegistered)
			{
				newUserButton.onClick.AddListener(delegate { RegisterNewUser(); });
				FirebaseManager.OnUserProfileSaved += OnNewUserRegistered;
			}

			FirebaseManager.OnPostScore += OnPostScore;
			FirebaseManager.OnGetLeaderboard += OnGetLeaderboard;

			SetCurrentLeaderboard(Leaderboard.None);
			GetLeaderboard(Leaderboard.Kudos);

			// hide if already registered!
			newUserButton.gameObject.SetActive(string.IsNullOrEmpty(FightManager.SavedGameStatus.UserId));
		}

		private void OnDisable()
		{
			refreshButton.onClick.RemoveListener(RefreshScores);
			kudosLeaderboardButton.onClick.RemoveListener(delegate { GetLeaderboard(Leaderboard.Kudos); });
			damageLeaderboardButton.onClick.RemoveListener(delegate { GetLeaderboard(Leaderboard.DojoDamage); });
			survivalLeaderboardButton.onClick.RemoveListener(delegate { GetLeaderboard(Leaderboard.SurvivalRounds); });
			challengeLeaderboardButton.onClick.RemoveListener(delegate { GetLeaderboard(Leaderboard.ChallengeWinnings); });
			worldTourLeaderboardButton.onClick.RemoveListener(delegate { GetLeaderboard(Leaderboard.WorldTours); });

//			newUserButton.onClick.RemoveListener(delegate { RegisterNewUser(); });
//			postTestButton.onClick.RemoveListener(delegate { PostTest(); });

			bool userRegistered = !string.IsNullOrEmpty(FightManager.SavedGameStatus.UserId);

			if (!userRegistered)
			{
				newUserButton.onClick.RemoveListener(delegate { RegisterNewUser(); });
				FirebaseManager.OnUserProfileSaved -= OnNewUserRegistered;
			}

			FirebaseManager.OnPostScore -= OnPostScore;
			FirebaseManager.OnGetLeaderboard -= OnGetLeaderboard;
//			FirebaseManager.OnUserProfileUploaded -= OnNewUserRegistered;

			DestroyScoreEntries();
		}
			
//		private void PostTest()
//		{
//			if (currentLeaderboard != Leaderboard.None)
//			{
//				string userId = currentLeaderboard.ToString() + "Champ" + UnityEngine.Random.Range(1, 99);
//				int score = 0;
//
//				switch (currentLeaderboard)
//				{
//					case Leaderboard.Kudos:
//						score = UnityEngine.Random.Range(1, 100000);
//						break;
//
//					case Leaderboard.SurvivalRounds:
//						score = UnityEngine.Random.Range(1, 500);
//						break;
//
//					case Leaderboard.ChallengeWinnings:
//						score = UnityEngine.Random.Range(1, 50000);
//						break;
//
//					case Leaderboard.DojoDamage:
//						score = UnityEngine.Random.Range(1, 1000);
//						break;
//
//					default:
//						score = UnityEngine.Random.Range(1, 100);
//						break;
//				}
//
////				Debug.Log("PostTest: " + ", userId = " + userId + ", score = " + score);
//
//				FirebaseManager.PostLeaderboardScore(currentLeaderboard, score, userId);
//				postTestButton.interactable = false;		
//			}
//		}

		private void OnPostScore(Leaderboard leaderboard, LeaderboardScore score, bool success)
		{
			if (success && leaderboard == currentLeaderboard)
				RefreshScores();

//			postTestButton.interactable = true;		
			Debug.Log("OnPostScore: " + ", userId = " + score.UserId + ", score = " + score.Score + ", success = " + success);
		}

		private void RefreshScores()
		{
//			Debug.Log("RefreshScores: GettingScores = " + GettingScores);

			if (GettingScores)
				return;

			if (currentLeaderboard != Leaderboard.None)
				GetLeaderboard(currentLeaderboard);
		}

		private void RegisterNewUser()
		{
			if (string.IsNullOrEmpty(FightManager.SavedGameStatus.UserId))
				FightManager.RegisterNewUser();
		}

		private void OnNewUserRegistered(string userId, UserProfile profile, bool success)
		{
			if (!string.IsNullOrEmpty(FightManager.SavedGameStatus.UserId) && userId == FightManager.SavedGameStatus.UserId)
			{
				if (success)
				{
					newUserButton.gameObject.SetActive(false);
					newUserButton.onClick.RemoveListener(delegate { RegisterNewUser(); });
					FirebaseManager.OnUserProfileSaved -= OnNewUserRegistered;

					FirebaseManager.PostLeaderboardScore(Leaderboard.Kudos, FightManager.SavedGameStatus.Kudos);
					FirebaseManager.PostLeaderboardScore(Leaderboard.SurvivalRounds, FightManager.SavedGameStatus.BestSurvivalEndurance);
					FirebaseManager.PostLeaderboardScore(Leaderboard.ChallengeWinnings, FightManager.SavedGameStatus.TotalChallengeWinnings);
					FirebaseManager.PostLeaderboardScore(Leaderboard.DojoDamage, FightManager.SavedGameStatus.BestDojoDamage);
				}
			}
		}

		private void GetLeaderboard(Leaderboard leaderboard)
		{
			if (GettingScores || leaderboard == Leaderboard.None)
				return;

			GettingScores = true;
			FirebaseManager.GetLeaderboardScores(leaderboard);
		}


		private void OnGetLeaderboard(Leaderboard leaderboard, List<LeaderboardScore> scoresList, bool success)
		{
			GettingScores = false;

			if (!success)
			{
				ErrorMessage.text = FightManager.Translate("unableToRetrieve") + " " + FightManager.Translate("leaderBoard")
											+ " [" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + "]";
				return;
			}
				
			SetCurrentLeaderboard(leaderboard);
				
			// clean out previous score entries before repopulating
			DestroyScoreEntries();

//			Debug.Log("OnGetLeaderboard: scoresList = " + scoresList.Count);

			int rank = 1;

			foreach (var score in scoresList)		// scores are provided in descending order
			{
				var scoreEntryPanel = Instantiate(ScoreEntryPrefab, ScoresContent.transform) as GameObject;
				scoreEntryPanel.transform.localScale = Vector3.one;						// somehow corrupted by instantiate! - crappy
				scoreEntryPanel.transform.localPosition = Vector3.zero;					// to make sure z is zero!! ...

				var background = scoreEntryPanel.GetComponent<Image>();

				if (rank == 1)
					background.sprite = diamondSprite;
				else if (rank == 2)
					background.sprite = goldSprite;
				else if (rank == 3)
					background.sprite = silverSprite;
				else if (rank == 4)
					background.sprite = bronzeSprite;
				else
					background.sprite = ironSprite;
				
				var scoreName = scoreEntryPanel.transform.Find("Name").GetComponent<Text>();
				var scoreValue = scoreEntryPanel.transform.Find("Score").GetComponent<Text>();
				var rankValue = scoreEntryPanel.transform.Find("Rank").GetComponent<Text>();

				// just show first name on score entry panel
				if (!string.IsNullOrEmpty(score.UserId))
					scoreName.text = score.UserId.Split(' ')[0]; // .ToUpper();
				else
					scoreName.text = rank.ToString();

				scoreValue.text = string.Format("{0:N0}", score.Score);		// comma separated
				rankValue.text = "#" + rank.ToString();
			
				rank++;
			}
		}

		private void SetCurrentLeaderboard(Leaderboard leaderboard)
		{
			currentLeaderboard = leaderboard;

			switch (currentLeaderboard)
			{
				default:
				case Leaderboard.None:
					currentLeaderboardTitle.text = FightManager.Translate("noLeaderboard");
					return;

				case Leaderboard.Kudos:
					currentLeaderboardTitle.text = FightManager.Translate("topKudos") + " " + FirebaseManager.LeaderboardMax;
					break;

				case Leaderboard.DojoDamage:
					currentLeaderboardTitle.text = FightManager.Translate("topDojoDamage") + " " + FirebaseManager.LeaderboardMax;
					break;

				case Leaderboard.SurvivalRounds:
					currentLeaderboardTitle.text = FightManager.Translate("topSurvivalEndurance") + " " + FirebaseManager.LeaderboardMax;
					break;

				case Leaderboard.ChallengeWinnings:
					currentLeaderboardTitle.text = FightManager.Translate("topChallengeWinnings") + " " + FirebaseManager.LeaderboardMax;
					break;

				case Leaderboard.WorldTours:
					currentLeaderboardTitle.text = FightManager.Translate("topWorldTours") + " " + FirebaseManager.LeaderboardMax;
					break;
			}

			kudosLeaderboardButton.interactable = currentLeaderboard != Leaderboard.Kudos;
			damageLeaderboardButton.interactable = currentLeaderboard != Leaderboard.DojoDamage;	
			survivalLeaderboardButton.interactable = currentLeaderboard != Leaderboard.SurvivalRounds;	
			challengeLeaderboardButton.interactable = currentLeaderboard != Leaderboard.ChallengeWinnings;	
			worldTourLeaderboardButton.interactable = currentLeaderboard != Leaderboard.WorldTours;	
		}


		private void DestroyScoreEntries()
		{
			foreach (Transform scoreEntry in ScoresContent.transform)
			{
				Destroy(scoreEntry.gameObject);
			}
		}
	}
}

