using UnityEngine;
using System.Collections;

namespace FightingLegends
{
	public class Danjuma : Fighter
	{
		private const string DanjumaToLeoni = "It is good to keep heritage alive, but dancing around in a fight is going to get you hurt.";
		private const string DanjumaToShiro = "I can see a real warrior spirit in you. Stop fighting for status. Do it for the joy of it.";
		private const string DanjumaToNatalya = "You think you can kick hard? Try going barefoot!";
		private const string DanjumaToDanjuma = "Don't ever give up. That's how to win – it's really that simple.";
		private const string DanjumaToHoiLun = "They don't let girls compete in Dambe, but if they did you'd be a real contender!";
		private const string DanjumaToJackson = "You're soft. Too much technique and not enough fury. Remember your roots.";
		private const string DanjumaToShiyang = "The reason you lost is because you're far too used to not getting hit.";
		private const string DanjumaToAlazne = "It takes real courage to protect the innocent. I respect that.";
		private const string DanjumaToNinja = "Sorry to break this to you, but the swift never beat the strong.";
		private const string DanjumaToSkeletron = "What a monstrosity! I destroyed you not for sport, but for the good of all people.";

		// Danjuma's special extra returns straight to idle (ie. with no special recovery)
		protected override void EndSpecialExtra()	
		{
			CompleteMove();			// straight back to idle
		}

		public override string WinQuote(string loserName)
		{
			switch (loserName)
			{
				case "Shiro":
					return DanjumaToShiro;
				case "Natalya":
					return DanjumaToNatalya;
				case "Hoi Lun":
					return DanjumaToHoiLun;
				case "Leoni":
					return DanjumaToLeoni;
				case "Danjuma":
					return DanjumaToDanjuma;
				case "Jackson":
					return DanjumaToJackson;
				case "Alazne":
					return DanjumaToAlazne;
				case "Shiyang":
					return DanjumaToShiyang;
				case "Ninja":
					return DanjumaToNinja;
				case "Skeletron":
					return DanjumaToSkeletron;
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
