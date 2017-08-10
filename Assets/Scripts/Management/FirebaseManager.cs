
using System;
using UnityEngine;

using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Firebase;
using Firebase.Database;
using Firebase.Unity.Editor;

//using Facebook.MiniJSON;


namespace FightingLegends
{
	public class FirebaseManager: MonoBehaviour
	{
		private const string databaseUrl = "https://fighting-legends.firebaseio.com/";

		public const int LeaderboardMax = 100;				// max records stored / listed in each leaderboard		

		private static DatabaseReference databaseRoot;

		public delegate void UploadChallengeDelegate(ChallengeCategory category, ChallengeData challenge, bool success);
		public static UploadChallengeDelegate OnChallengeSaved;

		public delegate void GetChallengesDelegate(ChallengeCategory category, List<ChallengeData> challenges, bool success);
		public static GetChallengesDelegate OnChallengesDownloaded;

		public delegate void ChallengeAcceptedDelegate(TeamChallenge challenge, string challengerId, bool success);
		public static ChallengeAcceptedDelegate OnChallengeAccepted;

		public delegate void ChallengeCompletedDelegate(TeamChallenge challenge, bool success);
		public static ChallengeCompletedDelegate OnChallengeCompleted;

		public delegate void PostLeaderboardScoreDelegate(Leaderboard leaderboard, LeaderboardScore score, bool success);
		public static PostLeaderboardScoreDelegate OnPostScore;

		public delegate void GetLeaderboardDelegate(Leaderboard leaderboard, List<LeaderboardScore> scores, bool success);
		public static GetLeaderboardDelegate OnGetLeaderboard;

		public delegate void UploadUserProfileDelegate(string userId, UserProfile profile, bool success);
		public static UploadUserProfileDelegate OnUserProfileUploaded;

		public delegate void GetUserProfileDelegate(string userId, UserProfile profile, bool success);
		public static GetUserProfileDelegate OnGetUserProfile;

//		public delegate void UserChallengePotDelegate(string userId, ChallengePot challengePot, bool success);
//		public static UserChallengePotDelegate OnUserChallengePotUpdated;


		public void Start()
		{
			// set url before calling into the realtime database to allow Unity access to db
			FirebaseApp.DefaultInstance.SetEditorDatabaseUrl(databaseUrl);

			// get the root reference location of the database
			databaseRoot = FirebaseDatabase.DefaultInstance.RootReference;

//			DummyUsers();
//
//			GetUserProfile("");
//			GetUserProfile(null);
//			GetUserProfile("User99");
//			GetUserProfile("User1");

//			DummyScores();

//			List<LeaderboardScore> scores;
//			List<Challenge> challenges;
//
//			GetCategoryChallenges(ChallengeCategory.Gold, out challenges);
//			GetCategoryChallenges(ChallengeCategory.Silver, out challenges);
//
//			GetLeaderboardScores(Leaderboard.Kudos, out scores);
//			GetLeaderboardScores(Leaderboard.DojoDamage, out scores);

//			int score = 0;
//			GetLeaderboardScore(Leaderboard.Kudos, "Steve", out score);
//			GetLeaderboardScore(Leaderboard.DojoDamage, "Mark", out score);
		}

		private static Dictionary<string, object> ChallengeToDict(ChallengeData challenge)
		{
			return Facebook.MiniJSON.Json.Deserialize(ChallengeToJson(challenge)) as Dictionary<string, object>;	// simplified MiniJSON
		}

