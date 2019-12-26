using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Pinto.HostListener
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private Publisher _publisher;
        public ValuesController(Publisher publisher)
        {
            _publisher = publisher;
        }
        // GET: api/<controller>
        [HttpGet]
        public void Get()
        {
            for (int i = 0; i < 10; i++)
            {
                if (i % 2 == 0)
                {
                    _publisher.PushMessage("TestKey2", "AAAAA12345----------54321AAAAA");
                }
                else
                {
                    _publisher.PushMessage("TestKey1", "BBBBB12345----------54321BBBBB");
                }
            }
        }

 
    }
}
