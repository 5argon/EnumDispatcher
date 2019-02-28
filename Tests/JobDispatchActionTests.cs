using E7.EnumDispatcher;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Collections;

namespace E7.EnumDispatcher.Tests
{
    internal class JobDispatchActionTests : EnumDispatcherTestBase
    {
        private class TestActionHandlerSystem : ActionHandlerSystem
        {
            ActionCategory<Magic> magicCategory;
            ActionCategory<Items> itemCategory;
            protected override void OnCreateManager()
            {
                base.OnCreateManager();
                itemCategory = GetActionCategory<Items>();
                magicCategory = GetActionCategory<Magic>();
            }

            public ActionFlag Flag(string flag) => GetActionFlag(flag);

            public ActionCategory<ENUM> Cat<ENUM>()
             where ENUM : struct, IConvertible 
             => GetActionCategory<ENUM>();

            public ActionExact Exactly<ENUM>(ENUM action) where ENUM : struct, IConvertible => GetActionExact(action);

            protected override JobHandle OnAction(DispatchAction da, JobHandle jobHandle)
            {
                return jobHandle;
            }

            public void JobSurvivalTest()
            {

            }

        }

        private TestActionHandlerSystem TAHS;

        [SetUp]
        public void AddHandlerSystem()
        {
            TAHS = World.CreateManager<TestActionHandlerSystem>();
        }

        [Test]
        public void MatchInCategory()
        {
            var thunder = Dispatch(Magic.Thunder).CastJob();
            var magicCategory = TAHS.Cat<Magic>();
            var thunderExact = TAHS.Exactly(Magic.Thunder);
            var flareExact = TAHS.Exactly(Magic.Flare);

            Assert.That(thunder.Is(thunderExact));
            Assert.That(thunder.Is(flareExact), Is.Not.True);
            Assert.That(thunder.Category(magicCategory, out _));
        }

        [Test]
        public void SwitchCasing()
        {
            var magicCategory = TAHS.Cat<Magic>();
            var itemsCategory = TAHS.Cat<Items>();
            var meteoExact = TAHS.Exactly(Magic.Meteo);
            var xPotionExact = TAHS.Exactly(Items.XPotion);

            var act = Dispatch(Magic.Thunder).CastJob();
            Assert.That(SwitchTest(act), Is.EqualTo(1));

            act = Dispatch(Magic.Meteo).CastJob();
            Assert.That(SwitchTest(act), Is.EqualTo(2), "It have to hit the default case not the Is case");

            act = Dispatch(Items.XPotion).CastJob();
            Assert.That(SwitchTest(act), Is.EqualTo(4), "It have to hit the Is case before the next switch case");

            act = Dispatch(Items.Elixir).CastJob();
            Assert.That(SwitchTest(act), Is.EqualTo(5));

            act = Dispatch(Items.Potion).CastJob();
            Assert.That(SwitchTest(act), Is.EqualTo(6));

            act = Dispatch(Act.Jump).CastJob();
            Assert.That(SwitchTest(act), Is.EqualTo(7));

            int SwitchTest(JobDispatchAction jda)
            {
                using (NativeArray<int> getResult = new NativeArray<int>(1, Allocator.TempJob))
                {
                    new SwitchCasingJob()
                    {
                        da = jda,
                        magicCategory = magicCategory,
                        itemsCategory = itemsCategory,
                        meteoExact = meteoExact,
                        xPotionExact = xPotionExact,
                        result = getResult,
                    }.Schedule().Complete();
                    Assert.That(getResult[0], Is.Not.EqualTo(default(int)));
                    return getResult[0];
                }
            }
        }

        [BurstCompile]
        private struct SwitchCasingJob : IJob
        {
            public JobDispatchAction da;
            public ActionCategory<Magic> magicCategory;
            public ActionCategory<Items> itemsCategory;
            public ActionExact meteoExact;
            public ActionExact xPotionExact;
            public NativeArray<int> result;

            public void Execute() => result[0] = Yo(da);

