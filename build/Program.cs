using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Bullseye;
using CliWrap;
using CliWrap.Buffered;
using static Bullseye.Targets;

namespace Build
{
    // DragonFruit approach doesn't the best option here (for example currently it doesn't support aliases),
    // but it's clean and works, so I've decided to use it.
    internal static class Program
    {
        /// <summary>Build project for repository</summary>
        /// <param name="arguments">A list of targets to run or list.</param>
        /// <param name="clear">Clear the console before execution.</param>
        /// <param name="dryRun">Do a dry run without executing actions.</param>
        /// <param name="host">Force the mode for a specific host environment (normally auto-detected).</param>
        /// <param name="listDependencies">List all (or specified) targets and dependencies, then exit.</param>
        /// <param name="listInputs">List all (or specified) targets and inputs, then exit.</param>
        /// <param name="listTargets">List all (or specified) targets, then exit.</param>
        /// <param name="listTree">List all (or specified) targets and dependency trees, then exit.</param>
        /// <param name="noColor">Disable colored output.</param>
        /// <param name="parallel">Run targets in parallel.</param>
        /// <param name="skipDependencies">Do not run targets' dependencies.</param>
        /// <param name="verbose">Enable verbose output.</param>
        /// <param name="cancellationToken">The terminate program cancellation</param>
        /// <param name="configuration">The configuration for building</param>
        private static async Task Main(
            string[] arguments,
            bool clear,
            bool dryRun,
            Host host,
            bool listDependencies,
            bool listInputs,
            bool listTargets,
            bool listTree,
            bool noColor,
            bool parallel,
            bool skipDependencies,
            bool verbose,
            CancellationToken cancellationToken,
            // our options here
            string configuration = "Debug"
            )
        {
            SetEnvVariables();

            var options = new Options {
                Clear = clear,
                DryRun = dryRun,
                Host = host,
                ListDependencies = listDependencies,
                ListInputs = listInputs,
                ListTargets = listTargets,
                ListTree = listTree,
                NoColor = noColor,
                Parallel = parallel,
                SkipDependencies = skipDependencies,
                Verbose = verbose,
            };

            var dotnet = TryFindDotNetExePath()
                ?? throw new FileNotFoundException("'dotnet' command isn't found. Try to set DOTNET_ROOT variable.");

            Target("watch", async () => {
                var cmd = await Cli.Wrap(dotnet).WithArguments($"watch --project {Path.Combine("src", "Lokman.Server")} run -- -c DEBUG")
                    .WithEnvironmentVariables(new Dictionary<string, string>() { ["ASPNETCORE_ENVIRONMENT"] = "Development" })
                    .ToConsole()
                    .ExecuteBufferedAsync(cancellationToken).Task.ConfigureAwait(false);

            });

            Target("restore-tools", async () => {
                var cmd = await Cli.Wrap(dotnet).WithArguments($"tool restore --ignore-failed-sources").ToConsole()
                    .ExecuteBufferedAsync().Task.ConfigureAwait(false);
            });

            Target("restore", async () => {
                var isPublicRelease = bool.Parse(Environment.GetEnvironmentVariable("NBGV_PublicRelease") ?? "false");
                var cmd = await Cli.Wrap(dotnet).WithArguments($"msbuild -noLogo " +
                    "-t:Restore " +
                    "-p:RestoreForce=true " +
                    "-p:RestoreIgnoreFailedSources=True " +
                    $"-p:Configuration={configuration} " +
                    // for Nerdbank.GitVersioning
                    $"-p:PublicRelease={isPublicRelease} "
                    ).ToConsole()
                    .ExecuteBufferedAsync(cancellationToken).Task.ConfigureAwait(false);
            });

            Target("build", async () => {
                var cmd = await Cli.Wrap(dotnet).WithArguments($"build -noLogo -c {configuration}")
                    .ToConsole()
                    .ExecuteBufferedAsync(cancellationToken).Task.ConfigureAwait(false);
            });

            Target("coverage", async () => {
                var resultsDirectory = Path.GetFullPath(Path.Combine("artifacts", "tests", "output"));
                if (!Directory.Exists(resultsDirectory))
                    Directory.CreateDirectory(resultsDirectory);
                var cmd = await Cli.Wrap(dotnet)
                    .WithArguments($"test " +
                    "--nologo " +
                    "--no-restore " +
                    $"--collect:\"XPlat Code Coverage\" --results-directory {resultsDirectory} " +
                    $"--logger trx;LogFileName=\"{Path.Combine(resultsDirectory, "tests.trx").Replace("\"", "\\\"")}\" " +
                    $"-c {configuration} " +
                    "-- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=json,cobertura"
                    )
                    .ToConsole()
                    .ExecuteBufferedAsync(cancellationToken).Task.ConfigureAwait(false);

                MoveAttachmentsToResultsDirectory(resultsDirectory, cmd.StandardOutput);
                TryRemoveTestsOutputDirectories(resultsDirectory);

                // Removes all files in inner folders, workaround of https://github.com/microsoft/vstest/issues/2334
                static void TryRemoveTestsOutputDirectories(string resultsDirectory)
                {
                    foreach (var directory in Directory.EnumerateDirectories(resultsDirectory))
                    {
                        try
                        {
                            Directory.Delete(directory, recursive: true);
                        }
                        catch { }
                    }
                }

                // Removes guid from tests output path, workaround of https://github.com/microsoft/vstest/issues/2378
                static void MoveAttachmentsToResultsDirectory(string resultsDirectory, string output)
                {
                    var attachmentsRegex = new Regex($@"Attachments:(?<filepaths>(?<filepath>[\s]+[^\n]+{Regex.Escape(resultsDirectory)}[^\n]+[\n])+)", RegexOptions.Singleline | RegexOptions.CultureInvariant);
                    var match = attachmentsRegex.Match(output);
                    if (match.Success)
                    {
                        var regexPaths = match.Groups["filepaths"].Value.Trim('\n', ' ', '\t', '\r');
                        var paths = regexPaths.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
                        if (paths.Length > 0)
                        {
                            foreach (var path in paths)
                            {
                                File.Move(path, Path.Combine(resultsDirectory, Path.GetFileName(path)), overwrite: true);
                            }
                            Directory.Delete(Path.GetDirectoryName(paths[0]), true);
                        }
                    }
                }
            });

