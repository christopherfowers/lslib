﻿using System;
using System.Collections.Generic;
using System.IO;
using LSLib.LS.Enums;

namespace LSLib.LS
{
    public class ResourceUtils
    {
        public delegate void ProgressUpdateDelegate(string status, long numerator, long denominator);
        public ProgressUpdateDelegate progressUpdate = delegate { };

        public static ResourceFormat ExtensionToResourceFormat(string path)
        {
            var extension = Path.GetExtension(path).ToLower();

            switch (extension)
            {
                case ".lsx":
                    return ResourceFormat.LSX;

                case ".lsb":
                    return ResourceFormat.LSB;

                case ".lsf":
                    return ResourceFormat.LSF;

                case ".lsj":
                    return ResourceFormat.LSJ;

                default:
                    throw new ArgumentException("Unrecognized file extension: " + extension);
            }
        }

        public static Resource LoadResource(string inputPath)
        {
            return LoadResource(inputPath, ExtensionToResourceFormat(inputPath));
        }

        public static Resource LoadResource(string inputPath, ResourceFormat format)
        {
            var file = new FileStream(inputPath, FileMode.Open, FileAccess.Read);
            switch (format)
            {
                case ResourceFormat.LSX:
                    {
                        using (var reader = new LSXReader(file))
                        {
                            return reader.Read();
                        }
                    }

                case ResourceFormat.LSB:
                    {
                        using (var reader = new LSBReader(file))
                        {
                            return reader.Read();
                        }
                    }

                case ResourceFormat.LSF:
                    {
                        using (var reader = new LSFReader(file))
                        {
                            return reader.Read();
                        }
                    }

                case ResourceFormat.LSJ:
                    {
                        using (var reader = new LSJReader(file))
                        {
                            return reader.Read();
                        }
                    }

                default:
                    throw new ArgumentException("Invalid resource format");
            }
        }

        public static void SaveResource(Resource resource, string outputPath)
        {
            SaveResource(resource, outputPath, ExtensionToResourceFormat(outputPath));
        }

        public static void SaveResource(Resource resource, string outputPath, ResourceFormat format, FileVersion version = 0x0)
        {
            FileManager.TryToCreateDirectory(outputPath);

            using (var file = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            {
                switch (format)
                {
                    case ResourceFormat.LSX:
                        {
                            var writer = new LSXWriter(file);
                            writer.PrettyPrint = true;
                            writer.Write(resource);
                            break;
                        }

                    case ResourceFormat.LSB:
                        {
                            var writer = new LSBWriter(file);
                            writer.Write(resource);
                            break;
                        }

                    case ResourceFormat.LSF:
                        {
                            // Write in V2 format for D:OS EE compatibility
                            FileVersion lsfVersion = version == 0x0 ? FileVersion.VerChunkedCompress : version;

                            var writer = new LSFWriter(file, lsfVersion);
                            writer.Write(resource);
                            break;
                        }

                    case ResourceFormat.LSJ:
                        {
                            var writer = new LSJWriter(file);
                            writer.PrettyPrint = true;
                            writer.Write(resource);
                            break;
                        }

                    default:
                        throw new ArgumentException("Invalid resource format");
                }
            }
        }

        private void EnumerateFiles(List<string> paths, string rootPath, string currentPath, string extension)
        {
            foreach (string filePath in Directory.GetFiles(currentPath))
            {
                var fileExtension = Path.GetExtension(filePath);
                if (fileExtension.ToLower() == extension)
                {
                    var relativePath = filePath.Substring(rootPath.Length);
                    if (relativePath[0] == '/' || relativePath[0] == '\\')
                    {
                        relativePath = relativePath.Substring(1);
                    }

                    paths.Add(relativePath);
                }
            }

            foreach (string directoryPath in Directory.GetDirectories(currentPath))
            {
                EnumerateFiles(paths, rootPath, directoryPath, extension);
            }
        }

        public void ConvertResources(string inputDir, string outputDir, ResourceFormat inputFormat, ResourceFormat outputFormat, FileVersion outputVersion = 0x0)
        {
            this.progressUpdate("Enumerating files ...", 0, 1);
            var paths = new List<string>();
            EnumerateFiles(paths, inputDir, inputDir, "." + inputFormat.ToString().ToLower());

            this.progressUpdate("Converting resources ...", 0, 1);
            for (var i = 0; i < paths.Count; i++)
            {
                var path = paths[i];
                var inPath = inputDir + "/" + path;
                var outPath = outputDir + "/" + Path.ChangeExtension(path, outputFormat.ToString().ToLower());

                FileManager.TryToCreateDirectory(outPath);

                this.progressUpdate("Converting: " + inPath, i, paths.Count);
                var resource = LoadResource(inPath, inputFormat);
                SaveResource(resource, outPath, outputFormat, outputVersion);
            }
        }
    }
}
