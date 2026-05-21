using UnityEngine;
using TMPro;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections;

public class LobbyUI : MonoBehaviour
{
    public static LobbyUI Instance;

    [SerializeField] private Button mainButton;
    [SerializeField] private TMP_Text mainButtonText;
    [SerializeField] private TMP_Text buttonText;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;


        NetworkManager.Singleton.OnClientConnectedCallback += OnClientChanged;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientChanged;

        StartCoroutine(WaitForLocalPlayer());
    }

    public void OnStartGamePressed()
    {
        LobbyManager.Instance.TryStartGame();
    }

    public void UpdatePlayerReadyState(
        ulong clientId,
        bool ready
    )
    {
        Debug.Log($"Player {clientId} ready: {ready}");

        UpdateMainButton();
        
    }

    public void OnMainButtonPressed()
    {
        // HOST
        if (NetworkManager.Singleton.IsHost)
        {
            if (LobbyManager.Instance.AreAllPlayersReady())
            {
                LobbyManager.Instance.TryStartGame();
            }

            return;
        }

        // CLIENT
        bool currentReady =
            PlayerLobbyState.LocalPlayer.IsReady.Value;

        PlayerLobbyState.LocalPlayer.SetReadyServerRpc(!currentReady);
    }

    public void UpdateMainButton()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            if (LobbyManager.Instance.AreAllPlayersReady())
            {
                buttonText.text = "Start Game";
            }
            else
            {
                buttonText.text = "Waiting...";
            }
        }
        else
        {
            var player = PlayerLobbyState.LocalPlayer;

            if (player == null)
                return;

            mainButtonText.text =
                player.IsReady.Value
                    ? "Unready"
                    : "Ready";
        }
    }

    private void OnClientChanged(ulong clientId)
    {
        UpdateMainButton();
    }

    private IEnumerator WaitForLocalPlayer()
    {
        while (PlayerLobbyState.LocalPlayer == null)
            yield return null;

        UpdateMainButton();
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton == null)
            return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientChanged;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientChanged;
    }
}
