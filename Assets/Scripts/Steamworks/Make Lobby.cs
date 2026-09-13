using System;
using Steamworks;
using Steamworks.Data;
using UnityEngine;

public class MakeLobby : MonoBehaviour
{
    [SerializeField] private int _maxPlayerCount = 4;
    private Lobby _currentLobby;

    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
        SetUpCallBacks();
    }

    private void SetUpCallBacks()
    {
        SteamMatchmaking.OnLobbyCreated += OnLobbyCreate;
        SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
    }

    private void OnLobbyCreate(Result result, Lobby lobby)
    {
        try
        {
            if (!_currentLobby.Id.IsValid)
            {
                _currentLobby = lobby;
                Debug.Log("Lobby Created");
            }
        }
        catch (Exception e)
        {
            Debug.Log("Creation failed");
            Debug.Log(e.Message);
        }
    }

    private void OnLobbyMemberJoined(Lobby lobby, Friend friend)
    {
        Debug.Log(friend.Name + "joined");
    }

    private async void Start()
    {
        await SteamMatchmaking.CreateLobbyAsync(_maxPlayerCount);
        
    }
}
