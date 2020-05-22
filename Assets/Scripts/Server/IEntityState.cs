using MLAPI.Serialization.Pooled;

public interface IEntityState
{
    uint Tick { get; }
    bool IsServerState { get; }
    void Serialize(PooledBitWriter writer);
}