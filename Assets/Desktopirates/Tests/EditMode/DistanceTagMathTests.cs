using NUnit.Framework;
using UnityEngine;

namespace Desktopirates.Tests
{
    public sealed class DistanceTagMathTests
    {
        [Test]
        public void TagGrowsAsTargetGetsCloser()
        {
            float near = DistanceTagMath.ScaleForDistance(9f);
            float far = DistanceTagMath.ScaleForDistance(60f);
            Assert.That(near, Is.GreaterThan(far));
            Assert.That(DistanceTagMath.PixelSizeForDistance(9f), Is.GreaterThan(80f));
            Assert.That(DistanceTagMath.PixelSizeForDistance(60f), Is.EqualTo(62f).Within(0.001f));
        }

        [Test]
        public void CameraRightQuarterTurnMovesWorldNorthToScreenLeft()
        {
            Vector2 rotated = DistanceTagMath.WorldToScreenDirection(Vector2.up, 90f);
            Assert.That(rotated.x, Is.EqualTo(-1f).Within(0.001f));
            Assert.That(rotated.y, Is.EqualTo(0f).Within(0.001f));
        }

        [TestCase(0f, 0f, 1f)]
        [TestCase(90f, -1f, 0f)]
        [TestCase(180f, 0f, -1f)]
        [TestCase(270f, 1f, 0f)]
        public void WorldNorthTracksAllCameraQuadrants(float yaw, float expectedX, float expectedY)
        {
            Vector2 screen = DistanceTagMath.WorldToScreenDirection(Vector2.up, yaw);
            Assert.That(screen.x, Is.EqualTo(expectedX).Within(0.001f));
            Assert.That(screen.y, Is.EqualTo(expectedY).Within(0.001f));
        }

        [Test]
        public void WorldEastRemainsScreenRightAtDefaultCamera()
        {
            Vector2 screen = DistanceTagMath.WorldToScreenDirection(Vector2.right, 0f);
            Assert.That(screen.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(screen.y, Is.EqualTo(0f).Within(0.001f));
        }

        [TestCase(1f, 0f)]
        [TestCase(0f, 1f)]
        [TestCase(-1f, 0f)]
        [TestCase(0f, -1f)]
        [TestCase(0.7071068f, 0.7071068f)]
        public void LandmarkPositionAlwaysLiesOnTheSeaEllipse(float x, float y)
        {
            Vector2 center = new Vector2(0f, -63f);
            Vector2 radii = new Vector2(312f, 218f);
            Vector2 position = DistanceTagMath.PositionOnEllipse(new Vector2(x, y), center, radii);
            Vector2 normalized = new Vector2(
                (position.x - center.x) / radii.x,
                (position.y - center.y) / radii.y);
            Assert.That(normalized.sqrMagnitude, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void LandmarkMarkerBeginsOutsideTheProjectedSeaRim()
        {
            Vector2 center = new Vector2(0f, -63f);
            Vector2 rim = new Vector2(260f, -63f);
            Vector2 marker = DistanceTagMath.PositionOutsideRim(center, rim, 94f, TagRingController.RimPadding);
            Assert.That(marker.x - rim.x, Is.EqualTo(47f + TagRingController.RimPadding).Within(0.001f));
        }

        [Test]
        public void MarkerIsClampedWithItsWholeArtworkInsideTheWindow()
        {
            Rect canvas = new Rect(-360f, -380f, 720f, 760f);
            Vector2 marker = DistanceTagMath.ClampMarkerInside(new Vector2(420f, 440f), canvas, 94f, TagRingController.WindowPadding);
            Rect artwork = DistanceTagMath.MarkerRect(marker, 94f);
            Assert.That(artwork.xMax, Is.LessThanOrEqualTo(canvas.xMax - TagRingController.WindowPadding + 0.001f));
            Assert.That(artwork.yMax, Is.LessThanOrEqualTo(canvas.yMax - TagRingController.WindowPadding + 0.001f));
        }

        [Test]
        public void MarkerCollisionIncludesRequestedVisualSeparation()
        {
            Rect first = DistanceTagMath.MarkerRect(Vector2.zero, 40f, TagRingController.MarkerSeparation);
            Rect touching = DistanceTagMath.MarkerRect(new Vector2(44f, 0f), 40f, TagRingController.MarkerSeparation);
            Assert.That(first.Overlaps(touching), Is.True);
            Assert.That(DistanceTagMath.IsClear(touching, new[] { first }), Is.False);
        }

        [TestCase(0f, 280f)]
        [TestCase(0f, -280f)]
        [TestCase(240f, 240f)]
        public void WindowEdgeAdjustmentPreservesExactLandmarkBearing(float x, float y)
        {
            Vector2 center = new Vector2(0f, -63f);
            Vector2 candidate = new Vector2(x, y);
            Rect canvas = new Rect(-360f, -380f, 720f, 760f);
            Vector2 adjusted = DistanceTagMath.PullAlongRayInside(center, candidate, canvas, 94f, TagRingController.WindowPadding);
            float cross = (candidate.x - center.x) * (adjusted.y - center.y)
                - (candidate.y - center.y) * (adjusted.x - center.x);
            Assert.That(cross, Is.EqualTo(0f).Within(0.01f), "A HUD/window adjustment must not rotate the marker around the sea.");
            Assert.That(Vector2.Dot(candidate - center, adjusted - center), Is.GreaterThan(0f));
        }

        [TestCase(0f, 0f)]
        [TestCase(1f, -90f)]
        [TestCase(-1f, 90f)]
        public void BossCompassNeedleTracksScreenDirection(float worldX, float expectedAngle)
        {
            Vector2 direction = worldX == 0f ? Vector2.up : new Vector2(worldX, 0f);
            Assert.That(BossCompassModel.GetNeedleAngle(direction, 0f), Is.EqualTo(expectedAngle).Within(0.001f));
        }

        [Test]
        public void BossCompassNeedleUsesExactDialCenterAsRotationPivot()
        {
            var objectUnderTest = new GameObject("Needle", typeof(RectTransform));
            try
            {
                RectTransform needle = objectUnderTest.GetComponent<RectTransform>();
                needle.anchorMin = Vector2.zero;
                needle.anchorMax = Vector2.one;
                needle.pivot = Vector2.zero;
                needle.anchoredPosition = new Vector2(27f, -13f);

                BossCompassModel.CenterNeedleOnDial(needle);

                Assert.That(needle.anchorMin, Is.EqualTo(BossCompassModel.CenterAnchor));
                Assert.That(needle.anchorMax, Is.EqualTo(BossCompassModel.CenterAnchor));
                Assert.That(needle.pivot, Is.EqualTo(BossCompassModel.CenterAnchor));
                Assert.That(needle.anchoredPosition, Is.EqualTo(Vector2.zero));
                Assert.That(needle.localScale, Is.EqualTo(Vector3.one));
            }
            finally
            {
                Object.DestroyImmediate(objectUnderTest);
            }
        }

        [Test]
        public void BossCompassArtworkCompensatesForItsRightHeavyDrawing()
        {
            Assert.That(BossCompassModel.NeedleArtworkOffsetX, Is.LessThan(0f));
            Assert.That(Mathf.Abs(BossCompassModel.NeedleArtworkOffsetX), Is.LessThanOrEqualTo(3f));
        }
    }
}
