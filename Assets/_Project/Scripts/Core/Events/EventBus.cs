using System;
using System.Collections.Generic;
using MoveRush.Core.Utilities;

namespace MoveRush.Core.Events
{
    /// <summary>
    /// Strongly typed, allocation-light publish/subscribe channel used for cross-cutting
    /// notifications between systems that must not reference each other directly
    /// (Dependency Inversion). One static channel exists per event payload type.
    /// </summary>
    /// <typeparam name="T">Immutable event payload type.</typeparam>
    public static class EventBus<T> where T : struct
    {
        private static readonly List<Action<T>> Subscribers = new List<Action<T>>(8);
        private static readonly List<Action<T>> DispatchBuffer = new List<Action<T>>(8);

        /// <summary>
        /// Announces this channel to the registry the first time it is touched, so a single
        /// <see cref="EventBusRegistry.ClearAll"/> call can flush every live channel.
        /// </summary>
        static EventBus()
        {
            EventBusRegistry.Register(Clear);
        }

        /// <summary>Registers a handler. Duplicate registrations are ignored.</summary>
        /// <param name="handler">Callback invoked when the event is published.</param>
        public static void Subscribe(Action<T> handler)
        {
            if (handler == null || Subscribers.Contains(handler))
            {
                return;
            }

            Subscribers.Add(handler);
        }

        /// <summary>Removes a previously registered handler. Safe to call twice.</summary>
        /// <param name="handler">Callback to remove.</param>
        public static void Unsubscribe(Action<T> handler)
        {
            if (handler == null)
            {
                return;
            }

            Subscribers.Remove(handler);
        }

        /// <summary>
        /// Publishes an event to every subscriber. Handlers are copied before dispatch so a
        /// handler may safely subscribe or unsubscribe while the event is being delivered.
        /// A throwing handler is logged and never prevents the remaining handlers from running.
        /// </summary>
        /// <param name="payload">Event data to broadcast.</param>
        public static void Publish(T payload)
        {
            if (Subscribers.Count == 0)
            {
                return;
            }

            DispatchBuffer.Clear();
            DispatchBuffer.AddRange(Subscribers);

            for (int i = 0; i < DispatchBuffer.Count; i++)
            {
                try
                {
                    DispatchBuffer[i].Invoke(payload);
                }
                catch (Exception exception)
                {
                    Log.Exception(exception, $"EventBus<{typeof(T).Name}> handler failed.");
                }
            }

            DispatchBuffer.Clear();
        }

        /// <summary>
        /// Drops every subscriber. Called on application shutdown and before entering play mode
        /// so stale delegates from a previous session cannot leak into the next one.
        /// </summary>
        public static void Clear() => Subscribers.Clear();
    }
}
