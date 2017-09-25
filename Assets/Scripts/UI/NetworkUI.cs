using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FightingLegends
{
	public class NetworkUI : MonoBehaviour
	{
		public Text Message;


		public void Start()
		{
			NetworkMessage("");
		}

		public void NetworkMessage(string message)
		{
			Message.gameObject.SetActive(!string.IsNullOrEmpty(message));
			Message.text = message;
		}
	}
}
