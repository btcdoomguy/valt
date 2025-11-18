using LiteDB;

namespace Valt.Infra.DataAccess.LiteDBMappers;

public class DateOnlyMapper
{
    public static void Register(BsonMapper mapper)
    {
        mapper.RegisterType<DateOnly>(
            serialize: date => new BsonValue(date.ToValtDateTime()),
            deserialize: bson => DateOnly.FromDateTime(bson.AsDateTime)
        );
    }
}