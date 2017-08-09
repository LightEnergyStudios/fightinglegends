using UnityEngine;
using System.Collections;

namespace FightingLegends
{
	public class Natalya : Fighter
	{
		private const string NatalyaToLeoni = "You're a very attractive woman, but Russian girls are still the greatest!";
		private const string NatalyaToShiro = "I can beat up men who would fold you in half. Don't waste my time.";
		private const string NatalyaToNatalya = "In Soviet Russia it is I that kicks you.";
		private const string NatalyaToDanjuma = "You lay on quite a beating I'll give you that, but I can take ten times the punishment.";
		private const string NatalyaToHoiLun = "Skateboards don't work in the snow. The children I know play with kettlebells.";
		private const string NatalyaToJackson = "Tall, dark and handsome. A pity you live so far away.";
		private const string NatalyaToShiyang = "You're a superb fighter, I'm impressed! But you need to learn to soak up pain better.";
		private const string NatalyaToAlazne = "You can't fight even with a weapon in your hand. Shape up!";
		private const string NatalyaToNinja = "You are weak. Like kitten.";
		private const string NatalyaToSkeletron = "How clever that you made a body of iron. In Russia we turn our bodies into iron!";

		// Natalya's special extra returns straight to idle (ie. with no special recovery)
		protected override void EndSpecialExtra()	
		{
			CompleteMove();			// straight back to idle
		}

		public override string WinQuote(string loserName)
		{
			switch (loserName)
			{
				case "Shiro":
					return NatalyaToShiro;
				case "Natalya":
					return NatalyaToNatalya;
				case "Hoi Lun":
					return NatalyaToHoiLun;
				case "Leoni":
					return NatalyaToLeoni;
				case "Danjuma":
					return NatalyaToDanjuma;
				case "Jackson":
					return NatalyaToJackson;
				case "Alazne":
					return NatalyaToAlazne;
				case "Shiyang":
					return NatalyaToShiyang;
				case "Ninja":
					return NatalyaToNinja;
				case "Skeletron":
					return NatalyaToSkeletron;
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
			get { return SmokeFXType.Uppercut; }
		}
	}
}
