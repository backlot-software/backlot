using System;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;

namespace Backlot.Core.Services;

public interface ICacheFactory
{
    /// <summary>
    /// Gets the web cache.
    /// </summary>
    IMemoryCache Cache { get; }

    T GetWithAbsoluteExpiration<T>(string key, Func<T> exec, int durationInMinutes = 2)
        where T : class;

    T GetWithSlidingExpiration<T>(string key, Func<T> exec, int durationInMinutes = 2)
        where T : class;

    bool TryGet<T>(
        string key,
        out T cached)
        where T : class;

    /// <summary>
    /// Removes the item.
    /// </summary>
    /// <param name="key">The key.</param>
    void RemoveItem(string key);
}

public class CacheFactory : IDisposable, ICacheFactory
    {
        public CacheFactory(IMemoryCache memoryCache)
        {
            Cache = memoryCache;
        }

        //public delegate void CacheDelegate<in T>(T objectToCheck, ref bool remove);

        public IMemoryCache Cache { get; }

        public T GetWithAbsoluteExpiration<T>(string key, Func<T> exec, int durationInMinutes = 2)
            where T : class
        {
            if (string.IsNullOrWhiteSpace(key) || durationInMinutes == -1)
            {
                return exec();
            }

            if (TryGet(key, out T cached))
            {
                Trace.TraceInformation($"'{key}' taken from app cache");
                return cached;
            }

            var item = exec();

            MemoryCacheEntryOptions options = new()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(durationInMinutes)
            };

            Cache.Set(key, item, options);

            Trace.TraceInformation(
                $"Added object to the cache with absolute expiration for {durationInMinutes} minutes: '{key}'.");

            return item;
        }
        
        public T GetWithSlidingExpiration<T>(string key, Func<T> exec, int durationInMinutes = 2)
            where T : class
        {
            if (string.IsNullOrWhiteSpace(key) || durationInMinutes == -1)
            {
                return exec();
            }

            if (TryGet(key, out T cached))
            {
                Trace.TraceInformation($"'{key}' taken from app cache");
                return cached;
            }

            var item = exec();

            MemoryCacheEntryOptions options = new()
            {
                SlidingExpiration = TimeSpan.FromMinutes(durationInMinutes)
            };

            Cache.Set(key, item, options);

            Trace.TraceInformation(
                $"Added object to the cache with absolute expiration for {durationInMinutes} minutes: '{key}'.");

            return item;
        }

        public bool TryGet<T>(
            string key,
            out T cached)
            where T : class
        {
            cached = default(T);

            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            var result = Cache.TryGetValue(key, out T value);
            if (result)
            {
                cached = value;
            }

            return result;
        }

        

        public void RemoveItem(string key)
        {
            Cache.Remove(key);

            Trace.TraceInformation($"Removed an object with key: '{key}' from the cache.");
        }
        
        #region disposable
        
        private bool _disposed;

        ~CacheFactory()
        {
            // Simply call Dispose(false).
            Dispose(false);
        }

        public virtual void ReleaseManagedResources()
        {
            // This can be overriden by the concrete class.
        }

        public virtual void ReleaseUnmangedResources()
        {
            // This can be overriden by the concrete class.
        }

        public void Dispose() // Implement IDisposable
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    ReleaseManagedResources();
                }

                ReleaseUnmangedResources();
                _disposed = true;
            }
        }
        
        #endregion
    }