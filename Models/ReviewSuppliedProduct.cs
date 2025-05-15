﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blink_API.Models
{
    public class ReviewSuppliedProduct
    {
        [Key,DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RequestId { get; set; }
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public bool? RequestStatus { get; set; } = null;
        public string ProductName { get; set; }
        public string ProductDescription { get; set; }
        public int BrandId { get; set; }
        public int CategoryId { get; set; }
        public string SupplierId { get; set; }
        public int InventoryId { get; set; }
        public double ProductPrice { get; set; }
        public int ProductQuantity { get; set; }
        [ForeignKey("BrandId")]
        public Brand Brand { get; set; }
        [ForeignKey("CategoryId")]
        public Category Category { get; set; }
        [ForeignKey("SupplierId")]
        public ApplicationUser Supplier { get; set; }
        [ForeignKey("InventoryId")]
        public Inventory Inventory { get; set; }
        public virtual ICollection<ReviewSuppliedProductImages> ReviewSuppliedProductImages { get; set; } = new HashSet<ReviewSuppliedProductImages>();
    }
}
