using NUnit.Framework;
using PocketArcade.UI;
using UnityEngine;

namespace PocketArcade.Tests
{
    public sealed class SafeAreaLayoutTests
    {
        [TestCase(1080, 1920)]
        [TestCase(1080, 2340)]
        [TestCase(1080, 2400)]
        public void FullScreenAcrossTargetRatiosUsesEntireCanvas(int width, int height)
        {
            Assert.That(SafeAreaLayout.TryGetAnchors(new Rect(0, 0, width, height),
                new Vector2(width, height), out var min, out var max), Is.True);
            Assert.That(min, Is.EqualTo(Vector2.zero));
            Assert.That(max, Is.EqualTo(Vector2.one));
        }

        [Test]
        public void NotchAndGestureInsetsPreserveUsableArea()
        {
            Assert.That(SafeAreaLayout.TryGetAnchors(new Rect(0, 60, 1080, 2160),
                new Vector2(1080, 2400), out var min, out var max), Is.True);
            Assert.That(min.y, Is.EqualTo(0.025f).Within(0.00001f));
            Assert.That(max.y, Is.EqualTo(0.925f).Within(0.00001f));
            Assert.That(min.x, Is.Zero);
            Assert.That(max.x, Is.EqualTo(1f));
        }

        [Test]
        public void InsetsOnAllEdgesAreRespected()
        {
            Assert.That(SafeAreaLayout.TryGetAnchors(new Rect(50, 100, 900, 1800),
                new Vector2(1000, 2000), out var min, out var max), Is.True);
            Assert.That(min, Is.EqualTo(new Vector2(0.05f, 0.05f)));
            Assert.That(max, Is.EqualTo(new Vector2(0.95f, 0.95f)));
        }

        [Test]
        public void OutOfBoundsAreaIsClampedToScreen()
        {
            Assert.That(SafeAreaLayout.TryGetAnchors(new Rect(-10, -20, 1100, 2450),
                new Vector2(1080, 2400), out var min, out var max), Is.True);
            Assert.That(min, Is.EqualTo(Vector2.zero));
            Assert.That(max, Is.EqualTo(Vector2.one));
        }

        [TestCase(0, 1920)]
        [TestCase(1080, 0)]
        [TestCase(-1, 1920)]
        public void InvalidScreenSizeDoesNotProduceInvalidAnchors(int width, int height)
        {
            Assert.That(SafeAreaLayout.TryGetAnchors(new Rect(0, 0, 1080, 1920),
                new Vector2(width, height), out var min, out var max), Is.False);
            Assert.That(min, Is.EqualTo(Vector2.zero));
            Assert.That(max, Is.EqualTo(Vector2.one));
        }

        [Test]
        public void EmptyOrOutsideSafeAreaIsRejected()
        {
            var screen = new Vector2(1080, 1920);
            Assert.That(SafeAreaLayout.TryGetAnchors(new Rect(0, 0, 0, 0),
                screen, out _, out _), Is.False);
            Assert.That(SafeAreaLayout.TryGetAnchors(new Rect(1100, 0, 10, 10),
                screen, out _, out _), Is.False);
        }

        [Test]
        public void NonFiniteInputsAreRejected()
        {
            Assert.That(SafeAreaLayout.TryGetAnchors(new Rect(float.NaN, 0, 1080, 1920),
                new Vector2(1080, 1920), out _, out _), Is.False);
            Assert.That(SafeAreaLayout.TryGetAnchors(new Rect(0, 0, 1080, 1920),
                new Vector2(float.PositiveInfinity, 1920), out _, out _), Is.False);
        }
    }
}
