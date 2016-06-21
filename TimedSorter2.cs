
//using Sandbox.ModAPI;
//using SpaceEngineersScripts;
//using System;
//using System.Collections.Generic;
//using Sandbox.ModAPI.Ingame;

//namespace MinerScript
//{
/

//        const string SORTERS_TO_CONT = "sorters to cont";
//        const string SORTERS_FROM_CONT = "sorters from cont";
//        const string DRAINING_TIMER_NAME = "draining timer";
//        const string DRAINING_TIMER_ARGUMENT = "to drain all";
//        const string NORMAL_DRAIN_TIMER_NAME = "normal drain timer";
//        const string NORMAL_DRAIN_TIMER_ARGUMENT = "to normal drain";
//        const string PROCEDURE_NAME = "resource sorting";


//            MainAction = new ScriptAction(this, name: "Main");

//            var toContDrainAll = 
//                new GroupCommand(this, SORTERS_TO_CONT, env => env.IsSorter,"DrainAll");

//            var fromContOff =
//                new GroupCommand(this, SORTERS_FROM_CONT, env => env.IsSorter, "OnOff_Off");

//            var fromContOn =
//                new GroupCommand(this, SORTERS_FROM_CONT, env => env.IsSorter, "OnOff_On");

//            var normaDrainTimerOn =
//                new BlockCommand(this, NORMAL_DRAIN_TIMER_NAME, env => env.NoCondition, "OnOff_On");

//            var normaDrainTimerOff =
//                new BlockCommand(this, NORMAL_DRAIN_TIMER_NAME, env => env.NoCondition, "OnOff_Off");

//            var DrainingTimerOn =
//                new BlockCommand(this, DRAINING_TIMER_NAME, env => env.NoCondition, "OnOff_On");

//            var DrainingTimerOff =
//                new BlockCommand(this, DRAINING_TIMER_NAME, env => env.NoCondition, "OnOff_Off");


//            var debugPanelShowActions =
//                new ScriptAction(this, name: LCD_NAME + " show actions", action: env => 
//                {
//                    env.ForBlockWhereApply(LCD_NAME, env.IsIMyTextPanel, 
//                        block  =>
//                        {
//                            (block as Sandbox.ModAPI.IMyTextPanel).WritePrivateText(MainAction.GetRecursiceDescription());
//                        }
//                );

//        }




//        public void Main(string eventName)
//        {
//        }


//    }
//}