		private static string ChallengeToJson(ChallengeData challenge)
		{
			return JsonUtility.ToJson(challenge);
		}

//		private void DummyChallenges()
//		{
//			var challenge1 = CreateDummyChallenge(ChallengeCategory.Gold, "Steve's Challenge1", FightManager.china, 1000);
//			UploadChallenge(challenge1, ChallengeCategory.Gold);
//
//			var challenge2 = CreateDummyChallenge(ChallengeCategory.Silver, "Steve's Challenge2", FightManager.tokyo, 2000);
//			UploadChallenge(challenge2, ChallengeCategory.Silver);
//
//			var challenge3 = CreateDummyChallenge(ChallengeCategory.Gold, "Steve's Challenge3", FightManager.ghetto, 3000);
//			UploadChallenge(challenge3, ChallengeCategory.Gold);
//		}

//		private void DummyUsers()
//		{
//			UploadUserProfile(new UserProfile {
//				UserID = "User10",
//				DateAdded = DateTime.Now.ToShortDateString(),
//				TimeAdded = DateTime.Now.ToShortTimeString(),
////				CoinsToCollect = 1,
//			}
//			);
//
//			UploadUserProfile(new UserProfile {
//				UserID = "User20",
//				DateAdded = DateTime.Now.ToShortDateString(),
//				TimeAdded = DateTime.Now.ToShortTimeString(),
////				CoinsToCollect = 2,
//			}
//			);
//				
//			UploadUserProfile(new UserProfile {
//				UserID = "User30",
//				DateAdded = DateTime.Now.ToShortDateString(),
//				TimeAdded = DateTime.Now.ToShortTimeString(),
////				CoinsToCollect = 3,
//			}
//			);
//		}
			
		public static bool SaveChallenge(ChallengeData challenge, ChallengeCategory category, bool isNew)
		{
			if (string.IsNullOrEmpty(challenge.UserId))
				return false;
			
			var categoryNode = databaseRoot.Child("PlayerChallenges").Child("CategoryChallenges").Child(category.ToString());

			if (isNew)
				challenge.Key = categoryNode.Push().Key;		// new unique id

			// assign all team members to the new challenge key
			foreach (ChallengeTeamMember teamMember in challenge.Team)
			{
				teamMember.ParentChallenge = challenge.Key;
			}
				
			var challengeDict = ChallengeToDict(challenge);

			// atomic update of new challenge and the user's current (and only) challenge id
			Dictionary<string, object> childUpdates = new Dictionary<string, object>();
			childUpdates["/PlayerChallenges/CategoryChallenges/" + category.ToString() + "/" + challenge.Key] = challengeDict;
			childUpdates["/Users/" + challenge.UserId + "/ChallengeKey"] = challenge.Key;
					
			databaseRoot.UpdateChildrenAsync(childUpdates).ContinueWith(task => {

				if (task.IsCompleted)
				{
					if (OnChallengeSaved != null)
						OnChallengeSaved(category, challenge, true); 
				}
				else
				{
					Debug.Log("SaveChallenge: error = " + task.Exception.Message);
					if (OnChallengeSaved != null)
						OnChallengeSaved(category, challenge, false); 
				}
			});

			return true;
		}


		public static void CompleteChallenge(TeamChallenge challenge, bool defenderWon, int challengePot)
		{
			if (challenge == null)
				return;

			// update the profile of the user that posted the challenge
			string userId = challenge.UserId;

			if (string.IsNullOrEmpty(userId))		// really shouldn't happen
				return;

			Dictionary<string, object> childUpdates = new Dictionary<string, object>();
			childUpdates["/Users/" + userId + "/CoinsToCollect"] = defenderWon ? challengePot : 0;	
			childUpdates["/Users/" + userId + "/ChallengeKey"] =  "";
			childUpdates["/Users/" + userId + "/ChallengeResult"] = defenderWon ? "Won" : "Lost";

			databaseRoot.UpdateChildrenAsync(childUpdates).ContinueWith(task => {

				if (task.IsCompleted)
				{
					RemoveChallenge(challenge.ChallengeCategory.ToString(), challenge.ChallengeKey);

					if (OnChallengeCompleted != null)
						OnChallengeCompleted(challenge, true);	
				}
				else
				{
					Debug.Log("CompleteChallenge: error = " + task.Exception.Message);

					if (OnChallengeCompleted != null)
						OnChallengeCompleted(challenge, false);	
				}
			});
		}

