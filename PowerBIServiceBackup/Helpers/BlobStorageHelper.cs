using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace PowerBIServiceBackup.Helpers
{
    public static class BlobStorageHelper
    {
        public static CloudBlockBlob GetBlob(string connectionString, string containerName, string blobName)
        {
            // Container name should be lowercase
            containerName = containerName.ToLower();

            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);

            container.CreateIfNotExists();

            // Retrieve reference to a blob named "myblob".
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);

            return blockBlob;

        }
    }
}
