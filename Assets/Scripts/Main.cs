using MLAPI;
using MLAPI.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Main : MonoBehaviour
{
    public string host = "localhost";
    public int port = 5555;

    private string game;

    public void PlayOffline()
    {
        SceneManager.LoadScene("OfflineGame");
    }

    public void PlayClient(bool host)
    {
        NetworkingManager.Singleton.NetworkConfig.NetworkedPrefabs[0].PlayerPrefab = host;
        NetworkingManager.Singleton.NetworkConfig.NetworkedPrefabs[2].PlayerPrefab = !host;
        NetworkingManager.Singleton.StartClient();
    }

    public void PlayHost()
    {
        game = "HostGame";
        NetworkingManager.Singleton.NetworkConfig.NetworkedPrefabs[0].PlayerPrefab = true;
        NetworkingManager.Singleton.NetworkConfig.NetworkedPrefabs[2].PlayerPrefab = false;
        NetworkingManager.Singleton.OnClientConnectedCallback += CheckPlayers;
        NetworkingManager.Singleton.StartHost();
    }

    public void PlayServer()
    {
        game = "ServerGame";
        NetworkingManager.Singleton.NetworkConfig.NetworkedPrefabs[0].PlayerPrefab = false;
        NetworkingManager.Singleton.NetworkConfig.NetworkedPrefabs[2].PlayerPrefab = true;
        NetworkingManager.Singleton.OnClientConnectedCallback += CheckPlayers;
        NetworkingManager.Singleton.StartServer();
    }

    private void CheckPlayers(ulong client)
    {
        if (NetworkingManager.Singleton.ConnectedClientsList.Count > 1)
        {
            NetworkSceneManager.SwitchScene(game);
        }
    }
}
