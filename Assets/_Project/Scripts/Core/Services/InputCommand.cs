namespace MoveRush.Core.Services
{
    /// <summary>Discrete actions the player character understands.</summary>
    public enum InputCommand
    {
        /// <summary>No action.</summary>
        None = 0,

        /// <summary>Move one lane towards negative X.</summary>
        MoveLeft = 1,

        /// <summary>Move one lane towards positive X.</summary>
        MoveRight = 2,

        /// <summary>Leave the ground.</summary>
        Jump = 3,

        /// <summary>Duck under an obstacle.</summary>
        Slide = 4
    }
}
