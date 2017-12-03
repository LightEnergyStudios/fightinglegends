
using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace FightingLegends
{
	public class Fighter : Animation
	{
		public string FighterName;					// as displayed in-game
		public string FullName { get { return FighterName + " " + ColourScheme + (UnderAI ? " [ AI ]" : ""); } }
		public string ColourScheme;					// P1, P2 or P3

		[HideInInspector]
		public bool UnderAI = false;				// is character operating under AI control?
		[HideInInspector]
		public bool InTraining = false;				// is character currently undergoing basic training?

		public GameObject ProfilePrefab;			// stats / experience / personality / skills (set in Inspector)
		protected Profile profile;					// script (contains ProfileData) - instantiated from prefab

		public ProfileData ProfileData
		{ 
			get
			{
				if (profile == null)
					Debug.Log(FullName + ": Fighter null profile!!");

				else if (profile.ProfileData == null)
					Debug.Log("Fighter null ProfileData!!");
				
				return profile != null ? profile.ProfileData : null;	
			}
		}
		public HitFrameSignature StateSignature { get { return profile != null ? profile.StateSignature : null; }}

		protected FightManager fightManager;
		protected FighterUI fighterUI;				// displays hit damage
		protected FeedbackUI feedbackUI;			// displays info bubble messages

		private List<FailedInput> FailedMoves = new List<FailedInput>();
		public const int MaxFailedInputs = 3;		// shows info bubble hint

		public bool IsFireElement { get { return profile.IsElement(FighterElement.Fire); }}
		public bool IsWaterElement { get { return profile.IsElement(FighterElement.Water); }}
		public bool IsEarthElement { get { return profile.IsElement(FighterElement.Earth); }}
		public bool IsAirElement { get { return profile.IsElement(FighterElement.Air); }}
	
		public float DistanceToOpponent { get { return Opponent == null ? 0 : Opponent.transform.position.x - transform.position.x; } }	// distance between fighters

		private float StrikeTravelTime { get { return ProfileData.StrikeTravelTime / fightManager.AnimationSpeed; } }
		private float RecoilTravelTime { get { return ProfileData.RecoilTravelTime / fightManager.AnimationSpeed; } }

		private const float fastExpiryFactor = 0.5f;	// for survival and challenge modes

		public bool Attacking { get; private set; }	// start of step in to end of opponent stepping back
		public bool Returning { get; private set; }	// returning to fighting distance after being attacked

		private bool returnToDefault = false;			// after hit freeze

		public bool HoldingBlock { get; private set; }		// true while finger held for block idle (if not in training or smart AI)

		public bool IncreasedGauge { get; private set; }	// last change in gauge was an increase

		public bool ArmourDown { get; private set; }		// take double damage while true
		public bool ArmourUp { get; private set; }			// take half damage while true
		public bool OnFire { get; private set; }			// health reduced each second while true
		public bool HealthUp { get; private set; }			// single health boost

		private const int StatusEffectFrames = 50;			// on fire, armour up, armour down, health up
//		private const int OKEffectFrames = 18;				// second life triggered
//		private const int HealthUpEffectFrames = 50;
		private const int KnockOutFreezeFrames = 30;
//		private int StatusEffectFramesRemaining = 0;
		private StatusEffect currentStatusEffect = StatusEffect.None;
		private int StatusEffectStartFrame = 0;

		private const float levelUpXPBase = 100.0f;				// XP required to increase from level 1 to 2
		public const int maxLevel = 100;

		private float damageWhileOnFire = 0;				// incremented each tick while OnFire

		private float gainCoinChance = 0.25f;				// for survival mode landed hit	(ie. 25% probability)

		public bool IsOnTop { get; private set; }			// higher priority
		private const string fighterLayer = "Fighters";		
		private const string onTopLayer = "FighterOnTop";	// 'switch camera' when on top
		private const string previewLayer = "Menu";			// fighter uses for preview

		private SpotFX spotFX;						// script (to trigger effect) - instantiated from prefab
		private SpotFX spotFXx2;					// script (to trigger effect) - instantiated from prefab at double scale
		private ElementsFX elementsFX;				// script (to trigger effect) - instantiated from prefab
		private SmokeFX smokeFX;					// script (to trigger effect) - instantiated from prefab

		private const float spotFXOffsetX = 300;
		private const float spotFXOffsetY = -50;
		private const float spotFXOffsetZ = -50;

		private const float spotFXx2OffsetX = -250;
		private const float spotFXx2OffsetY = 950;
		private const float spotFXx2OffsetZ = -50;

		private const float elementsFXOffsetX = 0;
		private const float elementsFXOffsetY = 0;
		private const float elementsFXOffsetZ = -20;

		private const float smokeFXOffsetX = 0;
		private const float smokeFXOffsetY = 0;
		private const float smokeFXOffsetZ = -20;

		private const float fireOffsetX = 395;
		private const float fireOffsetY = -500;
		private const float fireOffsetZ = 0;

		private const float fighterUIOffsetX = -200;
		private const float fighterUIOffsetY = -600;

		private const float feedbackOffsetX = -260;			// for positioning next to fighters
		private const float feedbackSwipeOffsetX = 70;		// nearer to centre
		private const float feedbackOffsetY = 20; //-20;	insertcoin// armour up/down, on fire, health up

		#region camera tracking

		private Queue<float> trackingPositions;			// last x coordinates
		private const float trackOffset = 300.0f;		// off-centre position for camera tracking a fighter

		public float TrackPosition
		{
			get { return transform.position.x + trackOffset; }
		}

		public float LagPosition
		{
			get
			{
				if (trackingPositions == null)
					return 0;
				
				if (trackingPositions.Count == 0)
					return TrackPosition;

				// first in (limited length) queue is the oldest
				return trackingPositions.Peek() + (IsPlayer1 ? trackOffset : -trackOffset);
			}
		}

		#endregion

		#region properties used by UI

		public bool MoveOk {get; protected set; }		// for StatusUI canvas
		public string DebugUI { get; protected set; }	// for StatusUI canvas

		[HideInInspector]
		public string StateUI;							// for StatusUI canvas
		 
		[HideInInspector]
		public string NextMoveUI;						// for StatusUI canvas

		private IEnumerator hitFlashCoroutine;			// so it can be interrupted
		private IEnumerator colourFlashCoroutine;		// so it can be interrupted

		private Color colourFlashColour = Color.white;
		private float powerUpFlashTime = 0.75f;
		private float levelUpFlashTime = 0.25f;

		#endregion 		// properties used by UI


		#region combos and chaining flags

		// combo and chaining extra flags
		public bool comboTriggered {get; private set; } 		// when ComboPossible
		public bool chainedCounter {get; private set; } 		// when ChainPossible
		public bool chainedSpecial {get; private set; } 		// when ChainPossible
		public bool specialExtraTriggered {get; private set; } 	// during special opportunity

		private bool specialExtraPerformed = false; 
		private bool comboInProgress = false;
		private bool chainInProgress = false;

//		private bool autoCombo = false;			// used when in training to show l-m-h

		#endregion


		#region state frame data

		// hit frame dictionary keyed by state + frame number
		// for fast lookup of hit frames, can queue, FX, etc.
		private Dictionary<int, HitFrameData> hitFrameDictionary;

		private int hitStunFramesRemaining = 0;		// counts down from HitData.StunFrames to zero
		private int blockStunFramesRemaining = 0;	// counts down from HitData.StunFrames to zero

		private bool counterTriggerStun = false;	// for deferral of stun till following frame

		private const int expiryFreezeFrames = 10;		// will unfreeze at end of KO feedback

		private int hitComboCount = 0;
		public int HitComboCount
		{
			get { return hitComboCount; }
			private set
			{
				if (hitComboCount == value)
					return;

//				if (value == 0)
//					Debug.Log(FullName + " HitComboCount reset!" + " [" + AnimationFrameCount + "]");

				hitComboCount = value;

				if (OnComboCountChanged != null)
					OnComboCountChanged(hitComboCount);
			}
		}	

		public bool isFrozen { get; private set; }
		public bool frozenByInfoBubble { get; private set; }

		private int romanCancelFreezeFramesRemaining = 0;
//		public bool romanCancelFrozen { get { return romanCancelFreezeFramesRemaining > 0; } }
		public bool romanCancelFrozen { get; private set; }

		private int powerUpFreezeFramesRemaining = 0;
//		public bool powerUpFrozen { get { return powerUpFreezeFramesRemaining > 0; } }
		public bool powerUpFrozen { get; private set; }

		private int freezeFightFrames = 0;			// for freeze of both fighters, deferred to frame following a hit

		private const int levelUpFreezeFrames = 30;

		private float recoilTravelDistance;			// after taking a hit, deferred till after freeze

		public int defaultCameraShakes = 2;			// on each hit
		public float defaultShakeDistance = 1;		// on each hit

		private HitFlash hitFlash;

		#endregion

		// flags used for AI triggers
		private int idleFrameCount = 0;			
		private int blockIdleFrameCount = 0;	
		private int canContinueFrameCount = 0;	
		private int vengeanceFrameCount = 0;				// frames since start of vengeance
		private int gaugeIncreaseFrameCount = 0;			// frames since an increase in gauge
		private int stunnedFrameCount = 0;					// frames since start of hit / block / shove stun
		private int lastHitFrameCount = 0;					// frames since a last hit - reset to zero at end of state

		private bool takenLastHit = false;					// from last hit frame to end of state
		public bool takenLastFatalHit { get; protected set; }	// taken last of fatal sequence of blows

		public AIController AIController { get; private set; }	
		public Trainer Trainer { get; private set; }	
		private Animator textureAnimator;
		private CameraController cameraController;

//		public float HealthPercent { get { return ProfileData.Health / ProfileData.InitialHealth; } }
		public float HealthLost { get { return ProfileData.LevelHealth - ProfileData.SavedData.Health; } }
//		private float damageSustained = 0;					// reset when sufficient to trip gauge

		public const int maxGauge = 4;

		private float hitStringDamage = 0.0f;				// total damage of hits including last hit
		private int specialOpportunityTapCount = 0;
		private const int specialExtraTaps = 2; // 3;		// taps during special opportunity required to trigger special extra (for fire characters)

		public const int Default_Priority = 0;				// also shove
		public const int Punishable_Priority = 1;			// on last hit (not LMH)
		public const int Windup_Light_Priority = 2;			// windup
		public const int Strike_Light_Priority = 3;			// hit frame
		public const int Windup_Medium_Priority = 4;		// windup
		public const int Strike_Medium_Priority = 5;		// hit frame
		public const int Windup_Heavy_Priority = 6;			// windup
		public const int Strike_Heavy_Priority = 7;			// hit frame
		public const int Special_Start_Priority = 8;		// until first hit
		public const int Special_Hit_Priority = 9;			// first hit, until last hit
		public const int Special_Opportunity_Priority = 9;	// entire state
		public const int Special_Extra_Priority = 10;		// until last hit
		public const int Vengeance_Windup_Priority = 11;	// until first hit
		public const int Vengeance_Hit_Priority = 12;		// first hit, until last hit
		public const int Counter_Priority = 20;				// counter trigger. attack retains existing priority until last hit. recover returns to default
		public const int Power_Attack_Priority = 25;		// triggered by power-up
		public const int Tutorial_Punch_Start_Priority = 13; // 8;	// punch start and punch. punch end returns to default
		public const int Tutorial_Punch_Priority = 12; // 9;		// first and only hit frame


		#region event delegates

		public delegate void RoundWonDelegate(int roundsWon);
		public RoundWonDelegate OnScoreChanged;

		public delegate void UpdateHealthDelegate(float damage, bool updateGauge);
		public UpdateHealthDelegate OnUpdateHealth;

		public delegate void HealthChangedDelegate(FighterChangedData newState);
		public HealthChangedDelegate OnHealthChanged;

		public delegate void GaugeChangedDelegate(FighterChangedData newState, bool stars);
		public GaugeChangedDelegate OnGaugeChanged;

		public delegate void MoveExecutedDelegate(FighterChangedData newState, bool continuing);
		public MoveExecutedDelegate OnMoveExecuted;

		public delegate void ContinuationCuedDelegate(Move move);
		public ContinuationCuedDelegate OnContinuationCued;

		public delegate void ComboTriggeredDelegate(FighterChangedData newState); 			// 'combo' = (light)-medium-heavy strike
		public ComboTriggeredDelegate OnComboTriggered;

		public delegate void ChainCounterDelegate(FighterChangedData newState);				// 'chain' = counter from medium/heavy strike
		public ChainCounterDelegate OnChainCounter;

		public delegate void ChainSpecialDelegate(FighterChangedData newState);				// 'chain' = special from medium/heavy strike
		public ChainSpecialDelegate OnChainSpecial;

		public delegate void SpecialExtraTriggeredDelegate(FighterChangedData newState);
		public SpecialExtraTriggeredDelegate OnSpecialExtraTriggered;

		public delegate void MoveCompletedDelegate(FighterChangedData newState);
		public MoveCompletedDelegate OnMoveCompleted;

		public delegate void StateStartedDelegate(FighterChangedData newState);
		public StateStartedDelegate OnStateStarted;

		public delegate void StateEndedDelegate(FighterChangedData oldState);
		public StateEndedDelegate OnStateEnded;

		public delegate void PriorityChangedDelegate(FighterChangedData newState);
		public PriorityChangedDelegate OnPriorityChanged;

		public delegate void LastHitDelegate(FighterChangedData newState);
		public LastHitDelegate OnLastHit;

//		public delegate void HitDelegate(FighterChangedData newState);
//		public HitDelegate OnHit;

		public delegate void HitStunDelegate(FighterChangedData newState);
		public HitStunDelegate OnHitStun;

		public delegate void ShoveStunDelegate(FighterChangedData newState);
		public ShoveStunDelegate OnShoveStun;

		public delegate void BlockStunDelegate(FighterChangedData newState);
		public BlockStunDelegate OnBlockStun;

		public delegate void RomanCancelDelegate(FighterChangedData newState);
		public LastHitDelegate OnRomanCancel;

		public delegate void EndRomanCancelFreezeDelegate(Fighter fighter);
		public EndRomanCancelFreezeDelegate OnEndRomanCancelFreeze;

		public delegate void EndPowerUpFreezeDelegate(Fighter fighter, bool fromIdle);
		public EndPowerUpFreezeDelegate OnEndPowerUpFreeze;

		public delegate void CanContinueDelegate(FighterChangedData newState);
		public CanContinueDelegate OnCanContinue;

		public delegate void IdleFrameDelegate(FighterChangedData newState);
		public IdleFrameDelegate OnIdleFrame;

		public delegate void BlockIdleFrameDelegate(FighterChangedData newState);
		public BlockIdleFrameDelegate OnBlockIdleFrame;

		public delegate void CanContinueFrameDelegate(FighterChangedData newState);
		public CanContinueFrameDelegate OnCanContinueFrame;

		public delegate void VengeanceFrameDelegate(FighterChangedData newState);
		public VengeanceFrameDelegate OnVengeanceFrame;

		public delegate void GaugeIncreasedFrameDelegate(FighterChangedData newState);
		public GaugeIncreasedFrameDelegate OnGaugeIncreasedFrame;

		public delegate void StunnedFrameDelegate(FighterChangedData newState);
		public StunnedFrameDelegate OnStunnedFrame;

		public delegate void LastHitFrameDelegate(FighterChangedData newState);
		public LastHitFrameDelegate OnLastHitFrame;

		public delegate void ComboCountDelegate(int comboCount);
		public ComboCountDelegate OnComboCountChanged;

		public delegate void KnockOutFreezeDelegate(Fighter fighter);	// start of KO freeze (second life opportunity)
		public KnockOutFreezeDelegate OnKnockOutFreeze;

		public delegate void KnockOutDelegate(Fighter fighter);	
		public KnockOutDelegate OnKnockOut;

		public delegate void DamageInflictedDelegate(float damage);
		public DamageInflictedDelegate OnDamageInflicted;

		public delegate void XPChangedDelegate(float xp);
		public XPChangedDelegate OnXPChanged;

		public delegate void XPLevelDelegate(int level);
		public XPLevelDelegate OnLevelChanged;

		public delegate void LockedDelegate(Fighter fighter, bool locked);
		public LockedDelegate OnLockedChanged;

		public delegate void TriggerPowerUpChangedDelegate(PowerUp powerUp);
		public TriggerPowerUpChangedDelegate OnTriggerPowerUpChanged;

		public delegate void PowerUpTriggeredDelegate(Fighter fighter, PowerUp powerUp, bool fromIdle);
		public PowerUpTriggeredDelegate OnPowerUpTriggered;

		public delegate void StaticPowerUpChangedDelegate(PowerUp powerUp);
		public StaticPowerUpChangedDelegate OnStaticPowerUpChanged;

		public delegate void StaticPowerUpAppliedDelegate(PowerUp powerUp);
		public StaticPowerUpAppliedDelegate OnStaticPowerUpApplied;

		public delegate void ExecuteMoveOkDelegate(Move move, bool ok);
		public ExecuteMoveOkDelegate OnExecuteMoveOk;


		#endregion 		// event delegates


		#region fighter select scene

		public bool PreviewIdle { get; private set; }		// idle only - for preview in fighter select menus

		#endregion 		// fighter select scene


		// 'Constructor'
		// NOT called when returning from background
		private void Awake()
		{
			var fightManagerObject = GameObject.Find("FightManager");
			fightManager = fightManagerObject.GetComponent<FightManager>();

			var fighterUIObject = GameObject.Find("FighterUI");
			if (fighterUIObject != null)
				fighterUI = fighterUIObject.GetComponent<FighterUI>();

//			var feedbackUIObject = GameObject.Find("FeedbackUI");
//			if (feedbackUIObject != null)
//				feedbackUI = fighterUIObject.GetComponent<FeedbackUI>();

			AIController = GetComponent<AIController>();
			Trainer = GetComponent<Trainer>();
			textureAnimator = GetComponent<Animator>();

			cameraController = Camera.main.GetComponent<CameraController>();

			InitAnimation();			// base class initialisation (frame labels, etc)

			if (ProfilePrefab != null)
			{
				var profileObject = Instantiate(ProfilePrefab, Vector3.zero, Quaternion.identity) as GameObject;
				profile = profileObject.GetComponent<Profile>();

				// make profile a child of the fighter
				profileObject.transform.parent = transform;
			}

			if (profile.SpotFXPrefab != null)
			{
				var spotFXObject = Instantiate(profile.SpotFXPrefab, Vector3.zero, Quaternion.identity) as GameObject;
				spotFX = spotFXObject.GetComponent<SpotFX>();
				spotFX.OnEndState += FXStateEnd;			// listening for feedback state ends

				// make spotFX a child of the fighter
				spotFXObject.transform.parent = transform;

				// adjust position of spotFX (relative to fighter)
				spotFXObject.transform.localPosition = new Vector3(spotFXOffsetX, spotFXOffsetY, spotFXOffsetZ);

				// spotFX scale for player2 needs to be reversed to fire from right to left
				// bit weird but prolly something to do with double negatives ... or something
				if (IsPlayer1)
					spotFXObject.transform.localScale = new Vector3(-1, 1, 1);


				// double-scale spot FX (eg. roman cancel lightning)

				var spotFXx2Object = Instantiate(profile.SpotFXPrefab, Vector3.zero, Quaternion.identity) as GameObject;
				spotFXx2 = spotFXx2Object.GetComponent<SpotFX>();
				spotFXx2.OnEndState += FXStateEnd;			// listening for feedback state ends

				spotFXx2.transform.localScale = new Vector3(4, 4, 4);

				// make spotFXx2 a child of the fighter
				spotFXx2Object.transform.parent = transform;

				// adjust position of spotFXx2 (relative to fighter)
				spotFXx2Object.transform.localPosition = new Vector3(spotFXx2OffsetX, spotFXx2OffsetY, spotFXx2OffsetZ);
			}

			if (profile.ElementsFXPrefab != null)
			{
				var elementsFXObject = Instantiate(profile.ElementsFXPrefab, Vector3.zero, Quaternion.identity) as GameObject;
				elementsFX = elementsFXObject.GetComponent<ElementsFX>();
				elementsFX.OnEndState += FXStateEnd;			// listening for feedback state ends

				// make elementsFXObject a child of the fighter
				elementsFXObject.transform.parent = transform;

				// adjust position of elementsFXObject (relative to fighter)
				elementsFXObject.transform.localPosition = new Vector3(elementsFXOffsetX, elementsFXOffsetY, elementsFXOffsetZ);

				// elementsFX scale for player2 needs to be reversed to fire from right to left
				// bit weird but prolly something to do with double negatives ... or something
				if (IsPlayer1)
					elementsFXObject.transform.localScale = new Vector3(-1, 1, 1);
			}

			if (profile.SmokeFXPrefab != null)
			{
				var smokeFXObject = Instantiate(profile.SmokeFXPrefab, Vector3.zero, Quaternion.identity) as GameObject;
				smokeFX = smokeFXObject.GetComponent<SmokeFX>();
				smokeFX.OnEndState += FXStateEnd;			// listening for feedback state ends

				// make smokeFXObject a child of the fighter
				smokeFXObject.transform.parent = transform;

				// adjust position of smokeFXObject (relative to fighter)
				smokeFXObject.transform.localPosition = new Vector3(smokeFXOffsetX, smokeFXOffsetY, smokeFXOffsetZ);

				// smokeFX scale for player2 needs to be reversed to fire from right to left
				// bit weird but prolly something to do with double negatives ... or something
				if (IsPlayer1)
					smokeFXObject.transform.localScale = new Vector3(-1, 1, 1);
			}

			var hitFlashObject = GameObject.Find("HitFlash");
			if (hitFlashObject != null)
				hitFlash = hitFlashObject.GetComponent<HitFlash>();

			BuildStateDictionary();	// for fast lookup of state frames (hits, can queue, FX, etc)

			trackingPositions = new Queue<float>();
		}
			

		// Initialization
		// NOT called when returning from background
		void Start()
		{
			if (!PreviewIdle)
			{
				MovieClipFrame = 0;
//				Debug.Log(FullName + ": Start MovieClipFrame = " + MovieClipFrame + ", StateLabel " + currentAnimation.StateLabel + ", StateLength " + currentAnimation.StateLength);
				SetToFighterLayer();

//				LevelXPTest();
//				LevelDamageTest();
			}
			else
			{
				SetToPreviewLayer();
			}

			CurrentState = StartingState;
			CurrentPriority = Default_Priority;
			CanContinue = false;
		}


		public override bool StateLoops(string stateLabel)
		{
			return (stateLabel == "IDLE" ||
					stateLabel == "BLOCK_IDLE" ||
					stateLabel == "IDLE_DAMAGED" ||
					stateLabel == "READY_TO_DIE" ||
					stateLabel == "DEFAULT");
		}
			

		// AI fighter needs to watch opponent and own health / gauge
		public void StartWatching()
		{
			if (UnderAI && AIController != null)
				AIController.StartWatching();  // opponent and self
		}

		public void StopWatching()
		{
			if (UnderAI && AIController != null)
				AIController.StopWatching();	// opponent and self
		}

		private void OnDestroy()
		{
			// unsubscribe from touch events
			if (! PreviewIdle)
				StopListeningForInput();

			// destroy instantiated children

			spotFX.OnEndState -= FXStateEnd;
			spotFXx2.OnEndState -= FXStateEnd;
			elementsFX.OnEndState -= FXStateEnd;
			smokeFX.OnEndState -= FXStateEnd;

			Destroy(spotFX.gameObject);
			Destroy(spotFXx2.gameObject);
			Destroy(elementsFX.gameObject);
			Destroy(smokeFX.gameObject);
			Destroy(profile.gameObject);
		}

		public void StartListeningForInput()
		{
//			Debug.Log(FullName + ": StartListeningForInput");
			if (FightManager.IsNetworkFight && FightManager.CombatMode == FightMode.Arcade)		// handled by FighterController
			{
				FightManager.OnNetworkReadyToFight += OnNetworkReadyToFight;
				return;
			}
			
			// subscribe to touch events
			if (UnderAI || IsDojoShadow)
				return;
			
			GestureListener.OnTap += SingleFingerTap;				// strike		
			GestureListener.OnHoldStart += HoldDown;				// start block	
			GestureListener.OnHoldEnd += HoldRelease;				// end block
			GestureListener.OnSwipeLeft += SwipeLeft;				// counter
			GestureListener.OnSwipeRight += SwipeRight;				// special
			GestureListener.OnSwipeLeftRight += SwipeLeftRight;		// vengeance
			GestureListener.OnSwipeDown += SwipeDown;				// shove
			GestureListener.OnSwipeUp += SwipeUp;					// power up

			GestureListener.OnTwoFingerTap += TwoFingerTap;			// roman cancel		

			GestureListener.OnFingerTouch += FingerTouch;			// reset moveCuedOk
			GestureListener.OnFingerRelease += FingerRelease;		// to ensure block released

			TrainingUI.OnInfoBubble += InterruptFightBubble;
			Trainer.OnFailedInput += OnLogFailedInput;

			FightManager.OnReadyToFight += OnReadyToFight;
		}

		private void StopListeningForInput()
		{
//			Debug.Log(FullName + ": StopListeningForInput");
			if (FightManager.IsNetworkFight && FightManager.CombatMode == FightMode.Arcade)		// handled by FighterController
			{
				FightManager.OnNetworkReadyToFight -= OnNetworkReadyToFight;
				return;
			}
			
			if (UnderAI || IsDojoShadow)
				return;
			
			// unsubscribe from touch events
			GestureListener.OnTap -= SingleFingerTap;		

			GestureListener.OnHoldStart -= HoldDown;		
			GestureListener.OnHoldEnd -= HoldRelease;		
			GestureListener.OnSwipeLeft -= SwipeLeft;
			GestureListener.OnSwipeRight -= SwipeRight;
			GestureListener.OnSwipeLeftRight -= SwipeLeftRight;
			GestureListener.OnSwipeDown -= SwipeDown;
			GestureListener.OnSwipeUp -= SwipeUp;

			GestureListener.OnTwoFingerTap -= TwoFingerTap;	

			GestureListener.OnFingerTouch -= FingerTouch;			
			GestureListener.OnFingerRelease -= FingerRelease;

			TrainingUI.OnInfoBubble -= InterruptFightBubble;
			Trainer.OnFailedInput -= OnLogFailedInput;

			FightManager.OnReadyToFight -= OnReadyToFight;
		}

		#region animation

		// CurrentState used to lookup state frame data for hits, state end, can queue, etc
		protected override string CurrentFrameLabel { get { return CurrentState.ToString().ToUpper(); } } 	// to match movieclip frame labels

		[HideInInspector]
		public State StartingState = State.Idle;

//		protected bool nextHitWillKO = false;		// skeletron ready to die state while health == 0

		private State currentState = State.Idle;
		public State CurrentState
		{
			get { return currentState; }

			set
			{
				// don't return if no change in state as usual -
				// might want to restart the current state (eg. hit stun)
				bool stateChanged = currentState != value;
				var newState = new FighterChangedData(this); 		// snapshot before changed

				currentState = value;

				if (movieClipStates == null)	// may not have been initialised yet
				{
					Debug.Log(FullName + ": movieClipStates == null!!");
					return;
				}

				currentAnimation = LookupCurrentAnimation;

				if (currentAnimation != null)
				{
					if (!PreviewIdle)
					{
						MovieClipFrame = currentAnimation.FirstFrame;
//						Debug.Log(FullName + ": CurrentState MovieClipFrame = " + MovieClipFrame + ", StateLabel " + currentAnimation.StateLabel + ", StateLength " + currentAnimation.StateLength);
					}

					if (stateChanged && OnStateStarted != null)
					{
						newState.StartedState(currentState, CanChain, CanCombo, CanSpecialExtra);

//						Debug.Log(FighterFullName + ": OnStateStarted called: " + newState.Fighter.FighterFullName + " -> " + newState.State);
						OnStateStarted(newState);
					}

					if (stateChanged)
					{
						StateFeedback(currentState);
					}
				}
				else
					Debug.Log(FullName + ": CurrentState KEY NOT FOUND: " + CurrentFrameLabel);
			}
		}
			
		#endregion


		#region moves

		public int MoveFrameCount { get; private set; }		// frame count for move (eg. strike, special, etc)
		public int StateFrameCount { get; private set; }	// frame count for each state (eg. windup, hit, recovery, cutoff)
		public int HitFrameCount { get; private set; }	// frame count for each state, reset on each hit

		// TODO: reinstate? private const int AIStateFrameTimeout = 90;			// 6 seconds at 15 FPS
		private const int AIHitFrameTimeout = 30; //25;	

		[HideInInspector]
		public Move CurrentMove = Move.Idle;

		public int AnimationFrameCount { get { return fightManager.AnimationFrameCount; } }

		private int FightFrameCount = 0;

		private int currentPriority = Default_Priority;
		public int CurrentPriority
		{
			get { return currentPriority; }

			set
			{
				if (value == currentPriority)
					return; 		// no change

				bool increased = value > currentPriority;

				var stateChanged = new FighterChangedData(this); 		// snapshot before changed
				currentPriority = value;

				if (OnPriorityChanged != null)
				{
					stateChanged.ChangedPriority(CurrentPriority);
					OnPriorityChanged(stateChanged);
				}
					
				// higher priority fighter drawn on top
				if (Opponent != null && CurrentPriority > Opponent.CurrentPriority)
					OnTopOfOpponent();

				// more kudos for higher priority states
				if (IsPlayer1 && increased)
					FightManager.IncreaseKudos(CurrentPriority);
			}
		}
			

		public bool CanPowerUp { get { return IsIdle || CanContinue; } }
		private bool powerUpTriggered = false;

		private bool CanTriggerPowerUp
		{
			get
			{
//				Debug.Log(FullName + ": CanTriggerPowerUp - powerUpTriggered: " + powerUpTriggered + ", CanPowerUp = " + CanPowerUp);
				if (ExpiredHealth && TriggerPowerUp != FightingLegends.PowerUp.SecondLife)
					return false;

				if (fightManager.PowerUpFeedbackActive)
					return false;
				
				if (powerUpTriggered)
					return false;

				if (TriggerCoolingDown)		// countdown
					return false;

				if (FightManager.CombatMode == FightMode.Dojo)		// ignores assigned power-ups - dummy power-up
					return CanPowerUp;		// idle or can continue
				
				switch (TriggerPowerUp)
				{
					case FightingLegends.PowerUp.None:
						return false;

					case FightingLegends.PowerUp.SecondLife:
						return CanSecondLife;

					case FightingLegends.PowerUp.Ignite:
						return CanPowerUp && Opponent != null && ! Opponent.OnFire;

					case FightingLegends.PowerUp.HealthBooster:			// health booster only accessible once health has fallen below a threshold
						var healthLevel = UnderAI ? ProfileData.AIHealthBoostLevel : ProfileData.InitialHealth;
						return CanPowerUp && ProfileData.SavedData.Health < healthLevel;

					case FightingLegends.PowerUp.PowerAttack:
						return CanPowerUp;

					case FightingLegends.PowerUp.VengeanceBooster:		// vengeance booster only accessible once gauge has fallen below a threshold
						var gaugeLevel = UnderAI ?  ProfileData.AIVengeanceBoostLevel : maxGauge;
						return CanPowerUp && ProfileData.SavedData.Gauge < gaugeLevel;

					default:
						return CanPowerUp;
				}
			}
		}

		public bool performingPowerAttack { get; private set; }			// powerup

		[HideInInspector]
		public bool TriggerCoolingDown = false;		// trigger power-up

		private bool secondLifeOpportunity = false;
		private bool secondLifeTriggered = false;
		public bool CanSecondLife { get { return secondLifeOpportunity && !secondLifeTriggered; } }


		public PowerUp TriggerPowerUp
		{
			get { return ProfileData.SavedData.TriggerPowerUp; }

			set
			{
				if (value == ProfileData.SavedData.TriggerPowerUp)
					return;

				ProfileData.SavedData.TriggerPowerUp = value;

				if (OnTriggerPowerUpChanged != null)
					OnTriggerPowerUpChanged(TriggerPowerUp);
			}
		}

		public PowerUp StaticPowerUp
		{
			get { return ProfileData.SavedData.StaticPowerUp; }

			set
			{
				if (value == ProfileData.SavedData.StaticPowerUp)
					return;
				
				ProfileData.SavedData.StaticPowerUp = value;

				if (OnStaticPowerUpChanged != null)
					OnStaticPowerUpChanged(StaticPowerUp);
			}
		}
			
			
		// a cued move is executed on next animation frame
		private Move cuedMove = Move.None;	
		private bool HasCuedMove { get { return cuedMove != Move.None; } }

		private bool moveCuedOk = false;		// for red / green sparks feedback

		private bool canContinue = false;
		public bool CanContinue
		{ 	
			get { return canContinue; }
			set
			{	
				if (canContinue == value)			// no change
					return;

				canContinue = value;

				if (OnCanContinue != null)
				{
					FighterChangedData stateData = new FighterChangedData(this);
					stateData.CanContinue(CanContinue);
					OnCanContinue(stateData);
				};
			}
		}

		// a move continuation is executed at CompleteMove() instead of returning to idle
		private Queue<Move> MoveContinuations = new Queue<Move>();		// TODO: replace queue with single move (like cuedMove)
		private Move NextContinuation { get { return MoveContinuations.Count > 0 ? MoveContinuations.Peek() : Move.None; }}
		public bool HasContinuation { get { return NextContinuation != Move.None; } }

		public string CuedUI
		{
			get
			{
				string cuedUI = "";

//				if (CanContinue && ! HasContinuation)
//					return "Continuation Possible!";

				if (HasContinuation)
					cuedUI = "Continue: " + NextContinuation.ToString() + " ";

				if (cuedMove != Move.None)
					cuedUI += "Move: " + cuedMove.ToString();
					
				return cuedUI == "" ? "Nothing cued" : cuedUI;
			}
		}

		public void CueContinuation(Move move)
		{
			if (move == Move.None || move == Move.Idle)
				return;

			ClearFailedMoves();

			MoveContinuations.Clear();
			MoveContinuations.Enqueue(move);

			if (UnderAI && move == Move.Block)
				HoldingBlock = true;

//			if (UnderAI)
//				Debug.Log(FullName + ": CueContinuation: " + move + ", CurrentState = " + CurrentState + " [" + AnimationFrameCount + "]");

			if (! UnderAI && ! IsDojoShadow)
			{
				moveCuedOk = true;
				fightManager.MoveCuedFeedback(moveCuedOk);
//				Debug.Log(FullName + ": CueContinuation: " + move + ", CurrentState = " + CurrentState + " [" + AnimationFrameCount + "]");

				if (OnContinuationCued != null)
					OnContinuationCued(move);
			}
		}

		private void ExecuteContinuation()
		{
			if (HasContinuation)	
			{
//				Debug.Log(FullName + ": ExecuteContinuation " + NextContinuation);
				ExecuteMove(NextContinuation, true);
			}
		}

		private void ExecuteCuedMove()
		{
			if (HasCuedMove)	
				ExecuteMove(cuedMove, false);
		}
			
		public bool IsIdle { get { return (CurrentState == State.Idle || CurrentState == State.Idle_Damaged) && !ExpiredState; } }
		public bool IsBlockIdle { get { return CurrentState == State.Block_Idle; } }
		public bool IsBlocking { get { return IsBlockIdle || IsBlockStunned; } }
		public bool IsDashing { get { return CurrentState == State.Dash; } }

		public bool CanChain  		// special or counter chain possible
		{
			get
			{
				return (CurrentState == State.Medium_HitFrame || CurrentState == State.Medium_Recovery ||
					CurrentState == State.Heavy_HitFrame || CurrentState == State.Heavy_Recovery) && CurrentMove != Move.Power_Attack;
			}
		}

		private bool Comboed
		{
			get { return comboTriggered || comboInProgress; }
		}
			
		private bool Chained
		{
			get { return chainedCounter || chainedSpecial || chainInProgress; }
		}

		public bool CanStrikeLight
		{
			get { return IsIdle; }
		}

		public bool CanStrikeMedium		// hit combo possible (light -> medium)
		{
			get { return CurrentState == State.Light_HitFrame || CurrentState == State.Light_Recovery; }
		}

		public bool CanStrikeHeavy		// hit combo possible (medium -> heavy)
		{
			get { return CurrentState == State.Medium_HitFrame || CurrentState == State.Medium_Recovery; }
		}

		public bool CanCombo		// hit combo possible (light -> medium -> heavy)
		{
			get { return CanStrikeMedium || CanStrikeHeavy; }			
		}

		public bool CanSpecial
		{
			get { return IsIdle || CanChain || CanSpecialExtra; }
		}

		public virtual bool HasSpecialExtra
		{
			get { return true; }
		}

		public bool CanSpecialExtra
		{
			get { return CurrentState == State.Special_Opportunity && !specialExtraTriggered; }
		}

		public bool CanSpecialExtraWater
		{
			get { return CanSpecialExtra && IsWaterElement; }
		}

		public bool CanSpecialExtraFire
		{
			get { return CanSpecialExtra && IsFireElement; } 
		}


		public virtual bool HasShove
		{
			get { return true; }
		}

		public bool CanShove
		{
			get { return IsIdle; }
		}

		protected virtual bool CanBeShoved
		{
			get { return true; }
		}


		public bool HasBlock
		{
			get { return true; }
		}

		public bool CanBlock  
		{
			get { return IsIdle; }
		}

		public bool CanReleaseBlock  
		{
			get { return IsBlockIdle || HoldingBlock; }
		}

		public bool HasCounterGauge
		{
			get { return ProfileData.SavedData.Gauge >= ProfileData.CounterGauge; }	
		}

		public virtual bool HasCounter 
		{
			get { return true; }
		}

		public bool CanCounter 
		{
			get { return IsIdle || CanChain; } 			// deliberately not checking gauge here!
		}
			
		public bool HasVengeanceGauge 
		{
			get { return ProfileData.SavedData.Gauge >= ProfileData.VengeanceGauge; }	
		}
			
		public bool CanVengeance 
		{
			get { return IsIdle || IsBlocking; } 		// deliberately not checking gauge here!
		}

		public bool HasRomanCancelGauge
		{
			get { return ProfileData.SavedData.Gauge >= ProfileData.RomanCancelGauge; }
		}

		public bool CanRomanCancel
		{
			get { return !IsIdle; } 			// deliberately not checking gauge here!
		}
			

		public bool IsStunned { get { return IsHitStunned || IsShoveStunned || IsBlockStunned; } }

		public bool IsHitStunned
		{
			get { return CurrentState == State.Hit_Stun_Hook || CurrentState == State.Hit_Stun_Mid ||
				CurrentState == State.Hit_Stun_Straight || CurrentState == State.Hit_Stun_Uppercut; }
		}
		public bool IsShoveStunned { get { return CurrentState == State.Shove_Stun; } }
		public bool IsBlockStunned { get { return CurrentState == State.Block_Stun; } }


		public virtual bool ExpiredState
		{
			get { return CurrentState == State.Hit_Hook_Die || CurrentState == State.Hit_Mid_Die ||
							CurrentState == State.Hit_Straight_Die || CurrentState == State.Hit_Uppercut_Die; }
		}
			
		protected virtual bool FallenState { get { return false; }}	// skeletron only
			
		public bool ExpiredHealth { get { return ProfileData.SavedData.Health <= 0; } }

		protected virtual bool TravelOnExpiry { get { return true; } }
			
		#endregion 		// moves


		public bool IsPlayer1
		{
 			get
			{
				if (! fightManager.HasPlayer1)
					return false;

				try
				{
					return gameObject.GetInstanceID() == fightManager.Player1.gameObject.GetInstanceID();
				}
				catch (Exception ex)
				{
					Debug.Log(ex.Message);
					return false;
				}
			}
		}

		public bool IsPlayer2
		{
			get
			{
				if (! fightManager.HasPlayer2)
					return false;
				
				return gameObject.GetInstanceID() == fightManager.Player2.gameObject.GetInstanceID();
			}
		}

		public bool IsDojoShadow
		{
			get { return FightManager.CombatMode == FightMode.Dojo && IsPlayer2; }
		}

		public Fighter Opponent
		{
			get { return IsPlayer1 ? fightManager.Player2 : fightManager.Player1; } 
		}

		public bool InFight
		{
			get { return IsPlayer1 || IsPlayer2; }
		}

		private void OnReadyToFight(bool ready, bool changed, FightMode fightMode)
		{
			if (ready)
				FightFrameCount = 0;
		}

		private void OnNetworkReadyToFight(bool ready)
		{
			if (ready)
				FightFrameCount = 0;
		}
			
		private void OnTopOfOpponent()
		{
			if (IsOnTop)
				return;
			
			IsOnTop = true;
			gameObject.layer = LayerMask.NameToLayer(onTopLayer);

			if (Opponent != null)
				Opponent.BehindOpponent();
		}

		private void BehindOpponent()
		{
			IsOnTop = false;
			SetToFighterLayer();
		}
			
		public void SetToPreviewLayer()
		{
			gameObject.layer = LayerMask.NameToLayer(previewLayer);
		}

		private void SetToFighterLayer()
		{
			gameObject.layer = LayerMask.NameToLayer(fighterLayer);
		}

		public void Hide()
		{
			gameObject.SetActive(false);
		}

		public void Reveal()
		{
			gameObject.SetActive(true);
		}

		private void StateFeedback(State state)
		{
			if (!FightManager.SavedGameStatus.ShowStateFeedback)
				return;

			if (FightManager.CombatMode == FightMode.Training)
				return;
			
			switch (state)
			{
				case State.Light_HitFrame:
					fightManager.StateFeedback(IsPlayer1, FightManager.Translate("firstHit"), true, false); //, layer);
					break;

				case State.Medium_HitFrame:
					fightManager.StateFeedback(IsPlayer1, FightManager.Translate("secondImpact"), true, false); //, layer);
					break;

				case State.Heavy_HitFrame:
					if (CurrentMove != Move.Power_Attack)
						fightManager.StateFeedback(IsPlayer1, FightManager.Translate("thirdStrike"), true, false); //, layer);
					break;

//				case State.Block_Idle:
//					if (PreviewMoves)
//						fightManager.StateFeedback(isPlayer1, FightManager.Translate("block", false, true), false, true, layer);
//					break;

//				case State.Shove:
//					if (PreviewMoves)
//						fightManager.StateFeedback(IsPlayer1, FightManager.Translate("shove", false, true), false, true); //, layer);
//					break;

				case State.Shove_Stun:
					fightManager.StateFeedback(IsPlayer1, FightManager.Translate("shoved"), false, true); //, layer);
					break;

				case State.Block_Stun:
					fightManager.StateFeedback(IsPlayer1, FightManager.Translate("blocked"), false, true); //, layer);
					break;
//
//				case State.Counter_Taunt:
//					if (PreviewMoves)
//						fightManager.StateFeedback(IsPlayer1, FightManager.Translate("taunt", false, true), false, true); //, layer);
//					break;

				case State.Counter_Trigger:
					fightManager.StateFeedback(! IsPlayer1, FightManager.Translate("countered"), true, true); //, layer);		// opponent
					break;

				case State.Special:
					fightManager.StateFeedback(IsPlayer1, FightManager.Translate("special", false, true), true, true); //, layer);
					break;

				case State.Special_Extra:
					fightManager.StateFeedback(IsPlayer1, FightManager.Translate("extra", false, true), true, false); //, layer);
					break;

				case State.Vengeance:
					fightManager.StateFeedback(IsPlayer1, FightManager.Translate("vengeance", false, true), true, true); //, layer);
					break;

//				case State.Hit_Stun_Mid:
//				case State.Hit_Stun_Uppercut:
//				case State.Hit_Stun_Straight:
//				case State.Hit_Stun_Hook:
//					fightManager.StateFeedback(IsPlayer1, "HIT!");
//					break;

				default:
					break;
			}
		}


		private void FXStateEnd(AnimationState endingState)
		{
			CancelFX();
		}

		public void CancelFX()
		{
			CancelElementFX();
			CancelSpotFX();
			CancelSmokeFX();
		}
			

		#region gesture event handlers

		public void SingleFingerTap()		// same signature as GestureListener.TapAction delegate
		{
//			Debug.Log(FullName + ": SingleFingerTap");

//			if (FeedbackUI.InfoBubbleShowing)
//				return;

			if (FightManager.FightPaused) // && !PreviewMoves)
				return;

			if (UnderAI)		// listening but not interested in this
				return;

			if (!fightManager.ReadyToFight) // && !PreviewMoves)
				return;

			if (fightManager.EitherFighterExpiredHealth)
				return;

			if (InTraining && !Trainer.ValidateMove(Move.Strike_Light))		// will unfreeze if move ok
				return;

//			Debug.Log(FullName + ": TAP [ " + AnimationFrameCount + " ] State = " + CurrentState + ", CanContinue = " + CanContinue);
			DebugUI = "TAP [ " + AnimationFrameCount + " ]";

			if (CanSpecialExtra && IsFireElement)
			{
				specialOpportunityTapCount++;

				string feedback = specialOpportunityTapCount + " of " + specialExtraTaps + " ...!";
				StateUI = "MASH! " + feedback; 

				if (specialOpportunityTapCount >= specialExtraTaps)
				{
					if (InTraining && !Trainer.ValidateMove(Move.Mash))		// will unfreeze if move ok
						return;
					
					SpecialExtra();
					specialOpportunityTapCount = 0;
				}
			}
			else if (! specialExtraTriggered)
			{
				if (CanCombo)				// during light/medium hit/recovery
				{
					if (! comboTriggered)	// already triggered - super-fast taps during medium or heavy hit/recovery
					{
						TriggerCombo();		// will execute at end of recovery
						StateUI = "COMBO! [ " + MoveFrameCount + " ]"; 

						if (!UnderAI && !IsDojoShadow)
						{
							moveCuedOk = true;
							fightManager.MoveCuedFeedback(moveCuedOk);

							if (OnComboTriggered != null)
								OnComboTriggered(new FighterChangedData(this));				// snapshot
						}
					}
				}

//				Debug.Log(FullName + ": SingleFingerTap - CanContinue = " + CanContinue + ", State = " + CurrentState + ", comboTriggered = " + comboTriggered);
//				DebugUI = "TAP - CanContinue = " + CanContinue + ", State = " + CurrentState;

				if (! comboTriggered)
				{
					if (CanContinue)
						CueContinuation(Move.Strike_Light);
					else
						CueMove(Move.Strike_Light);
				}
//				else
//					Debug.Log(FullName + ": SingleFingerTap - comboTriggered!");

//				Debug.Log(FullName + ": SingleFingerTap - CanContinue = " + CanContinue + ", State = " + CurrentState + ", comboTriggered = " + comboTriggered);
			}

			if (InTraining)
			{
				Trainer.UnFreezeTraining();
			}
		}


		public void TwoFingerTap()		// same signature as GestureListener.TwoFingerTapAction delegate
		{
//			if (FeedbackUI.InfoBubbleShowing)
//				return;
			
			if (FightManager.FightPaused) // && !PreviewMoves)
				return;

			if (fightManager.EitherFighterExpiredHealth)
				return;

			if (UnderAI)		// listening but not interested in this
				return;

			if (!fightManager.ReadyToFight) // && !PreviewMoves)
				return;

//			Debug.Log(FullName + ": TwoFingerTap - CanContinue = " + CanContinue + ", state = " + CurrentState);

			if (InTraining && !Trainer.ValidateMove(Move.Roman_Cancel))		// will unfreeze if move ok
				return;

			if (! HasRomanCancelGauge)
			{
				NoGaugeFeedback(ProfileData.RomanCancelGauge);
				return;
			}
				
			CueMove(Move.Roman_Cancel);		// only move that overrides another queued move

			if (InTraining)
			{
//				Debug.Log(FullName + ": TwoFingerTap - CanContinue = " + CanContinue + ", state = " + CurrentState);
				Trainer.UnFreezeTraining();
			}
		}

			
		public void HoldDown()		// same signature as GestureListener.HeldDownAction delegate
		{
//			if (FeedbackUI.InfoBubbleShowing)
//				return;
			
			if (FightManager.FightPaused) // && !PreviewMoves)
				return;

			if (!fightManager.ReadyToFight) // && !PreviewMoves)
				return;

			if (fightManager.EitherFighterExpiredHealth)
				return;

			if (UnderAI)		// listening but not interested in this
				return;

			if (InTraining && !Trainer.ValidateMove(Move.Block))		// will unfreeze if move ok
				return;

			if (! InTraining)  			// to stop a return to block idle at end of block stun countdown
				HoldingBlock = true;
			
			// start block idle
			if (CanContinue)
				CueContinuation(Move.Block);
			else
				CueMove(Move.Block);

			if (InTraining)
			{
//				Debug.Log(FullName + ": HoldDown - CanContinue = " + CanContinue + ", state = " + CurrentState);
				Trainer.UnFreezeTraining();
			}
		}

		public void HoldRelease()		// same signature as GestureListener.HeldUpAction delegate
		{
			if (FightManager.FightPaused) // && !PreviewMoves)
				return;

			if (UnderAI || InTraining)		// listening but not interested in this
				return;

			ReleaseBlock();
		}


		public void SwipeLeft()	// same signature as GestureListener.SwipeLeftAction delegate
		{
//			if (FeedbackUI.InfoBubbleShowing)
//				return;
			
			if (FightManager.FightPaused) // && !PreviewMoves)
				return;

			if (!fightManager.ReadyToFight) // && !PreviewMoves)
				return;

			if (fightManager.EitherFighterExpiredHealth)
				return;
			
			if (UnderAI)		// listening but not interested in this
				return;

			if (InTraining && !Trainer.ValidateMove(Move.Counter))		// will unfreeze if move ok
				return;

			if (! ChainCounter())			// don't cue counter if chainedCounter
			{
				if (! HasCounterGauge)
				{
					NoGaugeFeedback(ProfileData.CounterGauge);
					return;
				}

				CueCounter();					// virtual
			}

			if (InTraining)
			{
//				Debug.Log(FullName + ": SwipeLeft - CanContinue = " + CanContinue + ", state = " + CurrentState);
				Trainer.UnFreezeTraining();
			}
		}

		public void SwipeRight()	// same signature as GestureListener.SwipeRightAction delegate
		{
//			if (FeedbackUI.InfoBubbleShowing)
//				return;
			
			if (FightManager.FightPaused) // && !PreviewMoves)
				return;

			if (!fightManager.ReadyToFight) // && !PreviewMoves)
				return;

			if (fightManager.EitherFighterExpiredHealth)
				return;
			
			if (UnderAI)		// listening but not interested in this
				return;

			if (InTraining && !Trainer.ValidateMove(Move.Special))		// will unfreeze if move ok
				return;

//			Debug.Log(FullName + ": SwipeRight - CanContinue = " + CanContinue + ", state = " + CurrentState);

			if (! ChainSpecial())	// if possible
			{
				if (IsWaterElement)  	// profile.IsElement(FighterElement.Water))
					SpecialExtra();		// if possible
			}
					
			if (! chainedSpecial && ! specialExtraTriggered)	
			{
				if (CanContinue || IsBlocking)
					CueContinuation(Move.Special);
				else if (! CanSpecialExtra)			// to prevent execution of special during special opportunity for non-water fighters
					CueMove(Move.Special);
			}

			if (InTraining)
			{
//				Debug.Log(FullName + ": SwipeRight - CanContinue = " + CanContinue + ", state = " + CurrentState);
				Trainer.UnFreezeTraining();
			}
		}
			
			
		public void SwipeLeftRight()	// same signature as GestureListener.SwipeLeftRightAction delegate
		{
//			if (FeedbackUI.InfoBubbleShowing)
//				return;
			
			if (FightManager.FightPaused) // && !PreviewMoves)
				return;

			if (!fightManager.ReadyToFight) // && !PreviewMoves)
				return;

			if (fightManager.EitherFighterExpiredHealth)
				return;
			
			if (UnderAI)		// listening but not interested in this
				return;

			if (InTraining && !Trainer.ValidateMove(Move.Vengeance))		// will unfreeze if move ok
				return;

//			Debug.Log(FullName + ": SwipeLeftRight - CanContinue = " + CanContinue + ", state = " + CurrentState);

			if (! HasVengeanceGauge)
			{
				NoGaugeFeedback(ProfileData.VengeanceGauge);
				return;
			}

			if (CanContinue || IsBlocking)
				CueContinuation(Move.Vengeance);
			else
				CueMove(Move.Vengeance);

			if (InTraining)
			{
//				Debug.Log(FullName + ": SwipeLeftRight - CanContinue = " + CanContinue + ", state = " + CurrentState);
				Trainer.UnFreezeTraining();
			}
		}
			
		// shove
		public void SwipeDown()	// same signature as GestureListener.SwipeDownAction delegate
		{
//			if (FeedbackUI.InfoBubbleShowing)
//				return;
			
			if (FightManager.FightPaused) // && !PreviewMoves)
				return;

			if (!fightManager.ReadyToFight) // && !PreviewMoves)
				return;

			if (fightManager.EitherFighterExpiredHealth)
				return;

			if (UnderAI)		// listening but not interested in this
				return;

			if (InTraining && !Trainer.ValidateMove(Move.Shove))		// will unfreeze if move ok
				return;
			
			if (CanContinue || IsBlocking)
				CueContinuation(Move.Shove);
			else
				CueMove(Move.Shove);

			if (InTraining)
			{
//				Debug.Log(FullName + ": SwipeDown - CanContinue = " + CanContinue + ", state = " + CurrentState);
				Trainer.UnFreezeTraining();
			}
		}

		// powerup
		public void SwipeUp()	// same signature as GestureListener.SwipeUpAction delegate
		{
//			if (FeedbackUI.InfoBubbleShowing)
//				return;
			
//			Debug.Log(FullName + ": SwipeUp - FightPaused = " + fightManager.FightPaused + ", TriggerPowerUp = " + TriggerPowerUp + ", CanTriggerPowerUp = " + CanTriggerPowerUp);

			if (FightManager.FightPaused) // && !PreviewMoves)
				return;

			if (!fightManager.ReadyToFight) // && !PreviewMoves)
				return;

			if (fightManager.EitherFighterExpiredHealth)
				return;

			if (UnderAI)		// listening but not interested in this
				return;

			if (InTraining && !Trainer.ValidateMove(Move.Power_Up))		// will unfreeze if move ok
				return;

			if (FightManager.CombatMode == FightMode.Arcade) // && !PreviewMoves)
				return;

			if (FightManager.CombatMode != FightMode.Dojo && ! CanTriggerPowerUp)
			{
				WrongFeedbackFX();
				return;
			}

			if (fightManager.PowerUpFeedbackActive)
				return;

			PowerUp();

			if (InTraining)
			{
//				Debug.Log(FullName + ": SwipeUp - CanContinue = " + CanContinue + ", state = " + CurrentState);
				Trainer.UnFreezeTraining();
			}
		}


		public void FingerTouch(Vector3 position = default(Vector3))
		{
			if (FightManager.SavedGameStatus.FightInProgress && ! FightManager.FightPaused)
				moveCuedOk = false;
		}

		public void FingerRelease(Vector3 position = default(Vector3))  		// same signature as GestureListener.FingerReleaseAction delegate
		{			
			if (UnderAI)  		// listening but not interested in this
				return;

			if (! InTraining)
				CueMove(Move.ReleaseBlock);						// just to make sure!
		}
			
		#endregion


		#region info bubble 

		private void InterruptFightBubble(InfoBubbleMessage message, bool isShowing, bool freezeFight)
		{
			if (isShowing)
			{
				if (freezeFight && !isFrozen)
				{
					frozenByInfoBubble = true;
					fightManager.FreezeFight();
				}
			}
			else
			{
				if (isFrozen && frozenByInfoBubble)
				{
					frozenByInfoBubble = true;
					fightManager.UnfreezeFight();
				}
				
				FightManager.InfoMessageRead(message);
			}
		}


		private void OnLogFailedInput(FailedInput failedInput)		// currently during training only
		{
			if (! InTraining)
				return;
			
			FailedMoves.Add(failedInput);

			if (FailedMoves.Count >= MaxFailedInputs)
			{
				fightManager.FailedInputBubble(failedInput);
				ClearFailedMoves();
			}
		}

		private void ClearFailedMoves()
		{
			FailedMoves.Clear();
		}

		#endregion

		private void Update()
		{
			if (! InFight)
				return;

			if (FightManager.FightPaused)
				return;

			// handle key strokes for testing in Unity
			if (!FightManager.IsNetworkFight && ! UnderAI && !IsDojoShadow && ! DeviceDetector.IsMobile)
			{
				if (Input.GetKeyDown(KeyCode.X))
				{
					TwoFingerTap();				// roman cancel (requires gauge)
				}
			}
		}


		protected override void OnNextAnimationFrame()
		{
			// state frame numbers may trigger AI strategies
			IdleFrameCount();
			BlockIdleFrameCount();
			CanContinueFrameCount();
			VengeanceFrameCount();
			GaugeIncreaseFrameCount();
			StunnedFrameCount();
			LastHitFrameCount();
		}

		private void FixedUpdate()
		{
			if (PreviewIdle && FightManager.IsFighterAnimationFrame) 			// eg. for fighter select scene preview idle (not driven by FightManager)
				NextAnimationFrame();
		}
			

		// driven by UpdateAnimation
		private void CountStatusEffectFrames()
		{
			if (StatusEffectStartFrame == 0)
				return;

			if (currentStatusEffect == StatusEffect.None)
			{
				StatusEffectStartFrame = 0;		// shouldn't happen
				return;
			}

			int statusEffectFrames = 0;

			switch (currentStatusEffect)
			{
				case StatusEffect.KnockOut:
					statusEffectFrames = KnockOutFreezeFrames;
					break;

				case StatusEffect.HealthUp:
				case StatusEffect.OnFire:
				case StatusEffect.ArmourUp:
				case StatusEffect.ArmourDown:
					statusEffectFrames = StatusEffectFrames;
					break;

//				case StatusEffect.OK:
//					StatusEffectFramesRemaining = OKEffectFrames;
//					break;
			}

//			Debug.Log(FullName + ": CountStatusEffectFrames: " + (AnimationFrameCount - StatusEffectStartFrame) + " / " + statusEffectFrames);
//			fightManager.HealthDebugText(IsPlayer1, FightFrameCount + " - " + StatusEffectStartFrame + " / " + statusEffectFrames + " damage: " + damageWhileOnFire);

			if (FightFrameCount - StatusEffectStartFrame == statusEffectFrames)
				StopCurrentStatusEffect();
		}

		private void StartStatusEffectFrameCount(StatusEffect effect)
		{
			if (currentStatusEffect != StatusEffect.None)
				StopCurrentStatusEffect();

			StatusEffectStartFrame = FightFrameCount;
			currentStatusEffect = effect;

//			fightManager.HealthDebugText(IsPlayer1, currentStatusEffect.ToString() + " / " + StatusEffectStartFrame.ToString());
		}

		private void StopCurrentStatusEffect()
		{
//			Debug.Log(FullName + "StopCurrentStatusEffect: damageWhileOnFire = " + damageWhileOnFire);

			switch (currentStatusEffect)
			{
				case StatusEffect.KnockOut:
					fightManager.SnapshotCameraPosition(ProfileData.FighterClass == FighterClass.Boss);
					fightManager.UnfreezeFight();

					if (ExpiredHealth || ExpiredState) 		// TODO: check this! (skeletron) 
						EndKOFreeze();				// next round if didn't take second life opportunity
					break;

				case StatusEffect.OnFire:
					StopOnFire();
					break;

				case StatusEffect.HealthUp:
					StopHealthUp();
					break;

				case StatusEffect.ArmourUp:
					StopArmourUp();
					break;

				case StatusEffect.ArmourDown:
					StopArmourDown();
					break;

//				case StatusEffect.OK:
//					StopOK();
//					break;
			}

			currentStatusEffect = StatusEffect.None;
			StatusEffectStartFrame = 0;
		}

//		public void StopAllStatusEffects()
//		{
//			Debug.Log(FullName + ": StopAllStatusEffects");
//			StopOnFire();
//			StopHealthUp();
//			StopArmourUp();
//			StopArmourDown();
//		}


		public void SetPreview(uint idleFrameNumber) 		// for preview in fighter select menus
		{
			MovieClipFrame = idleFrameNumber;

//			Debug.Log(FullName + ": SetIdleFrame MovieClipFrame = " + MovieClipFrame + ", StateLabel " + currentAnimation.StateLabel + ", StateLength " + currentAnimation.StateLength);
			PreviewIdle = true;
		}
			
		private void IdleFrameCount()
		{
			if (IsIdle)
			{	
				idleFrameCount++;

//				Debug.Log(FullName + " IdleFrameCount " + idleFrameCount);

				if (OnIdleFrame != null)
				{
					var stateData = new FighterChangedData(this);
					stateData.IdleFrame(idleFrameCount);
					OnIdleFrame(stateData);
				}
			}
			else
				idleFrameCount = 0; 
		}

		private void BlockIdleFrameCount()
		{
			if (IsBlockIdle)
			{
				blockIdleFrameCount++;

				if (OnBlockIdleFrame != null)
				{
					var stateData = new FighterChangedData(this);
					stateData.BlockIdleFrame(blockIdleFrameCount);
					OnBlockIdleFrame(stateData);
				}
			}
			else
				blockIdleFrameCount = 0; 
		}

		private void CanContinueFrameCount()
		{
			if (CanContinue)
			{
				canContinueFrameCount++;

				if (OnCanContinueFrame != null)
				{
					var stateData = new FighterChangedData(this);
					stateData.CanContinueFrame(canContinueFrameCount);
					OnCanContinueFrame(stateData);
				}
			}
			else
				canContinueFrameCount = 0; 
		}

		private void VengeanceFrameCount()
		{
			if (CurrentState == State.Vengeance)
			{
				vengeanceFrameCount++;

				if (OnVengeanceFrame != null)
				{
					var stateData = new FighterChangedData(this);
					stateData.VengeanceFrame(vengeanceFrameCount);
					OnVengeanceFrame(stateData);
				}
			}
			else
				vengeanceFrameCount = 0; 
		}

		private void GaugeIncreaseFrameCount()
		{
			if (! IncreasedGauge)			// last gauge change was not an increase
			{
				gaugeIncreaseFrameCount = 0;
				return;
			}

			gaugeIncreaseFrameCount++;

			if (OnGaugeIncreasedFrame != null)
			{
				var stateData = new FighterChangedData(this);
				stateData.GaugeIncreasedFrame(gaugeIncreaseFrameCount, ProfileData.SavedData.Gauge);
				OnGaugeIncreasedFrame(stateData);
			}
		}

		private void StunnedFrameCount()
		{
			if (IsStunned)
			{
				stunnedFrameCount++;

				if (OnStunnedFrame != null)
				{
					var stateData = new FighterChangedData(this);
					stateData.StunnedFrame(stunnedFrameCount);
					OnStunnedFrame(stateData);
				}
			}
			else
				stunnedFrameCount = 0; 
		}

		private void LastHitFrameCount()
		{
			if (takenLastHit)
			{
				lastHitFrameCount++;

				if (OnLastHitFrame != null)
				{
					var stateData = new FighterChangedData(this);
					stateData.LastHitFrame(lastHitFrameCount);
					OnLastHitFrame(stateData);
				}
			}
			else
				lastHitFrameCount = 0; 
		}
			

		public void Freeze()
		{
			isFrozen = true;
		}

		public void Unfreeze()
		{
			isFrozen = false;
		}


		// character animation is at 15fps by default
		public void UpdateAnimation()
		{
			if (!InFight)
				return;

			if (!FightManager.IsFighterAnimationFrame) 		// just in case
				return;

			FightFrameCount++;

			if (OnFire)
				OnFireDamage();  	// reduce health if on fire

			Regenerate();			// increase health if regenerator static power-up or dojo shadow fighter

			if (StatusEffectStartFrame > 0)			// on fire, health up, armour up, armour down, KO
				CountStatusEffectFrames();
			
			if (fightManager.FightFrozen)
				return;

			// return if frozen independently..
			if (isFrozen)
			{
				if (romanCancelFrozen && romanCancelFreezeFramesRemaining >= 0)
					RomanCancelFreezeCountdown();		// for set number of frames
				else if (powerUpFrozen && powerUpFreezeFramesRemaining >= 0)
					PowerUpFreezeCountdown();		// for set number of frames

				return;
			}
				
			// ..or if fighters are both frozen
			if (freezeFightFrames > 0)
			{
				fightManager.FreezeFight(freezeFightFrames);	// to ensure both fighters are frozen in sync
				freezeFightFrames = 0;
				return;
			}
				
			if (counterTriggerStun)		// deferred from previous frame
			{
				StartCounterTriggerStun();
				counterTriggerStun = false;
			}

			if (HasCuedMove && !isFrozen)
				ExecuteCuedMove();
			else
				NextAnimationFrame();
				
			var stateFrame = StateFrameCount + 1;		// count starts from 0
			StateUI = IsIdle ? "" : CurrentFrameLabel + " [ " + stateFrame + " ]";
				
			MoveFrameCount++;		
			StateFrameCount++;		// frame number used to determine hit frames, etc.
			HitFrameCount++;		// state frame count, reset on every hit

			if ((IsHitStunned || IsShoveStunned) && hitStunFramesRemaining >= 0)
				HitStunCountdown();		// countdown to zero and then return to idle
			else if (!InTraining && IsBlockStunned && blockStunFramesRemaining >= 0)
				BlockStunCountdown();	// countdown to zero and then return to idle

			// add latest position to tracking lag queue and remove the oldest if queue is full
			trackingPositions.Enqueue(transform.position.x);
			if (cameraController != null && trackingPositions.Count > cameraController.TrackingLagFrames)
				trackingPositions.Dequeue(); 

			// check for a hit frame and action according to type
			if (! IsIdle)
			{
				// catch-all in case AI stuck in a non-idle state for too long
//				if (UnderAI && !IsBlockIdle && !IsStunned && !ExpiredState && !ExpiredHealth && StateFrameCount > AIStateFrameTimeout)
				if (UnderAI && !IsBlockIdle && !IsStunned && !ExpiredState && !ExpiredHealth && !FallenState && !IsDashing && HitFrameCount > AIHitFrameTimeout)
				{
					var timeOut = "TIMEOUT: " + CurrentState + ", Continuation: " + NextContinuation + ", isFrozen: " + isFrozen;
					Debug.Log(FullName + ": " + timeOut);
					DebugUI = timeOut;
					ReturnToIdle();
				}
					
				var hitData = LookupHitFrameData;	// by state and hit frame count

				if (hitData != null)				// null if not a hit frame
					ProcessHitData(hitData); 		// multiple events possible
			}

			if (UnderAI && AIController != null)
				AIController.StrategyCued = false;		// reset each beat - prevents >1 move triggered by AI receiving events
		}

		// driven by UpdateAnimation
		private void OnFireDamage()
		{	
			if (! OnFire)
				return;

			if (ProfileData.SavedData.Health <= 1.0f)			// can't die from being on fire!
				return;

			var damagePerTick = ProfileData.OnFireDamagePerTick;
			var damage = damagePerTick + (damagePerTick * ProfileData.LevelFactor);

//			damage *= ProfileData.HitDamageFactor;		// TEMP: remove
//			damage *= fightManager.HitDamageFactor;		// TEMP: remove

			if (damage > ProfileData.SavedData.Health - 1.0f)			// can't die from being on fire!
				damage = ProfileData.SavedData.Health - 1.0f;
			
			UpdateHealth(damage);
			damageWhileOnFire += damage;

//			Debug.Log(FullName + ": OnFireDamage: " + damage + "(" + damageWhileOnFire + ")");
		}

		// driven by UpdateAnimation
		private void Regenerate()
		{
			// in the dojo, the shadow fighter constantly regenerates health
			bool dojoRegenerate = FightManager.CombatMode == FightMode.Dojo && IsDojoShadow
									&& !ExpiredHealth && !ExpiredState && !Opponent.ExpiredHealth && !Opponent.ExpiredState
									&& !FallenState && !fightManager.FightFrozen;

			// constantly increase health if fighter has the regenerator power-up
			bool powerUpRegenerate = FightManager.CombatMode != FightMode.Arcade && StaticPowerUp == FightingLegends.PowerUp.Regenerator &&
				!ExpiredHealth && !Opponent.ExpiredHealth && !fightManager.FightFrozen;

			if (ProfileData.SavedData.Health < ProfileData.LevelHealth && (dojoRegenerate || powerUpRegenerate))
			{
				var regeneratorFactor = ProfileData.RegeneratorFactor * 4;
				var healthIncrease = regeneratorFactor + (regeneratorFactor * ProfileData.LevelFactor);
				
				UpdateHealth(-healthIncrease);
			}
		}
			
		private bool ActionHit(HitFrameData hitData, bool lastHit)
		{
			bool hitOk = false;		// miss if false

			StartCoroutine(StrikeTravel());			// to make sure every strike connects

			if (hitData.TypeOfHit == HitType.Shove)
				hitOk = Opponent.CanBeShoved && DeliverShove(hitData, ProfileData.AttackDistance / 2.0f);	// shove stun + spot FX
			else
				hitOk = DeliverHit(hitData, ProfileData.AttackDistance, lastHit);	// hit stun + inflict damage + spot FX

//			Debug.Log(FullName + ": HIT FRAME! Miss = " + !hitOk + ", Priority = " + CurrentPriority + " [ " + AnimationFrameCount + " ]");

			return hitOk;
		}

		// each state frame may have multiple actions (eg. hit + state end)
		private void ProcessHitData(HitFrameData hitData)
		{
			if (hitData == null)
				return;
			
			foreach (var frameAction in hitData.Actions)
			{
				switch (frameAction)
				{
					case FrameAction.None:
						continue;

					case FrameAction.Hit:		// hit frame!
					{
						bool hitMissed = !ActionHit(hitData, false);
						if (!hitMissed)
							HitFrameCount = 0;		// reset AI timeout on each hit

//						if (!hitMissed && OnHit != null)
//						{
//							var newState = new FighterChangedData(this);
//							newState.LastHit(CurrentState);
//							OnHit(newState);
//						}
						break;
					}

					case FrameAction.LastHit:		// hit frame!
					{
						takenLastHit = true;		// reset at end of state
						lastHitFrameCount = 0;		// to make sure

						bool hitMissed = !ActionHit(hitData, true);		

						// receiver of final hit moves back to the default position
						if (! LMH_HitFrame(hitData))
						{
							if (Opponent != null)
							{
								if (hitData.FreezeFrames > 0)
									Opponent.returnToDefault = true; 			// deferred till after freeze / next frame
								else
									Opponent.ReturnToDefaultDistance();			// immediate
							}

							CanContinue = true;				// until next end of state
						}
							
						if (! hitMissed)
						{
							if (OnLastHit != null)
							{
								var newState = new FighterChangedData(this);
								newState.LastHit(CurrentState);
								OnLastHit(newState);
							}
						}

						// punishable priority for some states
						switch (CurrentState)
						{
							case State.Special:
							case State.Special_Extra:
							case State.Vengeance:
							case State.Counter_Attack:
								CurrentPriority = Punishable_Priority;
								break;
						}
						break;
					}

//					case FrameAction.CanContinue:
//						CanContinue = true;				// until next end of state
//						break;

					case FrameAction.SpecialFX:
						TriggerSpotFX(hitData.SpotEffect, false);
						break;

					default:
						break;
				}
			}
		}


		private void LastHitXP(HitFrameData hitData)
		{
			float xp = 0;

			switch (hitData.State)
			{
				case State.Light_HitFrame:
					xp = FightManager.LightStrikeXP;
					break;
				case State.Medium_HitFrame:
					xp = FightManager.MediumStrikeXP;
					break;
				case State.Heavy_HitFrame:
					xp = FightManager.HeavyStrikeXP;
					break;
				case State.Special:
					xp = FightManager.SpecialXP;
					break;
				case State.Special_Extra:
					xp = FightManager.SpecialExtraXP;
					break;
				case State.Counter_Attack:
					xp = FightManager.CounterAttackXP;
					break;
				case State.Vengeance:
					xp = FightManager.VengeanceXP;
					break;
				case State.Shove:
					xp = FightManager.ShoveXP;
					break;
			}

			IncreaseXP(xp);
		}


		private void IncreaseXP(float xp)
		{
//			if (PreviewMoves)
//				return;
			
			if (UnderAI)
				return;

			if (InTraining)
				return;

			if (FightManager.CombatMode == FightMode.Arcade || FightManager.CombatMode == FightMode.Training || FightManager.CombatMode == FightMode.Dojo)
				return;
			
			if (xp <= 0)
				return;
			
			xp *= FightManager.XPFactor;

//			fightManager.TextFeedback(IsPlayer1, string.Format("XP +{0:0.##} = {1:0.##}", xp, ProfileData.SavedData.XP + xp));

			XP += xp;
			ProfileData.SavedData.TotalXP += xp;

			CheckForLevelIncrease();
		}
			
		public static float XPToNextLevel(int level)
		{
			float levelUpXp = levelUpXPBase;
			float factor = 0;

			for (int i = 1; i < (level+1); i++)
			{
				//					factor = 1.0f + ((float)i / 1000.0f);		// 0.1% (level 1)
				factor = 1.0f + ((float)i / 1500.0f);		// 0.075%
//				factor = 1.0f + ((float)i / 2000.0f); 		// 0.05%
				//					factor = 1.0f + (1.0f / 100.0f);			// 0.01%
				levelUpXp *= factor;
			}

			return levelUpXp;
		}
			
		public float LevelUpXP
		{
			get { return XPToNextLevel(Level); }	
		}


		private void LevelXPTest()
		{
			var originalLevel = Level;
			for (int level = 1; level <= maxLevel; level++)
			{
				Level = level;
				Debug.Log(FullName + ": LevelXPTest: Level = " + Level + ", LevelUpXP = " + Mathf.RoundToInt(LevelUpXP));
			}

			Level = originalLevel;
		}

//		private void LevelDamageTest()
//		{
//			var originalLevel = Level;
//			float damage = 10.0f;
//
//			for (int level = 1; level <= maxLevel; level++)
//			{
//				Level = level;
//				Debug.Log(FullName + ": LevelDamageTest: Level = " + Level + ", damage = " + (damage + (damage * ProfileData.LevelFactor)));
//			}
//
//			Level = originalLevel;
//		}

		private void CheckForLevelIncrease()
		{
			if (UnderAI)
				return;

//			if (PreviewMoves)
//				return;

			if (FightManager.CombatMode != FightMode.Survival)
				return;

			if (Level+1 >= maxLevel)
				return;

			bool nextLevel = XP >= LevelUpXP;

			if (nextLevel)
			{
				Level++;
				XP = 0;		// reset till next level

				// freeze both fighters for effect ... on next frame
				SetFreezeFrames(levelUpFreezeFrames);	
				fightManager.LevelUpFeedback(ProfileData.SavedData.Level, true, false);

				if (hitFlash != null)
				{
					if (colourFlashCoroutine != null)
						StopCoroutine(colourFlashCoroutine);

					colourFlashCoroutine = hitFlash.PlayColourFlash(colourFlashColour, levelUpFlashTime);
					StartCoroutine(colourFlashCoroutine);
				}
			}
		}

		public int Level
		{
			get { return ProfileData.SavedData.Level; }

			set
			{
				if (value == ProfileData.SavedData.Level)
					return;

				ProfileData.SavedData.Level = value;

//				Debug.Log(FullName + ": LEVEL changed to " + ProfileData.SavedData.Level);

				if (OnLevelChanged != null)
					OnLevelChanged(Level);
			}
		}
	
		public float XP
		{
			get { return ProfileData.SavedData.XP; }

			set
			{
				if (value == ProfileData.SavedData.XP)
					return;

				ProfileData.SavedData.XP = value;

//				Debug.Log(FullName + ": XP changed to " + ProfileData.SavedData.XP);

				if (OnXPChanged != null)
					OnXPChanged(XP);
			}
		}

		public bool IsLocked
		{
			get { return ProfileData.SavedData.IsLocked; }

			set
			{
				if (value == ProfileData.SavedData.IsLocked)
					return;

				ProfileData.SavedData.IsLocked = value;

				if (OnLockedChanged != null)
					OnLockedChanged(this, IsLocked);
			}
		}

		public bool CanUnlockOrder
		{
			get { return IsLocked && UnlockOrder == FightManager.SavedGameStatus.FighterUnlockedLevel+1; }
		}

		public bool CanUnlockDefeats
		{
			get { return ProfileData.SavedData.CanUnlockDefeats; }
		}

		public int UnlockCoins
		{
			get { return ProfileData.SavedData.UnlockCoins; }
		}

		public int UnlockOrder
		{
			get { return ProfileData.SavedData.UnlockOrder; }
		}

		public int UnlockDefeats
		{
			get { return ProfileData.SavedData.UnlockDefeats; }
		}

		public AIDifficulty UnlockDifficulty
		{
			get { return ProfileData.SavedData.UnlockDifficulty; }
		}

//		private bool NextLevel()
//		{
//			float power = 0.25f; // 0.33f;  0.25f;
//
//			// formula for graph curve is y=pow(x, 0.5), where x is XP and y is level
//			int level = (int)(Mathf.Pow(ProfileData.SavedData.TotalXP, power));
//
//			return level > ProfileData.SavedData.Level;
//		}


		#region power-ups

		// swipe up
		public bool PowerUp()
		{
//			Debug.Log(FullName + ": PowerUp " + TriggerPowerUp + ", CanTriggerPowerUp = " + CanTriggerPowerUp + ", FightPaused = " + FightManager.FightPaused);

			if (fightManager.PowerUpFeedbackActive)
				return false;
			
			if (FightManager.CombatMode != FightMode.Dojo)
			{
				if (FightManager.FightPaused)
					return false;

				if (InTraining)
					return false;

				if (! CanTriggerPowerUp)
					return false;
				
				switch (TriggerPowerUp)
				{
					case FightingLegends.PowerUp.SecondLife:
						StateUI = "SECOND LIFE! [ " + MoveFrameCount + " ]"; 
						TriggerSecondLife();
						break;

					case FightingLegends.PowerUp.Ignite:
						StateUI = "IGNITE! [ " + MoveFrameCount + " ]"; 
						TriggerIgnite();
						break;

					case FightingLegends.PowerUp.HealthBooster:
						StateUI = "HEALTH BOOSTER! [ " + MoveFrameCount + " ]"; 
						TriggerHealthBooster();
						break;

					case FightingLegends.PowerUp.PowerAttack:
						StateUI = "POWER ATTACK! [ " + MoveFrameCount + " ]"; 
						TriggerPowerAttack();
						break;

					case FightingLegends.PowerUp.VengeanceBooster:
						StateUI = "VENGEANCE BOOSTER! [ " + MoveFrameCount + " ]"; 
						TriggerVengeanceBooster();
						break;

					case FightingLegends.PowerUp.None:
					default:
						break;
				}
			}
			else 		// dojo - no trigger power-up assigned so just provide feedback
			{
				powerUpTriggered = true;
			}
					
			if (powerUpTriggered)
			{
				if (!performingPowerAttack)
					PowerUpFeedback();
			}

			return powerUpTriggered;
		}

		private void PowerUpFeedback()
		{
			fightManager.PowerUpAudio();
//			fightManager.StateFeedback(IsPlayer1, FightManager.Translate("powerUp", false, true), !IsDojoShadow, IsDojoShadow);

			PowerUpFreeze();
			if (Opponent != null)
				Opponent.PowerUpFreeze();

			StartCoroutine(fightManager.PowerUpFeedback(FightManager.CombatMode == FightMode.Dojo ? FightingLegends.PowerUp.None : TriggerPowerUp, true, false));

			if (hitFlash != null)
			{
				if (colourFlashCoroutine != null)
					StopCoroutine(colourFlashCoroutine);

				colourFlashCoroutine = hitFlash.PlayColourFlash(colourFlashColour, powerUpFlashTime);
				StartCoroutine(colourFlashCoroutine);
			}

			if (!UnderAI && !IsDojoShadow)
			{
				moveCuedOk = true;
				fightManager.MoveCuedFeedback(moveCuedOk);
			}

			// more kudos for more expensive power-ups
			if (IsPlayer1)
				FightManager.IncreaseKudos(ProfileData.SavedData.TriggerPowerUpCost); // * FightManager.KudosPowerUpFactor);

			if (OnPowerUpTriggered != null)
				OnPowerUpTriggered(this, TriggerPowerUp, CurrentState == State.Idle);		// stars + cool-off countdown
		}


		public void ResetPowerUpTrigger()
		{
			powerUpTriggered = false;
		}

		public void TriggerSecondLife()		// swipe up after KO (if powered-up)
		{
			if (! CanSecondLife)
				return;

			powerUpTriggered = true;
			secondLifeTriggered = true;			// activated at end of KO freeze
			fightManager.StateFeedback(IsPlayer1, FightManager.Translate("secondLife", false, true), true, false);
		}

		private void TriggerIgnite()	
		{
			if (Opponent != null)
			{
				var xOffset = IsPlayer1 ? feedbackOffsetX : -feedbackOffsetX;
				Opponent.StartOnFire(-xOffset);
			}
			powerUpTriggered = true;
			fightManager.StateFeedback(IsPlayer1, FightManager.Translate("ignite", false, true), true, false);
		}

		private void TriggerHealthBooster()		
		{
			ResetHealth(false);
			powerUpTriggered = true;
			fightManager.StateFeedback(IsPlayer1, FightManager.Translate("healthBoost", false, true), true, false);
		}

		private void TriggerPowerAttack()	
		{
			if (CanContinue || IsBlocking)
				CueContinuation(Move.Power_Attack);
			else
				CueMove(Move.Power_Attack);
			
			powerUpTriggered = true;
			performingPowerAttack = true;
			fightManager.StateFeedback(IsPlayer1, FightManager.Translate("powerAttack", false, true), true, false);
		}

		private void TriggerVengeanceBooster()	
		{
			MaxGauge();
			powerUpTriggered = true;
			fightManager.StateFeedback(IsPlayer1, FightManager.Translate("vengeanceBoost", false, true), true, false);
		}

		#endregion  // power-ups


		#region state start / end

		protected override void EndState()
		{
			if (OnStateEnded != null)
			{
				var oldState = new FighterChangedData(this);
				oldState.EndedState(CurrentState);
				OnStateEnded(oldState);
			}

			StartCoroutine(UnfreezeEndState());		// waits while isFrozen
		}

		private IEnumerator UnfreezeEndState()
		{
//			Debug.Log(FullName + ": UnfreezeEndState: CurrentState = " + CurrentState);
			while (isFrozen)
				yield return null;
			
			CanContinue = false;		// ie. at end of all states
			StateFrameCount = 0;		// reset for next state
			HitFrameCount = 0;		// reset for next state

			takenLastHit = false;
			lastHitFrameCount = 0;

			switch (CurrentState)		
			{
				case State.Light_Windup:
					EndLightWindup();
					break;		
				case State.Light_HitFrame:
					EndLightHitFrame();
					break;			
				case State.Light_Recovery:
					EndLightRecovery();
					break;		
				case State.Light_Cutoff:
					EndLightCutoff();
					break;

				case State.Medium_Windup:
					EndMediumWindup();
					break;			
				case State.Medium_HitFrame:
					EndMediumHitFrame();
					break;			
				case State.Medium_Recovery:
					EndMediumRecovery();
					break;			
				case State.Medium_Cutoff:
					EndMediumCutoff();
					break;

				case State.Heavy_Windup:
					EndHeavyWindup();
					break;			
				case State.Heavy_HitFrame:
					EndHeavyHitFrame();
					break;			
				case State.Heavy_Recovery:
					EndHeavyRecovery();
					break;			
				case State.Heavy_Cutoff:
					EndHeavyCutoff();
					break;

				case State.Shove:
					EndShove();
					break;

				case State.Vengeance:
					EndVengeance();
					break;

				case State.Special_Start:
					EndSpecialStart();
					break;
				case State.Special:
					EndSpecial();
					break;
				case State.Special_Opportunity:
					EndSpecialOpportunity();
					break;
				case State.Special_Extra:
					EndSpecialExtra();	
					break;
				case State.Special_Recover:
					EndSpecialRecover();
					break;

				case State.Counter_Taunt:
					EndCounterTaunt();
					break;
				case State.Counter_Trigger:
					EndCounterTrigger();
					break;
				case State.Counter_Attack:
					EndCounterAttack();
					break;
				case State.Counter_Recovery:
					EndCounterRecovery();
					break;	

				case State.Hit_Stun_Straight:
				case State.Hit_Stun_Uppercut:
				case State.Hit_Stun_Mid:
				case State.Hit_Stun_Hook:
				case State.Block_Stun:
				case State.Shove_Stun:
					break;

				case State.Block_Idle:
				case State.Block_Idle_Damaged:
					break;	

				case State.Hit_Straight_Die:
				case State.Hit_Uppercut_Die:
				case State.Hit_Mid_Die:
				case State.Hit_Hook_Die:
					break;

				case State.Idle_Damaged:
					EndIdleDamaged();
					break;

				case State.Fall:			// skeletron -> ready to die
					EndFalling();
					break;

				case State.Ready_To_Die:	// skeletron (loops)
//					EndReadyToDie();
					break;

				case State.Die:
					break;

				case State.Dash:
					ReturnToIdle();
					break;

				case State.Tutorial_Punch_Start:
					EndTutorialPunchStart();
					break;

				case State.Tutorial_Punch:
					EndTutorialPunch();
					break;

				case State.Tutorial_Punch_End:
					EndTutorialPunchEnd();
					break;

				case State.Idle:
				case State.Void:
				case State.Default:
				default:
					break;
			}

//			Debug.Log(FighterFullName + " EndState: " + CurrentStateLabel + " at [" + fightManager.AnimationFrameCount + "]");
			yield return null;
		}

		protected virtual void EndLightWindup()
		{
			CurrentState = State.Light_HitFrame;
		}

		protected virtual void EndMediumWindup()
		{
			CurrentState = State.Medium_HitFrame;
		}

		protected virtual void EndHeavyWindup()
		{
			CurrentState = State.Heavy_HitFrame;
		}

		protected virtual void EndLightHitFrame()
		{
			CurrentState = State.Light_Recovery;
		}

		protected virtual void EndMediumHitFrame()
		{
			CurrentState = State.Medium_Recovery;
		}

		protected virtual void EndHeavyHitFrame()
		{
			CurrentState = State.Heavy_Recovery;
		}
		
		protected virtual void EndLightRecovery()
		{
//			if (autoCombo)
//				comboTriggered = true;

			comboInProgress = comboTriggered;

			if (comboTriggered)
			{
				Strike(HitStrength.Medium, false);
			}
			else
			{
				CurrentState = State.Light_Cutoff;

				// if the opponent blocked the hit, they return to idle now
				if (Opponent != null && Opponent.IsBlockStunned)
					Opponent.StopBlockStun();		// return to idle

				CanContinue = true;	
				CurrentPriority = Default_Priority;

				if (Opponent != null)
					Opponent.ReturnToDefaultDistance();
			}
		}

		protected virtual void EndMediumRecovery()
		{
//			if (autoCombo)
//				comboTriggered = true;
			
			comboInProgress = comboTriggered;
			chainInProgress = chainedCounter || chainedSpecial;

			if (comboTriggered)	
			{
				Strike(HitStrength.Heavy, false);
//				autoCombo = false;
			}
			else if (chainedCounter)
			{
				ResetFrameCounts();
				if (Opponent != null)
					Opponent.ReturnToDefaultDistance();

				CounterAttack(true);
				chainedCounter = false;

				TriggerSpotFX(SpotFXType.Chain);
			}
			else if (chainedSpecial)
			{
				ResetFrameCounts();

				chainedSpecial = false;

				TriggerSpotFX(SpotFXType.Chain);

				CurrentMove = Move.Special;
				CurrentState = State.Special;

				if (Opponent != null)
					Opponent.ReturnToDefaultDistance();
			}
			else
			{
				CurrentState = State.Medium_Cutoff;

				// if the opponent blocked the hit, they return to idle now
				if (Opponent != null && Opponent.IsBlockStunned)
					Opponent.StopBlockStun();		// return to idle

				CanContinue = true;
				CurrentPriority = Default_Priority;

				if (Opponent != null)
					Opponent.ReturnToDefaultDistance();
			}
		}

		protected virtual void EndHeavyRecovery()
		{
			chainInProgress = chainedCounter || chainedSpecial;

			if (chainedCounter)
			{
				ResetFrameCounts();
				chainedCounter = false;

				if (Opponent != null)
					Opponent.ReturnToDefaultDistance();

				CounterAttack(true);
				TriggerSpotFX(SpotFXType.Chain);
			}
			else if (chainedSpecial)
			{
				ResetFrameCounts();
				chainedSpecial = false;

				TriggerSpotFX(SpotFXType.Chain);

				CurrentMove = Move.Special;
				CurrentState = State.Special;

				if (Opponent != null)
					Opponent.ReturnToDefaultDistance();
			}
			else
			{
				CurrentState = State.Heavy_Cutoff;

				// if the opponent blocked the hit, they return to idle now
				if (Opponent != null && Opponent.IsBlockStunned)
					Opponent.StopBlockStun();		// return to idle

				CanContinue = true;
				CurrentPriority = Default_Priority;
				performingPowerAttack = false;

				if (Opponent != null)
					Opponent.ReturnToDefaultDistance();
			}
		}

		protected virtual void EndLightCutoff()
		{
			CompleteMove();

			// if the opponent took (ie. didn't block) the hit, they return to idle now
			if (Opponent != null && Opponent.IsHitStunned)
				Opponent.StopHitStun();		// return to idle
		}

		protected virtual void EndMediumCutoff()
		{
			CompleteMove();

			// if the opponent took (ie. didn't block) the hit, they return to idle now
			if (Opponent != null && Opponent.IsHitStunned)
				Opponent.StopHitStun();		// return to idle
		}

		protected virtual void EndHeavyCutoff()
		{
			CompleteMove();

			// if the opponent took (ie. didn't block) the hit, they return to idle now
			if (Opponent != null && Opponent.IsHitStunned)
				Opponent.StopHitStun();		// return to idle
		}

		protected virtual void EndVengeance()
		{
			CompleteMove();
		}


		// counter

		protected virtual void EndCounterTaunt()			// end of state - not struck, so vulnerable
		{
			CurrentState = State.Counter_Recovery;
			CurrentPriority = Default_Priority;	
		}
		protected virtual void EndCounterTrigger()
		{
			CounterAttack(false);
		}
		protected virtual void EndCounterAttack()
		{
			CompleteMove();
		}

		protected virtual void EndCounterRecovery()
		{
			CompleteMove();
		}

		// special

		protected virtual void EndSpecialStart()
		{
			CurrentState = State.Special;
		}

		protected virtual void EndSpecial()
		{
			CurrentState = State.Special_Opportunity;
			CurrentPriority = Punishable_Priority;

			if (UnderAI || (InTraining && Trainer.CurrentStepIsCombo))
				return;

			if (! Opponent.ExpiredState)
			{
				SpecialOpportunityFeedbackFX();		// mash (fire) or swipe forward (water)
				specialOpportunityTapCount = 0;
			}
		}

		protected virtual void EndSpecialOpportunity()
		{
			if (specialExtraTriggered) 		// swiped during special opportunity - perform special extra
			{
				specialExtraTriggered = false;
				specialOpportunityTapCount = 0;
				specialExtraPerformed = true;

				CurrentState = State.Special_Extra;
				CurrentPriority = Special_Extra_Priority;

				// invoke fire or water element effect
				SpecialExtraElementFX();
			}
			else
			{
//				Debug.Log(FullName + ": EndSpecialOpportunity -->  Special_Recover");

				CurrentState = State.Special_Recover;
				CanContinue = true;
			}
				
			// cancel special opportunity feedback (sets to void state)
//			Debug.Log(FullName + "Cancel feedback: EndSpecialOpportunity");
			if (! Opponent.ExpiredState && !InTraining)
				TriggerFeedbackFX(FeedbackFXType.None);		
		}

		protected virtual void EndSpecialExtra()	
		{
			CurrentState = State.Special_Recover;
			CanContinue = true;
		}

		protected virtual void EndSpecialRecover()
		{
			CompleteMove();
		}

		protected virtual void EndShove()
		{
			CompleteMove();
		}

		// skeletron only (currently)
		protected virtual void EndIdleDamaged()
		{
			
		}

		protected virtual void EndFalling()
		{
			
		}

//		protected virtual void EndReadyToDie()
//		{
//			
//		}

		// tutorial punch (AI only)

		protected virtual void EndTutorialPunchStart()
		{
			CurrentState = State.Tutorial_Punch;
			CurrentPriority = Tutorial_Punch_Priority;	
		}

		protected virtual void EndTutorialPunch()
		{
			CurrentState = State.Tutorial_Punch_End;
			CurrentPriority = Default_Priority;	
			CanContinue = true;
		}

		protected virtual void EndTutorialPunchEnd()
		{
			CompleteMove();
		}

		#endregion 	// end state


		#region freezing


		// freeze both fighters for effect ... on next frame
		private void SetFreezeFrames(int freezeFrames)
		{
			if (freezeFrames > freezeFightFrames)
				freezeFightFrames = freezeFrames;	
		}


		// used for freezing this fighter only - fightManager.FreezeFight() freezes both in sync
		private void RomanCancelFreeze()
		{
			int freezeFrames = ProfileData.RomanCancelFreezeFrames;

//			Debug.Log(FullName + ": RomanCancelFreeze - state = " + CurrentState + ", RomanCancelFreezeFrames = " + freezeFrames + " [" + AnimationFrameCount + "]");

			if (freezeFrames > 0)
			{
				if (freezeFrames > romanCancelFreezeFramesRemaining)		// this is a new freeze or extending an existing freeze
				{
					romanCancelFreezeFramesRemaining = freezeFrames;
					romanCancelFrozen = true;
				}
				
				Freeze();
			}
		}
			
		// used for freezing this fighter only - fightManager.FreezeFight() freezes both in sync
		private void RomanCancelFreezeCountdown()
		{
			if (romanCancelFreezeFramesRemaining == 0)
			{
				Unfreeze();
				romanCancelFrozen = false;

//				if (UnderAI)
//					Debug.Log(FullName + ": END RomanCancel freeze - CurrentMove = " + CurrentMove + " -> " + NextContinuation);

				if (CurrentMove == Move.Roman_Cancel)
				{
//					Debug.Log(FullName + ": END RomanCancel freeze - State = " + CurrentState + " -> " + NextContinuation);
					CompleteMove();				// execute the move continuation if present

					if (Opponent != null)
						Opponent.ResetStunDuration(Opponent.RomanCancelStunFrames);		// if opponent is stunned

					if (OnEndRomanCancelFreeze != null)
						OnEndRomanCancelFreeze(this);
				}
			}
			else
			{
//				Debug.Log(FullName + ": frozen " + romanCancelFreezeFramesRemaining + " frames remaining" + " [" + AnimationFrameCount + "]");
				StateUI = "[ Frozen... " + romanCancelFreezeFramesRemaining + " ]";
				romanCancelFreezeFramesRemaining--;
			}
		}


		// used for freezing this fighter only - fightManager.FreezeFight() freezes both in sync
		private void PowerUpFreeze()
		{
			int freezeFrames = ProfileData.PowerUpFreezeFrames;

			if (freezeFrames > 0)
			{
				if (freezeFrames > powerUpFreezeFramesRemaining)		// this is a new freeze or extending an existing freeze
				{
					powerUpFreezeFramesRemaining = freezeFrames;
					powerUpFrozen = true;
				}

				Freeze();
			}
		}

		private void PowerUpFreezeCountdown()
		{
			if (powerUpFreezeFramesRemaining == 0)
			{
				Unfreeze();
				powerUpFrozen = false;

				if (OnEndPowerUpFreeze != null)
					OnEndPowerUpFreeze(this, CurrentState == State.Idle);
			}
			else
			{
//				Debug.Log(FullName + ": PowerUpFreezeCountdown " + powerUpFreezeFramesRemaining + " frames remaining" + " [" + AnimationFrameCount + "]");
				StateUI = "[ Frozen... " + powerUpFreezeFramesRemaining + " ]";
				powerUpFreezeFramesRemaining--;
			}
		}

		#endregion		// freezing


		#region spot FX

		private void TriggerSpotFX(SpotFXType FXType, bool x2 = false, bool randomRotation = true)
		{
//			Debug.Log(FullName + ": TriggerSpotEffect " + FXType);
			
			if (x2)
			{
				if (spotFXx2 != null)
				{
					spotFXx2.gameObject.SetActive(true);
					spotFXx2.TriggerEffect(FXType);
				}
			}
			else
			{
				if (spotFX != null)
				{
//					if (randomRotation)
//					{
//						Quaternion rotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(0, 360));
//						spotFX.movieClip.transform.rotation = rotation;	
//					}
					spotFX.gameObject.SetActive(true);
					spotFX.TriggerEffect(FXType);	
				}
			}
		}

		public void CancelSpotFX()
		{
//			Debug.Log(FullName + " CancelSpotFX");
			if (spotFX != null)
			{
				spotFX.VoidState();
				spotFX.gameObject.SetActive(false);
			}
			if (spotFXx2 != null)
			{
				spotFXx2.VoidState();
				spotFXx2.gameObject.SetActive(false);
			}
		}

		#endregion  // spot FX


		private void TriggerSmoke(SmokeFXType smoke, float xOffset = 0, float yOffset = 0)
		{
			if (smokeFX == null)
				return;

			smokeFX.gameObject.SetActive(true);
			smokeFX.TriggerSmoke(smoke, xOffset, yOffset);
		}

		public void CancelSmokeFX()
		{
//			Debug.Log(FullName + " CancelSmokeFX");
			if (smokeFX != null)
			{
				smokeFX.VoidState();
				smokeFX.gameObject.SetActive(false);
			}
		}
			

		public bool CheckGauge(Move move)
		{
			switch (move)
			{
				case Move.Counter:
					bool canCounter = HasCounterGauge || CanChain;	// gauge not required to chain counter
					if (! canCounter && ! HasCounterGauge)
						NoGaugeFeedback(ProfileData.CounterGauge);
					return canCounter;

				case Move.Vengeance:
					if (! HasVengeanceGauge)
						NoGaugeFeedback(ProfileData.VengeanceGauge);
					return HasVengeanceGauge;

				case Move.Roman_Cancel:
					if (! HasRomanCancelGauge)
						NoGaugeFeedback(ProfileData.RomanCancelGauge);
					return HasRomanCancelGauge;

				default:
					return true;			// gauge not required
			}
		}


		public bool CanExecuteMove(Move move, bool report = false)
		{
			if ((!fightManager.ReadyToFight || fightManager.EitherFighterExpired)) // && !PreviewMoves)
				return false;
			
			switch (move)
			{
				case Move.None:
					return true;

				case Move.Idle:
					return false;

				case Move.Tutorial_Punch:
					return CanStrikeLight || CanContinue;

				case Move.Strike_Light:
					return CanStrikeLight || CanContinue;

				case Move.Strike_Medium:
					return CanStrikeMedium;

				case Move.Strike_Heavy:
					return CanStrikeHeavy;

				case Move.Power_Attack:
					return CanPowerUp;

				case Move.Power_Up:
					return CanTriggerPowerUp;

				case Move.Block:
					return HasBlock && (CanBlock || CanContinue);

				case Move.ReleaseBlock:
					return HasBlock && CanReleaseBlock;

				case Move.Special:
					return CanSpecial || CanContinue;

				case Move.Counter:
//					if (report && UnderAI)
//						Debug.Log("CanExecuteMove COUNTER: HasCounterGauge = " + HasCounterGauge + " IsIdle = " + IsIdle + " CanContinue = " + CanContinue + " CanChain = " + CanChain);
					return (HasCounterGauge && (IsIdle || CanContinue)) || CanChain;	// gauge not required to chain counter

				case Move.Vengeance:
					return HasVengeanceGauge && (CanVengeance || CanContinue);

				case Move.Shove:
					return HasShove && (CanShove || CanContinue);

				case Move.Roman_Cancel:
					return HasRomanCancelGauge && CanRomanCancel;
			}

			return false;
		}


		// execute move immediately
		public bool ExecuteMove(Move move, bool continuing)
		{
			var executedFromState = CurrentState;			// for OnMoveExecuted event

			if (!fightManager.ReadyToFight || fightManager.EitherFighterExpired)
			{
				MoveOk = false;
			}
			else
			{
				// AI always releases a block before executing a move
				if (UnderAI && move != Move.ReleaseBlock)
				{
//					Debug.Log(FullName + ": AI ExecuteMove - ReleaseBlock: CanReleaseBlock = " + CanReleaseBlock + ", HoldingBlock = " + HoldingBlock + ", IsBlocking = " + IsBlocking);
					ReleaseBlock();		// only if blocking
				}
					
//				Debug.Log(FullName + ": ExecuteMove " + move + ", continuing = " + continuing);

				switch (move)
				{
					case Move.Idle:
						ReturnToIdle();
						MoveOk = true;
						break;

					case Move.Strike_Light:
						MoveOk = Strike(HitStrength.Light, continuing);
						break;

					case Move.Strike_Medium:
						MoveOk = Strike(HitStrength.Medium, continuing);
						break;

					case Move.Strike_Heavy:
						MoveOk = Strike(HitStrength.Heavy, continuing);
						break;

					case Move.Power_Attack:
						MoveOk = Strike(HitStrength.Power, continuing);
						break;

					case Move.Shove:
						MoveOk = Shove(continuing);
						break;

					case Move.Vengeance:
						MoveOk = Vengeance(continuing);
						break;

					case Move.Counter:
						MoveOk = Counter(continuing);		// taunt ... CounterAttack() if struck or if chaining 
						break;

					case Move.Special:
						MoveOk = Special(continuing);
						break;

					case Move.Block:
						MoveOk = Block(continuing);		// starts block - released when hold released
						break;

					case Move.ReleaseBlock:
						MoveOk = ReleaseBlock();
						break;

					case Move.Roman_Cancel:
						MoveOk = RomanCancel();
						break;

					case Move.Tutorial_Punch:
						MoveOk = TutorialPunch(continuing);
						break;

					case Move.Power_Up:					// used by dojo for playback
						MoveOk = PowerUp();
						break;

					default:
						MoveOk = false;
						break;
				}
			}

			if (MoveOk)
			{
				if (OnMoveExecuted != null)
				{
					var newState = new FighterChangedData(this);
					newState.ExecutedMove(move, executedFromState);
					OnMoveExecuted(newState, continuing);

//					if (UnderAI)
//						Debug.Log(FullName + " ExecuteMove: " + move);
				}
				
				// don't suspend AI countdowns for roman cancel as move was instantaneous
				bool suspendCountdown = move != Move.Roman_Cancel;

				if (UnderAI && AIController != null)
					AIController.MoveCountdownSuspended = suspendCountdown;		// until CompleteMove
			}
//			else
//			{
//				var log = FullName + ": could not " + (continuing ? "continue '" : "execute '") + move + "', State = " + CurrentState + ", CanContinue = " + CanContinue;
//				Debug.Log(log);
////				LastMoveUI = log;
//			}

//			if (UnderAI)
//				Debug.Log(FullName + ": ExecuteMove " + move + ", continuing = " + continuing);

			// clear continuations + cued move, even if move failed
			// continuation may be cued immediately on roman cancel for dojo playback at end of freeze, so don't clear it here
			bool dojoShadowContinuation = IsDojoShadow && move == Move.Roman_Cancel && HasContinuation;	
			bool AIRomanCancel = UnderAI && move == Move.Roman_Cancel;
			bool clearContinuations = !dojoShadowContinuation && !AIRomanCancel;		// kludge!
			ClearCuedMoves(clearContinuations && move != Move.ReleaseBlock); 
//			ClearCuedMoves(!dojoShadowContinuation && move != Move.ReleaseBlock); 

			return MoveOk;
		}

		// clear continuations / cued move
		private void ClearCuedMoves(bool clearContinuation = true)
		{
//			Debug.Log(FullName + ": ClearCuedMoves - clearContinuation = " + clearContinuation);
			if (clearContinuation)
				MoveContinuations.Clear(); 
			
			cuedMove = Move.None;
		}

		public void CueAIMove(Move move)
		{
			if (! UnderAI)
				return;

			if (CanCombo)				// during light / medium hit or recovery
				TriggerCombo();			// will execute at end of recovery
			else
				CueMove(move);
		}

		protected virtual void CueCounter()
		{
			if (CanContinue || IsBlocking)
				CueContinuation(Move.Counter);
			else
				CueMove(Move.Counter);
		}

		// cue move to execute on next animation frame
		public void CueMove(Move move)
		{
			if (move == Move.None)
				return;

			ClearFailedMoves();

//			Debug.Log(FullName + ": CueMove " + move + ", cuedMove = " + cuedMove);

			if (HasCuedMove)
			{
				if (cuedMove == Move.Roman_Cancel)		// roman cancel already cued supercedes all - don't overwrite
					return;

				// we don't want releasing a block to override a swipe that was recorded while blocking (holding down)
				if (move == Move.ReleaseBlock)
					return;
			}
				
			// determine if the move is valid at current state
			if (CanExecuteMove(move))
			{
				cuedMove = move;

				if (!UnderAI)
					moveCuedOk = true;
			}
			else if (move == Move.ReleaseBlock && CanReleaseBlock) 		// release block at any time (including when expired)
			{
				cuedMove = move;

				if (!UnderAI)
					moveCuedOk = true;
			}
			else
				moveCuedOk = false;

			if (!UnderAI && moveCuedOk && ! IsDojoShadow)
				fightManager.MoveCuedFeedback(moveCuedOk);
		}


		public void CompleteMove()
		{
//			Debug.Log(FullName + ": CompleteMove: " + CurrentMove + " / " + CurrentState); // + " at [" + fightManager.AnimationFrameCount + "] MoveFrameCount = " + MoveFrameCount);
				
			switch (CurrentMove)
			{
				case Move.Strike_Light:
				case Move.Strike_Medium:
				case Move.Strike_Heavy:
					comboTriggered = false;
					comboInProgress = false;
					break;

				case Move.Power_Attack:
					break;

				case Move.Shove:
					break;

				case Move.Vengeance:
					break;

				case Move.Counter:
					chainedCounter = false;
					chainInProgress = false;
					break;

				case Move.Special:
					chainedSpecial = false;
					specialExtraPerformed = false;
					chainInProgress = false;
					break;

				case Move.Roman_Cancel:
					break;

				case Move.Idle:
					break;

				case Move.Block:
				case Move.ReleaseBlock:
					break;

				case Move.Tutorial_Punch:
					break;

				case Move.None:
				default:
					break;
			}
					
			if (OnMoveCompleted != null)
			{
				var newState = new FighterChangedData(this);
				newState.CompletedMove(CurrentMove);
				OnMoveCompleted(newState);
			}

			if (Opponent != null)
			{
//				Debug.Log(FullName + " CompleteMove: " + CurrentMove + " - OPPONENT.HitComboCount = 0" + " [" + AnimationFrameCount + "]");
				Opponent.HitComboCount = 0;
			}
			
			ResetFrameCounts();				// move and state
			Attacking = false;

			// if holding down, continue into block idle
			if (HoldingBlock && ! ExpiredHealth)
				CueContinuation(Move.Block);
			
			// execute the move continuation if present
			ContinueOrIdle();
			MoveOk = true;
		}

		private void ContinueOrIdle()
		{	
//			if (UnderAI)
//				Debug.Log(FullName + " ContinueOrIdle:" + " HasContinuation = " +  HasContinuation + " CheckGauge = " + CheckGauge(NextContinuation));

			if (HasContinuation && CheckGauge(NextContinuation))	 // final check for sufficient gauge (if required)	
				ExecuteContinuation();
			else
			{
				// if returning to idle after a roman cancel (Dash state while frozen), then 'step back'
				if (CurrentMove == Move.Roman_Cancel)
					ReturnToDefaultDistance();
				
				ReturnToIdle();					// reset move includes clearing cued moves (in case continuation could not be executed because of gauge)
			}
		}

		public void ReturnToIdle()
		{
//			Debug.Log(FullName + " ReturnToIdle" + " [" + AnimationFrameCount + "]");
			if (PreviewIdle)
				return;

			ResetMove(false);

			if (UnderAI && AIController != null)
				AIController.MoveCountdownSuspended = false;		// resume countdown to next move
		}

		protected virtual void IdleState()
		{
//			Debug.Log(FullName + " IdleState");

			CurrentMove = Move.Idle;
			CurrentState = State.Idle;				// fires state change event
			CurrentPriority = Default_Priority;		// fires state change event
		}

		protected virtual void BlockIdleState()
		{
//			Debug.Log(FullName + " BlockIdleState");

			CurrentMove = Move.Block;
			CurrentState = State.Block_Idle;		// fires state change event
		}
			
		protected void ResetFrameCounts()
		{
			MoveFrameCount = 0;
			StateFrameCount = 0;
			HitFrameCount = 0;
		}

		public void Reset()
		{
			Reveal();

			ResetUI();
			ResetHealth();
			ResetMove(true);
		}

		private void ResetUI()
		{
			DebugUI = "";
			StateUI = "";

			MoveOk = true;

			fightManager.StopComboFeedback(IsPlayer1);
			fightManager.ClearComboFeedback(IsPlayer1);
			fightManager.StopStateFeedback(IsPlayer1);
			fightManager.ClearStateFeedback(IsPlayer1);
			fightManager.StopGaugeFeedback(IsPlayer1);
			fightManager.ClearGaugeFeedback(IsPlayer1);
		}
			
		public void ResetHealth(bool resetGauge = true)
		{
			if (profile == null)
				return;

			var stateChanged = new FighterChangedData(this);				// snapshot before changed!

			ProfileData.SavedData.Health = ProfileData.LevelHealth; 		// more health as level up

			// broadcast health changed event
			if (OnHealthChanged != null)
			{
				stateChanged.ChangedHealth(ProfileData.SavedData.Health);
				OnHealthChanged(stateChanged);
			}

			if (resetGauge)
			{
				ProfileData.SavedData.Gauge = 0;
				ProfileData.SavedData.GaugeDamage = 0;

				// broadcast gauge changed event
				if (OnGaugeChanged != null)
				{
					stateChanged.ChangedGauge(ProfileData.SavedData.Gauge);
					OnGaugeChanged(stateChanged, true);
				}
			}

			ArmourDown = false;
			ArmourUp = false;
			OnFire = false;
			HealthUp = false;

			damageWhileOnFire = 0;
//			damageSustained = 0;
		}


		public void ResetPosition(bool setScale = false)
		{
			transform.position = fightManager.GetFighterPosition(IsPlayer1, false, true);

			if (setScale)
				transform.localScale = fightManager.GetFighterScale(IsPlayer1);  			// scale player 1 to face right, player 2 faces left
		}

		public IEnumerator ResetPosition(float resetTime)
		{
			var resetPosition = fightManager.GetFighterPosition(IsPlayer1, false, true);
			var startPosition = transform.position;

			float t = 0.0f;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / resetTime); 	// timeScale of 1.0 == real time

				transform.position = Vector3.Lerp(startPosition, resetPosition, t);
				yield return null;
			}

//			Debug.Log(FullName + ": ResetPosition to " + transform.position);
			yield return null;
		}

		public void ResetMove(bool resetTraining)
		{
//			Debug.Log(FullName + ": ResetMove" + " [" + AnimationFrameCount + "]");

			// combos and chaining
			comboTriggered = false; 
			chainedCounter = false; 
			chainedSpecial = false; 
			specialExtraTriggered = false; 
			specialExtraPerformed = false; 
			specialOpportunityTapCount = 0;

			comboInProgress = false;
			chainInProgress = false;
//			autoCombo = false;

			HitComboCount = 0;

			hitStunFramesRemaining = 0;
			blockStunFramesRemaining = 0;

			romanCancelFreezeFramesRemaining = 0;
			powerUpFreezeFramesRemaining = 0;
			isFrozen = false;
			romanCancelFrozen = false;
			powerUpFrozen = false;

			takenLastFatalHit = false;	

			ClearCuedMoves();

			CanContinue = false;

			Attacking = false;
			Returning = false;

			MoveOk = true;

			if (UnderAI)
				InTraining = false;
			else if (resetTraining)
				InTraining = FightManager.CombatMode == FightMode.Training; // !FightManager.SavedGameStatus.CompletedBasicTraining;

			ResetFrameCounts();

			IdleState();		// skeletron may be idle_damaged

			returnToDefault = false;

			secondLifeOpportunity = false;
			secondLifeTriggered = false;

			powerUpTriggered = false;
			performingPowerAttack = false;

			// AI trigger frame counts
			idleFrameCount = 0; 
			blockIdleFrameCount = 0; 
			canContinueFrameCount = 0; 
			vengeanceFrameCount = 0; 
			gaugeIncreaseFrameCount = 0;
			stunnedFrameCount = 0;	
			lastHitFrameCount = 0;	
		}


		#region health updates

		// returns true if expired
		public bool UpdateHealth(float damage, bool updateGauge = true)
		{
			if (damage == 0)
				return false;

//			if (FightManager.IsNetworkFight)
//			{
//				if (OnUpdateHealth != null)
//					OnUpdateHealth(damage, updateGauge);
//
//				return false;
//			}

			var newState = new FighterChangedData(this); 		// snapshot before changed

			// global damage factor to change damage for all hits
			damage *= Opponent.ProfileData.HitDamageFactor;
			damage *= fightManager.HitDamageFactor;

			if (Opponent != null && Opponent.CurrentMove == Move.Power_Attack)
				damage *= ProfileData.PowerAttackDamageFactor;
			
			ProfileData.SavedData.Health -= damage;

			if (ProfileData.SavedData.Health < 0)	
				ProfileData.SavedData.Health = 0;

			if (ProfileData.SavedData.Health > ProfileData.LevelHealth)
				ProfileData.SavedData.Health = ProfileData.LevelHealth;

			// broadcast health changed event
			if (OnHealthChanged != null)
			{
				newState.ChangedHealth(ProfileData.SavedData.Health);
				OnHealthChanged(newState);
			}
				
			UpdateDamage(damage, updateGauge);

			return ExpiredHealth;
		}

			
		// negative damage to decrease gauge
		private void UpdateDamage(float damage, bool updateGauge = true)
		{
			float damageTaken = damage;		// for damage stats
			float gaugeDamage = damage;		// for gauge

//			if (! PreviewMoves && updateGauge && gaugeDamage > 0 && StaticPowerUp == FightingLegends.PowerUp.Avenger)
			if (updateGauge && gaugeDamage > 0 && StaticPowerUp == FightingLegends.PowerUp.Avenger)
			{
				gaugeDamage *= ProfileData.AvengerFactor;

				if (OnStaticPowerUpApplied != null)
					OnStaticPowerUpApplied(FightingLegends.PowerUp.Avenger);
			}
				
			if (gaugeDamage > 0)
			{
//				Debug.Log(FullName + " UpdateDamage: gaugeDamage = " + gaugeDamage);

				ProfileData.SavedData.GaugeDamage += gaugeDamage;

				if (ProfileData.SavedData.GaugeDamage < 0)
					ProfileData.SavedData.GaugeDamage = 0;

				if (ProfileData.SavedData.GaugeDamage >= ProfileData.LevelDamagePerGauge)
				{
					int gaugeGained = (int)(ProfileData.SavedData.GaugeDamage / ProfileData.LevelDamagePerGauge);

					// save remainder for next UpdateDamage
					ProfileData.SavedData.GaugeDamage = ProfileData.SavedData.GaugeDamage % ProfileData.LevelDamagePerGauge;		

//					Debug.Log(FullName + " UpdateDamage: LevelDamagePerGauge = " + ProfileData.LevelDamagePerGauge + ", GaugeDamage = " + ProfileData.SavedData.GaugeDamage + ", gaugeGained = " + gaugeGained);
					if (updateGauge)
					{
						int newGauge = ProfileData.SavedData.Gauge + gaugeGained;

						if (newGauge > maxGauge)
							newGauge = maxGauge;
						if (newGauge < 0)
							newGauge = 0;

						UpdateGauge(newGauge, true);
					}
				}
			}

			// update damage stats
			if (ProfileData != null) // && !PreviewMoves)
			{
				ProfileData.SavedData.DamageSustained += damageTaken;
				Opponent.ProfileData.SavedData.DamageInflicted += damageTaken;

				if (! UnderAI)
					FightManager.SavedGameStatus.DamageSustained += damageTaken;
				else
					FightManager.SavedGameStatus.DamageInflicted += damageTaken;
			}
		}
			
		public void UpdateGauge(int newGauge, bool stars)
		{
//			Debug.Log(FullName + ": UpdateGauge ProfileData.Gauge = " + ProfileData.SavedData.Gauge + ", newGauge = " + newGauge);

			if (ProfileData.SavedData.Gauge != newGauge)
			{
				var newState = new FighterChangedData(this);		// snapshot before changed!

				IncreasedGauge = newGauge > ProfileData.SavedData.Gauge;
				ProfileData.SavedData.Gauge = newGauge;

				if (OnGaugeChanged != null)
				{
					newState.ChangedGauge(ProfileData.SavedData.Gauge);
					OnGaugeChanged(newState, stars);
				}
			}
		}

		public void MaxGauge()
		{
			UpdateGauge(maxGauge, true);
		}

		protected void DeductGauge(int gaugeUsed)
		{
//			if (PreviewMoves && !PreviewUseGauge)
//				return;
			
			var newState = new FighterChangedData(this);		// records current gauge

			ProfileData.SavedData.Gauge -= gaugeUsed;

			if (OnGaugeChanged != null)
			{
				newState.ChangedGauge(ProfileData.SavedData.Gauge);
				OnGaugeChanged(newState, true);
			}
		}

		#endregion 		// health updates

			
		#region execution of moves

		private bool Strike(HitStrength strength, bool continuing)		// tap
		{
			if (fightManager.EitherFighterExpiredState)
				return false;

			switch (strength)
			{
				case HitStrength.Light:
//					if (!UnderAI)
//						Debug.Log(FullName + ": Strike Light at [" + fightManager.AnimationFrameCount + ", CanStrikeLight = " + CanStrikeLight + ", continuing = " + continuing + ", State = " + CurrentState);
					if (! CanStrikeLight && ! continuing)
						return false;
					break;

				case HitStrength.Medium:
					if (! CanStrikeMedium)
						return false;
					break;

				case HitStrength.Heavy:
					if (! CanStrikeHeavy)
						return false;
					break;

				case HitStrength.Power:
					if (! CanPowerUp)
						return false;
					break;

				default:		// what strength??
					return false;
			}

			ResetFrameCounts();						// reset (start of move)

			switch (strength)
			{
				default:
				case HitStrength.Light:
				{
					if (ProfileData.lightWhiff != null)
						AudioSource.PlayClipAtPoint(ProfileData.lightWhiff, Vector3.zero, FightManager.SFXVolume);

					CurrentMove = Move.Strike_Light; 
					CurrentState = State.Light_Windup; 
					CurrentPriority = Windup_Light_Priority;

					TriggerSmoke(SmokeFXType.Small);

					StartCoroutine(StrikeTravel());
					break;
				}

				case HitStrength.Medium:
				{
					if (ProfileData.mediumWhiff != null)
						AudioSource.PlayClipAtPoint(ProfileData.mediumWhiff, Vector3.zero, FightManager.SFXVolume);

					CurrentMove = Move.Strike_Medium; 
					CurrentState = State.Medium_Windup;
					CurrentPriority = Windup_Medium_Priority;
					break;
				}

				case HitStrength.Heavy:
				{
					if (ProfileData.heavyWhiff != null)
						AudioSource.PlayClipAtPoint(ProfileData.heavyWhiff, Vector3.zero, FightManager.SFXVolume);

					CurrentMove = Move.Strike_Heavy; 
					CurrentState = State.Heavy_Windup;
					CurrentPriority = Windup_Heavy_Priority;
					break;
				}
						
				case HitStrength.Power:
				{
					if (ProfileData.heavyWhiff != null)
						AudioSource.PlayClipAtPoint(ProfileData.heavyWhiff, Vector3.zero, FightManager.SFXVolume);

					CurrentMove = Move.Power_Attack; 
					CurrentState = State.Heavy_Windup;
					CurrentPriority = Power_Attack_Priority;

					PowerUpFeedback();
					break;
				}
			}

			comboTriggered = false;
			return true;
		}
			
		private bool Shove(bool continuing)			// swipe down
		{
			if (fightManager.EitherFighterExpiredState)
				return false;
			
			if (! CanShove && ! continuing)
				return false;

			CurrentMove = Move.Shove;
			ResetFrameCounts();		// reset (start of move)

			CurrentState = State.Shove;
			CurrentPriority = Default_Priority;
			CanContinue = true;

			StartCoroutine(StrikeTravel(true));
			return true;
		}

		private bool Vengeance(bool continuing)		// swipe left - right
		{
			if (fightManager.EitherFighterExpiredState)
				return false;

//			if (! UnderAI)
//				Debug.Log(FullName + ": Vengeance, CanVengeance = " + CanVengeance + ", HasVengeanceGauge = " + HasVengeanceGauge + ", Gauge = " + ProfileData.SavedData.Gauge + ", State = " + CurrentState + ", continuing = " + continuing);

			if (! CanVengeance && ! continuing)
				return false;

			if (! HasVengeanceGauge)
				return false;

			DeductGauge(ProfileData.VengeanceGauge);
				
			CurrentMove = Move.Vengeance;
			ResetFrameCounts();		// reset (start of move)

			CurrentState = State.Vengeance;
			CurrentPriority = Vengeance_Windup_Priority;

			StartCoroutine(StrikeTravel());

			TriggerSpotFX(SpotFXType.Vengeance);
			TriggerSmoke(VengeanceSmoke);		// virtual

			if (ProfileData.vengeanceWhiff != null)
				AudioSource.PlayClipAtPoint(ProfileData.vengeanceWhiff, Vector3.zero, FightManager.SFXVolume);

			return true;
		}


		private bool RomanCancel()
		{
			if (fightManager.EitherFighterExpiredState)
				return false;

//			Debug.Log(FullName + ": RomanCancel state = " + CurrentState + ", CanRomanCancel = " + CanRomanCancel + ", HasRomanCancelGauge = " + HasRomanCancelGauge);

			if (! CanRomanCancel)
				return false;

			if (! HasRomanCancelGauge)
				return false;
						
			DeductGauge(ProfileData.RomanCancelGauge);

			TriggerSpotFX(SpotFXType.Roman_Cancel, true);
			fightManager.CancelFeedbackFX();

			if (hitFlash != null)
				StartCoroutine(hitFlash.PlayBlackFlash());

			if (OnRomanCancel != null)
			{
				var newState = new FighterChangedData(this);		// records current state

				newState.RomanCancel(CurrentState);
				OnRomanCancel(newState);
			}
				
			if (ProfileData.romanCancelSound != null)
				AudioSource.PlayClipAtPoint(ProfileData.romanCancelSound, Vector3.zero, FightManager.SFXVolume);

			fightManager.StateFeedback(IsPlayer1, FightManager.Translate("reset", false, true), true, true);

			CurrentMove = Move.Roman_Cancel;		// until end of freeze
			CurrentState = State.Dash;				// until end of freeze
			CanContinue = true;						// for follow-up after freeze (eg. counter)

			RomanCancelFreeze(); 	// continue or idle after freeze

			if (Opponent != null)
				Opponent.RomanCancelFreeze();

			return true;
		}

		public void TriggerCombo()
		{
			comboTriggered = true;		// will execute at end of recovery
		}

		public void TriggerChainCounter()
		{
			chainedCounter = true;		// executes at end of recovery
		}

		private bool ChainCounter()
		{
//			Debug.Log(FullName + ": ChainCounter - CanChain = " + CanChain);
			if (CanChain)
			{
				TriggerChainCounter();		// executes at end of recovery
				StateUI = "CHAINED COUNTER! [ " + MoveFrameCount + " ]"; 

				if (!UnderAI && ! IsDojoShadow)
				{
					moveCuedOk = true;
					fightManager.MoveCuedFeedback(moveCuedOk);

					if (OnChainCounter != null)
						OnChainCounter(new FighterChangedData(this));				// snapshot
				}
			}
			return chainedCounter;
		}
			
		protected virtual bool Counter(bool continuing)
		{
			if (fightManager.EitherFighterExpiredState)
				return false;

			if (! HasCounter)
				return false;
			
			if (UnderAI && ChainCounter())		// ie. counter not executed via SwipeLeft	
				return true;					// don't counter taunt if chainedCounter
			
			if (! CanCounter && ! continuing)
				return false;

			if (! HasCounterGauge)
				return false;

			DeductGauge(ProfileData.CounterGauge);

			CurrentMove = Move.Counter;
			ResetFrameCounts();		// reset (start of move)

			CurrentState = State.Counter_Taunt; 
			CurrentPriority = Default_Priority; 		// inviting any kind of strike
			return true;
		}
			

		public void TriggerChainSpecial()
		{
			chainedSpecial = true;		// executes at end of recovery
		}

		private bool ChainSpecial()
		{
//			Debug.Log(FullName + ": ChainSpecial - CanChain = " + CanChain);

			if (CanChain)
			{
				TriggerChainSpecial();		// executes at end of recovery
				StateUI = "CHAINED SPECIAL! [ " + MoveFrameCount + " ]"; 

				if (!UnderAI && ! IsDojoShadow)
				{
					moveCuedOk = true;
					fightManager.MoveCuedFeedback(moveCuedOk);

					if (OnChainSpecial != null)
						OnChainSpecial(new FighterChangedData(this));				// snapshot
				}
			}
			return chainedSpecial;
		}


		public void TriggerSpecialExtra()
		{
			specialExtraTriggered = true;		// executes at end of special opportunity
		}

		private bool SpecialExtra()
		{
//			Debug.Log(FullName + ": SpecialExtra - CanSpecialExtra = " + CanSpecialExtra);

			if (CanSpecialExtra)
			{
				TriggerSpecialExtra();	// executes at end of special opportunity
				StateUI = "SPECIAL EXTRA! [ " + MoveFrameCount + " ]"; 
//				Debug.Log(FullName + ": specialExtraTriggered!");

				if (!UnderAI && ! IsDojoShadow)
				{
					moveCuedOk = true;
					fightManager.MoveCuedFeedback(moveCuedOk);

					if (OnSpecialExtraTriggered != null)
						OnSpecialExtraTriggered(new FighterChangedData(this));			// snapshot
				}
			}
			return specialExtraTriggered;
		}

		private bool Special(bool continuing)
		{
			if (fightManager.EitherFighterExpiredState)
				return false;

			if (UnderAI) 		// ie. special not executed via SwipeRight
			{
				if (ChainSpecial())				// if possible
					return true;
				else if (SpecialExtra())		// if possible
					return true;
			}

//			Debug.Log(FullName + ": Special - CanSpecial = " + CanSpecial + ", continuing = " + continuing);
				
			if (!CanSpecial && !continuing)
				return false;
			
			CurrentMove = Move.Special;
			ResetFrameCounts();		// reset (start of move)

			if (ProfileData.specialWhiff != null)
				AudioSource.PlayClipAtPoint(ProfileData.specialWhiff, Vector3.zero, FightManager.SFXVolume);
			
			CurrentState = State.Special_Start;
			CurrentPriority = Special_Start_Priority;

			StartCoroutine(StrikeTravel());
			TriggerSmoke(SpecialSmoke);		// virtual
			return true;
		}

			
		private bool Block(bool continuing)			// tap and hold -> release
		{
			if (fightManager.EitherFighterExpiredState)
				return false;

			// triggered only on touch hold start (released on touch hold end)

			if (! CanBlock && ! continuing)
				return false;

			StartBlock();
			return true;
		}

		private void StartBlock()
		{
			BlockIdleState();				// virtual (for skeletron)

			ResetFrameCounts();				// reset (start of move)

			if (UnderAI)
			{
				HoldingBlock = true;

				if (FightManager.OnAIBlock != null)
					FightManager.OnAIBlock();			// shove info bubble basically
			}
		}


		public bool ReleaseBlock(bool force = false)
		{
//			Debug.Log(FullName + ": ReleaseBlock: CanReleaseBlock = " + CanReleaseBlock + ", HoldingBlock = " + HoldingBlock + ", IsBlockIdle = " + IsBlockIdle);
			if (!force && !CanReleaseBlock)
				return false;

			HoldingBlock = false;

			if (IsBlockIdle)		// block idle
			{	
				CompleteMove();
			}

//			Debug.Log(FullName + ": Releaseblock, moveCuedOk = " + moveCuedOk);
			return true;
		}


		public virtual bool TutorialPunch(bool continuing)			// Ninja only
		{
			return false;
		}

		protected virtual SmokeFXType SpecialSmoke
		{
			get { return SmokeFXType.None; }
		}

		protected virtual SmokeFXType VengeanceSmoke
		{
			get { return SmokeFXType.None; }
		}

		private bool HitMissed
		{
			get
			{
				if (Opponent != null)
				{
					if (Opponent.IsIdle || Opponent.IsStunned)
						return false;
					
					if (CurrentPriority > Opponent.CurrentPriority)
						return false;
					
					if (CurrentPriority < Opponent.CurrentPriority)
						return true;

					// same priority - player wins over AI if both same class (ie. AI misses)
					if (ProfileData.FighterClass == Opponent.ProfileData.FighterClass)
						return UnderAI;
				}
					
				// different classes - power wins over speed (ie. speed misses)
				return ProfileData.FighterClass == FighterClass.Speed;
			}
		}
			
		private bool ShoveMissed
		{
			get
			{
				if (Opponent != null)
				{
					if (Opponent.IsIdle || Opponent.IsStunned)
						return false;

					if (CurrentPriority > Opponent.CurrentPriority) 		// not very likely for a shove!
					return false;

					if (CurrentPriority < Opponent.CurrentPriority)
						return true;

					if (Opponent.IsBlockIdle)		// unlike a hit, a shove will push out of block idle into a shove stun
						return false;

					// same priority - player wins over AI if both same class (ie. AI misses)
					if (ProfileData.FighterClass == Opponent.ProfileData.FighterClass)
						return UnderAI;
				}

				// different classes - power wins over speed (ie. speed misses)
				return ProfileData.FighterClass == FighterClass.Speed;
			}
		}

		// deliver hit to opponent, who will be stunned as a result
		// returns true if a successful hit (not a miss)
		private bool DeliverHit(HitFrameData hitData, float travelDistance, bool lastHit)
		{
			if (hitData == null)
				return false;
			
			if (hitData.TypeOfHit == HitType.Shove)
				return false;
			
			if (fightManager.EitherFighterExpiredState)
				return false;

			switch (hitData.State)
			{
				case State.Special:
					if (! lastHit)
						CurrentPriority = Special_Hit_Priority;
					break;

				case State.Vengeance:
					if (! lastHit)
						CurrentPriority = Vengeance_Hit_Priority;
					break;

				case State.Light_HitFrame:
					CurrentPriority = Strike_Light_Priority;
					break;

				case State.Medium_HitFrame:
					CurrentPriority = Strike_Medium_Priority;
					break;

				case State.Heavy_HitFrame:
					CurrentPriority = performingPowerAttack ? Power_Attack_Priority : Strike_Heavy_Priority;
					break;
			}

//			Debug.Log(FighterFullName + " " + hitData.TypeOfHit + " hit DELIVERED at [ " + fightManager.AnimationFrameCount + " ] frame = " + MoveFrameCount + " / " + StateFrameCount + " state = " + CurrentMove + " / " + CurrentState);

			if (HitMissed) 		// priority lower than opponent
			{
				TriggerSpotFX(SpotFXType.Miss);
				fightManager.StateFeedback(IsPlayer1, FightManager.Translate("missed"), false, true);

				if (ProfileData.missSound != null)
					AudioSource.PlayClipAtPoint(ProfileData.missSound, Vector3.zero, FightManager.SFXVolume);

//				Debug.Log(FullName + ": " + hitData.TypeOfHit + " MISS at [ " + fightManager.AnimationFrameCount + " ] frame = " + MoveFrameCount + " / " + StateFrameCount + " state = " + CurrentState);
				return false;
			}
			else  	// hit is valid - opponent takes the hit
			{	
				// invoke air or earth element effect for counter attack hits
				if (hitData.State == State.Counter_Attack)
					CounterHitElementFX();

				if (Opponent.TakeHit(hitData, travelDistance, lastHit))		// survived hit or block stun
				{
					SetFreezeFrames(hitData.FreezeFrames);	// defer synced freeze of both fighters until next frame
//					Debug.Log(FullName + ": Hit taken - freezeFightFrames = " + freezeFightFrames);

					if (hitFlash != null)
					{
						if (hitFlashCoroutine != null)
							StopCoroutine(hitFlashCoroutine);

						hitFlashCoroutine = hitFlash.PlayHitFlash();
						StartCoroutine(hitFlashCoroutine);
					}
				}

				return true;
			}
		}
			
		private bool DeliverShove(HitFrameData hitData, float travelDistance)
		{
			if (hitData == null)
				return false;

			if (hitData.TypeOfHit != HitType.Shove)
				return false;

			if (fightManager.EitherFighterExpiredState)
				return false;

//			Debug.Log(FighterFullName + ": DeliverShove at [ " + fightManager.AnimationFrameCount + " ] frame = " + MoveFrameCount + " / " + StateFrameCount + " state = " + CurrentMove + " / " + CurrentState);

			if (ShoveMissed) 		// priority lower than opponent
			{
				TriggerSpotFX(SpotFXType.Miss);		// no need for miss! text feedback .. it's just a shove
				if (ProfileData.missSound != null)
					AudioSource.PlayClipAtPoint(ProfileData.missSound, Vector3.zero, FightManager.SFXVolume);
				return false;
			}
			else
			{
				if (Opponent != null)
					Opponent.TakeShove(hitData, travelDistance);
				
				return true;
			}
		}


		// returns false if hit was fatal
		private bool TakeHit(HitFrameData hitData, float travelDistance, bool lastHit)
		{
			if (hitData == null)
				return false;

			if (hitData.TypeOfHit == HitType.Shove)
				return false;

			if (hitData.CameraShakes > 0 && cameraController != null)
				StartCoroutine(cameraController.Shake(hitData.CameraShakes));

			bool alreadyExpired = ExpiredHealth;
			bool survivedHit = true;
			bool hitBlocked = false;

			if (CurrentState == State.Fall) 	// skeletron -> ready to die
			{
				if (hitData.SoundEffect != null)
					AudioSource.PlayClipAtPoint(hitData.SoundEffect, Vector3.zero, FightManager.SFXVolume);

				Opponent.TriggerSpotFX(hitData.SpotEffect, false);
			}
			else if (CurrentState == State.Ready_To_Die)		// one last hit to FINISH HIM
			{
				takenLastFatalHit = lastHit;

				//				Debug.Log(FullName + " Ready_To_Die: takenLastFatalHit = " + takenLastFatalHit);

				if (takenLastFatalHit)
					KnockOut(); 	// virtual
				else
				{
					if (hitData.SoundEffect != null)
						AudioSource.PlayClipAtPoint(hitData.SoundEffect, Vector3.zero, FightManager.SFXVolume);

					Opponent.TriggerSpotFX(hitData.SpotEffect, false);
				}
			}
			else if (CurrentState == State.Counter_Taunt && Opponent.CurrentMove != Move.Power_Attack)			// struck during taunt (not shove)
			{
				//				Debug.Log(FighterFullName + " hit while counter taunting (" + stateData.HitDamage + ") [ " + fightManager.AnimationFrameCount + " ]");

				if (UnderAI && AIController != null)
					AIController.MoveCountdownSuspended = true;

				CurrentState = State.Counter_Trigger;
				CurrentPriority = Counter_Priority;

				// attacker gets XP for a successful, unblocked hit
				Opponent.IncreaseXP(FightManager.CounterTriggerXP);

				TriggerSpotFX(SpotFXType.Counter);
				TriggerSmoke(SmokeFXType.Counter);

				if (ProfileData.counterTriggerSound != null)
					AudioSource.PlayClipAtPoint(ProfileData.counterTriggerSound, Vector3.zero, FightManager.SFXVolume);

				// attacker goes into a shove stun .. deferred till next frame
				if (Opponent.CanBeShoved)
					Opponent.counterTriggerStun = true; 
			}
			else if (IsBlocking && Opponent.CurrentMove != Move.Power_Attack)		// can't block a power attack
			{
//				Debug.Log(FullName + " TakeHit: BLOCKING lastHit = " + lastHit + " alreadyExpired = " + alreadyExpired);

				// hit blocked - sustain block damage and go into block stun
				if (alreadyExpired)		// aleady expired, don't take damage
				{
					survivedHit = false;
					takenLastFatalHit = lastHit;			// TODO: this ok???  Steve 24/10

					if (lastHit)							// fatal blow was before last hit
					{
						KOState(hitData);					// start appropriate expiry animation according to type of hit
						StartCoroutine(ExpireToNextRound());

						if (OnKnockOut != null)
							OnKnockOut(this);
					}
					else									// expiry on last hit
						StartBlockStun(hitData);
				}
				else 
				{
					// update profiles
					ProfileData.SavedData.HitsBlocked++;					// hits successfully blocked
					Opponent.ProfileData.SavedData.BlockedHits++;			// unsuccessful hits (blocked)

					if (! UnderAI)
						FightManager.SavedGameStatus.HitsBlocked++;					// hits successfully blocked
					else
						FightManager.SavedGameStatus.BlockedHits++;					// unsuccessful hits (blocked)

					if (survivedHit = TakeDamage(hitData, true, lastHit, true))	// survived block damage
					{
						if (hitData.BlockStunFrames > 0)
						{
							StartBlockStun(hitData);
							hitBlocked = true;
						}
					}
					else			// this is the fatal blow (while blocking)
					{		
//						Debug.Log(FullName + " TakeHit: BLOCKING fatal lastHit = " + lastHit);

						takenLastFatalHit = lastHit;	// expire after freeze if last hit

						Opponent.TriggerSpotFX(SpotFXType.Guard_Crush);
						Debug.Log(FullName + " Guard Crush!");
						AudioSource.PlayClipAtPoint(ProfileData.counterTriggerSound, Vector3.zero, FightManager.SFXVolume);

						if (takenLastFatalHit)
						{
							KOState(hitData);	// start appropriate expiry animation according to type of hit
						}
						else
						{
							StartHitStun(hitData);
						}

						if (FighterName != "Skeletron") // && ! FallenState)
							KOFreeze();			// freeze for effect ... on next frame - a KO hit will freeze until KO feedback ends
					}
				}
			}
			else		// hit not blocked - sustain hit damage and go into hit stun
			{
				//				LastMoveUI = hitData.TypeOfHit.ToString().ToUpper() + " (" + hitData.HitDamage + ") TAKEN [ " + fightManager.AnimationFrameCount + " ]";
				//				Debug.Log(FighterFullName + " hit TAKEN (" + hitData.TypeOfHit + ") at frame " + fightManager.AnimationFrameCount + ", moveFrameCount: " + MoveFrameCount + ", stun duration: " + hitData.HitStunFrames + ", damage: " + hitData.HitDamage);

				if (alreadyExpired)		// aleady expired, don't take damage
				{
					survivedHit = false;
					takenLastFatalHit = lastHit;			// TODO: this ok???  Steve 24/10

					if (lastHit)				// fatal blow was before last hit
					{
						//						Debug.Log(FullName + ": last hit! (alreadyExpired) -> ReadyToExpire / ExpireToNextRound");
						KOState(hitData);						// start appropriate expiry animation according to type of hit
						StartCoroutine(ExpireToNextRound());

						if (OnKnockOut != null)
							OnKnockOut(this);
					}
					else										// expiry on last hit
						StartHitStun(hitData);
				}
				else
				{
					// update profiles
					ProfileData.SavedData.HitsTaken++;						// hits taken
					Opponent.ProfileData.SavedData.DeliveredHits++;			// successful hits delivered

					if (!UnderAI)
						FightManager.SavedGameStatus.HitsTaken++;				// hits taken
					else
						FightManager.SavedGameStatus.SuccessfulHits++;			// successful hits delivered

					if (survivedHit = TakeDamage(hitData, false, lastHit, false) && hitData.HitStunFrames > 0)	// survived hit damage
					{
						// start appropriate hit stun animation according to type of hit
						StartHitStun(hitData);

						// armour/health up/down only if survived last hit
						if (lastHit)
						{
							// last counter attack hit invokes opponent armour down / armour up
							if (hitData.State == State.Counter_Attack)
								Opponent.ArmourUpDown();

							// last special extra hit invokes opponent on fire / health up
							if (hitData.State == State.Special_Extra)
								Opponent.OnFireHealthUp();
						}

						// stop on fire if hit was not blocked
						if (Opponent.OnFire)
							Opponent.StopOnFire();		// reset on a successful hit

						// 'roll dice' for attacker to gain a coin in survival mode for a successful hit
						if (UnderAI && FightManager.CombatMode == FightMode.Survival && UnityEngine.Random.value <= gainCoinChance)
						{
							Opponent.KnockCoin(true);		// gain coin on player hit
							//							Opponent.KnockCoin(UnderAI);	// gain coin on player hit, lose coin on AI hit
						}
					}
					else			// this is the fatal blow
					{		
						takenLastFatalHit = lastHit;	// expire after freeze if last hit

						if (takenLastFatalHit)
							KOState(hitData);	// start appropriate expiry animation according to type of hit
						else
							StartHitStun(hitData);	// start appropriate hit stun animation according to type of hit

						if (FighterName != "Skeletron") // && (! FallenState)				// skeletron only
							KOFreeze();			// freeze for effect ... on next frame - a KO hit will freeze until KO feedback ends
					}
				}
			}

			if (hitBlocked)
			{
				if (ProfileData.blockedHitSound != null)
					AudioSource.PlayClipAtPoint(ProfileData.blockedHitSound, Vector3.zero, FightManager.SFXVolume);

				Opponent.TriggerSpotFX(SpotFXType.Block);

				// blocking a hit increases XP
				if (lastHit)
					IncreaseXP(FightManager.BlockXP);
			}
			else if (! FallenState)
			{
				// attacker gets XP for successful hit
				if (lastHit)
					Opponent.LastHitXP(hitData);

				if (hitData.SoundEffect != null)
					AudioSource.PlayClipAtPoint(hitData.SoundEffect, Vector3.zero, FightManager.SFXVolume);

				Opponent.TriggerSpotFX(hitData.SpotEffect, false);

				// update attacker's combo count if hit not blocked
				Opponent.IncrementComboCount();
			}

			return survivedHit;
		}

			
