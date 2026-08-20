using MoveRush.Core.Services;

namespace MoveRush.Gameplay.Obstacles
{
    /// <summary>
    /// Beam suspended above the road. Only a slide gets under it, so it is the counterpart to
    /// the barrier and the reason a run needs both vertical moves.
    /// </summary>
    public class LaserGate : ObstacleBase
    {
        /// <inheritdoc />
        public override ObstacleAvoidance Avoidance => ObstacleAvoidance.Slide;

        /// <inheritdoc />
        protected override bool IsAvoidedBy(IPlayerService playerService) => playerService.IsSliding;
    }
}
