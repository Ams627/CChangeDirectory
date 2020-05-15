using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CChangeDirectory
{
    public interface ISettings
    {
        string[] GetList(string settingName);
    }
    public class Settings : ISettings
    {
        Dictionary<string, List<string>> listSettings = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        // Dictionary<string, string> singleSettings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Settings(string settingsFile)
        {
            if (!File.Exists(settingsFile))
            {
                var directory = Path.GetDirectoryName(settingsFile);
                Directory.CreateDirectory(directory);
                File.WriteAllLines(settingsFile, new[] {
                    "# Place repo-specific settings here. Comments start with #",
                    "# include=dir to include a directory",
                    "# By default all directories in the repo are included but if there are any includes then nothing else will be included",
                    "# exclude=dir to exclude a directory. Exclude filters are applied after all the include filters."
                });
            }
            else
            {
                foreach (var line in File.ReadLines(settingsFile).Select(x => x.Trim()))
                {
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                    {
                        continue;
                    }

                    if (line.Count(x => x == '=') == 1)
                    {
                        var segments = line.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
                        if (segments[0].Equals("include", StringComparison.OrdinalIgnoreCase))
                        {
                            DictUtils.AddEntryToList(listSettings, "include", segments[1]);
                        }
                        if (segments[0].Equals("exclude", StringComparison.OrdinalIgnoreCase))
                        {
                            DictUtils.AddEntryToList(listSettings, "exclude", segments[1]);
                        }
                    }
                }
            }
        }
        public string[] GetList(string settingName) => listSettings.TryGetValue(settingName, out var list) ? list.ToArray() : new string[] { };
    }
}