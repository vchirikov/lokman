using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Bullseye;
using CliWrap;
using CliWrap.Buffered;
using Crayon;
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
            Console.OutputEncoding = Encoding.UTF8;
            SetEnvVariables();
            PrintHeader();

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
                ?? throw new ExceptionWithoutStack(new FileNotFoundException("'dotnet' command isn't found. Try to set DOTNET_ROOT variable."));

            Target("proto", async () => {
                var protoc = TryFindCommandPath("protoc")
                    ?? throw new ExceptionWithoutStack(new FileNotFoundException("'protoc' command isn't found. " +
                        "Install and put into PATH env varibale protoc from https://github.com/protocolbuffers/protobuf/releases"));

                var protocGrpc = TryFindCommandPath("protoc-gen-grpc-web")
                    ?? throw new ExceptionWithoutStack(new FileNotFoundException("'protoc-gen-grpc-web' protoc plugin isn't found. " +
                        "Install and put into PATH env varibale from https://github.com/grpc/grpc-web#code-generator-plugin.\n" +
                        "Don't forgot rename file to 'protoc-gen-grpc-web' or 'protoc-gen-grpc-web.exe'"));


                var path = Path.GetFullPath(Path.Combine("src", "Protos"));
                var outputPath = Path.Combine("src", "Lokman.Server", "ClientApp", "src", "apis");

                if (!Directory.Exists(outputPath))
                    Directory.CreateDirectory(outputPath);

                foreach (var filePath in Directory.EnumerateFiles(path, "*.proto", SearchOption.AllDirectories))
                {
                    /// <see href="https://github.com/grpc/grpc-web#typescript-support" />
                    await Cli.Wrap(protoc).WithArguments($"{filePath} " +
                            $"--proto_path={path} " +
                            $"--error_format=msvs " +
                            $"--js_out=import_style=commonjs:{outputPath} " +
                            $"--grpc-web_out=import_style=typescript,mode=grpcwebtext:{outputPath}  " +
                            $"--plugin=protoc-gen-grpc-web={protocGrpc}")
                        .ToConsole(prefix: "protoc: ".Cyan())
                        .ExecuteAsync(cancellationToken).Task.ConfigureAwait(false);
                }

            });

            Target("watch", async () => {
                var node = TryFindCommandPath("node")
                    ?? throw new ExceptionWithoutStack(new FileNotFoundException("'node' command isn't found. Install nodejs from https://nodejs.org/"));

                // if one of process exits then close another on Cancel()
                var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                try
                {
                    var dotnetWatch = Cli.Wrap(dotnet).WithArguments($"watch --project {Path.Combine("src", "Lokman.Server")} run -- -c DEBUG")
                        .WithEnvironmentVariables(new Dictionary<string, string>() { ["ASPNETCORE_ENVIRONMENT"] = "Development" })
                        .ToConsole(prefix: "dotnet: ".Green())
                        .ExecuteAsync(cts.Token);

                    // 'npm start' runs npm-cli.js with react-app-rewired and for some reason
                    // Process.Kill(entireProcessTree: true) doesn't work well with it and inner process doesn't close
                    // this is workaround for this.
                    var webpackDevServer = Cli.Wrap(node)
                        .WithArguments(Path.Combine("node_modules", "react-app-rewired", "bin", "index.js") + " start")
                        .WithWorkingDirectory(Path.Combine("src", "Lokman.Server", "ClientApp"))
                        .WithEnvironmentVariables(new Dictionary<string, string>() { ["CI"] = "true" })
                        .ToConsole("webpack: ".Blue())
                        .ExecuteAsync(cts.Token);

                    await Task.WhenAny(dotnetWatch.Task, webpackDevServer.Task).ConfigureAwait(false);
                }
                finally
                {
                    if (!cts.IsCancellationRequested)
                        cts.Cancel();
                    cts.Dispose();
                }
                Console.WriteLine("The watching is over".BrightYellow());
            });

            Target("restore-tools", async () => {
                await Cli.Wrap(dotnet).WithArguments($"tool restore --ignore-failed-sources")
                   .ToConsole()
                   .ExecuteAsync().Task.ConfigureAwait(false);
            });

            Target("restore", async () => {
                var isPublicRelease = bool.Parse(Environment.GetEnvironmentVariable("NBGV_PublicRelease") ?? "false");
                var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                try
                {
                    var dotnetRestore = Cli.Wrap(dotnet).WithArguments($"msbuild -noLogo " +
                        "-t:Restore " +
                        "-p:RestoreForce=true " +
                        "-p:RestoreIgnoreFailedSources=True " +
                        $"-p:Configuration={configuration} " +
                        // for Nerdbank.GitVersioning
                        $"-p:PublicRelease={isPublicRelease} "
                        ).ToConsole("dotnet restore: ".Green())
                        .ExecuteAsync(cts.Token).Task;

                    var npmRestore = WrapNpmWithArguments("install --no-fund --progress false --loglevel error")
                        .WithWorkingDirectory(Path.Combine("src", "Lokman.Server", "ClientApp"))
                        .ToConsole("npm install: ".Blue())
                        .ExecuteAsync(cts.Token).Task;

                    await Task.WhenAll(dotnetRestore, npmRestore).ConfigureAwait(false);
                }
                finally
                {
                    cts.Cancel();
                    cts.Dispose();
                }
            });

            Target("build", async () => {
                await Cli.Wrap(dotnet).WithArguments($"build -noLogo -c {configuration}")
                    .ToConsole()
                    .ExecuteAsync(cancellationToken).Task.ConfigureAwait(false);
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
                            Directory.Delete(Path.GetDirectoryName(paths[0])!, true);
                        }
                    }
                }
            });

            Target("default", DependsOn("build"));

            try
            {
                /// <see cref="RunTargetsAndExitAsync"/> will hang Target on ctrl+c
                await RunTargetsWithoutExitingAsync(arguments, options, ex => ex is OperationCanceledException || ex is ExceptionWithoutStack).ConfigureAwait(false);
            }
            catch (TargetFailedException targetException)
            {
                if (targetException.InnerException is OperationCanceledException || targetException.InnerException is ExceptionWithoutStack)
                {
                    Console.WriteLine(targetException.Message.Red());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unhandled exception: {ex}".Red());
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

            static void PrintHeader()
            {

                const string header = "|   _  |  ._ _   _. ._  \n|_ (_) |< | | | (_| | |";
                const double freq = 0.1;
                var rainbow = new Rainbow(freq);
                for (var i = 0; i < header.Length; i++)
                {
                    Console.Write(rainbow.Next().Text(header[i].ToString()));
                    if (header[i] == '\n')
                        rainbow = new Rainbow(freq);
                }

                Console.Write("\n");
            }
        }

        /// <summary>
        /// Returns full path for short commands like "npm" (on windows it will be 'C:\Program Files\nodejs\npm.cmd' for example)
        /// or null if full path not found
        /// </summary>
        internal static string? TryFindCommandPath(string cmd)
        {
            if (File.Exists(cmd))
            {
                return Path.GetFullPath(cmd);
            }

            var values = Environment.GetEnvironmentVariable("PATH");
            if (values == null)
                return null;

            var isWindows = Environment.OSVersion.Platform != PlatformID.Unix;

            foreach (var path in values.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                var fullPath = Path.Combine(path, cmd);
                if (isWindows)
                {
                    if (File.Exists(fullPath + ".exe"))
                        return fullPath + ".exe";
                    else if (File.Exists(fullPath + ".cmd"))
                        return fullPath + ".cmd";
                    else if (File.Exists(fullPath + ".bat"))
                        return fullPath + ".bat";
                }
                else
                {
                    if (File.Exists(fullPath + ".sh"))
                        return fullPath + ".sh";
                }
                if (File.Exists(fullPath))
                    return fullPath;
            }
            return null;
        }

        private static Command WrapNpmWithArguments(string args)
        {
            // On Windows, the node executable is a .cmd file, so it can't be executed directly
            // (except with UseShellExecute=true, but that's no good, because it prevents capturing stdio).
            // So we need to invoke it via "cmd /c".
            var isWindows = false;
            //var isWindows = Environment.OSVersion.Platform != PlatformID.Unix;
            var cmdPath = (isWindows ? TryFindCommandPath("cmd") : TryFindCommandPath("npm"))
                ?? throw new ExceptionWithoutStack(new FileNotFoundException("'npm' command isn't found."));

            var cmdArgs = isWindows
                ? $"/C /D /U \"npm {args.Replace("\\", "\\\\").Replace("\"", "\\\"")}\""
                : args;
            // https://github.com/Tyrrrz/CliWrap/issues/37
            // can't work with it.
            var cmd = Cli.Wrap(cmdPath).WithArguments(cmdArgs);
            return cmd;
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

    internal class ExceptionWithoutStack : Exception
    {
        public ExceptionWithoutStack() { }

        public ExceptionWithoutStack(string? message) : base(message)
        {
        }

        public ExceptionWithoutStack(Exception innerException) : base(innerException.Message, innerException)
        {
        }

        public ExceptionWithoutStack(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected ExceptionWithoutStack(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}

