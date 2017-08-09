using UnityEngine;
using System.Collections;


namespace FightingLegends
{
	public class Dust : MonoBehaviour
	{
		private ParticleSystem dustParticles;

		private void Awake()
		{
			dustParticles = GetComponent<ParticleSystem>();
		}
			
		public void Trigger(float force)
		{
			// get the force over lifetime module
			ParticleSystem.ForceOverLifetimeModule dustForce = dustParticles.forceOverLifetime;

			// modify the value
			ParticleSystem.MinMaxCurve rate = new ParticleSystem.MinMaxCurve();
			rate.constantMax = force; 	// positive or negative x direction

			dustForce.x = rate;

			if (dustParticles != null)
				dustParticles.Play();
		}
	}
}
