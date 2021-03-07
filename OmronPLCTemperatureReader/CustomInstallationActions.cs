using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;

namespace OmronPLCTemperatureReader
{
    [RunInstaller(true)]
    public partial class CustomInstallationActions : Installer
    {
        public CustomInstallationActions()
        {
            InitializeComponent();
        }

        // private string FolderPathContextKey = "targetdir"; // /TargetDir="[TARGETDIR]"
        private string ExePathContextKey = "assemblypath";
        private string FirewallRuleDisplayName = "PLC Monitor";

        protected override void OnAfterInstall(IDictionary savedState)
        {
            base.OnAfterInstall(savedState);
            AddFirewallRule();
        }

        protected override void OnAfterUninstall(IDictionary savedState)
        {
            base.OnAfterUninstall(savedState);
            RemoveFirewallRule();
        }

        private void AddFirewallRule()
        {
            string addFirewallRuleCommand = $"New-NetFirewallRule -DisplayName '{FirewallRuleDisplayName}' -Direction Inbound -Program '{Context.Parameters[ExePathContextKey]}' -Action Allow -Enabled True";
            ExecutePowerShellCommand(addFirewallRuleCommand);
        }

        private void RemoveFirewallRule()
        {
            string removeFirewallRuleCommand = $"Remove-NetFirewallRule -DisplayName '{FirewallRuleDisplayName}'";
            ExecutePowerShellCommand(removeFirewallRuleCommand);
        }

        private void ExecutePowerShellCommand(string command)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = @"powershell.exe";
            startInfo.Arguments = "-Command \"& {" + command + "}\"";
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            Process process = new Process();
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
        }




    }
}
