using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Facebook.Unity;
using Facebook.MiniJSON;

namespace FightingLegends
{
	public class FacebookManager : MenuCanvas
	{
		private const string googlePlayStoreLink = "https://play.google.com/store/apps/details?id=com.burningheartsoftware.fightinglegends";
	//	private const string iTunesAppStoreLink = "https://itunes.apple.com/au/app/fightinglegends/id412265690?mt=8";
		private const string iTunesAppStoreLink = "https://itunes.apple.com/us/app/fighting-legends/id1205820391?ls=1&mt=8";

		private const string facebookPageLink = "https://www.facebook.com/FightingLegendsGame";
		private const string facebookAppLink = "https://www.facebook.com/games/FightingLegendsGame";

		private bool IsInitialized = false;
		private bool LoggingIn = false;
		public bool IsLoggedIn { get { return FB.IsLoggedIn; } }

		public Text UserName;
		public Image ProfilePic;
		public Text Kudos;
		public Button postKudosButton;			// not used
		public Button getScoresButton;			// refresh
		public Button inviteFriendsButton;
		public Button shareFriendsButton;

		public Text titleLabel;				// top friends
		public Text getScoresLabel;			// refresh
		public Text inviteFriendsLabel;
		public Text shareFriendsLabel;

		public GameObject ScoresContent;		// content of ScrollRect
		public GameObject ScoreEntryPrefab;		// panel to be instantiated in ScoresContent

		public Text Loading;
		private bool gettingScores = false;
		private bool GettingScores
		{
			get { return gettingScores; }
			set
			{
				gettingScores = value;
				Loading.gameObject.SetActive(gettingScores);
			}
		}

		public Sprite diamondSprite;			
		public Sprite goldSprite;
		public Sprite silverSprite;
		public Sprite bronzeSprite;
		public Sprite ironSprite;

		public Sprite DefaultUserPic;	
		public Sprite DefaultProfilePic;		// friends

		public Text ErrorMessage;


		[HideInInspector]
		public static bool FacebookOk = true;

		private List<FriendScore> scoresList = new List<FriendScore>();

		public delegate void LoginSuccessDelegate();
		public LoginSuccessDelegate OnLoginSuccess;

		public delegate void LoginFailDelegate(string error);
		public LoginFailDelegate OnLoginFail;

		public delegate void FBUserNameDelegate(string userName);
		public static FBUserNameDelegate OnLookupUserName;

		public delegate void FBProfilePicDelegate(Sprite profilePic);
		public static FBProfilePicDelegate OnLookupProfilePic;


		private void Start()
		{
			FightManager.OnThemeChanged += SetTheme;

			titleLabel.text = FightManager.Translate("topFriends");
			getScoresLabel.text = FightManager.Translate("refresh");
			inviteFriendsLabel.text = FightManager.Translate("invite");
			shareFriendsLabel.text = FightManager.Translate("share");
		}

		private void OnDestroy()
		{
			FightManager.OnThemeChanged -= SetTheme;
		}

//		private void Awake()
//		{
//			try
//			{
//				FB.Init(InitCallback, OnHideUnity);
//			}
//			catch (Exception ex)
//			{
//				FacebookOk = false;
//				Debug.Log("FB.Init failed: " + ex.Message);
//			}
//		}

		private void Init()
		{
			try
			{
				ErrorMessage.text = "Init";
				FB.Init(InitCallback, OnHideUnity);
			}
			catch (Exception ex)
			{
				FacebookOk = false;
				ErrorMessage.text = "FB.Init failed: " + ex.Message;
				Debug.Log("FB.Init failed: " + ex.Message);
			}
		}

//		public void Init(bool login)
//		{
//			if (IsInitialized)
//				return;
//
//			LoggingIn = login;
//			
//			try
//			{
//				FB.Init(InitCallback, OnHideUnity);
//			}
//			catch (Exception ex)
//			{
//				FacebookOk = false;
//				Debug.Log("FB.Init failed: " + ex.Message);
//			}
//		}

