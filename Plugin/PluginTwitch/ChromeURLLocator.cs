using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace PluginTwitch
{
    public class ChromeURLLocator : WebBrowserURLLocator
    {
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
                _URLBar = null;
                return null;
            }

            Debug.WriteLine("Chrome is running.");

            // if we can't find the URLbar chrome changed their layout again.
            if (URLBar == null)
                return null;

            Debug.WriteLine("Found urlbar.");


            // there might not be a valid pattern to use, so we have to make sure we have one
            AutomationPattern[] patterns = URLBar.GetSupportedPatterns();
            if (patterns.Length != 1)
                return null;

            Debug.WriteLine("1 pattern.");

            string ret = "";
            try
            {
                ret = ((ValuePattern)URLBar.GetCurrentPattern(patterns[0])).Current.Value;
            }
            catch { }

            if (ret == "")
                return null;

            Debug.WriteLine("Ret valid");


            // must match a domain name (and possibly "https://" in front)
            if (!Regex.IsMatch(ret, @"^(https:\/\/)?[a-zA-Z0-9\-\.]+(\.[a-zA-Z]{2,4}).*$"))
                return null;


            // prepend http:// to the url, because Chrome hides it if it's not SSL
            if (!ret.StartsWith("http"))
                ret = "http://" + ret;

            Debug.WriteLine("Is valid url: " + ret);

            return ret;
        }

        private bool ChromeIsRunning()
        {
            return Process.GetProcessesByName("chrome").Length > 0;
        }

        private AutomationElement GetURLBar()
        {
            Process[] procsChrome = Process.GetProcessesByName("chrome");
            foreach (Process chrome in procsChrome)
            {
                // the chrome process must have a window
                if (chrome.MainWindowHandle == IntPtr.Zero)
                    continue;

                try
                {
                    // find the automation element
                    AutomationElement elm = AutomationElement.FromHandle(chrome.MainWindowHandle);

                    // manually walk through the tree, searching using TreeScope.Descendants is too slow (even if it's more reliable)
                    // walking path found using inspect.exe (Windows SDK) for Chrome 52 m (currently the latest stable)
                    var elm1 = elm.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, "Google Chrome"));
                    if (elm1 == null)
                        continue; // not the right chrome.exe
                    var elm2 = elm1.FindAll(TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, ""))[1]; // Second element is the correct one
                    var elm3 = elm2.FindAll(TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, ""))[1]; // Second element is the correct one here as well
                    var elm4 = elm3.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, "main"));
                    var elm5 = elm4.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, ""));
                    return elm5.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, "Address and search bar"));
                }
                catch
                {
                    // Chrome has changed it's layout, the above code has to be modified.
                    return null;
                }
            }
            return null;
        }
    }
}
