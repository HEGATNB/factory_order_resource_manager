using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductionManagementSystem.Models
{
    public class ProductionLine
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Название линии")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Статус")]
        public string Status { get; set; } = "Active"; // "Active" или "Stopped"

        [Display(Name = "Коэффициент эффективности")]
        [Range(0.5, 2.0, ErrorMessage = "Коэффициент должен быть от 0.5 до 2.0")]
        public float EfficiencyFactor { get; set; } = 1.0f;

        [Display(Name = "Текущий заказ")]
        public int? CurrentWorkOrderId { get; set; }

        // Навигационные свойства
        [ForeignKey(nameof(CurrentWorkOrderId))]
        public WorkOrder? CurrentWorkOrder { get; set; }
        public ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
    }
}