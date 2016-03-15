using Sandbox.ModAPI.Ingame;
using SpaceEngineersScripts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinerScript
{
    class TimedSorter : ScriptBase
    {
        public override void Main(string argument)
        {
            const string SORTERS_TO_CONT = "sorters to cont";
            const string SORTERS_FROM_CONT = "sorters from cont";
            const string DRAINING_TIMER_NAME = "draining timer";
            const string DRAINING_TIMER_ARGUMENT = "to drain all";
            const string NORMAL_DRAIN_TIMER_NAME = "normal drain timer";            
            const string NORMAL_DRAIN_TIMER_ARGUMENT = "to normal drain";
            const string PROCEDURE_NAME = "resource sorting";

            Action <IEnumerable<IMyBlockGroup>, Func<IMyBlockGroup, bool>, Action<IMyBlockGroup>>
                ForAllIMyBlockGroups = (s, c, a) => { foreach (var x in s) { if (c(x)) a(x); }};

            Action<IEnumerable<IMyTerminalBlock>, Func<IMyTerminalBlock, bool>, Action<IMyTerminalBlock>>
                ForAllIMyTerminalBlock = (s, c, a) => {foreach (var x in s) { if (c(x)) a(x); }};

            Func<IMyTerminalBlock, bool> IsSorter = x => x as IMyConveyorSorter != null;

            Func<string, List<IMyTerminalBlock>> GetBlocks = (group) =>
            {
                var blocks = new List<IMyTerminalBlock>();
                var groups = new List<IMyBlockGroup>();

                GridTerminalSystem.GetBlockGroups(groups);

                ForAllIMyBlockGroups(
                    groups,
                    x => x.Name == group,
                    x => blocks.AddRange(x.Blocks)
                    );
                    return blocks;
            };

            Action<string,Func<IMyTerminalBlock, bool>, string> ApplyCommand = (group, check, command) =>
            {
                ForAllIMyTerminalBlock(
                    GetBlocks(group),
                    check,
                    block => block.GetActionWithName(command).Apply(block)
                    );
            };

            Action<string,string> AplyForTimer = (timerName, actionName) =>
            {
                var timer = GridTerminalSystem.GetBlockWithName(timerName) as IMyTimerBlock;
                if (timer == null) return;
                timer.GetActionWithName(actionName).Apply(timer);
            };

            Action DrainToContainers = () =>
            {
                ApplyCommand(SORTERS_TO_CONT, IsSorter, "DrainAll" );
                ApplyCommand(SORTERS_FROM_CONT, IsSorter, "OnOff_Off");               
            };

            Action NormalizeDrain = () =>
            {
                ApplyCommand(SORTERS_TO_CONT, IsSorter, "DrainAll");
                ApplyCommand(SORTERS_FROM_CONT, IsSorter, "OnOff_On");
            };

            if (string.IsNullOrEmpty(argument))
                argument = Storage;

            Echo(PROCEDURE_NAME + " " + argument);

            switch (argument)
            {
                case DRAINING_TIMER_ARGUMENT:
                    AplyForTimer(NORMAL_DRAIN_TIMER_NAME, "Stop");
                    DrainToContainers();
                    Storage = NORMAL_DRAIN_TIMER_ARGUMENT;                    
                    AplyForTimer(DRAINING_TIMER_NAME, "Start");
                    break;
                case NORMAL_DRAIN_TIMER_ARGUMENT:
                    AplyForTimer(DRAINING_TIMER_NAME, "Stop");
                    NormalizeDrain();
                    Storage = DRAINING_TIMER_ARGUMENT;                    
                    AplyForTimer(NORMAL_DRAIN_TIMER_NAME, "Start");
                    break;
            }

        }
    }
}
