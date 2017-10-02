using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;


namespace FightingLegends
{
	public class GameUI : MonoBehaviour
	{
		public HealthUI Player1Health;
		public HealthUI Player2Health;
	
		public Text CombatMode;
		public Text ScoreDash;

		// score
		public Color ScoreColour;				// arcade and training
		public Color NetworkColour;				// multiplayer
		public Color SurvivalScoreColour;
		public Color ChallengeScoreColour;
		public Color DojoColour;

		// traffic light
		public static bool TrafficLightVisible = false;		// arcade / dojo only
		private TrafficLight currentTrafficLight = TrafficLight.None;

		public GameObject TrafficLights;
		public Image UnlitTrafficLight;	
		public Image RedTrafficLight;	
		public Image YellowTrafficLight;
		public Image GreenTrafficLight;
		public Image LeftTrafficLight;			// green
		public Image RightTrafficLight;			// green

		public ParticleSystem RedLightStars;
		public ParticleSystem YellowLightStars;
		public ParticleSystem GreenLightStars;
		public AudioClip trafficLightSound;
		private const float trafficLightFlashInterval = 0.1f;

		// combo steps
		public GameObject comboStepPrefab;

		public Image ComboPanel;		// steps are children
		private List<ComboStepUI> comboUISteps;

//		public Image ComboVerticalFlash;	// to hilight current combo step - animated
		public Text ComboPrompt;			// tap! etc. (child of panel)
		public Text ComboText;				// eg. for testing (child of panel)

		public Image Splat;
		public Image PaintStrokeLeft;
		public Image PaintStrokeRight;

		private const float splatTime = 0.15f;
		private Color splatMinColour = Color.clear;
		private Color splatMaxColour = Color.white;

		private float splatMinScale = 2.5f;
		private float splatMaxScale = 3.2f;

		// images for combo steps
		public Sprite tapSprite;
		public Sprite holdSprite;
		public Sprite swipeForwardSprite;
		public Sprite swipeBackSprite;
		public Sprite swipeUpSprite;
		public Sprite swipeDownSprite;
		public Sprite swipeVengeanceSprite;
		public Sprite mashSprite;
		public Sprite resetSprite;

//		public AudioClip ComboStepAudio;				// bling

//		public Color StepWaitingColour;					// white
//		public Color StepSetupColour;					// semi-transparent (when first set up)
//		public Color StepCompletedColour;				// semi-transparent (tick enabled)
//		public Color StepDefaultColour;					// semi-transparent
//
//		private const float comboSetupPause = 0.1f;		// as each step set up
//		private const float comboStepWidth = 64;		// image
//		private const float comboStepSpace = 6;			// between images
//		private const float comboStepGrowTime = 0.2f; 	// when waiting for input (activated)
//		private const float comboStepGrowScale = 2.0f; 	// when waiting for input (activated)

		// power-up sprites (for HealthUI level buttons) - set in Inspector
		public Sprite ArmourPiercing;
		public Sprite Avenger;
		public Sprite Ignite;
		public Sprite HealthBooster;
		public Sprite PoiseMaster;
		public Sprite PoiseWrecker;
		public Sprite PowerAttack;
		public Sprite Regenerator;
		public Sprite SecondLife;
		public Sprite VengeanceBooster;

		// entry animation sounds
		public AudioClip NameEnterSound;
		public AudioClip NameArriveSound;
		public AudioClip GaugeSlotEnterSound;
		public AudioClip GaugeSlotArriveSound;
		public AudioClip GaugeCrystalArriveSound;

		public delegate void TrafficLightDelegate(TrafficLight colour, bool flashing);
		public static TrafficLightDelegate TrafficLightInfoBubble;

		public delegate void GaugeIncreasedDelegate(int newGauge);
		public static GaugeIncreasedDelegate OnGaugeIncreased;


		private FightManager fightManager;
		private CameraController cameraController;

