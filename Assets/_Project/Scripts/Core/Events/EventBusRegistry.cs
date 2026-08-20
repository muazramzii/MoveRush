using System;
using System.Collections.Generic;

namespace MoveRush.Core.Events
{
    /// <summary>
    /// Tracks every <see cref="EventBus{T}"/> channel that has been used so all of them can be
    /// flushed in one call. This matters when "Enter Play Mode Options" disables the domain
    /// reload: without a flush, delegates from the previous play session would survive and fire
    /// against destroyed objects.
    /// </summary>
    public static class EventBusRegistry
    {
        private static readonly List<Action> Cleaners = new List<Action>(16);

        /// <summary>Registers a channel's clear delegate. Called by the channel's static constructor.</summary>
        /// <param name="clearAction">Delegate that drops the channel's subscribers.</param>
        public static void Register(Action clearAction)
        {
            if (clearAction != null)
            {
                Cleaners.Add(clearAction);
            }
        }

        /// <summary>Drops the subscribers of every channel that has been used this session.</summary>
        public static void ClearAll()
        {
            for (int i = 0; i < Cleaners.Count; i++)
            {
                Cleaners[i].Invoke();
            }
        }
    }
}
