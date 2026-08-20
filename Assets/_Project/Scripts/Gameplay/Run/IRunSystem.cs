namespace MoveRush.Gameplay.Run
{
    /// <summary>
    /// Implemented by every system that owns run state. The director drives them in a defined
    /// order, which is what makes an instant retry possible: nothing is destroyed and rebuilt,
    /// each system simply returns its objects and lays the run out again.
    /// </summary>
    public interface IRunSystem
    {
        /// <summary>Relative reset order. Lower values reset first.</summary>
        int RunOrder { get; }

        /// <summary>Clears run state and rebuilds the starting situation.</summary>
        void OnRunReset();

        /// <summary>Stops run activity after a game over.</summary>
        void OnRunEnded();
    }
}
