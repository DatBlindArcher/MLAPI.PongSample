using MLAPI;
using MLAPI.Serialization.Pooled;
using System.IO;
using UnityEngine;

public class HostPong : NetworkedBehaviour
{
    public float speed = 5f;

    public KeyCode up = KeyCode.W;
    public KeyCode down = KeyCode.S;

    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void NetworkStart(Stream stream)
    {
        using (PooledBitReader reader = PooledBitReader.Get(stream))
        {
            string name = reader.ReadString().ToString();
            gameObject.name = name;
        }
    }

    private void FixedUpdate()
    {
        if (IsOwner)
        {
            float movement = 0f;

            if (Input.GetKey(up))
                movement += 1f;

            if (Input.GetKey(down))
                movement -= 1f;

            rb.MovePosition(transform.position + new Vector3(0f, movement, 0f) * speed * Time.fixedDeltaTime);
        }
    }
}
