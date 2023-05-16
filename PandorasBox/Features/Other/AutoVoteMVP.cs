using Dalamud.Game;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Gui;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.IoC;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.Logging;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace PandorasBox.Features.Other
{
    public unsafe class AutoVoteMVP : Feature
    {
        [PluginService]
        internal static PartyList PartyList { get; private set; }

        [PluginService]
        public static ChatGui ChatGui { get; private set; }

        [PluginService]
        public static SigScanner SigScanner { get; private set; }

        public override string Name => "Auto Commend";
        public override string Description => "Auto commend at the end of a duty.";

        public class Configs : FeatureConfig
        {
            public bool LeaveAfterVoting = false;
        }

        public Configs Config { get; private set; }
        public override FeatureType FeatureType => FeatureType.Other;
        public override bool UseAutoConfig => false;

        public delegate void OpenAbandonDutyDelegate(nint agent);
        public static OpenAbandonDutyDelegate OpenAbandonDuty;
        public static nint itemContextMenuAgent = nint.Zero;

        private void RunFeature(Framework framework)
        {
            if (Svc.ClientState.LocalPlayer == null) return;

            var bannerWindow = (AtkUnitBase*)Svc.GameGui.GetAddonByName("BannerMIP", 1);
            if (bannerWindow == null) return;

            try
            {
                VoteBanner(bannerWindow, ChoosePlayer());
                if (Config.LeaveAfterVoting) LeaveDuty();
            }
            catch (Exception e)
            {
                PluginLog.Error(e, "Failed to vote!");
            }
        }

        private static unsafe int ChoosePlayer()
        {
            var hud = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()
                ->GetUiModule()->GetAgentModule()->GetAgentHUD();

            if (hud == null) throw new Exception("HUD is empty!");

            var list = PartyList.Where(i =>
            i.ObjectId != Svc.ClientState.LocalPlayer.ObjectId && i.GameObject != null)
                .Select(PartyMember => (Math.Max(0, GetPartySlotIndex(PartyMember.ObjectId, hud) - 1), PartyMember));

            if (!list.Any()) throw new Exception("Party list is empty! Can't vote anyone!");

            var tanks = list.Where(i => i.PartyMember.ClassJob.GameData.Role == 1);
            var healer = list.Where(i => i.PartyMember.ClassJob.GameData.Role == 4);
            var dps = list.Where(i => !(i.PartyMember.ClassJob.GameData.Role is 1 or 4));

            (int index, PartyMember member) voteTarget;
            switch (Svc.ClientState.LocalPlayer.ClassJob.GameData.Role)
            {
                //Tank
                case 1:
                    if (tanks.Any()) voteTarget = RandomPick(tanks);
                    else if (healer.Any()) voteTarget = RandomPick(healer);
                    else voteTarget = RandomPick(dps);
                    break;

                //Healer
                case 4:
                    if (healer.Any()) voteTarget = RandomPick(healer);
                    else if (tanks.Any()) voteTarget = RandomPick(tanks);
                    else voteTarget = RandomPick(dps);
                    break;

                //DPS
                default:
                    if (dps.Any()) voteTarget = RandomPick(dps);
                    else if (tanks.Any()) voteTarget = RandomPick(tanks);
                    else voteTarget = RandomPick(healer);
                    break;
            }

            if (voteTarget.member == null) throw new Exception("No members! Can't vote!");

            ChatGui.Print(new SeString(new List<Payload>()
            {
                new TextPayload("Vote to "),
                voteTarget.member.ClassJob.GameData.Role switch
                {
                    1 => new IconPayload(BitmapFontIcon.Tank),
                    4 => new IconPayload(BitmapFontIcon.Healer),
                    _ => new IconPayload(BitmapFontIcon.DPS),
                },
                new PlayerPayload(voteTarget.member.Name.TextValue, voteTarget.member.World.GameData.RowId),
            }));
            return voteTarget.index;
        }

        static unsafe int GetPartySlotIndex(uint objectId, AgentHUD* hud)
        {
            var list = (HudPartyMember*)hud->PartyMemberList;
            for (var i = 0; i < hud->PartyMemberCount; i++)
            {
                if (list[i].ObjectId == objectId)
                {
                    return i;
                }
            }

            return 0;
        }

        private static T RandomPick<T>(IEnumerable<T> list)
        => list.ElementAt(new Random().Next(list.Count()));

        private unsafe void VoteBanner(AtkUnitBase* bannerWindow, int index)
        {
            var atkValues = (AtkValue*)Marshal.AllocHGlobal(2 * sizeof(AtkValue));
            atkValues[0].Type = atkValues[1].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int;
            atkValues[0].Int = 12;
            atkValues[1].Int = index;
            try
            {
                bannerWindow->FireCallback(2, atkValues);
            }
            finally
            {
                Marshal.FreeHGlobal(new IntPtr(atkValues));
            }
            TaskManager.EnqueueImmediate(() => EzThrottler.Throttle("Voting Player", 300));
            TaskManager.EnqueueImmediate(() => EzThrottler.Check("Voting Player"));
        }

        private bool LeaveDuty()
        {
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat]) return false;
            var dfWindow = (AtkUnitBase*)Svc.GameGui.GetAddonByName("ContentsFinderMenu", 1);
            if (dfWindow == null) return true;

            var leaveButton = stackalloc AtkValue[1];
            leaveButton[0] = new()
            {
                Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                Int = 0,
            };

            dfWindow->FireCallback(2, leaveButton);
            TaskManager.EnqueueImmediate(() => EzThrottler.Throttle("Leaving", 300));
            TaskManager.EnqueueImmediate(() => EzThrottler.Check("Leaving"));

            var yesNoWindow = (AtkUnitBase*)Svc.GameGui.GetAddonByName("SelectYesno", 1);
            if (yesNoWindow == null) return true;

            var yesValues = stackalloc AtkValue[1];
            yesValues[0] = new()
            {
                Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                Int = 0,
            };

            yesNoWindow->FireCallback(2, yesValues);
            TaskManager.EnqueueImmediate(() => EzThrottler.Throttle("Confirming", 300));
            TaskManager.EnqueueImmediate(() => EzThrottler.Check("Confirming"));

            return true;
        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }

        public override void Disable()
        {
            SaveConfig(Config);
            Svc.Framework.Update -= RunFeature;
            base.Disable();
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool _) =>
        {
            // TODO: Add a config for custom vote priority
            ImGui.Checkbox("Leave After Voting", ref Config.LeaveAfterVoting);
        };
    }
}
