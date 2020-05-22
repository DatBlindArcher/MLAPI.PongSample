using MLAPI;
using MLAPI.Serialization.Pooled;
using System.Collections.Generic;
using System.IO;

public abstract class Entity : NetworkedBehaviour
{
    public bool extrapolate = false;

    public override void NetworkStart(Stream stream)
    {
        // TODO: gameObject.SetActive(false);

        using (PooledBitReader reader = PooledBitReader.Get(stream))
        {
            string name = reader.ReadString().ToString();
            gameObject.name = name;

            if (gameObject.GetComponent<ServerPong>() is ServerPong pong)
                pong.position = gameObject.transform.position.x;
        }

        EntityNetwork.Singleton.entities.Add(NetworkId, this);
    }

    protected virtual void OnDestroy()
    {
        EntityNetwork.Singleton?.entities?.Remove(NetworkId);
    }

    internal abstract IEntityState DoStateTick(uint tick);
    internal virtual IEntityInput DoInputTick(uint tick) { return null; }
    internal virtual void ReceiveState(uint tick, PooledBitReader reader) { }
    internal virtual void ReceiveInput(uint tick, PooledBitReader reader) { }
    public abstract void Present();
}

public abstract class Entity<TState> : Entity where TState : IEntityState
{
    public TState CurrentState { get; set; } = default(TState);

    internal readonly LinkedList<TState> stateBuffer = new LinkedList<TState>();

    internal sealed override void ReceiveState(uint tick, PooledBitReader reader)
    {
        TState state = (TState)System.Activator.CreateInstance(typeof(TState), tick, reader);

        var node = stateBuffer.First;

        while (node != null)
        {
            if (node.Value.Tick > tick)
            {
                stateBuffer.AddBefore(node, state);
                return;
            }

            if (node.Value.Tick == tick)
            {
                stateBuffer.AddBefore(node, state);
                stateBuffer.Remove(node);
                return;
            }

            node = node.Next;
        }

        stateBuffer.AddLast(state);
    }

    internal sealed override IEntityState DoStateTick(uint tick)
    {
        if (EntityNetwork.Singleton.IsServer)
        {
            if ((CurrentState?.Tick ?? 0) == 0) CurrentState = StartTick(tick);
            CurrentState = StateTick(tick, CurrentState);
        }

        else
        {
            while (stateBuffer.Count > 0 && stateBuffer.First.Value.Tick < EntityNetwork.Singleton.CurrentTick)
                stateBuffer.RemoveFirst();

            if (stateBuffer.Count > 0 && stateBuffer.First.Value.Tick == tick)
                CurrentState = stateBuffer.First.Value;

            else
            {
                if ((CurrentState?.Tick ?? 0) == 0) CurrentState = StartTick(tick);
                CurrentState = StateTick(tick, CurrentState);
            }
        }

        return CurrentState;
    }

    protected abstract TState StartTick(uint tick);
    protected abstract TState StateTick(uint tick, TState previousState);
}

public abstract class Entity<TInput, TState> : Entity<TState> where TInput : IEntityInput where TState : IEntityState
{
    public TInput CurrentInput { get; set; } = default(TInput);

    internal readonly LinkedList<TInput> inputBuffer = new LinkedList<TInput>();

    internal sealed override void ReceiveInput(uint tick, PooledBitReader reader)
    {
        TInput input = (TInput)System.Activator.CreateInstance(typeof(TInput), tick, reader);

        var node = inputBuffer.First;

        while (node != null)
        {
            if (node.Value.Tick > tick)
            {
                inputBuffer.AddBefore(node, input);
                return;
            }

            if (node.Value.Tick == tick)
                return;

            node = node.Next;
        }

        inputBuffer.AddLast(input);
    }

    internal sealed override IEntityInput DoInputTick(uint tick)
    {
        while (inputBuffer.Count > 0 && inputBuffer.First.Value.Tick < EntityNetwork.Singleton.CurrentTick)
            inputBuffer.RemoveFirst();

        TInput input = InputTick(tick);
        inputBuffer.AddLast(input);
        return input;
    }

    protected abstract TInput InputTick(uint tick);
    protected abstract TInput NextInputTick(uint tick, TInput previousInput);
    protected abstract TState ApplyInputToState(uint tick, TState state, TInput input);

    protected sealed override TState StateTick(uint tick, TState lastState)
    {
        if (EntityNetwork.Singleton.IsServer)
        {
            TState state;

            while (inputBuffer.Count > 0 && inputBuffer.First.Value.Tick < tick)
                inputBuffer.RemoveFirst();

            if (inputBuffer.Count > 0 && inputBuffer.First.Value.Tick == tick)
            {
                CurrentInput = inputBuffer.First.Value;
                state = ApplyInputToState(tick, lastState, CurrentInput);
                inputBuffer.RemoveFirst();
            }

            else
            {
                CurrentInput = NextInputTick(tick, CurrentInput);
                state = ApplyInputToState(tick, lastState, CurrentInput);
            }

            return state;
        }

        else
        {
            foreach (TInput input in inputBuffer)
            {
                if (input.Tick == tick)
                {
                    CurrentInput = input;
                    break;
                }
            }

            return ApplyInputToState(tick, lastState, CurrentInput);
        }
    }
}