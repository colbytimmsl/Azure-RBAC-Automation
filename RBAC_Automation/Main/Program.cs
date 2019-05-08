// Copyright (c) Softlanding Solutions Inc. All rights reserved.
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace RBAC_Automation
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                RunAsync().Wait();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                var aggregateException = ex as AggregateException;
                if (aggregateException != null)
                {
                    foreach (Exception subEx in aggregateException.InnerExceptions)
                    {
                        Console.WriteLine(subEx.Message);
                    }
                }
                else
                {
                    Console.WriteLine(ex.Message);
                }
                Console.ResetColor();
            }
        }

        private static async Task RunAsync()
        {
            SampleConfiguration config = new SampleConfiguration();
            var app = new PublicClientApplication(config.ClientId, config.Authority);
            var httpClient = new HttpClient();

            RbacAutomation rbacAutomation = new RbacAutomation(app, httpClient, config.MicrosoftGraphBaseEndpoint);
            await rbacAutomation.RunAutomationRetryingWhenWrongCredentialsAsync();
        }
    }
}
