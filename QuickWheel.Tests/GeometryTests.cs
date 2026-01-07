using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuickWheel.Infrastructure;
using System.Windows;

namespace QuickWheel.Tests
{
    [TestClass]
    public class GeometryTests
    {
        [TestMethod]
        public void TestDonutSlicePoints()
        {
            // Constants
            double radius = 150;
            double innerRadius = 50;
            double center = 150;
            int totalCount = 4;
            int index = 0; // 0 to 90 degrees

            // Expected Angles (in Radians)
            // Start: 0
            // End: 90 deg = PI/2 = 1.5708

            double sliceAngle = 360.0 / totalCount;
            double startAngle = index * sliceAngle;
            double endAngle = (index + 1) * sliceAngle;

            double startRad = startAngle * (Math.PI / 180.0);
            double endRad = endAngle * (Math.PI / 180.0);

            // Calculations manually
            // InnerStart: (150 + 50*cos(0), 150 + 50*sin(0)) = (200, 150)
            // OuterStart: (150 + 150*cos(0), 150 + 150*sin(0)) = (300, 150)
            // OuterEnd:   (150 + 150*cos(90), 150 + 150*sin(90)) = (150, 300)
            // InnerEnd:   (150 + 50*cos(90), 150 + 50*sin(90)) = (150, 200)

            Point innerStart = new Point(center + innerRadius * Math.Cos(startRad), center + innerRadius * Math.Sin(startRad));
            Point outerStart = new Point(center + radius * Math.Cos(startRad), center + radius * Math.Sin(startRad));
            Point outerEnd = new Point(center + radius * Math.Cos(endRad), center + radius * Math.Sin(endRad));
            Point innerEnd = new Point(center + innerRadius * Math.Cos(endRad), center + innerRadius * Math.Sin(endRad));

            Assert.AreEqual(200, innerStart.X, 0.001);
            Assert.AreEqual(150, innerStart.Y, 0.001);

            Assert.AreEqual(300, outerStart.X, 0.001);
            Assert.AreEqual(150, outerStart.Y, 0.001);

            Assert.AreEqual(150, outerEnd.X, 0.001);
            Assert.AreEqual(300, outerEnd.Y, 0.001);

            Assert.AreEqual(150, innerEnd.X, 0.001);
            Assert.AreEqual(200, innerEnd.Y, 0.001);
        }
    }
}
