using System;
using System.Linq;

namespace CChangeDirectory
{
    static class Utils
    {
        public static string BashOrNot(string path)
        {
            var bashPrompt = Environment.GetEnvironmentVariable("PS1");
            if (!string.IsNullOrEmpty(bashPrompt) && bashPrompt.Any(char.IsLetter))
            {
                return PathExtensions.GetBashPath(path);
            }
            else
            {
                return path;
            }
        }
    }
}
