using UnityEngine;
using System.Collections;
using GAFInternal.Core;
using GAF.Objects;

namespace GAF.Core
{
	[AddComponentMenu("GAF/GAFCanvasClip")]
	[RequireComponent(typeof(GAFObjectsManager))]
	[ExecuteInEditMode]
	public class GAFCanvasClip : GAFCanvasClipInternal<GAFCanvasObjectsManagerInternal>
	{

	}
}
