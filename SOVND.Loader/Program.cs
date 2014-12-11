using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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

            var fullinstallpath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), installpath);
            PackageManager packageManager = new PackageManager(repo, fullinstallpath);

            var exeFile = Path.Combine(fullinstallpath, packageID + "." + package.Version, "lib", executable);
            if (!File.Exists(exeFile))
                packageManager.UpdatePackage(package, true, true);

            var process = new Process();
            process.StartInfo.FileName = Path.GetFullPath(exeFile);
            process.StartInfo.WorkingDirectory = Path.GetFullPath(Path.GetDirectoryName(exeFile));
            process.Start();
        }
    }
}
