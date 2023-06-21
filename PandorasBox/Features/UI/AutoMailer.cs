using PandorasBox.FeaturesSetup;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System.Collections.Generic;
using ECommons;
using ImGuiNET;
using Dalamud.Memory;
using ECommons.Logging;

namespace PandorasBox.Features.UI
{
    public unsafe class AutoMailer : Feature
    {
        public override string Name => "Auto Mailer";

        public override string Description => "Mails a specific item to a specific person.";

        public override FeatureType FeatureType => FeatureType.UI;

        // internal Overlays OverlayWindow;

        public class Configs : FeatureConfig
        {
            public string SelectedFriend = "";
            public uint SelectedItem = 17836;
            public int position_in_list = 1;
        }

        public Configs Config { get; private set; }
        public override bool UseAutoConfig => false;

        protected override DrawConfigDelegate DrawConfigTree => (ref bool _) =>
        {
            var agent = AgentFriendList.Instance();
            if (agent == null) return;
            if (ImGui.BeginCombo("Select Friend", ""))
            {
                if (ImGui.Selectable("", Config.SelectedFriend == ""))
                {
                    Config.SelectedFriend = "";
                }
                for (var i = 0U; i < agent->Count; i++)
                {
                    var friend = agent->GetFriend(i);
                    if (friend == null)
                    {
                        PluginLog.Log("Null friend entry. Skipping.");
                        continue;
                    }
                    if (friend->HomeWorld != Svc.ClientState.LocalPlayer.HomeWorld.Id) continue;
                    try
                    {
                        var name = MemoryHelper.ReadString(new nint(friend->Name), 32);
                        var selected = ImGui.Selectable(name, Config.SelectedFriend == name);
                        if (selected)
                        {
                            Config.SelectedFriend = name;
                        }
                    }
                    catch { return; }
                }
            }
        };

        private void TryAutoMailer()
        {
            TaskManager.Enqueue(() => SelectNew(), "Selecting New");
            TaskManager.Enqueue(() => OpenRecipient(), "Opening Dropdown");
            TaskManager.Enqueue(() => SelectRecipient(Config.SelectedFriend, Config.position_in_list), "Selecting Friend");
            TaskManager.Enqueue(() => AttachItem(Config.SelectedItem), "Attaching Item");
            TaskManager.Enqueue(() => SendButton(Config.SelectedFriend), "Selecting Send");
            // select yes function or something
            TaskManager.DelayNext("WaitForDelay", 400);
        }

