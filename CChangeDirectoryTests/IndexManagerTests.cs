using Microsoft.VisualStudio.TestTools.UnitTesting;
using CChangeDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using FluentAssertions;
using System.Diagnostics;

namespace CChangeDirectory.Tests
{
    [TestClass()]
    public class IndexManagerTests
    {
        static string TestPath = "";

        [ClassInitialize]
        public static void Init(TestContext context)
        {
            var fullname = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var progname = Path.GetFileNameWithoutExtension(fullname);
            var tempPath = Path.GetTempPath();
            TestPath = Path.Combine(tempPath, progname);

            Directory.CreateDirectory(TestPath);

            // try to delete the directories 0000, 0001, etc which are used for this test. If we can't delete them, we'll just ignore the error and the end-user
            // will have to cleanup:
            var intDirs = Directory.GetDirectories(TestPath).Select(path => new DirectoryInfo(path).Name).Where(x => x.Length == 4 && x.All(char.IsDigit));
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

            var testDir = GetNextTestDir();
            var fullTestDirPath = Path.Combine(TestPath, testDir);
            Directory.CreateDirectory(fullTestDirPath);
            Directory.SetCurrentDirectory(fullTestDirPath);
        }

        /// <summary>
        /// Test the case where we have an include line in the .ccd/settings file
        /// </summary>
        [TestMethod()]
        public void CreateTestWithInclude()
        {
            var dir1 = "Quite-a-long-name";

            Directory.CreateDirectory(dir1);
            Directory.SetCurrentDirectory(dir1);

            Directory.CreateDirectory(".git");

            var testDirs = new[]
            {
                "Sally/Sheila/Bob/Harry",
                "duck/goose/farm/cow.green",
                "duck/goose/farm/cow.brown",
                "duck/cow.blue",
                "duck/cow.yellow",
                "pig/cow/frog"
            }.ToList();
            testDirs.ForEach(x => Directory.CreateDirectory(x));

            var mockSettings = new MockSettings(2);
            var indexManager = new IndexManager(mockSettings);
            indexManager.Create();

            var indexDir = ".ccd";
            Assert.IsTrue(Directory.Exists(indexDir), "index dir does not exist");
            Assert.IsTrue(File.Exists(Path.Combine(indexDir, "index")), "index file does not exists");

            var indexFilePath = Path.Combine(indexDir, "index");
            var lookup = File.ReadAllLines(indexFilePath).Select(x => x.Split('|')).Select(y => new { Key = y[0], Value = y.Skip(1).ToList() }).ToLookup(z => z.Key, StringComparer.OrdinalIgnoreCase);
            (lookup.Where(x => x.Count() != 1)).Should().BeEmpty();
            lookup["cow"].First().Value.Should().BeEquivalentTo(@"duck\goose\farm\cow.green", @"duck\goose\farm\cow.brown");
        }

        [TestMethod()]
        public void CreateTest()
        {
            var dir1 = Path.Combine("Fred", "Jim", "Sally");
            Directory.CreateDirectory(dir1);
            Directory.SetCurrentDirectory(dir1);


            var cwd = Directory.GetCurrentDirectory();
            Directory.CreateDirectory(".git");

            var testDirs = new[]
            {
                "Sally/Sheila/Bob/Harry",
                "duck/cow.green",
                "duck/cow.blue",
                "duck/cow.yellow",
                "pig/cow/frog"
            }.ToList();
            testDirs.ForEach(x => Directory.CreateDirectory(x));

            var settings = new MockSettings(1);
            var indexManager = new IndexManager(settings);
            indexManager.Create();

            var indexDir = ".ccd";
            Assert.IsTrue(Directory.Exists(indexDir), "index dir does not exist");
            Assert.IsTrue(File.Exists(Path.Combine(indexDir, "index")), "index file does not exists");

            var indexFilePath = Path.Combine(indexDir, "index");
            var lookup = File.ReadAllLines(indexFilePath).Select(x => x.Split('|')).Select(y => new { Key = y[0], Value = y.Skip(1).ToList() }).ToLookup(z => z.Key, StringComparer.OrdinalIgnoreCase);

            // should be zero cases where there is more than one key:
            Assert.IsFalse(lookup.Where(x => x.Count() != 1).Any());

            var n = lookup["Sheila"];

            lookup["cow"].First().Value.Should().BeEquivalentTo(@"duck\cow.green", @"duck\cow.blue", @"duck\cow.yellow", @"pig\cow");
            lookup["duck"].First().Value.Should().BeEquivalentTo("duck");
            lookup["frog"].First().Value.Should().BeEquivalentTo(@"pig\cow\frog");
            lookup["sally"].First().Value.Should().BeEquivalentTo("Sally");
            lookup["sheila"].First().Value.Should().BeEquivalentTo(@"Sally\Sheila");
            lookup["bob"].First().Value.Should().BeEquivalentTo(@"Sally\Sheila\Bob");
            lookup["harry"].First().Value.Should().BeEquivalentTo(@"Sally\Sheila\Bob\Harry");
        }

