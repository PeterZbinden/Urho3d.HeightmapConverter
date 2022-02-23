using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mono.Options;

namespace Urho3d.HeightmapConverter
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string sourceFile = null;
            string destinationFile = null;
            var help = false;
            var interactive = false;
            var batch = false;
            var batchFiles = new List<string>();

            var p = new OptionSet
            {
                {"s|source=", "The input-file to be converted", v => sourceFile = v},
                {"d|destination=", "The output-file of the conversion (overwrites file if it already exists)", v => destinationFile = v},
                {"b|batch=", "provide multiple files to be processed in a row. Add files as arguments in format: '{input-file}|{output-file}'", v =>
                    {
                        batch = v != null;
                        batchFiles.Add(v);
                    }
                },
                {"i|interactive", "", v => interactive = v!= null},
                {"h|help", "Show help", v => help = v!= null},
            };

            batchFiles.AddRange(p.Parse(args));

            if (help)
            {
                p.WriteOptionDescriptions(Console.Out);
            }

            var converter = new Lib.HeightmapConverter();
            if (interactive)
            {
                await InteractiveMode(converter);
            }
            else if (batch)
            {
                foreach (var batchFile in batchFiles)
                {
                    var files = batchFile.Split(new []{"|"}, StringSplitOptions.RemoveEmptyEntries);
                    if (files.Length >= 2)
                    {
                        var input = files[0].Trim();
                        var output = files[1].Trim();
                        Console.WriteLine($"Trying to convert '{input}' to '{output}'");
                        await ProcessSavely(converter, input, output);
                    }
                    else
                    {
                        Console.WriteLine($"Skipping input '{batchFile}'");
                    }
                }
            }
            else
            {
                await ProcessInput(sourceFile, destinationFile, converter);
            }
        }

        private static async Task ProcessInput(string sourceFile, string destinationFile, Lib.HeightmapConverter converter)
        {
            if (!string.IsNullOrEmpty(sourceFile))
            {
                if (!string.IsNullOrEmpty(destinationFile))
                {
                    await converter.ConvertHeightMapAsync(sourceFile, destinationFile);
                }
            }
        }

        private static async Task InteractiveMode(Lib.HeightmapConverter converter)
        {
            do
            {
                var input = GetUserInput("Enter path to source-file");
                var output = GetUserInput("Enter path to destination-file");

                await ProcessSavely(converter, input, output);

                if (!GetUserBoolInput("Continue?"))
                {
                    break;
                }
            } while (true);
        }

        private static async Task ProcessSavely(Lib.HeightmapConverter converter, string input, string output)
        {
            try
            {
                await converter.ConvertHeightMapAsync(input, output);
                Console.WriteLine("Conversion successful");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static bool GetUserBoolInput(string prompt)
        {
            do
            {
                var userInput = GetUserInput($"{prompt} (yes/no)");
                var cleanedInput = userInput.ToLower().Trim();
                if (cleanedInput == "yes" || cleanedInput == "y")
                {
                    return true;
                }
                else if (cleanedInput == "no" || cleanedInput == "n")
                {
                    return false;
                }

                Console.WriteLine($"Your input ('{userInput}') must either be 'yes' or 'no'");

            } while (true);
        }

        private static string GetUserInput(string prompt)
        {
            Console.WriteLine($"{prompt}:");
            return Console.ReadLine();
        }
    }
}
