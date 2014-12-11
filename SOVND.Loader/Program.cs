using System.Diagnostics;
using System.IO;
using System.Linq;
using NuGet;

// SOVND.Loader: The simplest possible autoupdater
//
// Checks a NuGet feed for new packages
// Uses Nuget lib to manage packages
// Runs exe from latest package

namespace SOVND.Loader
{
    public class Program
    {
        static void Main(string[] args)
        {
            string installpath = "packages";
            string packageID = "SOVND.Client";
            string executable = "SOVND.Client.exe";

            IPackageRepository repo = PackageRepositoryFactory.Default.CreateRepository("https://www.myget.org/F/sovnd/");

            var package = repo.FindPackagesById(packageID)
                              .FirstOrDefault(x => x.IsLatestVersion);

            PackageManager packageManager = new PackageManager(repo, installpath);

            packageManager.UpdatePackage(package, true, true);

            var exe = Path.Combine(installpath, packageID + "." + package.Version.ToString(), "lib", executable);
            Process.Start(exe);
        }
    }
}
