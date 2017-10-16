using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;


namespace FightingLegends
{
	public class DifficultySelector : MonoBehaviour
	{
		public Button simpleButton;
		public Button easyButton;
		public Button mediumButton;
		public Button hardButton;
		public Button brutalButton;

		public Color simpleColour;
		public Color easyColour;
		public Color mediumColour;
		public Color hardColour;
		public Color brutalColour;

		public Color simpleOffColour;
		public Color easyOffColour;
		public Color mediumOffColour;
		public Color hardOffColour;
		public Color brutalOffColour;

		public Color difficultyOffColour;		// almost transparent

		public Image simpleImage;
		public Image easyImage;
		public Image mediumImage;
		public Image hardImage;
		public Image brutalImage;

		public Image simpleGlow;
		public Image easyGlow;
		public Image mediumGlow;
		public Image hardGlow;
		public Image brutalGlow;


		public Text difficultyLabel;	// as selected
		private const float difficultyFadeTime = 0.3f;

		public AIDifficulty DefaultDifficulty { get; private set; }
		public AIDifficulty SelectedDifficulty { get; private set; }

		public delegate void DifficultySelectedDelegate(AIDifficulty selectedDifficulty);
		public DifficultySelectedDelegate OnDifficultySelected;


		private void Start()
		{
//			DefaultDifficulty = AIDifficulty.Medium;
//			SelectedDifficulty = DefaultDifficulty;
//
//			SyncDifficultyButtons();

			simpleButton.onClick.AddListener(delegate { SetDifficulty(AIDifficulty.Simple); });
			easyButton.onClick.AddListener(delegate { SetDifficulty(AIDifficulty.Easy); });
			mediumButton.onClick.AddListener(delegate { SetDifficulty(AIDifficulty.Medium); });
			hardButton.onClick.AddListener(delegate { SetDifficulty(AIDifficulty.Hard); });
			brutalButton.onClick.AddListener(delegate { SetDifficulty(AIDifficulty.Brutal); });
		}

		private void OnEnable()
		{
			SetDifficulty(FightManager.SavedGameStatus.Difficulty);
		}

		private void OnDestroy()
		{
			simpleButton.onClick.RemoveListener(delegate { SetDifficulty(AIDifficulty.Simple); });
			easyButton.onClick.RemoveListener(delegate { SetDifficulty(AIDifficulty.Easy); });
			mediumButton.onClick.RemoveListener(delegate { SetDifficulty(AIDifficulty.Medium); });
			hardButton.onClick.RemoveListener(delegate { SetDifficulty(AIDifficulty.Hard); });
			brutalButton.onClick.RemoveListener(delegate { SetDifficulty(AIDifficulty.Brutal); });
		}
			

		private void SetDifficulty(AIDifficulty difficulty)
		{			
			SelectedDifficulty = difficulty;
			difficultyLabel.text = FightManager.Translate(difficulty.ToString().ToLower());

			SyncDifficultyButtons();

			if (OnDifficultySelected != null)
				OnDifficultySelected(SelectedDifficulty);
		}


		public void EnableDifficulties(bool enable)
		{
			simpleButton.interactable = enable;
			easyButton.interactable = enable;
			mediumButton.interactable = enable;
			hardButton.interactable = enable;
			brutalButton.interactable = enable;
		}

		private void SyncDifficultyButtons()
		{
			StartCoroutine(ActivateDifficulty(AIDifficulty.Simple, SelectedDifficulty == AIDifficulty.Simple));
			StartCoroutine(ActivateDifficulty(AIDifficulty.Easy, SelectedDifficulty == AIDifficulty.Easy));
			StartCoroutine(ActivateDifficulty(AIDifficulty.Medium, SelectedDifficulty == AIDifficulty.Medium));
			StartCoroutine(ActivateDifficulty(AIDifficulty.Hard, SelectedDifficulty == AIDifficulty.Hard));
			StartCoroutine(ActivateDifficulty(AIDifficulty.Brutal, SelectedDifficulty == AIDifficulty.Brutal));
		}

		private void ResetDifficultyGlows()
		{
			ActivateGlow(simpleGlow, false);
			ActivateGlow(easyGlow, false);
			ActivateGlow(mediumGlow, false);
			ActivateGlow(hardGlow, false);
			ActivateGlow(brutalGlow, false);
		}

		private IEnumerator ActivateDifficulty(AIDifficulty difficulty, bool on)
		{
			float t = 0.0f;

			var glow = GetDifficultyGlow(difficulty);
			ActivateGlow(glow, on);
			
			var image = GetDifficultyImage(difficulty);

			Color startColour = image.color;
			Color targetColour = GetDifficultyColour(difficulty, on);

			if (image.color == targetColour)
				yield break;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / difficultyFadeTime); 
				image.color = Color.Lerp(startColour, targetColour, t);
				yield return null;
			}

			yield return null;
		}

		private void ActivateGlow(Image glow, bool activate)
		{
			if (glow == null)
				return;

			glow.GetComponent<Image>().enabled = activate;
			glow.GetComponent<Animator>().enabled = activate;
		}


		private Image GetDifficultyImage(AIDifficulty difficulty)
		{
			switch (difficulty)
			{
				case AIDifficulty.Simple:
					return simpleImage;

				case AIDifficulty.Easy:
					return easyImage;

				case AIDifficulty.Medium:
					return mediumImage;

				case AIDifficulty.Hard:
					return hardImage;

				case AIDifficulty.Brutal:
					return brutalImage;

				default:
					return mediumImage;
			}
		}

		private Image GetDifficultyGlow(AIDifficulty difficulty)
		{
			switch (difficulty)
			{
				case AIDifficulty.Simple:
					return simpleGlow;

				case AIDifficulty.Easy:
					return easyGlow;

				case AIDifficulty.Medium:
					return mediumGlow;

				case AIDifficulty.Hard:
					return hardGlow;

				case AIDifficulty.Brutal:
					return brutalGlow;

				default:
					return mediumGlow;
			}
		}


		private Color GetDifficultyColour(AIDifficulty difficulty, bool isOn)
		{
			switch (difficulty)
			{
				case AIDifficulty.Simple:
					return isOn ? simpleColour : simpleOffColour;

				case AIDifficulty.Easy:
					return isOn ? easyColour : easyOffColour;

				case AIDifficulty.Medium:
					return isOn ? mediumColour : mediumOffColour;

				case AIDifficulty.Hard:
					return isOn ? hardColour : hardOffColour;

				case AIDifficulty.Brutal:
					return isOn ? brutalColour : brutalOffColour;

				default:
					return mediumColour;
			}
		}
	}
}
