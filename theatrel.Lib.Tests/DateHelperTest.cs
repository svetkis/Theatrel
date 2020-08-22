using System;
using System.Linq;
using Xunit;

namespace theatrel.Lib.Tests
{
    public class DateHelperTest
    {
        [Theory]
        [InlineData(2020, 9, 17, 2020, 10, 5, 9, 10)]
        [InlineData(2020, 9, 8, 2020, 9, 1)]
        [InlineData(2020, 9, 1, 2020, 9, 1, 9)]
        [InlineData(2020, 9, 1, 2020, 9, 9, 9)]
        [InlineData(2020, 11, 1, 2020, 10, 18)]
        [InlineData(2020, 11, 30, 2021, 2, 4, 11, 12, 1, 2)]
        public void Test(int yearFrom, int monthFrom, int dayFrom, int yearTo, int monthTo, int dayTo, params int[] expected)
        {
            var results = new DateTime(yearFrom, monthFrom, dayFrom).GetMonthsBetween(new DateTime(yearTo, monthTo, dayTo));
            Assert.Equal(expected, results.Select(dt => dt.Month));
        }
    }
}
