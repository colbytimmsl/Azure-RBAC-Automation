// Copyright (c) Softlanding Solutions Inc. All rights reserved.
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Graph;
using System.Net.Http.Headers;
using System.Diagnostics;

namespace RBAC_Automation
{
    public class RbacAutomation
    {
        public RbacAutomation(PublicClientApplication app, HttpClient client, string microsoftGraphBaseEndpoint)
        {
            tokenAcquisitionHelper = new PublicAppUsingUsernamePassword(app);
            protectedApiCallHelper = new ProtectedApiCallHelper(client);
            this.MicrosoftGraphBaseEndpoint = microsoftGraphBaseEndpoint;
        }

        protected PublicAppUsingUsernamePassword tokenAcquisitionHelper;

        protected ProtectedApiCallHelper protectedApiCallHelper;

        /// <summary>
        /// Scopes to request access to the protected Web API (here Microsoft Graph)
        /// </summary>
        private static string[] Scopes { get; set; } = new string[] { "User.Read", "User.ReadBasic.All" };

        /// <summary>
        /// Base endpoint for Microsoft Graph
        /// </summary>
        private string MicrosoftGraphBaseEndpoint { get; set; }

        /// <summary>
        /// URLs of the protected Web APIs to call (here Microsoft Graph endpoints)
        /// </summary>
        private string WebApiUrlMe { get { return $"{MicrosoftGraphBaseEndpoint}/v1.0/me"; } }
        private string WebApiUrlMyManager { get { return $"{MicrosoftGraphBaseEndpoint}/v1.0/me/manager"; } }


        /// <summary>
        /// Calls the Web API and displays its information
        /// </summary>
        /// <returns></returns>
        public async Task RunAutomationRetryingWhenWrongCredentialsAsync()
        {
            bool again = true;
            while(again)
            {
                again = false;
                try
                {
                    await RunAutomationAsync();
                }
                catch (ArgumentException ex) when (ex.Message.StartsWith("U/P"))
                {
                    // Wrong user or password
                    WriteTryAgainMessage();
                    again = true;
                }
            }
        }

        /// <summary>
        /// Main automation task that runs all required methods to set roles for respectives groups listed in GroupRoles.csv
        /// </summary>
        /// <returns></returns>
        private async Task RunAutomationAsync()
        {
            string username = Environment.GetEnvironmentVariable("Auth_UserName");
            string secret = Environment.GetEnvironmentVariable("Auth_UserSecret");
            SecureString password = ConvertPassword(secret);

            AuthenticationResult authenticationResult = await tokenAcquisitionHelper.AcquireATokenFromCacheOrUsernamePasswordAsync(Scopes, username, password);

            if (authenticationResult != null)
            {
                DisplaySignedInAccount(authenticationResult.Account);

                //Download blob files
                await FetchBlobFiles();

                string accessToken = authenticationResult.AccessToken;
                GraphServiceClient graphServiceClient = Authenticate(accessToken);
                Console.WriteLine("Authenticated.");
                // Write role templates to .txt file
                //await GraphHelper.GetAllRoles(graphServiceClient);

                Stopwatch stopwatch = new Stopwatch();
                StopWatch.Start(stopwatch);

                await GraphHelper.AssignRoles(graphServiceClient);

                StopWatch.Stop(stopwatch);
            }
        }

        public static GraphServiceClient Authenticate(string accessToken)
        {
            var graphServiceClient = new GraphServiceClient(new DelegateAuthenticationProvider((requestMessage) => {
                requestMessage
                    .Headers
                    .Authorization = new AuthenticationHeaderValue("bearer", accessToken);

                return Task.FromResult(0);
            }));
            return graphServiceClient;
        }

        private static void WriteTryAgainMessage()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Wrong user or password. Try again!");
            Console.ResetColor();
        }

        private static SecureString ConvertPassword(string passwordString)
        {
            SecureString password = new SecureString();
            foreach (char c in passwordString)
            {
                password.AppendChar(c);
            }
            return password;
        }

        private static void DisplaySignedInAccount(IAccount account)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{account.Username} successfully signed-in");
        }

        /// <summary>
        /// Display the result of the Web API call
        /// </summary>
        /// <param name="result">Object to display</param>
        private static void Display(JObject result)
        {
            foreach (JProperty child in result.Properties().Where(p => !p.Name.StartsWith("@")))
            {
                Console.WriteLine($"{child.Name} = {child.Value}");
            }
        }

        private async static Task FetchBlobFiles()
        {
            await StorageHelper.GetBlobFile(fileName: "GroupRoles.csv", containerName: "group-roles");
            await StorageHelper.GetBlobFile(fileName: "EmailAccounts.csv", containerName: "email-resources");
            await StorageHelper.GetBlobFile(fileName: "alert_template.html", containerName: "general-resources");
            await StorageHelper.GetBlobFile(fileName: "newgrouprole_template.html", containerName: "general-resources");
        }
    }
}
