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
            const string DRAINING_TIMER_ARGUMENT = "to draining";
            const string NORMAL_DRAIN_TIMER_NAME = "normal drain timer";            
            const string NORMAL_DRAIN_TIMER_ARGUMENT = "to normal drain";

            Func<string, List<IMyTerminalBlock>> GetBlocks = (group) =>
            {
                var blocks = new List<IMyTerminalBlock>();
                var groups = new List<IMyBlockGroup>();

                GridTerminalSystem.GetBlockGroups(groups);

                groups
                    .Where(x => x.Name == group)
                    .Select(x => x.Blocks)
                    .ForEach(x => blocks.AddRange(x));
                    return blocks;
            };
            Action<string,Type, string> ApplyCommand = (group, type, command) =>
            {
                GetBlocks(group)
                    .Where(x => type.IsInstanceOfType(x) )
                    .ForEach(block => block.GetActionWithName(command).Apply(block));
            };

            Action<string> StartTimer = timerName =>
            {
                var timer = GridTerminalSystem.GetBlockWithName(timerName) as IMyTimerBlock;
                if (timer == null) return;
                timer.GetActionWithName("Start").Apply(timer);
            };

            Action DrainToContainers = () =>
            {
                ApplyCommand(SORTERS_TO_CONT,typeof(IMyConveyorSorter), "DrainAll" );
                ApplyCommand(SORTERS_FROM_CONT, typeof(IMyConveyorSorter), "OnOff_Off");               
            };

            Action NormalizeDrain = () =>
            {
                ApplyCommand(SORTERS_TO_CONT, typeof(IMyConveyorSorter), "DrainAll");
                ApplyCommand(SORTERS_FROM_CONT, typeof(IMyConveyorSorter), "OnOff_On");
            };

            switch(argument)
            {
                case DRAINING_TIMER_ARGUMENT:
                    DrainToContainers();
                    StartTimer(DRAINING_TIMER_NAME);
                    break;
                case NORMAL_DRAIN_TIMER_ARGUMENT:
                    NormalizeDrain();
                    StartTimer(NORMAL_DRAIN_TIMER_NAME);
                    break;
            }

        }
    }
}
