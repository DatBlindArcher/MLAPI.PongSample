using MLAPI;
using MLAPI.Messaging;
using MLAPI.Serialization.Pooled;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ServerSpawn : NetworkedBehaviour
{
    public GameObject pong;
    public GameObject ball;

    private List<ulong> clients;

    private void Start()
    {
        if (NetworkingManager.Singleton.IsServer)
            clients = new List<ulong>();

        if (NetworkingManager.Singleton.IsClient)
        {
            using (PooledBitStream stream = PooledBitStream.Get())
                InvokeServerRpcPerformance(ReadyRPC, stream);
        }
    }

    [ServerRPC(RequireOwnership = false)]
    private void ReadyRPC(ulong clientId, Stream stream)
    {
        using (PooledBitReader reader = PooledBitReader.Get(stream))
        {
            clients.Add(clientId);

            if (clients.Count == 2)
            {
                SpawnPlayer(true, clients[0]);
                SpawnPlayer(false, clients[1]);
                SpawnBall();
            }
        }
    }

    private void SpawnPlayer(bool left, ulong clientId)
    {
        var netObj = Instantiate(pong, new Vector3(left ? -6f : 6f, 0f), Quaternion.identity).GetComponent<NetworkedObject>();

        using (PooledBitStream stream = PooledBitStream.Get())
        {
            using (PooledBitWriter writer = PooledBitWriter.Get(stream))
            {
                writer.WriteString(left ? "Left" : "Right");
                netObj.SpawnAsPlayerObject(clientId, stream, true);
            }
        }
    }

    private void SpawnBall()
    {
        var netObj = Instantiate(ball).GetComponent<NetworkedObject>();
        using (PooledBitStream stream = PooledBitStream.Get())
        {
            using (PooledBitWriter writer = PooledBitWriter.Get(stream))
            {
                writer.WriteString("Ball");
                netObj.Spawn(stream, true);
            }
        }
    }
}
