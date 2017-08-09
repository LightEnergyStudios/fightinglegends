using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace FightingLegends
{
	public class PowerUpButton : MonoBehaviour
	{
		public PowerUp PowerUp;
//		public Image Border;
		public Image Hilight;
		public string Name;
		public Text InventoryQuantity;
//		public int Cost;			// coins

		public bool IsTrigger = false;
		public int TriggerCoolDown = 0;			// resets after x 1/100s
		public Color TriggerFlash = Color.white;
	}


	// used to pass to AreYouSure for power-up preview / coin confirmation
	public class PowerUpDetails
	{
		public PowerUp PowerUp;
		public string Name;
		public string Description;
		public bool IsTrigger;

		public Sprite Icon;
		public int CoinValue = 0;
		public string Cost;			// formatted
		public string Activation;	// constantly active for static powerup / swipe-up for trigger (+ cooldown)
		public string Cooldown;		// for trigger powerup

		public string Confirmation; 		// coin confirmation
	}
}
