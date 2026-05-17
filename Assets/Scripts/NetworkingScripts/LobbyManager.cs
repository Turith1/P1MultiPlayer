using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{

    public Lobby currentLobby;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public async Task CreateLobby()
    {
        var options = new CreateLobbyOptions();

        currentLobby = await LobbyService.Instance.CreateLobbyAsync("My Lobby", 4, options);
        Debug.Log(currentLobby.LobbyCode);

        HeartBeat();

        SceneManager.LoadScene("LobbyScene");
    }

    public async Task JoinLobby(string code)
    {
        currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code);
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
