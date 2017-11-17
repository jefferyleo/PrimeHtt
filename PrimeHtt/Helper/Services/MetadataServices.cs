using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace PrimeHtt.Helper.Services
{
    public class MetadataServices
    {
        public static DateTime GetCurrentDateTime()
        {
            return DateTime.Now;
        }

        public static DateTime GetMalaysiaDateTime()
        {
            return DateTime.Now.AddHours(8);
        }

        public static string GetDateWithoutSlash()
        {
            return DateTime.Now.AddHours(8).ToString("yyyyMMdd");
        }

        public static string UploadToCloud(string getContainer, HttpPostedFileBase imageFile, out string newFileName)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.ConnectionStrings["StorageConnectionString"].ConnectionString);

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            CloudBlobContainer setContainer = blobClient.GetContainerReference(getContainer);
            setContainer.SetPermissions(
                new BlobContainerPermissions
                {
                    PublicAccess = BlobContainerPublicAccessType.Container
                });

            var fileName = imageFile.FileName;
            var getFileName = fileName.Substring(0, fileName.IndexOf('.'));
            string newfileName = GetDateWithoutSlash() + "-" + fileName;
            CloudBlockBlob blockBlob = setContainer.GetBlockBlobReference(newfileName);
            blockBlob.Properties.ContentType = "image/jpeg";
            //if (!blockBlob.Exists())
            //{
            blockBlob.UploadFromStream(imageFile.InputStream);
            //}
            //else
            //{
            //    return "Fail";
            //}
            newFileName = newfileName;
            return blockBlob.Uri.AbsoluteUri;
        }

        //public static void DeleteFromCloud(string imageURL)
        //{
        //    var credentials = new StorageCredentials("primehtt", "8az6p/xUNIxYR48nkfV1M24GhT38YO3aiXrL601NopqvjnZUA8xkHQHgJ11rQqOVFk4/XxKaMnF/INNe4OaiRA==");
        //    var cloudBlob = new CloudBlob(new Uri(imageURL), credentials);
        //    cloudBlob.DeleteIfExists();
        //}

        public static void DeleteFromCloud(string getContainer, string fileName)
        {
            var _containerName = getContainer;
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.ConnectionStrings["StorageConnectionString"].ConnectionString);
            CloudBlobClient _blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer _cloudBlobContainer = _blobClient.GetContainerReference(_containerName);
            CloudBlockBlob _blockBlob = _cloudBlobContainer.GetBlockBlobReference(fileName);
            //delete blob from container    
            _blockBlob.Delete();
        }
    }
}