using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;

    public Lobby currentLobby;

    public bool IsHost;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    public async Task CreateLobby()
    {
        try
        {
            currentLobby = await LobbyService.Instance.CreateLobbyAsync(
                "My Lobby",
                4
            );

            Debug.Log($"Lobby created: {currentLobby.LobbyCode}");

            // 1. Create Relay
            string relayCode = await RelayManager.Instance.CreateRelay(4);

            // 2. Store relay code inside Lobby data (VERY important)
            var updateOptions = new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
            {
                {
                    "relayCode",
                    new DataObject(
                        DataObject.VisibilityOptions.Public,
                        relayCode
                    )
                }
            }
            };

            await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, updateOptions);

            // 3. Load lobby scene (NGO already running)
            SceneManager.LoadScene("LobbyScene");

            LobbyManager.Instance.IsHost = true;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    public async Task JoinLobby(string code)
    {
        try
        {
            currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code);

            Debug.Log($"Joined lobby: {currentLobby.Name}");

            // 1. Get relay code from lobby
            string relayCode = currentLobby.Data["relayCode"].Value;

            // 2. Load lobby scene first (optional but cleaner UX)
            SceneManager.LoadScene("LobbyScene");

            // 3. Join Relay (NGO client starts here)
            await RelayManager.Instance.JoinRelay(relayCode);

            LobbyManager.Instance.IsHost = false;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    async void HeartBeat()
    {
        while(currentLobby != null)
        {
            await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
            await Task.Delay(15000);
        }
    }
}