		public static void RemoveChallenge(string category, string challengeKey)
		{
			var challengeNode = databaseRoot.Child("PlayerChallenges").Child("CategoryChallenges").Child(category).Child(challengeKey);

			challengeNode.RemoveValueAsync().ContinueWith(task => {

				if (task.IsCompleted)
				{
					Debug.Log("RemoveChallenge: success!" + challengeKey);
				}
				else
				{
					Debug.Log("RemoveChallenge: error = " + task.Exception.Message);
				}
			});
		}


		public static void AcceptChallenge(TeamChallenge challenge, int challengerTeamCoins)
		{			
			string category = challenge.ChallengeCategory.ToString();
			string challengeKey = challenge.ChallengeKey;

			var userId = FightManager.SavedGameStatus.UserId;
			if (string.IsNullOrEmpty(userId))
			{
				if (OnChallengeAccepted != null)
					OnChallengeAccepted(challenge, userId, false);

				return;
			}
				
			if (string.IsNullOrEmpty(challengeKey))
			{
				if (OnChallengeAccepted != null)
					OnChallengeAccepted(challenge, userId, false);

				return;
			} 
				
			var categoryNode = databaseRoot.Child("PlayerChallenges").Child("CategoryChallenges").Child(category.ToString());

			Dictionary<string, object> childUpdates = new Dictionary<string, object>();
			childUpdates["/" + challengeKey + "/ChallengerId"] = userId;							// effectively flags as 'in progress'
			childUpdates["/" + challengeKey + "/DateAccepted"] = DateTime.Now.ToString();
			childUpdates["/" + challengeKey + "/ChallengerTeamCoins"] = challengerTeamCoins;

			categoryNode.UpdateChildrenAsync(childUpdates).ContinueWith(task => {
				
				if (task.IsCompleted)
				{
					if (OnChallengeAccepted != null)
						OnChallengeAccepted(challenge, userId, true);
				}
				else
				{
					Debug.Log("AcceptChallenge: error = " + task.Exception.Message);
					if (OnChallengeAccepted != null)
						OnChallengeAccepted(challenge, userId, false);
				}
			});
		}


		public static void GetCategoryChallenges(ChallengeCategory category)
		{
			var categoryNode = databaseRoot.Child("PlayerChallenges").Child("CategoryChallenges").Child(category.ToString());
			var challengeList = new List<ChallengeData>();

			categoryNode.GetValueAsync().ContinueWith(task => {

				if (task.IsCompleted)
				{
					DataSnapshot snapshot = task.Result;
					var challengesDict = Facebook.MiniJSON.Json.Deserialize(snapshot.GetRawJsonValue()) as Dictionary<string, object>;	// simplified MiniJSON

					// populate challenges list with all the challenges in the deserialised dictionary - skipping any that are already in progress
					challengesDict.Values.ToList().ForEach(c => ExtractChallenge(category, c as Dictionary<string, object>, challengeList));

					if (OnChallengesDownloaded != null)
						OnChallengesDownloaded(category, challengeList, true);
//					foreach(var challenge in challenges)
//						PrintChallenge(challenge);
				}
				else
				{
					Debug.Log("GetCategoryChallenges: error = " + task.Exception.Message);
					if (OnChallengesDownloaded != null)
						OnChallengesDownloaded(category, challengeList, false);
				}
			});
		}

		private static void ExtractChallenge(ChallengeCategory category, Dictionary<string, object> challengeDict, List<ChallengeData> challenges)
		{
			if (challengeDict["ChallengerId"].ToString() != "")		// skip if already in progress
				return;

			var challenge = new ChallengeData {
				Key = challengeDict["Key"].ToString(),
				Name = challengeDict["Name"].ToString(),
				DateCreated = challengeDict["DateCreated"].ToString(),
				ExpiryDate = challengeDict["ExpiryDate"].ToString(),
				Location = challengeDict["Location"].ToString(),
				UserId = challengeDict["UserId"].ToString(),
				PrizeCoins = Convert.ToInt32(challengeDict["PrizeCoins"]),
				ParentCategory = category.ToString(), 

				Team = new List<ChallengeTeamMember>(),
			};
				
			var team = challengeDict["Team"] as List<object>;	// each member of team list is a dictionary
			team.ForEach(teamMember => ExtractTeam(teamMember as Dictionary<string, object>, challenge.Team));	

			challenges.Add(challenge);
		}
			