		// 'Constructor'
		// NOT called when returning from background
		private void Awake()
		{
			var fightManagerObject = GameObject.Find("FightManager");
			fightManager = fightManagerObject.GetComponent<FightManager>();

			cameraController = Camera.main.GetComponent<CameraController>();

			gameObject.SetActive(false);		// until fighters are set
		}
	
		// initialization
		void Start()
		{
			SetTrafficLightColour(TrafficLight.None);
			SetFighters(true);
		}

		private void OnEnable()
		{
			SetupCombatMode();
		}

		private void OnDestroy()
		{
			StopListening();
		}
			
	
		public void SetupCombatMode()
		{				
			if (FightManager.CombatMode == FightMode.Challenge)			// score represents number left in each team
			{
				Player1Health.ShowScore(true);
				Player2Health.ShowScore(true);
				ScoreDash.gameObject.SetActive(true);

				var category = fightManager.ChosenCategory.ToString().ToLower();
				var mode = FightManager.CombatMode.ToString().ToLower();
				var challengeName = (fightManager.ChosenCategory == ChallengeCategory.None) ? "" : FightManager.Translate(category);  // fightManager.ChosenCategory.ToString().ToUpper();
				CombatMode.text = challengeName + " " + FightManager.Translate(mode);

				Player1Health.SetScoreColour(ChallengeScoreColour);
				Player2Health.SetScoreColour(ChallengeScoreColour);
				ScoreDash.color = ChallengeScoreColour;
				CombatMode.color = ChallengeScoreColour;
			}
			else if (FightManager.CombatMode == FightMode.Survival) 	// not relevant for AI (match end as soon as AI wins)
			{
				Player1Health.ShowScore(true);
				Player2Health.ShowScore(true);					// not relevant - end of match when AI wins!
				ScoreDash.gameObject.SetActive(true);			// not relevant - end of match when AI wins!

				var mode = FightManager.CombatMode.ToString().ToLower();
				CombatMode.text = FightManager.Translate(mode);

				Player1Health.SetScoreColour(SurvivalScoreColour);
				Player2Health.SetScoreColour(SurvivalScoreColour);	
				ScoreDash.color = SurvivalScoreColour;	
				CombatMode.color = SurvivalScoreColour;
			}
			else if (FightManager.CombatMode == FightMode.Dojo)
			{
				Player1Health.ShowScore(false);
				Player2Health.ShowScore(false);					// not relevant
				ScoreDash.gameObject.SetActive(false);			// not relevant

				var mode = FightManager.CombatMode.ToString().ToLower();
				CombatMode.text = FightManager.Translate(mode);

				Player1Health.SetScoreColour(DojoColour);
				Player2Health.SetScoreColour(DojoColour);	
				ScoreDash.color = DojoColour;	
				CombatMode.color = DojoColour;
			}
			else  		// arcade / training
			{
				bool trainingCompleted = FightManager.SavedGameStatus.CompletedBasicTraining;

				Player1Health.ShowScore(trainingCompleted);
				Player2Health.ShowScore(trainingCompleted);
				ScoreDash.gameObject.SetActive(trainingCompleted);

				if (trainingCompleted)
				{
					var mode = FightManager.CombatMode.ToString().ToLower();
					var difficulty = FightManager.SavedGameStatus.Difficulty.ToString().ToLower();

					if (FightManager.IsNetworkFight && FightManager.CombatMode == FightMode.Arcade)
						CombatMode.text = string.Format("{0} - {1}", FightManager.Translate(mode), FightManager.Translate("twoPlayer"));
					else
						CombatMode.text = FightManager.SavedGameStatus.NinjaSchoolFight ? FightManager.Translate("ninjaSchool")
												: string.Format("{0} - {1}", FightManager.Translate(mode), FightManager.Translate(difficulty));
				}
				else
					CombatMode.text = FightManager.Translate("ninjaSchool");

				if (FightManager.IsNetworkFight && FightManager.CombatMode == FightMode.Arcade)
				{
					Player1Health.SetScoreColour(NetworkColour);
					Player2Health.SetScoreColour(NetworkColour);
					ScoreDash.color = NetworkColour;
					CombatMode.color = NetworkColour;
				}
				else
				{
					Player1Health.SetScoreColour(ScoreColour);
					Player2Health.SetScoreColour(ScoreColour);
					ScoreDash.color = ScoreColour;
					CombatMode.color = ScoreColour;
				}
			}

			// no traffic lights in hard/brutal arcade, survival, challenge or training modes (training 'scripts' controls traffic lights)
			bool arcadeTrafficLights = FightManager.CombatMode == FightMode.Arcade && (FightManager.SavedGameStatus.NinjaSchoolFight || FightManager.SavedGameStatus.Difficulty <= AIDifficulty.Medium);
			EnableTrafficLights(arcadeTrafficLights || FightManager.CombatMode == FightMode.Dojo);
		}

