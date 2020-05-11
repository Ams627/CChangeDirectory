using System.IO;
using System.Linq;

namespace CChangeDirectory
{
    public class PathExtensions
    {
        public static string GetRelativePath(string relativeTo, string path)
        {
            var relativeFull = Path.GetFullPath(relativeTo);
            var pathFull = Path.GetFullPath(path);

            var rootRelative = Path.GetPathRoot(relativeFull);
            var rootFull = Path.GetPathRoot(pathFull);

            // paths do not share a common root:
            if (rootRelative != rootFull)
            {
                return path;
            }

            var splitRelative = relativeFull.Split(Path.DirectorySeparatorChar).Where(x => x != string.Empty).ToArray();
            var splitPath = pathFull.Split(Path.DirectorySeparatorChar).Where(x => x != string.Empty).ToArray();

            int firstNonCommon = 0;
            for (int i = 0; i < splitPath.Length; i++)
            {
                bool segmentEqual = true;
                if (i < splitRelative.Length)
                {
                    segmentEqual = splitRelative[i] == splitPath[i];
                    if (segmentEqual)
                    {
                        continue;
                    }
                }
                if (!segmentEqual)
                {
                    return pathFull;
                }
                firstNonCommon = i;
                break;
            }

            var result = Path.Combine(splitPath.Skip(firstNonCommon).ToArray());
            return result;
        }
    }
}