		private static void ExtractTeam(Dictionary<string, object> teamDict, List<ChallengeTeamMember> team)
		{
			var teamMember = new ChallengeTeamMember {
				FighterName = teamDict["FighterName"].ToString(),
				FighterColour = teamDict["FighterColour"].ToString(),
				Level = Convert.ToInt32(teamDict["Level"]),
				XP = Convert.ToInt32(teamDict["XP"]),
				Difficulty = teamDict["Difficulty"].ToString(),
				StaticPowerUp = teamDict["StaticPowerUp"].ToString(),
				TriggerPowerUp = teamDict["TriggerPowerUp"].ToString(),
			};
				
			team.Add(teamMember);
		}

		private static void PrintChallenge(ChallengeData challenge)
		{
			Debug.Log("PrintChallenge: " + challenge.Key + " / " + challenge.DateCreated + " / " + challenge.Location + " / " + challenge.ParentCategory + " / " + challenge.Team.Count + " in team");
			foreach (var teamMember in challenge.Team)
			{
				Debug.Log("Team: " + teamMember.FighterName + " / " + teamMember.Level + " / " + teamMember.Difficulty + " / " + teamMember.StaticPowerUp + " / " + teamMember.TriggerPowerUp);
			}
		}


//		private ChallengeData CreateDummyChallenge(ChallengeCategory category, string name, string location, int prizeCoins)
//		{
//			return new ChallengeData
//			{
//				Name = name,
//				ParentCategory = category.ToString(),
//				Location = location,
//				PrizeCoins = prizeCoins,
//
//				Team = new List<ChallengeTeamMember>
//				{
//					new ChallengeTeamMember
//					{
//						FighterName = "Leoni",
//						Level = 1,
//						XP = 25,
//						Difficulty = AIDifficulty.Medium.ToString(),
//						StaticPowerUp = PowerUp.Regenerator.ToString(),
//						TriggerPowerUp = PowerUp.SecondLife.ToString(),
//						ParentChallenge = name,
//					},
//
//					new ChallengeTeamMember
//					{
//						FighterName = "Shiro",
//						Level = 4,
//						XP = 75,
//						Difficulty = AIDifficulty.Easy.ToString(),
//						StaticPowerUp = PowerUp.Regenerator.ToString(),
//						TriggerPowerUp = PowerUp.HealthBooster.ToString(),
//						ParentChallenge = name,
//					},
//
//
//					new ChallengeTeamMember
//					{
//						FighterName = "Shiyang",
//						Level = 12,
//						XP = 10,
//						Difficulty = AIDifficulty.Hard.ToString(),
//						StaticPowerUp = PowerUp.ArmourPiercing.ToString(),
//						TriggerPowerUp = PowerUp.Ignite.ToString(),
//						ParentChallenge = name,
//					},
//				}
//			};
//		}


