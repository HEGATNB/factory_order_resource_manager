using TouristGuide.Models;
using System;
using System.Linq;

namespace TouristGuide.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            context.Database.EnsureCreated();
            
            // Проверка наличия данных
            if (context.Cities.Any())
            {
                return;   // База данных уже инициализирована
            }
            
            var cities = new City[]
            {
                new City
                {
                    Name = "Москва",
                    Region = "Центральный федеральный округ",
                    Population = 12655050,
                    History = "Москва — столица России, город федерального значения. Основана в 1147 году князем Юрием Долгоруким. Крупнейший по численности населения город России и её субъект — 12 655 050 человек (2021), центр Московской городской агломерации.",
                    CoatOfArmsImageUrl = "/images/coats/moscow.jpg",
                    PhotoUrl = "/images/cities/moscow.jpg"
                },
                new City
                {
                    Name = "Санкт-Петербург",
                    Region = "Северо-Западный федеральный округ",
                    Population = 5384342,
                    History = "Санкт-Петербург — второй по численности населения город России. Основан 16 (27) мая 1703 года Петром I. В 1712—1918 годах — столица Российского государства.",
                    CoatOfArmsImageUrl = "/images/coats/spb.jpg",
                    PhotoUrl = "/images/cities/spb.jpg"
                },
                new City
                {
                    Name = "Казань",
                    Region = "Приволжский федеральный округ",
                    Population = 1257341,
                    History = "Казань — столица Республики Татарстан, крупный порт на левом берегу реки Волги. Один из крупнейших экономических, научных, образовательных, религиозных, культурных и спортивных центров России.",
                    CoatOfArmsImageUrl = "/images/coats/kazan.jpg",
                    PhotoUrl = "/images/cities/kazan.jpg"
                },
                new City
                {
                    Name = "Екатеринбург",
                    Region = "Уральский федеральный округ",
                    Population = 1493749,
                    History = "Екатеринбург — четвёртый по численности населения город России, административный центр Уральского федерального округа. Основан в 1723 году как железоделательный завод.",
                    CoatOfArmsImageUrl = "/images/coats/ekb.jpg",
                    PhotoUrl = "/images/cities/ekb.jpg"
                },
                new City
                {
                    Name = "Сочи",
                    Region = "Южный федеральный округ",
                    Population = 432322,
                    History = "Сочи — крупнейший курортный город России. Расположен на северо-восточном побережье Чёрного моря. В 2014 году здесь прошли XXII зимние Олимпийские игры.",
                    CoatOfArmsImageUrl = "/images/coats/sochi.jpg",
                    PhotoUrl = "/images/cities/sochi.jpg"
                }
            };
            
            foreach (var city in cities)
            {
                context.Cities.Add(city);
            }
            context.SaveChanges();
            
            var attractions = new Attraction[]
            {
                new Attraction
                {
                    Name = "Красная площадь",
                    History = "Красная площадь — главная площадь Москвы, расположенная в центре радиально-кольцевой планировки города между Кремлём и Китай-городом. С 1990 года входит в список Всемирного наследия ЮНЕСКО.",
                    PhotoUrl = "/images/attractions/red_square.jpg",
                    WorkingHours = "Круглосуточно",
                    Cost = "Бесплатно",
                    CityId = 1
                },
                new Attraction
                {
                    Name = "Московский Кремль",
                    History = "Московский Кремль — крепость в центре Москвы, древнейшая её часть, главный общественно-политический и историко-художественный комплекс города, официальная резиденция Президента Российской Федерации.",
                    PhotoUrl = "/images/attractions/kremlin.jpg",
                    WorkingHours = "10:00 - 17:00, выходной — четверг",
                    Cost = "500 рублей",
                    CityId = 1
                },
                new Attraction
                {
                    Name = "Эрмитаж",
                    History = "Государственный Эрмитаж — музей изобразительного и декоративно-прикладного искусства, расположенный в Санкт-Петербурге. Один из крупнейших художественных музеев мира.",
                    PhotoUrl = "/images/attractions/hermitage.jpg",
                    WorkingHours = "10:30 - 18:00, среда-четверг 10:30 - 21:00",
                    Cost = "500 рублей",
                    CityId = 2
                },
                new Attraction
                {
                    Name = "Исаакиевский собор",
                    History = "Исаакиевский собор — крупнейший православный храм Санкт-Петербурга. Построен в 1818—1858 годы по проекту архитектора Огюста Монферрана.",
                    PhotoUrl = "/images/attractions/isaac.jpg",
                    WorkingHours = "10:00 - 18:00, среда — выходной",
                    Cost = "350 рублей",
                    CityId = 2
                },
                new Attraction
                {
                    Name = "Казанский Кремль",
                    History = "Казанский кремль — историческая крепость и сердце Казани. С 2000 года входит в список Всемирного наследия ЮНЕСКО. На территории кремля находится мечеть Кул-Шариф.",
                    PhotoUrl = "/images/attractions/kazan_kremlin.jpg",
                    WorkingHours = "08:00 - 22:00",
                    Cost = "Бесплатно",
                    CityId = 3
                },
                new Attraction
                {
                    Name = "Храм всех религий",
                    History = "Вселенский храм — архитектурное сооружение в Казани, объединяющее элементы разных религиозных культур. Строительство начато в 1994 году художником Ильдаром Хановым.",
                    PhotoUrl = "/images/attractions/temple.jpg",
                    WorkingHours = "09:00 - 18:00",
                    Cost = "Бесплатно",
                    CityId = 3
                }
            };
            
            foreach (var attraction in attractions)
            {
                context.Attractions.Add(attraction);
            }
            context.SaveChanges();
        }
    }
}