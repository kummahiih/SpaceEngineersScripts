﻿
using Sandbox.ModAPI;
using SpaceEngineersScripts;
using System;
using System.Collections.Generic;


namespace MinerScript
{
    namespace ActionSystem
    {
        public class Program : ScriptBase
        {
            public static void ForEach(IEnumerable<ScriptAction> source, Action<ScriptAction> action) {
                foreach (var x in source) { if (action != null) action(x); }
            }

            //linq substitutes
            //public static void ForEach<T>(IEnumerable<T> source, Action<T> action)
            //{
            //    foreach (var x in source) { if(action != null)action(x); }
            //}

            //public static IEnumerable<V> Select<T, V>(IEnumerable<T> source, Func<T, V> select)
            //{
            //    var ret = new List<V>();
            //    ForEach(source, x => ret.Add(select(x)));
            //    return ret;
            //}

            public static IEnumerable<ScriptAction> Where(IEnumerable<ScriptAction> source, Func<ScriptAction, bool> condition)
            {
                var ret = new List<ScriptAction>();
                ForEach(source, x => { if (condition == null || condition(x)) ret.Add(x); });
                return ret;
            }

            //public static IEnumerable<T> Where<T>(IEnumerable<T> source, Func<T, bool> condition)
            //{
            //    var ret = new List<T>();
            //    ForEach(source, x => { if (condition == null || condition(x)) ret.Add(x); });
            //    return ret;
            //}

            //public static void ApplyCommand(Sandbox.ModAPI.Ingame.IMyTerminalBlock block, string command)
            //{
            //    block.GetActionWithName(command).Apply(block);
            //}

            //public List<Sandbox.ModAPI.Ingame.IMyTerminalBlock> GetBlocks(string group)
            //{
            //    var blocks = new List<Sandbox.ModAPI.Ingame.IMyTerminalBlock>();
            //    var groups = new List<Sandbox.ModAPI.Ingame.IMyBlockGroup>();
            //    GridTerminalSystem.GetBlockGroups(groups);
            //    ForEach(Where(groups, x => x.Name == group), x => blocks.AddRange(x.Blocks));
            //    return blocks;
            //}

            //public void ForBlocksInGoupWhereApply(
            //    string group, Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool> condition, string command)
            //{
            //    ForEach(Where(GetBlocks(group: group), x => x is Sandbox.ModAPI.Ingame.IMyConveyorSorter), x => ApplyCommand(x, command: command));
            //}


            //public void ForBlockWhereApply(string name, Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool> condition, Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock> command)
            //{
            //    var block = GridTerminalSystem.GetBlockWithName(name);
            //    if (block != null && (condition == null || condition(block)) && command != null)
            //        command(block);
            //}

            //public void ForBlockWhereApply(string name, Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool> condition, string command)
            //{
            //    ForBlockWhereApply(name, condition, block => ApplyCommand(block, command));
            //}

            //public bool IsSorter(IMyTerminalBlock x)
            //{
            //    return x is IMyConveyorSorter;
            //}
            //public bool IsTimer(IMyTerminalBlock x)
            //{
            //    return x is IMyTimerBlock;
            //}

            //public bool IsIMyTextPanel(IMyTerminalBlock x)
            //{
            //    return x is IMyTextPanel;
            //}

            //public bool NoCondition(IMyTerminalBlock x)
            //{
            //    return true;
            //}


            //action naming, the tree structure of actions and action execution 
            public class ScriptAction
            {
                public String Name { get; private set; }
                protected Action<string> ExecuteAction { get; private set; }
                public List<ScriptAction> EntryActions { get; private set; }

                public ScriptAction(Action<string> action = null, string name = null)
                {
                    ExecuteAction = action; Name = name;
                    EntryActions = new List<ScriptAction>();
                }
                public void Add(ScriptAction action) { EntryActions.Add(action); }

                public virtual void Execute(string param = "")
                {
                    if (ExecuteAction != null)
                        ExecuteAction.Invoke(param);
                }

                public override string ToString()
                {
                    return Name;
                }
            }

            public void Execute(ScriptAction scriptAction, string param = "")
            {
                Echo(" on action: " + scriptAction.ToString() + (string.IsNullOrEmpty(param) ? " with param " + param : ""));
                scriptAction.Execute(param);
                ForEach(Where(scriptAction.EntryActions, x => string.IsNullOrEmpty(param) || 
                x.Name == param), x => Execute(x));
            }

            //TODO json
            //public string GetRecursiceDescription(ScriptAction scriptAction)
            //{
            //    var ret = scriptAction.Name;
            //    ForEach(scriptAction.EntryActions, action => { ret += "\n|" + action.ToString().Replace("\n", "\n|"); });
            //    return ret;
            //}


            //public ScriptAction GetGroupCommand(string group,
            //    Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool> condition,
            //    string command)
            //{
            //    return new ScriptAction(
            //        name: "G:" + group + ", C:" + command,
            //        action: param => ForBlocksInGoupWhereApply(group: group, condition: condition, command: command));
            //}

            //public ScriptAction GetBlockCommand(string name,
            //    Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool> condition, string command)
            //{
            //    return new ScriptAction(
            //        name: "N:" + name + ", C:" + command,
            //        action: param => ForBlockWhereApply(name: name, condition: condition, command: command));
            //}

            //const string LCD_OUT_NAME = "outPanel";

            //public ScriptAction GetLcdOutAction(
            //        string name, Func<string> data)
            //{
            //    return new ScriptAction(
            //        name: "N:" + LCD_OUT_NAME + ", LcdOut:" + name,
            //        action: param =>
            //        {
            //            ForBlockWhereApply(LCD_OUT_NAME, IsIMyTextPanel,
            //                block =>
            //                {
            //                    (block as Sandbox.ModAPI.IMyTextPanel).WritePrivateText(name);
            //                    (block as Sandbox.ModAPI.IMyTextPanel).WritePrivateText(data());
            //                });
            //        });
            //}

            public ScriptAction Initialize()
            {
                var main = new ScriptAction(name: "Main");

                var helloAction = new ScriptAction(name: "helloTerminal", action: param => { Echo("Hello Terminal"); });
                main.Add(helloAction);
                //var helloLcd = GetLcdOutAction(name: "helloLcd", data: () => "Hello Lcd");
                //main.Add(helloLcd);
                //var showActions = GetLcdOutAction(name: "listActions", data: () => { return GetRecursiceDescription(main); });
                //main.Add(showActions);

                return main;
            }




            public void Main(string eventName)
            {
                var main = Initialize();
                Echo("Foo2");
                //Execute(main, eventName);
            }


        }
    }
}