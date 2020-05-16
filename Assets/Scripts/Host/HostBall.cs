using MLAPI;
using MLAPI.Messaging;
using MLAPI.Serialization.Pooled;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class HostBall : NetworkedBehaviour
{
    public float speed = 5f;
    public int left = 0;
    public int right = 0;

    public Text leftText;
    public Text rightText;

    private float angle = 0f;

    private void Start()
    {
        leftText = GameObject.Find("LeftText").GetComponent<Text>();
        rightText = GameObject.Find("RightText").GetComponent<Text>();

        if (!IsServer) GetComponent<Collider2D>().enabled = false;
        else Spawn();
    }

    private void Spawn()
    {
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        angle = Random.Range(45f, 225f);
        if (angle > 135f) angle += 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void FixedUpdate()
    {
        if (IsServer)
            transform.position += transform.up * speed * Time.fixedDeltaTime;
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        switch (col.collider.name)
        {
            case "Roof":
            case "Floor":
                angle = 180f - angle;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
                break;

            case "Left":
                {
                    float yDiff = (transform.position.y - col.transform.position.y + 1f) / 2f;
                    angle = Mathf.Lerp(225f, 315f, yDiff);
                    transform.rotation = Quaternion.Euler(0f, 0f, angle);
                }
                break;

            case "Right":
                {
                    float yDiff = 1f - (transform.position.y - col.transform.position.y + 1f) / 2f;
                    angle = Mathf.Lerp(45f, 135f, yDiff);
                    transform.rotation = Quaternion.Euler(0f, 0f, angle);
                }
                break;
        }
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        switch (col.name)
        {
            case "LeftWall":
                right++;
                break;

            case "RightWall":
                left++;
                break;
        }

        using (PooledBitStream stream = PooledBitStream.Get())
        {
            using (PooledBitWriter writer = PooledBitWriter.Get(stream))
            {
                writer.WriteInt32(left);
                writer.WriteInt32(right);
                InvokeClientRpcOnEveryonePerformance(WinRPC, stream);
            }
        }

        Spawn();
    }

    [ClientRPC]
    private void WinRPC(ulong clientId, Stream stream)
    {
        using (PooledBitReader reader = PooledBitReader.Get(stream))
        {
            left = reader.ReadInt32();
            right = reader.ReadInt32();
            leftText.text = left.ToString();
            rightText.text = right.ToString();
        }
    }
}
