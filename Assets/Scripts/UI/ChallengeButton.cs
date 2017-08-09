using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace FightingLegends
{
	public class ChallengeButton : MonoBehaviour
	{
		public Text Name;
		public Text Date;
		public Text PrizeCoins;
		public RectTransform Fighters;		// viewport content

		public Image locationImage;
		public Image difficultyPanel;

		public Image simpleImage;
		public Image easyImage;
		public Image mediumImage;
		public Image hardImage;
		public Image brutalImage;

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


		public TeamChallenge Challenge { get; private set; }


		public void SetChallenge(TeamChallenge challenge)
		{
			Challenge = challenge;
			SetDifficulty();
		}
			
		private void SetDifficulty()
		{
			var teamDifficulty = GetTeamDifficulty(Challenge);

			difficultyPanel.enabled = teamDifficulty != null;		// enabled if not mixed team

			if (difficultyPanel.enabled)
			{
				simpleImage.color = (teamDifficulty == AIDifficulty.Simple) ? simpleColour : simpleOffColour;
				easyImage.color = (teamDifficulty == AIDifficulty.Easy) ? easyColour : easyOffColour;
				mediumImage.color = (teamDifficulty == AIDifficulty.Medium) ? mediumColour : mediumOffColour;
				hardImage.color = (teamDifficulty == AIDifficulty.Hard) ? hardColour : hardOffColour;
				brutalImage.color = (teamDifficulty == AIDifficulty.Brutal) ? brutalColour : brutalOffColour;
			}
		}

		private AIDifficulty? GetTeamDifficulty(TeamChallenge challenge)
		{
			AIDifficulty? difficulty = null;

			foreach (var teamMember in challenge.AITeam)
			{
				if (difficulty == null)					// first team member
					difficulty = teamMember.Difficulty;

				if (teamMember.Difficulty != difficulty)	// not same as first - must be mixed difficulties
					return null;
			}

			return difficulty;
		}

//		private AIDifficulty? TeamDifficulty
//		{
//			get
//			{
//				AIDifficulty? difficulty = null;
//
//				foreach (var teamMember in Challenge.AITeam)
//				{
//					if (difficulty == null)					// first team member
//						difficulty = teamMember.Difficulty;
//
//					if (teamMember.Difficulty != difficulty)	// not same as first - must be mixed difficulties
//						return null;
//				}
//
//				return difficulty;
//			}
//		}

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

		public void OnDestroy()
		{
			foreach (Transform fighter in Fighters.transform)
			{
				Destroy(fighter.gameObject);
			}
		}
	}
}
