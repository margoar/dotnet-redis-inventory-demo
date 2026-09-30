using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using RedisInventoryDemo.Application.Contracts.Inventory;
using RedisInventoryDemo.Application.Services;
using RedisInventoryDemo.Domain.Entities;

namespace RedisInventoryDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InventoryItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryItemResponse>> GetById(int id)
    {
        var item = await _inventoryService.GetByIdAsync(id);

        if (item is null)
            return NotFound();

        return Ok(new InventoryItemResponse(item.Id, item.Name, item.Stock));
    }

    [HttpPost("{id:int}/reserve")]
    public async Task<ActionResult<InventoryItemResponse>> Reserve(int id, ReserveInventoryRequest request)
    {
        if (request.Quantity <= 0)
        {
            return BadRequest("La cantidad debe ser mayor que cero.");
        }

        var result = await _inventoryService.ReserveAsync( id, request);

        return result.Status switch
        {
            ReserveStockStatus.InvalidQuantity => BadRequest("La cantidad debe ser mayor que cero."),
            ReserveStockStatus.InventoryNotFound => NotFound(),
            ReserveStockStatus.InsufficientStock => Conflict(),
            ReserveStockStatus.Reserved => Ok(new InventoryItemResponse(id,$"Producto {id}",result.RemainingStock!.Value)),


        _ => StatusCode(StatusCodes.Status500InternalServerError)
 
        };
    }
}