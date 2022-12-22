// Copyright 2022 Rive

using Microsoft.Extensions.Configuration;
using RiveSharp;
using SkiaSharp;

namespace Goldens;

internal class Goldens
{
    const int CELL = 256;
    const int W = 5;
    const int H = 5;
    const int GAP = 2;
    const int SW = W * CELL + (W + 1) * GAP;
    const int SH = H * CELL + (H + 1) * GAP;

    static int Main(string[] commandLineArgs)
    {
        var args = new ConfigurationBuilder().AddCommandLine(commandLineArgs).Build();
        string rivs = args["rivs"];
        string destination = args["destination"];
        bool verbose = args["verbose"] == "true";
        var skImageInfo = new SKImageInfo(SW, SH, SKColorType.Rgba8888, SKAlphaType.Premul);

        var rivFiles = Directory.GetFiles(rivs);
        Console.WriteLine($"Rendering {rivFiles.Length} pngs...");
        foreach (string riv in rivFiles)
        {
            // Load the animation.
            if (riv.Contains("Centaur_v2.riv") || riv.Contains("Planet_clean.riv"))
            {
                continue;  // https://github.com/rive-app/rive-cpp/issues/334
            }
            if (riv.Contains("paper.riv"))
            {
                continue;  // https://github.com/rive-app/rive/issues/4573
            }

            if (verbose)
            {
                Console.Write($"Loading {riv}...\n");
            }

            var fileStream = File.OpenRead(riv);
            if (fileStream == null)
            {
                Console.WriteLine($"ERROR: failed to open file '{riv}'");
                return -1;
            }
            var scene = new Scene();
            if (!scene.LoadFile(fileStream) || !scene.LoadArtboard("") || !scene.LoadAnimation(""))
            {
                Console.WriteLine($"ERROR: failed to load .riv animation '{riv}'");
                return -1;
            }
            if (verbose)
            {
                Console.WriteLine($"Loaded scene \"{scene.Name}\"");
            }

            // Render the grid.
            var surface = SKSurface.Create(skImageInfo);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            const int FRAMES = H * W;
            double duration = scene.DurationSeconds;
            double frameDuration = duration / FRAMES;

            scene.AdvanceAndApply(0);

            var renderer = new Renderer(canvas);
            renderer.Translate(GAP, GAP);
            renderer.Save();
            for (int y = 0; y < H; ++y)
            {
                for (int x = 0; x < W; ++x)
                {
                    renderer.Save();

                    renderer.Translate(x * (CELL + GAP), y * (CELL + GAP));
                    renderer.Align(Fit.Cover, Alignment.Center,
                                   new AABB(0, 0, CELL, CELL),
                                   new AABB(0, 0, scene.Width, scene.Height));
                    scene.Draw(renderer);

                    scene.AdvanceAndApply(frameDuration);

                    renderer.Restore();
                }
            }
            renderer.Restore();

            // Save out a png.
            canvas.Flush();
            var pngData = surface.Snapshot().Encode(SKEncodedImageFormat.Png, quality:100);
            Directory.CreateDirectory(destination);
            string png = Path.Combine(destination, Path.GetFileNameWithoutExtension(riv) + ".png");
            File.WriteAllBytes(png, pngData.Span.ToArray());

            if (verbose)
            {
                Console.WriteLine($"Wrote {png}\n");
            }
        }

        return 0;
    }
}
