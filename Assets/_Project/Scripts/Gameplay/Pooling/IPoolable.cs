namespace MoveRush.Gameplay.Pooling
{
    /// <summary>
    /// Implemented by anything that is reused from a pool. A pooled object is never freshly
    /// constructed, so every field that a run mutates has to be reset in
    /// <see cref="OnSpawnedFromPool"/> - that reset is what replaces the constructor.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>Called after the object is activated and before it is used.</summary>
        void OnSpawnedFromPool();

        /// <summary>Called before the object is deactivated and handed back to the pool.</summary>
        void OnReturnedToPool();
    }
}
