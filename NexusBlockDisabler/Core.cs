using System;
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
   //     internal static readonly MethodInfo getPcu = Type.GetType("SKO.GridPCULimiter.GridPCULimiterConfig").GetProperty("MaxGridPCU", BindingFlags.Instance | BindingFlags.Public).GetAccessors()[0] ?? throw new Exception("Failed to find patch method");
      

     //   internal static readonly MethodInfo update =
   //        typeof(Nexus.BoundarySystem.GridTransport).GetMethod("PrepareGrids", BindingFlags.Instance | BindingFlags.NonPublic);
        internal static readonly MethodInfo updatePatch =
typeof(NexusGridPatch).GetMethod(nameof(DisableDrive), BindingFlags.Static | BindingFlags.Public) ??
throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo getPCUPatch =
            typeof(NexusGridPatch).GetMethod(nameof(ReturnPCU), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");

        //        internal static readonly MethodInfo afterSpawn =
        //        typeof(Nexus.BoundarySystem.GridTransport).GetMethod("AfterGridSpawn", BindingFlags.Instance | BindingFlags.NonPublic) ??
        //         throw new Exception("Failed to find patch method");
        //        internal static readonly MethodInfo afterSpawnPatch =
        //typeof(NexusGridPatch).GetMethod(nameof(AfterGridSpawn), BindingFlags.Static | BindingFlags.Public) ??
        //throw new Exception("Failed to find patch method");


        public static void Patch(PatchContext ctx)
        {
            var getPcu = Type.GetType("SKO.GridPCULimiter.GridPCULimiterConfig")
                .GetProperty("MaxGridPCU", BindingFlags.Instance | BindingFlags.Public);

            NexusDisableCore.Log.Info("IS NULL? " + getPcu != null);
            //    if (update != null)
            //   {
            //     ctx.GetPattern(update).Prefixes.Add(updatePatch);
            // }

            // ctx.GetPattern(getPcu).Suffixes.Add(getPCUPatch);
            //   ctx.GetPattern(afterSpawn).Prefixes.Add(afterSpawnPatch);
        }
        public static void AfterGridSpawn(HashSet<MyCubeGrid> SpawnedGrids)
        {
            var turnOff = NexusDisableCore.config.BlockPairNamesToDisable;
            // NexusDisableCore.Log.Info(turnOff.Count + "COUNT");
            foreach (var grid in SpawnedGrids)
            {
                foreach (var block in grid.GetFatBlocks().Where(x => turnOff.Contains(x.BlockDefinition.BlockPairName)))
                {
                    if (block is MyFunctionalBlock func)
                    {
                        if (func.Enabled)
                        {
                            func.Enabled = true;
                        }
                    }
                }
            }
        }

        public static void ReturnPCU(long __result)
        {
            NexusDisableCore.Log.Info("Getting PCU");
            __result = 5;
        }

        public static Boolean DisableDrive(List<MyCubeGrid> Grids, bool AutoSend = true)
        {
            bool returning = true;
            var turnOff = NexusDisableCore.config.BlockPairNamesToDisable;
           // NexusDisableCore.Log.Info(turnOff.Count + "COUNT");
            foreach (var grid in Grids)
            {
                foreach (var block in grid.GetFatBlocks().Where(x => turnOff.Contains(x.BlockDefinition.BlockPairName)))
                {
                  //  NexusDisableCore.Log.Info(block.GetType());
                    if (block is MyFunctionalBlock func)
                    {
                        if (func.Enabled)
                        {
                       //     NexusDisableCore.Log.Info("Found a drive");
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
                config.BlockPairNamesToDisable.Add("FSDrive");
                utils.WriteToXmlFile<Config>(StoragePath + "\\NexusBlockDisabler.xml", config, false);
            }

        }
        private void SessionChanged(ITorchSession session, TorchSessionState newState)
        {

        }
        public static Config config;


    }
}

