using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TouristGuide.Models
{
    public class Attraction
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Название достопримечательности обязательно")]
        [Display(Name = "Название")]
        public string Name { get; set; } = string.Empty;
        
        [Display(Name = "История")]
        public string History { get; set; } = string.Empty;
        
        [Display(Name = "Фотография")]
        public string PhotoUrl { get; set; } = string.Empty;
        
        [Display(Name = "Часы работы")]
        public string WorkingHours { get; set; } = string.Empty;
        
        [Display(Name = "Стоимость посещения")]
        public string Cost { get; set; } = string.Empty;
        
        public int CityId { get; set; }
        
        [ForeignKey("CityId")]
        public City City { get; set; } = null!;
    }
}