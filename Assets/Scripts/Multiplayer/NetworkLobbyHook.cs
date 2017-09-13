using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using FightingLegends;

namespace Prototype.NetworkLobby
{
	public class NetworkLobbyHook : LobbyHook
	{
		// TODO: figure out what data needs to be passed from lobby to game
		public override void OnLobbyServerSceneLoadedForPlayer(NetworkManager networkManager, GameObject lobbyPlayer, GameObject gamePlayer)
		{
			LobbyPlayer lobby = lobbyPlayer.GetComponent<LobbyPlayer>();
			NetworkFighter localPlayer = gamePlayer.GetComponent<NetworkFighter>();

			localPlayer.PlayerNumber = lobby.playerNumber;
			localPlayer.PlayerName = lobby.playerName;
			localPlayer.PlayerColor = lobby.playerColor;
		}
	}
}
