// Copyright (c) Softlanding Solutions Inc. All rights reserved.
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace RBAC_Automation
{
    class DevTools
    {
        public async static Task CreateGroupswithMembers(GraphServiceClient graphServiceClient, int groupAmts, int memberAmts)
        {
            for(int index = 0; index < groupAmts; index++)
            {
                Group newGroup = new Group()
                {
                    DisplayName = "Group" + RandomString(6),
                    MailEnabled = false,
                    MailNickname = "mailNicknameTest",
                    SecurityEnabled = true,
                };
                var group = await graphServiceClient.Groups.Request().AddAsync(newGroup);
                Console.WriteLine("---------------------------------------");
                Console.WriteLine($"New group name: {group.DisplayName}");
                await CreateMembers(graphServiceClient, memberAmts, group.Id);
                //TextHelper.WriteTextFileAppend(path: @"Resources\TestGroups.txt", text: group.Id);
            }
        }

        private async static Task CreateMembers(GraphServiceClient graphServiceClient, int memberAmts, string groupId)
        {
            for (int index = 0; index < memberAmts; index++)
            {
                string userName = "User" + RandomString(8);
                string password = RandomString(10);
        
                PasswordProfile passwordProfile = new PasswordProfile()
                {
                    Password = password
                };
                User user = new User()
                {
                    DisplayName = userName,
                    MailNickname = "mailNicknameTest",
                    UserPrincipalName = userName + "@DOMAIN_NAME_HERE",
                    PasswordProfile = passwordProfile,
                    AccountEnabled = true
                };
                var member = await graphServiceClient.Users.Request().AddAsync(user);
                Console.WriteLine($"New user name: {member.DisplayName}");
                await graphServiceClient.Groups[groupId].Members.References.Request().AddAsync(member);
            }
        }

        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

    }
    class StopWatch
    {
        public static void Start(Stopwatch stopWatch)
        {
            stopWatch.Start();
        }
        public static void Stop(Stopwatch stopWatch)
        {
            stopWatch.Stop();
            long duration = stopWatch.ElapsedMilliseconds;
            Console.WriteLine($"The duration to run was: {duration} milliseconds");
        }

    }
}
