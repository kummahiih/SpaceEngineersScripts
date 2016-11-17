using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;

namespace SpaceEngineersScripts
{
    class Navigator : ScriptBase
    {
        public override void Main(string argument)
        {
            var navigatorname = "escapeOrbitControl";
            var minimumMass = 2;

            var navigators = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(navigators);

            if (navigators.Any( i => { var c = i as IMyRemoteControl; return c != null ? c.IsUnderControl : false; }))
                return;

            var escapeNavigator = navigators.Where(i => i.Name == navigatorname).FirstOrDefault() as IMyRemoteControl;
            if (escapeNavigator == null) return;

            var reactors = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyReactor>(reactors);

            float uranium = 0;

            foreach(IMyReactor reactor in reactors)
            {
                if (reactor == null) continue;

                var n = reactor.GetInventoryCount();
                for(var i = 0; i< n; i++)
                {
                    var item = reactor.GetInventory(i);
                    float itemMass = 0;
                    float.TryParse(item.CurrentMass.ToString(), out itemMass);
                    uranium += itemMass;
                }
            }
            if(uranium < minimumMass)
            {
                escapeNavigator.GetActionWithName("OnOff_On").Apply(escapeNavigator);
                escapeNavigator.SetAutoPilotEnabled(true);
            }
        }
    }
}
