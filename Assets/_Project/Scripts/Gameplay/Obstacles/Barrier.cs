using MoveRush.Core.Services;

namespace MoveRush.Gameplay.Obstacles
{
    /// <summary>
    /// Low barrier. It is cleared by jumping over it or by taking another lane, which makes it
    /// the obstacle that teaches the jump without ever forcing a lane change.
    /// </summary>
    public class Barrier : ObstacleBase
    {
        /// <inheritdoc />
        public override ObstacleAvoidance Avoidance => ObstacleAvoidance.Jump;

        /// <inheritdoc />
        protected override bool IsAvoidedBy(IPlayerService playerService) => playerService.IsAirborne;
    }
}
