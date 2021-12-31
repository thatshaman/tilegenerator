using System;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace tilegenerator
{
    class Program
    {
        static int Main(string[] args)
        {
            Option<string> input = new("--input", description: "Input file");
            input.AddAlias("--i");
            input.IsRequired = true;


            Option<string> output = new("--output", getDefaultValue: () => "tiles\\" + DateTime.Now.ToString("yyyy_MM_dd_HHmmss"), description: "Output folder");
            output.AddAlias("--o");

            Option<string> format = new("--format", getDefaultValue: () => "jpg", description: "Output file format (jpg, png)");
            format.AddAlias("--f");

            Option<int> quality = new("--quality", getDefaultValue: () => 90, description: "JPEG quality (1, 100)");
            quality.AddAlias("--q");

            Option<int> zoomlevels = new("--zoomlevels", getDefaultValue: () => 5, description: "Number of zoom levels");
            zoomlevels.AddAlias("--z");

            Option<int> tilesize = new("--tilesize", getDefaultValue: () => 256, description: "Tile size (min: 16, max: 2048)");
            tilesize.AddAlias("--s");

            Option<bool> leaflet = new("--leaflet", getDefaultValue: () => false, description: "Copy example leaflet file");

            RootCommand rootCommand = new()
            {
                input,
                output,
                format,
                quality,
                zoomlevels,
                tilesize,
                leaflet,
            };

            rootCommand.Description = "Convert an image to Leaflet compatible map tiles.";
            rootCommand.Handler = CommandHandler.Create<string, string, string, int, int, int, bool>((input, output, format, quality, zoomlevels, tilesize, leaflet) =>
            {
                if (!File.Exists(input))
                {
                    Console.WriteLine("File not found: " + input);
                    return 0;
                }

                format = format.ToLower().Trim();
                if (format != "jpg" && format != "png")
                {
                    Console.WriteLine("Unknown format: " + format);
                    return 0;
                }

                zoomlevels = Math.Min(Math.Max(1, zoomlevels), 99);
                quality = Math.Min(Math.Max(1, quality), 100);
                tilesize = Math.Min(Math.Max(16, tilesize), 2048);              

                return Export(input, output, format, quality, zoomlevels, tilesize, leaflet);
            });

            return rootCommand.InvokeAsync(args).Result;


        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        static int Export(string input, string output, string format, int quality, int zoomlevels, int tilesize, bool leaflet)
        {
            Console.WriteLine("Writing to: " + output);
            if (!Directory.Exists(output)) Directory.CreateDirectory(output);
            for (int i = 0; i < zoomlevels; i++) Directory.CreateDirectory(output + "\\" + i.ToString());

            Color clearColor = Color.Black;
            if (format == "png") clearColor = Color.Transparent;
 
            ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);
            EncoderParameters encoderParameters = new(1);
            encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);

            Bitmap tile = new(tilesize, tilesize);
            Graphics e = Graphics.FromImage(tile);

            Bitmap[] sources = new Bitmap[zoomlevels];
            sources[zoomlevels - 1] = new Bitmap(input, true);

            // Rescale
            for (int source = zoomlevels - 2; source > -1; source--)
            {
                sources[source] = new Bitmap((int)Math.Ceiling((decimal)sources[source + 1].Width / 2), (int)Math.Ceiling((decimal)sources[source + 1].Height / 2));
                Graphics q = Graphics.FromImage(sources[source]);
                q.DrawImage(sources[source + 1], 0, 0, sources[source].Width, sources[source].Height);
                q.Dispose();
            }

            // Draw
            for (int z = zoomlevels - 1; z > -1; z--)
            {
                int tilecountX = (int)Math.Ceiling((double)sources[z].Width / tilesize);
                int tilecountY = (int)Math.Ceiling((double)sources[z].Height / tilesize);

                for (int y = 0; y < tilecountY; y++)
                {
                    for (int x = 0; x < tilecountX; x++)
                    {
                        e.Clear(clearColor);
                        e.DrawImageUnscaled(sources[z], new Point(x * -tilesize, y * -tilesize));
                        Directory.CreateDirectory(string.Format("{0}\\{1}\\{2}", output, z, x));

                        if (format == "jpg")
                        {
                            tile.Save(string.Format("{0}\\{1}\\{2}\\{3}.jpg", output, z, x, y), jpgEncoder, encoderParameters);
                        }
                        else if (format == "png")
                        {
                            tile.Save(string.Format("{0}\\{1}\\{2}\\{3}.png", output, z, x, y));
                        }
                    }
                }
            }


            // Cleanup
            for (int i = 0; i < sources.Length; i++) sources[i].Dispose();
            tile.Dispose();
            GC.Collect();

            // Export leaflet demo
            if (leaflet)
            {
                string html = File.ReadAllText("leaflet.html");
                html = html.Replace("{maxZoom}", (zoomlevels - 1).ToString());
                html = html.Replace("{format}", format);
                html = html.Replace("{tileSize}", tilesize.ToString());
                File.WriteAllText(Path.Combine(output, "index.html"), html);
            }

            Console.WriteLine("Done");
            return 1;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid) return codec;
            }
            return null;
        }

    }
}
