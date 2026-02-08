using System;
using System.IO;
using Avalonia.Media.Imaging;
using SkiaSharp;
using Svg.Skia;

namespace Snap.Net.Avalonia.Helpers;

public class ImageHelpers
{
    public static Stream GetImageStream(string path)
    {
        if (File.Exists(path) == false)
        {
            throw new FileNotFoundException("File not found.", path);
        }
        return File.OpenRead(path);
    }

    public static Stream GetImageStreamFromBase64(string base64)
    {
        byte[] data = Convert.FromBase64String(base64);
        return new MemoryStream(data);
    }

    public static Bitmap ImageFromString(string base64, string extension)
    {
        Stream imageStream = GetImageStreamFromBase64(base64);
        if (extension == "svg")
        {
            SKSvg skSvg = new SKSvg();
            skSvg.Load(imageStream);
            using MemoryStream ms = new MemoryStream();
            skSvg.Save(ms, SKColors.Empty);
            ms.Seek(0, SeekOrigin.Begin);
            return new  Bitmap(ms);
        }
        return new Bitmap(imageStream);
    }

    public static Bitmap ImageFromFile(string path)
    {
        return new Bitmap(GetImageStream(path));
    }
}