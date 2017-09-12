﻿using FluentAssertions;
using NWheels.Kernel.Api.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Xunit;

namespace NWheels.Kernel.UnitTests.Api.Extensions
{
    public class PathUtilityTests
    {
        [Fact]
        public void ExpandPathFromBinary_RelativePath_Expanded()
        {
            //-- arrange

            var input = "abc/def";

            //-- act

            var actualOutput = PathUtility.ExpandPathFromBinary(input);

            //-- assert

            var binaryFolder = Path.GetDirectoryName(this.GetType().GetTypeInfo().Assembly.Location);
            var expectedOutput = Path.Combine(binaryFolder, "abc", "def");

            actualOutput.Should().Be(expectedOutput);
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public void ExpandPathFromBinary_RootedPath_NotExpanded()
        {
            //-- arrange

            var input = Path.Combine(Path.GetTempPath(), "abc", "def");

            //-- act

            var actualOutput = PathUtility.ExpandPathFromBinary(input);

            //-- assert

            actualOutput.Should().Be(input);
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public void ExpandPathFromBinary_CornerCases()
        {
            //-- arrange

            string inputEmpty = string.Empty;
            string inputNull = null;
            string[] inputNullArray = null;

            //-- act

            var outputForEmpty = PathUtility.ExpandPathFromBinary(inputEmpty);
            var outputForNull = PathUtility.ExpandPathFromBinary(inputNull);
            var outputForNullArray = PathUtility.ExpandPathFromBinary(inputNullArray);

            //-- assert

            var binaryFolder = Path.GetDirectoryName(this.GetType().GetTypeInfo().Assembly.Location);

            outputForEmpty.Should().Be(binaryFolder);
            outputForNull.Should().Be(binaryFolder);
            outputForNullArray.Should().Be(binaryFolder);
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public void ExpandPathFromBinaryParams_RelativePath_Expanded()
        {
            //-- arrange

            var input = new string[] { "ab", "cd/ef" };

            //-- act

            var actualOutput = PathUtility.ExpandPathFromBinary(input);

            //-- assert

            var binaryFolder = Path.GetDirectoryName(this.GetType().GetTypeInfo().Assembly.Location);
            var expectedOutput = Path.Combine(binaryFolder, "ab", "cd", "ef");

            actualOutput.Should().Be(expectedOutput);
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public void ExpandPathFromBinaryParams_CornerCases()
        {
            //-- arrange

            var inputEmptyArray = new string[0];
            var inputEmptyString = new[] { string.Empty };

            //-- act

            var outputForEmptyArray = PathUtility.ExpandPathFromBinary(inputEmptyArray);
            var outputForEmptyString = PathUtility.ExpandPathFromBinary(inputEmptyString);

            //-- assert

            var binaryFolder = Path.GetDirectoryName(this.GetType().GetTypeInfo().Assembly.Location);

            outputForEmptyArray.Should().Be(binaryFolder);
            outputForEmptyString.Should().Be(binaryFolder);
        }
    }
}