		private void StartListening()
		{
			if (Player1Health.Fighter != null)
			{
				var player1 = Player1Health.Fighter;

				// power-up listeners
				player1.OnTriggerPowerUpChanged += Player1SetTriggerPowerUp;
				player1.OnStaticPowerUpChanged += Player1SetStaticPowerUp;

				// traffic light listeners
				player1.OnStateStarted += OnStateStarted;
				player1.OnCanContinue += OnCanContinue;
				player1.OnGaugeChanged += OnGaugeChanged;

				if (player1.InTraining)
				{
					player1.Trainer.OnTriggerSplat += DoSplat;
					player1.Trainer.OnHideSplat += HideSplat;
					player1.Trainer.OnTriggerPaintStroke += DoPaintStroke;
					player1.Trainer.OnHidePaintStroke += HidePaintStrokes;
				}
			}

			// listen to player2 powerup changes for when AI executes powerups
			if (Player2Health.Fighter != null)
			{
				Player2Health.Fighter.OnTriggerPowerUpChanged += Player2SetTriggerPowerUp;
				Player2Health.Fighter.OnStaticPowerUpChanged += Player2SetStaticPowerUp;

				// traffic light listeners
				Player2Health.Fighter.OnStateStarted += OnStateStarted;
				Player2Health.Fighter.OnLastHit += OnLastHit;
//				Player2Health.Fighter.OnKnockOut += OnKnockOut;
			}

			FightManager.OnReadyToFight += OnReadyToFight;

//			FightManager.OnTriggerSplat += DoSplat;
//			FightManager.OnHideSplat += HideSplat;
//			FightManager.OnTriggerPaintStroke += DoPaintStroke;
//			FightManager.OnHidePaintStroke += HidePaintStroke;

//			FightManager.OnFeedbackStateStart += FeedbackStart;
//			FightManager.OnFeedbackStateEnd += FeedbackEnd;
		}

		private void StopListening()
		{
			if (Player1Health.Fighter != null)
			{
				var player1 = Player1Health.Fighter;

				player1.OnTriggerPowerUpChanged -= Player1SetTriggerPowerUp;
				player1.OnStaticPowerUpChanged -= Player1SetStaticPowerUp;

				player1.OnStateStarted -= OnStateStarted;
				player1.OnCanContinue -= OnCanContinue;
				player1.OnGaugeChanged -= OnGaugeChanged;

				if (player1.InTraining)
				{
					player1.Trainer.OnTriggerSplat -= DoSplat;
					player1.Trainer.OnHideSplat -= HideSplat;
					player1.Trainer.OnTriggerPaintStroke -= DoPaintStroke;
					player1.Trainer.OnHidePaintStroke -= HidePaintStrokes;
				}
			}
				
			if (Player2Health.Fighter != null)
			{
				Player2Health.Fighter.OnTriggerPowerUpChanged -= Player2SetTriggerPowerUp;
				Player2Health.Fighter.OnStaticPowerUpChanged -= Player2SetStaticPowerUp;

				// traffic light listeners
				Player2Health.Fighter.OnStateStarted -= OnStateStarted;
				Player2Health.Fighter.OnLastHit -= OnLastHit;
//				Player2Health.Fighter.OnKnockOut -= OnKnockOut;		// turn off traffic lights
			}

			FightManager.OnReadyToFight -= OnReadyToFight;

//			FightManager.OnTriggerSplat -= DoSplat;
//			FightManager.OnHideSplat -= HideSplat;
//			FightManager.OnTriggerPaintStroke -= DoPaintStroke;
//			FightManager.OnHidePaintStroke -= HidePaintStroke;

//			FightManager.OnFeedbackStateStart -= FeedbackStart;
//			FightManager.OnFeedbackStateEnd -= FeedbackEnd;
		}