        [TestMethod()]
        public void CreateTestPartial()
        {
            var dir1 = Path.Combine("Fred", "Jim", "Sally");
            Directory.CreateDirectory(dir1);
            Directory.SetCurrentDirectory(dir1);

            var cwd = Directory.GetCurrentDirectory();
            Directory.CreateDirectory(".git");

            var testDirs = new[]
            {
                "Sally/Sheila/Bob/Harry",
                "duck/cow.green",
                "duck/cow.blue",
                "duck/cow.yellow",
                "pig/cow/frog"
            }.ToList();
            testDirs.ForEach(x => Directory.CreateDirectory(x));

            var settings = new MockSettings(1);
            var indexManager = new IndexManager(settings);
            indexManager.Create();

            var indexDir = ".ccd";
            Assert.IsTrue(Directory.Exists(indexDir), "index dir does not exist");
            Assert.IsTrue(File.Exists(Path.Combine(indexDir, "index")), "index file does not exists");

            var indexFilePath = Path.Combine(indexDir, "index");
            var lookup = File.ReadAllLines(indexFilePath).Select(x => x.Split('|')).Select(y => new { Key = y[0], Value = y.Skip(1).ToList() }).ToLookup(z => z.Key, StringComparer.OrdinalIgnoreCase);

            // should be zero cases where there is more than one key:
            Assert.IsFalse(lookup.Where(x => x.Count() != 1).Any());

            var n = lookup["Sheila"];

            lookup["cow"].First().Value.Should().BeEquivalentTo(@"duck\cow.green", @"duck\cow.blue", @"duck\cow.yellow", @"pig\cow");
            lookup["duck"].First().Value.Should().BeEquivalentTo("duck");
            lookup["frog"].First().Value.Should().BeEquivalentTo(@"pig\cow\frog");
            lookup["sally"].First().Value.Should().BeEquivalentTo("Sally");
            lookup["sheila"].First().Value.Should().BeEquivalentTo(@"Sally\Sheila");
            lookup["bob"].First().Value.Should().BeEquivalentTo(@"Sally\Sheila\Bob");
            lookup["harry"].First().Value.Should().BeEquivalentTo(@"Sally\Sheila\Bob\Harry");
        }



        /// <summary>
        /// Get a numbered four digit directory starting with 0001. If 0001 exists, then return 0002 etc.
        /// </summary>
        /// <returns>a string containing the new directory name - just the final name componenent not the complete path.</returns>
        private static string GetNextTestDir()
        {
            var intDirs = Directory.GetDirectories(Directory.GetCurrentDirectory()).Select(path => new DirectoryInfo(path).Name).Where(x => x.Length == 4 && x.All(char.IsDigit)).Select(y => Int32.Parse(y)).OrderByDescending(z => z);
            var nextDir = intDirs.Any() ? 1 + intDirs.First() : 1;
            return $"{nextDir:D4}";
        }


        [ClassCleanup]
        public static void Cleanup()
        {
        }
    }
}