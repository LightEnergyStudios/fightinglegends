using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace FightingLegends
{
	public class Shiyang : Fighter
	{
		private const string ShiyangToLeoni = "You are a beautiful flower with deadly thorns. Truly a woman I can respect.";
		private const string ShiyangToShiro = "I did not want to soil my fists on scum like you, but you gave me no choice.";
		private const string ShiyangToNatalya = "Such amazing agility! You would excel at Chinese martial arts.";
		private const string ShiyangToDanjuma = "You have a will of iron and fists of steel, but your skills could be sharper.";
		private const string ShiyangToHoiLun = "You have a big heart. It is a dangerous world and you will need it.";
		private const string ShiyangToJackson = "Being a protector is not easy. We do it because we have to.";
		private const string ShiyangToShiyang = "Ghosts cannot die and shadows do not bleed. We must always remember that.";
		private const string ShiyangToAlazne = "The police do their best, but the streets need heroes without limits.";
		private const string ShiyangToNinja = "Speed alone cannot beat real skill.";
		private const string ShiyangToSkeletron = "Real strength comes not from the mind, but from the heart.";

		public override string WinQuote(string loserName)
		{
			switch (loserName)
			{
				case "Shiro":
					return ShiyangToShiro;
				case "Natalya":
					return ShiyangToNatalya;
				case "Hoi Lun":
					return ShiyangToHoiLun;
				case "Leoni":
					return ShiyangToLeoni;
				case "Danjuma":
					return ShiyangToDanjuma;
				case "Jackson":
					return ShiyangToJackson;
				case "Alazne":
					return ShiyangToAlazne;
				case "Shiyang":
					return ShiyangToShiyang;
				case "Ninja":
					return ShiyangToNinja;
				case "Skeletron":
					return ShiyangToSkeletron;
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
