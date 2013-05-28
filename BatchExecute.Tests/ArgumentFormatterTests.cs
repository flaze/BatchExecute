﻿using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BatchExecute.Tests
{
    [TestClass]
    public class ArgumentFormatterTests
    {
        private readonly TestFile _testFile;

        public ArgumentFormatterTests()
        {
            _testFile = new TestFile
            {
                DirectoryName = "C:\\SomeDirectory\\Here",
                Name = "Test-File",
                Extension = "ts"
            };
        }

        [TestMethod]
        public void Basic()
        {
            ArgumentFormatter.Format(
                "-i \"{DirectoryName}\\{Name}.{Extension}\" -o \"{DirectoryName}\\{Name}\" -hide -exit", _testFile).Should().Contain(
                "-i \"C:\\SomeDirectory\\Here\\Test-File.ts\" -o \"C:\\SomeDirectory\\Here\\Test-File\" -hide -exit");

            Assert.AreEqual(
                "-i \"C:\\SomeDirectory\\Here\\Test-File.ts\" -o \"C:\\SomeDirectory\\Here\\Test-File.dgi\" -e -h -a",
                ArgumentFormatter.Format(
                    "-i \"{DirectoryName}\\{Name}.{Extension}\" -o \"{DirectoryName}\\{Name}.dgi\" -e -h -a", _testFile).First());
        }

        [TestMethod]
        public void Number()
        {
            FormatRangeShouldBe(ArgumentFormatter.Number(6, 2, 0), 0, 2, 4, 6, 8, 10);

            FormatRangeShouldBe(ArgumentFormatter.Number(6, 2, 1), 1, 3, 5, 7, 9, 11);

            FormatRangeShouldBe(ArgumentFormatter.Number(9, 10, 3, 6, 7), 3, 6, 7, 13, 16, 17, 23, 26, 27);

            FormatRangeShouldBe(ArgumentFormatter.Number(7, 10, 3, 6, 7), 3, 6, 7, 13, 16, 17, 23);

            FormatRangeShouldBe(ArgumentFormatter.Number(5, 9, 0), 0, 9, 18, 27, 36);


            FormatRangeShouldBe(ArgumentFormatter.Format(">{number(4, 2, 2)}<", _testFile), ">2<", ">4<", ">6<", ">8<");

            FormatRangeShouldBe(ArgumentFormatter.Format(">{number(4, 3, 3)}<", _testFile), ">3<", ">6<", ">9<", ">12<");

            FormatRangeShouldBe(ArgumentFormatter.Format(">{number(5, 5, 5)}<", _testFile), ">5<", ">10<", ">15<", ">20<", ">25<");
        }

        [TestMethod]
        public void Range()
        {
            FormatRangeShouldBe(ArgumentFormatter.Format(">{range(3, 280, 14)}<", _testFile), ">0-13<", ">280-293<", ">560-573<");

            FormatRangeShouldBe(ArgumentFormatter.Format(">{range(3, 280, 14, 2)}<", _testFile), ">2-15<", ">282-295<", ">562-575<");

            FormatRangeShouldBe(ArgumentFormatter.Format(">{range(5, 2, 3, 2)}<", _testFile), ">2-4<", ">4-6<", ">6-8<", ">8-10<", ">10-12<");
        }

        private void FormatRangeShouldBe<T>(IEnumerable<T> results, params T[] items)
        {
            var resultsArray = results.ToArray();

            resultsArray.Length.Should().Be(items.Length);

            for (var i = 0; i < items.Length; i++)
            {
                resultsArray[i].Should().Be(items[i]);
            }
        }
    }
}
