using CommandLine;
using GoogleDriveTools;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ConsoleUploader
{
    public class CliOptions
    {
        [Option('s', "source", HelpText = "Source directory", Required = true)]
        public string SourceDirectory { get; set; }

        [Option('p', "pattern", HelpText = "Search pattern", Default = "*.*")]
        public string SearchPattern { get; set; }

        [Option('d', "parentDirectory", HelpText = "Parent directory id", Required = true)]
        public string ParentDirectory { get; set; }

        [Option('q', "plainText", HelpText = "Create plain text document")]
        public bool PlainText { get; set; }

        [Option('t', "text", HelpText = "Plain text to create")]
        public string Text { get; set; }

        [Option('n', "plainTextName", HelpText = "Plain text document name", Default = "readme.txt")]
        public string PlainTextDocumentName { get; set; }

        [Option('x', "plainTextParentDirectory", HelpText = "Directory for plain text document")]
        public string PlainTextParentDir { get; set; }
    }

    public class Application
    {
        private readonly ILogger<Application> _logger;
        private readonly GoogleDrive _googleDrive;
        private readonly static ConcurrentDictionary<string, string> _extensionMimeTypeCache = new();
        public Application(ILogger<Application> logger, GoogleDrive googleDrive)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _googleDrive = googleDrive ?? throw new ArgumentNullException(nameof(googleDrive));
        }

        public async Task Run(string[] args)
        {
            await Parser.Default.ParseArguments<CliOptions>(args).WithParsedAsync(async options =>
            {
                if (options.PlainText)
                {
                    _logger.LogInformation("Creating text document");
                    var filename = options.PlainTextDocumentName;
                    var content = options.Text;
                    var parent = options.PlainTextParentDir ?? options.ParentDirectory;
                    MimeTypes.TryGetMimeType(filename, out var mimeType);
                    mimeType ??= "text/plain";
                    using var ms = new MemoryStream();
                    await using var writer = new StreamWriter(ms);
                    await writer.WriteAsync(content);
                    await writer.FlushAsync();
                    await foreach (var file in _googleDrive.GetFiles(parent))
                    {
                        if (file.Name == filename)
                        {
                            _logger.LogInformation("Delete existing {filename}", filename);
                            await _googleDrive.DeleteFile(file.Id);
                        }
                    }
                    await _googleDrive.UploadFile(ms, filename, mimeType, parent, string.Empty);
                }
                var files = Directory.EnumerateFiles(options.SourceDirectory, options.SearchPattern).ToList();

                foreach (var file in files)
                {
                    _logger.LogDebug("Processing file {file}", file);
                    var extension = Path.GetExtension(file);
                    var mimeType = string.Empty;
                    if (!_extensionMimeTypeCache.TryGetValue(extension, out mimeType))
                    {
                        MimeTypes.TryGetMimeType(file, out mimeType);
                    }
                    if (!string.IsNullOrWhiteSpace(mimeType))
                    {
                        _extensionMimeTypeCache.TryAdd(extension, mimeType);
                        _logger.LogDebug("Mime type for file {file} is {mimeType}", file, mimeType);
                        await using var fs = File.OpenRead(file);
                        await _googleDrive.UploadFile(fs, Path.GetFileName(file), mimeType, options.ParentDirectory, string.Empty);
                    } else
                    {
                        _logger.LogWarning("Unknown mime type for {file}", file);
                    }
                }
            });
        }
    }
}
