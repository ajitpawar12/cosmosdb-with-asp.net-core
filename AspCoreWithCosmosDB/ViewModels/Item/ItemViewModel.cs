using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspCoreWithCosmosDB.ViewModels.Item
{
    public class ItemViewModel
    {
        
        public string Id { get; set; }        
        public string Name { get; set; }        
        public string Description { get; set; }
        public bool Completed { get; set; }       
        public string ImageName { get; set; }        
        public string ImagePath { get; set; }
        public IFormFile ProductFile { get; set; }
    }
}
