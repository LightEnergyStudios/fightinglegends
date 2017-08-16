using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace FightingLegends
{
	public class UserRegistration : MonoBehaviour
	{
		public Image panel;
		public Color panelColour;	
		private Vector3 panelScale;

		private Image background;
		public Color backgroundColour;	

		public Button registerButton;
		public Text registerLabel;
		public Button cancelButton;
		public Text cancelLabel;

		public InputField userIdInput;
		public Text placeHolderText;

		public Text feedbackMessage;

		public float fadeTime;

		private const int MaxUserIdLength = 25;			// display limitation


		void Awake()
		{
			background = GetComponent<Image>();
			background.enabled = false;
			background.color = backgroundColour;

			panel.gameObject.SetActive(false);
			panel.color = panelColour;
			panelScale = panel.transform.localScale;

			registerLabel.text = FightManager.Translate("register");
			cancelLabel.text = FightManager.Translate("cancel");
		}


		private void OnEnable()
		{
			registerButton.onClick.AddListener(RegisterClicked);
			cancelButton.onClick.AddListener(CancelClicked);

			FirebaseManager.OnGetUserProfile += OnGetUser;					// to check if nickname exists
			FirebaseManager.OnUserProfileSaved += OnUserUploaded;	

			DefaultPrompt();
		}

		private void OnDisable()
		{
			registerButton.onClick.RemoveListener(RegisterClicked);
			cancelButton.onClick.RemoveListener(CancelClicked);

			FirebaseManager.OnGetUserProfile -= OnGetUser;
			FirebaseManager.OnUserProfileSaved -= OnUserUploaded;
		}
			

		private void DefaultPrompt()
		{
			feedbackMessage.text = "";
			placeHolderText.text = FightManager.Translate("enterNickname");

			userIdInput.text = "";
			AllowInput(true);
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
				
			yield return null;
		}

		public IEnumerator Hide()
		{
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

			panel.transform.localScale = Vector3.one;

			yield return null;
		}

		// public interface
		public void PromptForNewUserId()
		{
			DefaultPrompt();
			StartCoroutine(Show());
		}
			

		private void RegisterNewUser()
		{
			FirebaseManager.SaveUserProfile(new UserProfile {
				UserID = userIdInput.text,
				DateCreated = DateTime.Now.ToString(),
			}
			);
		}
	

		private void RegisterClicked()
		{
			string newUserId = userIdInput.text;

			if (string.IsNullOrEmpty(newUserId))
			{
				feedbackMessage.text = "Name cannot be blank!";
				return;
			}

			newUserId = newUserId.Trim(); 		// leading and trailing white space
	
			if (newUserId.Length > MaxUserIdLength)
			{
				feedbackMessage.text = "Names are " + MaxUserIdLength + " characters max!";
				return;
			}

			registerButton.interactable = false;
			cancelButton.interactable = false;

			// check if name already exists
			FirebaseManager.GetUserProfile(userIdInput.text);		// callback will register if not already existing
			feedbackMessage.text = "Checking...";
//			if (YesSound != null)
//				AudioSource.PlayClipAtPoint(YesSound, Vector3.zero, FightManager.SFXVolume);
		}

		private void CancelClicked()
		{
//			if (NoSound != null)
//				AudioSource.PlayClipAtPoint(NoSound, Vector3.zero, FightManager.SFXVolume);

			AllowInput(false);

			StartCoroutine(Hide());
		}
			
		private void OnGetUser(string userId, UserProfile profile, bool success)
		{
			if (success)
			{
				if (profile == null)		// not found!
				{
					// register!
					if (userId == userIdInput.text)
					{
						userIdInput.interactable = false;

						feedbackMessage.text = "Registering '" + userId + "'...";
						RegisterNewUser();
					}
				}
				else
				{
					feedbackMessage.text = "Sorry - '" + userId + "' is already in use";
				}
			}
			else
			{
				feedbackMessage.text = "Unable to access users - please try again later!";
				AllowInput(true);
			}
		}


		private void OnUserUploaded(string userId, UserProfile profile, bool success)
		{
			if (!string.IsNullOrEmpty(FightManager.SavedGameStatus.UserId) && userId == FightManager.SavedGameStatus.UserId)
			{
				if (success)
				{
					feedbackMessage.text = "'" + userId + "' registered!";
					userIdInput.text = userId + "!";
					userIdInput.interactable = false;

					FightManager.SavedGameStatus.UserId = userId;

					StartCoroutine(Hide());
				}
				else
				{
					feedbackMessage.text = "Unable to register new user - please try again later!";
					AllowInput(true);
				}
			}
		}

		private void AllowInput(bool allow)
		{
			userIdInput.interactable = allow;
			registerButton.interactable = allow;
			cancelButton.interactable = allow;
		}
	}
}
