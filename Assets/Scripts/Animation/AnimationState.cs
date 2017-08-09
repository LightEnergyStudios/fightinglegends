
using System;
using UnityEngine;


namespace FightingLegends
{
	// Class to encapsulate the start and end frame numbers of an animation sequence / state / frame label
	public class AnimationState
	{		
		public string StateLabel;			// movie clip frame label

		public uint FirstFrame = 0;	
		public uint LastFrame = 0;	
		public bool StateLoops = false; 	// returns to start at end
		public int StateLength { get { return (int)(LastFrame - FirstFrame + 1); } }

		public bool HasEnded = false;		// played and reached last frame (and doesn't loop)
    }
}
