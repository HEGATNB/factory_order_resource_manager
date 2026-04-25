using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductionManagementSystem.Models
{
    public class ProductMaterial
    {
        [Key]
        [Column(Order = 0)]
        public int ProductId { get; set; }

        [Key]
        [Column(Order = 1)]
        public int MaterialId { get; set; }

        [Required]
        [Display(Name = "Необходимое количество")]
        public decimal QuantityNeeded { get; set; }

        // Навигационные свойства
        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        [ForeignKey(nameof(MaterialId))]
        public Material? Material { get; set; }
    }
}