//		// set appropriate hit stun state and start stun timer
//		// returns false if hit was fatal
//		private bool TakeHit(HitFrameData hitData, float travelDistance, bool lastHit)
//		{
//			if (hitData == null)
//				return false;
//
//			if (hitData.TypeOfHit == HitType.Shove)
//				return false;
//
//			if (hitData.CameraShakes > 0 && cameraController != null)
//				StartCoroutine(cameraController.Shake(hitData.CameraShakes));
//
//			bool alreadyExpired = ExpiredHealth;
//			bool survivedHit = true;
//			bool hitBlocked = false;
//
//			if (CurrentState == State.Fall) 	// skeletron -> ready to die
//			{
//				if (hitData.SoundEffect != null)
//					AudioSource.PlayClipAtPoint(hitData.SoundEffect, Vector3.zero, FightManager.SFXVolume);
//
//				Opponent.TriggerSpotFX(hitData.SpotEffect, false);
//			}
//			else if (CurrentState == State.Ready_To_Die)		// one last hit to FINISH HIM
//			{
//				takenLastFatalHit = lastHit;
//
////				Debug.Log(FullName + " Ready_To_Die: takenLastFatalHit = " + takenLastFatalHit);
//
//				if (takenLastFatalHit)
//					KnockOut(); 	// virtual
//				else
//				{
//					if (hitData.SoundEffect != null)
//						AudioSource.PlayClipAtPoint(hitData.SoundEffect, Vector3.zero, FightManager.SFXVolume);
//
//					Opponent.TriggerSpotFX(hitData.SpotEffect, false);
//				}
//			}
//			else if (CurrentState == State.Counter_Taunt && Opponent.CurrentMove != Move.Power_Attack)			// struck during taunt (not shove)
//			{
////				Debug.Log(FighterFullName + " hit while counter taunting (" + stateData.HitDamage + ") [ " + fightManager.AnimationFrameCount + " ]");
//
//				if (UnderAI && AIController != null)
//					AIController.MoveCountdownSuspended = true;
//
//				CurrentState = State.Counter_Trigger;
//				CurrentPriority = Counter_Priority;
//
//				// attacker gets XP for a successful, unblocked hit
//				Opponent.IncreaseXP(FightManager.CounterTriggerXP);
//
//				TriggerSpotFX(SpotFXType.Counter);
//				TriggerSmoke(SmokeFXType.Counter);
//
//				if (ProfileData.counterTriggerSound != null)
//					AudioSource.PlayClipAtPoint(ProfileData.counterTriggerSound, Vector3.zero, FightManager.SFXVolume);
//
//				// attacker goes into a shove stun .. deferred till next frame
//				if (Opponent.CanBeShoved)
//					Opponent.counterTriggerStun = true; 
//			}
//			else if (IsBlocking && Opponent.CurrentMove != Move.Power_Attack)		// can't block a power attack
//			{
//				// hit blocked - sustain block damage and go into block stun
//				if (alreadyExpired)		// aleady expired, don't take damage
//				{
//					survivedHit = false;
//
//					if (lastHit)							// fatal blow was before last hit
//					{
//						ReadyToKO(hitData);					// start appropriate expiry animation according to type of hit
//						StartCoroutine(ExpireToNextRound());
//
//						if (FightManager.IsNetworkFight)
//							fightManager.NetworkKnockOut(IsPlayer1);
//						
//						if (OnKnockOut != null)
//							OnKnockOut(this);
//					}
//					else									// expiry on last hit
//						StartBlockStun(hitData);
//				}
//				else 
//				{
//					// update profiles
//					ProfileData.SavedData.HitsBlocked++;					// hits successfully blocked
//					Opponent.ProfileData.SavedData.BlockedHits++;			// unsuccessful hits (blocked)
//
//					if (! UnderAI)
//						FightManager.SavedGameStatus.HitsBlocked++;					// hits successfully blocked
//					else
//						FightManager.SavedGameStatus.BlockedHits++;					// unsuccessful hits (blocked)
//
//					if (survivedHit = TakeDamage(hitData, true, lastHit, true))	// survived block damage
//					{
//						if (hitData.BlockStunFrames > 0)
//						{
//							StartBlockStun(hitData);
//							hitBlocked = true;
//						}
//					}
//					else			// this is the fatal blow (while blocking)
//					{		
//						takenLastFatalHit = lastHit;	// expire after freeze if last hit
//
//						Debug.Log(FullName + ": Guard crush!!");
//
//						Opponent.TriggerSpotFX(SpotFXType.Guard_Crush);
//						AudioSource.PlayClipAtPoint(ProfileData.counterTriggerSound, Vector3.zero, FightManager.SFXVolume);
//
//						if (takenLastFatalHit)
//						{
//							ReadyToKO(hitData);	// start appropriate expiry animation according to type of hit
//						}
//						else
//						{
//							ReleaseBlock(true);
//							StartHitStun(hitData);
//						}
//
//						if (! FallenState)				// skeletron only
//							KnockOutFreeze();			// freeze for effect ... on next frame - a KO hit will freeze until KO feedback ends
//					}
//				}
//			}
//			else		// hit not blocked - sustain hit damage and go into hit stun
//			{
////				LastMoveUI = hitData.TypeOfHit.ToString().ToUpper() + " (" + hitData.HitDamage + ") TAKEN [ " + fightManager.AnimationFrameCount + " ]";
////				Debug.Log(FighterFullName + " hit TAKEN (" + hitData.TypeOfHit + ") at frame " + fightManager.AnimationFrameCount + ", moveFrameCount: " + MoveFrameCount + ", stun duration: " + hitData.HitStunFrames + ", damage: " + hitData.HitDamage);
//
//				if (alreadyExpired)		// aleady expired, don't take damage
//				{
//					survivedHit = false;
//
//					if (lastHit)				// last hit is not fatal blow
//					{
////						Debug.Log(FullName + ": last hit! (alreadyExpired) -> ReadyToExpire / ExpireToNextRound");
//						ReadyToKO(hitData);						// start appropriate expiry animation according to type of hit
//						StartCoroutine(ExpireToNextRound());
//
//						if (OnKnockOut != null)
//							OnKnockOut(this);
//					}
//					else										// expiry on last hit
//						StartHitStun(hitData);
//				}
//				else
//				{
//					// update profiles
//					ProfileData.SavedData.HitsTaken++;						// hits taken
//					Opponent.ProfileData.SavedData.DeliveredHits++;			// successful hits delivered
//
//					if (!UnderAI)
//						FightManager.SavedGameStatus.HitsTaken++;				// hits taken
//					else
//						FightManager.SavedGameStatus.SuccessfulHits++;			// successful hits delivered
//						
//					if (survivedHit = TakeDamage(hitData, false, lastHit, false) && hitData.HitStunFrames > 0)	// survived hit damage
//					{
//						// start appropriate hit stun animation according to type of hit
//						StartHitStun(hitData);
//
//						// armour/health up/down only if survived last hit
//						if (lastHit)
//						{
//							// last counter attack hit invokes opponent armour down or armour up
//							if (hitData.State == State.Counter_Attack)
//								Opponent.ArmourUpDown();
//
//							// last special extra hit invokes opponent on fire or health up
//							if (hitData.State == State.Special_Extra)
//								Opponent.OnFireHealthUp();
//						}
//
//						// stop on fire if hit was not blocked
//						if (Opponent.OnFire)
////							Opponent.StopOnFire();		// reset on a successful hit
//							Opponent.StopCurrentStatusEffect();		// stops timer (timed in animation frames, so synced in network fight)
//
//						// 'roll dice' for attacker to gain a coin in survival mode for a successful hit
//						if (UnderAI && FightManager.CombatMode == FightMode.Survival && UnityEngine.Random.value <= gainCoinChance)
//						{
//							Opponent.KnockCoin(true);		// gain coin on player hit
////							Opponent.KnockCoin(UnderAI);	// gain coin on player hit, lose coin on AI hit
//						}
//					}
//					else			// this is the fatal blow
//					{		
//						takenLastFatalHit = lastHit;	// expire after freeze if last hit
//
//						if (takenLastFatalHit)
//							ReadyToKO(hitData);	// start appropriate expiry animation according to type of hit
//						else
//							StartHitStun(hitData);	// start appropriate hit stun animation according to type of hit
//
//						if (! FallenState)				// skeletron only
//							KnockOutFreeze();			// freeze for effect ... on next frame - a KO hit will freeze until KO feedback ends
//					}
//				}
//			}
//
//			if (hitBlocked)
//			{
//				if (survivedHit)
//				{
////					Debug.Log("TakeHit hitBlocked = true / survivedHit = true");
//					Opponent.TriggerSpotFX(SpotFXType.Block);
//					AudioSource.PlayClipAtPoint(ProfileData.blockedHitSound, Vector3.zero, FightManager.SFXVolume);
//				}
//					
//				// blocking a hit increases XP
//				if (lastHit)
//					IncreaseXP(FightManager.BlockXP);
//			}
//			else if (! FallenState)
//			{
//				// attacker gets XP for successful hit
//				if (lastHit)
//					Opponent.LastHitXP(hitData);
//
//				if (survivedHit)
//				{
//					if (hitData.SoundEffect != null)
//						AudioSource.PlayClipAtPoint(hitData.SoundEffect, Vector3.zero, FightManager.SFXVolume);
//					
//					Opponent.TriggerSpotFX(hitData.SpotEffect, false);
//				}
//
//				// update attacker's combo count if hit not blocked
//				Opponent.IncrementComboCount();
//			}
//
//			return survivedHit;
//		}


		private void IncrementComboCount()
		{
			HitComboCount++;

			if (HitComboCount > 1)
			{
//				string feedback = HitComboCount + " " + FightManager.Translate("hitCombo");

//				if ((!InTraining || Trainer.CurrentStepIsCombo) && Opponent != null && !Opponent.InTraining)
				if (FightManager.CombatMode != FightMode.Training || Trainer.CurrentStepIsCombo)
					fightManager.ComboFeedback(IsPlayer1, HitComboCount);

				if (HitComboCount == 20)
				{
					IncreaseXP(FightManager.Combo20XP);
					if (IsPlayer1)
						FightManager.IncreaseKudos(FightManager.KudosCombo20);
				}
				else if (HitComboCount == 30)
				{
					IncreaseXP(FightManager.Combo30XP);
					if (IsPlayer1)
						FightManager.IncreaseKudos(FightManager.KudosCombo30);
				}
				else if (HitComboCount == 40)
				{
					IncreaseXP(FightManager.Combo40XP);
					if (IsPlayer1)
						FightManager.IncreaseKudos(FightManager.KudosCombo40);
				}
				else if (HitComboCount == 50)
				{
					IncreaseXP(FightManager.Combo50XP);
					if (IsPlayer1)
						FightManager.IncreaseKudos(FightManager.KudosCombo50);
				}
			}
		}

		private void TakeShove(HitFrameData hitData, float travelDistance)
		{
			if (hitData == null)
				return;

			if (hitData.TypeOfHit != HitType.Shove)
				return;

//			Debug.Log(FighterFullName + ": TakeShove");

			// recipient is shove stunned 
			// a blocked shove will push the recipient from block idle into a shove stun
			// ... but for longer than usual (uses blockStunFrames)
			StartShoveStun(hitData, IsBlockIdle);

			Opponent.TriggerSpotFX(hitData.SpotEffect, false);

			if (hitData.SoundEffect != null)
				AudioSource.PlayClipAtPoint(hitData.SoundEffect, Vector3.zero, FightManager.SFXVolume);

			if (IsPlayer2)
				fightManager.ShoveKudos(false);		// shoving opponent
			else
				fightManager.ShoveKudos(true);		// receiving shove from opponent
		}
			
		private float LevelDamage(HitFrameData hitData)
		{
			float damage = hitData.HitDamage;
			damage += (damage * ProfileData.LevelFactor);
			return damage;
		}

		// returns false if damage was fatal
		private bool TakeDamage(HitFrameData hitData, bool blockStun, bool lastHit, bool hitBlocked)
		{
			// block damage is less than hit damage
			float damage = blockStun ? hitData.BlockDamage : hitData.HitDamage;

			if (damage <= 0)
				return true;

			if (ArmourDown)
				damage *= ProfileData.ArmourDownDamageFactor;
			else if (ArmourUp)
				damage *= ProfileData.ArmourUpDamageFactor;

			if (hitBlocked && Opponent.StaticPowerUp == FightingLegends.PowerUp.ArmourPiercing)
			{
				damage *= ProfileData.ArmourPiercingFactor;

				if (Opponent.OnStaticPowerUpApplied != null)
					Opponent.OnStaticPowerUpApplied(FightingLegends.PowerUp.ArmourPiercing);
			}

			damage += (damage * Opponent.ProfileData.LevelFactor);

			bool expired = UpdateHealth(damage);

			// show damage taken in UI
			if (damage > 0 && fighterUI != null)
				fighterUI.FighterUIText(IsPlayer1, Mathf.RoundToInt(damage).ToString());

			// update kudos (for both delivering and receiving hit), factoring in priority
			if (IsPlayer2)
				fightManager.DamageKudos(damage, CurrentPriority, false, hitBlocked);		// more kudos for attacker (opponent received hit)
			else
				fightManager.DamageKudos(damage, CurrentPriority, true, hitBlocked);		// less kudos if on receiving end of opponent hit

			if (Opponent.OnDamageInflicted != null)
				Opponent.OnDamageInflicted(damage);
			
			return !expired; 
		}

		private void KnockCoin(bool gainCoin)
		{
			if (fighterUI != null)
				fighterUI.FighterUICoin(! IsPlayer1);		// 'knock' coin from opponent

			if (gainCoin)
				FightManager.Coins++;
			else
				FightManager.Coins--;

			fightManager.CoinAudio();
		}

			
		private void ArmourUpDown()
		{
			if (profile == null)
				return;

			var xOffset = IsPlayer1 ? feedbackOffsetX : -feedbackOffsetX;

			if (IsAirElement)
			{
				Opponent.StartArmourDown(-xOffset);	// opponent
			}
			else if (IsEarthElement)
			{
				StartArmourUp(xOffset);				// player
			}
		}

		private void StartArmourDown(float xOffset)
		{
			if (InTraining || Opponent.InTraining)
				return;

//			textureAnimator.SetTrigger("StartArmourDown");p
			StopCurrentStatusEffect();
			fightManager.TriggerFeedbackFX(FeedbackFXType.Armour_Down, xOffset, feedbackOffsetY);
			ArmourDown = true; 
			fightManager.StateFeedback(IsPlayer1, FightManager.Translate("armourDown"), false, true);

			StartStatusEffectFrameCount(StatusEffect.ArmourDown);			// timed in animation frames (so synced in network fight)
		}

		private void StartArmourUp(float xOffset)
		{
			if (InTraining || Opponent.InTraining)
				return;

//			textureAnimator.SetTrigger("StartArmourUp");
			StopCurrentStatusEffect();
			fightManager.TriggerFeedbackFX(FeedbackFXType.Armour_Up, xOffset, feedbackOffsetY);
			ArmourUp = true;
			fightManager.StateFeedback(IsPlayer1, FightManager.Translate("armourUp"), true, false);

			StartStatusEffectFrameCount(StatusEffect.ArmourUp);			// timed in animation frames (so synced in network fight)
		}
			
		private void StartOnFire(float xOffset)
		{
			if (InTraining || Opponent.InTraining)
				return;

			textureAnimator.SetTrigger("StartOnFire");
			StopCurrentStatusEffect();
			fightManager.TriggerFeedbackFX(FeedbackFXType.On_Fire, xOffset, feedbackOffsetY);
			OnFire = true;
			damageWhileOnFire = 0;
			fightManager.StateFeedback(IsPlayer1, FightManager.Translate("onFire"), false, true);

			StartStatusEffectFrameCount(StatusEffect.OnFire);			// timed in animation frames (so synced in network fight)
		}
			
		private void StartHealthUp(float xOffset)
		{
			if (InTraining || Opponent.InTraining)
				return;

			textureAnimator.SetTrigger("StartHealthUp");
			StopCurrentStatusEffect();
			fightManager.TriggerFeedbackFX(FeedbackFXType.Health_Up, xOffset, feedbackOffsetY);
			HealthUp = true;
			fightManager.StateFeedback(IsPlayer1, FightManager.Translate("healthUp"), true, false);

			var healthBoost = ProfileData.HealthUpBoost + (ProfileData.HealthUpBoost * ProfileData.LevelFactor);
			UpdateHealth(-healthBoost);		// single one-off boost in health

			StartStatusEffectFrameCount(StatusEffect.HealthUp);			// timed in animation frames (so synced in network fight)
		}

		public void StopArmourDown()
		{
			if (! ArmourDown)
				return;

			textureAnimator.SetTrigger("StopArmourDown");
//			fightManager.CancelFeedbackFX();
			fightManager.StopStateFeedback(IsPlayer1);
			fightManager.ClearStateFeedback(IsPlayer1);
			ArmourDown = false; 
		}

		public void StopArmourUp()
		{
			if (! ArmourUp)
				return;

			textureAnimator.SetTrigger("StopArmourUp");
//			fightManager.CancelFeedbackFX();
			fightManager.StopStateFeedback(IsPlayer1);
			fightManager.ClearStateFeedback(IsPlayer1);
			ArmourUp = false;
		}

		public void StopOnFire()
		{
			if (! OnFire)
				return;

			textureAnimator.SetTrigger("StopOnFire");
//			fightManager.CancelFeedbackFX();
			fightManager.StopStateFeedback(IsPlayer1);
			fightManager.ClearStateFeedback(IsPlayer1);

			if (damageWhileOnFire > 0)
			{
				if (FightManager.SavedGameStatus.CompletedBasicTraining && fighterUI != null)
					fighterUI.FighterUIText(IsPlayer1, (Mathf.RoundToInt(damageWhileOnFire)).ToString());
			}

			OnFire = false;
//			fightManager.HealthDebugText(IsPlayer1, currentStatusEffect.ToString() + ": " + StatusEffectStartFrame + " - " + AnimationFrameCount + " damage: " + damageWhileOnFire);
			damageWhileOnFire = 0;
		}

		public void StopHealthUp()
		{
			if (! HealthUp)
				return;

			textureAnimator.SetTrigger("StopHealthUp");
			fightManager.StopStateFeedback(IsPlayer1);
			fightManager.ClearStateFeedback(IsPlayer1);
//			fightManager.CancelFeedbackFX();
			HealthUp = false;
		}

			
		private void CounterHitElementFX()
		{
			if (profile == null || elementsFX == null)
				return;
			
			if (IsAirElement)
			{
				elementsFX.gameObject.SetActive(true);
				elementsFX.TriggerElementEffect(FighterName, FighterElement.Air);	// FX may vary by fighter
				if (ProfileData.airSound != null)
					AudioSource.PlayClipAtPoint(ProfileData.airSound, Vector3.zero, FightManager.SFXVolume);
			}
			else if (IsEarthElement)
			{
				elementsFX.gameObject.SetActive(true);
				elementsFX.TriggerElementEffect(FighterName, FighterElement.Earth);
				if (ProfileData.earthSound != null)
					AudioSource.PlayClipAtPoint(ProfileData.earthSound, Vector3.zero, FightManager.SFXVolume);
			}
		}

		private void SpecialExtraElementFX()
		{
			if (profile == null || elementsFX == null)
				return;

			if (IsFireElement)
			{
				elementsFX.gameObject.SetActive(true);
				elementsFX.TriggerElementEffect(FighterName, FighterElement.Fire);
				if (ProfileData.fireSound != null)
					AudioSource.PlayClipAtPoint(ProfileData.fireSound, Vector3.zero, FightManager.SFXVolume);
			}
			else if (IsWaterElement)
			{
				elementsFX.gameObject.SetActive(true);
				elementsFX.TriggerElementEffect(FighterName, FighterElement.Water);
				if (ProfileData.waterSound != null)
					AudioSource.PlayClipAtPoint(ProfileData.waterSound, Vector3.zero, FightManager.SFXVolume);
			}
		}

		public void CancelElementFX()
		{
			if (elementsFX != null)
			{
				elementsFX.VoidState();
				elementsFX.gameObject.SetActive(false);
			}
		}

		public string ElementsLabel
		{
			get
			{
				var element1 = ProfileData.Element1;
				var element2 = ProfileData.Element2;

				string elementsLabel = "";
				if (element1 != FighterElement.Undefined)
					elementsLabel = FightManager.Translate(element1.ToString().ToLower());
				if (element2 != FighterElement.Undefined)
					elementsLabel += " & " + FightManager.Translate(element2.ToString().ToLower());

//				if (elementsLabel == "")
//					elementsLabel = FightManager.Translate("na");		// N/A

				return elementsLabel;
			}
		}

		public string ClassLabel
		{
			get
			{
				string classLabel = "";

				if (ProfileData.FighterClass != FighterClass.Undefined)
					classLabel = FightManager.Translate(ProfileData.FighterClass.ToString().ToLower());

				return classLabel;
			}
		}

		private void OnFireHealthUp()
		{
			if (profile == null)
				return;

			if (fightManager.EitherFighterExpiredHealth)
				return;

//			Debug.Log("OnFireHealthUp: IsFireElement = " + IsFireElement);

			var xOffset = IsPlayer1 ? feedbackOffsetX : -feedbackOffsetX;

			if (IsFireElement)
			{
				Opponent.StartOnFire(-xOffset);		// opponent
			}
			else if (IsWaterElement)
			{
				StartHealthUp(xOffset);				// player
			}
		}

		private void SpecialOpportunityFeedbackFX()
		{
			if (profile == null)
				return;

			if (fightManager.EitherFighterExpiredHealth)
				return;

			if (IsDojoShadow)
				return;

			if (InTraining)
				return;

//			var xOffset = IsPlayer1 ? feedbackOffsetX : -feedbackOffsetX;
			var xOffset = -feedbackOffsetX;

			if (! InTraining || Trainer.CurrentStepIsCombo)		// show special extra feedback when in combo trainin
			{
				if (IsFireElement)
					fightManager.TriggerFeedbackFX(FeedbackFXType.Mash, xOffset, 0, null);	
				else if (IsWaterElement)
				{
					xOffset -= feedbackSwipeOffsetX;		// nearer to centre
					fightManager.TriggerFeedbackFX(FeedbackFXType.Swipe_Forward, xOffset, 0, null);
				}
			}
		}
	

		// recoil deferred until after freeze
		public void RecoilFromAttack()
		{
//			if (PreviewMoves)
//				return;
			
			if (returnToDefault)
			{
				ReturnToDefaultDistance();		// default fighting distance
				returnToDefault = false;
			}
		}

	
		private bool IsAtDefaultDistance()
		{
			return transform.position.x == fightManager.GetRelativeDefaultPosition(IsPlayer1).x;
		}
			

		// return to default position on return to idle
		public void ReturnToDefaultDistance()
		{
			if (fightManager.EitherFighterExpiredState)
				return;
			
			if (Returning)
				return;

			var defaultPosition = fightManager.GetRelativeDefaultPosition(IsPlayer1);	// relative to opponent

			StartCoroutine(TravelToDefault(defaultPosition));
		}


		// return to default fighting distance
		private IEnumerator TravelToDefault(Vector3 targetPosition)
		{	
			Returning = true;
			var travelTime = RecoilTravelTime;
				
			// scale travelTime according to animation speed
			travelTime /= fightManager.AnimationSpeed;

			var startPosition = transform.position;

			float t = 0.0f;

			while (t < 1.0f)
			{
				if (isFrozen)
					yield return null;
				
				t += Time.deltaTime * (Time.timeScale / travelTime); 

				transform.position = Vector3.Lerp(startPosition, targetPosition, t);

				yield return null;
			}

			Returning = false;
			yield return null;
		}

		// step in to striking distance
		protected IEnumerator StrikeTravel(bool immediate = false)
		{	
			if (Attacking)  		// already on the move!
				yield break;

			// wait for unfreeze or opponent to stop travelling
			while (isFrozen || Returning || Opponent.Attacking || Opponent.Returning)
			{
				yield return null;
			}
				
			var targetPosition = fightManager.GetRelativeStrikePosition(IsPlayer1);		// relative to opponent

			if (transform.position == targetPosition)
				yield break;

			Attacking = true;

			if (immediate)
			{
				transform.position = targetPosition;
			}
			else
			{
				var travelTime = StrikeTravelTime;

				// scale travelTime according to animation speed
				travelTime /= fightManager.AnimationSpeed;

				var startPosition = transform.position;
				float t = 0.0f;

				while (t < 1.0f)
				{
					t += Time.deltaTime * (Time.timeScale / travelTime); 
					transform.position = Vector3.Lerp(startPosition, targetPosition, t);
					yield return null;
				}
			}

			Attacking = false;
			yield return null;
		}


