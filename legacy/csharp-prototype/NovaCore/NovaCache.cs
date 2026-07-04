using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NHtml = NexNova.Models;

namespace NexNova.NovaCore
{
    public class NovaCache
    {
        #region Campos
        private readonly string cacheDirectory;
        private readonly string imageCacheDirectory;
        private readonly string dataCacheDirectory;
        private readonly CacheConfig config;

        private Dictionary<string, CacheEntry> memoryCache = new Dictionary<string, CacheEntry>();
        private Dictionary<string, byte[]> imageCache = new Dictionary<string, byte[]>();
        private object cacheLock = new object();
        #endregion

        #region Constructor
        public NovaCache()
        {
            // Directorios de cache
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            cacheDirectory = Path.Combine(appData, "NexNova", "Cache");
            imageCacheDirectory = Path.Combine(cacheDirectory, "Images");
            dataCacheDirectory = Path.Combine(cacheDirectory, "Data");

            // Configuración por defecto
            config = new CacheConfig
            {
                MaxMemoryCacheSize = 50 * 1024 * 1024, // 50MB
                MaxDiskCacheSize = 500 * 1024 * 1024, // 500MB
                MaxImageCacheSize = 100 * 1024 * 1024, // 100MB
                CacheDurationHours = 24,
                EnableMemoryCache = true,
                EnableDiskCache = true,
                EnableImageCache = true
            };

            // Crear directorios
            InitializeDirectories();

            // Cargar cache del disco
            LoadDiskCache();
        }
        #endregion

