﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using Sandbox.Game.GameSystems;
using Sandbox.Game.Entities;
using NLog;

namespace NexusBlockDisabler
{
    [PatchShim]
    public class NexusGridPatch
    {
        internal static readonly MethodInfo update =
           typeof(Nexus.BoundarySystem.GridTransport).GetMethod("PrepareGrids", BindingFlags.Instance | BindingFlags.NonPublic) ??
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

