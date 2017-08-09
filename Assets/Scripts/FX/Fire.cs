using UnityEngine;
using System.Collections;


namespace FightingLegends
{
	public class Fire : MonoBehaviour
	{
		private ParticleSystem fireParticles;

		private void Awake()
		{
			fireParticles = GetComponent<ParticleSystem>();
		}
			
		public void Trigger()
		{
			if (fireParticles != null)
				fireParticles.Play();
		}
	}
}
