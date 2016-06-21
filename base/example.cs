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
    class StatusReport
    {
        IMyGridTerminalSystem GridTerminalSystem;
        //http://steamcommunity.com/sharedfiles/filedetails/?id=360966557

        void Main()
        {
            var blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(blocks);
            if (blocks.Count > 0)
                blocks[0].SetCustomName("Hello, Galaxy!");
        }
    }
}