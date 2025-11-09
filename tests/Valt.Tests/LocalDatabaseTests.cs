using LiteDB;
using Valt.Core.Common;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel.Time;
using Valt.Infra.Modules.Budget.Categories;

namespace Valt.Tests;

[TestFixture]
public class LocalDatabaseTests
{
    private string _tempFolderPath = null!;
    private string _dbFilePath = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _tempFolderPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempFolderPath);

        _dbFilePath = Path.Combine(_tempFolderPath, "testdb.db");
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        try
        {
            if (File.Exists(_dbFilePath))
            {
                File.Delete(_dbFilePath);
            }
        }
        catch
        {
            // ignored
        }

        try
        {
            if (Directory.Exists(_tempFolderPath))
            {
                Directory.Delete(_tempFolderPath, recursive: true);
            }
        }
        catch
        {
            // ignored
        }
    }
    
    [Test]
    public void Should_Change_Password()
    {
        var localDb = new LocalDatabase(new Clock());
        
        localDb.OpenDatabase(_dbFilePath, "123456");
        
        localDb.GetCategories().Insert(new CategoryEntity()
        {
            Icon = Icon.Empty.Name,
            Id = new ObjectId(),
            Name = "a"
        });
        
        localDb.ChangeDatabasePassword("123456", "123457");

        Assert.That(localDb.GetCategories().Query().Count(), Is.GreaterThan(0));
    }
}