//		public IEnumerator SwitchPosition(float switchTime, Vector3 targetPosition)
//		{	
//			if (switchTime <= 0)
//				yield break;
//
//			var startPosition = transform.position;
//
//			var startScale = new Vector3(transform.localScale.x, transform.localScale.y, transform.localScale.z);
//			var targetScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
//
//			float t = 0.0f;
//
//			while (t < 1.0f)
//			{
//				t += Time.deltaTime * (Time.timeScale / switchTime); 	// timeScale of 1.0 == real time
//
//				transform.localScale = Vector3.Lerp(startScale, targetScale, t);
//				transform.position = Vector3.Lerp(startPosition, targetPosition, t);
//				yield return null;
//			}
//			yield return null;
//		}
			

		public IEnumerator Slide(float slideTime, Vector3 targetPosition)
		{	
			if (slideTime <= 0)
				yield break;

			TriggerSmoke(SmokeFXType.Small);

			var startPosition = transform.position;
			float t = 0.0f;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / slideTime); 	// timeScale of 1.0 == real time

				transform.position = Vector3.Lerp(startPosition, targetPosition, t);
				yield return null;
			}
				
			TriggerSmoke(SmokeFXType.Small);
			yield return null;
		}
	

		private bool LMH_HitFrame(HitFrameData hitData)		// light -> medium -> heavy
		{
			return (hitData.State == State.Light_HitFrame ||
					hitData.State == State.Medium_HitFrame ||
					hitData.State == State.Heavy_HitFrame);
		}
	


		private void StartHitStun(HitFrameData hitData, bool useBlockStunFrames = false)
		{
//			Debug.Log(FullName + ": StartHitStun state = " + CurrentState + " inPowerAttack = " + inPowerAttack);

			if (hitData.TypeOfHit == HitType.Shove)
				return;

			switch (hitData.TypeOfHit)
			{
				case HitType.Hook:
					CurrentState = State.Hit_Stun_Hook;
					TriggerSmoke(Opponent.performingPowerAttack ? SmokeFXType.Counter : SmokeFXType.Hook);
					break;

				case HitType.Mid:
					CurrentState = State.Hit_Stun_Mid;
					TriggerSmoke(Opponent.performingPowerAttack ? SmokeFXType.Counter : SmokeFXType.Mid);
					break;

				case HitType.Straight:
					CurrentState = State.Hit_Stun_Straight;
					TriggerSmoke(Opponent.performingPowerAttack ? SmokeFXType.Counter : SmokeFXType.Straight);
					break;

				case HitType.Uppercut:
					CurrentState = State.Hit_Stun_Uppercut;
					TriggerSmoke(Opponent.performingPowerAttack ? SmokeFXType.Counter : SmokeFXType.Uppercut);
					break;

				case HitType.None:
				default:
					break;
			}

			CurrentPriority = Default_Priority;
			CanContinue = true;	
			stunnedFrameCount = 0;

			// start the hit stun timer
			// for L-M-H hits, the hit stun lasts until the attacker returns to idle,
			// otherwise according to hitData.HitStunFrames
			if (LMH_HitFrame(hitData))
			{
				var hitStunState = LookupCurrentAnimation;

				hitStunFramesRemaining = hitStunState != null ? hitStunState.StateLength : hitData.HitStunFrames;

//				Debug.Log(FullName + ": StartHitStun - hitStunFramesRemaining = " + hitStunFramesRemaining);
			}
			else 		// special / counter / vengeance
			{
				hitStunFramesRemaining = useBlockStunFrames ? hitData.BlockStunFrames : hitData.HitStunFrames;

				if (FightManager.CombatMode == FightMode.Survival || FightManager.CombatMode == FightMode.Challenge)
				{
					if (Opponent != null && Opponent.StaticPowerUp == FightingLegends.PowerUp.PoiseWrecker)
					{
						hitStunFramesRemaining = (int)((float)hitStunFramesRemaining * ProfileData.PoiseWreckerFactor);

						if (Opponent.OnStaticPowerUpApplied != null)
							Opponent.OnStaticPowerUpApplied(FightingLegends.PowerUp.PoiseWrecker);
					}
				
					if (StaticPowerUp == FightingLegends.PowerUp.PoiseMaster)
					{
						hitStunFramesRemaining = (int)((float)hitStunFramesRemaining * ProfileData.PoiseMasterFactor);

						if (OnStaticPowerUpApplied != null)
							OnStaticPowerUpApplied(FightingLegends.PowerUp.PoiseMaster);
					}
				}

//				Debug.Log(FullName + ": StartHitStun state = " + CurrentState + ", hitStunFramesRemaining = " + hitStunFramesRemaining);
			}
				
			if (OnHitStun != null)
			{
				var newState = new FighterChangedData(this);
				newState.Stun(CurrentState);
				OnHitStun(newState);
			}

			if (UnderAI && AIController != null)
				AIController.MoveCountdownSuspended = true;		// until stun complete
		}


		private void StartShoveStun(HitFrameData hitData, bool useBlockStunFrames = false)
		{
			if (hitData.TypeOfHit != HitType.Shove)
				return;

			if (!CanBeShoved)			// just in case - shouldn't get this far!
				return;
			
			CurrentState = State.Shove_Stun;
			CurrentPriority = Default_Priority;
			CanContinue = true;

			hitStunFramesRemaining = useBlockStunFrames ? hitData.BlockStunFrames : hitData.HitStunFrames;
//			Debug.Log(FullName + ": StartShoveStun useBlockStunFrames = " + useBlockStunFrames + ", stunframes = " + hitStunFramesRemaining);

			if (OnShoveStun != null)
			{
				var newState = new FighterChangedData(this);
				newState.Stun(CurrentState);
				OnShoveStun(newState);
			}
			
			if (UnderAI && AIController != null)
				AIController.MoveCountdownSuspended = true;		// until stun complete
		}


		// called each animation frame while stun timer still running
		// note: also applies to shove stun, which uses hitStunFramesRemaining
		private void HitStunCountdown()
		{
			if (isFrozen)
				return;
			
			if (hitStunFramesRemaining == 0)
			{
				// end stun animation (back to idle)
				StopHitStun();
			}
			else
			{
				StateUI = "[ Hit stunned... " + hitStunFramesRemaining + " ] " + CurrentState;
//				Debug.Log(FullName + ": hitStunFramesRemaining = " + hitStunFramesRemaining + " [" + fightManager.AnimationFrameCount + "]");
				hitStunFramesRemaining--;
			}
		}

		// also applies to shove stun, which uses hitStunFramesRemaining
		private void StopHitStun()
		{
			hitStunFramesRemaining = 0;

//			Debug.Log(FullName + ": StopHitStun at [" + fightManager.AnimationFrameCount + "]");
			CompleteMove();		// return to idle and execute a queued move if applicable

			CanContinue = false;
		}


		// hitData represents the hit that was delivered but blocked
		private void StartBlockStun(HitFrameData hitData)
		{
			if (hitData.BlockStunFrames > 0)
			{
//				Debug.Log(FighterFullName + ": StartBlockStun (" + hitData.BlockStunFrames + ") at [" + fightManager.AnimationFrameCount + "]");

				CurrentState = State.Block_Stun;
				CurrentPriority = Default_Priority;
				CanContinue = true;

				TriggerSmoke(SmokeFXType.Small);

				// start the block stun timer
				// for L-M-H hits, the hit stun lasts until the attacker reaches a cutoff,
				// otherwise according to hitData.BlockStunFrames
				if (LMH_HitFrame(hitData))
				{
					var blockStunState = LookupCurrentAnimation;
					blockStunFramesRemaining = blockStunState != null ? blockStunState.StateLength : hitData.BlockStunFrames;
				}
				else
					blockStunFramesRemaining = hitData.BlockStunFrames;

				if (OnBlockStun != null)
				{
					var newState = new FighterChangedData(this);
					newState.Stun(CurrentState);
					OnBlockStun(newState);
				}

				if (UnderAI && AIController != null)
					AIController.MoveCountdownSuspended = true;		// until stun complete
			}
		}

		// called each animation frame while blockStunned and block stun timer still running
		private void BlockStunCountdown()
		{
			if (isFrozen)
				return;
			
			if (blockStunFramesRemaining == 0)
			{
				// end stun animation (back to idle or block idle if holding)
//				Debug.Log(FighterFullName + ": BlockStunCountDown - StopBlockStun");
				StopBlockStun();
			}
			else
			{
				StateUI = "[ Block stunned... " + blockStunFramesRemaining + " ]";
//				Debug.Log(PlayerName + ": blockStunFramesRemaining = " + blockStunFramesRemaining);
				blockStunFramesRemaining--;
			}
		}
			
		public void StopBlockStun()
		{
			blockStunFramesRemaining = 0;

			// if holding when released from a block stun, return to block idle
			if (HoldingBlock)
			{
				StartBlock();
				CurrentPriority = Default_Priority;
			}
			else
				CompleteMove();		// return to idle and execute a queued move if applicable

			CanContinue = false;

//			Debug.Log(FullName + ": StopBlockStun at [" + fightManager.AnimationFrameCount + "]");
		}


		// attacker goes into a shove stun when he/she triggers a counter attack
		private void StartCounterTriggerStun()
		{
			CurrentState = State.Shove_Stun;
			CanContinue = true;

			// start the shove stun timer - will only last until counter attack hit arrives...
			var shoveStunState = LookupCurrentAnimation;
			hitStunFramesRemaining = shoveStunState.StateLength;

			if (UnderAI && AIController != null)
				AIController.MoveCountdownSuspended = true;		// until stun complete
		}

			
		// if the opponent is stunned when a fighter roman cancels, we need to impart a new stun 
		// duration to prevent an endless stun, in case we roman cancelled out of a L/M/H
		protected int RomanCancelStunFrames
		{
			get
			{
				if (IsHitStunned)
				{
					switch (ProfileData.FighterClass)
					{
						case FighterClass.Power:
							return 18;

						case FighterClass.Speed:
							return 12;

						case FighterClass.Boss:
							return 12;
					}
				}

				if (IsBlockStunned)
				{
					switch (ProfileData.FighterClass)
					{
						case FighterClass.Power:
							return 6;

						case FighterClass.Speed:
							return 4;

						case FighterClass.Boss:
							return 4;
					}
				}

				return 0;
			}
		}

		private void ResetStunDuration(int stunFrames)
		{
			if (IsHitStunned)
				hitStunFramesRemaining = stunFrames;
			else if (IsBlockStunned)
				blockStunFramesRemaining = stunFrames;

//			Debug.Log(FullName + ": ResetStunDuration - hitStunFramesRemaining = " + hitStunFramesRemaining + ", blockStunFramesRemaining = " + blockStunFramesRemaining);

			// shove stun plays out without interference
		}
			

		// loser departs ...
		private IEnumerator ExpireToNextRound()
		{	
//			Debug.Log(FullName + ": ExpireToNextRound: ExpiredState = " + ExpiredState);
			if (! ExpiredState)
				yield break;

			if (secondLifeTriggered)	// reinstate health, reset UI and move and continue fighting
			{
				fightManager.TriggerFeedbackFX(FeedbackFXType.OK);
				AudioSource.PlayClipAtPoint(fightManager.OKSound, Vector3.zero, FightManager.SFXVolume);

				Reset();
				ReturnToIdle();
				yield break;
			}

			// loser's power-ups expire
			if (FightManager.CombatMode != FightMode.Arcade && !UnderAI && !InTraining && FightManager.CombatMode != FightMode.Dojo)
			{
				ProfileData.SavedData.StaticPowerUp = FightingLegends.PowerUp.None;
				ProfileData.SavedData.TriggerPowerUp = FightingLegends.PowerUp.None;

				ProfileData.SavedData.TriggerPowerUpCoolDown = 0;
			}

			// update kudos - for winning or losing
			if (IsPlayer2)
				fightManager.KnockOutKudos(false);			// more kudos for KO'ing opponent
			else
				fightManager.KnockOutKudos(true);			// less kudos for being knocked out by opponent

			var winner = Opponent;

			// update scores
			if (! secondLifeTriggered && ! Opponent.InTraining)
			{
				if (FightManager.CombatMode != FightMode.Dojo && FightManager.CombatMode != FightMode.Training)
				{
					ProfileData.SavedData.RoundsLost++;
					ProfileData.SavedData.MatchRoundsLost++;

					winner.ProfileData.SavedData.RoundsWon++;
					winner.ProfileData.SavedData.MatchRoundsWon++;

					if (! UnderAI) 		// player lost
						FightManager.SavedGameStatus.RoundsLost++;
					else 				// AI lost
						FightManager.SavedGameStatus.RoundsWon++;
				}

				if (FightManager.CombatMode != FightMode.Challenge && FightManager.CombatMode != FightMode.Dojo)
				{
					if (winner.OnScoreChanged != null)
						winner.OnScoreChanged(winner.ProfileData.SavedData.MatchRoundsWon);
				}
			}
				
			var travelTime = ProfileData.ExpiryTime;

			if (FightManager.CombatMode == FightMode.Survival || FightManager.CombatMode == FightMode.Challenge)
				travelTime *= fastExpiryFactor;	

			travelTime /= fightManager.AnimationSpeed; 			// scale travelTime according to animation speed

			if (TravelOnExpiry)
			{
				var	expiryDistance = IsPlayer1 ? -ProfileData.ExpiryDistance : ProfileData.ExpiryDistance;
				var startPosition = transform.position;
				var targetPosition = new Vector3(startPosition.x + expiryDistance, startPosition.y, startPosition.z);
				var winnerStartPosition = winner.transform.position;
				var winnerTargetPosition = new Vector3(winnerStartPosition.x - expiryDistance, winnerStartPosition.y, winnerStartPosition.z);
				float t = 0.0f;

				while (t < 1.0f)
				{
					t += Time.deltaTime * (Time.timeScale / travelTime); 
					transform.position = Vector3.Lerp(startPosition, targetPosition, t);

					// winner also moves back in arcade mode (camera tracks the loser)
					if (FightManager.CombatMode == FightMode.Arcade || FightManager.CombatMode == FightMode.Dojo)
						winner.transform.position = Vector3.Lerp(winnerStartPosition, winnerTargetPosition, t);
				
					yield return null;
				}
			}
			else
			{
				yield return new WaitForSeconds(travelTime);
			}
				
			if (UnderAI && winner.InTraining) 					// AI KO'd in training
			{
				winner.InTraining = false;
				FightManager.SavedGameStatus.CompletedBasicTraining = true;

				StartWatching();								// AI now starts watching for triggers!

				fightManager.ShowFighterLevels();				// XP counted now training done
				yield return StartCoroutine(fightManager.NextMatch(winner));	
			}
			else
			{
				bool endOfMatch = false;

				switch (FightManager.CombatMode)
				{
					case FightMode.Arcade:
					case FightMode.Training:
					default:
						endOfMatch = winner.ProfileData.SavedData.MatchRoundsWon >= fightManager.EndMatchWins;
						break;

					case FightMode.Dojo:
						endOfMatch = false;								// keep returning to same fighters and location
						break;

					case FightMode.Survival:
						endOfMatch = winner.UnderAI;					// ends when beaten by AI

						int survivalEndurance = ProfileData.SavedData.MatchRoundsWon * Level;

						// record survival score if new personal best
						if (endOfMatch && survivalEndurance > FightManager.SavedGameStatus.BestSurvivalEndurance)
						{
							FightManager.SavedGameStatus.BestSurvivalEndurance = survivalEndurance;
							FirebaseManager.PostLeaderboardScore(Leaderboard.SurvivalRounds, survivalEndurance);
						}

						if (! endOfMatch)
							winner.ReturnToIdle();
						break; 

					case FightMode.Challenge:
						endOfMatch = fightManager.ChallengeLastInTeam(winner.UnderAI);		// when one team defeated

						if (! endOfMatch)
							winner.ReturnToIdle();
						break;
				}

//				Debug.Log(FullName + ": ExpireToNextRound: endOfMatch = " + endOfMatch);

				if (endOfMatch)
				{
					SaveProfile();				// not if AI
					winner.SaveProfile();		// not if AI

					FightManager.SavedGameStatus.FightInProgress = false;

					fightManager.HideDojoUI();

					switch (FightManager.CombatMode)
					{
						case FightMode.Arcade:
							if (FightManager.IsNetworkFight)	// Player2 is always opponent
							{
								if (winner.IsPlayer1)
									FightManager.SavedGameStatus.VSVictoryPoints++;
								else
									FightManager.SavedGameStatus.VSVictoryPoints--;

								if (FightManager.SavedGameStatus.VSVictoryPoints < 0)
									FightManager.SavedGameStatus.VSVictoryPoints = 0;
								
								FirebaseManager.PostLeaderboardScore(Leaderboard.VSVictoryPoints, FightManager.SavedGameStatus.VSVictoryPoints);

								yield return StartCoroutine(fightManager.NextMatch(winner)); 	// show winner/stats then back to mode select
							}
							else
							{
								if (!winner.UnderAI && FighterName != "Ninja") 					// player won - except if against ninja (in training) (kludge)
									fightManager.CompleteCurrentLocation();						// sets worldTourCompleted if last location
								yield return StartCoroutine(fightManager.NextMatch(winner)); 	// show winner/stats then world map to fly to next match location/AI opponent
							}
							break;

						case FightMode.Training:
							yield return StartCoroutine(fightManager.NextMatch(winner)); 	// show winner/stats then world map to fly to next match location/AI opponent
							break;

						case FightMode.Survival:
							yield return StartCoroutine(fightManager.NextMatch(winner)); 	// show winner/stats then back to mode select
							break;

						case FightMode.Challenge:
							fightManager.RecordFinalChallengeResult(winner.UnderAI);		// record result of final round in challenge
							yield return StartCoroutine(fightManager.NextMatch(winner)); 	// show winner/stats then back to mode select
							break;

						default:
							break;
					}

					FightManager.SavedGameStatus.NinjaSchoolFight = false;
				}
				else
				{
					yield return StartCoroutine(fightManager.NextRound(false));		// return to default with same fighters and scenery
				}
			}

			yield return null;
		}
			
		protected virtual void KOState(HitFrameData hitData)
		{
//			Debug.Log(FullName + ": KOState - State = " + CurrentState + ", CanContinue = " + CanContinue);
			Expire(hitData);				// expire immediately by default - skeletron falls to his knees to take one final blow
		}

		private void Expire(HitFrameData hitData)
		{			
			ClearCuedMoves();

			switch (hitData.TypeOfHit)
			{
				case HitType.Hook:
					CurrentState = State.Hit_Hook_Die;
					break;

				case HitType.Mid:
					CurrentState = State.Hit_Mid_Die;
					break;

				case HitType.Straight:
					CurrentState = State.Hit_Straight_Die;
					break;

				case HitType.Uppercut:
					CurrentState = State.Hit_Uppercut_Die;
					break;

				case HitType.Shove:		// no damage inflicted by shove
				case HitType.None:
				default:
					break;
			}
					
			KnockOut();		// virtual
		}

		// feedback offset to side of fighter
		public void TriggerFeedbackFX(FeedbackFXType feedbackFX, bool playerOffset = false, float yOffset = 0.0f)
		{
//			var xOffset = playerOffset ? (IsPlayer1 ? feedbackOffsetX : -feedbackOffsetX) : 0.0f;		// centred if not playerOffset
			var xOffset = -feedbackOffsetX;																// opposite player (ie Player2)

			switch (feedbackFX)
			{
				case FeedbackFXType.Swipe_Back:
//				case FeedbackFXType.Swipe_Down:
				case FeedbackFXType.Swipe_Forward:
//				case FeedbackFXType.Swipe_Up:
				case FeedbackFXType.Swipe_Vengeance:
					xOffset -= feedbackSwipeOffsetX;
					break;
			}

			fightManager.TriggerFeedbackFX(feedbackFX, xOffset, yOffset);
		}

