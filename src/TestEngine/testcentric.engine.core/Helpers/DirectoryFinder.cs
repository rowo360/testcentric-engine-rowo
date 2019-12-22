// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric Engine contributors.
// Licensed under the MIT License. See LICENSE.txt in root directory.
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Common;

namespace NUnit.Engine.Helpers
{
    /// <summary>
    /// DirectoryFinder is a utility class used for extended wildcard
    /// selection of directories and files. It's less than a full-fledged
    /// Linux-style globbing utility and more than standard wildcard use.
    /// </summary>
    public static class DirectoryFinder
    {
        /// <summary>
        /// Get a list of diretories matching and extended wildcard pattern.
        /// Each path component may have wildcard characters and a component
        /// of "**" may be used to represent all directories, recursively.
        /// </summary>
        /// <param name="baseDir">A DirectoryInfo from which the matching starts</param>
        /// <param name="pattern">The pattern to match</param>
        /// <returns>A list of DirectoryInfos</returns>
        public static IList<DirectoryInfo> GetDirectories(DirectoryInfo baseDir, string pattern)
        {
            Guard.ArgumentNotNullOrEmpty(pattern, "pattern");

            if (Path.DirectorySeparatorChar == '\\')
                pattern = pattern.Replace(Path.DirectorySeparatorChar, '/');

            var dirList = new List<DirectoryInfo>();
            dirList.Add(baseDir);

            while (pattern.Length > 0)
            {
                string range;
                int sep = pattern.IndexOf('/');

                if (sep >= 0)
                {
                    range = pattern.Substring(0, sep);
                    pattern = pattern.Substring(sep + 1);
                }
                else
                {
                    range = pattern;
                    pattern = "";
                }

                if (range == "." || range == "")
                    continue;

                dirList = ExpandOneStep(dirList, range);
            }

            return dirList;
        }

        /// <summary>
        /// Get files using an extended pattern with the option of wildcard 
        /// characters in each path component.
        /// </summary>
        /// <param name="baseDir">A DirectoryInfo from which the matching starts</param>
        /// <param name="pattern">The pattern to match</param>
        /// <returns>A list of FileInfos</returns>
        public static IList<FileInfo> GetFiles(DirectoryInfo baseDir, string pattern)
        {
            // If there is no directory path in pattern, delegate to DirectoryInfo
            int lastSep = pattern.LastIndexOf('/');
            if (lastSep < 0) // Simple file name entry, no path
                return baseDir.GetFiles(pattern);

            // Otherwise split pattern into two parts around last separator
            var pattern1 = pattern.Substring(0, lastSep);
            var pattern2 = pattern.Substring(lastSep + 1);

            var fileList = new List<FileInfo>();

            foreach (var dir in DirectoryFinder.GetDirectories(baseDir, pattern1))
                fileList.AddRange(dir.GetFiles(pattern2));

            return fileList;
        }

        public static DirectoryInfo GetPackageDirectory(DirectoryInfo startDir)
        {
            var dir = new DirectoryInfo(startDir.FullName).Parent;

            while (dir != null)
            {
                string tryPath = Path.Combine(dir.FullName, "packages");
                if (Directory.Exists(tryPath))
                    return new DirectoryInfo(tryPath);

                dir = dir.Parent;
            }

            return null;
        }

        private static List<DirectoryInfo> ExpandOneStep(IList<DirectoryInfo> dirList, string pattern)
        {
            var newList = new List<DirectoryInfo>();

            foreach (var dir in dirList)
            {
                if (pattern == "." || pattern == "")
                    newList.Add(dir);
                else if (pattern == "..")
                {
                    if (dir.Parent != null)
                        newList.Add(dir.Parent);
                }
                else if (pattern == "**")
                {
                    // ** means zero or more intervening directories, so we
                    // add the directory itself to start out.
                    newList.Add(dir);
                    var subDirs = dir.GetDirectories("*", SearchOption.AllDirectories);
                    if (subDirs.Length > 0) newList.AddRange(subDirs);
                }
                else
                {
                    var subDirs = dir.GetDirectories(pattern);
                    if (subDirs.Length > 0) newList.AddRange(subDirs);
                }
            }

            return newList;
        }
    }
}
