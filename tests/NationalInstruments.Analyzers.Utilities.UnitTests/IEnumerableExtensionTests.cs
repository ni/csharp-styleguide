using System.Collections.Generic;
using System.Linq;
using NationalInstruments.Analyzers.Utilities.Extensions;
using Xunit;

namespace NationalInstruments.Analyzers.Utilities.UnitTests
{
    public sealed class IEnumerableExtensionTests : UtilitiesTestBase
    {
        [Fact]
        public void ToSafeEnumerable_EmptyEnumerable_IsNonNullAndEqual()
        {
            VerifyToSafeEnumerable_IsNonNullAndEqual(Enumerable.Empty<int>());
        }

        [Fact]
        public void ToSafeEnumerable_OneElementEnumerable_IsNonNullAndEqual()
        {
            VerifyToSafeEnumerable_IsNonNullAndEqual(new int[] { 1 });
        }

        [Fact]
        public void ToSafeEnumerable_TwoElementEnumerable_IsNonNullAndEqual()
        {
            VerifyToSafeEnumerable_IsNonNullAndEqual(new int[] { 1, 2 });
        }

        [Fact]
        public void ToSafeEnumerable_NullEnumerable_IsNonNullAndEmpty()
        {
            IEnumerable<int>? nullEnumerable = null;
            Assert.NotNull(nullEnumerable.ToSafeEnumerable());
            Assert.Empty(nullEnumerable.ToSafeEnumerable());
        }

        private void VerifyToSafeEnumerable_IsNonNullAndEqual<T>(IEnumerable<T> enumerable)
        {
            Assert.NotNull(enumerable.ToSafeEnumerable());
            Assert.True(enumerable.SequenceEqual(enumerable.ToSafeEnumerable()));
        }
    }
}
