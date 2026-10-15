using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Snitch.Analysis
{
    internal sealed class ProjectFileReporter
    {
        public void WriteToFile(
            IReadOnlyList<ProjectAnalyzerResult> analyzerResults,
            string outputFileName,
            bool includePreReleases)
        {
            if (analyzerResults == null)
            {
                throw new ArgumentNullException(nameof(analyzerResults));
            }

            if (string.IsNullOrWhiteSpace(outputFileName))
            {
                throw new ArgumentException("An output filename is required.", nameof(outputFileName));
            }

            var results = analyzerResults
                .Where(x => x.CanBeRemoved.Count > 0 || x.MightBeRemoved.Count > 0 || (includePreReleases && x.HasPreReleases))
                .Select(x => new
                {
                    x.Project,
                    CanBeRemoved = x.CanBeRemoved.Select(ToPackage),
                    MightBeRemoved = x.MightBeRemoved.Select(y => new
                    {
                        PackageName = y.Package.Name,
                        PackageVersion = y.Package.GetVersionString(),
                        ReferencedBy = y.Original.Project.Name,
                        ReferencePackageVersion = y.Original.Package.GetVersionString(),
                    }),
                    PreRelease = includePreReleases
                        ? x.PreReleasePackages.Select(y => new
                        {
                            PackageName = y.Name,
                            PackageVersion = y.GetVersionString(),
                        })
                        : null,
                });

            using var stream = File.Create(outputFileName);
            JsonSerializer.Serialize(stream, results, new JsonSerializerOptions { WriteIndented = true });
        }

        private static object ToPackage(PackageToRemove package)
        {
            return new
            {
                PackageName = package.Package.Name,
                PackageVersion = package.Package.GetVersionString(),
                ReferencedBy = package.Original.Project.Name,
            };
        }
    }
}