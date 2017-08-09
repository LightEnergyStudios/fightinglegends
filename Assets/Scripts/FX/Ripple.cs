using UnityEngine;
using System.Collections;

namespace FightingLegends
{
	public class Ripple : MonoBehaviour
	{
		private const float effectMaxSize = 125.0f;

		private const float expandTime = 0.4f;
		private const float effectDelay = 50.0f;		// renderer enabled at 50% of expand lerp

		public Color lowColour;
		public Color highColour;
		
		private SpriteRenderer spriteRenderer;
		private IEnumerator expandCoroutine;

		
		// 'Constructor'
		void Awake()
		{
			spriteRenderer = GetComponent<SpriteRenderer>();
			spriteRenderer.enabled = false;
		}

		public void Expand(Vector3 touchPoint, bool destroyWhenDone = false)
		{
			if (expandCoroutine != null)
				StopCoroutine(expandCoroutine);

			transform.position = touchPoint;
			expandCoroutine = ExpandEffect(destroyWhenDone);
			StartCoroutine(expandCoroutine);
		}
		
		private IEnumerator ExpandEffect(bool destroyWhenDone)
		{
			float t = 0.0f;
			Vector3 startScale = new Vector3(0f,0f,0f);
			Vector3 targetScale = new Vector3(effectMaxSize,effectMaxSize,effectMaxSize);

			spriteRenderer.enabled = true;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / expandTime); 

				spriteRenderer.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
				spriteRenderer.color = Color.Lerp(highColour, lowColour, t);

//				// render effect midway during expansion, to keep out the way of the ripple spark
//				if (!spriteRenderer.enabled && (t > (effectDelay / 100.0f)))
//				{
//					spriteRenderer.enabled = true;
//					spriteRenderer.color = Color.Lerp(highColour, lowColour, t);
//				}
				
				yield return null;
			}

			if (destroyWhenDone)
				Destroy(gameObject);

			spriteRenderer.enabled = false;
		}
	}
}