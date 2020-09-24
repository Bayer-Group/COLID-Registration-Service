using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using COLID.Cache.Extensions;
using Xunit;

namespace COLID.Cache.Tests.Extensions
{
    [ExcludeFromCodeCoverage]
    public class ObjectExtensionTests
    {
        [Fact]
        public void SimplePositive()
        {
            string s1 = "Test";
            string s2 = "Test";
            Assert.Equal(s1.CalculateHash(), s2.CalculateHash());
        }

        [Fact]
        public void SimpleNegative()
        {
            string s1 = "Test";
            string s2 = "Test1";
            Assert.NotEqual(s1.CalculateHash(), s2.CalculateHash());
        }

        [Fact]
        public void ComplexPositive()
        {
            var obj1 = new
            {
                Test = "Property",
                Number = 23,
                Boolean = true
            };

            var obj2 = new
            {
                Test = "Property",
                Number = 23,
                Boolean = true
            };

            Assert.Equal(obj1.CalculateHash(), obj2.CalculateHash());
        }

        [Fact]
        public void ComplexNegative()
        {
            var obj1 = new
            {
                Test = "Property",
                Number = 1,
                Boolean = true
            };

            var obj2 = new
            {
                Test = "Property",
                Number = 2,
                Boolean = true
            };

            Assert.NotEqual(obj1.CalculateHash(), obj2.CalculateHash());
        }

        [Fact]
        public void ComplexPositiveIgnoreIds()
        {
            var obj1 = new
            {
                Id = 111,
                Test = "Property",
                Number = 23,
                Boolean = true
            };

            var obj2 = new
            {
                Id = 222,
                Test = "Property",
                Number = 23,
                Boolean = true
            };

            Assert.Equal(obj1.CalculateHash(), obj2.CalculateHash());

        }

        [Fact]
        public void ComplexPositiveIgnoreForeignKeyIds()
        {
            var obj1 = new
            {
                ActivityId = 1,
                Test = "Property",
                Number = 23,
                Boolean = true
            };

            var obj2 = new
            {
                ActivityId = 2,
                Test = "Property",
                Number = 23,
                Boolean = true
            };

            Assert.Equal(obj1.CalculateHash(), obj2.CalculateHash());

        }

        [Fact]
        public void ComplexPositiveEmptyId()
        {
            var obj1 = new
            {
                Id = 111,
                Test = "Property",
                Number = 23,
                Boolean = true
            };

            var obj2 = new
            {
                Test = "Property",
                Number = 23,
                Boolean = true
            };

            Assert.Equal(obj1.CalculateHash(), obj2.CalculateHash());
        }

        [Fact]
        public void NestedPositive()
        {
            var obj1 = new
            {
                Test = "Property",
                Number = 23,
                Boolean = true,
                Complex = new
                {
                    Name = "Complex"
                }
            };

            var obj2 = new
            {
                Test = "Property",
                Number = 23,
                Boolean = true,
                Complex = new
                {
                    Name = "Complex"
                }
            };

            Assert.Equal(obj1.CalculateHash(), obj2.CalculateHash());
        }

        [Fact]
        public void NestedNegative()
        {
            var obj1 = new
            {
                Test = "Property",
                Number = 23,
                Boolean = true,
                Complex = new
                {
                    Name = "Complex1"
                }
            };

            var obj2 = new
            {
                Test = "Property",
                Number = 23,
                Boolean = true,
                Complex = new
                {
                    Name = "Complex"
                }
            };

            Assert.NotEqual(obj1.CalculateHash(), obj2.CalculateHash());

        }

        [Fact]
        public void NestedNegativeWithEmpty()
        {
            var obj1 = new
            {
                Test = "Property",
                Number = 23,
                Boolean = true,
                Complex = new
                {
                    Name = "Complex"
                }
            };

            var obj2 = new
            {
                Test = "Property",
                Number = 23,
                Boolean = true,
                Complex = new
                {
                    Name = "Complex",
                    Another = "Prop"
                }
            };

            Assert.NotEqual(obj1.CalculateHash(), obj2.CalculateHash());
        }

        [Fact]
        public void DoubleNestedPositive()
        {
            var obj1 = new
            {
                Name = "Name",
                Complex = new
                {
                    Name = "Complex",
                    NestedComplex = new
                    {
                        Name = "NestedComplex"
                    }
                }
            };

            var obj2 = new
            {
                Name = "Name",
                Complex = new
                {
                    Name = "Complex",
                    NestedComplex = new
                    {
                        Name = "NestedComplex"
                    }
                }
            };

            Assert.Equal(obj1.CalculateHash(), obj2.CalculateHash());
        }

