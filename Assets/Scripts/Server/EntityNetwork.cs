using MLAPI.Serialization.Pooled;
using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.Messaging;

// TODO:
// - fixable?: glitchy ball physics on player hit with latency
// - Server state score
// Features TODO:
// - Catch up and slow down tick on client side
// - Deviation could be calculated
// - Custom Physics

public class EntityNetwork : MonoBehaviour
{
    [Range(10, 120)]
    public uint TickRate = 60;
    public bool Statistics = true;

    public uint Deviation = 5;

    public uint CurrentTick { get; private set; }
    public uint LastPredictedTick { get; private set; }
    public double FixedStep => 1000.0 / TickRate;
    public float FixedTimeStep => (float)FixedStep / 1000f;
    public double CurrentTime => timer.Elapsed.TotalMilliseconds;
    public bool IsServer => NetworkingManager.Singleton.IsServer;
    public double RTT => IsServer ? 0 : transport.GetCurrentRtt(0) + 2 * FixedStep;

    private LiteNetLibTransport.LiteNetLibTransport transport;

    private Stopwatch timer;
    private double LastUpdate;

    public Dictionary<ulong, Entity> entities;

    #region Singleton
    public static EntityNetwork Singleton { get; private set; }

    private void Awake()
    {
        if (Singleton != null)
        {
            Destroy(gameObject);
            return;
        }

        Singleton = this;
        // TODO: DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Singleton == this)
        {
            CustomMessagingManager.UnregisterNamedMessageHandler("NetworkInput");
            CustomMessagingManager.UnregisterNamedMessageHandler("NetworkState");
            Singleton = null;
        }
    }
    #endregion

    private void Start()
    {
        timer = Stopwatch.StartNew();
        entities = new Dictionary<ulong, Entity>();
        LastUpdate = CurrentTime;

        transport = NetworkingManager.Singleton.GetComponent<LiteNetLibTransport.LiteNetLibTransport>();
        CustomMessagingManager.RegisterNamedMessageHandler("NetworkInput", ReceiveInput);
        CustomMessagingManager.RegisterNamedMessageHandler("NetworkState", ReceiveState);
    }

    private void OnGUI()
    {
        if (Statistics)
        {
            GUILayout.TextArea($"FPS: {(1f / Time.smoothDeltaTime).ToString("0")}");
            GUILayout.TextArea($"Ping: {(RTT / 2).ToString("0")}");
            GUILayout.TextArea($"Tick: {CurrentTick}");
            GUILayout.TextArea($"PTick: {LastPredictedTick}");
            GUILayout.TextArea($"Catch: {false}");
            GUILayout.TextArea($"Slow: {false}");
            GUILayout.TextArea($"I: {0f.ToString("0.0")} KB/s");
            GUILayout.TextArea($"O: {0f.ToString("0.0")} KB/s");
        }
    }

    private void Update()
    {
        double time = CurrentTime;
        int ticks = (int)Math.Floor((time - LastUpdate) / FixedStep);

        // Ticks
        for (int i = 0; i < ticks; i++)
            Tick(++CurrentTick);

        LastUpdate += ticks * FixedStep;
    }

    internal void Tick(uint tick)
    {
        if (IsServer)
        {
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteUInt32(tick);
                    Dictionary<ulong, IEntityState> states = new Dictionary<ulong, IEntityState>(entities.Count);

                    foreach (Entity entity in entities.Values)
                    {
                        IEntityState state = entity.DoStateTick(tick);
                        if (state != null) states.Add(entity.NetworkId, state);
                    }

                    writer.WriteInt32(states.Count);

                    foreach (var state in states)
                    {
                        writer.WriteUInt64(state.Key);
                        state.Value.Serialize(writer);
                    }

                    CustomMessagingManager.SendNamedMessage("NetworkState", null, stream);
                }
            }
        }

        else
        {
            foreach (Entity entity in entities.Values)
                entity.DoStateTick(tick);

            uint predictTicks = (uint)Math.Ceiling(RTT / FixedStep) + 2 * Deviation;

            for (uint i = 0; i < predictTicks; i++)
            {
                if (tick + i > LastPredictedTick)
                {
                    using (PooledBitStream stream = PooledBitStream.Get())
                    {
                        using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                        {
                            LastPredictedTick = tick + i;
                            var authEntities = entities.Values.Where(e => e.IsOwner);
                            int count = authEntities.Count();
                            Dictionary<ulong, IEntityInput> inputs = new Dictionary<ulong, IEntityInput>(count);

                            foreach (Entity entity in authEntities)
                            {
                                IEntityInput input = entity.DoInputTick(tick + i);
                                if (input != null) inputs.Add(entity.NetworkId, input);
                            }

                            writer.WriteUInt32(tick + i);
                            writer.WriteInt32(inputs.Count);

                            foreach (var input in inputs)
                            {
                                writer.WriteUInt64(input.Key);
                                input.Value.Serialize(writer);
                            }

                            CustomMessagingManager.SendNamedMessage("NetworkInput", NetworkingManager.Singleton.ServerClientId, stream);
                        }
                    }
                }

                foreach (Entity entity in entities.Values.Where(e => e.extrapolate || e.IsOwner))
                    entity.DoStateTick(tick + i);
            }

            foreach (Entity entity in entities.Values)
                entity.Present();
        }
    }

    // Client Side
    internal void ReceiveState(ulong clientId, Stream stream)
    {
        using (PooledBitReader reader = PooledBitReader.Get(stream))
        {
            uint tick = reader.ReadUInt32();

            if (tick <= CurrentTick)
            {
                CurrentTick = tick - Deviation;
                LastPredictedTick = CurrentTick;
                UnityEngine.Debug.LogWarning("State tick came to late: " + (CurrentTick - tick));
                return;
            }

            if (tick - CurrentTick > Deviation)
            {
                CurrentTick = tick - Deviation;
                LastPredictedTick = CurrentTick;
                UnityEngine.Debug.LogWarning("Syncing Tick.");
            }

            int entities = reader.ReadInt32();

            for (int i = 0; i < entities; i++)
            {
                ulong entityId = reader.ReadUInt64();
                this.entities[entityId].ReceiveState(tick, reader);
            }
        }
    }

    // Server Side per client
    internal void ReceiveInput(ulong clientId, Stream stream)
    {
        using (PooledBitReader reader = PooledBitReader.Get(stream))
        {
            uint tick = reader.ReadUInt32();

            if (tick <= CurrentTick)
            {
                UnityEngine.Debug.LogWarning("Input tick came to late");
                return;
            }

            int entities = reader.ReadInt32();

            for (int i = 0; i < entities; i++)
            {
                ulong entityId = reader.ReadUInt64();
                this.entities[entityId].ReceiveInput(tick, reader);
            }
        }
    }
}