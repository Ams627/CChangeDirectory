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
        public IndexManager()
        {
        }

        public void Create()
        {
            var gitRootDir = GitWorktreeInfo.GetGitDir(Directory.GetCurrentDirectory());
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

                    if (name.StartsWith("."))
                    {
                        continue;
                    }

                    dirStack.Push(subDir);

                    var relativeDir = GetRelativeDir(gitRootDir, subDir);
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

        private string GetRelativeDir(string rootDir, string path)
        {
            if (!path.StartsWith(rootDir))
            {
                return path;
            }
            return path.Substring(rootDir.Length + 1);
        }

        internal void Lookup(string path)
        {
            throw new NotImplementedException();
        }


    }

}