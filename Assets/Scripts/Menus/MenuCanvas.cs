using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace FightingLegends
{
	public class MenuCanvas : MonoBehaviour
	{
		[HideInInspector]
		public bool DirectToOverlay;					// true when overlay is activated when menu is activated (eg. store power-up / buy / spend overlays)
		[HideInInspector]
		public MenuType NavigatedFrom;					// used for virtual CanGoBack - not used to actually navigate back (see fightManager.menuStack)
		[HideInInspector]
		public MenuCanvas ParentCanvas  = null;

		protected bool revealingPanel = false;			// true while lerping fade in
		protected bool hidingPanel = false;				// true while lerping fade out
		private const float overlayFadeTime = 0.15f;

		private Stack<Image> OverlayStack = new Stack<Image>();		// on top of this MenuCanvas (fade in / out)

		public AudioClip MusicTrack;	// music associated with this menu - played by SceneryManager
		public AudioClip FadeSound;

		protected delegate void CanvasShownDelegate();
		protected CanvasShownDelegate OnShown;

		protected delegate void CanvasHiddenDelegate();
		protected CanvasHiddenDelegate OnHidden;

		protected delegate void OverlayRevealedDelegate(Image panel, int overlayCount);
		protected OverlayRevealedDelegate OnOverlayRevealed;

		protected delegate void OverlayHidingDelegate(Image panel, int overlayCount);
		protected OverlayHidingDelegate OnOverlayHiding;

		protected delegate void OverlayHiddenDelegate(Image panel, int overlayCount);
		protected OverlayHiddenDelegate OnOverlayHidden;

		public delegate void GoBackDelegate();
		public GoBackDelegate OnDeactivate;

		public virtual bool CanNavigateBack { get { return NavigatedFrom != MenuType.None; } }


		public void Show()
		{
			FightManager.OnThemeChanged += SetTheme;

			SetTheme(FightManager.SavedGameStatus.Theme, FightManager.ThemeHeader, FightManager.ThemeFooter);

			gameObject.SetActive(true);

			if (OnShown != null)
				OnShown();
		}

		public void Hide()
		{
			FightManager.OnThemeChanged -= SetTheme;

			gameObject.SetActive(false);

			if (OnHidden != null)
				OnHidden();
		}

			
		protected int ActivatedOverlayCount
		{
			get { return OverlayStack.Count; }
		}

		public bool HasActivatedOverlay
		{
			get { return ActivatedOverlayCount > 0 || (ParentCanvas != null && ParentCanvas.HasActivatedOverlay); }
		}
			
		protected virtual void HideAllOverlays()
		{
			while (HasActivatedOverlay)
			{
				var overlay = OverlayStack.Pop();
//				Debug.Log("HideAllOverlays: count = " + OverlayStack.Count);
				overlay.gameObject.SetActive(false);

				if (OnOverlayHidden != null)
					OnOverlayHidden(overlay, OverlayStack.Count);
			}
		}

		// returns true if no active overlays or if overlays were activated directly
		// which means that the menu will be closed when navigating back
		public bool HideActiveOverlay()
		{
			bool activeOverlay = HasActivatedOverlay;
			if (!activeOverlay)		// nothing to hide
				return true;

//			Debug.Log("HideActiveOverlay: DirectToOverlay = " + DirectToOverlay + ", HasActivatedOverlay = " + HasActivatedOverlay);

			if (DirectToOverlay)
			{
				HideAllOverlays();
				DirectToOverlay = false;

				// deactivate this MenuCanvas (ie. as if back clicked)
				if (OnDeactivate != null)
					OnDeactivate();
			}
			else
			{
				StartCoroutine(HideOverlay(OverlayStack.Peek()));
			}

			return DirectToOverlay && activeOverlay;		// hiding overlay also deactivates menu canvas
		}

		protected Image ActiveOverlay
		{
			get { return HasActivatedOverlay ? OverlayStack.Peek() : null; } 
		}


		protected IEnumerator RevealOverlay(Image overlay)
		{
			while (hidingPanel)
				yield return null;
				
			OverlayStack.Push(overlay);
//			Debug.Log("RevealOverlay: count = " + OverlayStack.Count);

			revealingPanel = true;

			SetOverlayTheme(overlay, FightManager.SavedGameStatus.Theme, FightManager.ThemeHeader, FightManager.ThemeFooter);

			yield return StartCoroutine(FightManager.FadeImage(overlay, overlayFadeTime, false, FadeSound, Color.white));
			revealingPanel = false;
		
			if (OnOverlayRevealed != null)
				OnOverlayRevealed(overlay, OverlayStack.Count);

			yield return null;
		}

		protected IEnumerator HideOverlay(Image overlay)
		{
			while (revealingPanel)
				yield return null;

			if (OnOverlayHiding != null) 		// eg. special animation - eg. power-ups
				OnOverlayHiding(overlay, OverlayStack.Count);
			else
				StartCoroutine(FadeOverlay(overlay));
			
//			OverlayStack.Pop();
//
//			hidingPanel = true;
//			yield return StartCoroutine(FightManager.FadeImage(overlay, overlayFadeTime, true, FadeSound, Color.white));
//			hidingPanel = false;
//
//			if (OnOverlayHidden != null)
//				OnOverlayHidden(overlay, OverlayStack.Count);
				
			yield return null;
		}

		protected IEnumerator FadeOverlay(Image overlay)
		{
			OverlayStack.Pop();

			hidingPanel = true;
			yield return StartCoroutine(FightManager.FadeImage(overlay, overlayFadeTime, true, FadeSound, Color.white));
			hidingPanel = false;

			if (OnOverlayHidden != null)
				OnOverlayHidden(overlay, OverlayStack.Count);

			yield return null;
		}


		protected void SetTheme(UITheme theme, Sprite header, Sprite footer)
		{
			if (header == null || footer == null)
				return;
			
			var headerObject = transform.Find("Header");
			if (headerObject != null)
				headerObject.GetComponent<Image>().sprite = header;

			var footerObject = transform.Find("Footer");
			if (footerObject != null)
				footerObject.GetComponent<Image>().sprite = footer;

			foreach (var overlay in OverlayStack)
			{
				SetOverlayTheme(overlay, theme, header, footer);
			}
		}

		private void SetOverlayTheme(Image overlay, UITheme theme, Sprite header, Sprite footer)
		{
			if (header == null || footer == null)
				return;
			
			var headerObject = overlay.transform.Find("Header");
			if (headerObject != null)
				headerObject.GetComponent<Image>().sprite = header;

			var footerObject = overlay.transform.Find("Footer");
			if (footerObject != null)
				footerObject.GetComponent<Image>().sprite = footer;
		}

			
		#region music 

		// music played by SceneryManager (audioSource)
		public void PlayMusic()
		{
			if (MusicTrack != null)
				SceneryManager.PlayMusicTrack(MusicTrack);
		}
			
		#endregion  		// music
	}
}
