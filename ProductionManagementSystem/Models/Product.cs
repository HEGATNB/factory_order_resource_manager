using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductionManagementSystem.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Наименование продукта обязательно")]
        [Display(Name = "Наименование")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [Display(Name = "Технические характеристики")]
        public string? Specifications { get; set; } // JSON

        [Display(Name = "Категория")]
        public string? Category { get; set; }

        [Display(Name = "Минимальный запас")]
        public int MinimalStock { get; set; }

        [Display(Name = "Время производства (мин/шт)")]
        public int ProductionTimePerUnit { get; set; }

        // Навигационные свойства
        public ICollection<ProductMaterial> ProductMaterials { get; set; } = new List<ProductMaterial>();
        public ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
    }
}