            Target("default", DependsOn("build"));

            try
            {
                /// <see cref="RunTargetsAndExitAsync"/> will hang on ctrl+c, idk why, but it's happend after close target
                await RunTargetsWithoutExitingAsync(arguments, options, ex => ex is OperationCanceledException).ConfigureAwait(false);
            }
            catch (TargetFailedException targetException)
            {
                if (targetException.InnerException is OperationCanceledException operationCanceledException)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(operationCanceledException.Message);
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Unhandled exception: {ex}");
                Console.ResetColor();
            }

            static void SetEnvVariables()
            {
                Environment.SetEnvironmentVariable("DOTNET_CLI_TELEMETRY_OPTOUT", "1");
                Environment.SetEnvironmentVariable("DOTNET_SVCUTIL_TELEMETRY_OPTOUT", "1");
                Environment.SetEnvironmentVariable("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "1");
                Environment.SetEnvironmentVariable("DOTNET_NOLOGO", "1");
                Environment.SetEnvironmentVariable("POWERSHELL_TELEMETRY_OPTOUT", "1");
                Environment.SetEnvironmentVariable("POWERSHELL_UPDATECHECK_OPTOUT", "1");
                Environment.SetEnvironmentVariable("DOTNET_CLI_UI_LANGUAGE", "en");
            }
        }

        private static string? TryFindDotNetExePath()
        {
            var dotnet = "dotnet";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                dotnet += ".exe";

            var mainModule = Process.GetCurrentProcess().MainModule;
            if (!string.IsNullOrEmpty(mainModule?.FileName) && Path.GetFileName(mainModule.FileName)!.Equals(dotnet, StringComparison.OrdinalIgnoreCase))
                return mainModule.FileName;

            var environmentVariable = Environment.GetEnvironmentVariable("DOTNET_ROOT");
            if (!string.IsNullOrEmpty(environmentVariable))
                return Path.Combine(environmentVariable, dotnet);

            var paths = Environment.GetEnvironmentVariable("PATH");
            if (paths == null)
                return null;

            foreach (var path in paths.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                var fullPath = Path.Combine(path, dotnet);
                if (File.Exists(fullPath))
                    return fullPath;
            }
            return null;
        }
    }
}

