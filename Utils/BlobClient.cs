using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Utils
{
    public class BlobClient
    {
        public static string ReadBlob(string filename)
        {
            string result;
            using (WebClient client = new WebClient())
            {
                result = client.DownloadString(new Uri($"https://amitstorage11.blob.core.windows.net/input/{filename}"));
            }

            return result;
        }

        public static string WriteBlob(string containerName, string filename, byte[] payload)
        {
            string connectionString = ConnectionStrings.StorageAccountConnection;

            CloudStorageAccount account = CloudStorageAccount.Parse(connectionString);
            CloudBlobClient client = account.CreateCloudBlobClient();
            CloudBlobContainer container = client.GetContainerReference(containerName);
            container.CreateIfNotExists();
            BlobContainerPermissions containerPermissions = new BlobContainerPermissions() { PublicAccess = BlobContainerPublicAccessType.Blob };
            //container.SetPermissions(containerPermissions);
            CloudBlockBlob exe = container.GetBlockBlobReference(filename);
            exe.UploadFromByteArray(payload, 0, payload.Length);
            return exe.Uri.ToString();
        }
    }
}
