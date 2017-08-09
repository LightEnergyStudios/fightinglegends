
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace FightingLegends
{
    public class Scenery : MonoBehaviour
    {
		public GameObject prefab;  
        public string sceneryName;  
		public float width;     
		public int replications = 1;	// each side of centre (so 1 replication is 3 instances, 2 is 5, 3 is 7 etc.)

		// fog settings
		public bool fogEnabled = true;
		public FogMode fogMode = FogMode.Linear;
		public Color fogColour = Color.white;    
		public float fogStart = 200;    // linear fog
		public float fogEnd = 1500; 	// linear fog
		public float fogDensity;		// exponential fog

		public AudioClip MusicTrack;	// music associated with this scene - played by SceneryManager

		public bool PinToCamera = false;

		// list of multiple instances to facilitate swapping as camera tracks
		private List<GameObject> replicationList = new List<GameObject>();

		private GameObject LeftMost { get { return replicationList[ 0 ]; } }
		private GameObject RightMost { get { return replicationList[ replicationList.Count - 1 ]; } }
		private GameObject Centre { get { return replicationList[ replicationList.Count / 2 ]; } }

		private const float zeroTime = 1.0f;

		private CameraController cameraController;


        public void Awake()
        {
			replicationList = new List<GameObject>();

//			cameraController = Camera.main.GetComponent<CameraController>();
		}


		public void ConstructScenery()
		{
			var centre = Instantiate(prefab) as GameObject;
			var centrePosition = centre.transform.position;

			cameraController = Camera.main.GetComponent<CameraController>();

			// position centre scene in front of camera, which may have panned
			centrePosition = new Vector3(cameraController.DistancePanned, centre.transform.position.y, centre.transform.position.z);
			centre.transform.position = centrePosition;
				
			centre.transform.parent = PinToCamera ? Camera.main.transform : transform;

			replicationList.Add(centre);

			for (int i = 1; i <= replications; i++)
			{
				var offset = width * i;
				var leftPosition = new Vector3(centrePosition.x - offset, centrePosition.y, centrePosition.z);
				var rightPosition = new Vector3(centrePosition.x + offset, centrePosition.y, centrePosition.z);

				// add left-side scenery to beginning of list
				var left = Instantiate(prefab) as GameObject;
				left.transform.parent = transform;
				left.transform.position = leftPosition;
				replicationList.Insert(0, left);

				// add right-side scenery to end of list
				var right = Instantiate(prefab) as GameObject;
				right.transform.parent = transform;
				right.transform.position = rightPosition;
				replicationList.Add(right);
			}

			SetFog();
		}


		public void DestroyScenery()
		{
			foreach (var replication in replicationList)
				Destroy(replication);

			replicationList.Clear();
		}


		private void SetFog()
		{
			RenderSettings.fog = fogEnabled;
			RenderSettings.fogColor = fogColour;
			RenderSettings.fogMode = fogMode;
			RenderSettings.fogStartDistance = fogStart;
			RenderSettings.fogEndDistance = fogEnd;
			RenderSettings.fogDensity = fogDensity;
        }


		private void MoveLeftMostToRight()
		{
			var rightMostPosition = RightMost.transform.position;
			var leftMost = LeftMost;

			leftMost.transform.position = new Vector3(rightMostPosition.x + width, rightMostPosition.y, rightMostPosition.z);
		
			// move from first to last in list
			replicationList.Remove(leftMost);
			replicationList.Add(leftMost);			// add to end
		}
			
		private void MoveRightMostToLeft()
		{
			var leftMostPosition = LeftMost.transform.position;
			var rightMost = RightMost;

			rightMost.transform.position = new Vector3(leftMostPosition.x - width, leftMostPosition.y, leftMostPosition.z);

			// move from last to first in list
			replicationList.Remove(rightMost);
			replicationList.Insert(0, rightMost);	// add to beginning
		}


		public void MoveWithCamera()
		{
			if (replicationList.Count > 0)
			{
				// if the camera moves beyond the centre instance,
				// move the outer-most left or right to 'cover' any further camera movement in that direction
				if (Camera.main.transform.position.x > Centre.transform.position.x + (width / 2.0f))
				{
					MoveLeftMostToRight();
				}
				else if (Camera.main.transform.position.x < Centre.transform.position.x - (width / 2.0f))
				{
					MoveRightMostToLeft();
				}
			}
		}
//
//
//		public IEnumerator MoveToZero()
//		{
//			var centrePosition = Centre.transform.position;
//			var centreTargetPosition = new Vector3(-width / 2.0f, centrePosition.y, centrePosition.z);
//
//			float distance = centrePosition.x - centreTargetPosition.x;
//
//			foreach (var replication in replicationList)
//			{
//				var startPosition = replication.transform.position;
//				var targetPosition = new Vector3(startPosition.x + distance, startPosition.y, startPosition.z);
//
//				float t = 0.0f;
//
//				while (t < 1.0f)
//				{
//					t += Time.deltaTime * (Time.timeScale / zeroTime); 	// timeScale of 1.0 == real time
//
//					replication.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
//					yield return null;
//				}
//			}
//		}
    }
}
