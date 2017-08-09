using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace FightingLegends
{
	public class HoiLun : Fighter
	{
		private const string HoiLunToLeoni = "Wow! Those dance moves are so cool! Can you show me the steps?";
		private const string HoiLunToShiro = "You've got a motorbike? That is so cool!";
		private const string HoiLunToNatalya = "It's so cool that you fix aeroplanes!";
		private const string HoiLunToDanjuma = "You're just a little guy but you hit like a train. So cool!";
		private const string HoiLunToHoiLun = "Wing Chun is so cool! Thanks for the Chi Sao!";
		private const string HoiLunToJackson = "I can't believe it's you! You were in that movie right? With that other guy! So cool!";
		private const string HoiLunToShiyang = "Oh my God, you're SO COOL! I know you let me win – can we go out on a date?";
		private const string HoiLunToAlazne = "Being a policewoman must be so cool! Do you get in high-speed chases?";
		private const string HoiLunToNinja = "I love cats! That mask is really cool!";
		private const string HoiLunToSkeletron = "I think you're just a big meanie. Bullying people doesn't make you cool.";

		public override string WinQuote(string loserName)
		{
			switch (loserName)
			{
				case "Shiro":
					return HoiLunToShiro;
				case "Natalya":
					return HoiLunToNatalya;
				case "Hoi Lun":
					return HoiLunToHoiLun;
				case "Leoni":
					return HoiLunToLeoni;
				case "Danjuma":
					return HoiLunToDanjuma;
				case "Jackson":
					return HoiLunToJackson;
				case "Alazne":
					return HoiLunToAlazne;
				case "Shiyang":
					return HoiLunToShiyang;
				case "Ninja":
					return HoiLunToNinja;
				case "Skeletron":
					return HoiLunToSkeletron;
				default:
					return "";
			}
		}


		protected override SmokeFXType SpecialSmoke
		{
			get { return SmokeFXType.Straight; }
		}

		protected override SmokeFXType VengeanceSmoke
		{
			get { return SmokeFXType.Straight; }
		}
	}
}
