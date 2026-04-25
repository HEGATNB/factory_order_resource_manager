using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionManagementSystem.Models;

namespace ProductionManagementSystem.Controllers
{
    [Route("api/orders")]
    [ApiController]
    public class OrdersApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrdersApiController> _logger;

        public OrdersApiController(ApplicationDbContext context, ILogger<OrdersApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET /api/orders?status=active&date=today
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetOrders(
            [FromQuery] string? status = null,
            [FromQuery] string? date = null)
        {
            var query = _context.WorkOrders
                .Include(w => w.Product)
                .Include(w => w.ProductionLine)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                var statuses = status.Split(',', StringSplitOptions.RemoveEmptyEntries);
                query = query.Where(w => statuses.Contains(w.Status));
            }

            if (date == "today")
            {
                var today = DateTime.Today;
                query = query.Where(w => w.StartDate.Date == today);
            }

            var orders = await query
                .OrderByDescending(w => w.StartDate)
                .Select(w => new
                {
                    id = w.Id,
                    productId = w.ProductId,
                    productName = w.Product != null ? w.Product.Name : "Unknown",
                    quantity = w.Quantity,
                    status = w.Status,
                    startDate = w.StartDate,
                    estimatedEndDate = w.EstimatedEndDate,
                    progressPercent = w.ProgressPercent,
                    productionLineName = w.ProductionLine != null ? w.ProductionLine.Name : null,
                    productionLineId = w.ProductionLineId
                })
                .ToListAsync();

            return Ok(orders);
        }

