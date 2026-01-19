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
        var changedProperties = new List<string>();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(p => p.GetCustomAttribute<PersistableSettingAttribute>() != null))
        {
            var key = $"{className}.{prop.Name}";
            var setting = settings.FirstOrDefault(s => s.Property == key);
            if (setting != null && !string.IsNullOrEmpty(setting.Value))
            {
                try
                {
                    var currentValue = prop.GetValue(this);
                    object newValue;

                    // Handle enum types specially since Convert.ChangeType doesn't work with enums
                    if (prop.PropertyType.IsEnum)
                    {
                        newValue = Enum.Parse(prop.PropertyType, setting.Value);
                    }
                    else
                    {
                        newValue = Convert.ChangeType(setting.Value, prop.PropertyType);
                    }

                    // Only update and notify if value actually changed
                    if (!Equals(currentValue, newValue))
                    {
                        prop.SetValue(this, newValue);
                        changedProperties.Add(prop.Name);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load setting {key}: {ex.Message}");
                }
            }
        }

        // Send messages only for properties that actually changed
        foreach (var propName in changedProperties)
        {
            WeakReferenceMessenger.Default.Send(new SettingsChangedMessage(propName));
        }
    }

    public virtual void Save()
    {
        if (_localDatabase is null || !_localDatabase.HasDatabaseOpen)
            return;

        var type = GetType();
        var className = type.Name;
        var changedProperties = new List<string>();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(p => p.GetCustomAttribute<PersistableSettingAttribute>() != null))
        {
            var key = $"{className}.{prop.Name}";
            var value = prop.GetValue(this)?.ToString() ?? string.Empty;

            var setting = _localDatabase.GetSettings().FindOne(x => x.Property == key);
            if (setting == null)
            {
                setting = new SettingEntity { Property = key, Value = value };
                _localDatabase.GetSettings().Insert(setting);
                changedProperties.Add(prop.Name);
            }
            else if (setting.Value != value)
            {
                // Only update and notify if value actually changed
                setting.Value = value;
                _localDatabase.GetSettings().Update(setting);
                changedProperties.Add(prop.Name);
            }
        }

        // Only notify for properties that actually changed
        foreach (var propName in changedProperties)
        {
            WeakReferenceMessenger.Default.Send(new SettingsChangedMessage(propName));
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