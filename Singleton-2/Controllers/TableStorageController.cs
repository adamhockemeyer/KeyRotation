using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Singleton_2.Services;

namespace Singleton_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TableStorageController : ControllerBase
    {
        private readonly ILogger<TableStorageController> _logger;
        private readonly TableStorageService _tableService;
        public TableStorageController(TableStorageService tableService, ILogger<TableStorageController> logger)
        {
            _logger = logger;
            _tableService = tableService;
        }

        [HttpGet]
        public async Task<IActionResult> ListItems()
        {
            var results = await _tableService.GetAll<CustomerEntity>("Customers");

            return new OkObjectResult(results);
        }
    }
}