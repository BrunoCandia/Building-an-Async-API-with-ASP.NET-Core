using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Books.Api.Entities
{
    [Table("Books")]
    public class Book
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; }

        [MaxLength(2500)]
        public string Description { get; set; }

        /// <summary>
        /// This is the FK to the Author table
        /// </summary>
        public Guid AuthorId { get; set; }

        /// <summary>
        /// This is the navegation property
        /// </summary>
        public Author Author { get; set; }
    }
}
