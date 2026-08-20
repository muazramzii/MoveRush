using MoveRush.Core.Services;

namespace MoveRush.Gameplay.Obstacles
{
    /// <summary>
    /// Long carriage filling a whole lane. Nothing but a lane change gets past it, and its
    /// length is what forces the player to commit to a decision early.
    /// </summary>
    public class Train : ObstacleBase
    {
        /// <inheritdoc />
        public override ObstacleAvoidance Avoidance => ObstacleAvoidance.Dodge;

        /// <inheritdoc />
        protected override bool IsAvoidedBy(IPlayerService playerService) => false;
    }
}
