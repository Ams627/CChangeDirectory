using Microsoft.VisualStudio.TestTools.UnitTesting;
using CChangeDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace CChangeDirectory.Tests
{
    [TestClass()]
    public class IndexManagerTests
    {
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

            Directory.SetCurrentDirectory(testDirs[0]);

            var indexManager = new IndexManager();
            indexManager.Create();
            var indexDir = Path.Combine(dir1, ".ccd");
            Assert.IsTrue(Directory.Exists(indexDir), "index dir does not exist");
            Assert.IsTrue(File.Exists(Path.Combine(indexDir, "index")), "index file does not exists");

            var indexFilePath = Path.Combine(indexDir, "index");
            var lookup = File.ReadAllLines(indexFilePath).Select(x => x.Split('|')).Select(y => new { Key = y[0], Value = y.Skip(1).ToList() }).ToLookup(z => z.Key, StringComparer.OrdinalIgnoreCase);
            Assert.IsFalse(lookup.Where(x => x.Count() != 1).Any());

            var cow = lookup["cow"].First().Value.ToHashSet();
            var expected = new HashSet<string> { @"duck\cow.green", @"duck\cow.blue", @"duck\cow.yellow", @"pig\cow\frog" };

            Assert.IsTrue(cow.SetEquals(expected));

        }
    }
}