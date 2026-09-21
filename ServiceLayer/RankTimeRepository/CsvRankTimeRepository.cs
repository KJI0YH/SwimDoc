using System.Collections.Concurrent;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using DataLayer.Display;
using DataLayer.EfClasses;
using ServiceLayer.Logging;

namespace ServiceLayer.RankTimeRepository;

public sealed class CsvRankTimeRepository : IRankTimeRepository
{
    private readonly string _filePath;
    private readonly IAppLog _log;
    private readonly object _ioLock = new();
    private readonly ConcurrentDictionary<(Course course, int meters, Stroke stroke, int relayCount, Gender gender, Category category), int>
        _store = new();

    public CsvRankTimeRepository() : this(ApplicationPaths.EnsureUserDataFileFromBundle("rank-times.csv"), NullAppLog.Instance)
    {
    }

    public CsvRankTimeRepository(string filePath) : this(filePath, NullAppLog.Instance)
    {
    }

    public CsvRankTimeRepository(IAppLog log) : this(ApplicationPaths.EnsureUserDataFileFromBundle("rank-times.csv"), log)
    {
    }

    public CsvRankTimeRepository(string filePath, IAppLog log)
    {
        _filePath = filePath;
        _log = log;
        LoadFromFile();
    }

    public int GetRankTime(Course course, int meters, Stroke stroke, int relayCount, Gender sex, Category category)
    {
        if (meters <= 0 || relayCount < 0)
            return 0;
        return _store.TryGetValue((course, meters, stroke, relayCount, sex, category), out var value) ? value : 0;
    }

    public void SetRankTime(Course course, int meters, Stroke stroke, int relayCount, Gender sex, Category category,
        int timeHundredths)
    {
        if (meters <= 0 || relayCount < 0)
            return;
        _store[(course, meters, stroke, relayCount, sex, category)] = Math.Max(0, timeHundredths);
    }

    public IReadOnlyDictionary<Category, int> GetRankTimes(
        Course course,
        int meters,
        Stroke stroke,
        int relayCount,
        Gender sex)
    {
        var result = new Dictionary<Category, int>();
        foreach (var category in CategoryDisplay.FromHighest)
        {
            var value = GetRankTime(course, meters, stroke, relayCount, sex, category);
            if (value > 0)
                result[category] = value;
        }
        return result;
    }

    public void Save()
    {
        lock (_ioLock)
            SaveToFileLocked();
        _log.Info($"Saved rank times: {_filePath} ({_store.Count} values)");
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
            foreach (var record in csv.GetRecords<RankTimeCsvRow>())
            {
                if (!Enum.TryParse<Course>(record.course, ignoreCase: true, out var course))
                    continue;
                if (!Enum.TryParse<Stroke>(record.stroke, ignoreCase: true, out var stroke))
                    continue;
                if (!Enum.TryParse<Gender>(record.sex, ignoreCase: true, out var sex))
                    continue;
                if (record.meters <= 0 || record.relaycount < 0)
                    continue;
                StoreCategory(course, record.meters, stroke, record.relaycount, sex, Category.IMoS, record.msmk);
                StoreCategory(course, record.meters, stroke, record.relaycount, sex, Category.MoS, record.ms);
                StoreCategory(course, record.meters, stroke, record.relaycount, sex, Category.CMoS, record.kms);
                StoreCategory(course, record.meters, stroke, record.relaycount, sex, Category.FirstAdult, record.i);
                StoreCategory(course, record.meters, stroke, record.relaycount, sex, Category.SecondAdult, record.ii);
                StoreCategory(course, record.meters, stroke, record.relaycount, sex, Category.ThirdAdult, record.iii);
                StoreCategory(course, record.meters, stroke, record.relaycount, sex, Category.FirstJunior, record.i_yun);
                StoreCategory(course, record.meters, stroke, record.relaycount, sex, Category.SecondJunior, record.ii_yun);
            }
        }
    }

    private void StoreCategory(
        Course course,
        int meters,
        Stroke stroke,
        int relayCount,
        Gender sex,
        Category category,
        int time)
    {
        if (time < 0)
            return;
        _store[(course, meters, stroke, relayCount, sex, category)] = time;
    }

    private void SaveToFileLocked()
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);
        var groups = _store
            .GroupBy(item => (item.Key.course, item.Key.meters, item.Key.stroke, item.Key.relayCount, item.Key.gender))
            .OrderBy(g => g.Key.course)
            .ThenBy(g => g.Key.relayCount)
            .ThenBy(g => g.Key.stroke)
            .ThenBy(g => g.Key.meters)
            .ThenBy(g => g.Key.gender);
        var tmp = _filePath + ".tmp";
        using (var sw = new StreamWriter(tmp, false))
        using (var csv = new CsvWriter(sw, CultureInfo.InvariantCulture))
        {
            csv.WriteHeader<RankTimeCsvRow>();
            csv.NextRecord();
            foreach (var group in groups)
            {
                var lookup = group.ToDictionary(x => x.Key.category, x => x.Value);
                var row = new RankTimeCsvRow
                {
                    course = group.Key.course.ToString(),
                    meters = group.Key.meters,
                    stroke = group.Key.stroke.ToString(),
                    sex = group.Key.gender.ToString(),
                    relaycount = group.Key.relayCount,
                    msmk = GetOrZero(lookup, Category.IMoS),
                    ms = GetOrZero(lookup, Category.MoS),
                    kms = GetOrZero(lookup, Category.CMoS),
                    i = GetOrZero(lookup, Category.FirstAdult),
                    ii = GetOrZero(lookup, Category.SecondAdult),
                    iii = GetOrZero(lookup, Category.ThirdAdult),
                    i_yun = GetOrZero(lookup, Category.FirstJunior),
                    ii_yun = GetOrZero(lookup, Category.SecondJunior)
                };
                csv.WriteRecord(row);
                csv.NextRecord();
            }
        }
        File.Copy(tmp, _filePath, overwrite: true);
        File.Delete(tmp);
    }

    private static int GetOrZero(IReadOnlyDictionary<Category, int> lookup, Category category) =>
        lookup.TryGetValue(category, out var value) ? value : 0;
}
