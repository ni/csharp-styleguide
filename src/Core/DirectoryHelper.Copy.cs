using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NationalInstruments.Tools.Core
{
    public static partial class DirectoryHelper
    {
        private static ILogger Logger { get; } = ApplicationLogging.CreateLogger<EscapeKeyMonitor>();

        public static async Task<List<string>> CopyFastAsync(DirectoryInfo source, DirectoryInfo destination, bool overwrite, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var errors = new List<string>();

            if (source == null || !source.Exists)
            {
                return errors;
            }

            if (!destination.Exists)
            {
                destination.Create();
            }

            await CopyChildDirectoriesAsync(source, destination, overwrite, errors, cancellationToken).ConfigureAwait(false);
            await CopyFilesAsync(source, destination, overwrite, errors, cancellationToken).ConfigureAwait(false);

            return errors;
        }

        private static async Task CopyChildDirectoriesAsync(DirectoryInfo source, DirectoryInfo destination, bool overwrite, List<string> errors, CancellationToken cancellationToken)
        {
            var directories = source.EnumerateFileSystemInfos().Where(t => t is DirectoryInfo);

            foreach (var directory in directories)
            {
                var e = await CopyFastAsync(
                    directory as DirectoryInfo,
                    new DirectoryInfo(Path.Combine(destination.FullName, directory.Name)),
                    overwrite,
                    cancellationToken).ConfigureAwait(false);

                lock (errors)
                {
                    errors.AddRange(e);
                }
            }
        }

        private static async Task CopyFilesAsync(DirectoryInfo source, DirectoryInfo destination, bool overwrite, List<string> errors, CancellationToken cancellationToken)
        {
            var files = source.EnumerateFileSystemInfos().Where(t => t is FileInfo);

            var tasks = files.Select(
                x =>
                {
                    return Task.Run(
                        () =>
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            Logger.LogInformation("Copying: " + x.FullName + " to " + destination.FullName);

                            try
                            {
                                var destinationPathString = Path.Combine(destination.FullName, x.Name);

                                if (overwrite)
                                {
                                    var destinationPath = new FileInfo(destinationPathString);

                                    if (destinationPath.Exists)
                                    {
                                        destinationPath.Attributes = FileAttributes.Normal;
                                    }
                                }

                                ((FileInfo)x).CopyTo(destinationPathString, overwrite);
                            }
                            catch (Exception exception)
                            {
                                lock (errors)
                                {
                                    errors.Add(exception.Message);
                                }
                            }
                        },
                        cancellationToken);
                });

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}
