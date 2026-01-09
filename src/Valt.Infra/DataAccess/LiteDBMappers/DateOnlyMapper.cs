using LiteDB;

namespace Valt.Infra.DataAccess.LiteDBMappers;

public class DateOnlyMapper
{
    public static void Register(BsonMapper mapper)
    {
        mapper.RegisterType<DateOnly>(
            serialize: date => new BsonValue(date.ToValtDateTime()),
            // Convert to UTC before extracting date to ensure timezone doesn't shift the date
            deserialize: bson => DateOnly.FromDateTime(bson.AsDateTime.ToUniversalTime())
        );
    }
}