		private void InitCallback()
		{
			// TODO: if android or iOS
			FB.ActivateApp();

			IsInitialized = true;
			ErrorMessage.text = "InitCallback ok";

			if (LoggingIn)
				LoginWithPermissions();
		}


		public void Login()
		{
			if (IsLoggedIn)
			{
				GetUserData();		// refresh scores etc
				return;
			}

			LoggingIn = true;

			if (IsInitialized)
				LoginWithPermissions();
			else
				Init();		// will login on init callback
		}

		private void LoginWithPermissions()
		{
			if (!IsInitialized)
				return;

			ErrorMessage.text = "LoginWithPermissions";
			
			List<string> permissions = new List<string>();
			//		permissions.Add("public_profile,user_friends");
			permissions.Add("public_profile,user_friends,publish_actions");

			FB.LogInWithPublishPermissions(permissions, LoginCallback);
		}


		private void LoginCallback(ILoginResult result)
		{
			if (result.Error != null)
			{
				if (OnLoginFail != null)
					OnLoginFail(result.Error);

				ErrorMessage.text = "FB Login failed: " + result.Error + " AccessToken = " + result.AccessToken;
				Debug.Log("LoginCallback error: " + result.Error + " AccessToken = " + result.AccessToken);
			}
			else
			{
				ErrorMessage.text = "LoginCallback ok";

				if (IsLoggedIn)
				{
					Debug.Log("LoginCallback ok: FB is logged in");
					GetUserData();
				}
				else
					Debug.Log("LoginCallback ok: FB is not logged in");

				if (OnLoginSuccess != null)
					OnLoginSuccess();
			}

			LoggingIn = false;
		}

//		public void Error(string errorMsg)
//		{
//			ErrorMessage.text = errorMsg;
//		}

		private void OnEnable()
		{
//			postKudosButton.onClick.AddListener(PostKudos);
			getScoresButton.onClick.AddListener(RefreshScores);
			inviteFriendsButton.onClick.AddListener(InviteFriends);
			shareFriendsButton.onClick.AddListener(ShareWithFriends);

			SetKudos();
		}

		private void OnDisable()
		{
//			postKudosButton.onClick.RemoveListener(PostKudos);
			getScoresButton.onClick.RemoveListener(RefreshScores);
			inviteFriendsButton.onClick.RemoveListener(InviteFriends);
			shareFriendsButton.onClick.RemoveListener(ShareWithFriends);

			DestroyScoreEntries();
		}


		private void GetUserData()
		{
			PostKudos();			// RefreshScores in callback
		}
			
		private void GetUserName()
		{
			FB.API("/me?fields=first_name", HttpMethod.GET, GetUserNameCallback);
		}

		private void GetUserNameCallback(IGraphResult result)
		{
			if (result.Error == null)
			{
				UserName.text = result.ResultDictionary["first_name"].ToString(); //.ToUpper();
//				Debug.Log("GetUserNameCallback: " + UserName.text);

				if (OnLookupUserName != null)
					OnLookupUserName(UserName.text);
			}
		}


		private void GetProfilePic()
		{
			FB.API("/me/picture?type=square&height=128&width=128", HttpMethod.GET, GetProfilePicCallback);
		}

		private void GetProfilePicCallback(IGraphResult result)
		{
			if (result.Texture != null)
			{
				ProfilePic.sprite = Sprite.Create(result.Texture, new Rect(0, 0, 128, 128), new Vector2());

				if (OnLookupProfilePic != null)
					OnLookupProfilePic(ProfilePic.sprite);
			}
			else
				ProfilePic.sprite = DefaultProfilePic;
		}
			
		private void RefreshScores()
		{
//			Debug.Log("GetScores: GettingScores = " + GettingScores);

			if (GettingScores)
				return;
			
			GettingScores = true;
			FB.API("/app/scores?fields=score,user.limit(50)", HttpMethod.GET, GetScoresCallback);
		}