		public void SetFighters(bool active)
		{
			if (fightManager != null)
			{
				StopListening();		// detach from current fighters, if any

				if (fightManager.HasPlayer1)
				{
					Player1Health.Fighter = fightManager.Player1;		// sets name / health / gauge
					SetPowerUpSprites(fightManager.Player1);

					// initialise power-up sprites
					Player1SetTriggerPowerUp(fightManager.Player1.TriggerPowerUp);
					Player1SetStaticPowerUp(fightManager.Player1.StaticPowerUp);
				}

				if (fightManager.HasPlayer2)
				{
					Player2Health.Fighter = fightManager.Player2;
					SetPowerUpSprites(fightManager.Player2);

					// initialise power-up sprites
					Player2SetTriggerPowerUp(fightManager.Player2.TriggerPowerUp);
					Player2SetStaticPowerUp(fightManager.Player2.StaticPowerUp);
				}

				StartListening();
				gameObject.SetActive(active);
			}
		}


		private void Player1SetTriggerPowerUp(PowerUp powerUp)
		{
//			Debug.Log("Player1SetTriggerPowerUp: " + powerUp);

			var sprite = PowerUpSprite(powerUp);
			Player1Health.SetTriggerPowerUp(sprite, false);
		}

		private void Player2SetTriggerPowerUp(PowerUp powerUp)
		{
//			Debug.Log("Player2SetTriggerPowerUp: " + powerUp);

			var sprite = PowerUpSprite(powerUp);
			Player2Health.SetTriggerPowerUp(sprite, false);
		}

		private void Player1SetStaticPowerUp(PowerUp powerUp)
		{
//			Debug.Log("Player1SetStaticPowerUp: " + powerUp);

			var sprite = PowerUpSprite(powerUp);
			Player1Health.SetStaticPowerUp(sprite, false);
		}

		private void Player2SetStaticPowerUp(PowerUp powerUp)
		{
//			Debug.Log("Player2SetStaticPowerUp: " + powerUp);

			var sprite = PowerUpSprite(powerUp);
			Player2Health.SetStaticPowerUp(sprite, false);
		}
			

		private void SetPowerUpSprites(Fighter fighter)
		{
//			Debug.Log(fighter.FullName + ": SetPowerUpSprites: Trigger: " + fighter.TriggerPowerUp + " Static: " + fighter.StaticPowerUp);

			var triggerPowerUp = PowerUpSprite(fighter.TriggerPowerUp);
			var staticPowerUp = PowerUpSprite(fighter.StaticPowerUp);

			if (fighter.IsPlayer1)
				Player1Health.SetPowerUps(triggerPowerUp, staticPowerUp, false);
			else
				Player2Health.SetPowerUps(triggerPowerUp, staticPowerUp, false);
		}

		private Sprite PowerUpSprite(PowerUp powerUp)
		{
			switch (powerUp)
			{
				case PowerUp.None:
				default:
					return null;

				case PowerUp.ArmourPiercing:
					return ArmourPiercing;

				case PowerUp.Avenger:
					return Avenger;
						
				case PowerUp.Ignite:
					return Ignite;

				case PowerUp.HealthBooster:
					return HealthBooster;

				case PowerUp.PoiseMaster:
					return PoiseMaster;
						
				case PowerUp.PoiseWrecker:
					return PoiseWrecker;

				case PowerUp.PowerAttack:
					return PowerAttack;

				case PowerUp.Regenerator:
					return Regenerator;

				case PowerUp.SecondLife:
					return SecondLife;

				case PowerUp.VengeanceBooster:
					return VengeanceBooster;
			}
		}


