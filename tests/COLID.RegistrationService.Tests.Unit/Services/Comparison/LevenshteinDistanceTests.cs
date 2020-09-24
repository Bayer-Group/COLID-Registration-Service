using COLID.RegistrationService.Services.Implementation.Comparison;
using Xunit;

namespace COLID.RegistrationService.Tests.Unit.Services.Comparison
{
    public class LevenshteinDistanceTests
    {
        [Theory]
        [InlineData(0,  "Lorem", "Lorem")]
        [InlineData(4,  "Lorem", "ipsum")]
        [InlineData(5,  "Lorem", "sit")]
        [InlineData(8,  "Lorem", "consetetur")]
        [InlineData(8,  "sit", "consetetur")]
        [InlineData(0,  "Lorem ipsum dolor sit amet", "Lorem ipsum dolor sit amet")]
        [InlineData(22, "Lorem ipsum dolor sit amet", "consetetur sadipscing elitr")]
        [InlineData(0,  "1", "1")]
        [InlineData(0,  "111", "111")]
        [InlineData(1,  "111", "121")]
        [InlineData(0,  "A", "A")]
        [InlineData(1,  "A", "B")]
        [InlineData(1,  "A", "AA")]
        [InlineData(1,  "A", "")]
        [InlineData(1,  "A", " ")]
        [InlineData(0,  "<h1>Lorem</h1><p><b>ipsum</b> dolor sit amet, <ul><li>consetetur</li><li>sadipscing</li><li>elitr</li></ul>, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua</p>",
                        "<h1>Lorem</h1><p><b>ipsum</b> dolor sit amet, <ul><li>consetetur</li><li>sadipscing</li><li>elitr</li></ul>, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua</p>")]
        [InlineData(63, "<h1>Lorem</h1><p><b>ipsum</b> dolor sit amet, <ul><li>consetetur</li><li>sadipscing</li><li>elitr</li></ul>, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua</p>",
                        "<h1>Lorem</h1><p><b>ipsum</b> dolor sit amet, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua</p>")]
        [InlineData(0,  " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~",
                        " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~")]
        [InlineData(26, " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~",
                        " !\"#$%&'()*+,-./0123456789:;<=>?@[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~")]
        public void CalculationEqualsExpectedResult(double expectedResult, string firstLiteral, string secondLiteral)
        {
            var result = LevenshteinDistance.Calculate(firstLiteral, secondLiteral);

            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData(1.0, "Lorem", "Lorem")]
        [InlineData(0.2, "Lorem", "ipsum")]
        [InlineData(0.0, "Lorem", "sit")]
        [InlineData(0.2, "Lorem", "consetetur")]
        [InlineData(0.2, "sit", "consetetur")]
        [InlineData(1.0, "Lorem ipsum dolor sit amet", "Lorem ipsum dolor sit amet")]
        [InlineData(0.18518518518518517, "Lorem ipsum dolor sit amet", "consetetur sadipscing elitr")]
        [InlineData(1.0, "1", "1")]
        [InlineData(1.0, "111", "111")]
        [InlineData(0.66666666666666663, "111", "121")]
        [InlineData(1.0, "A", "A")]
        [InlineData(0.0, "A", "B")]
        [InlineData(0.5, "A", "AA")]
        [InlineData(0.0, "A", "")]
        [InlineData(0.0, "A", " ")]
        [InlineData(1.0, "<h1>Lorem</h1><p><b>ipsum</b> dolor sit amet, <ul><li>consetetur</li><li>sadipscing</li><li>elitr</li></ul>, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua</p>",
                        "<h1>Lorem</h1><p><b>ipsum</b> dolor sit amet, <ul><li>consetetur</li><li>sadipscing</li><li>elitr</li></ul>, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua</p>")]
        [InlineData(0.7, "<h1>Lorem</h1><p><b>ipsum</b> dolor sit amet, <ul><li>consetetur</li><li>sadipscing</li><li>elitr</li></ul>, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua</p>",
                        "<h1>Lorem</h1><p><b>ipsum</b> dolor sit amet, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua</p>")]
        [InlineData(1.0,  " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~",
                        " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~")]
        [InlineData(0.72631578947368425, " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~",
                        " !\"#$%&'()*+,-./0123456789:;<=>?@[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~")]
        public void NormalizedCalculationEqualsExpectedResult(double expectedResult, string firstLiteral, string secondLiteral)
        {
            var result = LevenshteinDistance.CalculateNormalized(firstLiteral, secondLiteral);

            Assert.Equal(expectedResult, result);
        }
    }
}
