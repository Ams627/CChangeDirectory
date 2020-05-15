using Microsoft.VisualStudio.TestTools.UnitTesting;
using CChangeDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using FluentAssertions;

namespace CChangeDirectory.Tests
{
    [TestClass()]
    public class IndexManagerTests
    {
        [TestMethod()]
        public void CreateTestWithInclude()
        {
            var path = Path.GetTempPath();
            var dir1 = Path.Combine(path, "Quite-a-long-path");
            Directory.CreateDirectory(dir1);
            Directory.SetCurrentDirectory(dir1);
            var cwd = Directory.GetCurrentDirectory();
            Assert.IsTrue(Directory.Exists(cwd));
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


        }

        [TestMethod()]
        public void CreateTest()
        {
            var path = Path.GetTempPath();
            var dir1 = Path.Combine(path, "Fred", "Jim", "Sally");
            Directory.CreateDirectory(dir1);
            Directory.SetCurrentDirectory(dir1);
            var cwd = Directory.GetCurrentDirectory();
            Assert.IsTrue(Directory.Exists(cwd));
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


            // get expected result:
            var keys = new HashSet<string>();
            foreach (var dir in testDirs)
            {
                var segments = dir.Split('.');
                foreach (var segment in segments)
                {
                    if (segment.Contains('.'))
                    {
                        var subsegments = segment.Split('.');
                        foreach (var subsegment in subsegments)
                        {
                            keys.Add(subsegment);
                        }
                        keys.Add(segment);
                    }
                }
            }

            Directory.SetCurrentDirectory(testDirs[0]);

            var settings = new MockSettings(1);

            var indexManager = new IndexManager(settings);
//            indexManager.Create();
            var indexDir = Path.Combine(dir1, ".ccd");
            Assert.IsTrue(Directory.Exists(indexDir), "index dir does not exist");
            Assert.IsTrue(File.Exists(Path.Combine(indexDir, "index")), "index file does not exists");

            var indexFilePath = Path.Combine(indexDir, "index");
            var lookup = File.ReadAllLines(indexFilePath).Select(x => x.Split('|')).Select(y => new { Key = y[0], Value = y.Skip(1).ToList() }).ToLookup(z => z.Key, StringComparer.OrdinalIgnoreCase);
            Assert.IsFalse(lookup.Where(x => x.Count() != 1).Any());

            var cow = lookup["cow"].First().Value.ToHashSet();
            var expected1 = new HashSet<string> { @"duck\cow.green", @"duck\cow.blue", @"duck\cow.yellow" };
            cow.Should().Contain(expected1);

            var duck = lookup["duck"].First().Value.ToHashSet();
            duck.Should().Contain(new HashSet<string> { "duck" });

            var frog = lookup["frog"].First().Value.ToHashSet();
            frog.Should().Contain(new HashSet<string> { @"pig\cow\frog" });

            var sally = lookup["sally"].First().Value.ToHashSet();
            var sheila = lookup["Sheila"].First().Value.ToHashSet();
            var bob = lookup["bob"].First().Value.ToHashSet();
            var harry = lookup["harry"].First().Value.ToHashSet();

            sally.Should().Contain("Sally");
            sheila.Should().Contain(@"Sally\Sheila");
            bob.Should().Contain(@"Sally\Sheila\Bob");
            harry.Should().Contain(@"Sally\Sheila\Bob\Harry");
        }
    }
}