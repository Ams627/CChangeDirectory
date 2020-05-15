namespace CChangeDirectory
{
    /// <summary>
    /// Class containing the necessary information for a single worktree - either the primary one or a secondary worktree added
    /// by git worktree add.
    /// </summary>
    public class GitWorktreeInfo
    {
        public string BranchName { get; set; }
        public string CommitHash { get; set; }
        public string Directory { get; set; }
        public string Description { get; set; }
    }
}