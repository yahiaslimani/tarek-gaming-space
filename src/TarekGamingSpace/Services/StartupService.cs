using System;
using System.Reflection;
using Microsoft.Win32;

namespace TarekGamingSpace.Services
{
    /// <summary>"Start with Windows" through the current user's Run key (no admin rights needed).</summary>
    public static class StartupService
    {
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string ValueName = "TarekGamingSpace";
        public const string StartupArg = "--startup";

        public static void Apply(bool enabled)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(RunKey))
                {
                    if (enabled)
                        key.SetValue(ValueName, $"\"{Assembly.GetEntryAssembly().Location}\" {StartupArg}");
                    else if (key.GetValue(ValueName) != null)
                        key.DeleteValue(ValueName, false);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Could not update the startup entry", ex);
            }
        }
    }
}
