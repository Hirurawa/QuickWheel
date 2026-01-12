using System;
using QuickWheel.Core;
using NUnit.Framework;

namespace QuickWheel.Tests
{
    [TestFixture]
    public class SettingsValidatorTests
    {
        [Test]
        public void ValidateRange_ValidInput_ReturnsTrueAndResult()
        {
            // Arrange
            string input = "100";
            double min = 50;
            double max = 2000;

            // Act
            bool isValid = SettingsValidator.ValidateRange(input, min, max, out int result);

            // Assert
            Assert.IsTrue(isValid);
            Assert.AreEqual(100, result);
        }

        [Test]
        public void ValidateRange_BelowMin_ReturnsFalse()
        {
            // Arrange
            string input = "10";
            double min = 50;
            double max = 2000;

            // Act
            bool isValid = SettingsValidator.ValidateRange(input, min, max, out int result);

            // Assert
            Assert.IsFalse(isValid);
        }

        [Test]
        public void ValidateRange_AboveMax_ReturnsFalse()
        {
            // Arrange
            string input = "3000";
            double min = 50;
            double max = 2000;

            // Act
            bool isValid = SettingsValidator.ValidateRange(input, min, max, out int result);

            // Assert
            Assert.IsFalse(isValid);
        }

        [Test]
        public void ValidateRange_NonNumeric_ReturnsFalse()
        {
            // Arrange
            string input = "abc";
            double min = 50;
            double max = 2000;

            // Act
            bool isValid = SettingsValidator.ValidateRange(input, min, max, out int result);

            // Assert
            Assert.IsFalse(isValid);
        }

        [Test]
        public void ValidateRange_BoundaryValues_ReturnsTrue()
        {
            // Arrange
            double min = 50;
            double max = 2000;

            // Act & Assert
            Assert.IsTrue(SettingsValidator.ValidateRange("50", min, max, out int resultMin));
            Assert.AreEqual(50, resultMin);

            Assert.IsTrue(SettingsValidator.ValidateRange("2000", min, max, out int resultMax));
            Assert.AreEqual(2000, resultMax);
        }
    }
}
