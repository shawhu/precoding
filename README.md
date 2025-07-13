# PreCoding

**PreCoding** is a command-line tool for aggregating and exporting the contents of source files in a directory tree into a single Markdown-formatted text file, ideal for context sharing with LLMs or for code review/prep. It supports both pattern-based and explicit file-list selection, skips common build/ignored folders, and can copy the output to your clipboard (if small enough).

---

## Features

- **Recursively collects** source files by pattern or explicit list, skipping ignored folders and hidden files.
- **Aggregates all files into `AllSourceFiles.txt`** with proper Markdown code fencing and file headers.
- **Customizable search:**
  - Default patterns: `.cs`, `.tsx`, `.ts`, `package.json`, `.csproj`
  - Or list files explicitly: `-files file1.cs file2.json ...`
- **Skips folders** like `node_modules`, `obj`, `Migrations`, and dot-prefixed or hidden directories.
- **UTF-8 safe**: Reads files as UTF-8, warns if files can’t be read.
- **Clipboard integration**: If the output is ≤1MB, it’s automatically copied to your clipboard.

---

## Usage

```
precoding.exe [<targetDir>]
precoding.exe -files file1*.cs file?.ts file3.json ...
precoding.exe -help
```

### Examples

1. **Aggregate all source files in current directory recursively**
   ```
   precoding.exe
   ```
2. **Aggregate all source files in a specific directory recursively:**
   ```
   precoding.exe C:\MyProject
   ```
3. **Aggregate only specific files recursively:**
   ```
   precoding.exe -files src/main.cs README.md app/app.tsx
   ```
4. **Show help:**
   ```
   precoding.exe -help
   ```

---

## Output

- **All matched files are aggregated into `AllSourceFiles.txt`** in the target directory.
- Each file is included as:

  ````markdown
  ### --- FILENAME: relative/path/to/file ---

  ```<language>
  <file contents>
  ```
  ````

- If output is ≤1MB, it is also copied to clipboard automatically.

---

## What’s Ignored

- Folders: `node_modules`, `Migrations`, `obj`, `app-example`, `staticdata`, `docs`, any folders or files starting with `.`, any hidden folders.
- Files starting with `.` (dotfiles).
- Non-UTF8-encoded files may fail to be read.

---

## Dependencies

- .NET 8 or newer
- [TextCopy](https://www.nuget.org/packages/TextCopy) NuGet package

---

## License

GPLv3

---

## Advanced

- **Custom prompt header:**  
  If a `prompt_header.md` file exists in the executable’s directory, its contents will be prepended to the output.

---

## Development

**Main entry:**  
[`Program.cs`](./Program.cs)

- File aggregation and filtering is implemented in `GetFilesSkippingFolders`.
- File type code fencing is determined in `GetFileType`.
- Clipboard integration is via `TextCopy.ClipboardService`.

---

## Contributing

PRs welcome!

---

## Troubleshooting

- **Nothing happens:** Check you’re in the right directory and have matching files.
- **Clipboard doesn’t work:** Output may be >1MB, or clipboard access may be restricted.
- **Errors reading files:** Check file permissions and encoding.

---

_Happy code aggregation!_
