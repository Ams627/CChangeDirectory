using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CChangeDirectory
{

    public class IndexManager
    {
        const string CcdDirectory = ".ccd";
        const string IndexFilename = "index";
        const string LastFilename = "last";

        ISettings _settings;

        public IndexManager(ISettings settings)
        {
            this._settings = settings;
        }

        public void Create()
        {
            var gitRootDir = GitWorkTreeManager.GetGitRoot(Directory.GetCurrentDirectory());
            if (string.IsNullOrEmpty(gitRootDir))
            {
                throw new DirectoryNotFoundException($"Cannot find a directory containing a .git directory or file.");
            }
            var resultingIndex = new SortedDictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            var includes = _settings.GetList("include");
            var excludes = _settings.GetList("exclude");

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

                    var relativePathOfSubdir = PathExtensions.GetRelativePath(gitRootDir, subDir);
                    if (!includes.Any(x=>subDir.StartsWith(x)))
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

        internal void Lookup(string path, string pattern = "")
        {
            var gitRootDir = GitWorkTreeManager.GetGitRoot(Directory.GetCurrentDirectory());
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
                    var filteredList = string.IsNullOrEmpty(pattern) ? pathList : pathList.Where(x => Regex.Match(x, pattern, RegexOptions.IgnoreCase).Success).ToList();
                    if (filteredList.Count() == 1)
                    {
                        var cdPath = Utils.BashOrNot(Path.Combine(gitRootDir, filteredList[0]));
                        Console.WriteLine($"#!cd {cdPath}");
                    }
                    else
                    {
                        using (var writer = new StreamWriter(lastPath))
                        {
                            for (int i = 0; i < filteredList.Count(); i++)
                            {
                                Console.WriteLine($"{i} {filteredList[i]}");
                                writer.WriteLine($"{i}|{filteredList[i]}");
                            }
                        }
                    }
                }
            }
        }
    }
}