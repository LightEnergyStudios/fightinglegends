using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FightingLegends
{
	public static class SceneSettings
	{
		public static bool OpeningSequencePlayed = false;	// opening scene (on drums) to prevent repeat performance!
		public static bool ShowLobbyUI = false;				// opening scene - set from Combat scene ModeSelect -> open lobby

		public static bool DirectToFighterSelect = false;	// combat scene - set from LobbyManager when players ready -> direct to VS FighterSelect

		public static bool BackFromLobby = false;	// back clicked - fade to black while reloading combat scene

	}
}
