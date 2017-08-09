
using System;
using UnityEngine;
using System.Collections.Generic;

namespace FightingLegends
{
	// Class to encapsulate data required by both fighters
	// when a strike makes contact (ie. hit frame)
	// Also used to flag the last frame of a state,
	// when during a move another move can be queued, etc.
	[Serializable]
	public class HitFrameData
	{		
		public State State;					// part of index in dictionary for fast lookup
		public int FrameNumber = 1;			// part of index in dictionary for fast lookup
		public List<FrameAction> Actions;	// multiple events possible per frame (eg. hit + state end)

		public HitType TypeOfHit;
		public int HitStunFrames;		// duration of hit stun (recipient of hit)
		public int BlockStunFrames;		// duration of block stun (recipient of hit)
//		public int HitStunContinueFrame;		// stun frame at which a move continue is possible
//		public int BlockStunContinueFrame;		// stun frame at which a move continue is possible

		public float HitDamage;			// deducted from recipient's health
		public float BlockDamage;		// deducted from recipient's health
		public int FreezeFrames;		// freeze both characters on impact for effect

		public int CameraShakes;		// number of shakes 

		// FX

		public AudioClip SoundEffect;	// eg. hit impact, whiff
		public SpotFXType SpotEffect;
    }
}
