﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTally.Extensions;
using NetTally.SystemInfo;
using Nito.AsyncEx;

namespace NetTally.Cache
{
    /// <summary>
    /// Class to handle caching web content.
    /// </summary>
    public sealed class PackedStringCache : IDisposable
    {
        #region Lazy singleton creation
        static readonly Lazy<PackedStringCache> lazy = new Lazy<PackedStringCache>(() => new PackedStringCache());
        public static PackedStringCache Instance => lazy.Value;

        PackedStringCache()
        {
            SetClock(null);
        }
        #endregion

        #region Disposal
        ~PackedStringCache()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true); //I am calling you from Dispose, it's safe
            GC.SuppressFinalize(this); //Hey, GC: don't bother calling finalize later
        }

        void Dispose(bool itIsSafeToAlsoFreeManagedObjects)
        {
            if (_disposed)
                return;

            if (itIsSafeToAlsoFreeManagedObjects)
            {
                Clear();
            }

            _disposed = true;
        }
        #endregion

        #region Local fields
        bool _disposed;

        IClock Clock { get; set; }

        const int MaxCacheEntries = 100;
        Dictionary<string, CacheObject<byte[]>> GZPageCache { get; } = new Dictionary<string, CacheObject<byte[]>>(MaxCacheEntries);

        readonly AsyncReaderWriterLock cacheLock = new AsyncReaderWriterLock();
        #endregion

        #region Public functions
        public static readonly DateTime DefaultExpiration = CacheObject<byte[]>.DefaultExpiration;

        /// <summary>
        /// Allow setting the clock interface to be used by the cache.
        /// </summary>
        /// <param name="clock">The clock interface that will be used to determine timestamps.</param>
        public void SetClock(IClock clock)
        {
            using (cacheLock.ReaderLock())
            {
                Clock = clock ?? new SystemClock();
            }
        }

        /// <summary>
        /// Add the original HTML string to the cache.
        /// </summary>
        /// <param name="url">The URL the document was retrieved from.</param>
        /// <param name="html">The HTML string to cache.</param>
        public async Task AddAsync(string url, string html, DateTime expires)
        {
            var zipped = await CompressString(html);
            var toGZCache = new CacheObject<byte[]>(zipped, Clock, expires);

            using (cacheLock.WriterLock())
            {
                GZPageCache[url] = toGZCache;

                if (GZPageCache.Count > MaxCacheEntries)
                {
                    var oldestEntry = GZPageCache.MinObject(p => p.Value.Timestamp);
                    GZPageCache.Remove(oldestEntry.Key);
                }
            }
        }

        /// <summary>
        /// Try to get a cached document for a specified URL.
        /// </summary>
        /// <param name="url">The URL being checked.</param>
        /// <returns>Returns the document for the URL if it's available and less than 30 minutes old.
        /// Otherwise returns null.</returns>
        public async Task<(bool found, string content)> GetAsync(string url)
        {
            using (cacheLock.ReaderLock())
            {
                if (GZPageCache.TryGetValue(url, out CacheObject<byte[]> gzCache))
                {
                    if (gzCache.Expires > Clock.Now)
                    {
                        string content = await GetUncompressedString(gzCache.Store).ConfigureAwait(false);

                        return (true, content);
                    }
                }
            }

            return (false, string.Empty);
        }

        /// <summary>
        /// Clear the current cache.
        /// </summary>
        public void Clear()
        {
            using (cacheLock.WriterLock())
            {
                GZPageCache.Clear();
            }
        }

        /// <summary>
        /// Remove all entries that are older than our defined time limit to retain cached pages.
        /// </summary>
        /// <param name="time">The reference time to use when determining the age of a page.</param>
        public void ExpireCache(DateTime time)
        {
            using (cacheLock.WriterLock())
            {
                var pagesToRemove = GZPageCache.Where(p => p.Value.Expires < time).ToList();

                foreach (var page in pagesToRemove)
                {
                    GZPageCache.Remove(page.Key);
                }
            }
        }
        #endregion

        #region Private functions        
        /// <summary>
        /// Compresses the string.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>Returns the string compressed into a GZipped byte array.</returns>
        private async Task<byte[]> CompressString(string input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipStream zs = new GZipStream(ms, CompressionMode.Compress, true))
                {
                    byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                    await zs.WriteAsync(inputBytes, 0, inputBytes.Length).ConfigureAwait(false);
                }

                return ms.ToArray();
            }
        }

        /// <summary>
        /// Gets the uncompressed string.
        /// </summary>
        /// <param name="input">The input byte array.</param>
        /// <returns>Returns the uncompressed string.</returns>
        private async Task<string> GetUncompressedString(byte[] input)
        {
            using (MemoryStream mso = new MemoryStream())
            {
                using (MemoryStream ms = new MemoryStream(input))
                using (GZipStream zs = new GZipStream(ms, CompressionMode.Decompress, true))
                {
                    await zs.CopyToAsync(mso).ConfigureAwait(false);
                }

                return Encoding.UTF8.GetString(mso.ToArray());
            }
        }
        #endregion

    }
}