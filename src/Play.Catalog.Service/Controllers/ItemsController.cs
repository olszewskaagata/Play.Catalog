using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Play.Catalog.Contracts;
using Play.Catalog.Service.Dtos;
using Play.Catalog.Service.Entities;
using Play.Common;

namespace Play.Catalog.Service.Controllers{
    [ApiController]
    [Route("items")]
    public class ItemsController : ControllerBase
    {
        private readonly IRepository<Item> _itemsRepository;
       // private static int _requestCounter;
       private readonly IPublishEndpoint _publishEndpoint;

        public ItemsController(IRepository<Item> itemsRepository, IPublishEndpoint publishEndpoint)
        {
            _itemsRepository = itemsRepository;
            _publishEndpoint = publishEndpoint;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ItemDto>>> GetAsync() 
        {
            // _requestCounter++;
            // Console.WriteLine($"Request {_requestCounter}: Starting...");

            // if(_requestCounter <= 2) 
            // {
            //     Console.WriteLine($"Request {_requestCounter}: Delaying...");
            //     await Task.Delay(TimeSpan.FromSeconds(10));
            // }

            // if(_requestCounter <= 4) 
            // {
            //     Console.WriteLine($"Request {_requestCounter}: 500 Internal Server Error...");
            //     return StatusCode(500);
            // }

            var items = (await _itemsRepository.GetAllAsync())
                        .Select(items => items.AsDto());
                        //Console.WriteLine($"Request {_requestCounter}: 200 OK...");
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ItemDto>> GetByIdAsync(Guid id)
        {
            var item = await _itemsRepository.GetItemAsync(id);

            if(item==null) return NotFound();
            return item.AsDto();
        }

        [HttpPost]
        public async Task<ActionResult<ItemDto>> PostAsync(CreateItemDto itemCreate)
        {
            var item = new Item { Name = itemCreate.Name, Description = itemCreate.Description, Price = itemCreate.Price, CreatedDate = DateTimeOffset.UtcNow };
            await _itemsRepository.CreateAsync(item);

            await _publishEndpoint.Publish(new CatalogItemCreated(item.Id, item.Name, item.Description));

            return CreatedAtAction(nameof(GetByIdAsync), new {id = item.Id}, item);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutAsync(Guid id, UpdateItemDto itemUpdate)
        {
            var  existingItem = await _itemsRepository.GetItemAsync(id);
            if (existingItem == null) return NotFound();

            existingItem.Name = itemUpdate.Name;
            existingItem.Description = itemUpdate.Description;
            existingItem.Price = itemUpdate.Price;

            await _itemsRepository.UpdateAsync(existingItem);

            await _publishEndpoint.Publish(new CatalogItemUpdated(existingItem.Id, existingItem.Name, existingItem.Description));

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            var  existingItem = await _itemsRepository.GetItemAsync(id);
            if (existingItem == null) return NotFound();

            await _itemsRepository.RemoveAsync(existingItem.Id);

            await _publishEndpoint.Publish(new CatalogItemDeleted(existingItem.Id));

            return NoContent();
        }
    }
}