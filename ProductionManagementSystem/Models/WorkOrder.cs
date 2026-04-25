using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductionManagementSystem.Models
{
    public class WorkOrder
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Продукт")]
        public int ProductId { get; set; }

        [Display(Name = "Производственная линия")]
        public int? ProductionLineId { get; set; }

        [Required]
        [Display(Name = "Количество")]
        public int Quantity { get; set; }

        [Required]
        [Display(Name = "Дата начала")]
        public DateTime StartDate { get; set; }

        [Display(Name = "Расчетная дата завершения")]
        public DateTime EstimatedEndDate { get; set; }

        [Required]
        [Display(Name = "Статус")]
        public string Status { get; set; } = "Pending"; // "Pending", "InProgress", "Completed", "Cancelled"

        [Display(Name = "Прогресс выполнения (%)")]
        public int ProgressPercent { get; set; } = 0;

        // Навигационные свойства
        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        [ForeignKey(nameof(ProductionLineId))]
        public ProductionLine? ProductionLine { get; set; }
    }
}