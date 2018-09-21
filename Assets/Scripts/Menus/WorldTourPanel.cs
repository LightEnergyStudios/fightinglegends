using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class WorldTourPanel : MonoBehaviour
{
	public delegate void WorldTourCongratsEndDelegate();
	public WorldTourCongratsEndDelegate	OnWorldTourCongratsEnd;

	// animation event
	public void WorldTourCongratsEnd()
	{
		if (OnWorldTourCongratsEnd != null)
			OnWorldTourCongratsEnd();
	}
}
