using Dalamud.Game.ClientState.Conditions;
using Dalamud.Utility.Signatures;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using Lumina.Excel.GeneratedSheets;
using Lumina.Excel;
using PandorasBox.FeaturesSetup;

namespace PandorasBox.Features.Commands
{
    public unsafe class JoinDF : CommandFeature
    {
        public override string Name => "Join Duty Finder";
        public override string Command { get; set; } = "/pan-join";
        public override string[] Alias => new string[] { "/pan-j" };

        public override List<string> Parameters => new List<string>() { "test", "test2", "test3" };
        public override string Description => "Activates the join button in duty finder to queue into whatever duty is selected. Optionally provide a duty as argument to select that one.";

        public Configs Config { get; private set; }

        public override bool UseAutoConfig => true;

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Fuzzy Matching Threshold", IntMin = 0, IntMax = 10, EditorSize = 300)]
            public int fuzzyMatchingThreshold = 4;
        }
        protected override void OnCommand(List<string> args)
        {
            // foreach (var p in Parameters)
            // {
            //     if (args.Any(x => x == p))
            //     {
            //         Svc.Chat.Print($"Test command executed with argument {p}.");
            //         // var gatheringPoint = Svc.Data.GetExcelSheet<ContentFinderCondition>().First(x => x.RowId == duty);
            //         SelectJoin();
            //     }
            // }

            var arg = string.Join(" ", args);
            List<string> allowedContentTypes = new List<string>()
            {
                "Dungeons",
                "Guildhests",
                "Trials",
                "Raids",
                "PvP",
                "Gold Saucer",
                "Ultimate Raids"
            };

            var duties = Svc.Data.GetExcelSheet<ContentFinderCondition>()
                .Where(c => allowedContentTypes.Contains(c.ContentType.ToString()))
                .Select(c => c.Name)
                .ToList();

            List<string> fuzzyMatches = new List<string>();

            foreach (string duty in duties)
            {
                int matchScore = CalculateFuzzyMatchScore(duty, arg);
                if (matchScore >= Config.fuzzyMatchingThreshold)
                {
                    fuzzyMatches.Add(duty);
                }
            }

            if (fuzzyMatches.Count == 0)
            {
                Svc.Chat.Print($"Unable to match {arg} to a valid duty.");
            }

            var matchedDuty = fuzzyMatches.FirstOrDefault();
            var cfc = Svc.Data.GetExcelSheet<ContentFinderCondition>()!
                .FirstOrDefault(cfc => cfc.Name == matchedDuty);

            if (cfc == null) return;

            OpenRegularDuty(cfc.RowId); // this opens df to the selected duty, it still needs to be checked
            SelectJoin();
        }

        [Signature("48 89 6C 24 ?? 48 89 74 24 ?? 57 48 81 EC ?? ?? ?? ?? 48 8B F9 41 0F B6 E8")]
        private readonly OpenDutyDelegate openDuty = null!;

        private delegate IntPtr OpenDutyDelegate(IntPtr agent, uint contentFinderCondition, byte a3);

        // public static List<uint> GetInstanceListFromDutyName(string duty)
        // {
        //     return Svc.Data.GetExcelSheet<ContentFinderCondition>()!
        //             .Where(c => c.Name == duty)
        //             .OrderBy(c => c.SortKey)
        //             .Select(c => c.TerritoryType.Row)
        //             .ToList();
        // }

        private unsafe bool ShowDutyFinder(uint cfcID)
        {
            if (cfcID == 0)
                return false;

            var framework = Framework.Instance();
            var uiModule = framework->GetUiModule();
            var agentModule = uiModule->GetAgentModule();
            var cfAgent = (IntPtr)agentModule->GetAgentByInternalId(AgentId.ContentsFinder);

            this.openDuty(cfAgent, cfcID, 0);
            return true;
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

        private static int CalculateFuzzyMatchScore(string source, string target)
        {
            int sourceLength = source.Length;
            int targetLength = target.Length;

            if (sourceLength == 0)
                return targetLength;

            if (targetLength == 0)
                return sourceLength;

            int[,] matrix = new int[sourceLength + 1, targetLength + 1];

            for (int i = 0; i <= sourceLength; i++)
            {
                matrix[i, 0] = i;
            }

            for (int j = 0; j <= targetLength; j++)
            {
                matrix[0, j] = j;
            }

            for (int i = 1; i <= sourceLength; i++)
            {
                for (int j = 1; j <= targetLength; j++)
                {
                    int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;

                    int deletion = matrix[i - 1, j] + 1;
                    int insertion = matrix[i, j - 1] + 1;
                    int substitution = matrix[i - 1, j - 1] + cost;

                    matrix[i, j] = Math.Min(Math.Min(deletion, insertion), substitution);
                }
            }

            return matrix[sourceLength, targetLength];
        }

        private void OpenRegularDuty(uint contentFinderCondition)
        {
            AgentContentsFinder.Instance()->OpenRegularDuty(contentFinderCondition);
        }
    }
}
