using System;
using System.Collections.Generic;
using COLID.Exception.Models;
using COLID.RegistrationService.Services.Implementation.Comparison;
using Xunit;

namespace COLID.RegistrationService.Tests.Unit.Services.Comparison
{
    public class CorrectIdCountGuardTests
    {
        [Theory]
        [InlineData(null , null)]
        [InlineData("ABC", null)]
        [InlineData(null , "ABC")]
        [InlineData(""   , "")]
        [InlineData("ABC", "")]
        [InlineData(""   , "ABC")]
        [InlineData(" "  , " ")]
        [InlineData("ABC", " ")]
        [InlineData(" "  , "ABC")]
        public void CorrectIdCount_Guard_NonNullEmptyOrWhitespace(string firstId, string secondId)
        {
            var idsUnderTest = new List<string>() { firstId, secondId };

            var ex = Assert.Throws<ArgumentException>(() => Guard.CorrectIdCount(idsUnderTest));
            Assert.Equal("id cannot be null, empty, or only whitespace.", ex.Message);
        }

        [Fact]
        public void CorrectIdCount_Guard_LessThanTwo()
        {
            var idsUnderTest = new List<string>() { "ABC" };

            var ex = Assert.Throws<BusinessException>(() => Guard.CorrectIdCount(idsUnderTest));
        }
    }
}
