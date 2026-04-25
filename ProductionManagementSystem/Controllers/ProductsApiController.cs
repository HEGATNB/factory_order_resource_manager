using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionManagementSystem.Models;

namespace ProductionManagementSystem.Controllers
{
    [Route("api/products")]
    [ApiController]
    public class ProductsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET /api/products?category={category}
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetProducts([FromQuery] string? category = null)
        {
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(p => p.Category == category);
            }

            var products = await query
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    description = p.Description,
                    specifications = p.Specifications,
                    category = p.Category,
                    minimalStock = p.MinimalStock,
                    productionTimePerUnit = p.ProductionTimePerUnit
                })
                .ToListAsync();

            return Ok(products);
        }

        // GET /api/products/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductMaterials)
                .ThenInclude(pm => pm.Material)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound(new { message = "Продукт не найден" });

            return Ok(new
            {
                id = product.Id,
                name = product.Name,
                productionTimePerUnit = product.ProductionTimePerUnit,
                category = product.Category,
                minimalStock = product.MinimalStock,
                materials = product.ProductMaterials.Select(pm => new
                {
                    materialId = pm.MaterialId,
                    name = pm.Material?.Name,
                    quantityNeeded = pm.QuantityNeeded,
                    unitOfMeasure = pm.Material?.UnitOfMeasure
                })
            });
        }

        // GET /api/products/{id}/materials
        [HttpGet("{id}/materials")]
        public async Task<ActionResult> GetProductMaterials(int id)
        {
            var productMaterials = await _context.ProductMaterials
                .Include(pm => pm.Material)
                .Where(pm => pm.ProductId == id)
                .Select(pm => new
                {
                    materialId = pm.MaterialId,
                    name = pm.Material != null ? pm.Material.Name : "Unknown",
                    quantityNeeded = pm.QuantityNeeded,
                    unitOfMeasure = pm.Material != null ? pm.Material.UnitOfMeasure : "шт",
                    availableQuantity = pm.Material != null ? pm.Material.Quantity : 0
                })
                .ToListAsync();

            if (!productMaterials.Any())
                return Ok(new List<object>());

            return Ok(productMaterials);
        }

        // POST /api/products
        [HttpPost]
        public async Task<ActionResult> CreateProduct([FromBody] ProductCreateUpdateModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { message = "Название продукта обязательно" });

            if (model.ProdTime <= 0)
                return BadRequest(new { message = "Время производства должно быть больше 0" });

            var product = new Product
            {
                Name = model.Name,
                ProductionTimePerUnit = model.ProdTime,
                Category = model.Category ?? "",
                MinimalStock = model.MinimalStock >= 0 ? model.MinimalStock : 0
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Добавляем материалы если есть
            if (model.Materials != null && model.Materials.Any())
            {
                foreach (var mat in model.Materials)
                {
                    var productMaterial = new ProductMaterial
                    {
                        ProductId = product.Id,
                        MaterialId = mat.MaterialId,
                        QuantityNeeded = mat.QuantityNeeded
                    };
                    _context.ProductMaterials.Add(productMaterial);
                }
                await _context.SaveChangesAsync();
            }

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, new
            {
                id = product.Id,
                name = product.Name,
                productionTimePerUnit = product.ProductionTimePerUnit,
                category = product.Category,
                minimalStock = product.MinimalStock
            });
        }

        // PUT /api/products/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateProduct(int id, [FromBody] ProductCreateUpdateModel model)
        {
            var product = await _context.Products
                .Include(p => p.ProductMaterials)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound(new { message = "Продукт не найден" });

            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { message = "Название продукта обязательно" });

            // Обновляем основные данные
            product.Name = model.Name;
            product.ProductionTimePerUnit = model.ProdTime;
            product.Category = model.Category ?? "";
            product.MinimalStock = model.MinimalStock >= 0 ? model.MinimalStock : 0;

            // Удаляем старые связи с материалами
            _context.ProductMaterials.RemoveRange(product.ProductMaterials);

            // Добавляем новые материалы
            if (model.Materials != null && model.Materials.Any())
            {
                foreach (var mat in model.Materials)
                {
                    var productMaterial = new ProductMaterial
                    {
                        ProductId = product.Id,
                        MaterialId = mat.MaterialId,
                        QuantityNeeded = mat.QuantityNeeded
                    };
                    _context.ProductMaterials.Add(productMaterial);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = product.Id,
                name = product.Name,
                message = "Продукт обновлен"
            });
        }

        // DELETE /api/products/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductMaterials)
                .Include(p => p.WorkOrders)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound(new { message = "Продукт не найден" });

            // Проверяем, есть ли активные заказы
            var activeOrders = product.WorkOrders
                .Where(w => w.Status == "InProgress" || w.Status == "Paused" || w.Status == "Pending")
                .ToList();

            if (activeOrders.Any())
            {
                return BadRequest(new 
                { 
                    message = $"Нельзя удалить продукт. Есть активные заказы: {activeOrders.Count} шт." 
                });
            }

            // Удаляем связи с материалами
            _context.ProductMaterials.RemoveRange(product.ProductMaterials);

            // Удаляем завершенные/отмененные заказы или отвязываем их
            foreach (var order in product.WorkOrders)
            {
                order.ProductId = 0; // Или можно пометить как удаленные
            }

            // Удаляем продукт
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Продукт удален" });
        }
    }

    public class ProductCreateModel
    {
        public string Name { get; set; } = string.Empty;
        public int ProdTime { get; set; }
        public string? Category { get; set; }
    }

    public class ProductCreateUpdateModel
    {
        public string Name { get; set; } = string.Empty;
        public int ProdTime { get; set; }
        public string? Category { get; set; }
        public int MinimalStock { get; set; }
        public List<ProductMaterialModel>? Materials { get; set; }
    }

    public class ProductMaterialModel
    {
        public int MaterialId { get; set; }
        public decimal QuantityNeeded { get; set; }
    }
}