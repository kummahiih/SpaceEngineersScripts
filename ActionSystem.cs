
using Sandbox.ModAPI;
using SpaceEngineersScripts;
using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;

namespace MinerScript
{
    public class ActionSystem : ScriptBase
    {
        //global functions and such
        public class ScriptEnv
        {
            Sandbox.ModAPI.IMyGridProgram Env { get; }
            public ScriptEnv(Sandbox.ModAPI.IMyGridProgram env) { Env = env; }
            public void Echo(string msg) { Env.Echo(msg); }

            //linq substitutes
            public void ForEach<T>(IEnumerable<T> source, Action<T> action)
            {
                foreach (var x in source) { action(x); }
            }

            public IEnumerable<V> Select<T, V>(IEnumerable<T> source, Func<T, V> select)
            {
                var ret = new List<V>();
                ForEach(source, x => ret.Add(select(x)));
                return ret;
            }

            public IEnumerable<T> Where<T>(IEnumerable<T> source, Func<T, bool> condition)
            {
                var ret = new List<T>();
                ForEach(source, x => { if (condition == null || condition(x)) ret.Add(x); });
                return ret;
            }

            public List<Sandbox.ModAPI.Ingame.IMyTerminalBlock> GetBlocks(string group)
            {
                var blocks = new List<Sandbox.ModAPI.Ingame.IMyTerminalBlock>();
                var groups = new List<Sandbox.ModAPI.Ingame.IMyBlockGroup>();
                Env.GridTerminalSystem.GetBlockGroups(groups);
                ForEach(Where(groups, x => x.Name == group), x => blocks.AddRange(x.Blocks));
                return blocks;
            }

            public void ApplyCommand(Sandbox.ModAPI.Ingame.IMyTerminalBlock block, string command)
            {
                block.GetActionWithName(command).Apply(block);
            }

            public void ForBlocksInGoupWhereApply(string group, Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool> condition, string command)
            {
                ForEach(Where(GetBlocks(group: group), x => x is Sandbox.ModAPI.Ingame.IMyConveyorSorter), x => ApplyCommand(x, command: command));
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

            public bool IsSorter(Sandbox.ModAPI.Ingame.IMyTerminalBlock x)
            {
                return x is Sandbox.ModAPI.Ingame.IMyConveyorSorter;
            }
            public bool IsTimer(Sandbox.ModAPI.Ingame.IMyTerminalBlock x)
            {
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
        public class ScriptAction : ScriptEnv
        {
            Sandbox.ModAPI.IMyGridProgram Env { get; }
            public String Name { get; }
            protected Action<ScriptAction> ExecuteAction { get; }
            protected List<ScriptAction> EntryActions { get; }

            public ScriptAction(Sandbox.ModAPI.IMyGridProgram env, Action<ScriptAction> action = null, string name = null)
                : base(env)
            {
                ExecuteAction = action; Name = name;
                EntryActions = new List<ScriptAction>();
            }
            public void Add(ScriptAction action) { EntryActions.Add(action); }

            public string GetRecursiceDescription()
            {
                var ret = Name;
                ForEach(EntryActions, action => { ret += "\n|" + action.ToString().Replace("\n", "\n|"); });
                return ret;
            }

            public void Execute(string param = ""){

                Echo(" on state: " + Name + (string.IsNullOrEmpty(param) ? " with param " + param : ""));
                ExecuteAction?.Invoke(this);
                ForEach(Where(EntryActions, x => string.IsNullOrEmpty(param) || x.Name == param), x => x.Execute());
            }
        }

        public class GroupCommand : ScriptAction{
            public GroupCommand(Sandbox.ModAPI.IMyGridProgram baseEnv, string group,
                Func<ScriptEnv, Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool>> condition, string command) :
                base(baseEnv, name: "(G " + group + "." + command + ")",
                    action: env => env.ForBlocksInGoupWhereApply(group: group, condition: condition(env), command: command)
                )
            { }
        }

        public class BlockCommand : ScriptAction {
            public BlockCommand(Sandbox.ModAPI.IMyGridProgram baseEnv, string name,
                Func<ScriptEnv, Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool>> condition, string command) :
                base(baseEnv, name: "(N " + name + "." + command + ")",
                    action: env => env.ForBlockWhereApply(name: name, condition: condition(env), command: command)
                )
            { }
        }

        const string LCD_OUT_NAME = "outPanel";

        public class LcdOutAction : ScriptAction {
            public LcdOutAction(Sandbox.ModAPI.IMyGridProgram baseEnv,
                string name, Func<string> data) : base(baseEnv, name: LCD_OUT_NAME + "." + name,
                    action: env =>
                    {
                        env.ForBlockWhereApply(LCD_OUT_NAME, env.IsIMyTextPanel,
                            block =>
                            {
                                (block as Sandbox.ModAPI.IMyTextPanel).WritePrivateText(name);
                                (block as Sandbox.ModAPI.IMyTextPanel).WritePrivateText(data());
                            });
                    })
            {}
        }



        ScriptAction MainAction;





        public void Program()
        {
            var main = new ScriptAction(this, name: "Main");

            var debugPanelShowActions = new LcdOutAction(this, name: "listActions",  data:() => {return main.GetRecursiceDescription(); } );
            MainAction = main;
        }




        public void Main(string eventName)
        {
        }


    }
}