        #region Inicialización
        private void InitializeDirectories()
        {
            try
            {
                if (!Directory.Exists(cacheDirectory))
                    Directory.CreateDirectory(cacheDirectory);

                if (!Directory.Exists(imageCacheDirectory))
                    Directory.CreateDirectory(imageCacheDirectory);

                if (!Directory.Exists(dataCacheDirectory))
                    Directory.CreateDirectory(dataCacheDirectory);

                // Crear archivo de configuración si no existe
                string configPath = Path.Combine(cacheDirectory, "cache_config.json");
                if (!File.Exists(configPath))
                {
                    SaveConfig();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cache directory initialization error: {ex.Message}");
            }
        }

        private void LoadDiskCache()
        {
            try
            {
                string cacheIndexPath = Path.Combine(dataCacheDirectory, "cache_index.json");
                if (File.Exists(cacheIndexPath))
                {
                    string json = File.ReadAllText(cacheIndexPath);
                    var index = JsonSerializer.Deserialize<CacheIndex>(json);

                    if (index != null && index.Entries != null)
                    {
                        // Cargar entradas válidas
                        foreach (var entry in index.Entries)
                        {
                            if (!IsExpired(entry) && memoryCache.Count < 1000) // Límite de memoria
                            {
                                memoryCache[entry.Key] = entry;
                            }
                        }

                        Console.WriteLine($"Loaded {memoryCache.Count} cache entries from disk");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Load disk cache error: {ex.Message}");
            }
        }

        private void SaveConfig()
        {
            try
            {
                string configPath = Path.Combine(cacheDirectory, "cache_config.json");
                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Save config error: {ex.Message}");
            }
        }
        #endregion

        #region Métodos Públicos - Cache General
        public string Get(string key)
        {
            if (!config.EnableMemoryCache || string.IsNullOrEmpty(key))
                return null;

            lock (cacheLock)
            {
                if (memoryCache.TryGetValue(key, out CacheEntry entry))
                {
                    if (!IsExpired(entry))
                    {
                        entry.LastAccessed = DateTime.UtcNow;
                        entry.AccessCount++;
                        return entry.Data;
                    }
                    else
                    {
                        // Eliminar entrada expirada
                        memoryCache.Remove(key);
                        RemoveFromDisk(key);
                    }
                }
            }

            return null;
        }

        public void Save(string key, string data)
        {
            if (!config.EnableMemoryCache || string.IsNullOrEmpty(key) || string.IsNullOrEmpty(data))
                return;

            lock (cacheLock)
            {
                // Verificar tamaño de cache
                if (GetTotalMemoryCacheSize() + data.Length > config.MaxMemoryCacheSize)
                {
                    CleanupMemoryCache();
                }

                var entry = new CacheEntry
                {
                    Key = key,
                    Data = data,
                    ContentType = "text/html",
                    Created = DateTime.UtcNow,
                    LastAccessed = DateTime.UtcNow,
                    AccessCount = 1,
                    Size = data.Length
                };

                memoryCache[key] = entry;

                // Guardar en disco si está habilitado
                if (config.EnableDiskCache)
                {
                    SaveToDisk(entry);
                }

                UpdateCacheIndex();
            }
        }

        public bool Contains(string key)
        {
            if (!config.EnableMemoryCache || string.IsNullOrEmpty(key))
                return false;

            lock (cacheLock)
            {
                if (memoryCache.TryGetValue(key, out CacheEntry entry))
                {
                    if (!IsExpired(entry))
                        return true;
                    else
                        memoryCache.Remove(key);
                }
            }

            return false;
        }

        public void Remove(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;

            lock (cacheLock)
            {
                memoryCache.Remove(key);
                RemoveFromDisk(key);
                UpdateCacheIndex();
            }
        }

        public void Invalidate(string key)
        {
            Remove(key);
        }

        public void Clear()
        {
            lock (cacheLock)
            {
                memoryCache.Clear();
                imageCache.Clear();

                // Limpiar disco
                if (Directory.Exists(dataCacheDirectory))
                {
                    foreach (var file in Directory.GetFiles(dataCacheDirectory))
                    {
                        try { File.Delete(file); } catch { }
                    }
                }

                if (Directory.Exists(imageCacheDirectory))
                {
                    foreach (var file in Directory.GetFiles(imageCacheDirectory))
                    {
                        try { File.Delete(file); } catch { }
                    }
                }

                UpdateCacheIndex();
            }
        }

        public void Cleanup()
        {
            lock (cacheLock)
            {
                // Limpiar entradas expiradas
                var expiredKeys = memoryCache.Where(kvp => IsExpired(kvp.Value))
                                             .Select(kvp => kvp.Key)
                                             .ToList();

                foreach (var key in expiredKeys)
                {
                    memoryCache.Remove(key);
                    RemoveFromDisk(key);
                }

                // Limpiar por tamaño
                if (GetTotalMemoryCacheSize() > config.MaxMemoryCacheSize)
                {
                    CleanupMemoryCache();
                }

                // Limpiar imágenes
                CleanupImageCache();

                UpdateCacheIndex();
            }
        }
        #endregion

        #region Métodos Públicos - Cache de Imágenes
        public async Task CacheImageAsync(string url, NovaNetwork network)
        {
            if (!config.EnableImageCache || string.IsNullOrEmpty(url) || network == null)
                return;

            // Verificar si ya está en cache
            if (HasCachedImage(url))
                return;

            try
            {
                // Descargar imagen
                byte[] imageData = await network.DownloadBytesAsync(url);
                if (imageData == null || imageData.Length == 0)
                    return;

                lock (cacheLock)
                {
                    // Verificar tamaño de cache de imágenes
                    if (GetTotalImageCacheSize() + imageData.Length > config.MaxImageCacheSize)
                    {
                        CleanupImageCache();
                    }

                    // Guardar en memoria
                    imageCache[GetImageCacheKey(url)] = imageData;

                    // Guardar en disco
                    SaveImageToDisk(url, imageData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cache image error: {ex.Message}");
            }
        }

        public byte[] GetCachedImage(string url)
        {
            if (!config.EnableImageCache || string.IsNullOrEmpty(url))
                return null;

            string cacheKey = GetImageCacheKey(url);

            lock (cacheLock)
            {
                if (imageCache.TryGetValue(cacheKey, out byte[] imageData))
                {
                    return imageData;
                }

                // Intentar cargar del disco
                imageData = LoadImageFromDisk(url);
                if (imageData != null)
                {
                    imageCache[cacheKey] = imageData;
                    return imageData;
                }
            }

            return null;
        }

        public bool HasCachedImage(string url)
        {
            if (!config.EnableImageCache || string.IsNullOrEmpty(url))
                return false;

            string cacheKey = GetImageCacheKey(url);

            lock (cacheLock)
            {
                if (imageCache.ContainsKey(cacheKey))
                    return true;

                // Verificar en disco
                string imagePath = GetImageFilePath(url);
                return File.Exists(imagePath);
            }
        }

        public void RemoveCachedImage(string url)
        {
            if (string.IsNullOrEmpty(url))
                return;

            string cacheKey = GetImageCacheKey(url);

            lock (cacheLock)
            {
                imageCache.Remove(cacheKey);

                // Eliminar del disco
                string imagePath = GetImageFilePath(url);
                if (File.Exists(imagePath))
                {
                    try { File.Delete(imagePath); } catch { }
                }
            }
        }
        #endregion

        #region Métodos Públicos - Estadísticas y Configuración
        public CacheStats GetStats()
        {
            lock (cacheLock)
            {
                return new CacheStats
                {
                    MemoryCacheEntries = memoryCache.Count,
                    MemoryCacheSize = GetTotalMemoryCacheSize(),
                    ImageCacheEntries = imageCache.Count,
                    ImageCacheSize = GetTotalImageCacheSize(),
                    DiskCacheSize = GetDiskCacheSize(),
                    TotalCacheSize = GetTotalMemoryCacheSize() + GetTotalImageCacheSize() + GetDiskCacheSize()
                };
            }
        }

        public CacheConfig GetConfig()
        {
            return config;
        }

        public void UpdateConfig(CacheConfig newConfig)
        {
            if (newConfig == null)
                return;

            lock (cacheLock)
            {
                config.MaxMemoryCacheSize = newConfig.MaxMemoryCacheSize;
                config.MaxDiskCacheSize = newConfig.MaxDiskCacheSize;
                config.MaxImageCacheSize = newConfig.MaxImageCacheSize;
                config.CacheDurationHours = newConfig.CacheDurationHours;
                config.EnableMemoryCache = newConfig.EnableMemoryCache;
                config.EnableDiskCache = newConfig.EnableDiskCache;
                config.EnableImageCache = newConfig.EnableImageCache;

                SaveConfig();

                // Aplicar nuevos límites
                if (GetTotalMemoryCacheSize() > config.MaxMemoryCacheSize)
                    CleanupMemoryCache();

                if (GetTotalImageCacheSize() > config.MaxImageCacheSize)
                    CleanupImageCache();
            }
        }
        #endregion

        #region Métodos Privados - Gestión de Cache
        private bool IsExpired(CacheEntry entry)
        {
            if (entry == null)
                return true;

            TimeSpan age = DateTime.UtcNow - entry.Created;
            return age.TotalHours > config.CacheDurationHours;
        }

        private long GetTotalMemoryCacheSize()
        {
            return memoryCache.Values.Sum(entry => entry.Size);
        }

        private long GetTotalImageCacheSize()
        {
            return imageCache.Values.Sum(data => data.Length);
        }

        private long GetDiskCacheSize()
        {
            try
            {
                if (!Directory.Exists(cacheDirectory))
                    return 0;

                long size = 0;
                foreach (var file in Directory.GetFiles(cacheDirectory, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        size += new FileInfo(file).Length;
                    }
                    catch { }
                }

                return size;
            }
            catch
            {
                return 0;
            }
        }

        private void CleanupMemoryCache()
        {
            // Ordenar por último acceso (los menos usados primero)
            var entries = memoryCache.Values.OrderBy(e => e.LastAccessed).ToList();
            long currentSize = GetTotalMemoryCacheSize();

            foreach (var entry in entries)
            {
                if (currentSize <= config.MaxMemoryCacheSize * 0.8) // Dejar 20% de margen
                    break;

                memoryCache.Remove(entry.Key);
                currentSize -= entry.Size;
            }
        }

        private void CleanupImageCache()
        {
            // Para imágenes, simplemente limpiar todas si excedemos el límite
            if (GetTotalImageCacheSize() > config.MaxImageCacheSize)
            {
                imageCache.Clear();

                // Limpiar directorio de imágenes
                if (Directory.Exists(imageCacheDirectory))
                {
                    foreach (var file in Directory.GetFiles(imageCacheDirectory))
                    {
                        try { File.Delete(file); } catch { }
                    }
                }
            }
        }
        #endregion

        #region Métodos Privados - Gestión de Disco
        private void SaveToDisk(CacheEntry entry)
        {
            try
            {
                string filePath = GetCacheFilePath(entry.Key);
                string json = JsonSerializer.Serialize(entry);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Save to disk error: {ex.Message}");
            }
        }

        private void RemoveFromDisk(string key)
        {
            try
            {
                string filePath = GetCacheFilePath(key);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Remove from disk error: {ex.Message}");
            }
        }

        private void SaveImageToDisk(string url, byte[] imageData)
        {
            try
            {
                string filePath = GetImageFilePath(url);
                File.WriteAllBytes(filePath, imageData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Save image to disk error: {ex.Message}");
            }
        }

        private byte[] LoadImageFromDisk(string url)
        {
            try
            {
                string filePath = GetImageFilePath(url);
                if (File.Exists(filePath))
                {
                    return File.ReadAllBytes(filePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Load image from disk error: {ex.Message}");
            }

            return null;
        }

        private void UpdateCacheIndex()
        {
            try
            {
                var index = new CacheIndex
                {
                    LastUpdated = DateTime.UtcNow,
                    Entries = memoryCache.Values.ToList()
                };

                string indexPath = Path.Combine(dataCacheDirectory, "cache_index.json");
                string json = JsonSerializer.Serialize(index, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(indexPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update cache index error: {ex.Message}");
            }
        }

        private string GetCacheFilePath(string key)
        {
            // Usar hash de la key como nombre de archivo
            string hash = GetHash(key);
            return Path.Combine(dataCacheDirectory, $"{hash}.cache");
        }

        private string GetImageFilePath(string url)
        {
            string hash = GetHash(url);
            return Path.Combine(imageCacheDirectory, $"{hash}.img");
        }

        private string GetImageCacheKey(string url)
        {
            return $"image_{GetHash(url)}";
        }

        private string GetHash(string input)
        {
            // Hash simple para nombres de archivo
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
        #endregion
    }

    #region Clases de Soporte
    public class CacheEntry
    {
        public string Key { get; set; }
        public string Data { get; set; }
        public string ContentType { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastAccessed { get; set; }
        public int AccessCount { get; set; }
        public long Size { get; set; }
    }

    public class CacheIndex
    {
        public DateTime LastUpdated { get; set; }
        public List<CacheEntry> Entries { get; set; } = new List<CacheEntry>();
    }

    public class CacheConfig
    {
        public long MaxMemoryCacheSize { get; set; } = 50 * 1024 * 1024; // 50MB
        public long MaxDiskCacheSize { get; set; } = 500 * 1024 * 1024; // 500MB
        public long MaxImageCacheSize { get; set; } = 100 * 1024 * 1024; // 100MB
        public int CacheDurationHours { get; set; } = 24;
        public bool EnableMemoryCache { get; set; } = true;
        public bool EnableDiskCache { get; set; } = true;
        public bool EnableImageCache { get; set; } = true;
    }

    public class CacheStats
    {
        public int MemoryCacheEntries { get; set; }
        public long MemoryCacheSize { get; set; }
        public int ImageCacheEntries { get; set; }
        public long ImageCacheSize { get; set; }
        public long DiskCacheSize { get; set; }
        public long TotalCacheSize { get; set; }

        public string GetFormattedStats()
        {
            return $"Memory: {MemoryCacheEntries} entries ({FormatSize(MemoryCacheSize)})\n" +
                   $"Images: {ImageCacheEntries} entries ({FormatSize(ImageCacheSize)})\n" +
                   $"Disk: {FormatSize(DiskCacheSize)}\n" +
                   $"Total: {FormatSize(TotalCacheSize)}";
        }

        private string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }
    }
    #endregion
}