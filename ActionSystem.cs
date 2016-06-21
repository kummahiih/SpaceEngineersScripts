
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

            #region copymeto programmable block
            #region linq substitutes
            //linq substitutes without templates nor static extensions
            //static extensions and templates are not supportet it seems
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
            #endregion
            #region basic operations
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
            #endregion
            #region basic tests to use with Where
            public bool IsSorter(Sandbox.ModAPI.Ingame.IMyTerminalBlock x)
            { return x is Sandbox.ModAPI.Ingame.IMyConveyorSorter; }

            //public bool IsTimer(IMyTerminalBlock x)  //where has this gone into?
            //{ return x is Sandbox.ModAPI.Ingame.IMyTimerBlock; }

            public bool IsIMyTextPanel(Sandbox.ModAPI.Ingame.IMyTerminalBlock x)
            { return x is IMyTextPanel; }

            public bool NoCondition(Sandbox.ModAPI.Ingame.IMyTerminalBlock x)
            { return true; }
            #endregion

            #region ScriptAction logic
            /// <summary>
            /// wraps actions into a tree like stucture. 
            /// actions are implemented as delegates, because some methods can be only called 
            /// </summary>
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
                //echo can not be called from ScriptAction so this has to be a local non static function
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
            #endregion

            #region scriptaction implementations
            #region generic actions (no block nor group names)
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
                public BlockAction(Action<string> action, string blockName, string actionName) : base(action, name: "BlockAction")
                { BlockName = blockName; ActionName = actionName; }

                public override string ToString()
                { return base.ToString() + ", B:" + BlockName + ", A:" + ActionName; }
            }

            public ScriptAction GetBlockAction(string name,
                Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool> condition, string blockName, string actionName)
            {
                return new BlockAction(                    
                    action: param => ForBlockWhereApplyS(name: name, condition: condition, actionName: actionName),
                    blockName:blockName, actionName:actionName);
            }

            public ScriptAction GetLcdOutAction(string blockName, string actionName, Func<string> data)
            {
                return new BlockAction(blockName:blockName, actionName:actionName,
                    action: param =>
                    {
                        ForBlockWhereApplyA(blockName, x => IsIMyTextPanel(x),
                            block =>
                            {
                                (block as Sandbox.ModAPI.IMyTextPanel).WritePublicText(data());
                                (block as Sandbox.ModAPI.IMyTextPanel).ShowPublicTextOnScreen();
                            });
                    });
            }

            public ScriptAction GetListActionsToLcdOutAction(string blockName, string actionName, ScriptAction main)
            {
                return GetLcdOutAction(blockName: blockName, actionName: actionName, data: () => { return GetRecursiceDescription(main); });
            }

            #endregion
            #region implementations with block or group names
            const string LCD_OUT_NAME = "outPanel";
            
            #endregion

            public ScriptAction Initialize()
            {
                var main = new ScriptAction(name: "Main");

                var helloAction = new ScriptAction(name: "helloTerminal", action: param => { Echo("Hello Terminal"); });
                main.Add(helloAction);
                var helloLcd = GetLcdOutAction(blockName:LCD_OUT_NAME, actionName: "helloLcd", data: () => "Hello Lcd");
                main.Add(helloLcd);
                var showActions = GetListActionsToLcdOutAction(blockName: LCD_OUT_NAME, actionName: "listActions", main:main);
                main.Add(showActions);

                return main;
            }
            #endregion

            #region script entry points. yes it gives a warning

            public void Main(string eventName)
            {
                var main = Initialize();
                Execute(main, eventName);
            }

            #endregion
            #endregion

        }
    }
}