        // POST /api/orders
        [HttpPost]
        public async Task<ActionResult> CreateOrder([FromBody] OrderCreateModel model)
        {
            if (model.ProductId <= 0)
                return BadRequest(new { message = "Укажите продукт" });

            if (model.Quantity <= 0)
                return BadRequest(new { message = "Количество должно быть больше нуля" });

            var product = await _context.Products.FindAsync(model.ProductId);
            if (product == null)
                return NotFound(new { message = "Продукт не найден" });

            // Проверка материалов (контроль материалов)
            var requiredMaterials = await _context.ProductMaterials
                .Include(pm => pm.Material)
                .Where(pm => pm.ProductId == model.ProductId)
                .ToListAsync();

            foreach (var pm in requiredMaterials)
            {
                var totalNeeded = pm.QuantityNeeded * model.Quantity;
                if (pm.Material != null && pm.Material.Quantity < totalNeeded)
                {
                    return BadRequest(new
                    {
                        message = $"Недостаточно материала '{pm.Material.Name}'. " +
                                  $"Требуется: {totalNeeded} {pm.Material.UnitOfMeasure}, " +
                                  $"в наличии: {pm.Material.Quantity} {pm.Material.UnitOfMeasure}"
                    });
                }
            }

            float efficiencyFactor = 1.0f;
            if (model.LineId.HasValue)
            {
                var line = await _context.ProductionLines.FindAsync(model.LineId.Value);
                if (line == null)
                    return NotFound(new { message = "Производственная линия не найдена" });

                if (line.Status != "Active")
                    return BadRequest(new { message = "Выбранная линия не активна" });

                if (line.CurrentWorkOrderId != null)
                    return BadRequest(new { message = "Выбранная линия уже занята другим заказом" });

                efficiencyFactor = line.EfficiencyFactor;
            }

            // Расчет времени: (Количество × ВремяНаЕдиницу) / КоэффЭффективности
            var totalMinutes = (model.Quantity * product.ProductionTimePerUnit) / efficiencyFactor;

            var order = new WorkOrder
            {
                ProductId = model.ProductId,
                ProductionLineId = model.LineId,
                Quantity = model.Quantity,
                StartDate = DateTime.Now,
                EstimatedEndDate = DateTime.Now.AddMinutes(totalMinutes),
                Status = "Pending",
                ProgressPercent = 0
            };

            _context.WorkOrders.Add(order);

            // Если указана линия, сразу назначаем заказ на неё
            if (model.LineId.HasValue)
            {
                var line = await _context.ProductionLines.FindAsync(model.LineId.Value);
                if (line != null && line.Status == "Active" && line.CurrentWorkOrderId == null)
                {
                    line.CurrentWorkOrderId = order.Id;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Order {order.Id} created for product {product.Name}, quantity: {order.Quantity}");

            return CreatedAtAction(nameof(GetOrders), new { id = order.Id }, new
            {
                id = order.Id,
                productId = order.ProductId,
                productName = product.Name,
                quantity = order.Quantity,
                status = order.Status,
                startDate = order.StartDate,
                estimatedEndDate = order.EstimatedEndDate,
                progressPercent = order.ProgressPercent,
                productionLineId = order.ProductionLineId
            });
        }

        // PUT /api/orders/{id}/start - запуск заказа в производство
        [HttpPut("{id}/start")]
        public async Task<ActionResult> StartOrder(int id)
        {
            var order = await _context.WorkOrders
                .Include(w => w.Product)
                .Include(w => w.ProductionLine)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (order == null)
                return NotFound(new { message = "Заказ не найден" });

            if (order.Status != "Pending")
                return BadRequest(new { message = "Можно запустить только ожидающий заказ" });

            if (order.Product == null)
                return BadRequest(new { message = "Продукт не найден" });

            // Проверяем и назначаем линию
            if (order.ProductionLineId.HasValue)
            {
                var line = await _context.ProductionLines.FindAsync(order.ProductionLineId.Value);
                if (line == null)
                    return BadRequest(new { message = "Производственная линия не найдена" });

                if (line.Status != "Active")
                    return BadRequest(new { message = "Производственная линия не активна" });

                if (line.CurrentWorkOrderId != null && line.CurrentWorkOrderId != order.Id)
                    return BadRequest(new { message = "Производственная линия занята другим заказом" });

                line.CurrentWorkOrderId = order.Id;
            }
            else
            {
                // Если линия не указана, пытаемся найти свободную
                var availableLine = await _context.ProductionLines
                    .FirstOrDefaultAsync(l => l.Status == "Active" && l.CurrentWorkOrderId == null);

                if (availableLine != null)
                {
                    order.ProductionLineId = availableLine.Id;
                    availableLine.CurrentWorkOrderId = order.Id;
                }
            }

            order.Status = "InProgress";
            order.StartDate = DateTime.Now;

            // Пересчитываем время завершения
            if (order.ProductionLine != null)
            {
                var totalMinutes = (order.Quantity * order.Product.ProductionTimePerUnit) / order.ProductionLine.EfficiencyFactor;
                order.EstimatedEndDate = DateTime.Now.AddMinutes(totalMinutes);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Order {order.Id} started. Line: {order.ProductionLineId}");

            return Ok(new
            {
                id = order.Id,
                status = order.Status,
                productionLineId = order.ProductionLineId,
                estimatedEndDate = order.EstimatedEndDate,
                message = "Заказ запущен в производство"
            });
        }

        // PUT /api/orders/{id}/pause
        [HttpPut("{id}/pause")]
        public async Task<ActionResult> PauseOrder(int id)
        {
            var order = await _context.WorkOrders
                .Include(w => w.ProductionLine)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (order == null)
                return NotFound(new { message = "Заказ не найден" });

            if (order.Status != "InProgress")
                return BadRequest(new { message = "Можно приостановить только выполняющийся заказ" });

            // Переводим заказ в статус "Paused"
            order.Status = "Paused";

            // Линия остаётся назначенной, но производство остановлено
            // Фоновая служба не будет обновлять прогресс для этого заказа

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Order {order.Id} paused on line {order.ProductionLineId}");

            return Ok(new
            {
                id = order.Id,
                status = order.Status,
                progressPercent = order.ProgressPercent,
                productionLineId = order.ProductionLineId,
                message = "Производство приостановлено. Заказ остаётся на линии."
            });
        }

        // PUT /api/orders/{id}/resume
        [HttpPut("{id}/resume")]
        public async Task<ActionResult> ResumeOrder(int id)
        {
            var order = await _context.WorkOrders
                .Include(w => w.Product)
                .Include(w => w.ProductionLine)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (order == null)
                return NotFound(new { message = "Заказ не найден" });

            if (order.Status != "Paused")
                return BadRequest(new { message = "Можно возобновить только приостановленный заказ" });

            // Проверяем, что линия всё ещё доступна
            if (order.ProductionLine != null)
            {
                if (order.ProductionLine.Status != "Active")
                    return BadRequest(new { message = "Производственная линия не активна. Запустите линию." });

                if (order.ProductionLine.CurrentWorkOrderId != null && 
                    order.ProductionLine.CurrentWorkOrderId != order.Id)
                    return BadRequest(new { message = "На линии уже выполняется другой заказ" });
            }

            // Возобновляем производство
            order.Status = "InProgress";

            // Пересчитываем оставшееся время
            if (order.Product != null && order.ProductionLine != null)
            {
                var remainingQuantity = order.Quantity * (100 - order.ProgressPercent) / 100.0;
                var totalMinutes = (remainingQuantity * order.Product.ProductionTimePerUnit) / order.ProductionLine.EfficiencyFactor;
                order.EstimatedEndDate = DateTime.Now.AddMinutes(totalMinutes);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Order {order.Id} resumed on line {order.ProductionLineId}");

            return Ok(new
            {
                id = order.Id,
                status = order.Status,
                estimatedEndDate = order.EstimatedEndDate,
                message = "Производство возобновлено."
            });
        }

        // PUT /api/orders/{id}/cancel
        [HttpPut("{id}/cancel")]
        public async Task<ActionResult> CancelOrder(int id)
        {
            var order = await _context.WorkOrders
                .Include(w => w.ProductionLine)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (order == null)
                return NotFound(new { message = "Заказ не найден" });

            if (order.Status == "Completed")
                return BadRequest(new { message = "Нельзя отменить завершённый заказ" });

            if (order.Status == "Cancelled")
                return BadRequest(new { message = "Заказ уже отменён" });

            order.Status = "Cancelled";

            // Освобождаем производственную линию
            if (order.ProductionLineId.HasValue)
            {
                var line = await _context.ProductionLines.FindAsync(order.ProductionLineId.Value);
                if (line != null && line.CurrentWorkOrderId == order.Id)
                {
                    line.CurrentWorkOrderId = null;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Order {order.Id} cancelled");

            return Ok(new
            {
                id = order.Id,
                status = order.Status,
                message = "Заказ отменён. Линия освобождена."
            });
        }

        // PUT /api/orders/{id}/changeline - смена производственной линии
        [HttpPut("{id}/changeline")]
        public async Task<ActionResult> ChangeProductionLine(int id, [FromBody] ChangeLineModel model)
        {
            if (model.NewLineId <= 0)
                return BadRequest(new { message = "Укажите корректный ID линии" });

            var order = await _context.WorkOrders
                .Include(w => w.Product)
                .Include(w => w.ProductionLine)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (order == null)
                return NotFound(new { message = "Заказ не найден" });

            if (order.Status == "Completed")
                return BadRequest(new { message = "Нельзя изменить линию для завершённого заказа" });

            if (order.Status == "Cancelled")
                return BadRequest(new { message = "Нельзя изменить линию для отменённого заказа" });

            var newLine = await _context.ProductionLines.FindAsync(model.NewLineId);
            if (newLine == null)
                return NotFound(new { message = "Производственная линия не найдена" });

            if (newLine.Status != "Active")
                return BadRequest(new { message = "Выбранная линия не активна" });

            if (newLine.CurrentWorkOrderId != null && newLine.CurrentWorkOrderId != order.Id)
                return BadRequest(new { message = "Выбранная линия уже занята другим заказом" });

            // Освобождаем старую линию
            if (order.ProductionLineId.HasValue)
            {
                var oldLine = await _context.ProductionLines.FindAsync(order.ProductionLineId.Value);
                if (oldLine != null && oldLine.CurrentWorkOrderId == order.Id)
                {
                    oldLine.CurrentWorkOrderId = null;
                }
            }

            // Назначаем новую линию
            var previousLineId = order.ProductionLineId;
            order.ProductionLineId = model.NewLineId;
            newLine.CurrentWorkOrderId = order.Id;

            // Если заказ был в работе, пересчитываем время завершения с новой эффективностью
            if (order.Status == "InProgress" && order.Product != null)
            {
                var remainingQuantity = order.Quantity * (100 - order.ProgressPercent) / 100.0;
                var totalMinutes = (remainingQuantity * order.Product.ProductionTimePerUnit) / newLine.EfficiencyFactor;
                order.EstimatedEndDate = DateTime.Now.AddMinutes(totalMinutes);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Order {order.Id} moved from line {previousLineId} to line {model.NewLineId}");

            return Ok(new
            {
                id = order.Id,
                productionLineId = order.ProductionLineId,
                estimatedEndDate = order.EstimatedEndDate,
                message = $"Заказ перенесён на линию '{newLine.Name}'"
            });
        }

        // GET /api/orders/{id}/details
        [HttpGet("{id}/details")]
        public async Task<ActionResult> GetOrderDetails(int id)
        {
            var order = await _context.WorkOrders
                .Include(w => w.Product)
                .Include(w => w.ProductionLine)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (order == null)
                return NotFound(new { message = "Заказ не найден" });

            var materials = await _context.ProductMaterials
                .Include(pm => pm.Material)
                .Where(pm => pm.ProductId == order.ProductId)
                .Select(pm => new
                {
                    materialId = pm.MaterialId,
                    materialName = pm.Material != null ? pm.Material.Name : "Unknown",
                    requiredPerUnit = pm.QuantityNeeded,
                    totalRequired = pm.QuantityNeeded * order.Quantity,
                    availableQuantity = pm.Material != null ? pm.Material.Quantity : 0,
                    deficit = Math.Max(0, (pm.QuantityNeeded * order.Quantity) - (pm.Material != null ? pm.Material.Quantity : 0)),
                    unit = pm.Material != null ? pm.Material.UnitOfMeasure : "шт"
                })
                .ToListAsync();

            return Ok(new
            {
                id = order.Id,
                productId = order.ProductId,
                productName = order.Product?.Name ?? "Unknown",
                productDescription = order.Product?.Description,
                productCategory = order.Product?.Category,
                quantity = order.Quantity,
                status = order.Status,
                startDate = order.StartDate,
                estimatedEndDate = order.EstimatedEndDate,
                progressPercent = order.ProgressPercent,
                productionLineId = order.ProductionLineId,
                productionLineName = order.ProductionLine?.Name,
                productionLineEfficiency = order.ProductionLine?.EfficiencyFactor,
                materials,
                canStart = order.Status == "Pending",
                canPause = order.Status == "InProgress",
                canResume = order.Status == "Paused",
                canCancel = order.Status != "Completed" && order.Status != "Cancelled"
            });
        }

        // GET /api/orders/statistics - статистика по заказам
        [HttpGet("statistics")]
        public async Task<ActionResult> GetOrderStatistics()
        {
            var totalOrders = await _context.WorkOrders.CountAsync();
            var pendingOrders = await _context.WorkOrders.CountAsync(w => w.Status == "Pending");
            var inProgressOrders = await _context.WorkOrders.CountAsync(w => w.Status == "InProgress");
            var pausedOrders = await _context.WorkOrders.CountAsync(w => w.Status == "Paused");
            var completedOrders = await _context.WorkOrders.CountAsync(w => w.Status == "Completed");
            var cancelledOrders = await _context.WorkOrders.CountAsync(w => w.Status == "Cancelled");

            var averageProgress = await _context.WorkOrders
                .Where(w => w.Status == "InProgress" || w.Status == "Paused")
                .AverageAsync(w => (double?)w.ProgressPercent) ?? 0;

            return Ok(new
            {
                totalOrders,
                pendingOrders,
                inProgressOrders,
                pausedOrders,
                completedOrders,
                cancelledOrders,
                averageProgress = Math.Round(averageProgress, 1),
                completionRate = totalOrders > 0 
                    ? Math.Round((double)completedOrders / totalOrders * 100, 1) 
                    : 0
            });
        }

        [HttpPut("{id}/removeline")]
        public async Task<ActionResult> RemoveOrderFromLine(int id)
        {
            var order = await _context.WorkOrders
                .Include(w => w.ProductionLine)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (order == null)
                return NotFound(new { message = "Заказ не найден" });

            if (order.Status == "Completed")
                return BadRequest(new { message = "Нельзя убрать с линии завершённый заказ" });

            if (order.Status == "Cancelled")
                return BadRequest(new { message = "Нельзя убрать с линии отменённый заказ" });

            // Сохраняем ID линии для логирования
            var previousLineId = order.ProductionLineId;

            // Освобождаем линию
            if (order.ProductionLineId.HasValue)
            {
                var line = await _context.ProductionLines.FindAsync(order.ProductionLineId.Value);
                if (line != null && line.CurrentWorkOrderId == order.Id)
                {
                    line.CurrentWorkOrderId = null;
                }
            }

            // Убираем заказ с линии и меняем статус на Pending
            order.ProductionLineId = null;
            
            // Если заказ был на паузе или в работе, возвращаем в ожидание
            if (order.Status == "InProgress" || order.Status == "Paused")
            {
                order.Status = "Pending";
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Order {order.Id} removed from line {previousLineId}");

            return Ok(new
            {
                id = order.Id,
                status = order.Status,
                productionLineId = order.ProductionLineId,
                message = $"Заказ #{order.Id} убран с линии и возвращён в статус ожидания"
            });
        }
    }

    // Вспомогательные классы моделей запросов

    public class OrderCreateModel
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public int? LineId { get; set; }
    }

    public class ProgressUpdateModel
    {
        public int Percent { get; set; }
    }

    public class ChangeLineModel
    {
        public int NewLineId { get; set; }
    }
}