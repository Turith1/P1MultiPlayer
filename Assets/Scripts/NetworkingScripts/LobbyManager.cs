using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Unity.Netcode;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;

    public Lobby currentLobby;

    public bool IsHost;
    private bool heartbeatRunning;

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

            NetworkManager.Singleton.StartHost();

            HeartBeat();

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
            //SceneManager.LoadScene("LobbyScene");
            NetworkManager.Singleton.SceneManager.LoadScene("LobbyScene", LoadSceneMode.Single);

            IsHost = true;
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

            // 3. Join Relay (NGO client starts here)
            await RelayManager.Instance.JoinRelay(relayCode);

            NetworkManager.Singleton.StartClient();

            IsHost = false;

            // 2. Load lobby scene first (optional but cleaner UX)
            //SceneManager.LoadScene("LobbyScene");
            //NetworkManager.Singleton.SceneManager.LoadScene("LobbyScene", LoadSceneMode.Single);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    async void HeartBeat()
    {
        if (heartbeatRunning)
            return;

        heartbeatRunning = true;

        while (currentLobby != null)
        {
            await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
            await Task.Delay(15000);
        }

        heartbeatRunning = false;
    }

    public bool AreAllPlayersReady()
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.ClientId == NetworkManager.Singleton.LocalClientId)
                continue;

            if (client.PlayerObject == null)
                return false;

            PlayerLobbyState playerState =
                client.PlayerObject.GetComponent<PlayerLobbyState>();

            if (playerState == null)
                return false;

            if (!playerState.IsReady.Value)
            {
                return false;
            }
        }

        return true;
    }


    public void TryStartGame()
    {
        Debug.Log("HOST TRYING TO START GAME");
        Debug.Log($"Players ready: {AreAllPlayersReady()}");

        if (!NetworkManager.Singleton.IsHost)
            return;

        if (!AreAllPlayersReady())
        {
            Debug.Log("Not everyone is ready.");
            return;
        }

        NetworkManager.Singleton.SceneManager.LoadScene(
            "SampleScene",
            LoadSceneMode.Single
        );
    }
}
