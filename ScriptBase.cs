using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceEngineersScripts
{
    public abstract class ScriptBase
    {
        protected IMyGridTerminalSystem GridTerminalSystem;

        public abstract void Main(string argument);
    }
}
