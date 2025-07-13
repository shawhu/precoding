using System.Diagnostics;
using System.Reflection;
using System.Text;
using TextCopy;


namespace PreCoding
{
    class Program
    {
        static void Main(string[] args)
        {
            // Get the path to the running EXE
            var metadata = GetExeMetadata();
            Console.WriteLine($"PreCoding Tool [Version: {metadata.appVersion}]");
            Console.WriteLine($"Author:  {metadata.companyName}");
            Console.WriteLine($"License: GPLv3");
            Console.WriteLine($"Build:   {metadata.lastWriteTime}");
            Console.WriteLine($"Ensure your source code files are UTF-8 encoded.\n");

            string targetDirectory = Directory.GetCurrentDirectory();
            //string targetDirectory = @"C:\Users\shawhu\ProjectC\heheng\hehengclient";

            bool fileListMode = (args.Length > 0 && args[0].Equals("-files", StringComparison.OrdinalIgnoreCase));
            bool helpMode = (args.Length > 0 && args[0].Equals("-help", StringComparison.OrdinalIgnoreCase));
            string[] fileList = [];

            if (helpMode)
            {
                Console.WriteLine("Usages examples:");
                Console.WriteLine("1. Specify file search patterns:");
                Console.WriteLine("precoding.exe -files file1.cs file2.json ...");
                Console.WriteLine();
                Console.WriteLine("Specify targetDir");
                Console.WriteLine("precoding.exe <targetDir>");
                Console.WriteLine();
                Console.WriteLine("it will search the currentDir with default search patterns");
                Console.WriteLine("precoding.exe");
                Console.WriteLine();
                return;
            }
            if (fileListMode)
            {
                // ADDED: File list mode, get list of files after the -files argument
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage: precoding.exe -files file1.cs file2.json ...");
                    return;
                }
                fileList = args.Skip(1).ToArray(); // CORRECT: this gets all arguments after -files
            }
            else if (args.Length > 0)
            {
                targetDirectory = args[0];
            }

            Console.WriteLine("========== File Aggregation Started ==========");
            if (!Directory.Exists(targetDirectory))
            {
                Console.WriteLine($"[ERROR] Target directory does not exist: {targetDirectory}");

                return;
            }

            // Folders to ignore (case-insensitive)
            var ignoredFolderNames = new HashSet<string>(new[] {
                "node_modules",
                "Migrations",
                "obj",
                "app-example",
                "staticdata",
                "docs"
                }, StringComparer.OrdinalIgnoreCase);

            // File patterns to search
            string[] searchPatterns;
            if (fileListMode)
            {
                // CHANGED: Use exact filenames as searchPatterns
                searchPatterns = fileList;
            }
            else
            {
                searchPatterns = new[] {
                    "*.cs",
                    "*.tsx",
                    "*.ts",
                    "package.json",
                    "*.csproj"
                };
            }

            // Printout the criteria
            Console.WriteLine($"Mode:            {(fileListMode ? "File List" : "Pattern Search")}");
            Console.WriteLine($"Search Targets:  {string.Join(", ", searchPatterns)}");
            Console.WriteLine($"Ignored Folders: {string.Join(", ", ignoredFolderNames)}");

            // Find all files, skipping hidden, ignored, and dot-prefixed folders/files
            var files = GetFilesSkippingFolders(targetDirectory, searchPatterns, ignoredFolderNames)
                .Distinct()
                .OrderBy(f => f)
                .ToList();

            if (!files.Any())
            {
                Console.WriteLine("No matching source files found. Exiting.");
                Console.WriteLine("========== Aggregation Complete ==============");

                return;
            }

            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();
            string templatePath = Path.Combine(AppContext.BaseDirectory ?? Directory.GetCurrentDirectory(), "prompt_header.md");
            if (File.Exists(templatePath))
            {
                output.AppendLine(File.ReadAllText(templatePath));
            }
            else
            {
                output.AppendLine("## Instructions:");
                output.AppendLine("1. **Code-First Replies:** Always respond with a code block containing the exact code (method, function, or class) to be replaced or inserted.");
                output.AppendLine("   - For long files, include only the full method/function/class that is being changed.");
                output.AppendLine("2. **Clear Change Comments:** Clearly mark your changes using `// CHANGED`, `// ADDED`, `// REMOVED`, etc.");
                output.AppendLine("3. **No Redundant Suggestions:** Double-check my code and **do not suggest fixes for issues already handled**.");
                output.AppendLine("4. **Prefer Inline Solutions:** Use concise, inline code when possible. Avoid multiple lines for changes that can be made in one.");
                output.AppendLine("5. **Be Accurate, Direct, and Minimal:**");
                output.AppendLine("   - Do not add extra features or explanations unless requested.");
                output.AppendLine("   - Provide only code and necessary comments.");
                output.AppendLine("6. **Reference the Provided Codebase:** Use the code files and their filenames below as context for your responses.");
                output.AppendLine();
                output.AppendLine("## Below are all the source code files for reference:");
            }

            foreach (var file in files)
            {
                try
                {
                    string filetype = GetFileType(file);
                    string relativePath = Path.GetRelativePath(targetDirectory, file);
                    output.AppendLine($"### --- FILENAME: {relativePath} ---");
                    output.AppendLine($"```{filetype}");
                    string content = File.ReadAllText(file, Encoding.UTF8);
                    output.AppendLine(content);
                    output.AppendLine("```");
                }
                catch (Exception ex)
                {
                    error.AppendLine($"ERROR reading {file}: {ex.Message} ---");
                    error.AppendLine();
                }
            }

            // Output file
            string outputStr = output.ToString();
            string outputFile = Path.Combine(targetDirectory, "AllSourceFiles.txt");
            File.WriteAllText(outputFile, outputStr, Encoding.UTF8);