		public static void PostLeaderboardScore(Leaderboard leaderboard, float score) //, string userId = null)
		{
			if (string.IsNullOrEmpty(FightManager.SavedGameStatus.UserId))
				return;
			
			if (leaderboard == Leaderboard.None || score <= 0)
				return;
			
			string leaderboardName = leaderboard.ToString();
			string userId = FightManager.SavedGameStatus.UserId;

			score = Mathf.Round(score);		// float rounded to nearest int (as float)

			var leaderboardsNode = databaseRoot.Child("Leaderboards");
			var leaderboardNode = leaderboardsNode.Child(leaderboardName);
			var userNode = leaderboardNode.Child(userId);

			// get the whole leaderboard, to check user's existing score (if present) and to limit to LeaderboardMax entries (ie. delete lowest)
			var leaderboardQuery = leaderboardNode.OrderByValue();

			leaderboardQuery.GetValueAsync().ContinueWith(lookupTask => {

				if (lookupTask.IsCompleted)
				{
					DataSnapshot snapshot = lookupTask.Result;
					var scoreList = snapshot.Children;
					var scores = new List<LeaderboardScore>();

					bool userInLeaderboard = false;
					bool newPB = false;
					float lowestScore = 0;
					string lowestScoreUserId = "";
					bool userBeatsLowest = false;

					// keep scores in descending order (so index 0 is lowest)
					foreach (var scoreEntry in scoreList)
					{
						if (scoreEntry.Key == userId)
						{
							userInLeaderboard = true;

							if (score > Convert.ToSingle(scoreEntry.Value))		// single precision float
								newPB = true;				// only post score if user's score in leaderboard is lower than new score
						}
						
						scores.Add(new LeaderboardScore { UserId = scoreEntry.Key, Score = Convert.ToSingle(scoreEntry.Value) });
					}
						
					// if the leaderboard is not full, simply add the user's score

					// it the leaderboard is full, only add the user's score if it's higher than the lowest
					// in which case the lowest is deleted from the leaderboard

					bool leaderboardFull = scores.Count == LeaderboardMax;
	
					if (leaderboardFull && !newPB)			
					{
						lowestScore = scores[0].Score;
						lowestScoreUserId = scores[0].UserId;

						userBeatsLowest = score > lowestScore;
					}
						
					if (!leaderboardFull || newPB || (!userInLeaderboard && userBeatsLowest))
					{
						userNode.SetValueAsync(score).ContinueWith(postTask => {

							if (postTask.IsCompleted)
							{
								if (userBeatsLowest)		// only true if leaderboard full
								{
									leaderboardNode.Child(lowestScoreUserId).RemoveValueAsync();
								}

								if (OnPostScore != null)
									OnPostScore(leaderboard, new LeaderboardScore { UserId = userId, Score = score }, true); 
							}
							else
							{
								Debug.Log("PostLeaderboardScore: post error = " + postTask.Exception.Message);
								if (OnPostScore != null)
									OnPostScore(leaderboard, new LeaderboardScore { UserId = userId, Score = score }, false); 
							}
						});
					}
				}
				else
				{
					if (OnPostScore != null)
						OnPostScore(leaderboard, new LeaderboardScore { UserId = userId, Score = score }, false); 
				}
			});
		}

		public static void GetLeaderboardScores(Leaderboard leaderboard, int maxScores = LeaderboardMax)
		{
			var leaderboardNode = databaseRoot.Child("Leaderboards").Child(leaderboard.ToString()).OrderByValue(); //.LimitToFirst(maxScores);
			var scores = new List<LeaderboardScore>();

			leaderboardNode.GetValueAsync().ContinueWith(task => {
				
				if (task.IsCompleted)
				{
					DataSnapshot snapshot = task.Result;
					var scoreList = snapshot.Children;

					// add each entry to the top of the list to reverse the sort order to descending
					foreach (var score in scoreList)
					{
//						Debug.Log("GetLeaderboardScores: " + score.Key + " / " + Convert.ToInt32(score.Value));
						scores.Insert(0, new LeaderboardScore { UserId = score.Key, Score = Convert.ToInt32(score.Value) });
					}

					if (OnGetLeaderboard != null)
						OnGetLeaderboard(leaderboard, scores, true);
//					foreach(var score in scores)
//						PrintScore(leaderboard, score);
				}
				else
				{
					Debug.Log("GetLeaderboardScores: error = " + task.Exception.Message);

					if (OnGetLeaderboard != null)
						OnGetLeaderboard(leaderboard, scores, false);		// empty list
				}
			});
		}

	
		private static void PrintScore(Leaderboard leaderboard, LeaderboardScore score)
		{
			Debug.Log("PrintScore " + leaderboard.ToString() + ": " + score.UserId + " / " + score.Score);
		}



