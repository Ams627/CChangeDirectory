using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace CChangeDirectory
{
    public static class SubstMappingsHelper
    {
        /// <summary>
        /// Create a mapping like subst, but do not allowed subsequent mappings if one mapping already exists.
        /// If a mapping exists, we will abort and return ERROR_ALREADY_EXISTS (187) - see winerror.h
        /// </summary>
        /// <param name="driveLetter">single character drive letter - can be upper or lower case</param>
        /// <param name="path">the path to map</param>
        /// <returns>0 meaning success or otherwise the code return by GetLastError</returns>
        public static int CreateMapping(char driveLetter, string path, bool deleteIfExisting = false)
        {
            const int ERROR_ALREADY_EXISTS = 187;
            if (GetMapping(driveLetter, out var _) == 0)
            {
                if (!deleteIfExisting)
                {
                    return ERROR_ALREADY_EXISTS;
                }
                DeleteMapping(driveLetter);
            }
            var result = NativeMethods.DefineDosDevice(0, $"{driveLetter}:", path);
            var lastError = result != 0 ? 0 : Marshal.GetLastWin32Error();
            return lastError;
        }

        /// <summary>
        /// Get a list of unmapped drives
        /// </summary>
        /// <returns>a single string - each character is an available drive letter</returns>
        public static string GetAvailableDrives()
        {
            const int sz = 33000;
            var sbDrives = new StringBuilder();
            foreach (var drive in "abcdefghijklmnopqrstuvwxyz")
            {
                StringBuilder sb = new StringBuilder(sz);
                var queryResult = NativeMethods.QueryDosDevice($"{drive}:", sb, sz);
                if (queryResult == 0)
                {
                    sbDrives.Append(drive);
                }
            }
            return sbDrives.ToString();
        }

        /// <summary>
        /// Get a subst mapping for the drive.
        /// </summary>
        /// <param name="driveLetter">single character drive letter - can be upper or lower case</param>
        /// <param name="path">The mapped path if the function succeeds</param>
        /// <returns>0 meaning success or an error code returned by GetLastError</returns>
        public static int GetMapping(char driveLetter, out string path)
        {
            const int sz = 33000;
            var sbDrives = new StringBuilder();
            StringBuilder sb = new StringBuilder(sz);
            var result = NativeMethods.QueryDosDevice($"{driveLetter}:", sb, sz);
            var rawPath = sb.ToString();
            path = rawPath.StartsWith(@"\??\") ? rawPath.Substring(4) : rawPath;
            var lastError = result != 0 ? 0 : Marshal.GetLastWin32Error();
            return lastError;
        }

        /// <summary>
        /// Delete an existing mapping
        /// </summary>
        /// <param name="driveLetter">a single character drive letter. Can be upper or lower case</param>
        /// <returns>0 meaning success or an error code returned by GetLastError</returns>
        public static int DeleteMapping(char driveLetter)
        {
            var result = NativeMethods.DefineDosDevice((int)NativeMethods.DefineDosDeviceFlags.DDD_REMOVE_DEFINITION, $"{driveLetter}:", null);
            return result == 0 ? Marshal.GetLastWin32Error() : 0;
        }

        public static List<(char driveLetter, string path)> GetMappings()
        {
            var result = new List<(char driveLetter, string path)>();

            const int sz = 33000;
            var sbDrives = new StringBuilder();
            foreach (var drive in "abcdefghijklmnopqrstuvwxyz")
            {
                StringBuilder sb = new StringBuilder(sz);
                var queryResult = NativeMethods.QueryDosDevice($"{drive}:", sb, sz);
                if (queryResult != 0)
                {
                    var path = sb.ToString();
                    if (path.StartsWith(@"\??\"))
                    {
                        result.Add((drive, path.Substring(4)));
                    }
                }
            }
            return result;
        }
    }
}