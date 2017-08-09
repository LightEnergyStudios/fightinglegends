
using System;
using UnityEngine;
using System.Collections.Generic;


namespace FightingLegends
{
	[Serializable]

	public class HitFrameSignature
	{
		// Inspector variables for each hit frame, grouped for legibility and convenience
		// primarily for hit frames, but may represent state changes at other frames
		// (eg, whiff effect, can queue move, state end)
		public List<HitFrameData> LightStrike;
		public List<HitFrameData> MediumStrike;
		public List<HitFrameData> HeavyStrike;
		public List<HitFrameData> Shove;
		public List<HitFrameData> Special;
		public List<HitFrameData> Vengeance;
		public List<HitFrameData> Counter;
		public List<HitFrameData> Tutorial;		// ninja only
	}
}
