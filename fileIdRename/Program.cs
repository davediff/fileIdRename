namespace fileIdRename
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    class Program
    {
        [Flags]
        enum LogLevel
        {
            silent = 0x1,
            error = 0x10,
            warning = 0x110,
            info = 0x1110,
            debug = 0x11110,
        }

        private static string CsvInputFile = string.Empty;
        private static string PathToFiles = string.Empty;
        private static int RenamedFileCount = 0;
        private static int ErrorFileCount = 0;
        private static LogLevel logLevel = LogLevel.error;

        private static readonly List<string> Extensions = new List<string>();

        static void Main(string[] args)
        {
            ParseCommandLine(args);

            var dictionary = new Dictionary<string, string>();

            // Read ids and names from csv file
            using (var reader = File.OpenText(CsvInputFile))
            {
                var kvp = reader.ReadLine().Split(',');

                while (kvp != null)
                {
                    try
                    {
                        dictionary.Add(kvp[0].Trim(), kvp[1].Trim().Trim('"'));
                    }
                    catch (ArgumentException ex)
                    {
                        WriteLog(LogLevel.warning, $"Could not add {kvp[0].Trim()} to dictionary already has value {dictionary[kvp[0].Trim()]}", ex);
                    }

                    kvp = reader.ReadLine()?.Split(',');
                }
            }

            var files = Directory.EnumerateFiles(PathToFiles);

            foreach (var file in files)
            {
                var path = Path.GetDirectoryName(file);
                var filename = Path.GetFileName(file);
                var extension = Path.GetExtension(file);

                if (ShouldRenameForExtension(Path.GetExtension(file)))
                {
                    var startIndex = filename.LastIndexOf("(") + 1;
                    var length = filename.LastIndexOf(")") - startIndex;
                    string version = string.Empty;
                    string id = Path.GetFileNameWithoutExtension(filename);

                    if (startIndex != -1 && length > 0)
                    {
                        version = filename.Substring(startIndex, length);
                        id = filename.Substring(0, startIndex - 2);
                    }
                    else
                    {
                        WriteLog(LogLevel.warning, $"Found file with id {id} and no version.");
                    }

                    WriteLog(LogLevel.debug, $"Looking for file with id {id} and version {version}");

                    dictionary.TryGetValue(id, out var title);

                    if (title != null)
                    {
                        var newFileName = string.IsNullOrEmpty(version) ? $"{title}{extension}" : $"{title} ({version}){extension}";

                        WriteLog(LogLevel.debug, $"Renaming {filename} to {newFileName}");

                        try
                        {
                            File.Move(file, Path.Combine(path, newFileName));
                            RenamedFileCount++;
                        }
                        catch (Exception ex)
                        {
                            ErrorFileCount++;
                            WriteLog(LogLevel.error, $"Could not rename {file} to {Path.Combine(path, newFileName)}", ex);
                        }
                    }
                    else
                    {
                        ErrorFileCount++;
                        WriteLog(LogLevel.error, $"Could not find id {id} in {CsvInputFile}");
                    }
                }
                else
                {
                    WriteLog(LogLevel.info, $"Ignoring file with unspecified extension: {Path.GetFileName(file)}");
                }
            }

            if (!logLevel.HasFlag(LogLevel.silent))
            {
                Console.WriteLine($"exit: Renamed {RenamedFileCount} files. Encountered {ErrorFileCount} errors.");
            }
        }

        private static bool ParseCommandLine(string[] args)
        {
            bool continueToParse = true;

            if (args.Length > 0 && !string.IsNullOrEmpty(args[0]))
            {
                CsvInputFile = args[0];
            }
            else
            {
                continueToParse = false;
                WriteLog(LogLevel.error, $"Must pass path to .csv input file (i.e. c:\\users\\user\\downloads\\idTitles.csv) as first argument");
            }

            if (continueToParse && args.Length > 1 && !string.IsNullOrEmpty(args[1]))
            {
                PathToFiles = args[1];
            }
            else
            {
                continueToParse = false;
                WriteLog(LogLevel.error, $"Must pass path to files you want renamed (i.e. c:\\users\\user\\downloads\\filesWithIds\\) as second argument");
            }

            if (args.Length > 2 && !string.IsNullOrEmpty(args[2]))
            {
                Extensions.Add(args[2]);

                int argsIndex = 3;

                while (args.Length > argsIndex)
                {
                    if (args[argsIndex].StartsWith("."))
                    {
                        Extensions.Add(args[argsIndex++]);
                    }
                    else if (Enum.TryParse<LogLevel>(args[argsIndex++], out var result))
                    {
                        logLevel = result;
                    }
                    else
                    {
                        continueToParse = false;
                        WriteLog(LogLevel.error, $"Optionally pass in the LogLevel after all extensions (i.e. silent, error, warning, info, debug)");
                    }
                }
            }
            else
            {
                continueToParse = false;
                WriteLog(LogLevel.error, $"Must pass at least one extension for file types to rename (i.e. .txt .docx) as third, fourth, etc. arguments");
            }

            return continueToParse;
        }

        private static void WriteLog(LogLevel level, string message, Exception ex = null)
        {
            if (level == LogLevel.error)
            {
                ErrorFileCount++;
            }

            if (logLevel.HasFlag(level))
            {
                Console.WriteLine($"{level}: {message}");

                if (ex != null)
                {
                    Console.WriteLine($"{ex.GetType()}: {ex.Message}".Trim());
                }
            }
        }

        private static bool ShouldRenameForExtension(string fileExtension)
        {
            bool shouldRename = false;

            foreach (var extension in Extensions)
            {
                shouldRename |= string.Compare(fileExtension, extension, StringComparison.OrdinalIgnoreCase) == 0;
            }

            return shouldRename;
        }
    }
}