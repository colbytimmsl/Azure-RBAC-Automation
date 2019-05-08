#Azure AD Role-Based Access Control Automation
##Purpose
The purpose of the Azure AD Role-Based Access Control (RBAC) automation system is provide the ability to assign directory roles to groups, thereby applying the respective role to all members that belong to said group. As of now, Azure doesn’t provide this option, which can prove difficult to track each individual user’s roles.
##Objective
Implement a RBAC automation system that adds assigned roles and removes roles from members of monitored groups automatically. 
##Workflow Overview
The workflow to assign roles to groups is as follows:
1. Add emails to an email csv file that allows the added user to receive error and new role emails which specify the group, user, and the role that has recently been assigned.
2. Add group object ids and role names to a group role csv file to specify the roles that will be assigned to each group.
##Installation
1. Navigate to Azure AD in Azure Portal (portal.azure.com) and sign in with an administrator account. Under Manage, select App Registrations (preview) and then click on new registration.
###insert img here
2. Provide a name for the application and select which supported account you would like the application to have access to. Leave the web field blank.
3. Navigate to API permissions in the newly created app and click add a permission. Add the following permissions:
a. Microsoft Graph delegated permissions
i. Directory.ReadWrite.All
ii. Group.ReadWrite.All
iii. User.Read
iv. User.ReadBasic.All
###insert img here
4. Once all the permissions have been added, select “grant admin consent for {your org name},” which will cause a checkmark and a granted string to appear under the admin consent required column.
###insert img here
5. Now go to the Manifest and change the “allowPublicClient” value to true.
###insert img here
6. Create a delegated user via Azure AD (or use an existing one) that will assign the roles to the users via the RBAC application. Note that this user should have at least the directory role of “Privileged role administrator” to be able to assign and remove directory roles.
7. Create an Azure Storage, an Azure Function App, and a ** to your resource group
a. For the Azure Storage, once created, add 3 private blob containers with the following names (lowercase):
i. “email-resources”
ii. “general-resources”
iii. “group-roles”
b. Create an Azure Storage table named: “groupRoles”
*NOTE*: These names can easily be adjusted in the source code.
c. For the Azure Function App, please make sure the following fields are selected when created:
i. OS = Windows
ii. Runtime Stack = .Net
iii. Storage = storage account recently provisioned
iv. Enable Application Insights (optional)
8. Once created, navigate to the Function App and create a function, selecting the following features:
a. Choose a development environment step = In-portal (optional - pick whichever dev environment works best for you)
b. Create a function step = Timer (optional - if you wish to have the Function app run at set times, use timer, else you can select any of the other triggers that match your needs)
9. Navigate to the function app Platform Features tab (click on the function app name in the left-hand pane to see this tab) and then open the App Service Editor under Development Tools.
10. In the Platform Features tab, navigate to Application Settings and add the following app setting fields shown in the table below (please note these setting fields are case sensitive): 
APP SETTING NAME VALUE
Auth_UserName Insert delegate user email here
Auth_UserSecret Insert delegate user password here
storageConnectionString Insert Azure Storage connection string here
RBAC_Automation:ClientId Insert app client ID here
RBAC_Automation:Instance https://login.microsoftonline.com/{0}
RBAC_Automation:MicrosoftGraphBaseEndpoint https://graph.microsoft.com
RBAC_Automation:Tenant Insert tenant ID here
RBAC_Function:Name Insert the name of the function here
RBAC_Automation:SendGridKey
11. Once in the App Service Editor, right-click on the time trigger directory and select “New Folder” and name the folder “Application.” Right-click on the newly created “Application” folder and add a new folder called “Resources.”
12. Drag-and-drop the .exe and all reference files to the “Application” folder in the App Service Editor (ensure the “Application” folder is selected before dropping the files into the folder).
13. In the 3 blob containers, add the following provided files to their respective containers:
a. Add “EmailAccounts.csv” to email-resources container.
b. Add “alert_template.html” and “newgrouprole_template.html” to general-resources container.
c. Add “GroupRoles.csv” and “RoleTemplates.txt” to group-roles container.
14. In the Azure Function time trigger app, add the provided function app .Net code to run.csx.
Sample Workflow to Assign Roles to Groups
If you have followed all the steps previously mentioned correctly, your group RBAC automation system should be ready to implement its first tasks. The next steps provide a sample workflow of what is required to add a role to a group and its respective members.
1. Navigate to the email-resources container in your Azure Storage account, click on the “EmailAccounts.csv” blob and then select Edit Blob.
2. Each line of the “EmailAccounts.csv” blob contains the name and email address (separated by a comma, no spaces) of the individuals you wish to send error and new role emails to. The format of each line in the csv should follow the format below.
Joe Smith,jsmith@company.ca
Jack Black,jblack@company.ca
Jason Newman,jnewman@company.ca
3. Navigate to the group-roles container in your Azure Storage account, click on the “GroupRoles.csv” blob and then select Edit Blob.
4. Each line of the “GroupRoles.csv” blob contains the group object id and role name (separated by a comma, no spaces) of the groups you wish to assign specific roles to. A sample csv is shown below. The “RoleTemplates.txt” contains all role template names that can be used in Azure AD.
c271c25e-cd73-4173-8c5c-84234cda3a63,Privileged Authentication Administrator
162dae11-b340-4ea0-a915-4f717a555ad9,SharePoint Service Administrator
50c8f1fc-b0b6-4b34-8a66-26649c649841,Directory Synchronization Accounts
99de9d14-62bf-4402-bd3b-5600450dd99e,CRM Service Administrator
ee1b8849-cdf3-4e4d-ae74-4934433703d1,Conditional Access Administrator
To retrieve the object id of a group, navigate to the group in Azure AD, and copy the object id from the group overview page.
5. If you have correctly added the group roles and email addresses, you should receive an email (depending on when the function app triggers) that shows the latest roles that have been applied to each user belonging to each role-assigned group.