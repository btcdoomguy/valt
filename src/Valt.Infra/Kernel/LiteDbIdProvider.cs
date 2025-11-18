using LiteDB;
using Valt.Core.Kernel.Factories;

namespace Valt.Infra.Kernel;

public class LiteDbIdProvider : IIdProvider
{
    public string NewId()
    {
        return ObjectId.NewObjectId().ToString();
    }
}