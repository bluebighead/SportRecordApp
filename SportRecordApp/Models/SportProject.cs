using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SportRecordApp.Models;

public class SportProject : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _targetTime = string.Empty;
    private DateTime _createdTime = DateTime.Now;
    private int _checkInDays = 0;
    private List<string> _checkInTimes = new();
    private bool _isCompletedBefore = false;

    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
            }
        }
    }

    public string TargetTime
    {
        get => _targetTime;
        set
        {
            if (_targetTime != value)
            {
                _targetTime = value;
                OnPropertyChanged();
                
                bool wasCompleted = IsCompleted;
                OnPropertyChanged(nameof(IsCompleted));
                OnPropertyChanged(nameof(CompletionRate));
                
                // 如果目标时间改变后项目不再完成，重置已完成标记
                if (!IsCompleted && wasCompleted)
                {
                    _isCompletedBefore = false;
                    OnPropertyChanged(nameof(IsCompletedBefore));
                }
            }
        }
    }

    public DateTime CreatedTime
    {
        get => _createdTime;
        set
        {
            if (_createdTime != value)
            {
                _createdTime = value;
                OnPropertyChanged();
            }
        }
    }

    public int CheckInDays
    {
        get => _checkInDays;
        set
        {
            if (_checkInDays != value)
            {
                _checkInDays = value;
                OnPropertyChanged();
                
                bool wasCompleted = IsCompleted;
                OnPropertyChanged(nameof(IsCompleted));
                OnPropertyChanged(nameof(CompletionRate));
                
                // 检测是否是首次完成
                if (IsCompleted && !wasCompleted && !_isCompletedBefore)
                {
                    _isCompletedBefore = true;
                    OnPropertyChanged(nameof(IsCompletedBefore));
                }
            }
        }
    }

    public bool IsCompletedBefore
    {
        get => _isCompletedBefore;
        set
        {
            if (_isCompletedBefore != value)
            {
                _isCompletedBefore = value;
                OnPropertyChanged();
            }
        }
    }

    public List<string> CheckInTimes
    {
        get => _checkInTimes;
        set
        {
            if (_checkInTimes != value)
            {
                _checkInTimes = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsCompleted
    {
        get
        {
            try
            {
                var match = System.Text.RegularExpressions.Regex.Match(TargetTime ?? string.Empty, @"\d+");
                if (match.Success && int.TryParse(match.Value, out int targetDays))
                {
                    return CheckInDays >= targetDays;
                }
            }
            catch
            {
            }
            return false;
        }
    }

    public string CompletionRate
    {
        get
        {
            try
            {
                var match = System.Text.RegularExpressions.Regex.Match(TargetTime ?? string.Empty, @"\d+");
                if (match.Success && int.TryParse(match.Value, out int targetDays))
                {
                    if (targetDays <= 0)
                    {
                        return "0%";
                    }
                    int rate = Math.Min(100, (int)((double)CheckInDays / targetDays * 100));
                    return IsCompleted ? "目标已达成" : $"{rate}%";
                }
            }
            catch
            {
            }
            return "0%";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
