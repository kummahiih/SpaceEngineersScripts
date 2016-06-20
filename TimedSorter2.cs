
using Sandbox.ModAPI;
using SpaceEngineersScripts;
using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;

namespace MinerScript
{
    public class TimedSorter2 : ScriptBase
    {
        //global functions and such
        public class ScriptEnv {
            Sandbox.ModAPI.IMyGridProgram Env { get; }
            public ScriptEnv( Sandbox.ModAPI.IMyGridProgram env) { Env = env; }
            public void Echo(string msg) { Env.Echo(msg); }

            //linq substitutes
            public void ForEach<T>(IEnumerable<T> source, Action<T> action) {
                foreach (var x in source) { action(x); }
            }

            public IEnumerable<V> Select<T, V>(IEnumerable<T> source, Func<T, V> select) {
                var ret = new List<V>();
                ForEach(source, x => ret.Add(select(x)));
                return ret;
            }

            public IEnumerable<T> Where<T>(IEnumerable<T> source, Func<T, bool> condition) {
                var ret = new List<T>();
                ForEach(source, x => { if (condition == null || condition(x)) ret.Add(x); });
                return ret;
            }

            public List<Sandbox.ModAPI.Ingame.IMyTerminalBlock> GetBlocks(string group) {
                var blocks = new List<Sandbox.ModAPI.Ingame.IMyTerminalBlock>();
                var groups = new List<Sandbox.ModAPI.Ingame.IMyBlockGroup>();
                Env.GridTerminalSystem.GetBlockGroups(groups);
                ForEach( Where(groups, x => x.Name == group), x => blocks.AddRange(x.Blocks) );
                return blocks;
            }

            public void ApplyCommand(Sandbox.ModAPI.Ingame.IMyTerminalBlock block, string command){
                block.GetActionWithName(command).Apply(block);
            }

            public void ForBlocksInGoupWhereApply(string group, Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool> condition, string command) {
                ForEach(Where(GetBlocks(group: group),x => x is Sandbox.ModAPI.Ingame.IMyConveyorSorter), x => ApplyCommand(x, command: command));
            }

            public void ForBlockWhereApply(string name, Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool> condition, Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock> command)
            {
                var block = Env.GridTerminalSystem.GetBlockWithName(name);
                if (block != null && (condition == null || condition(block)) && command != null)
                    command(block);
            }

            public void ForBlockWhereApply(string name, Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool> condition, string command)
            {
                ForBlockWhereApply(name, condition, block => ApplyCommand(block, command));
            }

            public bool IsSorter(Sandbox.ModAPI.Ingame.IMyTerminalBlock x) { 
                return x is Sandbox.ModAPI.Ingame.IMyConveyorSorter;
            }
            public bool IsTimer(Sandbox.ModAPI.Ingame.IMyTerminalBlock x)  {
                return x is Sandbox.ModAPI.Ingame.IMyConveyorSorter;
            }

            public bool IsIMyTextPanel(Sandbox.ModAPI.Ingame.IMyTerminalBlock x)
            {
                return x is Sandbox.ModAPI.IMyTextPanel;
            }

            public bool NoCondition(Sandbox.ModAPI.Ingame.IMyTerminalBlock x)
            {
                return true;
            }
        }
        
        //action naming, the tree structure of actions and action execution 
        public class ScriptAction: ScriptEnv {
            Sandbox.ModAPI.IMyGridProgram Env { get; }
            public String Name { get; }
            protected Action<ScriptAction> ExecuteAction { get; }
            protected List<ScriptAction> EntryActions { get; }

            public ScriptAction( Sandbox.ModAPI.IMyGridProgram env, Action<ScriptAction> action = null, string name = null)
                : base(env){
                 ExecuteAction = action;  Name = name;                
                EntryActions = new List<ScriptAction>();
            }
            public void Add(ScriptAction action) { EntryActions.Add(action); }

            public string GetRecursiceDescription()
            {
                var ret = Name;
                ForEach(EntryActions, action => { ret += "\n|" + action.ToString().Replace("\n", "\n|"); });
                return ret;
            }

            public void Execute(string param ="") {
                Echo( " on state: " + Name + (string.IsNullOrEmpty(param) ? " with param " + param : "") );
                ExecuteAction?.Invoke(this);
                ForEach(Where(EntryActions, x => string.IsNullOrEmpty(param)  || x.Name == param) , x => x.Execute());
            }
        }

        public class GroupCommand : ScriptAction
        {
            public GroupCommand(Sandbox.ModAPI.IMyGridProgram baseEnv, string group,                 
                Func<ScriptEnv, Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool>> condition, string command) :
                base(baseEnv, name: "(G "+group+":"+command+")",
                    action: env => env.ForBlocksInGoupWhereApply( group: group, condition: condition(env), command: command)                    
                ){}
        }

        public class BlockCommand : ScriptAction
        {
            public BlockCommand(Sandbox.ModAPI.IMyGridProgram baseEnv, string name,
                Func<ScriptEnv, Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool>> condition , string command) :
                base(baseEnv, name: "(N " + name + ":" + command+ ")",
                    action: env => env.ForBlockWhereApply( name:name, condition: condition(env), command: command)                    
                ){}
        }

        



        ScriptAction MainAction;

        const string SORTERS_TO_CONT = "sorters to cont";
        const string SORTERS_FROM_CONT = "sorters from cont";
        const string DRAINING_TIMER_NAME = "draining timer";
        const string DRAINING_TIMER_ARGUMENT = "to drain all";
        const string NORMAL_DRAIN_TIMER_NAME = "normal drain timer";
        const string NORMAL_DRAIN_TIMER_ARGUMENT = "to normal drain";
        const string PROCEDURE_NAME = "resource sorting";

        const string LCD_NAME = "debug panel";



        public void Program()
        {
            MainAction = new ScriptAction(this, name: "Main");

            var toContDrainAll = 
                new GroupCommand(this, SORTERS_TO_CONT, env => env.IsSorter,"DrainAll");

            var fromContOff =
                new GroupCommand(this, SORTERS_FROM_CONT, env => env.IsSorter, "OnOff_Off");

            var fromContOn =
                new GroupCommand(this, SORTERS_FROM_CONT, env => env.IsSorter, "OnOff_On");

            var normaDrainTimerOn =
                new BlockCommand(this, NORMAL_DRAIN_TIMER_NAME, env => env.NoCondition, "OnOff_On");

            var normaDrainTimerOff =
                new BlockCommand(this, NORMAL_DRAIN_TIMER_NAME, env => env.NoCondition, "OnOff_Off");

            var DrainingTimerOn =
                new BlockCommand(this, DRAINING_TIMER_NAME, env => env.NoCondition, "OnOff_On");

            var DrainingTimerOff =
                new BlockCommand(this, DRAINING_TIMER_NAME, env => env.NoCondition, "OnOff_Off");


            var debugPanelShowActions =
                new ScriptAction(this, name: LCD_NAME + " show actions", action: env => 
                {
                    env.ForBlockWhereApply(LCD_NAME, env.IsIMyTextPanel, 
                        block  =>
                        {
                            (block as Sandbox.ModAPI.IMyTextPanel).WritePrivateText(MainAction.GetRecursiceDescription());
                        }
                );

        }




        public void Main(string eventName)
        {
        }


    }
}