            private int Yo(JobDispatchAction da)
            {
                if (da.Category(magicCategory, out Magic m)) switch (m)
                    {
                        case Magic.Thunder: return 1;
                        default: return 2;
                    }
                else if (da.Is(meteoExact)) return 3;
                else if (da.Is(xPotionExact)) return 4;
                else if (da.Category(itemsCategory, out Items i)) switch (i)
                    {
                        case Items.Elixir: return 5;
                        default: return 6;
                    }
                return 7;
            }
        }

        [Test]
        public void PayloadMetaJobifying()
        {
            var fire = Dispatch(Magic.Fire, (PayloadKey.HitStat, (crit: true, weakness: false, sohot: 50807)));
            var fireJob = fire.CastJob();
            var payload = fire.GetPayload<(bool crit, bool weakness, int sohot)>(PayloadKey.HitStat);
            Assert.That(payload.crit);
            Assert.That(payload.weakness, Is.Not.True);
            Assert.That(payload.sohot, Is.EqualTo(50807));

            var survival = new SurvivalJob() { jda = fireJob, payload = payload, ultimate = TAHS.Flag(Ultimate) };
            survival.Schedule().Complete();
            //Survived
        }

        //Burst does not compile JobPayload???
        //[BurstCompile]
        private struct SurvivalJob : IJob
        {
            public JobDispatchAction jda;
            public (bool crit, bool weakness, int sohot) payload;
            public ActionFlag ultimate;
            public void Execute()
            {
                if (jda.Flagged(ultimate))
                {
                    int yay = payload.sohot * (payload.crit ? 5 : 2);
                }
            }
        }

        [Test]
        public void DoesNotMatchAcrossCategories()
        {
            var fire = Dispatch(Magic.Fire).CastJob();
            var potion = Dispatch(Items.Potion).CastJob();

            Assert.That(fire.Is(TAHS.Exactly(Items.Potion)), Is.Not.True);
            Assert.That(fire.Category(TAHS.Cat<Items>(), out _), Is.Not.True);

            Assert.That(potion.Is(TAHS.Exactly(Magic.Fire)), Is.Not.True);
            Assert.That(potion.Category(TAHS.Cat<Magic>(), out _), Is.Not.True);
        }

        [Test]
        public void FlagsWorksInJob()
        {
            var fire = Dispatch(Magic.Fire).CastJob();
            var potion = Dispatch(Items.Potion).CastJob();
            var ult_flare = Dispatch(Magic.Flare).CastJob();
            var ult_elixir = Dispatch(Items.Elixir).CastJob();
            var meteo = Dispatch(Magic.Meteo).CastJob();
            var thunder = Dispatch(Magic.Thunder).CastJob();
            using (NativeArray<int> fail = new NativeArray<int>(1, Allocator.TempJob))
            {
                new FlagTestJob()
                {
                    fire = fire,
                    potion = potion,
                    ult_flare = ult_flare,
                    ult_elixir = ult_elixir,
                    fail = fail,
                    meteo = meteo,
                    thunder = thunder,
                    ultimate = TAHS.Flag(Ultimate),
                    aoeMagic = TAHS.Flag(AOEMagic)
                }.Schedule().Complete();

                Assert.That(fail[0], Is.Zero);
            }
        }

        [BurstCompile]
        private struct FlagTestJob : IJob
        {
            public JobDispatchAction fire;
            public JobDispatchAction potion;
            public JobDispatchAction ult_flare;
            public JobDispatchAction ult_elixir;

            public JobDispatchAction meteo;
            public JobDispatchAction thunder;

            public NativeArray<int> fail;
            public ActionFlag ultimate;
            public ActionFlag aoeMagic;
            public void Execute()
            {
                if (fire.Flagged(ultimate)) Fail();
                if (potion.Flagged(ultimate)) Fail();
                if (!ult_flare.Flagged(ultimate)) Fail();
                if (!ult_elixir.Flagged(ultimate)) Fail();
                if (!meteo.Flagged(ultimate)) Fail();
                if (!meteo.Flagged(aoeMagic)) Fail();
                if (!thunder.Flagged(aoeMagic)) Fail();
                if (thunder.Flagged(ultimate)) Fail();
            }

            private void Fail() => fail[0] = 555;
        }
    }
}