            // Get file size in MB
            FileInfo fi = new FileInfo(outputFile);
            double sizeInMb = fi.Length / (1024.0 * 1024.0);
            Console.WriteLine($"Completed! Aggregated {files.Count} files.");
            Console.WriteLine($"Output written to: {outputFile} [{sizeInMb:F2} MB]");
            if (sizeInMb <= 1)
                Console.WriteLine("Output copied to clipboard.");
            else
                Console.WriteLine("Output too large for clipboard; only written to file.");
            Console.WriteLine("========== Aggregation Complete ==============");

            // Copy to clipboard
            if (sizeInMb <= 1) ClipboardService.SetText(outputStr);
            else Console.WriteLine($"Output file is too big, skipping automatic clipboard copying.");

            //if error
            if (!string.IsNullOrWhiteSpace(error.ToString()))
                Console.WriteLine("[WARN] Some files could not be read:\n" + error.ToString());


        }

        static IEnumerable<string> GetFilesSkippingFolders(
            string rootPath,
            string[] searchPatterns,
            HashSet<string> ignoredFolderNames
        )
        {
            var dirs = new Stack<DirectoryInfo>();
            dirs.Push(new DirectoryInfo(rootPath));

            while (dirs.Count > 0)
            {
                DirectoryInfo currentDir = dirs.Pop();

                // Skip hidden directories
                if ((currentDir.Attributes & FileAttributes.Hidden) != 0)
                    continue;

                // Skip ignored directories by name (e.g., node_modules)
                if (ignoredFolderNames.Contains(currentDir.Name))
                    continue;

                // Skip directories that start with a dot (.)
                if (currentDir.Name.StartsWith("."))
                    continue;

                // Get files for each pattern
                foreach (var pattern in searchPatterns)
                {
                    FileInfo[] files = [];
                    try
                    {
                        files = currentDir.GetFiles(pattern, SearchOption.TopDirectoryOnly);
                    }
                    catch
                    {
                        // Could not access this directory, skip
                        continue;
                    }

                    foreach (var file in files)
                    {
                        // Skip dot-prefixed files
                        if (file.Name.StartsWith("."))
                            continue;

                        yield return file.FullName;
                    }
                }

                // Push subdirectories that are not hidden, not ignored, and not dot-starting
                DirectoryInfo[] subDirs = [];
                try
                {
                    subDirs = currentDir.GetDirectories();
                }
                catch
                {
                    continue;
                }

                foreach (var subDir in subDirs)
                {
                    if ((subDir.Attributes & FileAttributes.Hidden) == 0 &&
                        !ignoredFolderNames.Contains(subDir.Name) &&
                        !subDir.Name.StartsWith("."))
                    {
                        dirs.Push(subDir);
                    }
                }
            }
        }

        static string GetFileType(string file)
        {
            var extension = Path.GetExtension(file).ToLowerInvariant();
            var typeMap = new Dictionary<string, string>
            {   { ".bat", "bat" },
                { ".c", "c" },
                { ".cc", "cpp" },
                { ".cmd", "bat" },
                { ".cpp", "cpp" },
                { ".cs", "csharp" },
                { ".css", "css" },
                { ".dart", "dart" },
                { ".diff", "diff" },
                { ".env", "env" },
                { ".go", "go" },
                { ".graphql", "graphql" },
                { ".gql", "graphql" },
                { ".h", "cpp" },
                { ".htm", "html" },
                { ".html", "html" },
                { ".ini", "ini" },
                { ".java", "java" },
                { ".jl", "julia" },
                { ".js", "javascript" },
                { ".json", "json" },
                { ".kt", "kotlin" },
                { ".kts", "kotlin" },
                { ".latex", "latex" },
                { ".lisp", "lisp" },
                { ".lsp", "lisp" },
                { ".lua", "lua" },
                { ".md", "markdown" },
                { ".mjs", "javascript" },
                { ".php", "php" },
                { ".pl", "perl" },
                { ".pm", "perl" },
                { ".properties", "properties" },
                { ".ps1", "powershell" },
                { ".psm1", "powershell" },
                { ".py", "python" },
                { ".pyw", "python" },
                { ".r", "r" },
                { ".rb", "ruby" },
                { ".rs", "rust" },
                { ".scala", "scala" },
                { ".sh", "bash" },
                { ".sql", "sql" },
                { ".swift", "swift" },
                { ".tex", "latex" },
                { ".toml", "toml" },
                { ".ts", "typescript" },
                { ".tsx", "typescript" },
                { ".txt", "plaintext" },
                { ".xml", "xml" },
                { ".yml", "yaml" },
                { ".yaml", "yaml" }
            };

            return typeMap.TryGetValue(extension, out var type) ? type : "";
        }

        static (string exePath, string appVersion, string companyName, DateTime lastWriteTime) GetExeMetadata()
        {
            string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
            DateTime lastWriteTime = DateTime.MinValue;
            string appVersion = "Unknown";
            string companyName = "Unknown";

            // Get assembly info from the file itself (EXE)
            if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
            {
                try
                {
                    var assembly = Assembly.LoadFrom(exePath);
                    appVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown";
                    companyName = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "Unknown";
                }
                catch
                {
                    // fallback: try FileVersionInfo
                    var fvi = FileVersionInfo.GetVersionInfo(exePath);
                    appVersion = fvi.ProductVersion ?? "Unknown";
                    companyName = fvi.CompanyName ?? "Unknown";
                }
                lastWriteTime = File.GetLastWriteTime(exePath);
            }
            return (exePath, appVersion, companyName, lastWriteTime);
        }
    }
}