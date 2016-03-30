using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using SpaceEngineersScripts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinerScript
{
    class TimedWelder : ScriptBase
    {
        public override void Main(string eventName)
        {
            const string TO_WELD_ARGUMENT = "to welding";
            const string TO_GRIND_ARGUMENT = "to grinding";
            const string TO_HALT_ARGUMENT = "to halt";

            const string WELDERS = "smallBomb welders";
            const string GRINDERS = "smallBomb grinders";

            const string WELDER_TIMER_NAME = "smallBomb welder timer";            
            const string GRINDER_TIMER_NAME = "smallBomb grinder timer";
            
            const string PROCEDURE_NAME = "small bomb plant";
            

            Action<IEnumerable<IMyBlockGroup>, Func<IMyBlockGroup, bool>, Action<IMyBlockGroup>>
                ForAllIMyBlockGroups = (s, c, a) => { foreach (var x in s) { if (c(x)) a(x); } };

            Action<IEnumerable<IMyTerminalBlock>, Func<IMyTerminalBlock, bool>, Action<IMyTerminalBlock>>
                ForAllIMyTerminalBlock = (s, c, a) => { foreach (var x in s) { if (c(x)) a(x); } };

            Func<IMyTerminalBlock, bool> IsWelder = x => x as IMyShipWelder != null;
            Func<IMyTerminalBlock, bool> IsGrinder = x => x as IMyShipGrinder != null;


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

            Action<string, Func<IMyTerminalBlock, bool>, string> ApplyCommand = (group, check, command) =>
            {
                ForAllIMyTerminalBlock(
                    GetBlocks(group),
                    check,
                    block => block.GetActionWithName(command).Apply(block)
                    );
            };

            Action<string, string> AplyForTimer = (timerName, actionName) =>
            {
                var timer = GridTerminalSystem.GetBlockWithName(timerName) as IMyTimerBlock;
                if (timer == null) return;
                timer.GetActionWithName(actionName).Apply(timer);
            };

            Action Weld = () =>
            {
                ApplyCommand(WELDERS, IsWelder, "OnOff_On");
                ApplyCommand(GRINDERS, IsGrinder, "OnOff_Off");
            };

            Action Grind = () =>
            {
                ApplyCommand(WELDERS, IsWelder, "OnOff_Off");
                ApplyCommand(GRINDERS, IsGrinder, "OnOff_On");
            };

            Action Stop = () =>
            {
                ApplyCommand(WELDERS, IsWelder, "OnOff_Off");
                ApplyCommand(GRINDERS, IsGrinder, "OnOff_Off");
            };

            if (string.IsNullOrEmpty(eventName))
                eventName = Storage;
            if (string.IsNullOrEmpty(eventName))
                eventName = TO_WELD_ARGUMENT;

            Echo(PROCEDURE_NAME + " " + eventName);

            switch (eventName)
            {
                case TO_WELD_ARGUMENT:
                    AplyForTimer(GRINDER_TIMER_NAME, "Stop");
                    Weld();
                    Storage = TO_GRIND_ARGUMENT;
                    AplyForTimer(WELDER_TIMER_NAME, "Start");
                    break;
                case TO_GRIND_ARGUMENT:
                    AplyForTimer(WELDER_TIMER_NAME, "Stop");
                    Grind();
                    Storage = TO_WELD_ARGUMENT;
                    AplyForTimer(GRINDER_TIMER_NAME, "Start");
                    break;
                case TO_HALT_ARGUMENT:
                    AplyForTimer(WELDER_TIMER_NAME, "Stop");
                    AplyForTimer(GRINDER_TIMER_NAME, "Stop");
                    Stop();
                    break;
            }
        }
    }
}
