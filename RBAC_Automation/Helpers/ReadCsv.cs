// Copyright (c) Softlanding Solutions Inc. All rights reserved.
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using FileHelpers;
using Microsoft.Graph;

namespace RBAC_Automation
{
    class ReadCsv
    {
        private static readonly string functionName = Environment.GetEnvironmentVariable("RBAC_Function:Name");

        public async static Task<List<KeyValuePair<string, string>>> GetGroupRoles(GraphServiceClient graphServiceClient)
        {
            try
            {
                var groupRoleDict = new List<KeyValuePair<string, string>>();
                string projectDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string path = $@"D:\home\site\wwwroot\{functionName}\Application\Resources\GroupRoles.csv";

                var engine = new FileHelperEngine<GroupRole>();
                var result = engine.ReadFile(path);

                foreach (GroupRole g in result)
                {
                    groupRoleDict.Add(new KeyValuePair<string, string>(g.GroupId, g.RoleName));
                }

                return groupRoleDict;
            }
            catch (ArgumentException ex)
            {
                string errorMsg = "Get role by name method failure. Check if GroupRoles.csv exists in blob container.";
                string exMsg = ex.Message;
                await ErrorHandling.ErrorEvent(errorMsg, exMsg);
            }
            return null;
        }

        public static List<KeyValuePair<string, string>> GetGroupRolesCheck()
        {
            var groupRoleDict = new List<KeyValuePair<string, string>>();
            //var groupRoleDict = new List<Tuple<string, string>>();
            string projectDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string path = projectDirectory + @"\Resources\GroupRolesCheck.csv";

            var engine = new FileHelperEngine<GroupRole>();
            var result = engine.ReadFile(path);

            foreach (GroupRole g in result)
            {
                groupRoleDict.Add(new KeyValuePair<string, string>(g.GroupId, g.RoleName));
            }

            return groupRoleDict;
        }

        public static List<KeyValuePair<string, string>> GetEmailAccounts()
        {
            var emailAccountDict = new List<KeyValuePair<string, string>>();

            string projectDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string path = $@"D:\home\site\wwwroot\{functionName}\Application\Resources\EmailAccounts.csv";

            var engine = new FileHelperEngine<EmailAccounts>();
            var result = engine.ReadFile(path);

            foreach (EmailAccounts e in result)
            {
                emailAccountDict.Add(new KeyValuePair<string, string>(e.Name, e.Email));
            }

            return emailAccountDict;
        }

    }

    [DelimitedRecord(",")]
    public class GroupRole
    {
        public string GroupId;
        public string RoleName;
    }

    [DelimitedRecord(",")]
    public class EmailAccounts
    {
        public string Name;
        public string Email;
    }
}
