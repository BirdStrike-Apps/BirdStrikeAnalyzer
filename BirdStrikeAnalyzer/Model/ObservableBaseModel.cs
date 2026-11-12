using System.ComponentModel;
using System.Runtime.CompilerServices;
using Supabase.Postgrest.Models;

namespace BirdStrikeAnalyzer.Model;

public abstract class ObservableBaseModel : BaseModel, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
