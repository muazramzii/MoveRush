using System;
using System.Collections.Generic;
using MoveRush.Core.Utilities;

namespace MoveRush.Core.Services
{
    /// <summary>
    /// Minimal composition root used to resolve long lived services by their contract.
    /// Consumers depend on abstractions (<see cref="ISaveService"/>, <see cref="IAudioService"/>,
    /// <see cref="IUIService"/>) instead of concrete MonoBehaviour singletons, which keeps the
    /// Dependency Inversion Principle intact and allows any service to be faked in tests.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>(16);

        /// <summary>Registers a service instance under the contract <typeparamref name="TContract"/>.</summary>
        /// <typeparam name="TContract">Interface consumers will resolve.</typeparam>
        /// <param name="instance">Concrete implementation to store.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="instance"/> is null.</exception>
        public static void Register<TContract>(TContract instance) where TContract : class
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            Type contract = typeof(TContract);
            if (Services.ContainsKey(contract))
            {
                Log.Warning($"ServiceLocator: '{contract.Name}' was already registered and will be replaced.");
            }

            Services[contract] = instance;
        }

        /// <summary>Removes a previously registered service.</summary>
        /// <typeparam name="TContract">Contract to unregister.</typeparam>
        public static void Unregister<TContract>() where TContract : class
        {
            Services.Remove(typeof(TContract));
        }

        /// <summary>Resolves a registered service.</summary>
        /// <typeparam name="TContract">Contract to resolve.</typeparam>
        /// <returns>The registered instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown when nothing is registered for the contract.</exception>
        public static TContract Get<TContract>() where TContract : class
        {
            if (Services.TryGetValue(typeof(TContract), out object service))
            {
                return (TContract)service;
            }

            throw new InvalidOperationException(
                "ServiceLocator: no instance registered for '" + typeof(TContract).Name +
                "'. Is the Bootstrap scene the first entry in the build settings?");
        }

        /// <summary>Attempts to resolve a service without throwing.</summary>
        /// <typeparam name="TContract">Contract to resolve.</typeparam>
        /// <param name="service">Resolved instance, or null when nothing is registered.</param>
        /// <returns>True when the service was found.</returns>
        public static bool TryGet<TContract>(out TContract service) where TContract : class
        {
            if (Services.TryGetValue(typeof(TContract), out object stored))
            {
                service = (TContract)stored;
                return true;
            }

            service = null;
            return false;
        }

        /// <summary>Returns true when a service is registered for the given contract.</summary>
        /// <typeparam name="TContract">Contract to test.</typeparam>
        /// <returns>True when registered.</returns>
        public static bool IsRegistered<TContract>() where TContract : class
        {
            return Services.ContainsKey(typeof(TContract));
        }

        /// <summary>Clears every registration. Called when the application shuts down.</summary>
        public static void Clear() => Services.Clear();
    }
}
