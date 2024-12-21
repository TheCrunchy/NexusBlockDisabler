using System;
using System.Collections.Generic;
using System.Linq;
using Torch;
using Torch.API;
using Torch.API.Plugins;
using Torch.Managers;
using Torch.API.Managers;
using Torch.Session;
using Torch.API.Session;
using Torch.Managers.PatchManager;
using System.Reflection;
using Sandbox.Game.Entities.Cube;
using System.IO;
using Sandbox.Game.Entities;
using NLog;

namespace NexusBlockDisabler
{
    public static class NexusGlobalGridPatch
    {
        internal static readonly MethodInfo update =
           Type.GetType("NGPlugin.BoundarySystem.GridTransportMessage").GetMethod("prepGridsForTransfer", BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new Exception("Failed to find patch method NGPlugin.BoundarySystem.GridTransportMessage");
        internal static readonly MethodInfo updatePatch =
typeof(NexusGlobalGridPatch).GetMethod(nameof(DisableDrive), BindingFlags.Static | BindingFlags.Public) ??
throw new Exception("Failed to find patch method");


        public static void Patch(PatchContext ctx)
        {
            NexusDisableCore.Log.Error("PATCHING THE METHOD");
            ctx.GetPattern(update).Prefixes.Add(updatePatch);
        }

        public static Boolean DisableDrive(List<MyCubeGrid> gridGroups, MyCubeGrid biggestGrid)
        {
            if (!NexusDisableCore.Setup)
            {
                return true;
            }

            bool returning = true;
            var turnOff = NexusDisableCore.config.BlockPairNamesToDisable;
            //      NexusDisableCore.Log.Info(turnOff.Count + "COUNT");
            foreach (var grid in gridGroups.Where(x => x != null))
            {
                if (grid.BlocksCount <= 0 || grid == null)
                {
                    return true;
                }
                var blocks = grid.GetFatBlocks().OfType<MyFunctionalBlock>().Where(x =>
                    x.BlockDefinition != null && turnOff.Contains(x.BlockDefinition.BlockPairName));
                NexusDisableCore.Log.Info($"{blocks != null} {blocks.Count()}");
                foreach (var block in blocks)
                {
                    //   NexusDisableCore.Log.Info(block.GetType());
                    if (block != null && block is MyFunctionalBlock func)
                    {
                        if (func.Enabled)
                        {
                            //    NexusDisableCore.Log.Info("Found a drive");
                            func.Enabled = false;
                            returning = false;
                        }
                    }
                    else
                    {
                        //  NexusDisableCore.Log.Info("NOT IT");
                    }
                }
            }
            return returning;
        }
    }

    public class NexusGridPatch
    {
        internal static readonly MethodInfo update =
            Type.GetType("Nexus.BoundarySystem.GridTransport").GetMethod("PrepareGrids", BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new Exception("Failed to find patch method");
        internal static readonly MethodInfo updatePatch =
typeof(NexusGridPatch).GetMethod(nameof(DisableDrive), BindingFlags.Static | BindingFlags.Public) ??
throw new Exception("Failed to find patch method");


        public static void Patch(PatchContext ctx)
        {
            ctx.GetPattern(update).Prefixes.Add(updatePatch);
        }

        public static Boolean DisableDrive(List<MyCubeGrid> Grids, bool AutoSend = true)
        {
            if (!NexusDisableCore.Setup)
            {
                return true;
            }
            NexusDisableCore.Log.Error("Attempting drive turn off");
            bool returning = true;
            var turnOff = NexusDisableCore.config.BlockPairNamesToDisable;
            //      NexusDisableCore.Log.Info(turnOff.Count + "COUNT");
            foreach (var grid in Grids.Where(x => x != null))
            {
                if (grid.BlocksCount <= 0 || grid == null)
                {
                    return true;
                }
                var blocks = grid.GetFatBlocks().OfType<MyFunctionalBlock>().Where(x =>
                    x.BlockDefinition != null && turnOff.Contains(x.BlockDefinition.BlockPairName));
                NexusDisableCore.Log.Info($"{blocks != null} {blocks.Count()}");
                foreach (var block in blocks)
                {
                 //   NexusDisableCore.Log.Info(block.GetType());
                    if (block != null && block is MyFunctionalBlock func)
                    {
                        if (func.Enabled)
                        {
                        //    NexusDisableCore.Log.Info("Found a drive");
                            func.Enabled = false;
                            returning = false;
                        }
                    }
                    else
                    {
                      //  NexusDisableCore.Log.Info("NOT IT");
                    }
                }
            }
            return returning;
        }
    }

  
    public class NexusDisableCore : TorchPluginBase
    {
        public static bool Setup = false;
        public static Logger Log = LogManager.GetLogger("NexusBlockDisabler");
        public override void Init(ITorchBase torch)
        {
            base.Init(torch);

            var sessionManager = Torch.Managers.GetManager<TorchSessionManager>();

            if (sessionManager != null)
            {
                sessionManager.SessionStateChanged += SessionChanged;
            }

            SetupConfig();
            Setup = true;
        }

        private bool InitPlugins = false;
        public override void Update()
        {
            if (!InitPlugins)
            {
                InitPluginDependencies(Torch.Managers.GetManager<PluginManager>(), Torch.Managers.GetManager<PatchManager>());
            }
        }

        private Guid NexusGlobalGUID = Guid.Parse("28a12184-0422-43ba-a6e6-2e228611cca6");
        private Guid NexusGUID = Guid.Parse("28a12184-0422-43ba-a6e6-2e228611cca5");
        public void InitPluginDependencies(PluginManager Plugins, PatchManager Patches)
        {
            InitPlugins = true;

            if (Plugins.Plugins.TryGetValue(NexusGlobalGUID, out ITorchPlugin nexusGlobal))
            {
              NexusGlobalGridPatch.Patch(Patches.AcquireContext());
              Patches.Commit();
            }

            if (Plugins.Plugins.TryGetValue(NexusGUID, out ITorchPlugin nexus))
            {
                NexusGridPatch.Patch(Patches.AcquireContext());
                Patches.Commit();
            }
        }

        private void SetupConfig()
        {
            FileUtils utils = new FileUtils();

            if (File.Exists(StoragePath + "\\NexusBlockDisabler.xml"))
            {
                config = utils.ReadFromXmlFile<Config>(StoragePath + "\\NexusBlockDisabler.xml");
                utils.WriteToXmlFile<Config>(StoragePath + "\\NexusBlockDisabler.xml", config, false);
            }
            else
            {
                config = new Config();
                config.BlockPairNamesToDisable.Add("FSDriveSmall");
                config.BlockPairNamesToDisable.Add("FSDriveLarge");
                config.BlockPairNamesToDisable.Add("PrototechFSDriveSmall");
                config.BlockPairNamesToDisable.Add("PrototechFSDriveLarge");
                utils.WriteToXmlFile<Config>(StoragePath + "\\NexusBlockDisabler.xml", config, false);
            }

        }
        private void SessionChanged(ITorchSession session, TorchSessionState newState)
        {

        }
        public static Config config;


    }
}

