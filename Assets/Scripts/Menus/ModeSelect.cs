﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Prototype.NetworkLobby;

namespace FightingLegends
{
	public class ModeSelect : MenuCanvas
	{
		public Button arcadeModeButton;
		public Button survivalModeButton;
		public Button challengeModeButton;
		public Button storeButton;
		public Button trainingButton;
		public Button networkFightButton;

		public Text arcadeModeLabel;
		public Text arcadeModeText;
		public Text survivalModeLabel;
		public Text survivalModeText;
		public Text challengeModeLabel;
		public Text challengeModeText;
		public Text storeLabel;
		public Text storeText;
		public Text trainingLabel;
		public Text trainingText;
		public Text networkFightLabel;
		public Text networkFightText;
		public Text noNetworkText;

		private FightManager fightManager;
//		private LobbyManager lobbyManager;

		public AudioClip entryAudio;
		public AudioClip entryCompleteAudio;

		private Animator animator;
		bool entryTriggered = false;

		private bool lanReachable = false;		// wi-fi needed for vs mode


		public void Awake()
		{
			var fightManagerObject = GameObject.Find("FightManager");
			fightManager = fightManagerObject.GetComponent<FightManager>();

			animator = GetComponent<Animator>();
		}

		private void OnEnable()
		{
			CheckLANAvailability();

//			var lobbyManagerObject = GameObject.Find("LobbyManager");
//			if (lobbyManagerObject != null)
//				lobbyManager = lobbyManagerObject.GetComponent<LobbyManager>();

			arcadeModeButton.onClick.AddListener(ArcadeMode);
			survivalModeButton.onClick.AddListener(SurvivalMode);
			challengeModeButton.onClick.AddListener(ChallengeMode);
			storeButton.onClick.AddListener(ShowDojo);
//			trainingButton.onClick.AddListener(Training);
			networkFightButton.onClick.AddListener(NetworkFight);

			animator.SetTrigger("EnterModes");
			entryTriggered = true;

			if (entryAudio != null)
				AudioSource.PlayClipAtPoint(entryAudio, Vector3.zero, FightManager.SFXVolume);
		}

		private void OnDisable()
		{
			arcadeModeButton.onClick.RemoveListener(ArcadeMode);
			survivalModeButton.onClick.RemoveListener(SurvivalMode);
			challengeModeButton.onClick.RemoveListener(ChallengeMode);
			storeButton.onClick.RemoveListener(ShowDojo);
//			trainingButton.onClick.RemoveListener(Training);
			networkFightButton.onClick.RemoveListener(NetworkFight);
		}

		public void Start()
		{
//			var fightManagerObject = GameObject.Find("FightManager");
//			fightManager = fightManagerObject.GetComponent<FightManager>();

			arcadeModeLabel.text = FightManager.Translate("arcadeMode");
			arcadeModeText.text = FightManager.Translate("arcadeTitle");
			survivalModeLabel.text = FightManager.Translate("survivalMode");
			survivalModeText.text = FightManager.Translate("survivalTitle");
			challengeModeLabel.text = FightManager.Translate("challengeMode");
			challengeModeText.text = FightManager.Translate("challengeTitle");
			storeLabel.text = FightManager.Translate("dojo");
			storeText.text = FightManager.Translate("dojoTitle");
//			trainingLabel.text = FightManager.Translate("training");
//			trainingLabel.text = FightManager.Translate("ninjaSchool");
//			trainingText.text = FightManager.Translate("trainingTitle");
			networkFightLabel.text = FightManager.Translate("vs");
			networkFightText.text = FightManager.Translate("networkTitle");
			noNetworkText.text = "[ " + FightManager.Translate("noNetwork") + " ]";

			FightManager.OnThemeChanged += SetTheme;
//
//			animator = GetComponent<Animator>();

//			if (!entryTriggered)
//			{
//				animator.SetTrigger("EnterModes");
//				entryTriggered = true;
//
//				if (entryAudio != null)
//					AudioSource.PlayClipAtPoint(entryAudio, Vector3.zero, FightManager.SFXVolume);
//			}
		}

		public void OnDestroy()
		{
			FightManager.OnThemeChanged -= SetTheme;
		}

		private void CheckLANAvailability()
		{
			lanReachable = (Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork);
			networkFightButton.interactable = lanReachable;
			noNetworkText.gameObject.SetActive(!lanReachable);
		}

		private void ArcadeMode()
		{
			FightManager.CombatMode = FightMode.Arcade;
			FightManager.SavedGameStatus.NinjaSchoolFight = false;
			fightManager.ModeSelectChoice = MenuType.ArcadeFighterSelect;		// triggers fade to black and new menu
		}

		private void SurvivalMode()
		{
			FightManager.CombatMode = FightMode.Survival;
			FightManager.SavedGameStatus.NinjaSchoolFight = false;
			fightManager.ModeSelectChoice = MenuType.SurvivalFighterSelect;	// triggers fade to black and new menu
		}

		private void ChallengeMode()
		{
			FightManager.CombatMode = FightMode.Challenge;
			FightManager.SavedGameStatus.NinjaSchoolFight = false;

			fightManager.PlayerCreatedChallenges = false;
			fightManager.ModeSelectChoice = MenuType.TeamSelect;		// triggers fade to black and new menu
		}


		private void ShowDojo()
		{
			fightManager.ModeSelectChoice = MenuType.Dojo;					// triggers fade to black and new menu
			FightManager.SavedGameStatus.NinjaSchoolFight = false;
		}

		// register a new user or open lobby (opening scene)
		private void NetworkFight()
		{
			if (string.IsNullOrEmpty(FightManager.SavedGameStatus.UserId))
			{
				FightManager.RegisterNewUser();
			}
			else
			{
//				FightManager.CombatMode = FightMode.Arcade;
				FightManager.IsNetworkFight = true;

//				if (lobbyManager != null)
//				{
//					SceneLoader.UnloadSceneAsync(SceneLoader.CombatScene);
//					lobbyManager.ShowLobbyUI();
//				}	
//				else
//				{
					SceneSettings.ShowLobbyUI = true;
					SceneLoader.LoadScene(SceneLoader.OpeningScene);			// straight to lobby
					Debug.Log("NetworkFight - Loaded OPENING scene");
//				}
			}
		}

		private void Training()
		{
			FightManager.CombatMode = FightMode.Training;
			FightManager.SavedGameStatus.NinjaSchoolFight = false;				// only after completed training

			fightManager.CleanupFighters();
			fightManager.SelectedLocation = FightManager.hawaii;
			fightManager.ModeSelectChoice = MenuType.Combat;				// triggers fade to black and new menu
		}

		public void EntryComplete()
		{
			if (entryCompleteAudio != null)
				AudioSource.PlayClipAtPoint(entryCompleteAudio, Vector3.zero, FightManager.SFXVolume);
		}
	}
}
