namespace Valt.Infra.Kernel;

public static class ValtEnvironment
{
    private static string _appDataPath = string.Empty;
    public static string AppDataPath
    {
        get
        {
            if (_appDataPath != string.Empty) 
                return _appDataPath;
            
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Valt");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            _appDataPath = path;

            return _appDataPath;
        }
    }
}