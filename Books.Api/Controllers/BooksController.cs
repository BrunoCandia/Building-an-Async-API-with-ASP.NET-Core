using AutoMapper;
using Books.Api.Filters;
using Books.Api.Models;
using Books.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Books.Api.Controllers
{
    [Route("api/books")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private IBooksRepository _booksRepository;
        private IMapper _mapper;

        public BooksController(IBooksRepository booksRepository, IMapper mapper)
        {
            _booksRepository = booksRepository ?? throw new ArgumentNullException(nameof(booksRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [HttpGet]
        [BooksResultFilter]
        public async Task<IActionResult> GetBooks()
        {
            var bookEntities = await _booksRepository.GetBooksAsync();

            return Ok(bookEntities);
        }

        [HttpGet]
        //[BookResultFilter]
        [BookWithCoversResultFilterAttribute]
        [Route("{id}", Name = "GetBook")]
        public async Task<IActionResult> GetBook(Guid id)
        {
            var bookEntity = await _booksRepository.GetBookAsync(id);

            if (bookEntity == null)
            {
                return NotFound();
            }

            //var bookCover = await _booksRepository.GetBookCoverAsync("dummycover");
            var bookCovers = await _booksRepository.GetBooksCoverAsync(id);

            //var propertyBag = new Tuple<Entities.Book, IEnumerable<ExternalModels.BookCover>>(bookEntity, bookCovers);
            //propertyBag.Item1;
            //propertyBag.Item2;

            //(Entities.Book book, IEnumerable<ExternalModels.BookCover> bookCovers) propertyBag = (bookEntity, bookCovers);

            //return Ok(bookEntity);

            return Ok((book: bookEntity, bookCovers: bookCovers));
        }

        [HttpPost]
        [BookResultFilter]
        public async Task<IActionResult> CreateBookAsync([FromBody] BookForCreation book)
        {
            var bookEntity = _mapper.Map<Entities.Book>(book);

            _booksRepository.AddBook(bookEntity);

            await _booksRepository.SaveChangesAsync();

            //After create the book, fetch(refetch) the book from the data source, including the author.
            await _booksRepository.GetBookAsync(bookEntity.Id);            

            return CreatedAtRoute("GetBook", new { id = bookEntity.Id }, bookEntity);
        }
    }
}