        private unsafe bool? SelectNew()
        {
            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("LetterList", 1);
            if (!Svc.Condition[ConditionFlag.OccupiedInEvent] || !Svc.Condition[ConditionFlag.NormalConditions] || addon == null || !GenericHelpers.IsAddonReady(addon)) return false;
            TaskManager.EnqueueImmediate(() => EzThrottler.Throttle("Selecting New", 300));
            TaskManager.EnqueueImmediate(() => EzThrottler.Check("Selecting New"));
            try
            {
                var letterslistPTR = Svc.GameGui.GetAddonByName("LetterList", 1);
                if (letterslistPTR == IntPtr.Zero)
                    return false;

                var letterslistWindow = (AtkUnitBase*)letterslistPTR;
                if (letterslistWindow == null)
                    return false;

                Callback.Fire(letterslistWindow, false, 1, 0, 0, 0);

                // var NewButton = stackalloc AtkValue[4];
                // NewButton[0] = new()
                // {
                //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                //     Int = 1,
                // };
                // NewButton[1] = new()
                // {
                //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                //     Int = 0,
                // };
                // NewButton[2] = new()
                // {
                //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                //     Int = 0,
                // };

                // NewButton[3] = new()
                // {
                //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                //     Int = 0,
                // };

                // letterslistWindow->FireCallback(1, NewButton);
                return true;
            }
            catch
            {
                return false;
            }
        }
        private unsafe bool? OpenRecipient()
        {
            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("LetterEditor", 1);
            if (!Svc.Condition[ConditionFlag.OccupiedInEvent] || !Svc.Condition[ConditionFlag.NormalConditions] || addon == null || !GenericHelpers.IsAddonReady(addon)) return false;
            TaskManager.EnqueueImmediate(() => EzThrottler.Throttle("Selecting Dropdown", 300));
            TaskManager.EnqueueImmediate(() => EzThrottler.Check("Selecting Dropdown"));
            try
            {
                var letterEditorPTR = Svc.GameGui.GetAddonByName("LetterEditor", 1);
                if (letterEditorPTR == IntPtr.Zero)
                    return false;

                var letterEditorWindow = (AtkUnitBase*)letterEditorPTR;
                if (letterEditorWindow == null)
                    return false;

                Callback.Fire(letterEditorWindow, false, 7);
                // var DropDownMenu = stackalloc AtkValue[1];
                // DropDownMenu[0] = new()
                // {
                //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                //     Int = 7,
                // };

                // letterEditorWindow->FireCallback(1, DropDownMenu);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private unsafe bool? SelectRecipient(string friend, int position_in_list)
        {
            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("LetterAddress", 1);
            if (!Svc.Condition[ConditionFlag.OccupiedInEvent] || !Svc.Condition[ConditionFlag.NormalConditions] || addon == null || !GenericHelpers.IsAddonReady(addon)) return false;
            TaskManager.EnqueueImmediate(() => EzThrottler.Throttle("Selecting Friend", 300));
            TaskManager.EnqueueImmediate(() => EzThrottler.Check("Selecting Friend"));
            try
            {
                var letterAddressPTR = Svc.GameGui.GetAddonByName("LetterAddress", 1);
                if (letterAddressPTR == IntPtr.Zero)
                    return false;

                var letterAddressWindow = (AtkUnitBase*)letterAddressPTR;
                if (letterAddressWindow == null)
                    return false;

                Callback.Fire(addon, false, 0, position_in_list, friend);
                // var FriendList = stackalloc AtkValue[3];
                // FriendList[0] = new()
                // {
                //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                //     Int = 7,
                // };
                // FriendList[1] = new()
                // {
                //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                //     Int = position_in_list,
                // };
                // FriendList[2] = new()
                // {
                //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.String,
                //     String = Marshal.FreeHGlobal(new IntPtr(friend)),
                // };

                // letterAddressWindow->FireCallback(1, FriendList);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private unsafe bool? AttachItem(uint itemId)
        {
            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("LetterAddress", 1);
            if (!Svc.Condition[ConditionFlag.OccupiedInEvent] || !Svc.Condition[ConditionFlag.NormalConditions] || addon == null || !GenericHelpers.IsAddonReady(addon)) return false;

            var invId = AgentModule.Instance()->GetAgentByInternalId(AgentId.Inventory)->GetAddonID();

            if (InventoryManager.Instance()->GetInventoryItemCount(itemId) == 0)
            {
                return true;
            }
            if (!Common.GetAddonByID(invId)->IsVisible)
            {
                return null;
            }

            var inventories = new List<InventoryType>
            {
                InventoryType.Inventory1,
                InventoryType.Inventory2,
                InventoryType.Inventory3,
                InventoryType.Inventory4,
            };

            foreach (var inv in inventories)
            {
                var container = InventoryManager.Instance()->GetInventoryContainer(inv);
                for (int i = 0; i < container->Size; i++)
                {
                    var item = container->GetInventorySlot(i);

                    if (item->ItemID == itemId)
                    {
                        var ag = AgentInventoryContext.Instance();
                        ag->OpenForItemSlot(container->Type, i, AgentModule.Instance()->GetAgentByInternalId(AgentId.Inventory)->GetAddonID());
                        var contextMenu = (AtkUnitBase*)Svc.GameGui.GetAddonByName("ContextMenu", 1);
                        if (contextMenu != null)
                        {
                            Callback.Fire(contextMenu, false, 0, 0u, 0u, 0u, 0u);
                            // var values = stackalloc AtkValue[5];
                            // values[0] = new AtkValue()
                            // {
                            //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                            //     Int = 0
                            // };
                            // values[1] = new AtkValue()
                            // {
                            //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                            //     UInt = 0
                            // };
                            // values[2] = new AtkValue()
                            // {
                            //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
                            //     UInt = 0
                            // };
                            // values[3] = new AtkValue()
                            // {
                            //     // Unknown Type: 0
                            //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
                            //     UInt = 0
                            // };
                            // values[4] = new AtkValue()
                            // {
                            //     // Unknown Type: 0
                            //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
                            //     UInt = 0
                            // };
                            // contextMenu->FireCallback(5, values, (void*)1);

                            // TaskManager.Enqueue(() => ActionManager.Instance()->GetActionStatus(ActionType.Item, itemId, Svc.ClientState.LocalPlayer.ObjectId) == 0);

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private unsafe bool? SendButton(string friend)
        {
            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("LetterEditor", 1);
            if (!Svc.Condition[ConditionFlag.OccupiedInEvent] || !Svc.Condition[ConditionFlag.NormalConditions] || addon == null || !GenericHelpers.IsAddonReady(addon)) return false;
            TaskManager.EnqueueImmediate(() => EzThrottler.Throttle("Selecting Send", 300));
            TaskManager.EnqueueImmediate(() => EzThrottler.Check("Selecting Send"));
            try
            {
                var letterEditorPTR = Svc.GameGui.GetAddonByName("LetterEditor", 1);
                if (letterEditorPTR == IntPtr.Zero)
                    return false;

                var letterEditorWindow = (AtkUnitBase*)letterEditorPTR;
                if (letterEditorWindow == null)
                    return false;

                Callback.Fire(addon, false, 0, 0, friend, "", 0u, 0u);


                // var SendBtn = stackalloc AtkValue[6];
                // SendBtn[0] = new()
                // {
                //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                //     Int = 0,
                // };
                // SendBtn[1] = new()
                // {
                //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                //     Int = 0,
                // };
                // SendBtn[2] = new()
                // {
                //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.String,
                //     String = friend,
                // };
                // SendBtn[3] = new()
                // {
                //     // it's empty???
                //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.String,
                // };
                // SendBtn[4] = new()
                // {
                //     // Unknown Type: 0
                //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
                //     UInt = 0
                // };
                // SendBtn[5] = new()
                // {
                //     // Unknown Type: 0
                //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
                //     UInt = 0
                // };

                // letterEditorWindow->FireCallback(1, SendBtn);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            // OverlayWindow = new(this);
            // P.Ws.AddWindow(OverlayWindow);
            base.Enable();
        }

        public override void Disable()
        {
            SaveConfig(Config);
            // P.Ws.RemoveWindow(OverlayWindow);
            // OverlayWindow = null;
            base.Disable();
        }
    }
}
