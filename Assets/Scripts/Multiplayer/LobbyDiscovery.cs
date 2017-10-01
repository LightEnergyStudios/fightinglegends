using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace FightingLegends
{
	public class LobbyDiscovery : NetworkDiscovery
	{
		public delegate void OnHostIPDelegate(string hostIP);
		public OnHostIPDelegate OnHostIP;

		// handle broadcast messages when running as a client
		public override void OnReceivedBroadcast(string fromAddress, string data)
		{
			base.OnReceivedBroadcast(fromAddress, data);

			if (OnHostIP != null)
				OnHostIP(fromAddress);
		}
	}
}
