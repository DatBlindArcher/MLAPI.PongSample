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

    public void PlayClient()
    {
        NetworkingManager.Singleton.StartClient();
    }

    public void PlayHost()
    {
        game = "HostGame";
        NetworkingManager.Singleton.OnClientConnectedCallback += CheckPlayers;
        NetworkingManager.Singleton.StartHost();
    }

    public void PlayServer()
    {
        game = "ServerGame";
        NetworkingManager.Singleton.OnClientConnectedCallback += CheckPlayers;
        NetworkingManager.Singleton.StartHost();
    }

    private void CheckPlayers(ulong client)
    {
        if (NetworkingManager.Singleton.ConnectedClientsList.Count > 1)
        {
            NetworkSceneManager.SwitchScene(game);
        }
    }
}
