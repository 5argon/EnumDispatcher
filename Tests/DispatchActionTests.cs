using E7.EnumDispatcher;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

namespace E7.EnumDispatcher.Tests
{
    internal class DispatchActionTests  : EnumDispatcherTestBase
    {
        [Test]
        public void MatchInCategory()
        {
            var thunder = Dispatch(Magic.Thunder);

            Assert.That(thunder.Is(Magic.Thunder));
            Assert.That(thunder.Is(Magic.Flare), Is.Not.True);
            Assert.That(thunder.Category<Magic>(out _));
        }

        [Test]
        public void SwitchCasing()
        {
            var act = Dispatch(Magic.Thunder);
            Assert.That(SwitchTest(act), Is.EqualTo(1));

            act = Dispatch(Magic.Meteo);
            Assert.That(SwitchTest(act), Is.EqualTo(2), "It have to hit the default case not the Is case");

            act = Dispatch(Items.XPotion);
            Assert.That(SwitchTest(act), Is.EqualTo(4), "It have to hit the Is case before the next switch case");

            act = Dispatch(Items.Elixir);
            Assert.That(SwitchTest(act), Is.EqualTo(5));

            act = Dispatch(Items.Potion);
            Assert.That(SwitchTest(act), Is.EqualTo(6));

            act = Dispatch(Act.Jump);
            Assert.That(SwitchTest(act), Is.EqualTo(7));

            int SwitchTest(DispatchAction da)
            {
                if (da.Category(out Magic m)) switch (m)
                    {
                        case Magic.Thunder: return 1;
                        default: return 2;
                    }
                else if(da.Is(Magic.Meteo)) return 3;
                else if(da.Is(Items.XPotion)) return 4;
                else if (da.Category(out Items i)) switch (i)
                    {
                        case Items.Elixir: return 5;
                        default: return 6;
                    }
                return 7;
            }
        }

        [Test]
        public void AsWorks()
        {
            var fire = Dispatch(Magic.Fire);
            Assert.That(fire.As<Magic>(), Is.EqualTo(Magic.Fire));
            Assert.That(fire.As<Items>(), Is.EqualTo(Items.Potion), "This is allowed, because As can assume any underlying int as any category.");
        }

        [Test]
        public void PayloadUnboxing()
        {
            var fire = Dispatch(Magic.Fire, (PayloadKey.HitStat, (crit: true, weakness: false)), (PayloadKey.Comment, "So hot"));

            if (fire.HasPayload(PayloadKey.HitStat, out bool criticalHit))
            {
                Assert.Fail("Type is wrong, the out overload should not work.");
            }
            if (fire.HasPayload(PayloadKey.HitStat, out (bool crit, bool weakness) hitStat))
            {
                //Payload can hold tuples too
                Assert.That(hitStat.crit);
                Assert.That(!hitStat.weakness);
            }
            else
            {
                Assert.Fail("When out type is castable it should go in the if");
            }

            if(fire.HasPayload(PayloadKey.Comment, out int ohno))
            {
                Assert.Fail("Type is wrong, the out overload should not work.");
            }
            if (fire.HasPayload(PayloadKey.HitStat, out string hot))
            {
                Assert.Fail("Type is right but the payload key is wrong, it should not work.");
            }
            if(fire.HasPayload(PayloadKey.Comment, out string ok))
            {
                Assert.That(ok, Is.EqualTo("So hot"));
            }

            //Test the GetPayload

            (bool crit, bool weakness) = fire.GetPayload<(bool, bool)>(PayloadKey.HitStat);

            Assert.That(crit);
            Assert.That(!weakness);

            Assert.Throws<System.InvalidCastException>(() => fire.GetPayload<bool>(PayloadKey.HitStat));

            var firePlain = Dispatch(Magic.Fire);

            Assert.Throws<System.InvalidCastException>(() => firePlain.GetPayload<bool>(PayloadKey.HitStat));

            fire = Dispatch(Magic.Fire, (PayloadKey.Attacker, 1), (FakePayloadKey.Attacker, 555));
            Assert.That(fire.GetPayload<int>(PayloadKey.Attacker), Is.EqualTo(1), "Check that payload key from 2 different enum with same value is discernable by the dict");
            Assert.That(fire.GetPayload<int>(FakePayloadKey.Attacker), Is.EqualTo(555), "Check that payload key from 2 different enum with same value is discernable by the dict");
            
        }

        [Test]
        public void DoesNotMatchAcrossCategories()
        {
            var fire = Dispatch(Magic.Fire);
            var potion = Dispatch(Items.Potion);

            Assert.That(fire.Is(Items.Potion), Is.Not.True);
            Assert.That(fire.Category<Items>(out _), Is.Not.True);

            Assert.That(potion.Is(Magic.Fire), Is.Not.True);
            Assert.That(potion.Category<Magic>(out _), Is.Not.True);
        }

        [Test]
        public void FlagsWorks()
        {
            var fire = Dispatch(Magic.Fire);
            var potion = Dispatch(Items.Potion);
            var ult_flare = Dispatch(Magic.Flare);
            var ult_elixir = Dispatch(Items.Elixir);
            var meteo = Dispatch(Magic.Meteo);
            var thunder = Dispatch(Magic.Thunder);

            Assert.That(fire.Flagged(Ultimate), Is.Not.True);
            Assert.That(potion.Flagged(Ultimate), Is.Not.True);
            Assert.That(ult_flare.Flagged(Ultimate));
            Assert.That(ult_elixir.Flagged(Ultimate));

            Assert.That(meteo.Flagged(AOEMagic),"Some action can has multiple flags");
            Assert.That(meteo.Flagged(Ultimate),"Some action can has multiple flags");

            Assert.That(thunder.Flagged(AOEMagic), "Some action can share a flag with other action in the same category, unlike enum values.");
            Assert.That(thunder.Flagged(Ultimate), Is.Not.True, "Flags that was shared with other action has no relationship with other flags.");
        }
        
        [Test]
        public void OptionalPayload()
        {
            var fire = Dispatch(Magic.Fire, (PayloadKey.Crit, true));
            var thunder = Dispatch(Magic.Thunder, (PayloadKey.Crit, true), (PayloadKey.All, true));

            Assert.Throws<System.InvalidCastException>(() => fire.GetPayload<bool, bool>(PayloadKey.Crit, PayloadKey.All), "No optional set, cannot find the 2nd key.");
            Assert.That(fire.GetPayload<bool, bool>(PayloadKey.Crit, PayloadKey.All, optionals: (false, true)).Item1, Is.True);
            Assert.That(fire.GetPayload<bool, bool>(PayloadKey.Crit, PayloadKey.All, optionals: (false, true)).Item2, Is.False, "Because optional the non existence key will be a default.");

            Assert.That(thunder.GetPayload<bool, bool>(PayloadKey.Crit, PayloadKey.All, optionals: (true, true)).Item1, 
            Is.True, 
            "Optionals do not modify a payload that do exist.");
            Assert.That(thunder.GetPayload<bool, bool>(PayloadKey.Crit, PayloadKey.All, optionals: (true, true)).Item2, 
            Is.True, 
            "Optionals do not modify a payload that do exist.");
        }
    }
}
