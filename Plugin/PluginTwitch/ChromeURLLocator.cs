using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Rainmeter;

namespace PluginTwitchChat
{
    public class ChromeURLLocator : WebBrowserURLLocator
    {
        private bool ManualWalkFailed = false;
        private List<AutomationElement> urlBars = new List<AutomationElement>();
        private HashSet<int> checkedProcesses = new HashSet<int>();

        public override string GetActiveUrl()
        {
            UpdateURLBars();
            foreach (var urlbar in urlBars)
            {
                // If the URLBar has focus the user might be typing and the URL is probably not valid
                if ((bool)urlbar.GetCurrentPropertyValue(AutomationElement.HasKeyboardFocusProperty))
                    continue;

                // there might not be a valid pattern to use, so we have to make sure we have one
                AutomationPattern[] patterns = urlbar.GetSupportedPatterns();
                if (patterns.Length != 1)
                    continue;

                string ret;
                try
                {
                    ret = ((ValuePattern)urlbar.GetCurrentPattern(patterns[0])).Current.Value;
                }
                catch
                {
                    // error occured
                    continue;
                }

                // must match a domain name (and possibly "https://" in front)
                if (!Regex.IsMatch(ret, @"^(https:\/\/)?[a-zA-Z0-9\-\.]+(\.[a-zA-Z]{2,4}).*$"))
                    continue;

                return ret;
            }
            return null;
        }

        private void UpdateURLBars()
        {
            foreach (Process proc in Process.GetProcessesByName("chrome"))
            {
                if (checkedProcesses.Contains(proc.Id))
                    continue;

                checkedProcesses.Add(proc.Id);
                // the chrome process must have a window
                if (proc.MainWindowHandle == IntPtr.Zero)
                    continue;

                // find the automation element
                AutomationElement elm = AutomationElement.FromHandle(proc.MainWindowHandle);

                var chromeMain = elm.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, "Google Chrome"));
                if (chromeMain == null)
                    continue; // not the right chrome.exe

                var urlBar = GetURLBar(chromeMain);
                urlBars.Add(urlBar);
            }
        }

        private AutomationElement GetURLBar(AutomationElement mainChrome)
        {
            if (mainChrome == null)
                return null;

            AutomationElement bar = null;
            if (!ManualWalkFailed)
            {
                bar = ManualWalk(mainChrome);
                ManualWalkFailed = bar == null;
            }

            return bar ?? AutomaticWalk(mainChrome);
        }

        // manually walk through the tree
        // walking path found using inspect.exe (Windows SDK) for Chrome  52.0.2743.116 m
        private AutomationElement ManualWalk(AutomationElement mainChrome)
        {
            try
            {
                var elm1 = mainChrome.FindAll(TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, ""))[1]; // Second element is the correct one
                var elm2 = elm1.FindAll(TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, ""))[1]; // Second element is the correct one here as well
                var elm3 = elm2.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, "main"));
                var elm4 = elm3.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, ""));
                return elm4.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, "Address and search bar"));
            }
            catch
            {
                // Walk failed
                API.Log(API.LogType.Warning, "Manual walk to find URL Bar in Chrome failed!");
                return null;
            }

        }

        // This should be reliable between versions but is very slow.
        private AutomationElement AutomaticWalk(AutomationElement mainChrome)
        {
            return mainChrome.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, "Address and search bar"));
        }
    }
}
