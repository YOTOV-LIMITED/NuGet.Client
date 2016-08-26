﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using NuGet.Common;
using NuGet.Test.Utility;
using Xunit;

namespace NuGet.XPlat.FuncTest
{
    public class XPlatLocalsTests
    {
        [Theory]
        [InlineData("locals all --list")]
        [InlineData("locals all -l")]
        [InlineData("locals --list all")]
        [InlineData("locals -l all")]
        [InlineData("locals http-cache --list")]
        [InlineData("locals http-cache -l")]
        [InlineData("locals --list http-cache")]
        [InlineData("locals -l http-cache")]
        [InlineData("locals temp --list")]
        [InlineData("locals temp -l")]
        [InlineData("locals --list temp")]
        [InlineData("locals -l temp")]
        [InlineData("locals global-packages --list")]
        [InlineData("locals global-packages -l")]
        [InlineData("locals --list global-packages")]
        [InlineData("locals -l global-packages")]
        public static void Locals_List_Succeeds(string args)
        {
            using (var mockBaseDirectory = TestFileSystemUtility.CreateRandomTestFolder())
            {
                // Arrange
                var mockGlobalPackagesDirectory = Directory.CreateDirectory(Path.Combine(mockBaseDirectory.Path, @"global-packages"));
                var mockHttpCacheDirectory = Directory.CreateDirectory(Path.Combine(mockBaseDirectory.Path, @"http-cache"));
                var mockTmpCacheDirectory = Directory.CreateDirectory(Path.Combine(mockBaseDirectory.Path, @"temp"));

                DotnetCliUtil.CreateTestFiles(mockGlobalPackagesDirectory.FullName);
                DotnetCliUtil.CreateTestFiles(mockHttpCacheDirectory.FullName);
                DotnetCliUtil.CreateTestFiles(mockTmpCacheDirectory.FullName);

                // Act
                var result = CommandRunner.Run(
                      DotnetCliUtil.GetDotnetCli(),
                      Directory.GetCurrentDirectory(),
                      DotnetCliUtil.GetXplatDll() + " " + args,
                      waitForExit: true,
                    environmentVariables: new Dictionary<string, string>
                    {
                        { "NUGET_PACKAGES", mockGlobalPackagesDirectory.FullName },
                        { "NUGET_HTTP_CACHE_PATH", mockHttpCacheDirectory.FullName },
                        { RuntimeEnvironmentHelper.IsWindows ? "TMP" : "TMPDIR", mockTmpCacheDirectory.FullName }
                    });
                // Unix uses TMPDIR as environment variable as opposed to TMP on windows

                // Assert
                DotnetCliUtil.VerifyResultSuccess(result, string.Empty);
            }
        }

