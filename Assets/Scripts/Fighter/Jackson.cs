using UnityEngine;
using System.Collections;

namespace FightingLegends
{
	public class Jackson : Fighter
	{
		private const string JacksonToLeoni = "Sleep tight prom queen!";
		private const string JacksonToShiro = "Don't arouse my anger. Fool!";
		private const string JacksonToNatalya = "You can really kick some. A dude better watch out he doesn't cross you!";
		private const string JacksonToDanjuma = "You're like a rough diamond that needs a little polishing, that's all";
		private const string JacksonToHoiLun = "Stay off the street! It's more dangerous than you know.";
		private const string JacksonToJackson = "You're just a jive turkey!";
		private const string JacksonToShiyang = "You're like me. You shepherd the weak from the tyranny of evil men.";
		private const string JacksonToAlazne = "Damn! You're all woman!";
		private const string JacksonToNinja = "Catch a nap, Kitty Cat.";
		private const string JacksonToSkeletron = "Man, you are straight out of a comic book!";

		public override string WinQuote(string loserName)
		{
			switch (loserName)
			{
				case "Shiro":
					return JacksonToShiro;
				case "Natalya":
					return JacksonToNatalya;
				case "Hoi Lun":
					return JacksonToHoiLun;
				case "Leoni":
					return JacksonToLeoni;
				case "Danjuma":
					return JacksonToDanjuma;
				case "Jackson":
					return JacksonToJackson;
				case "Alazne":
					return JacksonToAlazne;
				case "Shiyang":
					return JacksonToShiyang;
				case "Ninja":
					return JacksonToNinja;
				case "Skeletron":
					return JacksonToSkeletron;
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
			get { return SmokeFXType.Hook; }
		}
	}
}
