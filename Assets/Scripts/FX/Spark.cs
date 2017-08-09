using UnityEngine;
using System.Collections;

namespace FightingLegends
{
	public class Spark : MonoBehaviour
	{
		private ParticleSystem sparkParticles;

		// Awake is efectively the constructor, called when the script instance is being loaded
		void Awake()
		{
			sparkParticles = GetComponent<ParticleSystem>();
		}

		// Use this for initialization
		void Start()
		{

		}
		
		public void Play()
		{
			if (sparkParticles != null)
				sparkParticles.Play();
		}
			
		public void Stop()
		{
			if (sparkParticles != null && sparkParticles.isPlaying)
				sparkParticles.Stop();
		}

		public void SetColour(Color colour)
		{
			sparkParticles.startColor = colour;
		}

	}
}
