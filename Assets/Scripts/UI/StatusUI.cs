using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace FightingLegends
{
	public class StatusUI : MonoBehaviour
	{
		public Text ManagementText;

		public ProfileUI Player1Profile;
		public ProfileUI Player2Profile;

		private FightManager fightManager;

			
		// 'Constructor'
		// NOT called when returning from background
		private void Awake()
		{
			var fightManagerObject = GameObject.Find("FightManager");
			fightManager = fightManagerObject.GetComponent<FightManager>();
		}


		// initialization
		void Start()
		{
			SetFighters();
		}


		public void SetFighters()
		{
			if (fightManager != null)
			{
				if (Player1Profile != null)
					Player1Profile.Fighter = fightManager.Player1;

				if (Player2Profile != null)
					Player2Profile.Fighter = fightManager.Player2;
			}
		}


		private void Update()
		{
			UpdateStatus();
		}

		private void UpdateStatus()
		{
			ManagementText.color = fightManager.EitherFighterExpiredState ? Color.red :
									(fightManager.FightFrozen ? Color.blue : 
									(fightManager.FightPaused ? Color.magenta :
									(FightManager.SavedGameStatus.CompletedBasicTraining ? Color.white : Color.yellow)));

			if (! FightManager.SavedGameStatus.CompletedBasicTraining)
				ManagementText.text = "[ BASIC TRAINING ]";
			else
			{
//				ManagementText.text = " v" + fightManager.Version + "  ";
				ManagementText.text = "[ " + FightManager.CombatMode.ToString().ToUpper() + " ]";
				ManagementText.text += " ... MATCH " + fightManager.MatchCount + " / ROUND " + fightManager.RoundNumber;
				ManagementText.text += "\nCOINS: " + FightManager.Coins;

//				if (fightManager.TurboMode)
//					ManagementText.text += "\n[ TURBO ]";
				
//				ManagementText.text += "\n" + fightManager.CurrentSceneryName;

				ManagementText.text += string.Format("\n{0:0.##} FPS", fightManager.FightPaused ? 0.0f : fightManager.AnimationFPS);

//				ManagementText.text += "\n[ FRAME " + fightManager.AnimationFrameCount + " ]";

//				if (fightManager.FightFrozen)
//				{
//					ManagementText.text += "\n\n" + fightManager.FrozenStateUI;
//				}
//				else if (fightManager.FightPaused)
//				{
//					ManagementText.text += "\n\n[ ... FIGHT PAUSED ... ]";
//				}
//				else if (fightManager.EitherFighterExpiredState)
//				{
//					ManagementText.text += "\n\nR.I.P...";
//				}
	//			else if (fightManager.CanExecuteSwitchCommand) 		// next switch command
	//			{
	//				ManagementText.text += "\n\n[ -> <- ] " + fightManager.NextSwitchCommand.ToString();
	//			}
			}
		}
	}
}
