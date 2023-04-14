//#define USE_NEWTONSOFT
#if USE_NEWTONSOFT
using Newtonsoft.Json;
#elif UNITY_EDITOR || UNITY_STANDALONE
#define UNITY
using UnityEngine;
#else
using System.Text.Json;
#endif
using System.IO;
using System.Text;

namespace Triangulation
{
    public struct JsonUtils
    {
        public static T LoadFromJson<T>(string filepath)
        {
            if (File.Exists(filepath))
            {
                //using StreamReader reader = new StreamReader(filepath);
                //string json = reader.ReadToEnd();
                var data = File.ReadAllBytes(filepath);
                string json = Encoding.ASCII.GetString(data);
#if USE_NEWTONSOFT
                return JsonConvert.DeserializeObject<T>(json);
#elif UNITY
                return JsonUtility.FromJson<T>(json);
#else
                return JsonSerializer.Deserialize<T>(json);
#endif
            }
            return default;
        }

        public static void SaveToJson<T>(T serializable, string filepath, Encoding encoding)
        {
#if USE_NEWTONSOFT
            string json = JsonConvert.SerializeObject(serializable);
#elif UNITY
            string json = JsonUtility.ToJson(serializable);
#else
            string json = JsonSerializer.Serialize(serializable);
#endif
            byte[] bytes = encoding.GetBytes(json);
            File.WriteAllBytes(filepath, bytes);
        }

        public static void SaveToJson<T>(T serializable, string filepath)
        {
            SaveToJson(serializable, filepath, Encoding.ASCII);
        }
    }
}
