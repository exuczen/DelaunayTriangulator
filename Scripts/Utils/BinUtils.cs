using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;

namespace Triangulation
{
    public static class BinUtils
    {
        public static BinaryFormatter Formatter { get; } = new BinaryFormatter();

        public static string GetHash<T>(T data) where T : class
        {
            using var stream = new MemoryStream();
#pragma warning disable SYSLIB0011 // Type or member is obsolete
            Formatter.Serialize(stream, data);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
            using var md5 = MD5.Create();
            return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
        }

        public static void SaveToBinary<T>(T contents, string folderPath, string filename) where T : class
        {
            SaveToBinary(contents, Path.Combine(folderPath, filename));
        }

        public static void SaveToBinary<T>(T contents, string path) where T : class
        {
            using var stream = new FileStream(path, FileMode.Create);
#pragma warning disable SYSLIB0011 // Type or member is obsolete
            Formatter.Serialize(stream, contents);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
        }

        public static T LoadFromBinary<T>(string folderPath, string filename) where T : class
        {
            return LoadFromBinary<T>(Path.Combine(folderPath, filename));
        }

        public static T LoadFromBinary<T>(string path) where T : class
        {
            if (File.Exists(path))
            {
                using var stream = new FileStream(path, FileMode.Open);
#pragma warning disable SYSLIB0011 // Type or member is obsolete
                return Formatter.Deserialize(stream) as T;
#pragma warning restore SYSLIB0011 // Type or member is obsolete
            }
            else
            {
                return null;
            }
        }
    }
}
