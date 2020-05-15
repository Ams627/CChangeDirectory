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
                var currentGitRoot = GitWorkTreeManager.GetGitRoot(Directory.GetCurrentDirectory());
                var settings = new Settings(Path.Combine(currentGitRoot, ".ccd", "settings"));

                if (args.Length == 1 && args[0] == "-i")
                {
                    var indexManager = new IndexManager(settings);
                    indexManager.Create();
                }
                else if (args.Any())
                {
                    var path = args[0];
                    if (Directory.Exists(path))
                    {
                        Console.WriteLine($"#!cd {Utils.BashOrNot(path)}");
                    }
                    else if (File.Exists(path))
                    {
                        Console.WriteLine($"#!cd {Utils.BashOrNot(Path.GetDirectoryName(path))}");
                    }
                    else if (path == "gl")
                    {
                        var gitWorktreeInfo = new GitWorkTreeManager(Directory.GetCurrentDirectory());
                        var worktrees = gitWorktreeInfo.WorkTrees;

                        int count = 0;
                        foreach (var worktree in worktrees)
                        {
                            Console.WriteLine($"{count++} {worktree.BranchName} {worktree.Directory} {worktree.Description}");
                        }
                    }
                    else if (path[0] == 'g' && path.Substring(1).All(char.IsDigit))
                    {
                        var index = Int32.Parse(path.Substring(1));
                        var gitWorktreeInfo = new GitWorkTreeManager(Directory.GetCurrentDirectory());
                        var worktrees = gitWorktreeInfo.WorkTrees;
                        if (index > worktrees.Length - 1)
                        {
                            throw new Exception($"index out of range - maximum is {worktrees.Length - 1}");
                        }

                        var relpath = PathExtensions.GetRelativePath(currentGitRoot, Directory.GetCurrentDirectory());
                        var fullOtherPath = Path.Combine(worktrees[index].Directory, relpath);
                        Console.WriteLine($"#!cd {Utils.BashOrNot(fullOtherPath)}");
                    }
                    else if (path[0] == 'g' && int.TryParse(path.Substring(1, path.Length - 2), out var index) && char.IsLetter(path.Last()))
                    {
                        var gitWorktreeInfo = new GitWorkTreeManager(Directory.GetCurrentDirectory());
                        var worktrees = gitWorktreeInfo.WorkTrees;
                        if (index > worktrees.Length - 1)
                        {
                            throw new Exception($"index out of range - maximum is {worktrees.Length - 1}");
                        }
                        SubstMappingsHelper.CreateMapping(path.Last(), worktrees[index].Directory, deleteIfExisting:true);
                    }
                    else if (path[0] == 'g' && path.Last() == '-' && char.IsLetter(path[1]))
                    {
                        SubstMappingsHelper.DeleteMapping(path[1]);
                    }
                    else 
                    {
                        var pattern = args.Length > 1 ? args[1] : string.Empty;
                        var indexManager = new IndexManager(settings);
                        indexManager.Lookup(path, pattern);
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