//		public IEnumerator PauseTriggerFeedbackFX(float pauseTime, FeedbackFXType feedbackFX, bool playerOffset = true, float yOffset = 0.0f)
//		{
//			yield return new WaitForSeconds(pauseTime);
//			TriggerFeedbackFX(feedbackFX, playerOffset, yOffset);
//		}

		// feedback offset to side of fighter
		public void WrongFeedbackFX()
		{
			fightManager.Wrong(IsPlayer1 ? feedbackOffsetX : -feedbackOffsetX);
		}


		private void NoGaugeFeedback(int required)
		{
			if (UnderAI || IsDojoShadow)
				return;

			WrongFeedbackFX();

			if (required <= 0)
				fightManager.GaugeFeedback(IsPlayer1, FightManager.Translate("notEnoughCrystals"));
			else
				fightManager.GaugeFeedback(IsPlayer1, FightManager.Translate("needs") + " " + required + " " + (required == 1 ? FightManager.Translate("crystal") : FightManager.Translate("crystals")));
		}


		protected void KOFreeze()
		{
//			Debug.Log(FullName + ": KOFreeze ExpiredState = " + ExpiredState + ", IsBlocking = " + IsBlocking + ", HoldingBlock = " + HoldingBlock);
			ReleaseBlock();

			AudioSource.PlayClipAtPoint(fightManager.KOSound, Vector3.zero, FightManager.SFXVolume);
			fightManager.TriggerFeedbackFX(FeedbackFXType.KO);

			StartStatusEffectFrameCount(StatusEffect.KnockOut);

			// freeze both fighters for effect ... on next frame - a KO hit will freeze until KO status effect times out
			SetFreezeFrames(expiryFreezeFrames);	

			if (ProfileData.FighterClass != FighterClass.Boss)
				secondLifeOpportunity = true;   		// reset by EndKOFreeze

			if (OnKnockOutFreeze != null)			// for AI to trigger second life (if equipped)
				OnKnockOutFreeze(this);
		}

		// called at end of KO feedback / freeze
		private void EndKOFreeze()
		{
//			Debug.Log(FullName + ": EndKOFreeze ExpiredState = " + ExpiredState + ", takenLastFatalHit = " + takenLastFatalHit);

			if (FightManager.CombatMode == FightMode.Training) //! FightManager.SavedGameStatus.CompletedBasicTraining)
				Opponent.Trainer.TrainingComplete();			// clear prompt / feedback etc. -> NinjaSchoolFight
			
			if (! ExpiredState && ! takenLastFatalHit)
				return;

			secondLifeOpportunity = false;

			if (takenLastFatalHit)		// taken last of fatal blows
			{
				StartCoroutine(ExpireToNextRound());		// travel followed by next round / match
				takenLastFatalHit = false;

				if (OnKnockOut != null)
					OnKnockOut(this);
			}
		}
			
		protected virtual void KnockOut()
		{
			// final KO - overrides handle any special behaviour (eg. skeletron)	
		}
			
		protected virtual void CounterAttack(bool chained)
		{
			CurrentMove = Move.Counter;
			CurrentState = State.Counter_Attack;

			StartCoroutine(StrikeTravel());

			if (ProfileData.counterWhiff != null)
				AudioSource.PlayClipAtPoint(ProfileData.counterWhiff, Vector3.zero, FightManager.SFXVolume);
		}

		#endregion


		#region state dictionary

		// dictionary built for fast lookup of hit frames, state ends, etc
		private void BuildStateDictionary()
		{
			if (StateSignature == null)
				return;

			// keyed on state + event type + frame number
			hitFrameDictionary = new Dictionary<int, HitFrameData>();

			BuildHitFrameDictionary(StateSignature.LightStrike);
			BuildHitFrameDictionary(StateSignature.MediumStrike);
			BuildHitFrameDictionary(StateSignature.HeavyStrike);
			BuildHitFrameDictionary(StateSignature.Shove);
			BuildHitFrameDictionary(StateSignature.Special);
			BuildHitFrameDictionary(StateSignature.Vengeance);
			BuildHitFrameDictionary(StateSignature.Counter);
			BuildHitFrameDictionary(StateSignature.Tutorial);
		}


		private void BuildHitFrameDictionary(List<HitFrameData> hitFrameList)
		{
			foreach (var hitFrame in hitFrameList)
			{
				int key = (int)hitFrame.State + (hitFrame.FrameNumber > 0 ? hitFrame.FrameNumber : 1);

				if (hitFrameDictionary.ContainsKey(key))
				{
					Debug.Log(FullName + ": BuildHitFrameDictionary: key = " + key + " already in dictionary");
				}
					
				try
				{
					hitFrameDictionary.Add(key, hitFrame);
//					Debug.Log(FighterFullName + ": BuildHitFrameDictionary: ADDED key = " + key + " value = " + stateFrame.ToString());
				}
				catch (System.Exception ex)
				{
					Debug.Log(FullName + ": BuildHitFrameDictionary EXCEPTION: key = " + key + ", state = " + hitFrame.State + " Ex: " + ex.Message);
				}
			}
		}


		// lookup in stateDictionary for a state frame for the current state
		private HitFrameData LookupHitFrameData
		{
			get
			{
				if (IsIdle)
					return null;

				if (StateFrameCount == 0)
					return null;

				int key = (int)CurrentState + StateFrameCount;
				HitFrameData stateFrameData;

				return hitFrameDictionary.TryGetValue(key, out stateFrameData) ? stateFrameData : null;
			}
		}

		#endregion

		public void StartTraining()
		{
			if (Trainer != null && !UnderAI)
				Trainer.StartTraining();
		}

		public void StopTraining()
		{
			if (Trainer != null && InTraining)
			{
				Trainer.CleanupTraining();
				InTraining = false;
			}
		}

		public virtual string WinQuote(string loserName)
		{
			return "Win quote undefined for " + loserName;
		}


		public void SaveProfile()
		{
			if (UnderAI || InTraining)
				return;

			try
			{
				profile.Save(FighterName, ColourScheme);
				DebugUI = FullName + ": SAVED PROFILE - LEVEL " + profile.ProfileData.SavedData.Level;
			}
			catch (Exception ex)
			{
				DebugUI = "SaveProfile FAILED: " + ex.Message;
			}
		}

		public void LoadProfile()
		{
			if (UnderAI || InTraining)
				return;

			try
			{
				bool loaded = profile.LoadFighterProfile(FighterName);
				DebugUI = loaded ? ("RESTORED PROFILE - LEVEL " + profile.ProfileData.SavedData.Level) : "DID NOT RESTORE PROFILE";
			}
			catch (Exception ex)
			{
				DebugUI = "LoadProfile FAILED: " + ex.Message;
			}
		}
	}
}
