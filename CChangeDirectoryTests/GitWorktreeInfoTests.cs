using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.IO;
using FluentAssertions;
using System;
using System.Linq;
using System.Collections.Generic;

namespace CChangeDirectory.Tests
{
    [TestClass()]
    public class GitWorktreeInfoTests
    {
        [ClassInitialize]
        public static void Init()
        {
            // try to delete the directories 0000, 0001, etc which are used for this test. If we can't delete them, we'll just ignore the error and the end-user
            // will have to cleanup:
            var intDirs = Directory.GetDirectories(Directory.GetCurrentDirectory()).Select(path => new DirectoryInfo(path).Name).Where(x => x.Length == 4 && x.All(char.IsDigit));
            foreach (var dir in intDirs)
            {
                try
                {
                    Directory.Delete(dir);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"cannot delete directory {dir}: {ex.GetType()}: {ex.Message}");
                }
            }
        }

        [TestMethod()]
        public void GitWorktreeInfoTest()
        {
            var gitTestDirectory = "gitTestDir";
            Directory.CreateDirectory(gitTestDirectory);
            Directory.SetCurrentDirectory(gitTestDirectory);

            // Create a numeric test directory - we might not be able to erase others, so create a new one for each test:
            var numericTestDir = GetNextTestDir();
            Directory.CreateDirectory(numericTestDir);
            Directory.SetCurrentDirectory(numericTestDir);

            // Create a project directory:
            var gitProjDir = "Project1-B1";
            Directory.CreateDirectory(gitProjDir);
            Directory.SetCurrentDirectory(gitProjDir);

            // do a git init and store the main git folder:
            RunCommand("git", "init").Should().Be(0);
            var mainGitDir = Directory.GetCurrentDirectory();

            // add a single file to the repo and commit it:
            var repoFile = "testfile1.txt";
            File.WriteAllLines(repoFile, new[] { "the", "fat", "cat" });
            RunCommand("git", $"add {repoFile}").Should().Be(0);
            RunCommand("git", "commit -m \"first commit\"").Should().Be(0);

            // add new git worktrees:
            int numberOfTrees = 4;
            var branches = Enumerable.Range(1, numberOfTrees).Select(x => $"branch{x}").ToList();
            branches.ForEach(x => RunCommand("git", $"worktree add -b new{x} ../{gitProjDir}-{x}").Should().Be(0));

            var upperDirectory = new DirectoryInfo(mainGitDir).Parent.FullName;
            var expectedBranchDirectories = new List<string> { mainGitDir };
            expectedBranchDirectories.AddRange(branches.Select(x => Path.Combine(upperDirectory, $"{gitProjDir}-{x}")));

            // we're still in the main git directory - create a random folder and change into it:
            var testDir = "Toast/Egg/Jam";
            Directory.CreateDirectory(testDir);
            Directory.SetCurrentDirectory(testDir);

            var currentDir = Directory.GetCurrentDirectory();
            var gitWorktreeInfo = new GitWorktreeInfo(currentDir);

            var mainDir = gitWorktreeInfo.GitDir;
            mainDir.Should().Be(mainGitDir);
            gitWorktreeInfo.IsGitRepo.Should().BeTrue();
            gitWorktreeInfo.IsMainDir.Should().BeTrue();
            var worktreeList = gitWorktreeInfo.WorkTrees;
            worktreeList.Should().HaveCount(numberOfTrees + 1).And.BeEquivalentTo(expectedBranchDirectories);
            worktreeList[0].Should().Be(mainGitDir);

            // change into the parent directory of the main git dir, then change into one of the worktree subdirs:
            Directory.SetCurrentDirectory(upperDirectory);
            Directory.SetCurrentDirectory($"{gitProjDir}-branch{1}");
            var testDir2 = @"spam/salami/chicken";
            Directory.CreateDirectory(testDir2);
            Directory.SetCurrentDirectory(testDir2);
            var dircheck = Directory.GetCurrentDirectory();
            dircheck.Should().EndWith("chicken");

            var worktreeInfo2 = new GitWorktreeInfo(dircheck);
            var mainDir2 = worktreeInfo2.GitDir;
            mainDir2.Should().Be(mainGitDir);
            worktreeInfo2.IsGitRepo.Should().BeTrue();
            worktreeInfo2.IsMainDir.Should().BeFalse();
            var wtreeList = worktreeInfo2.WorkTrees;
            wtreeList.Should().HaveCount(numberOfTrees + 1).And.BeEquivalentTo(expectedBranchDirectories);
            wtreeList[0].Should().Be(mainGitDir);
        }


        /// <summary>
        /// Get a numbered four digit directory starting with 0001. If 0001 exists, then return 0002 etc.
        /// </summary>
        /// <returns>a string containing the new directory name - just the final name componenent not the complete path.</returns>
        private static string GetNextTestDir()
        {
            var intDirs = Directory.GetDirectories(Directory.GetCurrentDirectory()).Select(path=>new DirectoryInfo(path).Name).Where(x=>x.Length == 4 && x.All(char.IsDigit)).Select(y=>Int32.Parse(y)).OrderByDescending(z=>z);
            var nextDir = intDirs.Any() ? 1 + intDirs.First() : 1;
            return $"{nextDir:D4}";
        }


        /// <summary>
        /// Run a command using Process.Start - used mainly for running git:
        /// </summary>
        /// <param name="program">program name</param>
        /// <param name="args">arguments - space separated</param>
        /// <returns>The exit code of the command</returns>
        private int RunCommand(string program, string args)
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = program,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            var process = Process.Start(processInfo);

            Debug.WriteLine($"---- stdout: ----");
            while (!process.StandardOutput.EndOfStream)
            {
                var line = process.StandardOutput.ReadLine();
                Debug.WriteLine(line);
            }
            Debug.WriteLine($"---- stderr: ----");
            while (!process.StandardError.EndOfStream)
            {
                var line = process.StandardError.ReadLine();
                Debug.WriteLine(line);
            }

            process.WaitForExit();
            var result = process.ExitCode;
            return result;
        }

    }
}
