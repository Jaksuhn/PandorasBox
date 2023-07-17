using ClickLib.Clicks;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using PandorasBox.UI;
using System;
using System.Linq;
using System.Numerics;
using static ECommons.GenericHelpers;

namespace PandorasBox.Features.UI
{
    public unsafe class WorkshopHelper : Feature
    {
        public override string Name => "Materia Transmuter";

        public override string Description => "Adds a button when talking to Mutamix to loop transmuting materia.";

        public override FeatureType FeatureType => FeatureType.UI;
        private Overlays overlay;
        public static bool ResetPosition = false;
        public static bool enabled;

        private const int mutamixID = 1001425;

        public Configs Config { get; private set; }
        public override bool UseAutoConfig => true;
        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Transmute materia lower than this grade", "", 1, IntMin = 0, IntMax = 10, EditorSize = 300)]
            public int GradeCap = 9;
            [FeatureConfigOption("Exclude combat materia", "", 2)]
            public bool ExcludeCombat = false;
            [FeatureConfigOption("Exclude crafting materia", "", 3)]
            public bool ExcludeCrafting = true;
            [FeatureConfigOption("Exclude gathering materia", "", 4)]
            public bool ExcludeGathering = true;
            [FeatureConfigOption("Reverse Priority (transmute the same materia if possible, fallback on different)", "", 5)]
            public bool ReversePriority = false;
        }

        public override void Draw()
        {
            if (enabled)
            {
                var workshopWindow = Svc.GameGui.GetAddonByName("TradeMultiple", 1);
                if (Svc.Targets.Target.Name.ToString() != Svc.Data.GetExcelSheet<ENpcResident>().First(x => x.RowId == mutamixID).Singular.ToString())
                    return;
                var addonPtr = (AtkUnitBase*)workshopWindow;
                if (addonPtr == null)
                    return;

                var baseX = addonPtr->X;
                var baseY = addonPtr->Y;

                if (addonPtr->UldManager.NodeListCount > 1)
                {
                    if (addonPtr->UldManager.NodeList[1]->IsVisible)
                    {
                        var node = addonPtr->UldManager.NodeList[1];

                        if (!node->IsVisible)
                            return;

                        var position = GetNodePosition(node);
                        var scale = GetNodeScale(node);
                        var size = new Vector2(node->Width, node->Height) * scale;
                        var center = new Vector2((position.X + size.X) / 2, (position.Y - size.Y) / 2);
                        //position += ImGuiHelpers.MainViewport.Pos;

                        ImGuiHelpers.ForceNextWindowMainViewport();

                        if ((ResetPosition && position.X != 0))
                        {
                            ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(position.X + size.X + 7, position.Y + 7), ImGuiCond.Always);
                            ResetPosition = false;
                        }
                        else
                        {
                            ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(position.X + size.X + 7, position.Y + 7), ImGuiCond.FirstUseEver);
                        }

                        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(7f, 7f));
                        ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(200f, 200f));
                        ImGui.Begin($"###Options{node->NodeID}", ImGuiWindowFlags.NoScrollbar
                            | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.AlwaysUseWindowPadding);

                        DrawWindowContents();

                        ImGui.End();
                        ImGui.PopStyleVar(2);
                    }
                }
            }
        }

        public static unsafe Vector2 GetNodePosition(AtkResNode* node)
        {
            var pos = new Vector2(node->X, node->Y);
            var par = node->ParentNode;
            while (par != null)
            {
                pos *= new Vector2(par->ScaleX, par->ScaleY);
                pos += new Vector2(par->X, par->Y);
                par = par->ParentNode;
            }

            return pos;
        }

        public static unsafe Vector2 GetNodeScale(AtkResNode* node)
        {
            if (node == null) return new Vector2(1, 1);
            var scale = new Vector2(node->ScaleX, node->ScaleY);
            while (node->ParentNode != null)
            {
                node = node->ParentNode;
                scale *= new Vector2(node->ScaleX, node->ScaleY);
            }

            return scale;
        }

        private void DrawWindowContents()
        {
            if (ImGui.Button("test"))
                TryLoop();
            // right click item ContextMenu: 1 0k 0k int int
            // trigger transmute ContextMenu: 0 0 0u 0k 0k
            // InputNumeric amount 1-5
            // confirm button TradeMultiple: 0
            // SelectYesno: 0
            // Talk
            // wait
            // repeat
        }

        private void TryLoop()
        {
            TaskManager.Enqueue(() => RightClickMateria());
            TaskManager.Enqueue(() => ContextTransmute());
            TaskManager.Enqueue(() => FillNumeric());
            TaskManager.Enqueue(() => ConfirmTransmute());
            TaskManager.Enqueue(() => ConfirmYesNo());
        }

        private bool RightClickMateria()
        {
            throw new NotImplementedException();
        }

        private bool ContextTransmute()
        {
            throw new NotImplementedException();
        }

        private bool FillNumeric()
        {
            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("InputNumeric");
            if (addon == null || !IsAddonReady(addon)) return false;
            TaskManager.Enqueue(() => Callback.Fire(addon, true, 5));
            return true;
        }

        private bool ConfirmTransmute()
        {
            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("TradeMultiple", 1);
            if (addon == null || !IsAddonReady(addon)) return false;
            TaskManager.Enqueue(() => Callback.Fire(addon, false, 0));
            return true;
        }

        internal static bool ConfirmYesNo()
        {
            var transAddon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("TradeMultiple");
            if (transAddon == null) return false;

            if (transAddon->IsVisible && TryGetAddonByName<AddonSelectYesno>("SelectYesno", out var yesAddon) &&
                yesAddon->AtkUnitBase.IsVisible &&
                yesAddon->YesButton->IsEnabled &&
                yesAddon->AtkUnitBase.UldManager.NodeList[15]->IsVisible)
            {
                new ClickSelectYesNo((IntPtr)yesAddon).Yes();
                return true;
            }

            return false;
        }

        private void CheckIfUnableToTransmute(ref SeString message, ref bool isHandled)
        {
            if (message.ExtractText() == Svc.Data.GetExcelSheet<LogMessage>().First(x => x.RowId == 4252 || x.RowId == 4253 || x.RowId == 4254).Text.ExtractText())
            {
                TaskManager.Abort();
                PrintPluginMessage("Unable to continue transmutation. Ran out of materia or inventory is full");
            }
        }

        public void PrintPluginMessage(String msg)
        {
            var message = new XivChatEntry
            {
                Message = new SeStringBuilder()
                .AddUiForeground($"[{P.Name}] ", 45)
                .AddUiForeground($"[{Name}] ", 62)
                .AddText(msg)
                .Build()
            };

            Svc.Chat.PrintChat(message);
        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            overlay = new Overlays(this);
            enabled = true;
            Svc.Toasts.ErrorToast += CheckIfUnableToTransmute;
            base.Enable();
        }


        public override void Disable()
        {
            SaveConfig(Config);
            P.Ws.RemoveWindow(overlay);
            enabled = false;
            Svc.Toasts.ErrorToast -= CheckIfUnableToTransmute;
            base.Disable();
        }
    }
}
