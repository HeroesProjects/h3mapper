using System;
using System.IO;
using System.IO.Compression;
using H3Mapper.Analysis;
using H3Mapper.DataModel;
using H3Mapper.Internal;
using H3Mapper.Serialize;
using Serilog;

namespace H3Mapper
{
    public class Executor : IDisposable
    {
        private readonly IdMappings mappings;
        private readonly string path;
        private readonly MapValidator validator;
        private readonly DuplicateFinder duplicateFinder = new DuplicateFinder();

        public Executor(IdMappings idMappings, string path, bool validate)
        {
            mappings = idMappings;
            this.path = path;

            if (validate)
            {
                validator = new MapValidator(idMappings);
            }
        }

        public int Run()
        {
            if (File.Exists(path))
            {
                return Process(mappings, path);
            }

            if (Directory.Exists(path))
            {
                var result = 0;
                foreach (var file in Directory.EnumerateFiles(path, "*.h3m", SearchOption.AllDirectories))
                {
                    var fileResult = Process(mappings, file);
                    if (fileResult != 0)
                    {
                        result = fileResult;
                    }
                }

                duplicateFinder.Dump(Log.Logger);
                return result;
            }

            Log.Information("Given path: '{path}' does not point to an existing file or directory", path);
            return 1;
        }


        private int Process(IdMappings idMappings, string mapFilePath)
        {
            Log.Information("Processing {file}", mapFilePath);
            using (var mapFile = new GZipStream(File.OpenRead(mapFilePath), CompressionMode.Decompress))
            {
                var reader = new MapReader(idMappings);
                try
                {
                    var mapData = reader.Read(new MapDeserializer(new PositionTrackingStream(mapFile)));
                    validator?.Validate(mapData);
                    duplicateFinder.Process(mapData, mapFilePath);
                }
                catch (InvalidDataException e)
                {
                    Log.Error(e, "Failed to process map {file}. File is most likely corrupted.", mapFilePath);
                    return e.HResult;
                }
                catch (ArgumentException e)
                {
                    Log.Error(e, "Failed to process map {file}. File is most likely corrupted.", mapFilePath);
                    return e.HResult;
                }
                catch (InvalidOperationException e)
                {
                    Log.Error(e, "Failed to process map {file}. File is most likely corrupted.", mapFilePath);
                    return e.HResult;
                }

                return 0;
            }
        }

        public void Dispose()
        {
            duplicateFinder?.Dispose();
        }
    }
}