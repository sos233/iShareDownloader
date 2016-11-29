using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace iShareDownloader.Utils
{
    public static class AscArt
    {
        /// <summary>
        /// ref: https://csharp.2000things.com/2012/12/25/743-ascii-art-generator/
        /// Output: File with same name as input file but .txt extension
        /// </summary>
        /// <param name="fileName">Full path to bitmap file (e.g. JPG, PNG)</param>
        /// <param name="outputWidth">Width of target image, in # characters (e.g. 120)</param>
        public static void Generate(string fileName, int outputWidth)
        {
            FileInfo fi = new FileInfo(fileName);
            if (!fi.Exists)
                throw new Exception(string.Format("File {0} not found", fileName));
            string outputFile = Path.Combine(fi.DirectoryName, Path.GetFileNameWithoutExtension(fileName) + ".txt");

            Bitmap bmInput = new Bitmap(fileName);

            if (outputWidth > bmInput.Width)
                throw new Exception("Output width must be <= pixel width of image");

            // Generate the ASCII art
            AscArt.GenerateAsciiArt(bmInput, outputFile, outputWidth);
        }

        // Typical width/height for ASCII characters
        private const double FontAspectRatio = 0.6;

        // Available character set, ordered by decreasing intensity (brightness)
        private const string OutputCharSet = "@%#*+=-:. ";

        // Alternate char set uses more chars, but looks less realistic
        private const string OutputCharSetAlternate = "$@B%8&WM#*oahkbdpqwmZO0QLCJUYXzcvunxrjft/\\|()1{}[]?-_+~<>i!lI;:,\"^`'. ";

        public static void GenerateAsciiArt(Bitmap bmInput, string outputFile, int outputWidth)
        {
            // pixelChunkWidth/pixelChunkHeight - size of a chunk of pixels that will
            // map to 1 character.  These are doubles to avoid progressive rounding
            // error.
            double pixelChunkWidth = (double)bmInput.Width / (double)outputWidth;
            double pixelChunkHeight = pixelChunkWidth / FontAspectRatio;

            // Calculate output height to capture entire image
            int outputHeight = (int)Math.Round((double)bmInput.Height / pixelChunkHeight);

            // Generate output image, row by row
            double pixelOffSetTop = 0.0;
            StringBuilder sbOutput = new StringBuilder();

            for (int row = 1; row <= outputHeight; row++)
            {
                double pixelOffSetLeft = 0.0;

                for (int col = 1; col <= outputWidth; col++)
                {
                    // Calculate brightness for this set of pixels by averaging
                    // brightness across all pixels in this pixel chunk
                    double brightSum = 0.0;
                    int pixelCount = 0;
                    for (int pixelLeftInd = 0; pixelLeftInd < (int)pixelChunkWidth; pixelLeftInd++)
                        for (int pixelTopInd = 0; pixelTopInd < (int)pixelChunkHeight; pixelTopInd++)
                        {
                            // Each call to GetBrightness returns value between 0.0 and 1.0
                            int x = (int)pixelOffSetLeft + pixelLeftInd;
                            int y = (int)pixelOffSetTop + pixelTopInd;
                            if ((x < bmInput.Width) && (y < bmInput.Height))
                            {
                                brightSum += bmInput.GetPixel(x, y).GetBrightness();
                                pixelCount++;
                            }
                        }

                    // Average brightness for this entire pixel chunk, between 0.0 and 1.0
                    double pixelChunkBrightness = brightSum / pixelCount;

                    // Target character is just relative position in ordered set of output characters
                    int outputIndex = (int)Math.Floor(pixelChunkBrightness * OutputCharSet.Length);
                    if (outputIndex == OutputCharSet.Length)
                        outputIndex--;

                    char targetChar = OutputCharSet[outputIndex];

                    sbOutput.Append(targetChar);

                    pixelOffSetLeft += pixelChunkWidth;
                }
                sbOutput.AppendLine();
                pixelOffSetTop += pixelChunkHeight;
            }

            // Dump output string to file
            File.WriteAllText(outputFile, sbOutput.ToString());
        }
    }
}
