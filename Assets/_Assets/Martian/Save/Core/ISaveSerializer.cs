using System;

namespace Martian.Save
{
    public interface ISaveSerializer
    {
        string Serialize(object value, bool prettyPrint = false);
        T Deserialize<T>(string json);
        object Deserialize(string json, Type type);
    }
}
