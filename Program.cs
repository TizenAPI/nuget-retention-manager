using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Packaging.Core;
using NuGet.Configuration;
using NuGet.Versioning;

using Newtonsoft.Json;

namespace RetentionManager
{
    class Manager
    {
        ILogger _logger = new ConsoleLogger();
        SourceRepository _repository;

        List<PackageIdentity> _allPackages = new List<PackageIdentity>();
        List<PackageIdentity> _packagesToRemove = new List<PackageIdentity>();

        public Manager(string source)
        {
            PackageSource packageSource = new PackageSource(source);
            List<Lazy<INuGetResourceProvider>> providers = new List<Lazy<INuGetResourceProvider>>();
            providers.AddRange(Repository.Provider.GetCoreV3());
            _repository = new SourceRepository(packageSource, providers);
        }

        public async Task LoadAllPackages()
        {
            var searchFilter = new SearchFilter(includePrerelease: true);
            var packageSearchResource = _repository.GetResource<PackageSearchResource>();
            var searchResult = await packageSearchResource.SearchAsync(string.Empty, searchFilter, 0, 1000, _logger, CancellationToken.None);

            foreach (var pkg in searchResult)
            {

                var versions = await pkg.GetVersionsAsync();
                foreach (VersionInfo version in versions)
                {
                    _allPackages.Add(new PackageIdentity(pkg.Title, version.Version));
                }
            }
        }

        public void CalculatePackagesToRemove(RetentionRule rule)
        {
            // Console.WriteLine($"Rule : Id={rule.Id}, Version={rule.Version}, Stable={rule.Stable}, Prerelease={rule.Prerelease}");
            List<string> versions = new List<string>();
            if (rule.Version != null) {
                versions.Add(rule.Version);
            }
            if (rule.Versions != null) {
                versions.AddRange(rule.Versions);
            }

            foreach (var version in versions) {
                VersionRange range = VersionRange.Parse(version);

                IEnumerable<PackageIdentity> stablePackages =
                    from pkg in _allPackages
                    where pkg.Id == rule.Id && pkg.Version.IsPrerelease == false && range.Satisfies(pkg.Version)
                    orderby pkg.Version descending
                    select pkg;

                IEnumerable<PackageIdentity> prereleasePackages =
                    from pkg in _allPackages
                    where pkg.Id == rule.Id && pkg.Version.IsPrerelease == true && range.Satisfies(pkg.Version)
                    orderby pkg.Version descending
                    select pkg;

                int cnt;

                cnt = 0;
                foreach (var pkg in stablePackages)
                {
                    cnt++;
                    if (cnt > rule.Stable)
                        _packagesToRemove.Add(pkg);
                }

                cnt = 0;
                foreach (var pkg in prereleasePackages)
                {
                    cnt++;
                    if (cnt > rule.Prerelease)
                        _packagesToRemove.Add(pkg);
                }
            }
        }

        public async Task RemovePackage(string apiKey)
        {
            var packageUpdateResource = _repository.GetResource<PackageUpdateResource>();
            foreach (var pkg in _packagesToRemove)
            {
                Console.WriteLine($"Delete.. {pkg.Id} {pkg.Version.ToNormalizedString()}");
#if !TESTONLY
                await packageUpdateResource.Delete(pkg.Id, $"{pkg.Version.ToNormalizedString()}?hardDelete=true", endpoint => apiKey, desc => true, _logger);
#endif
            }
        }

        static void Main(string[] args)
        {
            // Arguments
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: PROG [configfile] [apikey]");
                return;
            }

            string configFilePath = args[0];
            string apiKey = args[1];

            // Read json configuration file
            Configuration config = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(configFilePath));

            // Setup Manager
            Manager manager = new Manager(config.Source);
            manager.LoadAllPackages().Wait();

            // Calculate for gathering packages to remove
            foreach (var rule in config.Rules)
            {
                manager.CalculatePackagesToRemove(rule);
            }

            // Remove old packages
            manager.RemovePackage(apiKey).Wait();
        }
    }
}
