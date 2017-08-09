using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlaylistManager : MonoBehaviour
{
	public List<AudioClip> Tracks;

	private AudioSource audioSource;

	private int currentTrack;			// 1 is first track


	void Awake()
	{
		audioSource = GetComponent<AudioSource>();
	}


	private void Start()
	{
//		SelectTrack(1, false);
	}


	public void SelectTrack(int trackNumber, bool play)		// 1 is first track
	{
		if (Tracks.Count == 0)
			throw new Exception("SelectTrack: No tracks in music playlist!");
		
		currentTrack = trackNumber;

		if (currentTrack <= 0 || currentTrack >= Tracks.Count)
			currentTrack = 1;
		
		audioSource.clip = Tracks[currentTrack - 1];

		if (play)
			Play();
	}

	public void NextTrack()
	{
		Stop();
		currentTrack++;

		if (currentTrack >= Tracks.Count)
			currentTrack = 1;

		Play();
	}
		
	public void Play()
	{
		audioSource.Play();
	}

	public void Pause()
	{
		audioSource.Pause();
	}

	public void UnPause()
	{
		audioSource.UnPause();
	}

	public void Mute()
	{
		audioSource.mute = true;
	}

	public void UnMute()
	{
		audioSource.mute = false;
	}

	public void Stop()
	{
		audioSource.Stop();
	}

}
