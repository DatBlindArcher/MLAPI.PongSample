using MLAPI.Serialization.Pooled;
using System;
using UnityEngine;

public class ServerPong : Entity<PongInput, PongState>
{
    public float position = 6f;
    public float speed = 5f;

    // TODO: Replace with custom physcs?
    public float size = 1.28f;
    public LayerMask mask;

    public KeyCode up = KeyCode.W;
    public KeyCode down = KeyCode.S;

    protected override PongState StartTick(uint tick)
    {
        return new PongState(tick, 0f);
    }

    protected override PongInput InputTick(uint tick)
    {
        MovementType movement = MovementType.None;
        if (Input.GetKey(up)) movement |= MovementType.Up;
        if (Input.GetKey(down)) movement |= MovementType.Down;
        return new PongInput(tick, movement);
    }

    protected override PongState ApplyInputToState(uint tick, PongState state, PongInput input)
    {
        float height = state.Height;
        float m = 0f;
        if ((input?.Movement & MovementType.Up) != 0) m += 1f;
        if ((input?.Movement & MovementType.Down) != 0) m -= 1f;

        // TODO: Replace with custom physcs?
        RaycastHit2D hit = Physics2D.Raycast(new Vector2(position, state.Height), m >= 0f ? Vector2.up : Vector2.down, Mathf.Abs(m) * (speed * EntityNetwork.Singleton.FixedTimeStep + size / 2f), mask);
        if (hit) height = hit.point.y + (m > 0f ? -size : size) / 2f;
        else height += m * speed * EntityNetwork.Singleton.FixedTimeStep;

        transform.position = new Vector3(position, height, 0f);
        return new PongState(tick, height);
    }

    public override void Present()
    {
        transform.position = new Vector3(position, CurrentState.Height, 0f);
    }

    protected override PongInput NextInputTick(uint tick, PongInput previousInput)
    {
        return new PongInput(tick, previousInput?.Movement ?? 0f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.up * (speed * Time.fixedDeltaTime + size / 2f));
    }
}

public class PongInput : IEntityInput
{
    public uint Tick { get; }
    public MovementType Movement { get; private set; }

    public PongInput(uint tick, MovementType movement)
    {
        Tick = tick;
        Movement = movement;
    }

    public PongInput(uint tick, PooledBitReader reader)
    {
        Tick = tick;
        Movement = (MovementType)reader.ReadByte();
    }

    public void Serialize(PooledBitWriter writer)
    {
        writer.WriteByte((byte)Movement);
    }
}

public class PongState : IEntityState
{
    public uint Tick { get; }
    public bool IsServerState { get; }
    public float Height { get; private set; }

    public PongState(uint tick, float height)
    {
        Tick = tick;
        Height = height;
        IsServerState = EntityNetwork.Singleton.IsServer;
    }

    public PongState(uint tick, PooledBitReader reader)
    {
        Tick = tick;
        IsServerState = true;
        Height = reader.ReadSingle();
    }

    public void Serialize(PooledBitWriter writer)
    {
        writer.WriteSingle(Height);
    }
}

[Flags]
public enum MovementType : byte
{
    None = 0,
    Up = 1,
    Down = 2
}