		public void ShowFighterLevels(bool show)
		{
			Player1Health.ShowLevel(show);
			Player2Health.ShowLevel(show);
		}

		#region traffic lights
	
		private void SetStateTrafficLight(FighterChangedData newState)
		{
			// traffic lights driven by state only in arcade mode or dojo
			// training steps control traffic lights (not driven by state)
			if (FightManager.CombatMode == FightMode.Survival || FightManager.CombatMode == FightMode.Challenge || FightManager.CombatMode == FightMode.Training)
				return;

			if (! gameObject.activeSelf)		// eg. if HUD up (debug only)
				return;

			var fighter = newState.Fighter;
			bool lastHit = newState.ChangeType == FighterChangeType.LastHit;

			var player1 = fightManager.Player1;
			var player2 = fightManager.Player2;

//			Debug.Log("SetStateTrafficLight player1 = " + player1.FighterName + "/" + player1.ColourScheme + " player2 = " + player2.FighterName + "/" + player2.ColourScheme);

			bool fighterCanCounter = player1.HasCounterGauge && (player1.IsIdle || player1.IsDashing || player1.IsBlockIdle || player1.IsBlockStunned);

			bool AIapproaching = player2.CurrentState == State.Light_Windup || player2.CurrentState == State.Medium_Windup ||
				player2.CurrentState == State.Heavy_Windup || player2.CurrentState == State.Counter_Attack ||
				player2.CurrentState == State.Special || player2.CurrentState == State.Special_Start || player2.CurrentState == State.Special_Opportunity ||
				player2.CurrentState == State.Special_Extra || player2.CurrentState == State.Vengeance;

//			bool AIvulnerable = player2.CurrentState == State.Light_Cutoff || player2.CurrentState == State.Medium_Cutoff ||
//			                    player2.CurrentState == State.Heavy_Cutoff || player2.CurrentState == State.Counter_Recovery ||
//			                    player2.CurrentState == State.Special_Opportunity;

			bool AIvulnerable = player2.CurrentState == State.Counter_Recovery || player2.CurrentState == State.Special_Opportunity;
			
			bool AIvulnerableOnLastHit = lastHit && (player2.CurrentState == State.Counter_Attack || player2.CurrentState == State.Special_Extra || player2.CurrentState == State.Vengeance);

			bool shouldBlock = player1.IsIdle || player1.IsBlockIdle || player1.IsBlockStunned;		// keep blocking if already doing so

			if (player1.ExpiredHealth || player2.ExpiredHealth || !fightManager.ReadyToFight)
			{
				SetTrafficLightColour(TrafficLight.None);
			}
			else if (player1.CanSpecialExtraWater)
			{
				SetTrafficLightColour(TrafficLight.Right);
			}
			else if (player1.CanSpecialExtraFire || (!player1.IsHitStunned && (AIvulnerableOnLastHit || AIvulnerable)))
			{
				SetTrafficLightColour(TrafficLight.Green, 3);
			}
			else if (fighterCanCounter && AIapproaching)
			{
				SetTrafficLightColour(TrafficLight.Left);
			}
			else if (player1.CanRomanCancel && player1.HasRomanCancelGauge)
			{
				SetTrafficLightColour(TrafficLight.Yellow);
			}
			else if (shouldBlock && AIapproaching)
			{
				SetTrafficLightColour(TrafficLight.Red);
			}
			else if (player1.IsIdle)
			{
				SetTrafficLightColour(TrafficLight.Green);
			}
			else
			{
				SetTrafficLightColour(TrafficLight.None);
			}
		}
			
		private void OnStateStarted(FighterChangedData newState)
		{
			// traffic light colours forced during training (except during a combo)
			if (newState.Fighter.InTraining) // && !newState.Fighter.Trainer.CurrentStepIsCombo)
				return;

			SetStateTrafficLight(newState);
		}

