﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using SharedLibrary;
using SharedLibrary.Interfaces;

namespace IW4MAdmin
{
    public class PluginImporter
    {
        public static List<Command> potentialCommands = new List<Command>();
        public static List<IPlugin> potentialPlugins = new List<IPlugin>();
        public static IPlugin webFront = null;
        //private static AppDomain pluginDomain;

        public static bool Load()
        {
            //pluginDomain = AppDomain.CreateDomain("Plugins");
            string[] dllFileNames = null;

            if (Directory.Exists(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\plugins"))
                dllFileNames = Directory.GetFiles(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\plugins", "*.dll");

            else
            {
                Manager.GetInstance().Logger.WriteWarning("Plugin folder does not exist!");
                return false;
            }

            if (dllFileNames == null || dllFileNames.Length == 0)
            {
                Manager.GetInstance().Logger.WriteDebug("No plugins to load");
                return true;
            }

            ICollection<Assembly> assemblies = new List<Assembly>(dllFileNames.Length);
            foreach (string dllFile in dllFileNames)
            {
                byte[] rawDLL = File.ReadAllBytes(dllFile); // because we want to update the plugin without restarting
                Assembly assembly = Assembly.Load(rawDLL);
                assemblies.Add(assembly);
            }

            int LoadedPlugins = 0;
            int LoadedCommands = 0;
            foreach (Assembly Plugin in assemblies)
            {
                if (Plugin != null)
                {
                    Type[] types = Plugin.GetTypes();
                    foreach(Type assemblyType in types)
                    {
                        if (assemblyType.IsClass && assemblyType.BaseType.Name == "Command")
                        {
                            Object commandObject = Activator.CreateInstance(assemblyType);
                            Command newCommand = (Command)commandObject;
                            potentialCommands.Add(newCommand);
                            Manager.GetInstance().Logger.WriteDebug("Registered command \"" + newCommand.Name + "\"");
                            LoadedCommands++;
                            continue;
                        }

                        try
                        {
                            if (assemblyType.GetInterface("IPlugin", false) == null)
                                continue;
                                   
                            Object notifyObject = Activator.CreateInstance(assemblyType);
                            IPlugin newNotify = (IPlugin)notifyObject;
                            if (potentialPlugins.Find(x => x.Name == newNotify.Name) == null)
                            {
                                potentialPlugins.Add(newNotify);
                                newNotify.OnLoadAsync();
                                Manager.GetInstance().Logger.WriteDebug($"Loaded plugin \"{ newNotify.Name }\" [{newNotify.Version}]");
                                LoadedPlugins++;
                            }
                        }

                        catch (Exception E)
                        {
                            Manager.GetInstance().Logger.WriteWarning($"Could not load plugin {Plugin.Location} - {E.Message}");
                        } 
                    }
                }
            }

            Manager.GetInstance().Logger.WriteInfo($"Loaded {LoadedPlugins} plugins and registered {LoadedCommands} commands.");
            return true;
        }
        
        /*
        public static void Unload()
        {

            foreach (IPlugin P in potentialPlugins)
            {
                try
                {
                    P.onUnload();
                }

                catch (Exception E)
                {
                    Manager.GetInstance().mainLog.Write("There was an error unloading \"" + P.Name + "\" plugin", Log.Level.Debug);
                    Manager.GetInstance().mainLog.Write("Error Message: " + E.Message, Log.Level.Debug);
                    Manager.GetInstance().mainLog.Write("Error Trace: " + E.StackTrace, Log.Level.Debug);
                    continue;
                }
            }

            potentialCommands = new List<Command>();
            potentialPlugins = new List<IPlugin>();
            AppDomain.Unload(pluginDomain);
        }*/
    }
}
