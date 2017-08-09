using UnityEngine;
using System.Collections;

public enum AnchorPresets
{
	TopLeft,
	TopCentre,
	TopRight,

	MiddleLeft,
	MiddleCentre,
	MiddleRight,

	BottomLeft,
	BottomCentre,
	BottomRight,

	VStretchLeft,
	VStretchRight,
	VStretchCentre,

	HStretchTop,
	HStretchCentre,
	HStretchBottom,

	StretchAll
}

public enum PivotPresets
{
	TopLeft,
	TopCentre,
	TopRight,

	MiddleLeft,
	MiddleCentre,
	MiddleRight,

	BottomLeft,
	BottomCentre,
	BottomRight,
}

public static class RectTransformExtensions
{
	public static void SetAnchor(this RectTransform rect, AnchorPresets align, float offsetX = 0, float offsetY = 0)
	{
		rect.anchoredPosition3D = Vector3.zero;			// to make sure posZ is zero!! ...
		rect.anchoredPosition = new Vector3(offsetX, offsetY, 0);

		switch (align)
		{
			case AnchorPresets.TopLeft:
				{
					rect.anchorMin = new Vector2(0, 1);
					rect.anchorMax = new Vector2(0, 1);
					break;
				}
			case  AnchorPresets.TopCentre:
				{
					rect.anchorMin = new Vector2(0.5f, 1);
					rect.anchorMax = new Vector2(0.5f, 1);
					break;
				}
			case AnchorPresets.TopRight:
				{
					rect.anchorMin = new Vector2(1, 1);
					rect.anchorMax = new Vector2(1, 1);
					break;
				}

			case AnchorPresets.MiddleLeft:
				{
					rect.anchorMin = new Vector2(0, 0.5f);
					rect.anchorMax = new Vector2(0, 0.5f);
					break;
				}
			case AnchorPresets.MiddleCentre:
				{
					rect.anchorMin = new Vector2(0.5f, 0.5f);
					rect.anchorMax = new Vector2(0.5f, 0.5f);
					break;
				}
			case AnchorPresets.MiddleRight:
				{
					rect.anchorMin = new Vector2(1, 0.5f);
					rect.anchorMax = new Vector2(1, 0.5f);
					break;
				}

			case AnchorPresets.BottomLeft:
				{
					rect.anchorMin = new Vector2(0, 0);
					rect.anchorMax = new Vector2(0, 0);
					break;
				}
			case AnchorPresets.BottomCentre:
				{
					rect.anchorMin = new Vector2(0.5f, 0);
					rect.anchorMax = new Vector2(0.5f,0);
					break;
				}
			case AnchorPresets.BottomRight:
				{
					rect.anchorMin = new Vector2(1, 0);
					rect.anchorMax = new Vector2(1, 0);
					break;
				}

			case AnchorPresets.HStretchTop:
				{
					rect.anchorMin = new Vector2(0, 1);
					rect.anchorMax = new Vector2(1, 1);
					break;
				}
			case AnchorPresets.HStretchCentre:
				{
					rect.anchorMin = new Vector2(0, 0.5f);
					rect.anchorMax = new Vector2(1, 0.5f);
					break;
				}
			case AnchorPresets.HStretchBottom:
				{
					rect.anchorMin = new Vector2(0, 0);
					rect.anchorMax = new Vector2(1, 0);
					break;
				}

			case AnchorPresets.VStretchLeft:
				{
					rect.anchorMin = new Vector2(0, 0);
					rect.anchorMax = new Vector2(0, 1);
					break;
				}
			case AnchorPresets.VStretchCentre:
				{
					rect.anchorMin = new Vector2(0.5f, 0);
					rect.anchorMax = new Vector2(0.5f, 1);
					break;
				}
			case AnchorPresets.VStretchRight:
				{
					rect.anchorMin = new Vector2(1, 0);
					rect.anchorMax = new Vector2(1, 1);
					break;
				}

			case AnchorPresets.StretchAll:
				{
					rect.anchorMin = new Vector2(0, 0);
					rect.anchorMax = new Vector2(1, 1);
					break;
				}
		}
	}

	public static void SetPivot(this RectTransform rect, PivotPresets preset)
	{
		switch (preset)
		{
			case PivotPresets.TopLeft:
				{
					rect.pivot = new Vector2(0, 1);
					break;
				}
			case PivotPresets.TopCentre:
				{
					rect.pivot = new Vector2(0.5f, 1);
					break;
				}
			case PivotPresets.TopRight:
				{
					rect.pivot = new Vector2(1, 1);
					break;
				}

			case PivotPresets.MiddleLeft:
				{
					rect.pivot = new Vector2(0, 0.5f);
					break;
				}
			case PivotPresets.MiddleCentre:
				{
					rect.pivot = new Vector2(0.5f, 0.5f);
					break;
				}
			case PivotPresets.MiddleRight:
				{
					rect.pivot = new Vector2(1, 0.5f);
					break;
				}

			case PivotPresets.BottomLeft:
				{
					rect.pivot = new Vector2(0, 0);
					break;
				}
			case PivotPresets.BottomCentre:
				{
					rect.pivot = new Vector2(0.5f, 0);
					break;
				}
			case PivotPresets.BottomRight:
				{
					rect.pivot = new Vector2(1, 0);
					break;
				}
		}
	}
}