		private void OnCanContinue(FighterChangedData newState)
		{
			// traffic lights forced during training, except during a combo
			if (newState.Fighter.InTraining) // && !newState.Fighter.Trainer.CurrentStepIsCombo)
				return;

			SetStateTrafficLight(newState);
		}

		private void OnGaugeChanged(FighterChangedData newState, bool stars)
		{
			if (newState.NewGauge > newState.OldGauge)
			{
				if (OnGaugeIncreased != null)
					OnGaugeIncreased(newState.NewGauge);		// info bubble
			}

			// traffic lights forced during training, except during a combo
			if (newState.Fighter.InTraining) // && !newState.Fighter.Trainer.CurrentStepIsCombo)
				return;
			
			SetStateTrafficLight(newState);
		}

		private void OnLastHit(FighterChangedData newState)
		{
			// traffic lights forced during training, except during a combo
			if (newState.Fighter.InTraining) // && !newState.Fighter.Trainer.CurrentStepIsCombo)
				return;

			SetStateTrafficLight(newState);
		}

		private void OnStartNewFight()
		{
			SetTrafficLightColour(TrafficLight.None);
		}

		private void OnReadyToFight(bool readyToFight, bool changed, FightMode fightMode)
		{
			if (readyToFight)
			{
				if (changed)
					SetTrafficLightColour(TrafficLight.Green);
			}
			else
				SetTrafficLightColour(TrafficLight.None);
		}

//		private void OnKnockOut(Fighter fighter)
//		{
//			SetTrafficLightColour(TrafficLight.None);
//		}


		private void EnableTrafficLights(bool enabled)
		{
			if (TrafficLightVisible == enabled)
				return;
			
			TrafficLightVisible = enabled;

			TrafficLights.SetActive(enabled);

			if (! enabled)
				TurnOffAllTrafficLights();
		}


		private void TurnOffAllTrafficLights()
		{
			UnlitTrafficLight.gameObject.SetActive(false);	
			RedTrafficLight.gameObject.SetActive(false);	
			YellowTrafficLight.gameObject.SetActive(false);	
			GreenTrafficLight.gameObject.SetActive(false);	
			LeftTrafficLight.gameObject.SetActive(false);			// green
			RightTrafficLight.gameObject.SetActive(false);			// green
		}

		private void OnTrafficLightEnabled()
		{
//			Debug.Log("OnTrafficLightEnabled" + ", trafficLightVisible = " + trafficLightVisible);
			if (TrafficLightVisible)	 			// no change - already enabled
				return;

			EnableTrafficLights(true);
		}

		private void SetTrafficLightColour(TrafficLight colour, int flashes = 0, bool stars = false)
		{
//			Debug.Log("SetTrafficLightColour " + colour + ", showBubble = " + showBubble);

			if (!TrafficLightVisible)		// eg. survival and challenge modes
				return;

			bool switchedOn = colour != TrafficLight.None && colour != currentTrafficLight;
			currentTrafficLight = colour;

			UnlitTrafficLight.gameObject.SetActive(colour == TrafficLight.None);
			RedTrafficLight.gameObject.SetActive(colour == TrafficLight.Red);
			YellowTrafficLight.gameObject.SetActive(colour == TrafficLight.Yellow);
			GreenTrafficLight.gameObject.SetActive(colour == TrafficLight.Green);
			LeftTrafficLight.gameObject.SetActive(colour == TrafficLight.Left);
			RightTrafficLight.gameObject.SetActive(colour == TrafficLight.Right);

			if (stars && switchedOn)
			{
				switch (colour)
				{
					case TrafficLight.Red:
						RedLightStars.Play();
						break;

					case TrafficLight.Yellow:
						YellowLightStars.Play();
						break;

					case TrafficLight.Green:
						GreenLightStars.Play();
						break;

					case TrafficLight.Left:
						GreenLightStars.Play();
						break;

					case TrafficLight.Right:
						GreenLightStars.Play();
						break;

					default:
						break;
				}
			}
				
			// don't show bubble if message for this light already read
			bool showBubble = switchedOn && ! FightManager.WasInfoBubbleMessageRead(TrafficLightMessage(colour, flashes > 0));	
				
			if (showBubble)
			{
				if (TrafficLightInfoBubble != null)
					TrafficLightInfoBubble(colour, flashes > 0);
			}

			if (flashes > 0 && colour != TrafficLight.None)
				StartCoroutine(FlashTrafficLight(colour, flashes));
		}


