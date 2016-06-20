using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceEngineersScripts
{
    public abstract class ScriptBase: IMyGridProgram
    {
        protected IMyGridTerminalSystem GridTerminalSystem;

        public Action<string> Echo
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public TimeSpan ElapsedTime
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public bool HasMainMethod
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool HasSaveMethod
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IMyProgrammableBlock Me
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public IMyGridProgramRuntimeInfo Runtime
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public string Storage
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        IMyGridTerminalSystem IMyGridProgram.GridTerminalSystem
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        //public virtual void Program() { }

        //public abstract void Main(string argument);

        //public virtual void Save() {  }
    }
}
