using Dalamud.Game.ClientState.Conditions;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandorasBox.Features.Commands
{
    public unsafe class JoinDF : CommandFeature
    {
        public override string Name => "Join Duty Finder";
        public override string Command { get; set; } = "/pan-join";
        public override string[] Alias => new string[] { "/pan-j" };

        public override List<string> Parameters => new List<string>() { "test", "test2", "test3" };
        public override string Description => "Activates the join button in duty finder to queue into whatever duty is selected. Optionally provide a duty as argument to select that one.";
        protected override void OnCommand(List<string> args)
        {
            foreach (var p in Parameters)
            {
                if (args.Any(x => x == p))
                {
                    Svc.Chat.Print($"Test command executed with argument {p}.");
                    SelectJoin();
                }
            }
        }

        private unsafe bool? SelectJoin()
        {
            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("ContentsFinder", 1);
            if (!Svc.Condition[ConditionFlag.NormalConditions] || addon == null || !GenericHelpers.IsAddonReady(addon))
            {
                // GenericHelpers.CommandProcessor.ExecuteThrottled("/dutyfinder");
            }
            TaskManager.EnqueueImmediate(() => EzThrottler.Throttle("Selecting Join", 500));
            TaskManager.EnqueueImmediate(() => EzThrottler.Check("Selecting Join"));
            try
            {
                var dfPTR = Svc.GameGui.GetAddonByName("ContentsFinder", 1);
                if (dfPTR == IntPtr.Zero)
                    return false;

                var dfWindow = (AtkUnitBase*)dfPTR;
                if (dfWindow == null)
                    return false;

                var JoinButtonClick = stackalloc AtkValue[4];
                JoinButtonClick[0] = new()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 12,
                };
                JoinButtonClick[1] = new()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 0,
                };

                dfWindow->FireCallback(1, JoinButtonClick);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