		public static InfoBubbleMessage TrafficLightMessage(TrafficLight colour, bool flashing)
		{
			switch (colour)
			{
				case TrafficLight.Red:
					return InfoBubbleMessage.RedLight;

				case TrafficLight.Yellow:
					return InfoBubbleMessage.YellowLight;

				case TrafficLight.Green:
					if (flashing)
					{
						return InfoBubbleMessage.FlashingGreenLight;

						// TODO: bubbleText = FightManager.Translate("specialExtraFireNarrative">Mash to follow-up your special attack!</string>
						//							bubbleImage = MashSprite;
					}
					else
					{
						return InfoBubbleMessage.GreenLight;
					}

				case TrafficLight.Left:
					return InfoBubbleMessage.LeftArrow;

				case TrafficLight.Right:
					return InfoBubbleMessage.RightArrow;

				default:
					return InfoBubbleMessage.None;
			}
		}

		private IEnumerator FlashTrafficLight(TrafficLight colour, int flashes)
		{
			if (colour == TrafficLight.None || flashes <= 0)
				yield break;
			
			for (int i = 0; i < flashes; i++)
			{
				// first turn light on
				SetTrafficLightColour(colour);
				yield return new WaitForSeconds(trafficLightFlashInterval);

				// turn off
				SetTrafficLightColour(TrafficLight.None);
				yield return new WaitForSeconds(trafficLightFlashInterval);
			}

			// finally turn light on
			SetTrafficLightColour(colour);
			yield return null;
		}

		#endregion

		private void DoSplat()
		{
//			Splat.gameObject.SetActive(true);

//			HidePaintStrokes();

			StartCoroutine(TriggerSplat());
//			var animator = GetComponent<Animator>();
//			animator.SetTrigger("EnterSplat");
		}

		private void HideSplat()
		{
//			Debug.Log("HideSplat");
//			Splat.gameObject.SetActive(false);

			StartCoroutine(FadeSplat());
		}

		private void DoPaintStroke(bool right)
		{
//			PaintStroke.transform.localScale = flip ? new Vector3(-1, 1, 1) : new Vector3(1, 1, 1);			// flipped to flow to the right
//			if (right)
//			{
//				PaintStrokeRight.gameObject.SetActive(true);
//				PaintStrokeLeft.gameObject.SetActive(false);
//			}
//			else
//			{
//				PaintStrokeLeft.gameObject.SetActive(true);
//				PaintStrokeRight.gameObject.SetActive(false);
//			}

//			HideSplat();
			StartCoroutine(TriggerPaintStroke(right));

//			var animator = GetComponent<Animator>();
//			animator.SetTrigger("EnterSplat");
		}

		private void HidePaintStrokes()
		{
//			Debug.Log("HidePaintStroke");
//			PaintStrokeLeft.gameObject.SetActive(false);
//			PaintStrokeRight.gameObject.SetActive(false);

			StartCoroutine(FadePaintStroke(true));
			StartCoroutine(FadePaintStroke(false));
		}
			
		private IEnumerator TriggerSplat()
		{
			float t = 0.0f;

//			if (splatSound != null)
//				AudioSource.PlayClipAtPoint(splatSound, Vector3.zero, FightManager.SFXVolume);

			HidePaintStrokes();

			Splat.color = splatMinColour;
			Splat.gameObject.SetActive(true);

			Splat.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(0.0f, 360.0f));
				
			var startScale = new Vector3(splatMinScale, splatMinScale, splatMinScale);
			var finishScale = new Vector3(splatMaxScale, splatMaxScale, splatMaxScale);

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / splatTime); 

