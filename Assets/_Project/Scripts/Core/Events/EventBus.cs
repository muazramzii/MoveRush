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

        /// <summary>
        /// Spare dispatch buffers. Each publish takes its own buffer and gives it back afterwards,
        /// so a nested publish cannot disturb the one already in flight, while the steady state
        /// still reuses buffers instead of allocating one per event.
        /// </summary>
        private static readonly Stack<List<Action<T>>> BufferPool = new Stack<List<Action<T>>>(2);

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
        /// Publishes an event to every subscriber. Handlers are copied into a buffer owned by this
        /// call, so a handler may safely subscribe, unsubscribe, or publish this same event type
        /// again without disturbing the dispatch already in flight.
        /// A throwing handler is logged and never prevents the remaining handlers from running.
        /// </summary>
        /// <param name="payload">Event data to broadcast.</param>
        public static void Publish(T payload)
        {
            if (Subscribers.Count == 0)
            {
                return;
            }

            List<Action<T>> buffer = RentBuffer();
            buffer.AddRange(Subscribers);

            try
            {
                for (int i = 0; i < buffer.Count; i++)
                {
                    try
                    {
                        buffer[i].Invoke(payload);
                    }
                    catch (Exception exception)
                    {
                        Log.Exception(exception, $"EventBus<{typeof(T).Name}> handler failed.");
                    }
                }
            }
            finally
            {
                ReturnBuffer(buffer);
            }
        }

        /// <summary>Takes a dispatch buffer, reusing a spare one when the pool has any.</summary>
        /// <returns>An empty buffer owned by the current publish.</returns>
        private static List<Action<T>> RentBuffer()
        {
            return BufferPool.Count > 0 ? BufferPool.Pop() : new List<Action<T>>(8);
        }

        /// <summary>Empties a dispatch buffer and puts it back for the next publish.</summary>
        /// <param name="buffer">Buffer to recycle.</param>
        private static void ReturnBuffer(List<Action<T>> buffer)
        {
            buffer.Clear();
            BufferPool.Push(buffer);
        }

        /// <summary>
        /// Drops every subscriber. Called on application shutdown and before entering play mode
        /// so stale delegates from a previous session cannot leak into the next one.
        /// </summary>
        public static void Clear() => Subscribers.Clear();
    }
}
