using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Networking;
using System;

namespace FightingLegends
{
	// network fight manager spawned by Network(Lobby)Manager on server change to combat scene
	// server only - to sync start fight / ready to fight / quit fight etc. via NetworkFighter rpc calls
	public class NetworkFightManager : NetworkBehaviour
	{
		private string Fighter1Name = "";
		private string Fighter1Colour = "";
		private string Fighter2Name = "";
		private string Fighter2Colour = "";
		private string SelectedLocation = "";

		private NetworkFighter player1 = null;
		private NetworkFighter player2  = null;
			
		private bool fightStarted = false;

		private bool Fighter1Set { get { return !string.IsNullOrEmpty(Fighter1Name) && !string.IsNullOrEmpty(Fighter1Colour); }}
		private bool Fighter2Set { get { return !string.IsNullOrEmpty(Fighter2Name) && !string.IsNullOrEmpty(Fighter2Colour); }}
		private bool LocationSet { get { return !string.IsNullOrEmpty(SelectedLocation); }}

		private bool HasPlayers { get { return player1 != null && player2 != null; }}
		private bool CanStartFight { get { return HasPlayers && !fightStarted && Fighter1Set && Fighter2Set && LocationSet; }}

		private bool Player1ReadyToFight = false;
		private bool Player2ReadyToFight = false;


		// called from lobby -> combat scene change hook
		[Server]
		public void SetPlayer(NetworkFighter player)
		{
//			Debug.Log("SetPlayer: IsPlayer1 = " + player.IsPlayer1);
			if (player.IsPlayer1)
				player1 = player;
			else
				player2 = player;
		}


		[Server]
		public void SetFighter(bool isPlayer1, string name, string colour)
		{
			if (fightStarted)
				return;

			if (isPlayer1)
			{
				Fighter1Name = name;
				Fighter1Colour = colour;
			}
			else
			{
				Fighter2Name = name;
				Fighter2Colour = colour;
			}

			if (CanStartFight)
				SyncStartFight();
		}

		[Server]
		public void SetLocation(bool isPlayer1, string location)
		{
			if (fightStarted)
				return;
			
			SelectedLocation = location;

			if (CanStartFight)
				SyncStartFight();
			else
			{
				player1.RpcSelectLocation(location);		// for (opponent) world map
			}
		}


		[Server]
		private void SyncStartFight()
		{
			if (! CanStartFight)
				return;

			// stop timeout countdown
//			if (startFightTimeoutCoroutine != null)
//				StopCoroutine(startFightTimeoutCoroutine);

			// doesn't matter which player invokes the RPC
			// might as well be player1 as the host / initiator...
			player1.RpcStartFight(Fighter1Name, Fighter1Colour, Fighter2Name, Fighter2Colour, SelectedLocation);

			// reset for next fight
			ResetFighters();

			Player1ReadyToFight = false;
			Player2ReadyToFight = false;

			fightStarted = true;
		}


		[Server]
		public void ExitFighterSelect(bool isPlayer1)
		{
			player1.RpcExitFighterSelect();							// back to mode select
		}

		[Server]
		public void ReadyToFight(bool isPlayer1, bool ready)
		{
			if (isPlayer1)
				Player1ReadyToFight = true;
			else
				Player2ReadyToFight = true;

			if (Player1ReadyToFight && Player2ReadyToFight)
			{
				Player1ReadyToFight = false;
				Player2ReadyToFight = false;

				SyncReadyToFight(ready);
			}
		}

		[Server]
		private void SyncReadyToFight(bool ready)
		{
			// doesn't matter which player invokes the RPC
			// might as well be player1 as the host / initiator...
			player1.RpcReadyToFight(ready);
		}


		[Server]
		private void ResetFighters()
		{
//			Debug.Log("ResetFighters");

			Fighter1Name = "";
			Fighter1Colour = "";
			Fighter2Name = "";
			Fighter2Colour = "";
			SelectedLocation = "";
		}
	}
}
