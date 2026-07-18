using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace FileScanner
{
    public class FileEntry
    {
        public string Path { get; set; }
    }

    public class DirectoryEntry
    {
        public string Path { get; set; }
        public List<FileEntry> Files { get; set; }
        public List<DirectoryEntry> Directories { get; set; }
    }

    public class ScanResult
    {
        public int TotalFilesScanned { get; set; }
        public int TotalDirectoriesScanned { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public class FileScannerService
    {
        private CancellationTokenSource _cancellationTokenSource;

        public event Action<int> ProgressChanged;

        public ScanResult Scan(List<string> rootDirectories, CancellationToken cancellationToken)
        {
            var result = new ScanResult
            {
                TotalFilesScanned = 0,
                TotalDirectoriesScanned = 0,
                Duration = TimeSpan.Zero
            };

            _cancellationTokenSource = new CancellationTokenSource(cancellationToken);

            try
            {
                using (_cancellationTokenSource.Token.Register(() => throw new OperationCanceledException()))
                {
                    foreach (var directory in rootDirectories)
                    {
                        ScanDirectory(directory, result);
                    }
                }

                result.Duration = DateTime.Now - DateTime.UtcNow;
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation
            }

            return result;
        }

        private void ScanDirectory(string path, ScanResult result)
        {
            if (_cancellationTokenSource.Token.IsCancellationRequested) throw new OperationCanceledException();

            try
            {
                var directoryInfo = new DirectoryInfo(path);
                if (!directoryInfo.Exists) return;

                result.TotalDirectoriesScanned++;

                var directoryEntry = new DirectoryEntry { Path = path, Files = new List<FileEntry>(), Directories = new List<DirectoryEntry>() };

                foreach (var file in directoryInfo.GetFiles())
                {
                    result.TotalFilesScanned++;
                    directoryEntry.Files.Add(new FileEntry { Path = file.FullName });
                }

                foreach (var subdirectory in directoryInfo.GetDirectories())
                {
                    directoryEntry.Directories.Add(ScanSubdirectory(subdirectory, result));
                }

                // Simulate progress reporting
                ProgressChanged?.Invoke(result.TotalFilesScanned + result.TotalDirectoriesScanned);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scanning directory {path}: {ex.Message}");
            }
        }

        private DirectoryEntry ScanSubdirectory(DirectoryInfo subdirectory, ScanResult result)
        {
            var entry = new DirectoryEntry { Path = subdirectory.FullName, Files = new List<FileEntry>(), Directories = new List<DirectoryEntry>() };

            foreach (var file in subdirectory.GetFiles())
            {
                entry.Files.Add(new FileEntry { Path = file.FullName });
            }

            foreach (var subdir in subdirectory.GetDirectories())
            {
                entry.Directories.Add(ScanSubdirectory(subdir, result));
            }

            return entry;
        }
    }
}