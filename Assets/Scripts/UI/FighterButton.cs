using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace FightingLegends
{
	// simple class to encapsulate a FighterCard and attach a button to it for prefab instantiation
	// used to dynamically populate challenge buttons with AI team member buttons
	public class FighterButton : MonoBehaviour
	{
		private FighterCard fighterCard; 

		public void SetFighterCard(Sprite portrait, FighterCard card)
		{
			GetComponentInChildren<Image>().sprite = portrait;

			if (card != null)
			{
				fighterCard = card;

				fighterCard.SetButton(GetComponent<Button>());
				fighterCard.SetFighterData();				// setup button with level, xp, power-ups etc as per FighterCard
			}
				
			// button has no OnClick handler (container challenge button starts fight), but could if required (eg. to view profile?)
		}
	}
}
