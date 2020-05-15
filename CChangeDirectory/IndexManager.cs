using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CChangeDirectory
{

    public class IndexManager
    {
        const string CcdDirectory = ".ccd";
        const string IndexFilename = "index";
        const string LastFilename = "last";
        public IndexManager()
        {
        }

        public void Create()
        {
            var gitRootDir = GitWorktreeInfo.GetGitRoot(Directory.GetCurrentDirectory());
            if (string.IsNullOrEmpty(gitRootDir))
            {
                throw new DirectoryNotFoundException($"Cannot find a directory containing a .git directory or file.");
            }
            var resultingIndex = new SortedDictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            var dirStack = new Stack<string>();
            dirStack.Push(gitRootDir);

            while (dirStack.Any())
            {
                var dir = dirStack.Pop();
                var subDirs = Directory.GetDirectories(dir);
                foreach (var subDir in subDirs)
                {
                    var name = new DirectoryInfo(subDir).Name;

                    if (name.StartsWith(".") || name == "obj" || name.StartsWith("Deploy", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    dirStack.Push(subDir);

                    var relativeDir = PathExtensions.GetRelativePath(gitRootDir, subDir);
                    if (name.Contains("."))
                    {
                        var segments = name.Split('.');
                        foreach (var segment in segments)
                        {
                            DictUtils.AddEntryToList(resultingIndex, segment, relativeDir);
                        }
                    }
                    if (name.Contains(" "))
                    {
                        var segments = name.Split(' ');
                        foreach (var segment in segments)
                        {
                            DictUtils.AddEntryToList(resultingIndex, segment, relativeDir);
                        }
                    }

                    DictUtils.AddEntryToList(resultingIndex, name, relativeDir);
                }
            }

            var ccdPath = Path.Combine(gitRootDir, CcdDirectory);
            Directory.CreateDirectory(ccdPath);
            var indexPath = Path.Combine(ccdPath, IndexFilename);

            using (var writer = new StreamWriter(indexPath))
            {
                foreach (var entry in resultingIndex)
                {
                    var matchingPaths = string.Join("|", entry.Value);
                    writer.WriteLine($"{entry.Key}|{matchingPaths}");
                }
            }
        }

        internal void Lookup(string path)
        {
            var gitRootDir = GitWorktreeInfo.GetGitRoot(Directory.GetCurrentDirectory());
            if (string.IsNullOrEmpty(gitRootDir))
            {
                throw new DirectoryNotFoundException($"Cannot find a directory containing a .git directory or file.");
            }

            var ccdPath = Path.Combine(gitRootDir, CcdDirectory);
            var lastPath = Path.Combine(ccdPath, LastFilename);
            var indexPath = Path.Combine(ccdPath, IndexFilename);

            if (File.Exists(lastPath) && path.Length < 3 && path.All(char.IsDigit))
            {
                var lastlines = File.ReadAllLines(lastPath);
                var separatorArray = new char[] { '|' };
                foreach (var line in lastlines)
                {
                    var segments = line.Split(separatorArray, StringSplitOptions.RemoveEmptyEntries);
                    if (segments.Count() >= 2 && segments[0] == path)
                    {
                        Console.WriteLine($"#!cd {segments[1]}");
                    }
                }
            }
            else
            {
                if (!File.Exists(indexPath))
                {
                    throw new FileNotFoundException($"ccd index file not found - please run ccd -i first.");
                }

                var dict = File.ReadAllLines(indexPath).Select(x => x.Split('|')).ToDictionary(y => y[0], y => y.Skip(1).ToList(), StringComparer.OrdinalIgnoreCase);
                if (dict.TryGetValue(path, out var pathList))
                {
                    if (pathList.Count() == 1)
                    {
                        Console.WriteLine($"#!cd {pathList[0]}");
                    }
                    else
                    {
                        using (var writer = new StreamWriter(lastPath))
                        {
                            for (int i = 0; i < pathList.Count(); i++)
                            {
                                Console.WriteLine($"{i} {pathList[i]}");
                                writer.WriteLine($"{i}|{pathList[i]}");
                            }
                        }
                    }
                }
            }
        }
    }
}