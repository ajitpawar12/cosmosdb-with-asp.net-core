using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AspCoreWithCosmosDB.Models;
using AspCoreWithCosmosDB.Services;
using AspCoreWithCosmosDB.ViewModels.Item;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AspCoreWithCosmosDB.Controllers
{
    public class ItemController : Controller
    {
        private readonly ICosmosDbService _cosmosDbService;
        public ItemController(ICosmosDbService cosmosDbService)
        {
            _cosmosDbService = cosmosDbService;
        }

        [ActionName("Index")]
        public async Task<IActionResult> Index()
        {
            return View(await _cosmosDbService.GetItemsAsync("SELECT * FROM c"));
        }

        [ActionName("Create")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ActionName("Create")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateAsync([Bind("Id,Name,Description,Completed,ProductFile")] ItemViewModel item)
        {
            if (ModelState.IsValid)
            {   
                var itemObj = new Item();
                itemObj.Id = Guid.NewGuid().ToString();
                itemObj.Name = item.Name;
                itemObj.Description = item.Description;
                itemObj.Completed = item.Completed;

                var iPath=await UploadImageToBlobStorageAsync(item.ProductFile,itemObj.Id);
                item.ImageName = item.ProductFile.FileName;
                item.ImagePath = iPath;

                await _cosmosDbService.AddItemAsync(itemObj);
                return RedirectToAction("Index");
            }

            return View(item);
        }

        [HttpPost]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditAsync([Bind("Id,Name,Description,Completed")] Item item)
        {
            if (ModelState.IsValid)
            {
                await _cosmosDbService.UpdateItemAsync(item.Id, item);
                return RedirectToAction("Index");
            }

            return View(item);
        }

        [ActionName("Edit")]
        public async Task<ActionResult> EditAsync(string id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            Item item = await _cosmosDbService.GetItemAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        [ActionName("Delete")]
        public async Task<ActionResult> DeleteAsync(string id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            Item item = await _cosmosDbService.GetItemAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmedAsync([Bind("Id")] string id)
        {
            await _cosmosDbService.DeleteItemAsync(id);
            return RedirectToAction("Index");
        }

        [ActionName("Details")]
        public async Task<ActionResult> DetailsAsync(string id)
        {
            return View(await _cosmosDbService.GetItemAsync(id));
        }

        public async Task<string> UploadImageToBlobStorageAsync(IFormFile file,string fileName)
        {
            string imagePath = null;
            var name = file.Name;
            var filename = fileName;
            var length = file.Length;
            var contentType = file.ContentType;

            var memoryStream = new MemoryStream();
            file.CopyTo(memoryStream);
            var bitesArray = memoryStream.ToArray();

            string storageConnectionString = "AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;DefaultEndpointsProtocol=http;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;";
            CloudStorageAccount cloudStotageAcoount;

            // connect to azure storage account using connectio string
            if (CloudStorageAccount.TryParse(storageConnectionString, out cloudStotageAcoount))
            {
                //create blobclient
                CloudBlobClient cloudBlobClient = cloudStotageAcoount.CreateCloudBlobClient();

                //container name
                string containerName = "mediafilescontainer";

                //create reference for container
                CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);

                //check for container exists, if container not exists then create new container on azure blob storage
                if (await cloudBlobContainer.CreateIfNotExistsAsync())
                {
                    await cloudBlobContainer.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
                }

                //create cloudblock blob for upload or delete blob
                CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(filename);

                await cloudBlockBlob.DeleteIfExistsAsync();
                //set content type for blob which is whatever the extension file has
                cloudBlockBlob.Properties.ContentType = contentType;

                //upload file from file stream to azure blob storage against current container
                await cloudBlockBlob.UploadFromByteArrayAsync(bitesArray, 0, bitesArray.Length);


                //Get url for uploaded file
                var path = cloudBlockBlob.Uri.AbsoluteUri;
                
                imagePath = path;
            }
            return imagePath;
        }
    }
}