        [InlineData("locals --clear all")]
        [InlineData("locals -c all")]
        [InlineData("locals http-cache --clear")]
        [InlineData("locals http-cache -c")]
        [InlineData("locals --clear http-cache")]
        [InlineData("locals -c http-cache")]
        [InlineData("locals temp --clear")]
        [InlineData("locals temp -c")]
        [InlineData("locals --clear temp")]
        [InlineData("locals -c temp")]
        [InlineData("locals global-packages --clear")]
        [InlineData("locals global-packages -c")]
        [InlineData("locals --clear global-packages")]
        [InlineData("locals -c global-packages")]
        public static void Locals_Clear_Succeeds(string args)
        {
            using (var mockBaseDirectory = TestFileSystemUtility.CreateRandomTestFolder())
            {
                // Arrange
                var mockGlobalPackagesDirectory = Directory.CreateDirectory(Path.Combine(mockBaseDirectory.Path, @"global-packages"));
                var mockHttpCacheDirectory = Directory.CreateDirectory(Path.Combine(mockBaseDirectory.Path, @"http-cache"));
                var mockTmpDirectory = Directory.CreateDirectory(Path.Combine(mockBaseDirectory.Path, @"temp"));
                var mockTmpCacheDirectory = Directory.CreateDirectory(Path.Combine(mockBaseDirectory.Path, @"NuGetScratch"));

                DotnetCliUtil.CreateTestFiles(mockGlobalPackagesDirectory.FullName);
                DotnetCliUtil.CreateTestFiles(mockHttpCacheDirectory.FullName);
                DotnetCliUtil.CreateTestFiles(mockTmpCacheDirectory.FullName);

                var cacheType = args.Split(null)[1].StartsWith("-") ? args.Split(null)[2] : args.Split(null)[1];

                // Act
                var result = CommandRunner.Run(
                      DotnetCliUtil.GetDotnetCli(),
                      Directory.GetCurrentDirectory(),
                      DotnetCliUtil.GetXplatDll() + " " + args,
                      waitForExit: true,
                    environmentVariables: new Dictionary<string, string>
                    {
                        { "NUGET_PACKAGES", mockGlobalPackagesDirectory.FullName },
                        { "NUGET_HTTP_CACHE_PATH", mockHttpCacheDirectory.FullName },
                        { RuntimeEnvironmentHelper.IsWindows ? "TMP" : "TMPDIR", mockTmpDirectory.FullName }
                    });
                // Unix uses TMPDIR as environment variable as opposed to TMP on windows

                // Assert
                if (StringComparer.Ordinal.Equals(cacheType, "all"))
                {
                    Assert.False(Directory.Exists(mockGlobalPackagesDirectory.FullName));
                    Assert.False(Directory.Exists(mockHttpCacheDirectory.FullName));
                    Assert.False(Directory.Exists(mockTmpCacheDirectory.FullName));
                }
                else if (StringComparer.Ordinal.Equals(cacheType, "global-packages"))
                {
                    Assert.False(Directory.Exists(mockGlobalPackagesDirectory.FullName));
                }
                else if (StringComparer.Ordinal.Equals(cacheType, "http-cache"))
                {
                    Assert.False(Directory.Exists(mockHttpCacheDirectory.FullName));
                }
                else
                {
                    Assert.False(Directory.Exists(mockTmpCacheDirectory.FullName));
                }
                DotnetCliUtil.VerifyResultSuccess(result, string.Empty);
            }
        }

        [Theory]
        [InlineData("locals")]
        [InlineData("locals --list")]
        [InlineData("locals -l")]
        [InlineData("locals --clear")]
        [InlineData("locals -c")]
        public static void Locals_Success_InvalidArguments_HelpMessage(string args)
        {
            // Arrange
            var expectedResult = string.Concat("error: usage: NuGet locals <all | http-cache | global-packages | temp> [--clear | -c | --list | -l]",
                                               Environment.NewLine,
                                               "error: For more information, visit http://docs.nuget.org/docs/reference/command-line-reference");

            // Act
            var result = CommandRunner.Run(
              DotnetCliUtil.GetDotnetCli(),
              Directory.GetCurrentDirectory(),
              DotnetCliUtil.GetXplatDll() + " " + args,
              waitForExit: true);

            // Assert
            DotnetCliUtil.VerifyResultFailure(result, expectedResult);
        }

        [Theory]
        [InlineData("locals --list unknownResource")]
        [InlineData("locals -l unknownResource")]
        [InlineData("locals --clear unknownResource")]
        [InlineData("locals -c unknownResource")]
        public static void Locals_Success_InvalidResourceName_HelpMessage(string args)
        {
            // Arrange
            var expectedResult = string.Concat("error: An invalid local resource name was provided. " +
                                               "Please provide one of the following values: http-cache, temp, global-packages, all.");

            // Act
            var result = CommandRunner.Run(
              DotnetCliUtil.GetDotnetCli(),
              Directory.GetCurrentDirectory(),
              DotnetCliUtil.GetXplatDll() + " " + args,
              waitForExit: true);

            // Assert
            DotnetCliUtil.VerifyResultFailure(result, expectedResult);
        }

        [Theory]
        [InlineData("locals -list")]
        [InlineData("locals -clear")]
        [InlineData("locals --l")]
        [InlineData("locals --c")]
        public static void Locals_Success_InvalidFlags_HelpMessage(string args)
        {
            // Arrange
            var expectedResult = string.Concat("Specify --help for a list of available options and commands.",
                                               Environment.NewLine, "error: Unrecognized option '", args.Split(null)[1], "'");

            // Act
            var result = CommandRunner.Run(
              DotnetCliUtil.GetDotnetCli(),
              Directory.GetCurrentDirectory(),
              DotnetCliUtil.GetXplatDll() + " " + args,
              waitForExit: true);

            // Assert
            DotnetCliUtil.VerifyResultFailure(result, expectedResult);
        }
    }
}