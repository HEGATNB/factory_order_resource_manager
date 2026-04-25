using Microsoft.EntityFrameworkCore;
using ProductionManagementSystem.Models;

namespace ProductionManagementSystem.Services
{
    public class ProductionSimulatorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ProductionSimulatorService> _logger;
        private readonly Dictionary<int, Timer> _productionTimers = new Dictionary<int, Timer>();

        public ProductionSimulatorService(
            IServiceProvider serviceProvider,
            ILogger<ProductionSimulatorService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Production Simulator Service started.");

            // Запускаем мониторинг активных заказов каждые 2 секунды
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

                // Получаем все активные заказы (в работе, не на паузе)
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
            // Проверяем, что линия активна и заказ всё ещё на ней
            if (order.ProductionLine == null || order.ProductionLine.Status != "Active")
                return;

            // Получаем коэффициент эффективности линии
            float efficiencyFactor = order.ProductionLine.EfficiencyFactor;

            // Вычисляем прирост прогресса
            // Базовый прирост: 1% за 10 секунд при эффективности 1.0
            int baseIncrementPerMinute = 6; // 6% в минуту при эффективности 1.0
            float adjustedIncrement = baseIncrementPerMinute * efficiencyFactor * (2.0f / 60.0f); // За 2 секунды

            // Если время производства маленькое, увеличиваем скорость
            if (order.Product != null && order.Product.ProductionTimePerUnit < 30)
            {
                adjustedIncrement *= 1.5f;
            }

            int newProgress = Math.Min(100, order.ProgressPercent + (int)Math.Ceiling(adjustedIncrement));

            // Если прогресс не изменился, пропускаем обновление
            if (newProgress == order.ProgressPercent)
                return;

            order.ProgressPercent = newProgress;

            if (order.ProgressPercent >= 100)
            {
                order.Status = "Completed";
                
                // Списываем материалы
                await ConsumeMaterials(context, order);
                
                // Освобождаем линию
                if (order.ProductionLine != null)
                {
                    order.ProductionLine.CurrentWorkOrderId = null;
                }

                _logger.LogInformation($"Order {order.Id} completed. Progress: 100%");
            }

            await context.SaveChangesAsync();
        }

        private async Task ConsumeMaterials(ApplicationDbContext context, WorkOrder order)
        {
            var productMaterials = await context.ProductMaterials
                .Include(pm => pm.Material)
                .Where(pm => pm.ProductId == order.ProductId)
                .ToListAsync();

            foreach (var pm in productMaterials)
            {
                if (pm.Material != null)
                {
                    var totalNeeded = pm.QuantityNeeded * order.Quantity;
                    pm.Material.Quantity = Math.Max(0, pm.Material.Quantity - totalNeeded);
                    
                    _logger.LogInformation(
                        $"Consumed {totalNeeded} {pm.Material.UnitOfMeasure} of {pm.Material.Name} " +
                        $"for order {order.Id}. Remaining: {pm.Material.Quantity}");
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Production Simulator Service stopping.");
            await base.StopAsync(cancellationToken);
        }
    }
}