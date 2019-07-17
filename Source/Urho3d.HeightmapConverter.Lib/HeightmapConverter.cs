using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BigGustave;

namespace Urho3d.HeightmapConverter.Lib
{
    public class HeightmapConverter
    {
        /// <summary>
        /// Converts the specified 16bit Grayscale Heightmap <see cref="sourceFilePath"/>
        /// into a 8bit PNG with Height-Information on the Red and Green chanel
        /// as documented in https://urho3d.github.io/documentation/HEAD/class_urho3_d_1_1_terrain.html#ad2a5a0e30901033ba358b2cf6d35d264
        /// </summary>
        /// <param name="sourceFilePath">The source-file</param>
        /// <param name="destinationFilePath">The desired output-file</param>
        /// <returns></returns>
        public async Task ConvertHeightMapAsync(string sourceFilePath, string destinationFilePath)
        {
            if (string.IsNullOrEmpty(sourceFilePath))
            {
                throw new ApplicationException($"Parameter '{nameof(sourceFilePath)}' can't be null or empty");
            }
            if (string.IsNullOrEmpty(destinationFilePath))
            {
                throw new ApplicationException($"Parameter '{nameof(destinationFilePath)}' can't be null or empty");
            }

            var sourceFileInfo = new FileInfo(sourceFilePath);
            var destinationFileInfo = new FileInfo(destinationFilePath);
            if (!sourceFileInfo.Exists)
            {
                throw new ApplicationException($"The specified '{nameof(sourceFilePath)}' ({sourceFilePath}) does not exist");
            }

            if (!destinationFileInfo.Directory.Exists)
            {
                throw new ApplicationException($"The specified output-directory of '{destinationFilePath}' does not exist");
            }

            var sourceExtension = sourceFileInfo.Extension.ToLower();

            if (sourceExtension == ".pgm")
            {
                var parsedHeightData = ParsePgm(sourceFilePath);
                WritePng(destinationFilePath, parsedHeightData);
            }
            else
            {
                throw new ApplicationException($"{nameof(sourceFilePath)} has a unknown file-extension of '{sourceExtension}'");
            }
        }

        private uint[,] ParsePgm(string filePath)
        {
            using (var reader = new StreamReader(filePath, Encoding.ASCII))
            {
                var pgmType = reader.ReadLine();
                var comment = reader.ReadLine();
                var dimensions = reader.ReadLine().Split(' ');

                var numberOfColumns = uint.Parse(dimensions[0]);
                var numberOfRows = uint.Parse(dimensions[1]);

                var values = new uint[numberOfRows, numberOfColumns];
                var x = 0;
                var y = 0;

                if (pgmType == "P2") // Only process Grayscale images
                {
                    var maxValue = uint.Parse(reader.ReadLine());

                    while (!reader.EndOfStream)
                    {
                        var lineInput = reader.ReadLine();
                        var lineElements = lineInput.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var element in lineElements)
                        {
                            var number = uint.Parse(element);

                            values[x, y] = number;

                            x++;
                            if (x >= numberOfColumns)
                            {
                                x = 0;
                                y++;
                            }
                        }
                    }
                }
                else
                {
                    throw new ApplicationException($"Can't process PGM-files of type '{pgmType}'");
                }

                return values;
            }
        }

        private void WritePng(string filePath, uint[,] data)
        {
            var builder = PngBuilder.Create(data.GetLength(0), data.GetLength(1), false);
            for (int x = 0; x < data.GetLength(1); x++)
            {
                for (int y = 0; y < data.GetLength(0); y++)
                {
                    var p = GetRedGreenPixel(data[x, y]);
                    builder.SetPixel(p, x, y);
                }
            }

            using (var writer = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                builder.Save(writer);
            }
        }

        private Pixel GetRedGreenPixel(uint depth)
        {
            var red = depth / 256;
            var green = depth % 256;

            return new Pixel((byte)red, (byte)green, 0, 0, false);
        }
    }
}
