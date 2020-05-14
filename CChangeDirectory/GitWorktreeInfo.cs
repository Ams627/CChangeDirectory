using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CChangeDirectory
{
    public class GitWorktreeInfo
    {
        private readonly string _dir;
        private readonly bool _isGitRepo;
        private readonly bool _isMainDir;
        private readonly string _mainDir;
        private readonly List<(string dir, string branch)> _worktreeList = new List<(string dir, string branch)>();

        private const string HeadPattern = @"ref:\s+refs/heads/([^\s]+)\s*$";
        public GitWorktreeInfo(string dir)
        {
            _dir = dir;
            
            var gitRoot = GetGitRoot(dir);
            var dotGit = Path.Combine(gitRoot, ".git");

            if (File.Exists(dotGit))
            {
                var gitline = File.ReadAllLines(dotGit).Where(x => !string.IsNullOrWhiteSpace(x)).Select(y => y.Trim()).First();
                var colonPos = gitline.IndexOf(':');
                if (colonPos == -1)
                {
                    throw new Exception($"Invalid .git file: {dotGit}");
                }
                if (gitline.Substring(0, colonPos) != "gitdir")
                {
                    throw new Exception($"Invalid .git file: the first line must start with 'gitdir:' ({dotGit})");
                }

                var gitDir = Path.GetFullPath(gitline.Substring(colonPos + 1)).Trim();
                if (!Directory.Exists(gitDir))
                {
                    throw new Exception($"Git file ({dotGit}) points to non-existent directory ({gitDir}).");
                }

                var mainDirInfo = new DirectoryInfo(gitDir)?.Parent?.Parent?.Parent;
                if (mainDirInfo == null)
                {
                    throw new Exception($"Git file ({dotGit}) does not point to a valid git working tree ({gitDir}).");
                }

                _mainDir = mainDirInfo.FullName;

                // from the gitdir in the file, get the full list of worktrees:
                var gitSubDirs = GetGitSubDirs(gitDir, true);
                _worktreeList.AddRange(gitSubDirs);
                _isMainDir = false;
                _isGitRepo = true;
            }
            else if (Directory.Exists(dotGit))
            {
                var mainDirInfo = new DirectoryInfo(dotGit)?.Parent;
                if (mainDirInfo == null)
                {
                    throw new Exception($"Git file ({dotGit}) does not point to a valid git working tree.");
                }

                _mainDir = mainDirInfo.FullName;
                var gitSubDirs = GetGitSubDirs(dotGit, false);
                _worktreeList.AddRange(gitSubDirs);
                _isMainDir = true;
                _isGitRepo = true;
            }
            else
            {
                _isGitRepo = false;
            }
        }

        /// <summary>
        /// The main git directory (in which we find the .git directory). This is always set if we are in a valid git repo regardless of whether or not
        /// the directory supplied to create this instance is in the main worktree or in a secondary worktree created using git worktree add.
        /// </summary>
        public string GitDir => _mainDir;

        /// <summary>
        /// true if we can find the .git folder (either directly or via a .git file in the root of a secondary worktree created with git worktree add
        /// </summary>
        public bool IsGitRepo => _isGitRepo;

        /// <summary>
        /// does this instance represent a directory under the main git directory or under a git worktree?
        /// if under a git worktree (created by git worktree add) this will be false:
        /// </summary>
        public bool IsMainDir => _isMainDir;

        /// <summary>
        /// a list of worktrees - the main worktree is always in Worktrees[0]. For each one we get the root of the worktree and the branch name
        /// </summary>
        public (string dir, string branch)[] WorkTrees => _worktreeList.ToArray();

        /// <summary>
        /// Given a starting folder, returns the path to .git in the root of the git repo, or the empty string if we are not in a git repo.
        /// .git can be a file if this is worktree created by git worktree add, or it will be a directory in the default case of a git
        /// repo created using git init without any subsequent git worktree add commands.
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static string GetGitRoot(string dir)
        {
            var dotGitPath = Path.Combine(dir, ".git");
            while (!File.Exists(dotGitPath) && !Directory.Exists(dotGitPath))
            {
                var nextDirectoryUp = new DirectoryInfo(dir).Parent;
                if (nextDirectoryUp == null)
                {
                    return string.Empty;
                }
                dir = nextDirectoryUp.FullName;
                dotGitPath = Path.Combine(dir, ".git");
            }
            return dir;
        }

        /// <summary>
        /// Get a list of worktree root directories given a git directory which is either
        ///     1. the repo main directory (the one CONTAINING the .git folder) - in the case of a single worktree (the default git repo state) 
        ///     or in the case of a main worktree (where one or more secondary worktrees have been added and this is the main worktree)
        ///     2. A gitdir - the directory read from a .git FILE in one of the secondary worktrees created by git worktree add
        /// </summary>
        /// <param name="dir">The repo main directory (which contains the .git directory) or a secondary worktree directory including .git/worktrees/[worktree]</param>
        /// <param name="isSecondaryWorktree">true if dir is read from the .git file in a worktree created by git worktree add</param>
        /// <returns>A list of tuples. Each tuple is dir: |worktree-dir| branch: |branch|</returns>
        private List<(string dir, string branchName)> GetGitSubDirs(string dir, bool isSecondaryWorktree)
        {
            var result = new List<(string dir, string branchname)>();

            // for secondary worktrees the dir is repoDir/.git/worktrees
            var workTreesDir = isSecondaryWorktree ? new DirectoryInfo(dir).Parent.FullName : Path.Combine(dir, "worktrees");

            // add main gitdir - it's always the first in the list:
            DirectoryInfo mainDirInfo;
            if (isSecondaryWorktree)
            {
                mainDirInfo = new DirectoryInfo(dir).Parent.Parent.Parent;
            }
            else
            {
                mainDirInfo = new DirectoryInfo(dir).Parent;
            }

            if (mainDirInfo == null)
            {
                throw new Exception($"{dir} does not specify a valid git directory");
            }
            result.Add((mainDirInfo.FullName, GetGitBranchName(Path.Combine(mainDirInfo.FullName, ".git", "HEAD"))));

            // worktrees dir will only exist after git worktree add:
            if (Directory.Exists(workTreesDir))
            {
                var gitFiles = Directory.GetDirectories(workTreesDir).Select(x => (gitdir: Path.Combine(x, "gitdir"), head: Path.Combine(x, "HEAD")));
                foreach (var (gitdir, head) in gitFiles)
                {
                    var gitDirLine = File.ReadAllLines(gitdir).Where(x => !string.IsNullOrWhiteSpace(x)).Select(y => y.Trim()).First();
                    var gitFile = Path.GetFullPath(gitDirLine);
                    var parentDir = Path.GetDirectoryName(gitFile);

                    result.Add((parentDir, GetGitBranchName(head)));
                }
            }

            return result;
        }

        private string GetGitBranchName(string headFile)
        {
            var branch = "";
            var headMatch = File.ReadAllLines(headFile).Select(x => Regex.Match(x, HeadPattern)).Where(y => y.Success);
            if (headMatch.Any())
            {
                var match = headMatch.First();
                if (match.Groups.Count > 1)
                {
                    branch = match.Groups[1].Value;
                }
            }

            return branch;
        }
    }
}