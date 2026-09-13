using System;
using UnityEngine;
using Steamworks;
using Steamworks.Data;

public class JoinLobby : MonoBehaviour
{
    private void Start()
    {
        DontDestroyOnLoad(this.gameObject);
        SetUpCallBacks();
    }

    private void SetUpCallBacks()
    {
        SteamFriends.OnGameLobbyJoinRequested += OnGameInviteAccepted; //Accept invite when game is closed
        SteamFriends.OnGameRichPresenceJoinRequested += OnGameRichPresenceJoinRequested; //Accept invite when game is open
    }

    private async void OnGameInviteAccepted(Lobby lobby, SteamId steamId)
    {
        Debug.Log("OnGameInviteAccepted called, trying to join lobby");
        await lobby.Join();
    }

    private async void OnGameRichPresenceJoinRequested(Friend friend, string idString)
    {
        Debug.Log("OnGameRichPresenceJoinRequested called, trying to join lobby");
        Debug.Log("IdString :" + idString);
        if (ulong.TryParse(idString, out ulong seshID))
        {
            Debug.Log("SeshID is valid");
            Lobby? lobby = await SteamMatchmaking.JoinLobbyAsync(seshID);
            if (lobby == null)
            {
                Debug.Log("Lobby null");
            }
        }
        else
        {
            Debug.Log("Bad seshID");
        }
    }
}
