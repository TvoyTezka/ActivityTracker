namespace TrackerApp;

public class TrackService
{
    private readonly Database _db;
    private readonly System.Windows.Forms.Timer _timer;
    private long? _currentSessionId;
    private string? _currentProcess;
    private string? _currentTitle;
    private double _idleThreshold;
    private bool _paused;
    private int _tickSeq;

    public bool Paused => _paused;

    public TrackService(Database db)
    {
        _db = db;
        var cfg = Config.Load();
        _idleThreshold = cfg.IdleThresholdS;
        _timer = new System.Windows.Forms.Timer { Interval = cfg.PollIntervalMs };
        _timer.Tick += OnTick;
    }

    public void ApplySettings(Config cfg)
    {
        _timer.Interval = cfg.PollIntervalMs;
        _idleThreshold = cfg.IdleThresholdS;
        cfg.Save();
    }

    public void Start() => _timer.Start();
    public void Stop()
    {
        _timer.Stop();
        EndCurrentSession();
    }

    public void Pause()
    {
        _paused = true;
        EndCurrentSession();
    }

    public void Resume() => _paused = false;

    public void TogglePause()
    {
        if (_paused) Resume(); else Pause();
    }

    private void EndCurrentSession()
    {
        if (_currentSessionId.HasValue)
        {
            _db.EndSession(_currentSessionId.Value);
            _currentSessionId = null;
            _currentProcess = null;
        }
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_paused) return;

        var seq = Interlocked.Increment(ref _tickSeq);

        try
        {
            if (seq != Volatile.Read(ref _tickSeq)) return;

            var (process, title) = Win32.GetForegroundInfo();
            var idle = Win32.GetIdleSeconds();

            if (idle > _idleThreshold)
            {
                EndCurrentSession();
                return;
            }

            if (process == _currentProcess && title == _currentTitle)
                return;

            EndCurrentSession();

            _currentProcess = process;
            _currentTitle = title;
            _currentSessionId = _db.StartSession(process, title);
        }
        catch
        {
            // silently continue on transient errors
        }
    }
}
