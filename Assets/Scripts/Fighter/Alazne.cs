using UnityEngine;
using System.Collections;

namespace FightingLegends
{
	public class Alazne : Fighter
	{
		private const string AlazneToLeoni = "Sure you've got the beach body - now you've got the broken face to go with it!";
		private const string AlazneToShiro = "You're heading down the wrong path sweetheart. Crime doesn't pay.";
		private const string AlazneToNatalya = "So you finally came back down to earth – now stay down!";
		private const string AlazneToDanjuma = "You're like a fierce little doggy, but I'll tame you one way or another.";
		private const string AlazneToHoiLun = "You should be at school. Don't make me bust you for truancy!";
		private const string AlazneToJackson = "I knew you were a troublemaker the moment I laid eyes on you. You're very easy on the eye though.";
		private const string AlazneToShiyang = "The law cannot tolerate vigilantes – you're under arrest!";
		private const string AlazneToAlazne = "Back off! This is my jurisdiction!";
		private const string AlazneToNinja = "You look like you're up to no good. Scram!";
		private const string AlazneToSkeletron = "You have the right to remain silent – forever!";

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
					return AlazneToShiro;
				case "Natalya":
					return AlazneToNatalya;
				case "Hoi Lun":
					return AlazneToHoiLun;
				case "Leoni":
					return AlazneToLeoni;
				case "Danjuma":
					return AlazneToDanjuma;
				case "Jackson":
					return AlazneToJackson;
				case "Alazne":
					return AlazneToAlazne;
				case "Shiyang":
					return AlazneToShiyang;
				case "Ninja":
					return AlazneToNinja;
				case "Skeletron":
					return AlazneToSkeletron;
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
