using System.ComponentModel;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Valt.Infra.DataAccess;

namespace Valt.Infra.Settings;

public abstract class BaseSettings : ObservableObject
{
    private readonly ILocalDatabase? _localDatabase;

    public BaseSettings(ILocalDatabase? localDatabase)
    {
        if (localDatabase is not null)
        {
            _localDatabase = localDatabase;
            _localDatabase.PropertyChanged += LocalDatabaseOnPropertyChanged;
        }

        LoadDefaults();
    }
    
    private void LocalDatabaseOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Load();
    }

    public virtual void Load()
    {
        if (_localDatabase is null || !_localDatabase.HasDatabaseOpen)
        {
            LoadDefaults();
            return;
        }
        
        var settings = _localDatabase.GetSettings().FindAll().ToList();
        var type = GetType();
        var className = type.Name;

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(p => p.GetCustomAttribute<PersistableSettingAttribute>() != null))
        {
            var key = $"{className}.{prop.Name}";
            var setting = settings.FirstOrDefault(s => s.Property == key);
            if (setting != null && !string.IsNullOrEmpty(setting.Value))
            {
                try
                {
                    var value = Convert.ChangeType(setting.Value, prop.PropertyType);
                    prop.SetValue(this, value);
                    WeakReferenceMessenger.Default.Send(new SettingsChangedMessage(prop.Name));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load setting {key}: {ex.Message}");
                }
            }
        }
    }

    public virtual void Save()
    {
        if (_localDatabase is null || !_localDatabase.HasDatabaseOpen)
            return;
        
        var type = GetType();
        var className = type.Name;

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(p => p.GetCustomAttribute<PersistableSettingAttribute>() != null))
        {
            var key = $"{className}.{prop.Name}";
            var value = prop.GetValue(this)?.ToString() ?? string.Empty;

            var setting = _localDatabase.GetSettings().FindOne(x => x.Property == key);
            if (setting == null)
            {
                setting = new SettingEntity() { Property = key, Value = value };
            }
            else
            {
                setting.Value = value;
            }
            _localDatabase.GetSettings().Upsert(setting);
        }

        //notify property changes
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(p => p.GetCustomAttribute<PersistableSettingAttribute>() != null))
        {
            WeakReferenceMessenger.Default.Send(new SettingsChangedMessage(prop.Name));
        }
    }
    
    protected abstract void LoadDefaults();
}

public class SettingsChangedMessage : ValueChangedMessage<string>
{
    public SettingsChangedMessage(string propertyName) : base(propertyName)
    {
    }
}