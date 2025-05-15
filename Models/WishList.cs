﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blink_API.Models
{
    public class WishList
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int WishlistId { get; set; }
        [Required]
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
        public virtual ICollection<WishListDetail> WishListDetails { get; set; } = new HashSet<WishListDetail>();


        //public virtual ICollection<Product> Products { get; set; } = new HashSet<Product>();
        public bool IsDeleted { get; set; } = false;


    }
}


