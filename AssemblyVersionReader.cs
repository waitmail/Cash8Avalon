using System;
using System.Diagnostics;
using System.IO;
namespace Cash8Avalon
{

    public static class AssemblyVersionReader
    {
        /// <summary>
        /// Получить версию продукта (InformationalVersion) через FileVersionInfo
        /// </summary>
        /// <param name="assemblyPath">Путь к .exe или .dll файлу</param>
        /// <returns>Значение InformationalVersion или null</returns>
        public static string GetProductVersion(string assemblyPath)
        {
            if (!File.Exists(assemblyPath))
                return null;

            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(assemblyPath);
                return versionInfo.ProductVersion; // Содержит InformationalVersion
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Получить время сборки из ProductVersion (если содержит Unix timestamp)
        /// </summary>
        /// <param name="assemblyPath">Путь к .exe или .dll файлу</param>
        /// <returns>DateTime UTC или null</returns>
        public static DateTime? GetBuildTimeFromProductVersion(string assemblyPath)
        {
            var versionStr = GetProductVersion(assemblyPath);

            if (long.TryParse(versionStr, out long timestamp))
            {
                return DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
            }

            return null;
        }

        /// <summary>
        /// Получить FileVersion (всегда в формате X.Y.Z.W)
        /// </summary>
        /// <param name="assemblyPath">Путь к .exe или .dll файлу</param>
        /// <returns>FileVersion или null</returns>
        public static string GetFileVersion(string assemblyPath)
        {
            if (!File.Exists(assemblyPath))
                return null;

            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(assemblyPath);
                return versionInfo.FileVersion; // Техническая версия файла
            }
            catch
            {
                return null;
            }
        }
    }
}

//// === Пример использования ===
//class Program
//{
//    static void Main(string[] args)
//    {
//        string externalFile = @"D:\path\to\your\app.exe"; // Укажите путь к внешнему файлу

//        var productVersion = AssemblyVersionReader.GetProductVersion(externalFile);
//        var fileVersion = AssemblyVersionReader.GetFileVersion(externalFile);
//        var buildTime = AssemblyVersionReader.GetBuildTimeFromProductVersion(externalFile);

//        Console.WriteLine($"InformationalVersion (без точек): {productVersion ?? "null"}");
//        Console.WriteLine($"FileVersion (с точками): {fileVersion ?? "null"}");

//        if (buildTime.HasValue)
//        {
//            Console.WriteLine($"Дата сборки: {buildTime:yyyy-MM-dd HH:mm:ss} UTC");
//        }
//    }
//}