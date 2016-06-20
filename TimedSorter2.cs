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
    class TimedSorter : ScriptBase {           
        class ScriptState {
            public String Code {get; private set;}
            public String Description {get; private set;}
            protected List<ScriptAction> EntryActions {get;  private set;}

            public void ExecuteEntryActions() {
                var msg = Description + EntryActions.Any() ? 
                     " :" + string.Join(", ", EntryActions.Select( x => x.Description) :
                    "";
                Echo(msg);
                EntryActions.ForEach( x => x.Execute();
            }             
        }

        abstract class ScriptAction {
            public String Description {get; private set;}
            abstract void Execute();
        }


        public override void Main(string eventName)
        {
        }
    }
}