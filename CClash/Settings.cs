﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Win32;

namespace CClash
{
    public sealed class Settings
    {
        public static bool DebugEnabled { get; set; }
        public static string DebugFile { get; set; }

        public static string MissLogFile { get; set; }
        public static bool MissLogEnabled { 
            get 
            {
                return !string.IsNullOrEmpty(MissLogFile); 
            }
        }

        static Settings() { }

        static bool ConditionVarsAreTrue(string prefix, Dictionary<string, string> envs )
        {
            string var = null;
            string varvalue = null;
            if (envs.TryGetValue(prefix + "_VAR", out var) && envs.TryGetValue( var, out varvalue ) )
            {
                string values = null;
                if (envs.TryGetValue(prefix + "_VALUES", out values))
                {
                    var vlist = values.Split(',');
                    foreach (var v in vlist)
                    {
                        if (v == varvalue) return true;
                    }
                }
            }
            return false;
        }

        static bool EnabledByConditions(Dictionary<string, string> envs)
        {
            return ConditionVarsAreTrue("CCLASH_ENABLE_WHEN", envs);
        }

        static bool DisabledByConditions(Dictionary<string, string> envs)
        {
            return ConditionVarsAreTrue("CCLASH_DISABLE_WHEN", envs);
        }

        static Dictionary<string, string> getEdict()
        {
            var envs = Environment.GetEnvironmentVariables();
            var edict = new Dictionary<string, string>(envs.Count);
            foreach (var n in envs.Keys)
            {
                edict.Add((string)n, (string)envs[n]);
            }
            return edict;
        }

        public static bool Disabled
        {
            get
            {
                var edict = getEdict();
                return edict.ContainsKey("CCLASH_DISABLED") || DisabledByConditions(edict) && (!EnabledByConditions(edict));
            }
        }

        static string cachedir = null;
        public static string CacheDirectory
        {
            get
            {
                if (cachedir == null)
                {
                    cachedir = Environment.GetEnvironmentVariable("CCLASH_DIR");
                    if (string.IsNullOrEmpty(cachedir))
                    {
                        var appdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                        cachedir = System.IO.Path.Combine(appdata, "cclash");
                    }
                }
                return cachedir;
            }
            set
            {
                cachedir = value;
            }
        }

        public static bool PreprocessorMode
        {
            get
            {
                return false;
            }
        }

        public static bool DirectMode
        {
            get
            {
                return !PreprocessorMode;
            }
        }

        public static bool ServiceMode
        {
            get
            {
                return Environment.GetEnvironmentVariable("CCLASH_SERVER") != null;
            }
        }

        private static int hashThreadCount;
        public static int HashThreadCount
        {
            get
            {
                if (hashThreadCount == 0) hashThreadCount = Environment.ProcessorCount;
                return hashThreadCount;
            }
            set
            {
                hashThreadCount = value;
            }
        }

        public static bool TryHardLinks
        {
            get
            {
                return Environment.GetEnvironmentVariable("CCLASH_TRY_HARDLINKS") != null;
            }
        }

        public static bool BypassPotentialIncludeChecks
        {
            get
            {
                return Environment.GetEnvironmentVariable("CCLASH_LAZY_NEW_INCLUDES") != null;

            }
        }

        public static bool GetServicePort(string cachedir, out int portnumber)
        {
            portnumber = -1;
            var r = GetCClashDirRegKey();
            portnumber = (int)r.GetValue(cachedir, portnumber);
            return portnumber > 0;
        }

        public static void SetServicePort(string cachedir, int portnumber)
        {
            var r = GetCClashDirRegKey();
            r.SetValue(cachedir, portnumber, RegistryValueKind.DWord);
        }

        const string CClashRegKeyName = "cclash";

        static RegistryKey GetCClashDirRegKey()
        {
            var cc = Registry.CurrentUser.OpenSubKey(CClashRegKeyName);
            if (cc == null)
            {
                cc = Registry.CurrentUser.CreateSubKey(CClashRegKeyName);
            }
            var cdir = cc.OpenSubKey("cachedir");
            if (cdir == null)
            {
                cdir = cc.CreateSubKey("cachedir");
            }
            return cdir;
        }
    }
}
