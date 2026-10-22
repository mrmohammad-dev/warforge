using NUnit.Framework;
using UnityEngine;
using WarForge.Combat;

namespace WarForge.Tests
{
    public class ArmorMathTests
    {
        [Test]
        public void PerpendicularHitHasNoAngle()
        {
            float a = ArmorMath.ImpactAngleDeg(new Vector3(0, 0, 1), new Vector3(0, 0, -1));
            Assert.That(a, Is.EqualTo(0f).Within(0.01f));
        }

        [Test]
        public void SlopeIncreasesEffectiveThickness()
        {
            float flat = ArmorMath.EffectiveThickness(100f, 0f);
            float sloped = ArmorMath.EffectiveThickness(100f, 60f);
            Assert.That(flat, Is.EqualTo(100f).Within(0.01f));
            Assert.That(sloped, Is.EqualTo(200f).Within(0.5f));
        }

        [Test]
        public void ShellNormalizationReducesSlopeEffect()
        {
            float raw = ArmorMath.EffectiveThickness(100f, 60f, 0f);
            float norm = ArmorMath.EffectiveThickness(100f, 60f, 10f);
            Assert.That(norm, Is.LessThan(raw));
        }

        [Test]
        public void EvaluateRicochetAtExtremeAngle()
        {
            var o = ArmorMath.Evaluate(500f, 50f, 75f, 70f, 0f, out _);
            Assert.That(o, Is.EqualTo(HitOutcome.Ricochet));
        }

        [Test]
        public void EvaluatePenetrationAndNonPenetration()
        {
            Assert.That(ArmorMath.Evaluate(100f, 80f, 0f, 70f, 0f, out _), Is.EqualTo(HitOutcome.Penetration));
            Assert.That(ArmorMath.Evaluate(60f, 80f, 0f, 70f, 0f, out _), Is.EqualTo(HitOutcome.NonPenetration));
        }
    }
}
