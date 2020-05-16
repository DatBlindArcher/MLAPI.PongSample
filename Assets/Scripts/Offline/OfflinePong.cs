using UnityEngine;

public class OfflinePong : MonoBehaviour
{
    public float speed = 5f;

    public KeyCode up = KeyCode.W;
    public KeyCode down = KeyCode.S;

    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        float movement = 0f;

        if (Input.GetKey(up))
            movement += 1f;

        if (Input.GetKey(down))
            movement -= 1f;

        rb.MovePosition(transform.position + new Vector3(0f, movement, 0f) * speed * Time.fixedDeltaTime);
    }
}
