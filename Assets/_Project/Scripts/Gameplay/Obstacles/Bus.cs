using MoveRush.Core.Services;

namespace MoveRush.Gameplay.Obstacles
{
    /// <summary>
    /// Short high vehicle. Like the train it can only be dodged, but it occupies far less road,
    /// so it is used to build tight two-lane squeezes without walling the track off.
    /// </summary>
    public class Bus : ObstacleBase
    {
        /// <inheritdoc />
        public override ObstacleAvoidance Avoidance => ObstacleAvoidance.Dodge;

        /// <inheritdoc />
        protected override bool IsAvoidedBy(IPlayerService playerService) => false;
    }
}
