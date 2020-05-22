using MLAPI.Serialization.Pooled;

public interface IEntityState
{
    uint Tick { get; }
    void Serialize(PooledBitWriter writer);
}