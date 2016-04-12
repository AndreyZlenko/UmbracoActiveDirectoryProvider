using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Configuration.Provider;
using System.Diagnostics;
using System.DirectoryServices;
using System.Linq;
using System.Web.Hosting;
using System.Web.Security;

namespace UmbracoActiveDirectoryProvider
{
    public class UmbracoActiveDirectoryRoleProvider : RoleProvider
    {
        private string m_ConnectionString;
        private string m_ConnectionUsername;
        private string m_ConnectionPassword;

        public override string ApplicationName { get; set; }

        private List<string> m_GroupsToUse;

        private readonly string[] m_GroupsToIgnore =
        {
            "Domain Guests", "Domain Computers", "Group Policy Creator Owners", "Guests", "Users",
            "Domain Users", "Pre-Windows 2000 Compatible Access", "Exchange Domain Servers", "Schema Admins",
            "Enterprise Admins", "Domain Admins", "Cert Publishers", "Backup Operators", "Account Operators",
            "Server Operators", "Print Operators", "Replicator", "Domain Controllers", "WINS Users",
            "DnsAdmins", "DnsUpdateProxy", "DHCP Users", "DHCP Administrators", "Exchange Services",
            "Exchange Enterprise Servers", "Remote Desktop Users", "Network Configuration Operators",
            "Incoming Forest Trust Builders", "Performance Monitor Users", "Performance Log Users",
            "Windows Authorization Access Group", "Terminal Server License Servers", "Distributed COM Users",
            "Administrators", "Everybody", "RAS and IAS Servers", "MTS Trusted Impersonators",
            "MTS Impersonators", "Everyone", "LOCAL", "Authenticated Users"
        };

        public override void Initialize(String name, NameValueCollection config)
        {
            // Setup config details
            if (config == null)
                throw new ArgumentNullException("config");

            if (string.IsNullOrEmpty(name))
                name = "ActiveDirectoryRoleProvider";

            if (string.IsNullOrEmpty(config["description"]))
            {
                config.Remove("description");
                config.Add("description", "Active Directory Role Provider");
            }

            base.Initialize(name, config);

            // Find the ldap connection string
            if (string.IsNullOrEmpty(config["connectionStringName"]))
                throw new ProviderException("The attribute 'connectionStringName' is missing or empty.");

            var connObj = ConfigurationManager.ConnectionStrings[config["connectionStringName"]];

            if (connObj != null)
                m_ConnectionString = connObj.ConnectionString;

            if (string.IsNullOrEmpty(m_ConnectionString))
                throw new ProviderException("The connection name 'connectionStringName' was not found in the applications configuration or the connection string is empty.");

            // Find the app name
            ApplicationName = config["applicationName"];

            if (string.IsNullOrEmpty(ApplicationName))
                ApplicationName = GetDefaultAppName();

            if (ApplicationName.Length > 256)
                throw new ProviderException("The application name is too long.");

            // Find connection credentdials
            m_ConnectionUsername = config["connectionUsername"];
            m_ConnectionPassword = config["connectionPassword"];

            // Find groups and users to ignore as we do not want
            // to use every single group or user from AD
            m_GroupsToUse = new List<string>();

            if (string.IsNullOrEmpty(config["groupsToUse"]))
                return;

            foreach (var group in config["groupsToUse"].Trim().Split(','))
                m_GroupsToUse.Add(group.Trim());
        }

        public override string[] GetRolesForUser(string username)
        {
            var results = new ArrayList();

            // Connect and query active directory
            var context = new DirectoryEntry(m_ConnectionString, m_ConnectionUsername, m_ConnectionPassword);
            var searcher = new DirectorySearcher(context, "(sAMAccountName=" + username + ")");

            var result = searcher.FindOne();
            if (result != null)
            {
                // Find the user's list of groups
                var groups = result.Properties["memberof"] as IEnumerable;
                if (groups != null)
                {
                    foreach (string group in groups)
                    {
                        if (!group.StartsWith("CN="))
                            continue;

                        // Get the group name
                        var groupName = group.Substring(3, group.IndexOf(",", StringComparison.Ordinal) - 3);

                        // Should we ignore it?
                        if (m_GroupsToIgnore.Contains(groupName))
                            continue;

                        results.Add(groupName);
                    }
                }
            }

            return results.ToArray(typeof(string)) as string[];
        }

        public override bool IsUserInRole(string username, string rolename)
        {
            var userRoles = GetRolesForUser(username);
            return userRoles.Contains(rolename);
        }

        public override string[] GetAllRoles()
        {
            return m_GroupsToUse.ToArray();
        }

        public override bool RoleExists(string rolename)
        {
            return GetAllRoles().Contains(rolename);
        }

        // Not implemented methods

        public override string[] GetUsersInRole(string rolename)
        {
            throw new NotImplementedException();
        }

        public override string[] FindUsersInRole(string rolename, string usernameToMatch)
        {
            throw new NotImplementedException();
        }

        // Non supported methods

        public override void AddUsersToRoles(string[] usernames, string[] rolenames)
        {
            throw new NotSupportedException("Unable to add users to roles. For security and management purposes, ActiveDirectoryRoleProvider only supports read operations against Active Direcory.");
        }

        public override void CreateRole(string rolename)
        {
            throw new NotSupportedException("Unable to create new role. For security and management purposes, ActiveDirectoryRoleProvider only supports read operations against Active Direcory.");
        }

        public override bool DeleteRole(string rolename, bool throwOnPopulatedRole)
        {
            throw new NotSupportedException("Unable to delete role. For security and management purposes, ActiveDirectoryRoleProvider only supports read operations against Active Direcory.");
        }

        public override void RemoveUsersFromRoles(string[] usernames, string[] rolenames)
        {
            throw new NotSupportedException("Unable to remove users from roles. For security and management purposes, ActiveDirectoryRoleProvider only supports read operations against Active Direcory.");
        }

        // Helper methods

        private static string GetDefaultAppName()
        {
            try
            {
                var appName = HostingEnvironment.ApplicationVirtualPath;

                if (!string.IsNullOrEmpty(appName))
                    return string.IsNullOrEmpty(appName) ? "/" : appName;

                appName = Process.GetCurrentProcess().MainModule.ModuleName;

                var indexOfDot = appName.IndexOf('.');
                if (indexOfDot != -1)
                    appName = appName.Remove(indexOfDot);

                return string.IsNullOrEmpty(appName) ? "/" : appName;
            }
            catch
            {
                return "/";
            }
        }
    }
}
