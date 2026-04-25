using ProductionManagementSystem.Models;

namespace ProductionManagementSystem.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            // Проверяем, есть ли уже данные
            if (context.Products.Any())
                return;

            // Материалы
            var materials = new Material[]
            {
                new Material { Name = "Сталь листовая", Quantity = 1000, UnitOfMeasure = "кг", MinimalStock = 100 },
                new Material { Name = "Алюминиевый профиль", Quantity = 500, UnitOfMeasure = "м", MinimalStock = 50 },
                new Material { Name = "Крепежные элементы", Quantity = 5000, UnitOfMeasure = "шт", MinimalStock = 500 },
                new Material { Name = "Электронные компоненты", Quantity = 200, UnitOfMeasure = "шт", MinimalStock = 50 },
                new Material { Name = "Краска порошковая", Quantity = 100, UnitOfMeasure = "кг", MinimalStock = 10 }
            };
            context.Materials.AddRange(materials);
            context.SaveChanges();

            // Продукты
            var products = new Product[]
            {
                new Product { Name = "Корпус прибора", Description = "Металлический корпус", Category = "Корпуса", ProductionTimePerUnit = 30, MinimalStock = 10 },
                new Product { Name = "Плата управления", Description = "Электронная плата", Category = "Электроника", ProductionTimePerUnit = 45, MinimalStock = 15 },
                new Product { Name = "Рама промышленная", Description = "Стальная конструкция", Category = "Конструкции", ProductionTimePerUnit = 120, MinimalStock = 5 }
            };
            context.Products.AddRange(products);
            context.SaveChanges();

            // Связи продуктов и материалов
            var productMaterials = new ProductMaterial[]
            {
                new ProductMaterial { ProductId = 1, MaterialId = 1, QuantityNeeded = 5 },
                new ProductMaterial { ProductId = 1, MaterialId = 3, QuantityNeeded = 20 },
                new ProductMaterial { ProductId = 2, MaterialId = 4, QuantityNeeded = 3 },
                new ProductMaterial { ProductId = 2, MaterialId = 3, QuantityNeeded = 10 },
                new ProductMaterial { ProductId = 3, MaterialId = 1, QuantityNeeded = 20 },
                new ProductMaterial { ProductId = 3, MaterialId = 2, QuantityNeeded = 5 }
            };
            context.ProductMaterials.AddRange(productMaterials);
            context.SaveChanges();

            // Производственные линии
            var lines = new ProductionLine[]
            {
                new ProductionLine { Name = "Линия штамповки №1", Status = "Active", EfficiencyFactor = 1.0f },
                new ProductionLine { Name = "Сборочная линия №2", Status = "Active", EfficiencyFactor = 1.2f },
                new ProductionLine { Name = "Линия покраски №3", Status = "Stopped", EfficiencyFactor = 0.8f }
            };
            context.ProductionLines.AddRange(lines);
            context.SaveChanges();

            // Заказы с разными статусами
            var orders = new WorkOrder[]
            {
                new WorkOrder 
                { 
                    ProductId = 1, 
                    ProductionLineId = 1, 
                    Quantity = 50, 
                    StartDate = DateTime.Now.AddHours(-2), 
                    EstimatedEndDate = DateTime.Now.AddHours(5),
                    Status = "InProgress",
                    ProgressPercent = 45
                },
                new WorkOrder 
                { 
                    ProductId = 2, 
                    ProductionLineId = 2, 
                    Quantity = 30, 
                    StartDate = DateTime.Now.AddHours(-1), 
                    EstimatedEndDate = DateTime.Now.AddHours(3),
                    Status = "Paused",  // Демонстрация статуса паузы
                    ProgressPercent = 25
                },
                new WorkOrder 
                { 
                    ProductId = 3, 
                    ProductionLineId = null, 
                    Quantity = 10, 
                    StartDate = DateTime.Now.AddDays(1), 
                    EstimatedEndDate = DateTime.Now.AddDays(2),
                    Status = "Pending",
                    ProgressPercent = 0
                }
            };
            context.WorkOrders.AddRange(orders);
            context.SaveChanges();

            // Обновляем текущие заказы для линий
            lines[0].CurrentWorkOrderId = 1;
            lines[1].CurrentWorkOrderId = 2;
            context.SaveChanges();
        }
    }
}