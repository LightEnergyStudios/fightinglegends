
using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
 

namespace FightingLegends
{
	public class SceneryManager : MonoBehaviour
	{
		public bool PlaySceneryMusic;		// starts when a scene is constructed

		private Scenery[] sceneryData;		// prefab, width, replications, fog settings, etc

		// currentScenery is a list as a scene may comprise near and far (and possibly other) prefabs
		private List<Scenery> currentScenery;

		private string currentLocationName; // { get; private set; }

		public static AudioSource audioSource;

//		private FightManager fightManager;


		private void Awake()
		{
			sceneryData = GetComponents<Scenery>();
			currentScenery = new List<Scenery>();

			audioSource = GetComponent<AudioSource>();
		}


		private void OnEnable()
		{
//			var fightManagerObject = GameObject.Find("FightManager");
//			fightManager = fightManagerObject.GetComponent<FightManager>();

			FightManager.OnMusicVolumeChanged += MusicVolumeChanged;
		}

		private void OnDisable()
		{
			FightManager.OnMusicVolumeChanged -= MusicVolumeChanged;
		}
			

		public void Update()
		{
			// move scenery to 'cover' any camera left/right movement
			foreach (var scenery in currentScenery)
			{
				scenery.MoveWithCamera();
			}
		}
			
		public bool BuildScenery(string locationName)
		{
//			Debug.Log("BuildScenery: locationName = " + locationName);

			if (locationName == currentLocationName)		// don't load scenery if already there!
				return true;

			bool foundScenery = false;

			DestroyCurrentScenery();

			foreach (var scenery in sceneryData)
			{
				if (scenery.sceneryName == locationName)
				{
					foundScenery = true;

					scenery.ConstructScenery();	 // list of replications for left/right scrolling
					currentScenery.Add(scenery);	

					// there may be >1 Scenery with the same name (eg. for near and far views)
					// so continue looping through sceneryData array
				}
			}

			if (foundScenery)
			{
//				Debug.Log("BuildScenery: PlaySceneryMusic = " + PlaySceneryMusic);

				if (PlaySceneryMusic)
					PlayCurrentSceneryTrack();

				currentLocationName = locationName;
			}
				
//			Debug.Log("BuildScenery: " + currentLocationName + ", foundScenery = " + foundScenery);
			return foundScenery;
		}

		public void PlayCurrentSceneryTrack()
		{
			if (! PlaySceneryMusic)
				return;
			
			foreach (var scenery in currentScenery)
			{
				if (scenery.MusicTrack != null)
				{
					PlayMusicTrack(scenery.MusicTrack);
					break;
				}
			}
		}
			
		
		public static void PlayMusicTrack(AudioClip track)
		{			
			if (track == null)
				return;

			if (track == audioSource.clip)		// track already playing
				return;

			StopMusic();

			audioSource.clip = track;
			audioSource.clip.LoadAudioData();

			PlayMusic();
		}

		private static void PlayMusic()
		{
//			Debug.Log("ScenerayManager.PlayMusic: audioSource.clip.name = " + audioSource.clip.name);

			if (audioSource != null)
				audioSource.Play();
		}

		public static void PauseMusic()
		{
			if (audioSource != null)
				audioSource.Pause();
		}

		public static void UnPauseMusic()
		{
			if (audioSource != null)
				audioSource.UnPause();
		}

		public static void MuteMusic()
		{
			if (audioSource != null)
				audioSource.mute = true;
		}

		public static void UnMuteMusic()
		{
			if (audioSource != null)
				audioSource.mute = false;
		}

		public static void StopMusic()
		{
			if (audioSource != null)
			{
				if (audioSource.clip != null)
					audioSource.clip.UnloadAudioData();
				
				audioSource.Stop();
			}
		}

		private void MusicVolumeChanged(float newVolume)
		{
			audioSource.volume = newVolume;
		}

		private void DestroyCurrentScenery()
		{
			StopMusic();

			foreach (var current in currentScenery)
			{
				current.DestroyScenery();		// destroy each replication
			}

			currentScenery.Clear();
		}
	}
}
