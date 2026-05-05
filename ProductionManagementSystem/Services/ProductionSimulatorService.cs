using Microsoft.EntityFrameworkCore;
using ProductionManagementSystem.Models;

namespace ProductionManagementSystem.Services
{
    public class ProductionSimulatorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ProductionSimulatorService> _logger;

        // Храним информацию о расходе материалов для каждого заказа
        private readonly Dictionary<int, OrderConsumption> _orderConsumptions = new();

        public ProductionSimulatorService(
            IServiceProvider serviceProvider,
            ILogger<ProductionSimulatorService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        private class OrderConsumption
        {
            public int OrderId { get; set; }
            public DateTime StartTime { get; set; }
            public Dictionary<int, MaterialConsumption> Materials { get; set; } = new();
        }

        private class MaterialConsumption
        {
            public int MaterialId { get; set; }
            public decimal TotalNeeded { get; set; }
            public decimal ConsumedSoFar { get; set; }
            public decimal LastConsumedAt { get; set; } // при каком прогрессе было последнее списание
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Production Simulator Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessActiveOrders();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing active orders");
                }

                await Task.Delay(2000, stoppingToken);
            }
        }

        private async Task ProcessActiveOrders()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var activeOrders = await context.WorkOrders
                    .Include(w => w.Product)
                    .Include(w => w.ProductionLine)
                    .Where(w => w.Status == "InProgress")
                    .ToListAsync();

                foreach (var order in activeOrders)
                {
                    try
                    {
                        await UpdateOrderProgress(context, order);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error updating progress for order {order.Id}");
                    }
                }
            }
        }

        private async Task UpdateOrderProgress(ApplicationDbContext context, WorkOrder order)
        {
            if (order.ProductionLine == null || order.ProductionLine.Status != "Active")
                return;

            // Инициализируем данные о расходе, если нужно
            if (!_orderConsumptions.ContainsKey(order.Id))
            {
                await InitializeConsumption(context, order);
            }

            float efficiencyFactor = order.ProductionLine.EfficiencyFactor;

            // Время производства одной единицы в секундах
            float productionTimePerUnitSeconds = order.Product.ProductionTimePerUnit * 60f;

            // Общее время производства всего заказа в секундах
            float totalProductionTimeSeconds = productionTimePerUnitSeconds * order.Quantity;

            // Прирост прогресса за 2 секунды
            float progressIncrement = (2.0f / totalProductionTimeSeconds) * 100f * efficiencyFactor;

            if (progressIncrement < 0.05f)
                progressIncrement = 0.05f;

            int oldProgress = order.ProgressPercent;
            float newProgressFloat = oldProgress + progressIncrement;
            int newProgress = Math.Min(100, (int)Math.Floor(newProgressFloat));

            if (newProgress == oldProgress)
                return;

            // Списываем материалы за прошедший интервал
            await ConsumeMaterialsForProgress(context, order, oldProgress, newProgress);

            order.ProgressPercent = newProgress;

            if (order.ProgressPercent >= 100)
            {
                order.Status = "Completed";

                // НЕ списываем материалы - они уже все списаны!
                // Просто логируем завершение
                _logger.LogInformation($"Order {order.Id} completed at 100%. Materials already consumed during production.");

                // Освобождаем линию
                if (order.ProductionLine != null)
                {
                    order.ProductionLine.CurrentWorkOrderId = null;
                }

                // Очищаем данные о расходе
                _orderConsumptions.Remove(order.Id);
            }

            await context.SaveChangesAsync();
        }

        private async Task InitializeConsumption(ApplicationDbContext context, WorkOrder order)
        {
            var productMaterials = await context.ProductMaterials
                .Include(pm => pm.Material)
                .Where(pm => pm.ProductId == order.ProductId)
                .ToListAsync();

            var consumption = new OrderConsumption
            {
                OrderId = order.Id,
                StartTime = DateTime.Now
            };

            foreach (var pm in productMaterials)
            {
                if (pm.Material != null)
                {
                    consumption.Materials[pm.MaterialId] = new MaterialConsumption
                    {
                        MaterialId = pm.MaterialId,
                        TotalNeeded = pm.QuantityNeeded * order.Quantity,
                        ConsumedSoFar = 0,
                        LastConsumedAt = 0
                    };
                }
            }

            _orderConsumptions[order.Id] = consumption;
        }

        private async Task ConsumeMaterialsForProgress(ApplicationDbContext context, WorkOrder order, int oldProgress, int newProgress)
        {
            if (!_orderConsumptions.ContainsKey(order.Id))
                return;

            var consumption = _orderConsumptions[order.Id];
            var productMaterials = await context.ProductMaterials
                .Include(pm => pm.Material)
                .Where(pm => pm.ProductId == order.ProductId)
                .ToListAsync();

            foreach (var pm in productMaterials)
            {
                if (pm.Material == null || !consumption.Materials.ContainsKey(pm.MaterialId))
                    continue;

                var materialConsumption = consumption.Materials[pm.MaterialId];

                // Сколько всего должно быть использовано при новом прогрессе
                decimal shouldBeConsumedTotal = (decimal)newProgress / 100m * materialConsumption.TotalNeeded;

                // Сколько должно быть использовано сейчас (округляем вверх для штук)
                decimal shouldBeConsumedNow;

                if (pm.Material.UnitOfMeasure == "шт")
                {
                    // Для штук: округляем вверх (5.1 -> 6)
                    shouldBeConsumedNow = Math.Ceiling(shouldBeConsumedTotal);
                }
                else
                {
                    // Для кг, литров, метров: округляем до целых вверх (как просили)
                    shouldBeConsumedNow = Math.Ceiling(shouldBeConsumedTotal);
                }

                // Сколько еще нужно списать
                decimal needToConsume = shouldBeConsumedNow - materialConsumption.ConsumedSoFar;

                if (needToConsume > 0)
                {
                    // Списываем столько, сколько нужно, но не больше доступного
                    decimal actualConsume = Math.Min(needToConsume, pm.Material.Quantity);

                    if (actualConsume > 0)
                    {
                        pm.Material.Quantity -= actualConsume;
                        materialConsumption.ConsumedSoFar += actualConsume;

                        _logger.LogInformation(
                            $"Order #{order.Id} [{oldProgress}%->{newProgress}%]: " +
                            $"Consumed {actualConsume} {pm.Material.UnitOfMeasure} of {pm.Material.Name} " +
                            $"(total: {materialConsumption.ConsumedSoFar}/{materialConsumption.TotalNeeded}, " +
                            $"remaining stock: {pm.Material.Quantity})");
                    }
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Production Simulator Service stopping.");
            _orderConsumptions.Clear();
            await base.StopAsync(cancellationToken);
        }
    }
}