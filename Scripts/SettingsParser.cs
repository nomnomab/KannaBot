using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KannaBot.Scripts
{
    public class SettingsParser
    {
        private static string Json;
    
        public static string GetField(string path)
        {
            var obj = GetLast(path);
            return (obj as JValue)?.Value.ToString();
        }

        public static JsonArray GetArray(string path)
        {
            var obj = GetLast(path);
            return obj is JArray ? new JsonArray(obj as JArray) : null;
        }

        private static object GetLast(string path)
        {
            string[] parts = path.Split('/');
            JObject data = JsonConvert.DeserializeObject<JObject>(Json);
            object value = null;
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                value = data[part].Value<object>();
                if (value is JObject)
                {
                    data = value as JObject;
                    continue;
                }
            }

            return value;
        }

        public static void ReloadJson(string json = "settings.json")
        {
            Json = File.ReadAllText(Environment.CurrentDirectory + '/' + json);
        }
    
        public class JsonArray
        {
            private readonly List<string> objects;
            public string[] Objects => objects.ToArray();
            public int Count => objects.Count;

            public string this[int index] => objects[index];

            public JsonArray(JArray array)
            {
                objects = new List<string>();
                foreach(var jToken in array)
                {
                    var value = (JValue) jToken;
                    objects.Add((string)value);
                }
            }
        }
    }
}