using NUnit.Framework;
using UnityEngine;
using WarForge.Combat;

namespace WarForge.Tests
{
    public class BallisticsMathTests
    {
        [Test]
        public void SolvedAngleHitsTargetInVacuumLikeCase()
        {
            const float v = 800f, g = 9.81f, x = 600f, y = 2f;
            Assert.That(BallisticsMath.SolveLowAngle(v, g, x, y, out float ang), Is.True);
            float t = BallisticsMath.FlightTime(v, ang, x);
            float heightAtX = v * Mathf.Sin(ang) * t - 0.5f * g * t * t;
            Assert.That(heightAtX, Is.EqualTo(y).Within(0.05f));
        }

        [Test]
        public void OutOfRangeReturnsFalse()
        {
            Assert.That(BallisticsMath.SolveLowAngle(100f, 9.81f, 5000f, 0f, out _), Is.False);
        }

        [Test]
        public void LeadPointMovesWithTargetVelocity()
        {
            var muzzle = Vector3.zero;
            var still = BallisticsMath.ComputeAimPoint(muzzle, new Vector3(0, 0, 400), Vector3.zero, 800f, 9.81f);
            var moving = BallisticsMath.ComputeAimPoint(muzzle, new Vector3(0, 0, 400), new Vector3(10, 0, 0), 800f, 9.81f);
            Assert.That(moving.x, Is.GreaterThan(still.x + 1f));
        }

        [Test]
        public void ZeroDistanceAimIsTarget()
        {
            var p = BallisticsMath.ComputeAimPoint(Vector3.zero, new Vector3(0, 5, 0), Vector3.zero, 800f, 9.81f);
            Assert.That(p.y, Is.EqualTo(5f).Within(0.01f));
        }
    }
}
