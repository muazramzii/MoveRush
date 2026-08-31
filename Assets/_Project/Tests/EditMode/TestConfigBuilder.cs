using System;
using System.Collections;
using System.Reflection;
using MoveRush.Gameplay.Config;
using UnityEngine;

namespace MoveRush.Tests
{
    /// <summary>
    /// Builds configured ScriptableObject instances for tests.
    /// Production configs expose their values as read-only properties over private serialised
    /// fields, which is correct for shipping code but leaves tests with no way to set up a
    /// scenario. Rather than widening the production API purely for testing, the builder writes
    /// the backing fields through reflection - the same thing the Unity inspector does.
    /// </summary>
    public static class TestConfigBuilder
    {
        private const BindingFlags FieldFlags =
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        /// <summary>Creates a ScriptableObject and writes the given backing fields.</summary>
        /// <typeparam name="T">ScriptableObject type to create.</typeparam>
        /// <param name="fields">Field name and value pairs to write.</param>
        /// <returns>The configured instance.</returns>
        public static T Create<T>(params (string Name, object Value)[] fields) where T : ScriptableObject
        {
            T instance = ScriptableObject.CreateInstance<T>();

            if (fields != null)
            {
                foreach ((string name, object value) in fields)
                {
                    SetField(instance, name, value);
                }
            }

            return instance;
        }

        /// <summary>Writes one private or public instance field, searching up the hierarchy.</summary>
        /// <param name="target">Object that owns the field.</param>
        /// <param name="fieldName">Exact field name.</param>
        /// <param name="value">Value to write.</param>
        /// <exception cref="ArgumentException">Thrown when the field does not exist.</exception>
        public static void SetField(object target, string fieldName, object value)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            FieldInfo field = FindField(target.GetType(), fieldName);

            if (field == null)
            {
                throw new ArgumentException(
                    $"'{target.GetType().Name}' has no field named '{fieldName}'. " +
                    "The test is out of date with the production class.");
            }

            field.SetValue(target, value);
        }

        /// <summary>Reads one private or public instance field.</summary>
        /// <typeparam name="TValue">Expected field type.</typeparam>
        /// <param name="target">Object that owns the field.</param>
        /// <param name="fieldName">Exact field name.</param>
        /// <returns>The current field value.</returns>
        public static TValue GetField<TValue>(object target, string fieldName)
        {
            FieldInfo field = FindField(target.GetType(), fieldName);

            if (field == null)
            {
                throw new ArgumentException($"'{target.GetType().Name}' has no field named '{fieldName}'.");
            }

            return (TValue)field.GetValue(target);
        }

        /// <summary>
        /// Builds a difficulty ramp from distance and speed pairs, constructing the private
        /// <see cref="SpeedStep"/> entries the asset stores.
        /// </summary>
        /// <param name="acceleration">Metres per second gained per second while easing.</param>
        /// <param name="steps">Distance and speed pairs, ordered by distance.</param>
        /// <returns>The configured ramp.</returns>
        public static DifficultyConfig CreateDifficulty(float acceleration, params (float Distance, float Speed)[] steps)
        {
            DifficultyConfig config = ScriptableObject.CreateInstance<DifficultyConfig>();
            SetField(config, "acceleration", acceleration);

            IList list = GetField<IList>(config, "steps");
            list.Clear();

            foreach ((float distance, float speed) in steps)
            {
                SpeedStep step = new SpeedStep();
                SetField(step, "distance", distance);
                SetField(step, "speed", speed);
                list.Add(step);
            }

            return config;
        }

        /// <summary>Walks the type hierarchy looking for a field by name.</summary>
        /// <param name="type">Type to start from.</param>
        /// <param name="fieldName">Exact field name.</param>
        /// <returns>The field, or null when it does not exist.</returns>
        private static FieldInfo FindField(Type type, string fieldName)
        {
            while (type != null)
            {
                FieldInfo field = type.GetField(fieldName, FieldFlags);

                if (field != null)
                {
                    return field;
                }

                type = type.BaseType;
            }

            return null;
        }
    }
}
