using MLAPI.Serialization.Pooled;
using UnityEngine;

public class ServerBall : Entity<BallState>
{
    public float speed = 5f;

    // TODO: Replace with custom physcs?
    public float physicsRadius = 1f;

    public override void Present()
    {
        transform.position = CurrentState.Position;
        transform.rotation = Quaternion.Euler(0f, 0f, CurrentState.Angle);
    }

    protected override BallState StartTick(uint tick)
    {
        Vector2 newPosition = Vector3.zero;
        transform.rotation = Quaternion.identity;
        Random.InitState((int)tick);
        float angle = Random.Range(45f, 225f);
        if (angle > 135f) angle += 90f;
        return new BallState(tick, newPosition, angle);
    }

    protected override BallState StateTick(uint tick, BallState previousState)
    {
        float angle = previousState.Angle;
        float distance = speed * EntityNetwork.Singleton.FixedTimeStep;
        Vector2 newPosition = previousState.Position;

        while (distance > 0)
        {
            Vector2 prev = newPosition;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            newPosition += (Vector2)transform.up * distance;
            Vector2 movement = newPosition - prev;

            // TODO: Replace with custom physcs?
            RaycastHit2D hit = Physics2D.CircleCast(prev, physicsRadius, movement, distance);

            if (hit)
            {
                newPosition = hit.point + (hit.normal * physicsRadius);
                OnColliderHit2D(hit, tick, ref angle, ref newPosition);
                distance -= (prev - newPosition).magnitude;
            }

            else distance = 0f;
        }

        transform.position = newPosition;
        return new BallState(tick, newPosition, angle);
    }

    private void OnColliderHit2D(RaycastHit2D hit, uint tick, ref float angle, ref Vector2 position)
    {
        Debug.Log(hit.collider.name);

        switch (hit.collider.name)
        {
            case "Roof":
            case "Floor":
                angle = 180f - angle;
                break;

            case "Left":
                {
                    float yDiff = (hit.point.y - hit.collider.transform.position.y + 1f) / 2f;
                    angle = Mathf.Lerp(225f, 315f, yDiff);
                }
                break;

            case "Right":
                {
                    float yDiff = 1f - (hit.point.y - hit.collider.transform.position.y + 1f) / 2f;
                    angle = Mathf.Lerp(45f, 135f, yDiff);
                }
                break;

            case "LeftWall":
                ServerGameState.Right++;
                position = Vector3.zero;
                Random.InitState((int)tick);
                angle = Random.Range(45f, 225f);
                if (angle > 135f) angle += 90f;
                break;

            case "RightWall":
                ServerGameState.Left++;
                position = Vector3.zero;
                Random.InitState((int)tick);
                angle = Random.Range(45f, 225f);
                if (angle > 135f) angle += 90f;
                break;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.up * speed * Time.fixedDeltaTime, physicsRadius);
    }
}

public class BallState : IEntityState
{
    public uint Tick { get; }
    public bool IsServerState { get; }
    public Vector2 Position { get; private set; }
    public float Angle { get; private set; }

    public BallState(uint tick, Vector2 position, float angle)
    {
        Tick = tick;
        Position = position;
        Angle = angle;
        IsServerState = EntityNetwork.Singleton.IsServer;
    }

    public BallState(uint tick, PooledBitReader reader)
    {
        Tick = tick;
        IsServerState = true;
        Position = reader.ReadVector2();
        Angle = reader.ReadSingle();
    }

    public void Serialize(PooledBitWriter writer)
    {
        writer.WriteVector2(Position);
        writer.WriteSingle(Angle);
    }
}