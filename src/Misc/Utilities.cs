
using System.Diagnostics;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Security.AccessControl;
using System.Security.Principal;

namespace windows_exploration 
{
    public class Utilities
    {
        public static bool IsInterestingACL(ActiveDirectoryAccessRule rule)
        {
            ActiveDirectoryRights[] interestingRights = new ActiveDirectoryRights[]
            {
                ActiveDirectoryRights.GenericAll,
                ActiveDirectoryRights.WriteOwner,
                ActiveDirectoryRights.WriteDacl,
                ActiveDirectoryRights.WriteProperty,
                ActiveDirectoryRights.Delete,
                ActiveDirectoryRights.DeleteTree
            };

            foreach (var right in interestingRights)
            {
                if ((rule.ActiveDirectoryRights & right) == right)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasObjectInterestingACL(DirectoryEntry directoryEntry)
        {
            ActiveDirectorySecurity security = directoryEntry.ObjectSecurity;
            AuthorizationRuleCollection acl = security.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
            
            foreach (ActiveDirectoryAccessRule rule in acl)
            {
                if (IsInterestingACL(rule)) {
                    return true;
                }
            }

            return false;
        }

        public static IEnumerable<ActiveDirectoryAccessRule> GetObjectInterestingACL(DirectoryEntry directoryEntry)
        {
            ActiveDirectorySecurity security = directoryEntry.ObjectSecurity;
            AuthorizationRuleCollection acl = security.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));

            foreach (ActiveDirectoryAccessRule rule in acl)
            {
                if (IsInterestingACL(rule))
                {
                    yield return rule;
                }
            }
        }

        public static bool IsRunningAsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        static void ElevateToAdministrator()
        {
            var proc = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = Process.GetCurrentProcess()?.MainModule?.FileName,
                Verb = "runas"
            };

            Process.Start(proc);
        }
        public static string GetDomainNameOrDefault(string? domainName)
        {
            return string.IsNullOrEmpty(domainName) ? Domain.GetCurrentDomain().Name : domainName;
        }

        private static bool HasCertificateTemplateEnrollmentRights(ActiveDirectorySecurity security, SecurityIdentifier userSid, List<SecurityIdentifier> groupSids)
        {
            AuthorizationRuleCollection rules = security.GetAccessRules(true, true, typeof(SecurityIdentifier));
            foreach (ActiveDirectoryAccessRule rule in rules)
            {
                if ((rule.IdentityReference == userSid || groupSids.Contains((SecurityIdentifier)rule.IdentityReference)) &&
                    rule.AccessControlType == AccessControlType.Allow)
                {
                    if (rule.ActiveDirectoryRights.HasFlag(ActiveDirectoryRights.ExtendedRight) &&
                        (rule.ObjectType == new Guid("0e10c968-78fb-11d2-90d4-00c04f79dc55") || // Certificate-Enrollment
                         rule.ObjectType == new Guid("a05b8cc2-17bc-4802-a710-e7c15ab866a2") || // Certificate-AutoEnrollment
                         rule.ObjectType == Guid.Empty)) // All ExtendedRights
                    {
                        return true;
                    }

                    if (rule.ActiveDirectoryRights.HasFlag(ActiveDirectoryRights.GenericAll) ||
                        rule.ActiveDirectoryRights.HasFlag(ActiveDirectoryRights.GenericWrite))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
