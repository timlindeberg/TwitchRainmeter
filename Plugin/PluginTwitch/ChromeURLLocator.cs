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
        private AutomationElement _URLBar;
        private AutomationElement URLBar
        {
            get
            {
                if (_URLBar == null)
                    _URLBar = GetURLBar();
                return _URLBar;
            }
        }

        public override string GetActiveUrl()
        {
            if (!ChromeIsRunning())
            {
                // Reset the URL bar if Chrome was closed down
                _URLBar = null;
                return null;
            }

            // if we can't find the URLbar chrome changed their layout and even the automatic walk can't find it.
            if (URLBar == null)
                return null;

            // If the URLBar has focus the user might be typing and the URL is probably not valid
            if ((bool)URLBar.GetCurrentPropertyValue(AutomationElement.HasKeyboardFocusProperty))
                return null;

            // there might not be a valid pattern to use, so we have to make sure we have one
            AutomationPattern[] patterns = URLBar.GetSupportedPatterns();
            if (patterns.Length != 1)
                return null;

            string ret;
            try
            {
                ret = ((ValuePattern)URLBar.GetCurrentPattern(patterns[0])).Current.Value;
            }
            catch
            {
                // error occured
                return null;
            }

            // must match a domain name (and possibly "https://" in front)
            if (!Regex.IsMatch(ret, @"^(https:\/\/)?[a-zA-Z0-9\-\.]+(\.[a-zA-Z]{2,4}).*$"))
                return null;

            return ret;
        }

        private bool ChromeIsRunning()
        {
            return Process.GetProcessesByName("chrome").Length > 0;
        }

        private AutomationElement GetURLBar()
        {
            var mainChrome = GetMainChromeElement();
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

        private AutomationElement GetMainChromeElement()
        {
            foreach (Process chrome in Process.GetProcessesByName("chrome"))
            {
                // the chrome process must have a window
                if (chrome.MainWindowHandle == IntPtr.Zero)
                    continue;

                // find the automation element
                AutomationElement elm = AutomationElement.FromHandle(chrome.MainWindowHandle);

                var chromeMain = elm.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, "Google Chrome"));
                if (chromeMain == null)
                    continue; // not the right chrome.exe

                return chromeMain;
            }
            return null;
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
