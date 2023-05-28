using ECommons.Automation;
using ECommons.DalamudServices;
using PandorasBox.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandorasBox.Features.Commands
{
    public unsafe class YesAlreadyToggle : CommandFeature
    {
        public override string Name => "YesAlready Toggle";
        public override string Command { get; set; } = "/p-yesalreadytoggle";
        public override string[] Alias => new string[] { "/p-yt" };

        public override List<string> Parameters => new List<string>() { };
        public override string Description => "Toggles YesAlready plugin.";
        protected override void OnCommand(List<string> args)
        {
            if (YesAlready.IsEnabled())
            {
                YesAlready.DisableIfNeeded();
                Svc.Chat.Print("Disabled YesAlready.");
                return;
            }
            if (!YesAlready.IsEnabled())
            {
                YesAlready.EnableIfNeeded();
                Svc.Chat.Print("Enabled YesAlready.");
                return;
            }
        }
    }
}
