using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using System.Linq;

namespace PandorasBox.Features.Commands
{
    public unsafe class MSQCountdown : CommandFeature
    {
        public override string Name => "MSQ Countdown";

        public override bool Disabled => true;  

        public override string Description => "Prints a message in chat with how many main scenario quests left in the current expansion you have to complete.";

        public override string Command { get; set; } = "/pmsq";

        private ExVersion CurrentExpansion;

        protected override void OnCommand(List<string> args)
        {
            string debug = "";
            if (args.Count > 0)
            {
                debug = args[0];
            }

            var questsheet = Svc.Data.GetExcelSheet<Quest>();
            var uim = UIState.Instance();

            var filteredList = questsheet.Where(x => x.JournalGenre.Value.Icon == 61412 && !string.IsNullOrEmpty(x.Name.ToString()));
            CurrentExpansion = Svc.Data.GetExcelSheet<ExVersion>().GetRow(0);

            if (debug == "")
            {
                foreach (var quest in filteredList)
                {
                    if (uim->IsUnlockLinkUnlockedOrQuestCompleted(quest.RowId, quest.TodoParams.Max(x => x.ToDoCompleteSeq)))
                    {
                        if (quest.Expansion.Value.RowId > CurrentExpansion.RowId)
                            CurrentExpansion = quest.Expansion.Value;
                    }
                }
            }
            else
            {
                CurrentExpansion = debug.ToLower() switch
                {
                    "arr" => Svc.Data.GetExcelSheet<ExVersion>().GetRow(0),
                    "hw" => Svc.Data.GetExcelSheet<ExVersion>().GetRow(1),
                    "stb" => Svc.Data.GetExcelSheet<ExVersion>().GetRow(2),
                    "shb" => Svc.Data.GetExcelSheet<ExVersion>().GetRow(3),
                    "ew" => Svc.Data.GetExcelSheet<ExVersion>().GetRow(4)
                };
            }

            int completed = 0;
            var totalMSQ = filteredList.Where(x => x.Expansion.RowId == CurrentExpansion.RowId).Count();

            int uncompleted = 0;
            foreach (var quest in filteredList.Where(x => x.Expansion.RowId == CurrentExpansion.RowId).OrderBy(x => x.Name.ToString()))
            {
                if (uim->IsUnlockLinkUnlockedOrQuestCompleted(quest.RowId, quest.TodoParams.Max(x => x.ToDoCompleteSeq)))
                {
                    Svc.Log.Debug($"{quest.Name} - Completed!");
                    completed++;
                }
                else
                {
                    Svc.Log.Error($"{quest.Name} - Not Completed!");
                    uncompleted++;
                }

            }

            Svc.Log.Error($"{uncompleted} quests not done, total MSQ is {totalMSQ}.");
            if (CurrentExpansion.RowId == 0)
            {
                if (PlayerState.Instance()->StartTown != 1)
                    totalMSQ -= 23;

                if (PlayerState.Instance()->StartTown != 2)
                    totalMSQ -= 23;

                if (PlayerState.Instance()->StartTown != 3)
                    totalMSQ -= 24;

                totalMSQ -= 8;
            }

            var diff = totalMSQ - completed;

            Svc.Log.Debug($"{diff} - {totalMSQ} {completed}");
            if (diff > 0)
            {
                if (Svc.Data.GetExcelSheet<ExVersion>().Max(x => x.RowId) == CurrentExpansion.RowId)
                {
                    Svc.Chat.Print($"You are currently in {CurrentExpansion.Name} with {diff} quests until completion.");
                }
                else
                {
                    Svc.Chat.Print($"You are currently in {CurrentExpansion.Name} with {diff} quests until {Svc.Data.GetExcelSheet<ExVersion>().GetRow(CurrentExpansion.RowId + 1).Name}");
                }
            }

            if (diff == 0)
            {
                if (Svc.Data.GetExcelSheet<ExVersion>().Max(x => x.RowId) == CurrentExpansion.RowId)
                {
                    Svc.Chat.Print($"Congratulations! You have no more MSQ quests to complete.... for now!");
                }
                else
                {
                    Svc.Chat.Print($"Congratulations on beating {CurrentExpansion.Name}! Onwards to {Svc.Data.GetExcelSheet<ExVersion>().GetRow(CurrentExpansion.RowId + 1).Name}!!!");
                }
            }

            if (diff < 0)
            {
                Svc.Chat.PrintError($"Something is wrong, you apparently have {diff} quests left? Surely not. Please contact the developer.");
            }
        }
    }
}
