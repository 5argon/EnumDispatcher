using NUnit.Framework;
using System;

namespace E7.EnumDispatcher.Tests
{
    public class EnumDispatcherTestBase : MyECSTestsFixture
    {
        protected enum FakePayloadKey
        {
            Attacker,
            Attackee,
            Crit,
            Weakness,
            HitStat,
            Comment,
            Target,
            All,
            ThrownItem,
        }

        protected enum PayloadKey
        {
            Attacker,
            Attackee,
            Crit,
            Weakness,
            HitStat,
            Comment,
            Target,
            All,
            ThrownItem,
        }

        public class MainMenu
        {
            public enum Action
            {
                QuitGame,
                ToModeSelect,
                ToCredits,
                TouchedEmptyArea,
                [F(Navigation)] LeftButton,
                [F(Navigation)] RightButton,
            }

            public enum PayloadKey
            {
                TouchCoordinate,
            }

            protected const string Navigation = nameof(Navigation);
        }

        protected enum Magic
        {
            Fire,
            Ice,
            [F(AOEMagic)] Thunder,
            [F(Ultimate)] Holy,
            [F(Ultimate)] Flare,
            [F(Ultimate, AOEMagic)] Meteo,
            Osmose,
            [F(Sucks)] Poison,
        }

        protected enum Items
        {
            [F(Healing)] Potion,
            [F(Healing)] HiPotion,
            [F(Healing)] XPotion,
            [F(Healing, Ultimate)] Elixir,
            Ether,
            [F(Sucks)] SmokeBomb
        }

        protected enum Act
        {
            Jump
        }

        protected const string Sucks = nameof(Sucks);
        protected const string Ultimate = nameof(Ultimate);
        protected const string Healing = nameof(Healing);
        protected const string AOEMagic = nameof(AOEMagic);

        [SetUp]
        public void PrepareDispatcher()
        {
            Dispatcher.Active.Subscribe(TestHandler);
        }

        [TearDown]
        public void UnregisterDispatcher()
        {
            Dispatcher.Active.Unsubscribe(TestHandler);
        }
        

        DispatchAction dispatchedAction;
        protected void TestHandler(DispatchAction da) => dispatchedAction = da;

        protected DispatchAction Dispatch<T>(T e,
            params (Enum, object)[] payload)
        where T : struct, IConvertible
        {
            Dispatcher.Dispatch<T>(e,payload);
            return dispatchedAction; 
        }
    }
}
