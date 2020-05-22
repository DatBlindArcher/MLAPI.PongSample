using MLAPI.Serialization.Pooled;

public interface IEntityInput
{
    uint Tick { get; }
    void Serialize(PooledBitWriter writer);
}