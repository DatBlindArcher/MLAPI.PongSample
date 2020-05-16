using UnityEngine;

public abstract class Entity : MonoBehaviour
{

}

public abstract class Entity<T> : Entity where T : EntityState
{

}

public interface EntityInput
{
    void Serialize();
}

public interface EntityState
{
    void Deserialize();
}