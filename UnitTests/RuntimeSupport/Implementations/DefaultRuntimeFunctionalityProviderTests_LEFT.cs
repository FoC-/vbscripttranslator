﻿using VBScriptTranslator.RuntimeSupport;
using VBScriptTranslator.RuntimeSupport.Exceptions;
using System;
using Xunit;

namespace VBScriptTranslator.UnitTests.RuntimeSupport.Implementations
{
    public static partial class DefaultRuntimeFunctionalityProviderTests
    {
        public class LEFT
        {
            /// <summary>
            /// Passing in VBScript Empty as the string will return in a blank string being returned (so long as the length argument can be interpreted as a non-negative number)
            /// </summary>
            [Fact]
            public void EmptyLengthOneReturnsBlankString()
            {
                Assert.Equal("", DefaultRuntimeSupportClassFactory.Get().LEFT(null, 1));
            }

            /// <summary>
            /// Passing in VBScript Null as the string will return in VBScript Null being returned (so long as the length argument can be interpreted as a non-negative number)
            /// </summary>
            [Fact]
            public void NullLengthOneReturnsNull()
            {
                Assert.Equal(DBNull.Value, DefaultRuntimeSupportClassFactory.Get().LEFT(DBNull.Value, 1));
            }

            [Fact]
            public void ZeroLengthIsAcceptable()
            {
                Assert.Equal("", DefaultRuntimeSupportClassFactory.Get().LEFT("", 0));
            }

            [Fact]
            public void NegativeLengthIsNotAcceptable()
            {
                Assert.Throws<InvalidProcedureCallOrArgumentException>(() =>
                {
                    DefaultRuntimeSupportClassFactory.Get().LEFT("", -1);
                });
            }

            [Fact]
            public void EmptyLengthIsTreatedAsZeroLength()
            {
                Assert.Equal("", DefaultRuntimeSupportClassFactory.Get().LEFT("abc", null));
            }

            [Fact]
            public void NullLengthIsNotAcceptable()
            {
                Assert.Throws<InvalidUseOfNullException>(() =>
                {
                    DefaultRuntimeSupportClassFactory.Get().LEFT("", DBNull.Value);
                });
            }

            [Fact]
            public void MaxLengthLongerThanInputStringLengthIsTreatedAsEqualingInputStringLength()
            {
                Assert.Equal("abc", DefaultRuntimeSupportClassFactory.Get().LEFT("abc", 10));
            }

            [Fact]
            public void EnormousLengthResultsInOverflow()
            {
                Assert.Throws<VBScriptOverflowException>(() =>
                {
                    DefaultRuntimeSupportClassFactory.Get().LEFT("", 1000000000000000);
                });
            }

            // These tests all illustrate that VBScript's standard "banker's rounding" is applied to fractional lengths
            [Fact]
            public void LengthZeroPointFiveTreatedAsLengthZero()
            {
                Assert.Equal("", DefaultRuntimeSupportClassFactory.Get().LEFT("abcd", 0.5));
            }
            [Fact]
            public void LengthZeroPointNineTreatedAsLengthOne()
            {
                Assert.Equal("a", DefaultRuntimeSupportClassFactory.Get().LEFT("abcd", 0.9));
            }
            [Fact]
            public void LengthOnePointFiveTreatedAsLengthTwo()
            {
                Assert.Equal("ab", DefaultRuntimeSupportClassFactory.Get().LEFT("abcd", 1.5));
            }
            [Fact]
            public void LengthOnePointNineTreatedAsLengthTwo()
            {
                Assert.Equal("ab", DefaultRuntimeSupportClassFactory.Get().LEFT("abcd", 1.9));
            }
            [Fact]
            public void LengthTwoPointFiveTreatedAsLengthTwo()
            {
                Assert.Equal("ab", DefaultRuntimeSupportClassFactory.Get().LEFT("abcd", 2.5));
            }
            [Fact]
            public void LengthTwoPointNineTreatedAsLengthThree()
            {
                Assert.Equal("abc", DefaultRuntimeSupportClassFactory.Get().LEFT("abcd", 2.9));
            }
            [Fact]
            public void LengthThreePointFiveTreatedAsLengthFour()
            {
                Assert.Equal("abcd", DefaultRuntimeSupportClassFactory.Get().LEFT("abcd", 3.5));
            }
            [Fact]
            public void LengthThreePointNineTreatedAsLengthFour()
            {
                Assert.Equal("abcd", DefaultRuntimeSupportClassFactory.Get().LEFT("abcd", 3.9));
            }
        }
    }
}