		public static void SaveUserProfile(UserProfile profile)
		{
			var usersNode = databaseRoot.Child("Users");
			var userJson = JsonUtility.ToJson(profile);

//			Debug.Log("UploadUserProfile: userId " + profile.UserID + " / " + Convert.ToDateTime(profile.TimeAdded));

			usersNode.Child(profile.UserID).SetRawJsonValueAsync(userJson).ContinueWith(task => {

				if (task.IsCompleted)
				{
					if (OnUserProfileUploaded != null)
						OnUserProfileUploaded(profile.UserID, profile, true); 

					GetUserProfile(profile.UserID);
				}
				else
				{
					Debug.Log("SaveUserProfile: error = " + task.Exception.Message);
					if (OnUserProfileUploaded != null)
						OnUserProfileUploaded(profile.UserID, profile, false); 
				}
			});
		}
			

		public static void GetUserProfile(string userId)
		{
			if (string.IsNullOrEmpty(userId))
			{
				if (OnGetUserProfile != null)
					OnGetUserProfile(userId, null, true);
				
//				PrintUser(null);
				return;
			}
				
			var userNode = databaseRoot.Child("Users").Child(userId);

			userNode.GetValueAsync().ContinueWith(task => {

				if (task.IsCompleted)
				{
					if (task.Result == null)		// user id does not exist
					{
						if (OnGetUserProfile != null)
							OnGetUserProfile(userId, null, true);
//						PrintUser(null);
					}
					else
					{
						DataSnapshot snapshot = task.Result;
						var userProfile = JsonUtility.FromJson<UserProfile>(snapshot.GetRawJsonValue());

						if (OnGetUserProfile != null)
							OnGetUserProfile(userId, userProfile, true);

//						PrintUser(userProfile);
					}
				}
				else
				{
					Debug.Log("GetUserProfile: error = " + task.Exception.Message);
					if (OnGetUserProfile != null)
						OnGetUserProfile(userId, null, false);
				}
			});
		}

//		private static void PrintUser(UserProfile profile)
//		{
//			if (profile == null)
//				Debug.Log("PrintUser: NOT FOUND");
//			else
//				Debug.Log("PrintUser ID " + profile.UserID + ": " + profile.TimeAdded + " COINS: " + profile.CoinsToCollect);
//		}
	}


	// challenge content + team
	// stored in firebase
	[Serializable]
	public class ChallengeData
	{
		public string Key = "";			// set on save (push), used to update

		public string Name = "";		// for future use

		public string DateCreated = "";		
		public string ExpiryDate = "";		

		public string Location = "";

		public List<ChallengeTeamMember> Team = null;
		public int PrizeCoins = 0;		// winnings

		public string ParentCategory;

		public string UserId = "";		// uploaded by

		public string ChallengerId = "";		// challenge accepted by
		public int ChallengerTeamCoins = 0;	
		public string DateAccepted = "";		
	}

	// fighter belonging to a challenge team
	[Serializable]
	public class ChallengeTeamMember
	{
		public string FighterName = "";
		public string FighterColour = "";
		public int Level = 1;
		public int XP = 0;
		public string StaticPowerUp = PowerUp.None.ToString();
		public string TriggerPowerUp = PowerUp.None.ToString();
		public string Difficulty = AIDifficulty.Easy.ToString();

		public string ParentChallenge;				// name
	}
		
	// leaderboard entry
	[Serializable]
	public class LeaderboardScore
	{
		public string UserId = "";
		public float Score = 0;
	}
		
	[Serializable]
	public class UserProfile
	{
		public string UserID = "";
		public string DateCreated = DateTime.Now.ToShortTimeString();

		public string ChallengeKey = "";	// current challenge (only one at a time allowed) - cleared when challenge completed
		public string ChallengeResult = "";	// true if last challenge posted won by AI team (defender)

		public int CoinsToCollect = 0;		// set if challenge won by user that posted it

//		public Sprite Picture;	
	}
}
