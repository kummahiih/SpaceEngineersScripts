using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common;
using Sandbox.Common.Components;
using Sandbox.Definitions;
using Sandbox.Engine;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game;


namespace Scripts
{
    class Autominer
    {
        IMyGridTerminalSystem GridTerminalSystem;

        void Main(string argument)
        {
            var drills = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyShipDrill>(drills);
            var groups = new List<IMyBlockGroup>();
            GridTerminalSystem.GetBlockGroups(groups);


            if (argument == "start")
            {

                drills.ForEach(
                    drill => drill.GetActionWithName("OnOff_On").Apply(drill)
                    );

                groups
                    .ForEach(group =>
                    {
                        if (group.Name == "forward")
                        {
                            group.Blocks.ForEach(
                               block =>
                               {
                                   var thruster = block as IMyThrust;
                                   thruster.GetActionWithName("OnOff_On").Apply(thruster);
                                   for (int j = 0; j < 10; j++)
                                       thruster.GetActionWithName("IncreaseOverride").Apply(thruster);
                               });
                        }
                    });

                var timer = GridTerminalSystem.GetBlockWithName("timer") as IMyTimerBlock;
                if (timer == null)
                    return;

                timer.GetActionWithName("Start").Apply(timer);
            }

            if (argument == "stop")
            {

                drills.ForEach(
                    drill => drill.GetActionWithName("OnOff_Off").Apply(drill)
                    );

                groups
                    .ForEach(group =>
                    {
                        if (group.Name == "forward")
                        {
                            group.Blocks.ForEach(block =>
                            {
                                var thruster = block as IMyThrust;
                                for (int j = 0; j < 10; j++)
                                    thruster.GetActionWithName("DecreaseOverride").Apply(thruster);
                            });
                        }
                    });                
            }


        }
    }
}