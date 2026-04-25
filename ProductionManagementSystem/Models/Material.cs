using System.ComponentModel.DataAnnotations;

namespace ProductionManagementSystem.Models
{
    public class Material
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Наименование материала обязательно")]
        [Display(Name = "Наименование")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Количество")]
        public decimal Quantity { get; set; }

        [Display(Name = "Единица измерения")]
        public string UnitOfMeasure { get; set; } = "шт";

        [Display(Name = "Минимальный запас")]
        public decimal MinimalStock { get; set; }

        // Навигационные свойства
        public ICollection<ProductMaterial> ProductMaterials { get; set; } = new List<ProductMaterial>();
    }
}