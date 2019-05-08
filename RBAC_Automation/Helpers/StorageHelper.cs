// Copyright (c) Softlanding Solutions Inc. All rights reserved.
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;

namespace RBAC_Automation
{
    class StorageHelper
    {
        public static string storageConnectionString = Environment.GetEnvironmentVariable("storageConnectionString");
        private static readonly string functionName = Environment.GetEnvironmentVariable("RBAC_Function:Name");
        public static async Task GetBlobFile(string fileName, string containerName)
        {
            CloudStorageAccount storageAccount = null;
            CloudBlobContainer cloudBlobContainer = null;
            string sourceFile = null;
            //string storageConnectionString = Environment.GetEnvironmentVariable("storageConnectionString");

            // Check whether the connection string can be parsed.
            if (CloudStorageAccount.TryParse(storageConnectionString, out storageAccount))
            {
                try
                {
                    // Create the CloudBlobClient that represents the Blob storage endpoint for the storage account.
                    CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();

                    //// Get reference to container 
                    cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);

                    string localFileName = fileName;
                    sourceFile = $@"D:\home\site\wwwroot\{functionName}\Application\Resources\" + fileName;

                    // Use the value of localFileName for the blob name.
                    CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(localFileName);
                    // Download the file from the blob
                    await cloudBlockBlob.DownloadToFileAsync(sourceFile, FileMode.Create);
                }
                catch (StorageException ex)
                {
                    Console.WriteLine("Error returned from the service: {0}", ex.Message);
                }
            }
            else
            {
                Console.WriteLine(
                    "A connection string has not been defined in the system environment variables. " +
                    "Add a environment variable named 'storageconnectionstring' with your storage " +
                    "connection string as a value.");
            }
        }

        public async static Task<GroupRoleEntity> CreateTableEntry(GraphServiceClient graphServiceClient, string groupId, string roleId, List<string> members)
        {
            try
            {
                Console.WriteLine("In createtableentry beginning");

                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                CloudTable table = tableClient.GetTableReference("groupRoles");
                TableOperation retrieveOperation = TableOperation.Retrieve<GroupRoleEntity>(groupId, roleId);
                TableResult retrievedResult = table.Execute(retrieveOperation);
                GroupRoleEntity test = (GroupRoleEntity)retrievedResult.Result;

                Console.WriteLine("Got result from table");
                if (test != null)
                {
                    await RemoveRoleFromOldUser(graphServiceClient, groupId, roleId, members, test);
                }

                string user = "";
                foreach (var member in members)
                {
                    user += $"{member},";
                }
                user = user.Remove(user.Length - 1);

                GroupRoleEntity groupRoleEntity = new GroupRoleEntity(groupId, roleId)
                {
                    ETag = "*",
                    GroupMembers = user
                };

                return groupRoleEntity;
            }
            catch (ArgumentException ex)
            {
                string errorMsg = "CreateTableEntry method failure.";
                string exMsg = ex.Message;
                await ErrorHandling.ErrorEvent(errorMsg, exMsg);
            }
            return null;
        }

        private async static Task RemoveRoleFromOldUser(GraphServiceClient graphServiceClient, string groupId, string roleId, 
                                                        List<string> currentMembers, GroupRoleEntity groupRoleEntity)
        {
            try
            {
                List<string> groupMemberList = groupRoleEntity.GroupMembers.Split(',').ToList();

                var removeMembersList = groupMemberList.Except(currentMembers).ToList();

                var test = await graphServiceClient.DirectoryRoles[roleId].Members.Request().GetAsync();
                List<string> roleMembers = new List<string>();
                bool notEmpty = removeMembersList.Any();
                if (notEmpty)
                {
                    foreach (var user in test)
                    {
                        roleMembers.Add(user.Id);
                    }

                    foreach (var member in removeMembersList)
                    {
                        if (roleMembers.Contains(member))
                        {
                            await graphServiceClient.DirectoryRoles[roleId].Members[member].Reference.Request().DeleteAsync();
                        }
                    }
                }
                
            }
            catch (ArgumentException ex)
            {
                string errorMsg = "Remove roles from member method failure.";
                string exMsg = ex.Message;
                await ErrorHandling.ErrorEvent(errorMsg, exMsg);
            }
        }

        public async static Task TableBatchOperation(GraphServiceClient graphServiceClient, TableBatchOperation batchOperation)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("groupRoles");
            try
            {
                await table.ExecuteBatchAsync(batchOperation);
            }
            catch (ArgumentException ex)
            {
                string errorMsg = "Batch Table Operation method failure.";
                string exMsg = ex.Message;
                await ErrorHandling.ErrorEvent(errorMsg, exMsg);
            }
            
        }
    }

    public class GroupRoleEntity : TableEntity
    {
        public GroupRoleEntity(string groupId, string roleId)
        {
            PartitionKey = groupId;
            RowKey = roleId;
        }

        public GroupRoleEntity() { }

        public string GroupMembers { get; set; }
    }
}
