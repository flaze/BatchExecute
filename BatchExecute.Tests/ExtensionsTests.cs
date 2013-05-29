using FluentAssertions;
using NUnit.Framework;

namespace BatchExecute.Tests
{
    [TestFixture]
    public class ExtensionsTests
    {
        [Test] public void Repeat()
        {
            1.Repeat(2).Should().Be(1);
            2.Repeat(2).Should().Be(2);

            3.Repeat(2).Should().Be(1);
            4.Repeat(2).Should().Be(2);

            15.Repeat(5).Should().Be(5);


            (1.Repeat(2) - 1).Should().Be(0);
            (2.Repeat(2) - 1).Should().Be(1);

            (3.Repeat(2) - 1).Should().Be(0);
        }
    }
}
