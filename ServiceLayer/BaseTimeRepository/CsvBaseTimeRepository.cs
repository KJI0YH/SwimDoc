using System.Collections.Concurrent;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using DataLayer.EfClasses;

namespace ServiceLayer.BaseTimeRepository;

public sealed class CsvBaseTimeRepository : IBaseTimeRepository
{
    private readonly string _filePath;
    private readonly object _ioLock = new();
    private readonly
        ConcurrentDictionary<(Course course, int meters, Stroke stroke, int relayCount, Gender gender), int> _store =
            new();

    public CsvBaseTimeRepository() : this(Path.Combine(Directory.GetCurrentDirectory(), "base-times.csv"))
    {
    }

    public CsvBaseTimeRepository(string filePath)
    {
        _filePath = filePath;
        LoadFromFile();
    }

    public int GetBaseTime(Course course, int meters, Stroke stroke, int relayCount, Gender sex)
    {
        if (meters <= 0 || relayCount < 0)
            return 0;
        return _store.TryGetValue((course, meters, stroke, relayCount, sex), out var value) ? value : 0;
    }

    public void SetBaseTime(Course course, int meters, Stroke stroke, int relayCount, Gender sex,
        int baseTimeHundredths)
    {
        if (meters <= 0 || relayCount < 0)
            return;
        _store[(course, meters, stroke, relayCount, sex)] = Math.Max(0, baseTimeHundredths);
    }

    public void Save()
    {
        lock (_ioLock)
            SaveToFileLocked();
    }

    private void LoadFromFile()
    {
        lock (_ioLock)
        {
            if (!File.Exists(_filePath))
                return;
            var fileInfo = new FileInfo(_filePath);
            if (fileInfo.Length == 0)
                return;
            using var sr = new StreamReader(_filePath);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                IgnoreBlankLines = true,
                MissingFieldFound = null,
                HeaderValidated = null,
                BadDataFound = null
            };
            using var csv = new CsvReader(sr, config);
            foreach (var record in csv.GetRecords<BaseTimeCsvRow>())
            {
                if (!Enum.TryParse<Course>(record.course, ignoreCase: true, out var course))
                    continue;
                if (!Enum.TryParse<Stroke>(record.stroke, ignoreCase: true, out var stroke))
                    continue;
                if (!Enum.TryParse<Gender>(record.sex, ignoreCase: true, out var sex))
                    continue;
                if (record.meters <= 0 || record.relaycount < 0)
                    continue;
                if (record.basetime < 0)
                    continue;
                _store[(course, record.meters, stroke, record.relaycount, sex)] = record.basetime;
            }
        }
    }

    private void SaveToFileLocked()
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);
        var tmp = _filePath + ".tmp";
        using (var sw = new StreamWriter(tmp, false))
        using (var csv = new CsvWriter(sw, CultureInfo.InvariantCulture))
        {
            csv.WriteHeader<BaseTimeCsvRow>();
            csv.NextRecord();
            foreach (var item in _store
                         .OrderBy(k => k.Key.course)
                         .ThenBy(k => k.Key.relayCount)
                         .ThenBy(k => k.Key.stroke)
                         .ThenBy(k => k.Key.meters)
                         .ThenBy(k => k.Key.gender))
            {
                var row = new BaseTimeCsvRow
                {
                    course = item.Key.course.ToString(),
                    meters = item.Key.meters,
                    stroke = item.Key.stroke.ToString(),
                    sex = item.Key.gender.ToString(),
                    relaycount = item.Key.relayCount,
                    basetime = item.Value
                };
                csv.WriteRecord(row);
                csv.NextRecord();
            }
        }
        File.Copy(tmp, _filePath, overwrite: true);
        File.Delete(tmp);
    }
}
