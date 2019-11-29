using System.Linq;
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
            // Redirect Log messages in NUnit which get swallowed (from GC invoking destructor in some cases)
            // System.Console.SetOut(NUnit.Framework.TestContext.Out);

            m_PreviousWorld = World.DefaultGameObjectInjectionWorld;
            World = World.DefaultGameObjectInjectionWorld = new World("Test World");

            m_Manager = World.EntityManager;
            m_ManagerDebug = new EntityManager.EntityManagerDebug(m_Manager);
        }

        [TearDown]
        public virtual void TearDown()
        {
            if (m_Manager != null && m_Manager.IsCreated)
            {
                // Clean up systems before calling CheckInternalConsistency because we might have filters etc
                // holding on SharedComponentData making checks fail
                while (World.Systems.ToArray().Length > 0)
                {
                    World.DestroySystem(World.Systems.ToArray()[0]);
                }

                m_ManagerDebug.CheckInternalConsistency();

                World.Dispose();
                World = null;
            }

            // Restore output
            var standardOutput = new System.IO.StreamWriter(System.Console.OpenStandardOutput());
            standardOutput.AutoFlush = true;
            System.Console.SetOut(standardOutput);
        }
    }
}
