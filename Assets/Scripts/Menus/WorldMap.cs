using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace FightingLegends
{
	public class WorldMap : MenuCanvas
	{
		public Text titleLabel;

		public Button tokyoButton;
		public Button hongKongButton;
		public Button hawaiiButton;
		public Button sovietButton;
		public Button cubaButton;
		public Button nigeriaButton;
		public Button chinaButton;
		public Button ghettoButton;
		public Button spaceStationButton;

		public Image tokyoDot;
		public Image hongKongDot;
		public Image hawaiiDot;
		public Image sovietDot;
		public Image cubaDot;
		public Image nigeriaDot;
		public Image chinaDot;
		public Image ghettoDot;
		public Image spaceStationDot;

		public Image currentLocale;						// (yellow dot) plane?
		private const float currentZOffset = -5.0f;
	
		public ParticleSystem flightPath;
		public float flightSpeed;

		private bool TransportOnArrival = true;		// false for testing flight path

		private FightManager fightManager;

		private bool flying = false;

		public delegate void LocationSelectedDelegate(string location);
		public static LocationSelectedDelegate OnLocationSelected;


		public void Awake()
		{
			var fightManagerObject = GameObject.Find("FightManager");
			fightManager = fightManagerObject.GetComponent<FightManager>();

			FightManager.OnThemeChanged += SetTheme;
		
			titleLabel.text = FightManager.Translate("chooseLocation");
		}

		private void OnDestroy()
		{
			FightManager.OnThemeChanged -= SetTheme;
		}

		private void OnEnable()
		{
//			Debug.Log("WorldMap OnEnable: WorldMapPosition1 = " + fightManager.WorldMapPosition);

//			if (fightManager.WorldMapPosition == Vector3.zero)
//				fightManager.WorldMapPosition = new Vector3(hawaiiDot.rectTransform.localPosition.x, hawaiiDot.rectTransform.localPosition.y, hawaiiDot.rectTransform.localPosition.z + currentZOffset);

			InitWorldMapPosition();

//			Debug.Log("WorldMap OnEnable: WorldMapPosition2 = " + fightManager.WorldMapPosition);

			currentLocale.rectTransform.localPosition = fightManager.WorldMapPosition;
			flightPath.transform.localPosition = fightManager.WorldMapPosition;
			flightPath.Play();

			// button handlers
			tokyoButton.onClick.AddListener(FlyToTokyo);
			hongKongButton.onClick.AddListener(FlyToHongKong);
			hawaiiButton.onClick.AddListener(FlyToHawaii);
			sovietButton.onClick.AddListener(FlyToSoviet);
			cubaButton.onClick.AddListener(FlyToCuba);
			nigeriaButton.onClick.AddListener(FlyToNigeria);
			chinaButton.onClick.AddListener(FlyToChina);
			ghettoButton.onClick.AddListener(FlyToGhetto);

			spaceStationButton.onClick.AddListener(FlyToSpaceStation);

//			EnableSpaceStation();		// if completed earth locations

			if (! FightManager.IsNetworkFight)
				TickCompletedLocations();
		}

		private void OnDisable()
		{
			flightPath.Stop();

			// button handlers
			tokyoButton.onClick.RemoveListener(FlyToTokyo);
			hongKongButton.onClick.RemoveListener(FlyToHongKong);
			hawaiiButton.onClick.RemoveListener(FlyToHawaii);
			sovietButton.onClick.RemoveListener(FlyToSoviet);
			cubaButton.onClick.RemoveListener(FlyToCuba);
			nigeriaButton.onClick.RemoveListener(FlyToNigeria);
			chinaButton.onClick.RemoveListener(FlyToChina);
			ghettoButton.onClick.RemoveListener(FlyToGhetto);

			spaceStationButton.onClick.RemoveListener(FlyToSpaceStation);

			UnTickAllLocations();
		}

//		public override bool CanNavigateBack { get { return base.CanNavigateBack && NavigatedFrom != MenuType.MatchStats; } }
		public override bool CanNavigateBack { get { return base.CanNavigateBack && 
														(NavigatedFrom == MenuType.Combat || NavigatedFrom == MenuType.ArcadeFighterSelect ||
														NavigatedFrom == MenuType.SurvivalFighterSelect); } }


		private void InitWorldMapPosition()
		{
			if (string.IsNullOrEmpty(fightManager.SelectedLocation))
				fightManager.SelectedLocation = FightManager.hawaii;

			switch (fightManager.SelectedLocation)
			{
				case FightManager.hawaii:
					fightManager.WorldMapPosition = hawaiiDot.rectTransform.localPosition;
					break;

				case FightManager.china:
					fightManager.WorldMapPosition = chinaDot.rectTransform.localPosition;
					break;

				case FightManager.tokyo:
					fightManager.WorldMapPosition = tokyoDot.rectTransform.localPosition;
					break;

				case FightManager.ghetto:
					fightManager.WorldMapPosition = ghettoDot.rectTransform.localPosition;
					break;

				case FightManager.cuba:
					fightManager.WorldMapPosition = cubaDot.rectTransform.localPosition;
					break;

				case FightManager.nigeria:
					fightManager.WorldMapPosition = nigeriaDot.rectTransform.localPosition;
					break;

				case FightManager.soviet:
					fightManager.WorldMapPosition = sovietDot.rectTransform.localPosition;
					break;

				case FightManager.hongKong:
					fightManager.WorldMapPosition = hongKongDot.rectTransform.localPosition;
					break;

				case FightManager.spaceStation:
					fightManager.WorldMapPosition = spaceStationDot.rectTransform.localPosition;
					break;

				case FightManager.dojo:
				default:
					break;
			}
		}


		private void FlyToTokyo()
		{
			if (HasCompletedLocation(FightManager.tokyo))
				return;

			if (! flying)
				StartCoroutine(FlyTo(tokyoDot.rectTransform.localPosition, FightManager.tokyo, "Shiro"));
		}

		private void FlyToChina()
		{
			if (HasCompletedLocation(FightManager.china))
				return;

			if (! flying)
				StartCoroutine(FlyTo(chinaDot.rectTransform.localPosition, FightManager.china, "Shiyang"));
		}
			
		private void FlyToSoviet()
		{
			if (HasCompletedLocation(FightManager.soviet))
				return;

			if (! flying)
				StartCoroutine(FlyTo(sovietDot.rectTransform.localPosition, FightManager.soviet, "Natalya"));
		}
			
		private void FlyToGhetto()
		{
			if (HasCompletedLocation(FightManager.ghetto))
				return;
			
			if (! flying)
				StartCoroutine(FlyTo(ghettoDot.rectTransform.localPosition, FightManager.ghetto, "Jackson"));
		}
			
		private void FlyToHawaii()
		{
			if (HasCompletedLocation(FightManager.hawaii))
				return;
			
			if (! flying)
				StartCoroutine(FlyTo(hawaiiDot.rectTransform.localPosition, FightManager.hawaii, "Leoni"));
		}
			
		private void FlyToCuba()
		{
			if (HasCompletedLocation(FightManager.cuba))
				return;
			
			if (! flying)
				StartCoroutine(FlyTo(cubaDot.rectTransform.localPosition, FightManager.cuba, "Alazne"));
		}

		private void FlyToNigeria()
		{
			if (HasCompletedLocation(FightManager.nigeria))
				return;
									
			if (! flying)
				StartCoroutine(FlyTo(nigeriaDot.rectTransform.localPosition, FightManager.nigeria, "Danjuma"));
		}
			
		private void FlyToHongKong()
		{
			if (HasCompletedLocation(FightManager.hongKong))
				return;

			if (! flying)
				StartCoroutine(FlyTo(hongKongDot.rectTransform.localPosition, FightManager.hongKong, "Hoi Lun"));
		}

		private void FlyToSpaceStation()
		{
			if (! fightManager.CompletedEarthLocations)
				return;
			if (HasCompletedLocation(FightManager.spaceStation))
				return;

			if (! flying)
				StartCoroutine(FlyTo(spaceStationDot.rectTransform.localPosition, FightManager.spaceStation, "Skeletron"));
		}
			
//		public void EnableSpaceStation()
//		{
//			spaceStationButton.interactable = fightManager.CompletedEarthLocations;
////			spaceStationDot.gameObject.SetActive(fightManager.CompletedEarthLocations);
//		}


		private IEnumerator FlyTo(Vector3 destination, string location, string fighterName)
		{
			var destinationLocation = new Vector3(destination.x, destination.y, destination.z + currentZOffset);
			var startLocation = fightManager.WorldMapPosition;

//			Debug.Log("WorldMap FlyTo start: startLocation = " + startLocation);

//			if (destinationLocation != startLocation)
			{
				float t = 0.0f;
				flying = true;

				var flightDistance = (startLocation - destinationLocation).magnitude;		// no need for sqrMagnitude - flightSpeed adjusted accordingly
				var flightTime = flightDistance / flightSpeed;
//				Debug.Log("flightDistance = " + flightDistance + ", time = " + time);

				while (t < 1.0f)
				{
					t += Time.deltaTime * (Time.timeScale / flightTime); 

					currentLocale.rectTransform.localPosition = Vector3.Lerp(startLocation, destinationLocation, t);
					flightPath.transform.localPosition = Vector3.Lerp(startLocation, destinationLocation, t);
					yield return null;
				}

				fightManager.WorldMapPosition = destinationLocation;
				flying = false;
			}

			// return with selected location / AI fighter
			fightManager.SelectedLocation = location;
			fightManager.SelectedAIName = fighterName;

			if (OnLocationSelected != null)
				OnLocationSelected(location);

//			Debug.Log("WorldMap FlyTo end: WorldMapPosition = " + fightManager.WorldMapPosition);
		
			if (TransportOnArrival && !FightManager.IsNetworkFight)
				fightManager.WorldMapChoice = MenuType.Combat;	
			
			yield return null;
		}


		private void TickCompletedLocations()
		{
			UnTickAllLocations();

			var fighterProfile = Profile.GetFighterProfile(FightManager.SelectedFighterName);
	
			if (fighterProfile != null && FightManager.CombatMode == FightMode.Arcade)
			{
				foreach (var location in fighterProfile.CompletedLocations)
				{
					TickLocation(location, true);
				}
			}
		}

		private void UnTickAllLocations()
		{
			TickLocation(FightManager.hawaii, false);
			TickLocation(FightManager.china, false);
			TickLocation(FightManager.tokyo, false);
			TickLocation(FightManager.ghetto, false);
			TickLocation(FightManager.cuba, false);
			TickLocation(FightManager.nigeria, false);
			TickLocation(FightManager.soviet, false);
			TickLocation(FightManager.hongKong, false);
			TickLocation(FightManager.spaceStation, false);
		}

		private void TickLocation(string location, bool tick)
		{
			Image locationDot = null;

			switch (location)
			{
				case FightManager.hawaii:
					locationDot = hawaiiDot;
					break;
				case FightManager.china:
					locationDot = chinaDot;
					break;
				case FightManager.tokyo:
					locationDot = tokyoDot;
					break;
				case FightManager.ghetto:
					locationDot = ghettoDot;
					break;
				case FightManager.cuba:
					locationDot = cubaDot;
					break;
				case FightManager.nigeria:
					locationDot = nigeriaDot;
					break;
				case FightManager.soviet:
					locationDot = sovietDot;
					break;
				case FightManager.hongKong:
					locationDot = hongKongDot;
					break;
				case FightManager.spaceStation:
					locationDot = spaceStationDot;
					break;
			}

			if (locationDot != null)
				locationDot.transform.Find("Tick").gameObject.SetActive(tick);
		}

		private bool HasCompletedLocation(string location)
		{
			if (FightManager.CombatMode != FightMode.Arcade)
				return false;

			var fighterProfile = Profile.GetFighterProfile(FightManager.SelectedFighterName);
			
			return fighterProfile.CompletedLocations.Contains(location);
		}
	}
}
