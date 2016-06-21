
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
            //linq substitutes without templates nor static extensions ...
            public static void ForEachA(IEnumerable<ScriptAction> source, Action<ScriptAction> action)
            { foreach (var x in source) { if (action != null) action(x); }}
            public static void ForEachIB(IEnumerable<Sandbox.ModAPI.Ingame.IMyTerminalBlock> source, Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock> action)
            { foreach (var x in source) { if (action != null) action(x); } }            
            public static void ForEachB(IEnumerable<IMyTerminalBlock> source, Action<IMyTerminalBlock> action)
            { foreach (var x in source) { if (action != null) action(x); } }
            public static void ForEachG(IEnumerable<Sandbox.ModAPI.Ingame.IMyBlockGroup> source, Action<Sandbox.ModAPI.Ingame.IMyBlockGroup> action)
            { foreach (var x in source) { if (action != null) action(x); } }

            public static IEnumerable<ScriptAction> WhereA(IEnumerable<ScriptAction> source, Func<ScriptAction, bool> condition) {
                var ret = new List<ScriptAction>();
                ForEachA(source, x => { if (condition == null || condition(x)) ret.Add(x); });
                return ret;
            }
            public static IEnumerable<IMyTerminalBlock> WhereB(IEnumerable<IMyTerminalBlock> source, Func<IMyTerminalBlock, bool> condition) {
                var ret = new List<IMyTerminalBlock>();
                ForEachB(source, x => { if (condition == null || condition(x)) ret.Add(x); });
                return ret;
            }
            public static IEnumerable<Sandbox.ModAPI.Ingame.IMyBlockGroup> WhereG(IEnumerable<Sandbox.ModAPI.Ingame.IMyBlockGroup> source, Func<Sandbox.ModAPI.Ingame.IMyBlockGroup, bool> condition) {
                var ret = new List<Sandbox.ModAPI.Ingame.IMyBlockGroup>();
                ForEachG(source, x => { if (condition == null || condition(x)) ret.Add(x); });
                return ret;
            }

            public static void ApplyTerminalAction(Sandbox.ModAPI.Ingame.IMyTerminalBlock block, string actionName)
            {
                block.GetActionWithName(actionName).Apply(block);
            }

            public List<IMyTerminalBlock> GetBlocks(string group)
            {
                var blocksConv = new List<IMyTerminalBlock>();
                var blocks= new List<Sandbox.ModAPI.Ingame.IMyTerminalBlock>();
                var groups = new List<Sandbox.ModAPI.Ingame.IMyBlockGroup>();
                GridTerminalSystem.GetBlockGroups(groups);
                ForEachG(WhereG(groups, x => x.Name == group), x => blocks.AddRange(x.Blocks));
                ForEachIB(blocks, b => blocksConv.Add((IMyTerminalBlock)b));
                return blocksConv;
            }

            public void ForBlocksInGoupWhereApply(string group, Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool> condition, string actionName)
            {
                ForEachB(WhereB(GetBlocks(group: group), x => x is Sandbox.ModAPI.Ingame.IMyConveyorSorter), x => ApplyTerminalAction(x, actionName: actionName));
            }


            public void ForBlockWhereApplyA(string name, Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool> condition, Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock> command){
                var block = GridTerminalSystem.GetBlockWithName(name);
                if (block != null && (condition == null || condition(block)) && command != null)
                    command(block);
            }

            public void ForBlockWhereApplyS(string name, Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool> condition, string actionName)
            {
                ForBlockWhereApplyA(name, condition, b => ApplyTerminalAction(b, actionName));
            }


            public bool IsSorter(Sandbox.ModAPI.Ingame.IMyTerminalBlock x)
            {
                return x is Sandbox.ModAPI.Ingame.IMyConveyorSorter;
            }

            //public bool IsTimer(IMyTerminalBlock x)
            //{
            //    return x is Sandbox.ModAPI.Ingame.IMyTimerBlock;
            //}

            public bool IsIMyTextPanel(Sandbox.ModAPI.Ingame.IMyTerminalBlock x)
            {
                return x is IMyTextPanel;
            }

            public bool NoCondition(Sandbox.ModAPI.Ingame.IMyTerminalBlock x)
            {
                return true;
            }
            

            //action naming, the tree structure of actions and action execution 
            public class ScriptAction
            {   
                public String Name { get; set; }
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
                    return "N:" +Name;
                }
                
            }

            public void Execute(ScriptAction scriptAction, string param = "")
            {
                Echo(" on action: " + scriptAction.ToString() + (string.IsNullOrEmpty(param) ? " with param " + param : ""));
                scriptAction.Execute(param);
                ForEachA(WhereA(scriptAction.EntryActions, x => string.IsNullOrEmpty(param) || 
                x.Name == param), x => Execute(x));
            }

            //no serializers of any kind available
            public string GetRecursiceDescription(ScriptAction scriptAction)
            {
                var ret = "{"+ scriptAction.ToString();
                ret += ",\n childs:{";
                ForEachA(scriptAction.EntryActions, action => { ret += GetRecursiceDescription(action) + ","; });
                ret += "}}\n";

                return ret;
            }



            public class GroupAction : ScriptAction
            {
                public String GroupName { get; private set; }
                public String ActionName { get; private set; }
                public GroupAction(Action<string> action, string group, string actionName) : base(action, name: "GroupAction")
                { GroupName = group; ActionName = actionName; }

                public override string ToString()
                { return base.ToString() + ", G:" + GroupName + ", A:" + ActionName; }

            }

            public ScriptAction GetGroupAction(string group,
                Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool> condition,
                string actionName)
            {
                return new GroupAction(                   
                    action: param => ForBlocksInGoupWhereApply(group: group, condition: condition, actionName: actionName),
                    group:group, actionName: actionName);
            }

            public class BlockAction : ScriptAction
            {
                public String BlockName { get; private set; }
                public String ActionName { get; private set; }
                public BlockAction(Action<string> action, string blockname, string actionName) : base(action, name: "BlockAction")
                { BlockName = blockname; ActionName = actionName; }

                public override string ToString()
                { return base.ToString() + ", B:" + BlockName + ", A:" + ActionName; }
            }

            public ScriptAction GetBlockCommand(string name,
                Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool> condition, string blockName, string actionName)
            {
                return new BlockAction(                    
                    action: param => ForBlockWhereApplyS(name: name, condition: condition, actionName: actionName),
                    blockname:blockName, actionName:actionName);
            }

            const string LCD_OUT_NAME = "outPanel";

            public ScriptAction GetLcdOutAction(
                    string actionName, Func<string> data)
            {
                return new ScriptAction(
                    name: actionName,
                    action: param =>
                    {
                        ForBlockWhereApplyA(LCD_OUT_NAME, x => IsIMyTextPanel(x),
                            block =>
                            {
                                (block as Sandbox.ModAPI.IMyTextPanel).WritePublicText(data());
                                (block as Sandbox.ModAPI.IMyTextPanel).ShowPublicTextOnScreen();
                            });
                    });

            }

            public ScriptAction Initialize()
            {
                var main = new ScriptAction(name: "Main");

                var helloAction = new ScriptAction(name: "helloTerminal", action: param => { Echo("Hello Terminal"); });
                main.Add(helloAction);
                var helloLcd = GetLcdOutAction(actionName: "helloLcd", data: () => "Hello Lcd");
                main.Add(helloLcd);
                var showActions = GetLcdOutAction(actionName: "listActions", data: () => { return GetRecursiceDescription(main); });
                main.Add(showActions);

                return main;
            }



            public void Main(string eventName)
            {
                var main = Initialize();
                Execute(main, eventName);
            }


        }
    }
}