		private void GetScoresCallback(IGraphResult result)
		{
			scoresList.Clear();

			var scoreDict = Json.Deserialize(result.RawResult) as Dictionary<string, object>;
			var scoreData = (List<object>)scoreDict["data"];

			foreach (var score in scoreData)
			{
				var scoreItem = score as Dictionary<string, object>;
				var scoreUser = scoreItem["user"] as Dictionary<string, object>;

				var friendScore = new FriendScore {
					Score = int.Parse(scoreItem["score"].ToString()),
					Name = scoreUser["name"].ToString(),
					Id = scoreUser["id"].ToString(),
				};

				scoresList.Add(friendScore);
			}

			FillScoreEntries();
			GettingScores = false;
		}
			

		private void FillScoreEntries()
		{
			// clean out previous score entries before repopulating
			DestroyScoreEntries();

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
				
				var scoreAvatar = scoreEntryPanel.transform.Find("Avatar").GetComponent<Image>();
				var scoreName = scoreEntryPanel.transform.Find("Name").GetComponent<Text>();
				var scoreValue = scoreEntryPanel.transform.Find("Score").GetComponent<Text>();
				var rankValue = scoreEntryPanel.transform.Find("Rank").GetComponent<Text>();

				// just show first name on score entry panel
				scoreName.text = score.Name.Split(' ')[0]; // .ToUpper();  	// score.Name;
				scoreValue.text = string.Format("{0:N0}", score.Score);		// comma separated
				rankValue.text = "#" + rank.ToString();
			
				// TODO: remove!
//				scoreAvatar.sprite = DefaultProfilePic;

				string query = "/" + score.Id.ToString() + "/picture?type=square&height=128&width=128";
				FB.API(query, HttpMethod.GET, delegate(IGraphResult pictureResult)
				{
					if (pictureResult.Error != null)
						scoreAvatar.sprite = DefaultProfilePic;
					else
						scoreAvatar.sprite = Sprite.Create(pictureResult.Texture, new Rect(0, 0, 128, 128), new Vector2());
				});

				rank++;
			}
		}


		private void DestroyScoreEntries()
		{
			foreach (Transform scoreEntry in ScoresContent.transform)
			{
				Destroy(scoreEntry.gameObject);
			}
		}

		private void InviteFriend(string friendId)
		{
			FB.AppRequest(
//				to: friendId,
				message: "Try to beat my Fighting Legends score!",
				title: "Invite friend"
			);
		}

		private void InviteFriends()
		{
			FB.AppRequest(
				message: "Try to beat my Fighting Legends score!",
				title: "Challenge your friends"
			);
		}

		private void ShareWithFriends()
		{
			FB.FeedShare(
				linkCaption: "I'm playing this game!",
				linkName: "Check it out!",
				link: new Uri(facebookPageLink),
				callback: ShareCallback
			);
		}

		private void ShareCallback(IShareResult result)
		{
			Debug.Log("ShareCallback: result = " + result.RawResult);
		}

		private void SetKudos()
		{
			Kudos.text = string.Format("{0:N0}", FightManager.Kudos);		// comma separated
		}

		private void PostKudos()
		{
			var scoreData = new Dictionary<string,string>();
			scoreData["score"] = string.Format("{0:F0}", FightManager.Kudos); // no decimal places
			FB.API("/me/scores", HttpMethod.POST, PostKudosCallback, scoreData);
		}

		private void PostKudosCallback(IGraphResult result)
		{
			Debug.Log("PostKudosCallback: kudos = " + result.RawResult);
			RefreshScores();
		}


		//	Unity will call OnApplicationPause(false) for a fresh launch and when resumed from background
		public void OnApplicationPause(bool pauseStatus)
		{
			// check pauseStatus to see if we are in foreground or background
			if (! pauseStatus)
			{
				// app resume
				if (FB.IsInitialized)
				{
//					FB.ActivateApp();		// for analytics - android and iOS only
				}
				else
				{
					Init();
				}
			}
		}

		// pause game if not showing
		private void OnHideUnity(bool isGameShowing)
		{
			// TODO: is this required?
//			Time.timeScale = isGameShowing ? 1 : 0;
		}
	}

	[Serializable]
	public class FriendScore
	{
		public string Name = "";
		public string Id = "";
		public float Score = 0;
	}
}

