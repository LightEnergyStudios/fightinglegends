﻿
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;


namespace FightingLegends
{
	// profile of fighter ProfileName
    public class Profile : MonoBehaviour
    {
		public string ProfileName;					// not used - set in Inspector purely as a label for the profile

		public GameObject SpotFXPrefab;				// triggers spot effects animations (set in Inspector)
		public GameObject ElementsFXPrefab;			// triggers elements effects animations (set in Inspector)
		public GameObject SmokeFXPrefab;			// triggers elements effects animations (set in Inspector)

		// values set in Inspector for each fighter
		public ProfileData ProfileData;  			// serializable - contains SavedProfile to save/load (filePath)

		// StateSignature represents the frames at which hits occur and data describing each hit
		// For hit frames, the data represents the type of strike, the damage inflicted, stun and freeze times, etc
		// Will not change so not saved / loaded
		// Values set in Inspector for each fighter
		public HitFrameSignature StateSignature;  	// serializable

		public delegate void ProfileLoadedDelegate(SavedProfile fighterProfile);
		public static ProfileLoadedDelegate OnProfileLoaded;

		public delegate void ProfileSavedDelegate(SavedProfile fighterProfile);
		public static ProfileSavedDelegate OnProfileSaved;
	

		public bool IsElement(FighterElement element)
		{
			return (ProfileData.Element1 == element || ProfileData.Element2 == element);
		}
			

		public void Save(string fighterName, string fighterColour, bool? isLocked = null)
		{
//			Debug.Log("Saving: " + fighterName);

//			BinaryFormatter bf = new BinaryFormatter();
//			FileStream file = File.Open(FilePath(fighterName), FileMode.OpenOrCreate);

			ProfileData.SavedData.FighterName = fighterName;					// in case not already set (ie. first save)
			ProfileData.SavedData.FighterColour = fighterColour;				// in case not already set (ie. first save)

			if (isLocked != null)
				ProfileData.SavedData.IsLocked = isLocked.GetValueOrDefault();
			
			ProfileData.SavedData.SavedTime = DateTime.Now;

			Save(ProfileData.SavedData);
		}

		public static void Save(SavedProfile profile)
		{
			BinaryFormatter bf = new BinaryFormatter();
			FileStream file = File.Open(FilePath(profile.FighterName), FileMode.OpenOrCreate);

			try
			{
				bf.Serialize(file, profile);
//				Debug.Log("Save ok: " + profile.FighterName + ", Level " + profile.Level + ", Trigger Power-up = " + profile.TriggerPowerUp + ", Static Power-up = " + profile.StaticPowerUp);

				if (OnProfileSaved != null)
					OnProfileSaved(profile);
			}
			catch (Exception ex)
			{
				Debug.Log("Save " + profile.FighterName + " failed: " + ex.Message);
			}
			finally
			{
				file.Close();
			}
		}
			

		// returns a new SavedProfile
		public static SavedProfile GetFighterProfile(string fighterName)
		{
			SavedProfile profile = new SavedProfile();

			if (LoadProfile(fighterName, false, out profile))
				return profile;

			return null;
		}

		// fills this instance of SavedProfile
		public bool LoadFighterProfile(string fighterName)
		{
			return LoadProfile(fighterName, true, out ProfileData.SavedData);
		}


		private static bool LoadProfile(string fighterName, bool deleteOnError, out SavedProfile profile)
		{
			var filePath = FilePath(fighterName);
			bool ok = false;

			if (File.Exists(filePath))
			{
				try
				{
					BinaryFormatter bf = new BinaryFormatter();
					FileStream file = File.Open(filePath, FileMode.Open);

					profile = (SavedProfile)bf.Deserialize(file);
					file.Close();

//					Debug.Log("LoadProfile ok: " + fighterName + ", Level " + profile.Level + ", Trigger Power-up = " + profile.TriggerPowerUp + ", Static Power-up = " + profile.StaticPowerUp);
					ok = true;
				}
				catch (Exception)
				{
					// TODO: most likely change in SavedProfile - handle 'upgrades'
					if (deleteOnError)
					{
						Debug.Log("LoadProfile: Deleting invalid file: " + filePath);
						File.Delete(filePath);
					}
					profile = new SavedProfile { FighterName = fighterName };
					ok = false;
				}
			}
			else
			{
//				Debug.Log("LoadProfile ok: " + fighterName + " (file not found)");
				profile = new SavedProfile { FighterName = fighterName };
				ok = true;
			}

			if (ok && profile.CompletedLocations == null)
				profile.CompletedLocations = new List<string>();

			if (ok && profile.Level > Fighter.maxLevel)
				profile.Level = Fighter.maxLevel;

			if (ok && OnProfileLoaded != null)
				OnProfileLoaded(profile);

//			if (ok)
//				Debug.Log("LoadProfile: " + fighterName + ", StaticPowerUp = " + profile.StaticPowerUp + ", TriggerPowerUp = " + profile.TriggerPowerUp);

			return ok;
		}


		private static string FilePath(string fighterName)
		{
			var fileName = fighterName + ".dat";
			return Application.persistentDataPath + "/" + fileName;
		}


		public static void InitFighterLockStatus(string fighterName, bool isLocked, int unlockOrder, int unlockCoins, int unlockDefeats, AIDifficulty unlockDifficulty) //, bool totalDefeats = false)
		{
			var profile = new SavedProfile();

			profile.FighterName = fighterName;
			profile.FighterColour = "P1";
			profile.SavedTime = DateTime.Now;
			profile.IsLocked = isLocked;

			profile.UnlockOrder = unlockOrder;
			profile.UnlockCoins = unlockCoins;
			profile.UnlockDefeats = unlockDefeats;
//			profile.TotalDefeats = totalDefeats;
			profile.UnlockDifficulty = unlockDifficulty;

			profile.Level = 1; // Level;
			profile.XP = profile.TotalXP = 0; // XP;

			Profile.Save(profile);
		}

		public static void DeleteFighterProfile(string fighterName)
		{
			var filePath = FilePath(fighterName);

			if (File.Exists(filePath))
			{
				File.Delete(filePath);
			}
		}
    }
}
