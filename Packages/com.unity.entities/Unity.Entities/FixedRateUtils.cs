using System.ComponentModel;
using Unity.Core;
using Unity.Mathematics;

namespace Unity.Entities
{
    public static class FixedRateUtils
    {
        /// <summary>
        /// Configure the given ComponentSystemGroup to update at a fixed timestep, given by timeStep.
        /// If the interval between the current time and the last update is bigger than the timestep,
        /// the group's systems will be updated more than once.
        /// </summary>
        /// <param name="group">The group whose UpdateCallback will be configured with a fixed time step update call</param>
        /// <param name="timeStep">The fixed time step (in seconds)</param>
        public static void EnableFixedRateWithCatchUp(ComponentSystemGroup group, float timeStep)
        {
            var manager = new FixedRateCatchUpManager(timeStep);
            group.UpdateCallback = manager.UpdateCallback;
        }

        /// <summary>
        /// Configure the given ComponentSystemGroup to update at a fixed timestep, given by timeStep.
        /// The group will always be ticked exactly once, and the time will be the given timeStep since
        /// the last time it was ticked.  This clock will drift from actual elapsed wall clock time.
        /// </summary>
        /// <param name="group">The group whose UpdateCallback will be configured with a fixed time step update call</param>
        /// <param name="timeStep">The fixed time step (in seconds)</param>
        public static void EnableFixedRateSimple(ComponentSystemGroup group, float timeStep)
        {
            var manager = new FixedRateSimpleManager(timeStep);
            group.UpdateCallback = manager.UpdateCallback;
        }

        /// <summary>
        /// Disable fixed rate updates on the given group, by setting the UpdateCallback to null.
        /// </summary>
        /// <param name="group">The group whose UpdateCallback to set to null.</param>
        public static void DisableFixedRate(ComponentSystemGroup group)
        {
            group.UpdateCallback = null;
        }

        internal class FixedRateSimpleManager
        {
            protected float m_FixedTimeStep;
            protected double m_LastFixedUpdateTime;
            protected bool m_DidPushTime;

            internal FixedRateSimpleManager(float fixedStep)
            {
                m_FixedTimeStep = fixedStep;
            }

            internal bool UpdateCallback(ComponentSystemGroup group)
            {
                // if this is true, means we're being called a second or later time in a loop
                if (m_DidPushTime)
                {
                    group.World.PopTime();
                    m_DidPushTime = false;
                    return false;
                }

                group.World.PushTime(new TimeData(
                    elapsedTime: m_LastFixedUpdateTime,
                    deltaTime: m_FixedTimeStep));

                m_LastFixedUpdateTime += m_FixedTimeStep;

                m_DidPushTime = true;
                return true;
            }
        }

        internal class FixedRateCatchUpManager
        {
            protected float m_FixedTimeStep;
            protected double m_LastFixedUpdateTime;
            protected long m_FixedUpdateCount;
            protected bool m_DidPushTime;

            internal FixedRateCatchUpManager(float fixedStep)
            {
                m_FixedTimeStep = fixedStep;
            }

            internal bool UpdateCallback(ComponentSystemGroup group)
            {
                // if this is true, means we're being called a second or later time in a loop
                if (m_DidPushTime)
                {
                    group.World.PopTime();
                }

                var elapsedTime = group.World.Time.ElapsedTime;
                if (elapsedTime - m_LastFixedUpdateTime >= m_FixedTimeStep)
                {
                    // Advance the timestep and update the system group
                    m_LastFixedUpdateTime += m_FixedTimeStep;
                }
                else if (m_FixedUpdateCount > 0)
                {
                    // m_FixedUpdateCount=0 means this is the very first update, which should occur at t=0.
                    // Otherwise, reaching this point means no further updates are necessary at this time.
                    m_DidPushTime = false;
                    return false;
                }

                m_FixedUpdateCount++;

                group.World.PushTime(new TimeData(
                    elapsedTime: m_LastFixedUpdateTime,
                    deltaTime: m_FixedTimeStep));

                m_DidPushTime = true;
                return true;
            }
        }
    }
}
