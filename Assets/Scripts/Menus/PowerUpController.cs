using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// script to handle power-up animator events
public class PowerUpController : MonoBehaviour
{
	public Animator PowerUpAnimator;		// entry / exit - set in Inspector

	public delegate void EnterPowerUpDelegate();
	public EnterPowerUpDelegate OnPowerUpEntry;

	public delegate void EntryCompleteDelegate();
	public EntryCompleteDelegate OnPowerUpEntryComplete;

	public delegate void ExitCompleteDelegate();
	public ExitCompleteDelegate OnPowerUpExitComplete;


	// exit animation to make power-ups 'fly away'
	// (entry animation triggered when power-up overlay enabled)
	public void TriggerExitAnimation()
	{
		if (PowerUpAnimator != null)
			PowerUpAnimator.SetTrigger("ExitPowerUps");

	}

	// event called from animator on entry of each power-up
	// relay event to listeners (eg. Store)
	public void OnEnterPowerUp()
	{
		if (OnPowerUpEntry != null)
			OnPowerUpEntry();
	}

	// event called from animator on completion of entry animation
	// relay event to listeners (eg. Store)
	public void OnEntryComplete()
	{
		if (OnPowerUpEntryComplete != null)
			OnPowerUpEntryComplete();
	}

	// event called from animator on completion of exit animation
	// relay event to listeners (eg. Store)
	public void OnExitComplete()
	{
		if (OnPowerUpExitComplete != null)
			OnPowerUpExitComplete();
	}
}
