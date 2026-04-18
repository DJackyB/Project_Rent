using System;
using Martian.Save;
using UnityEngine;

namespace Martian.Save.Runtime
{
    public sealed class UnityJsonSaveSerializer : ISaveSerializer
    {
        public string Serialize(object value, bool prettyPrint = false)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return JsonUtility.ToJson(value, prettyPrint);
        }

        public T Deserialize<T>(string json)
        {
            return JsonUtility.FromJson<T>(json);
        }

        public object Deserialize(string json, Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return JsonUtility.FromJson(json, type);
        }
    }
}
