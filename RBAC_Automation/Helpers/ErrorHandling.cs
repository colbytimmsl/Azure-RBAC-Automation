// Copyright (c) Softlanding Solutions Inc. All rights reserved.
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace RBAC_Automation
{
    class ErrorHandling
    {
        public async static Task ErrorEvent(string errorMsg, string exMsg)
        {
            string msg = errorMsg;
            await Email.SendGridErrorEmail(errorMsg, exMsg);
        }

    }

    public class Email
    {
        private static readonly string functionName = Environment.GetEnvironmentVariable("RBAC_Function:Name");
        private static readonly string sendGridKey = Environment.GetEnvironmentVariable("RBAC_Function:SendGridKey");

        private async static Task SendGridErrorEmail(string errorMessage, string exMsg,
                                                 List<SendGrid.Helpers.Mail.EmailAddress> recipients)
        {
            string emailTemplate = $@"D:\home\site\wwwroot\{functionName}\Application\Resources\alert_template.html";

            string text = System.IO.File.ReadAllText(emailTemplate);
            text = text.Replace("961b80fb-40bb-4eee-bb61-1f03a4a01668", errorMessage);
            text = text.Replace("e01cce6a-1306-426d-805b-fd024210f027", exMsg);
            System.IO.File.WriteAllText(emailTemplate, text);
            StreamReader reader = System.IO.File.OpenText(emailTemplate);

            var client = new SendGridClient(sendGridKey);
            var msg = new SendGridMessage()
            {
                From = new SendGrid.Helpers.Mail.EmailAddress("donotreply@healthbc.org", "RBAC Automation"),
                Subject = "ERROR: RBAC Automation Error Reporting",
                HtmlContent = reader.ReadToEnd()
            };
            msg.AddTos(recipients);

            Console.WriteLine("Email sending");

            try
            {
                var response = await client.SendEmailAsync(msg);
            }
            catch (ArgumentException ex)
            {
                string errorMsg = "Failed to send email for error message.";
                Console.WriteLine(errorMsg);
                Console.WriteLine(errorMessage);
                Console.WriteLine(exMsg);
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("Email sent");
        }

        private async static Task SendGridNewGroupRolesEmail(List<string> groupList,
                                                 List<SendGrid.Helpers.Mail.EmailAddress> recipients)
        {
            if (groupList.Any())
            {
                string emailTemplate = $@"D:\home\site\wwwroot\{functionName}\Application\Resources\newgrouprole_template.html";

                string text = System.IO.File.ReadAllText(emailTemplate);
                string groups = CreateGroupText(groupList);
                text = text.Replace("961b80fb-40bb-4eee-bb61-1f03a4a01668", groups);
                System.IO.File.WriteAllText(emailTemplate, text);

                StreamReader reader = System.IO.File.OpenText(emailTemplate);

                var client = new SendGridClient(sendGridKey);
                var msg = new SendGridMessage()
                {
                    From = new SendGrid.Helpers.Mail.EmailAddress("EMAIL_ADDRESS_HERE", "RBAC Automation"),
                    Subject = "RBAC Automation New Roles Assigned",
                    HtmlContent = reader.ReadToEnd()
                };
                msg.AddTos(recipients);
                
                Console.WriteLine("Email sending");

                try
                {
                    var response = await client.SendEmailAsync(msg);
                }
                catch (ArgumentException ex)
                {
                    string errorMsg = "Failed to send email for new roles.";
                    string exMsg = ex.Message;
                    await ErrorHandling.ErrorEvent(errorMsg, exMsg);
                }

                Console.WriteLine("Email sent");
            }
        }

        /// <summary>
        /// Send error email
        /// </summary>
        /// <param name="errorMessage"></param>
        /// <param name="exMsg"></param>
        /// <returns></returns>
        public async static Task SendGridErrorEmail(string errorMessage, string exMsg)
        {
            List<KeyValuePair<string, string>> accounts = ReadCsv.GetEmailAccounts();
            List<SendGrid.Helpers.Mail.EmailAddress> recipients = new List<SendGrid.Helpers.Mail.EmailAddress>();

            foreach (var account in accounts)
            {
                string name = account.Key;
                string address = account.Value;

                var emailAddress = new SendGrid.Helpers.Mail.EmailAddress()
                {
                    Name = name,
                    Email = address
                };
                recipients.Add(emailAddress);
            }
            await SendGridErrorEmail(errorMessage, exMsg, recipients);
        }

        /// <summary>
        /// Send email with a list of roles applied to new users in set groups
        /// </summary>
        /// <param name="graphServiceClient"></param>
        /// <param name="groups"></param>
        /// <returns></returns>
        public async static Task SendGridEmailNewGroups(List<string> groups)
        {
            List<KeyValuePair<string, string>> accounts = ReadCsv.GetEmailAccounts();
            List<SendGrid.Helpers.Mail.EmailAddress> recipients = new List<SendGrid.Helpers.Mail.EmailAddress>();

            foreach (var account in accounts)
            {
                string name = account.Key;
                string address = account.Value;

                var emailAddress = new SendGrid.Helpers.Mail.EmailAddress()
                {
                    Name = name,
                    Email = address
                };
                recipients.Add(emailAddress);
            }
            await SendGridNewGroupRolesEmail(groups, recipients);
        }

        /// <summary>
        /// Returns a string of new groups
        /// </summary>
        /// <param name="groupList"></param>
        /// <returns></returns>
        private static string CreateGroupText(List<string> groupList)
        {
            string text = "";
            foreach (var group in groupList)
            {
                text += $"{group}<br />";
            }
            return text;
        }
    }
}
