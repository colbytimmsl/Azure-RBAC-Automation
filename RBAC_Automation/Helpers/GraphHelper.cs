// Copyright (c) Softlanding Solutions Inc. All rights reserved.
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Graph;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace RBAC_Automation
{
    class GraphHelper
    {
        /// <summary>
        /// Retrieve the role name id string
        /// </summary>
        /// <param name="graphServiceClient"></param>
        /// <param name="roleName"></param>
        /// <param name="directoryRole"></param>
        /// <param name="directoryRoleTemplate"></param>
        /// <returns></returns>
        public async static Task<string> GetRoleByName(GraphServiceClient graphServiceClient, string roleName,
                                                       IGraphServiceDirectoryRolesCollectionPage directoryRole,
                                                       IGraphServiceDirectoryRoleTemplatesCollectionPage directoryRoleTemplate)
        {
            roleName = roleName.ToLower();
            try
            {
                foreach (var role in directoryRole)
                {
                    var directoryRoleName = role.DisplayName.ToLower();
                    if (directoryRoleName.Contains(roleName))
                    {
                        return role.Id;
                    }
                }
                foreach (var roleTemplate in directoryRoleTemplate)
                {
                    var directoryRoleTemplateName = roleTemplate.DisplayName.ToLower();
                    if (directoryRoleTemplateName.Contains(roleName))
                    {
                        var directoryRoleCreate = new DirectoryRole()
                        {
                            DisplayName = roleTemplate.DisplayName,
                            RoleTemplateId = roleTemplate.Id,
                        };

                        DirectoryRole newRole = await graphServiceClient.DirectoryRoles.Request().AddAsync(directoryRoleCreate);

                        if(newRole == null)
                        {
                            string errorMsg = "New role null. ";
                            await ErrorHandling.ErrorEvent(errorMsg, "N/A");
                        }

                        return newRole.Id;
                    }
                }
            }
            catch (ArgumentException ex)
            {
                string errorMsg = "Get role by name method failure.";
                string exMsg = ex.Message;
                await ErrorHandling.ErrorEvent(errorMsg, exMsg);
            }

            return null;
        }

        /// <summary>
        /// Gets all members from group and applies roles
        /// </summary>
        /// <param name="graphServiceClient"></param>
        /// <param name="groupId"></param>
        /// <param name="roleId"></param>
        /// <param name="groups"></param>
        /// <returns></returns>
        private async static Task<List<string>> GetMembersfromGroup(GraphServiceClient graphServiceClient, string groupId, string roleName,
                                                      IGraphServiceGroupsCollectionPage groups, IGraphServiceDirectoryRolesCollectionPage directoryRole,
                                                      IGraphServiceDirectoryRoleTemplatesCollectionPage directoryRoleTemplate)
        {
            List<string> groupRoles = new List<string>();
            try
            {
                string roleId = await GetRoleByName(graphServiceClient: graphServiceClient, roleName: roleName,
                                          directoryRole: directoryRole, directoryRoleTemplate: directoryRoleTemplate);
                if (!string.IsNullOrEmpty(roleId))
                {
                    TableBatchOperation batchOperation = new TableBatchOperation();
                    do
                    {
                        foreach (var group in groups)
                        {
                            List<string> members = new List<string>();
                            GroupRoleEntity groupRoleEntity = new GroupRoleEntity();
                            var users = await graphServiceClient.Groups[group.Id].Members.Request().GetAsync();
                            var groupInfo = await graphServiceClient.Groups[group.Id].Request().GetAsync();
                            var groupName = groupInfo.DisplayName;
                            if (group.Id == groupId)
                            {
                                do
                                {
                                    Console.WriteLine($"\n{group.Id}, {group.DisplayName}");
                                    Console.WriteLine("------");

                                    foreach (var user in users)
                                    {
                                        members.Add(user.Id);
                                        try
                                        {
                                            await graphServiceClient.DirectoryRoles[roleId].Members.References.Request().AddAsync(user);
                                            Console.WriteLine($"Assigning role to: {user.Id}");
                                            groupRoles.Add($"Group: {groupName} - Role: {roleName} - UserId: {user.Id}");
                                        }
                                        catch
                                        {
                                            Console.WriteLine($"{user.Id} already contains role {roleId}");
                                        }
                                    }
                                }
                                while (users.NextPageRequest != null && (users = await users.NextPageRequest.GetAsync()).Count > 0);
                                groupRoleEntity = await StorageHelper.CreateTableEntry(graphServiceClient, group.Id, roleId, members);
                                Console.WriteLine("CreateTableEntry workedS");
                                if (groupRoleEntity != null)
                                {
                                    batchOperation.InsertOrMerge(groupRoleEntity);
                                }
                            }
                        }
                    }
                    while (groups.NextPageRequest != null && (groups = await groups.NextPageRequest.GetAsync()).Count > 0);
                    if (batchOperation != null)
                    {
                        await StorageHelper.TableBatchOperation(graphServiceClient, batchOperation);
                    }
                }
                return groupRoles;
            }
            catch (ArgumentException ex)
            {
                string errorMsg = "Get members from group method failure.";
                string exMsg = ex.Message;
                await ErrorHandling.ErrorEvent(errorMsg, exMsg);
            }
            return null;
        }

        /// <summary>
        /// Gets all roles templates in AAD and saves to .txt file with objectId
        /// </summary>
        /// <param name="graphServiceClient"></param>
        /// <returns></returns>
        public async static Task GetAllRoles(GraphServiceClient graphServiceClient)
        {
            try
            {
                var roles = await graphServiceClient.DirectoryRoles.Request().GetAsync();
                var fullText = new System.Text.StringBuilder();
                string projectDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string path = projectDirectory + @"\Resources\RoleTemplates.txt";
                FileInfo f = new FileInfo(path);
                string fullPath = f.FullName;

                do
                {
                    foreach (var role in roles)
                    {
                        string text = $"Role name: {role.DisplayName}, Role ID: {role.Id}" + Environment.NewLine;
                        fullText.Append(text);
                    }
                }
                while (roles.NextPageRequest != null && (roles = await roles.NextPageRequest.GetAsync()).Count > 0);
                Console.WriteLine(fullText);
                TextHelper.WriteTextFile(path: fullPath, text: fullText.ToString());
            }
            catch (ArgumentException ex)
            {
                string errorMsg = "Failed to retrieve roles in get all roles method.";
                string exMsg = ex.Message;
                await ErrorHandling.ErrorEvent(errorMsg, exMsg);
            }
        }

        /// <summary>
        /// Gets all roles templates in AAD and saves to .txt file with objectId
        /// </summary>
        /// <param name="graphServiceClient"></param>
        /// <returns></returns>
        public async static Task GetAllRoleTemplates(GraphServiceClient graphServiceClient)
        {
            var roles = await graphServiceClient.DirectoryRoleTemplates.Request().GetAsync(); ;
            var fullText = new System.Text.StringBuilder();
            string projectDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string path = projectDirectory + @"\Resources\RoleTemplates.txt";
            FileInfo f = new FileInfo(path);
            string fullPath = f.FullName;

            do
            {
                foreach (var role in roles)
                {
                    string text = $"Role name: {role.DisplayName}, Role ID: {role.Id}" + Environment.NewLine;
                    fullText.Append(text);
                }
            }
            while (roles.NextPageRequest != null && (roles = await roles.NextPageRequest.GetAsync()).Count > 0);
            Console.WriteLine(fullText);
            TextHelper.WriteTextFile(path: fullPath, text: fullText.ToString());
        }

        /// <summary>
        /// Assign roles to all groups in GroupRoles.csv
        /// </summary>
        /// <param name="graphServiceClient"></param>
        /// <param name="userId"></param>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public async static Task AssignRoles(GraphServiceClient graphServiceClient)
        {
            try
            {
                List<KeyValuePair<string, string>> groupRoleDict = await ReadCsv.GetGroupRoles(graphServiceClient);
                var directoryRole = await graphServiceClient.DirectoryRoles.Request().GetAsync();
                var directoryRoleTemplate = await graphServiceClient.DirectoryRoleTemplates.Request().GetAsync();
                var groups = await graphServiceClient.Groups.Request().GetAsync();
                List<string> newMembersList = new List<string>();

                foreach (KeyValuePair<string, string> entry in groupRoleDict)
                {
                    List<string> members = new List<string>();
                    string groupId = entry.Key;
                    string roleName = entry.Value;

                    members = await GetMembersfromGroup(graphServiceClient: graphServiceClient, groupId: groupId, roleName: roleName, groups: groups,
                                              directoryRole: directoryRole, directoryRoleTemplate: directoryRoleTemplate);
                    if (members != null)
                    {
                        newMembersList.AddRange(members);
                    }
                }
                //EMAIL here
                //await Email.SendEmailNewGroups(graphServiceClient, newMembersList);
                await Email.SendGridEmailNewGroups(newMembersList);
            }
            catch (ArgumentException ex)
            {
                string errorMsg = "Failed to assign roles in AssignRoles method.";
                string exMsg = ex.Message;
                await ErrorHandling.ErrorEvent(errorMsg, exMsg);
            }

        }

    }
}