        [Fact]
        public void DoubleNestedNegative()
        {
            var obj1 = new
            {
                Name = "Name",
                Complex = new
                {
                    Name = "Complex",
                    NestedComplex = new
                    {
                        Name = "NestedComplex"
                    }
                }
            };

            var obj2 = new
            {
                Name = "Name",
                Complex = new
                {
                    Name = "Complex",
                    NestedComplex = new
                    {
                        Name = "NestedComplex1"
                    }
                }
            };

            Assert.NotEqual(obj1.CalculateHash(), obj2.CalculateHash());
        }

        [Fact]
        public void SimpleCollectionPositive()
        {
            var obj1 = new
            {
                Test = "Property",
                SimpleList = new List<string>
                {
                    "item1",
                    "item2"
                }
            };

            var obj2 = new
            {
                Test = "Property",
                SimpleList = new List<string>
                {
                    "item1",
                    "item2"
                }
            };

            Assert.Equal(obj1.CalculateHash(), obj2.CalculateHash());
        }

        [Fact]
        public void SimpleCollectionReversedItemPositive()
        {
            var obj1 = new
            {
                Test = "Property",
                Number = 23,
                Boolean = true,
                Complex = new
                {
                    Name = "Complex"
                },
                SimpleList = new List<string>
                {
                    "item2",
                    "item1"
                }
            };

            var obj2 = new
            {
                Test = "Property",
                Number = 23,
                Boolean = true,
                Complex = new
                {
                    Name = "Complex"
                },
                SimpleList = new List<string>
                {
                    "item1",
                    "item2"
                }
            };

            Assert.Equal(obj1.CalculateHash(), obj2.CalculateHash());
        }

        [Fact]
        public void SimpleCollectionNegative()
        {
            var obj1 = new
            {
                Test = "Property",
                SimpleList = new List<string>
                {
                    "item1",
                    "item3"
                }
            };

            var obj2 = new
            {
                Test = "Property",
                SimpleList = new List<string>
                {
                    "item1",
                    "item2"
                }
            };

            Assert.NotEqual(obj1.CalculateHash(), obj2.CalculateHash());
        }

        [Fact]
        public void ComplexCollectionPositive()
        {
            var obj1 = new
            {
                Test = "Property",
                Number = 23,
                Boolean = true,
                Complex = new
                {
                    Name = "Complex"
                },
                ComplexList = new List<object>
                {
                    new {Name = "Item1", IsComplex = true },
                    new {Name = "Item2", IsComplex = true },
                }
            };

            var obj2 = new
            {
                Test = "Property",
                Number = 23,
                Boolean = true,
                Complex = new
                {
                    Name = "Complex"
                },
                ComplexList = new List<object>
                {
                    new {Name = "Item1", IsComplex = true },
                    new {Name = "Item2", IsComplex = true },
                }
            };

            Assert.Equal(obj1.CalculateHash(), obj2.CalculateHash());
        }

        [Fact]
        public void ComplexCollectionReversedItemPositive()
        {
            var obj1 = new
            {
                Test = "Property",
                Number = 23,
                Boolean = true,
                Complex = new
                {
                    Name = "Complex"
                },
                ComplexList = new List<object>
                {
                    new {Name = "Item2", IsComplex = true },
                    new {Name = "Item1", IsComplex = true }
                }
            };

            var obj2 = new
            {
                Test = "Property",
                Number = 23,
                Boolean = true,
                Complex = new
                {
                    Name = "Complex"
                },
                ComplexList = new List<object>
                {
                    new {Name = "Item1", IsComplex = true },
                    new {Name = "Item2", IsComplex = true },
                }
            };

            Assert.Equal(obj1.CalculateHash(), obj2.CalculateHash());
        }

        [Fact]
        public void ComplexCollectionNegative()
        {
            var obj1 = new
            {
                Test = "Property",
                Number = 23,
                Boolean = true,
                Complex = new
                {
                    Name = "Complex"
                },
                ComplexList = new List<object>
                {
                    new {Name = "Item1", IsComplex = true },
                    new {Name = "Item2", IsComplex = true }
                }
            };

            var obj2 = new
            {
                Test = "Property",
                Number = 23,
                Boolean = true,
                Complex = new
                {
                    Name = "Complex"
                },
                ComplexList = new List<object>
                {
                    new {Name = "Item1", IsComplex = true },
                    new {Name = "Item3", IsComplex = true },
                }
            };

            Assert.NotEqual(obj1.CalculateHash(), obj2.CalculateHash());
        }
    }
}