				Splat.color = Color.Lerp(splatMinColour, splatMaxColour, t);
				Splat.transform.localScale = Vector3.Lerp(startScale, finishScale, t);

				yield return null;
			}

			yield return null;
		}

		private IEnumerator FadeSplat()
		{
			if (! Splat.gameObject.activeSelf)
				yield break;
			
			float t = 0.0f;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / splatTime); 

				Splat.color = Color.Lerp(splatMaxColour, splatMinColour, t);
				yield return null;
			}

			Splat.gameObject.SetActive(false);
			Splat.color = splatMaxColour;

			yield return null;
		}

		private IEnumerator TriggerPaintStroke(bool right)
		{
			float t = 0.0f;

//			if (splatSound != null)
//				AudioSource.PlayClipAtPoint(splatSound, Vector3.zero, FightManager.SFXVolume);

			var paintStroke = right ? PaintStrokeRight : PaintStrokeLeft;

			paintStroke.color = splatMinColour;
			paintStroke.gameObject.SetActive(true);

			StartCoroutine(FadePaintStroke(!right));
			HideSplat();

			var startScale = right ? new Vector3(-splatMinScale, splatMinScale, splatMinScale) : new Vector3(splatMinScale, splatMinScale, splatMinScale);			// flipped to flow to the right
			var finishScale = right ? new Vector3(-splatMaxScale, splatMaxScale, splatMaxScale) : new Vector3(splatMaxScale, splatMaxScale, splatMaxScale);			// flipped to flow to the right

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / splatTime); 

				paintStroke.color = Color.Lerp(splatMinColour, splatMaxColour, t);
				paintStroke.transform.localScale = Vector3.Lerp(startScale, finishScale, t);

				yield return null;
			}

			yield return null;
		}

		private IEnumerator FadePaintStroke(bool right)
		{
			var paintStroke = right ? PaintStrokeRight : PaintStrokeLeft;

			if (! paintStroke.gameObject.activeSelf)
				yield break;

			float t = 0.0f;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / splatTime); 

				paintStroke.color = Color.Lerp(splatMaxColour, splatMinColour, t);
				yield return null;
			}

			paintStroke.gameObject.SetActive(false);
			paintStroke.color = splatMaxColour;
			yield return null;
		}

		public void TriggerEntry()
		{
			var animator = GetComponent<Animator>();
			animator.SetTrigger("GameUIEnter");
		}

		private void CameraShake(int shakes)
		{
			StartCoroutine(cameraController.Shake(shakes));
		}

		public void OnEntryStart()
		{
//			FillGauge();
		}

			
		public void NameEntry()
		{
			if (NameEnterSound != null)
				AudioSource.PlayClipAtPoint(NameEnterSound, Vector3.zero, FightManager.SFXVolume);
		}

		public void NameArrive()
		{
			FillGauge();

			if (NameArriveSound != null)
				AudioSource.PlayClipAtPoint(NameArriveSound, Vector3.zero, FightManager.SFXVolume);
		}

		public void GaugeSlotEnter()
		{
			if (GaugeSlotEnterSound != null)
				AudioSource.PlayClipAtPoint(GaugeSlotEnterSound, Vector3.zero, FightManager.SFXVolume);
		}

		public void GaugeSlotArrive()
		{
			if (GaugeSlotArriveSound != null)
				AudioSource.PlayClipAtPoint(GaugeSlotArriveSound, Vector3.zero, FightManager.SFXVolume);
		}

		public void GaugeCrystalArrive()
		{
			if (GaugeCrystalArriveSound != null)
				AudioSource.PlayClipAtPoint(GaugeCrystalArriveSound, Vector3.zero, FightManager.SFXVolume);
		}

		public void OnEntryComplete()
		{
			fightManager.NewFightEntryComplete();
		}


		private void FillGauge()
		{
			fightManager.NewFightFillGauge();			// temporarily - for animation
		}

	}
}
