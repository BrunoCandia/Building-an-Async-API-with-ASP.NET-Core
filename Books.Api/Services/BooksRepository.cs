using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Books.Api.Contexts;
using Books.Api.Entities;
using Books.Api.ExternalModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Books.Api.Services
{
    public class BooksRepository : IBooksRepository
    {
        private BooksContext _context;
        private IHttpClientFactory _httpClientFactory;
        private ILogger _logger;
        private CancellationTokenSource _cancellationTokenSource;

        public BooksRepository(BooksContext context, IHttpClientFactory httpClientFactory, ILogger<BooksRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Book> GetBookAsync(Guid id)
        {
            return await _context.Books
                .Include(b => b.Author)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<IEnumerable<Book>> GetBooksAsync()
        {
            return await _context.Books
                .Include(b => b.Author)
                .ToListAsync();
        }

        public async Task<IEnumerable<Book>> GetBooksAsync(IEnumerable<Guid> bookIds)
        {
            return await _context.Books                
                .Where(b => bookIds.Contains(b.Id))
                .Include(b => b.Author)
                .ToListAsync();
        }

        public async Task<BookCover> GetBookCoverAsync(string coverId)
        {
            // From Asp.Net Core 2.1 use IHttpClientFactory instead of this
            //var httpClient = new HttpClient();

            var httpClient = _httpClientFactory.CreateClient();

            var response = await httpClient.GetAsync($"http://localhost:52644/api/bookcovers/{coverId}");

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<BookCover>(await response.Content.ReadAsStringAsync());
            }

            return null;
        }

        public async Task<IEnumerable<BookCover>> GetBooksCoverAsync(Guid bookId)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var bookCovers = new List<BookCover>();

            _cancellationTokenSource = new CancellationTokenSource(); 

            // Create alist of fake bookcover
            var bookCoverUrls = new[]
            {
                $"http://localhost:52644/api/bookcovers/{bookId}-dummycover1",
                $"http://localhost:52644/api/bookcovers/{bookId}-dummycover2",
                $"http://localhost:52644/api/bookcovers/{bookId}-dummycover3",
                $"http://localhost:52644/api/bookcovers/{bookId}-dummycover4",
                $"http://localhost:52644/api/bookcovers/{bookId}-dummycover5"
            };

            //foreach (var bookCoverUrl in bookCoverUrls)
            //{
            //    var response = await httpClient.GetAsync(bookCoverUrl);

            //    if (response.IsSuccessStatusCode)
            //    {
            //        bookCovers.Add(JsonConvert.DeserializeObject<BookCover>(await response.Content.ReadAsStringAsync()));
            //    }
            //}            

            //Create the tasks
            var downloadBookCoverTaskQuery = from bookCoverUrl
                                             in bookCoverUrls
                                             select DownloadBookCoverAsync(httpClient, bookCoverUrl, _cancellationTokenSource.Token);

            //Start the tasks
            var downloadBookCoverTask = downloadBookCoverTaskQuery.ToList();

            //return bookCovers;

            try
            {
                return await Task.WhenAll(downloadBookCoverTask);
            }
            catch (OperationCanceledException operationCanceledException)
            {
                _logger.LogInformation($"{operationCanceledException.Message}");

                foreach (var task in downloadBookCoverTask)
                {
                    _logger.LogInformation($"Task {task.Id} has status {task.Status}");
                }

                return new List<BookCover>();
            }
            catch (Exception exception)
            {
                _logger.LogInformation($"{exception.Message}");
                throw;
            }
        }

        private async Task<BookCover> DownloadBookCoverAsync(HttpClient httpClient, string bookCoverUrl, CancellationToken cancellationToken)
        {                        
            var response = await httpClient.GetAsync(bookCoverUrl, cancellationToken);

            //cancellationToken.ThrowIfCancellationRequested();

            if (response.IsSuccessStatusCode)
            {
                var bookCover =  JsonConvert.DeserializeObject<BookCover>(await response.Content.ReadAsStringAsync());

                return bookCover;
            }

            _cancellationTokenSource.Cancel();

            return null;
        }

        public IEnumerable<Book> GetBooks()
        {
            return _context.Books
                .Include(b => b.Author)
                .ToList();
        }

        public async Task AddBookAsync(Book bookToAdd)
        {
            if (bookToAdd == null)
            {
                throw new ArgumentNullException(nameof(bookToAdd));
            }

            await _context.AddAsync(bookToAdd);
        }

        public void AddBook(Book bookToAdd)
        {
            if (bookToAdd == null)
            {
                throw new ArgumentNullException(nameof(bookToAdd));
            }

            _context.Add(bookToAdd);
        }

        public async Task<bool> SaveChangesAsync()
        {
            // retur true if 1 or more entities were saved
            return await _context.SaveChangesAsync() > 0;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_context != null)
                {
                    _context.Dispose();
                    _context = null;
                }
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = null;
                }
            }
        }        
    }
}
