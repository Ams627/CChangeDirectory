using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CChangeDirectory
{
    class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                if (args.Length == 1 && args[0] == "-i")
                {
                    var indexManager = new IndexManager();
                    indexManager.Create();
                }
                else
                {
                    var path = args[0];
                    if (Directory.Exists(path))
                    {
                        Console.WriteLine($"#!cd {path}");
                    }
                    else if (File.Exists(path))
                    {
                        Console.WriteLine($"#!cd {Path.GetDirectoryName(path)}");
                    }
                    else if (path == "gl")
                    {
                        var gitWorktreeInfo = new GitWorktreeInfo(Directory.GetCurrentDirectory());
                        var worktree = gitWorktreeInfo.WorkTrees;

                        int count = 0;
                        foreach (var (dir, branch) in worktree)
                        {
                            Console.WriteLine($"{count++} {branch} {dir}");
                        }
                    }
                    else if (path[0] == 'g' && path.Substring(1).All(char.IsDigit))
                    {
                        var index = Int32.Parse(path.Substring(1));
                        var gitWorktreeInfo = new GitWorktreeInfo(Directory.GetCurrentDirectory());
                        var worktrees = gitWorktreeInfo.WorkTrees;
                        if (index > worktrees.Length - 1)
                        {
                            throw new Exception($"index out of range - maximum is {worktrees.Length - 1}");
                        }

                        var currentGitRoot = GitWorktreeInfo.GetGitRoot(Directory.GetCurrentDirectory());

                        var relpath = PathExtensions.GetRelativePath(currentGitRoot, Directory.GetCurrentDirectory());

                        var fullOtherPath = Path.Combine(worktrees[index].dir, relpath);
                        Console.WriteLine($"#!cd {fullOtherPath}");
                    }
                    else if (path[0] == 'g' && int.TryParse(path.Substring(1, path.Length - 2), out var index) && char.IsLetter(path.Last()))
                    {
                        var gitWorktreeInfo = new GitWorktreeInfo(Directory.GetCurrentDirectory());
                        var worktrees = gitWorktreeInfo.WorkTrees;
                        if (index > worktrees.Length - 1)
                        {
                            throw new Exception($"index out of range - maximum is {worktrees.Length - 1}");
                        }
                        SubstMappingsHelper.CreateMapping(path.Last(), worktrees[index].dir, deleteIfExisting:true);
                    }
                    else if (path[0] == 'g' && path.Last() == '-' && char.IsLetter(path[1]))
                    {
                        SubstMappingsHelper.DeleteMapping(path[1]);
                    }
                    else
                    {
                        var indexManager = new IndexManager();
                        indexManager.Lookup(path);
                    }
                }
            }
            catch (Exception ex)
            {
                var fullname = System.Reflection.Assembly.GetEntryAssembly().Location;
                var progname = Path.GetFileNameWithoutExtension(fullname);
                Console.Error.WriteLine(progname + ": Error: " + ex.Message);
            }

        }
    }
}
