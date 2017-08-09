using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace FightingLegends
{
	public class Leoni : Fighter
	{
		private const string LeoniToLeoni = "Isn't capoeira great? Keep on dancing!";
		private const string LeoniToShiro = "Nice try kid, but you're a bit short for me.";
		private const string LeoniToNatalya = "How do you get so much air in those boots?";
		private const string LeoniToDanjuma = "You are very, very tough. My feet are sore from kicking your ass!";
		private const string LeoniToHoiLun = "What a cutie you are! Let's go to the mall and chat up some boys.";
		private const string LeoniToJackson = "No doubt about it you're a very handsome man, but I'm a still a bit scared of you!";
		private const string LeoniToShiyang = "The way you move is just amazing. You'd make a great dancer.";
		private const string LeoniToAlazne = "I could show you a great dancing workout to shed those extra pounds.";
		private const string LeoniToNinja = "Sorry crazy cat lady, but I'm more of a dog person.";
		private const string LeoniToSkeletron = "To be honest I think you're over-compensating for something.";

		public override string WinQuote(string loserName)
		{
			switch (loserName)
			{
				case "Shiro":
					return LeoniToShiro;
				case "Natalya":
					return LeoniToNatalya;
				case "Hoi Lun":
					return LeoniToHoiLun;
				case "Leoni":
					return LeoniToLeoni;
				case "Danjuma":
					return LeoniToDanjuma;
				case "Jackson":
					return LeoniToJackson;
				case "Alazne":
					return LeoniToAlazne;
				case "Shiyang":
					return LeoniToShiyang;
				case "Ninja":
					return LeoniToNinja;
				case "Skeletron":
					return LeoniToSkeletron;
				default:
					return "";
			}
		}


		protected override SmokeFXType SpecialSmoke
		{
			get { return SmokeFXType.Hook; }
		}

		protected override SmokeFXType VengeanceSmoke
		{
			get { return SmokeFXType.Uppercut; }
		}
	}
}
