using NUnit.Framework;
using Unity.Entities;

namespace E7.EnumDispatcher.Tests
{
    /// <summary>
    /// Copied from ECSTestsFixture because test asm linking is kinda wonky right now
    /// </summary>
    public class MyECSTestsFixture
    {
        protected World m_PreviousWorld;
        protected World World;
        protected EntityManager m_Manager;
        protected EntityManager.EntityManagerDebug m_ManagerDebug;

        protected int StressTestEntityCount = 1000;

        [SetUp]
        public virtual void Setup()
        {
            m_PreviousWorld = World.Active;
            World = World.Active = new World("Test World");

            m_Manager = World.GetOrCreateManager<EntityManager>();
            m_ManagerDebug = new EntityManager.EntityManagerDebug(m_Manager);

#if !UNITY_2019_2_OR_NEWER 
            // Not raising exceptions can easily bring unity down with massive logging when tests fail.
            // From Unity 2019.2 on this field is always implicitly true and therefore removed.

            UnityEngine.Assertions.Assert.raiseExceptions = true;
#endif
        }

        [TearDown]
        public virtual void TearDown()
        {
            if (m_Manager != null)
            {
                // Clean up systems before calling CheckInternalConsistency because we might have filters etc
                // holding on SharedComponentData making checks fail
                var system = World.GetExistingManager<ComponentSystemBase>();
                while (system != null)
                {
                    World.DestroyManager(system);
                    system = World.GetExistingManager<ComponentSystemBase>();
                }

                World.Dispose();
                World = null;

                World.Active = m_PreviousWorld;
                m_PreviousWorld = null;
                m_Manager = null;
            }
        }
    }
}
