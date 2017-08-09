using UnityEngine;
using System.Collections;

namespace FightingLegends
{
	public class Shiro : Fighter
	{
		// win quotes
		private const string ShiroToLeoni = "Great body. You should swap the track pants for a miniskirt.";
		private const string ShiroToShiro = "You're not a real Yakuza like me!";
		private const string ShiroToNatalya = "That's great that you can fly. Shame you can't fight though.";
		private const string ShiroToDanjuma = "I never thought I'd say this to anyone, but I like your style!";
		private const string ShiroToHoiLun = "Want to join my gang? You can work your way up from the floor.";
		private const string ShiroToJackson = "Kicking like a donkey is for fools. Keep both feet on the ground if you want to win.";
		private const string ShiroToShiyang = "So you beat up street gangs do you? I hope you're choking on your own medicine!";
		private const string ShiroToAlazne = "I don't like cops! They interfere in an honest gangster's business!";
		private const string ShiroToNinja = "Revenge of Shinobi? I don't think so!";
		private const string ShiroToSkeletron = "Somewhere in Japan there's a much better robot body than that pile of junk!";

		// Shiro wraps up his counter attack with a special recovery...
		protected override void EndCounterAttack()
		{
			CurrentState = State.Special_Recover;
		}

		public override string WinQuote(string loserName)
		{
			switch (loserName)
			{
				case "Shiro":
					return ShiroToShiro;
				case "Natalya":
					return ShiroToNatalya;
				case "Hoi Lun":
					return ShiroToHoiLun;
				case "Leoni":
					return ShiroToLeoni;
				case "Danjuma":
					return ShiroToDanjuma;
				case "Jackson":
					return ShiroToJackson;
				case "Alazne":
					return ShiroToAlazne;
				case "Shiyang":
					return ShiroToShiyang;
				case "Ninja":
					return ShiroToNinja;
				case "Skeletron":
					return ShiroToSkeletron;
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
