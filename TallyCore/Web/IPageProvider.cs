﻿using System;
using System.Threading.Tasks;
using System.Threading;
using HtmlAgilityPack;

namespace NetTally
{
    public interface IPageProvider : IDisposable
    {
        /// <summary>
        /// Asynchronously load a specific page.
        /// </summary>
        /// <param name="url">The URL of the page to load.</param>
        /// <param name="shortDescrip">A short description that can be used in status updates.</param>
        /// <param name="bypassCache">Flag for whether to bypass the cache when trying to load the page.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Returns an HTML document.</returns>
        Task<HtmlDocument> GetPage(string url, string shortDescrip, Caching caching, CancellationToken token);

        /// <summary>
        /// Clear the cache of any previously loaded pages.
        /// </summary>
        void ClearPageCache();

        /// <summary>
        /// Have an event that can be watched for status messages.
        /// </summary>
        event EventHandler<MessageEventArgs> StatusChanged;
    }
}