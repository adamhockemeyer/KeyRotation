using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;

namespace Singleton_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TableStorageController : ControllerBase
    {
        private readonly ILogger<TableStorageController> _logger;
        private readonly CloudTableClient _client;
        public TableStorageController(CloudTableClient client, ILogger<TableStorageController> logger)
        {
            _logger = logger;
            _client = client;
        }

        [HttpGet]
        public IActionResult ListItems()
        {
            var table = _client.GetTableReference("Customers");

            IEnumerable<CustomerEntity> query = (from customers in table.CreateQuery<CustomerEntity>()
                                            select customers);

            return new OkObjectResult(query);
        }
    }
}