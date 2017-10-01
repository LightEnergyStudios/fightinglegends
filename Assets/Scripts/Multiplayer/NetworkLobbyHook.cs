using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using FightingLegends;

namespace Prototype.NetworkLobby
{
	public class NetworkLobbyHook : LobbyHook
	{
		// transfer player number and name from lobby player to game player
		public override void OnLobbyServerSceneLoadedForPlayer(NetworkManager networkManager, GameObject lobbyPlayer, GameObject gamePlayer)
		{
			LobbyPlayer lobby = lobbyPlayer.GetComponent<LobbyPlayer>();
			NetworkFighter localPlayer = gamePlayer.GetComponent<NetworkFighter>();

			localPlayer.PlayerNumber = lobby.playerNumber;
			localPlayer.PlayerName = lobby.playerName;			// user id

//			Debug.Log("NetworkLobbyHook - game player: " + localPlayer.PlayerNumber + " / " + localPlayer.PlayerName);
		}
	}
}
