using System;
using System.Collections.Generic;
using VRageMath;
using VRage.Game;
using System.Text;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.EntityComponents;
using VRage.Game.Components;
using VRage.Collections;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using SpaceEngineersScripts;
using static ActionSystem.Program;

namespace ActionSystem
{
    public class Program : MyGridProgram
    {

        #region copymeto programmable block 

        #region ScriptAction logic
        public void Execute(ScriptAction scriptAction, string param = "")
        {
            //echo can not be called from ScriptAction so this has to be a local non static function
            Echo(" on action: " + scriptAction.ToString() + (string.IsNullOrEmpty(param) ? " with param " + param : ""));
            scriptAction.Execute(param);
            scriptAction.EntryActions
                .WhereA(x => string.IsNullOrEmpty(param) ||x.Name == param)
                .ForEachA( x => Execute(x));
        }

        //no serializers of any kind available
        public string GetRecursiceDescription(ScriptAction scriptAction)
        {
            var ret = "{" + scriptAction.ToString();
            ret += ",\n childs:{";
            scriptAction.EntryActions
                .ForEachA(action => { ret += GetRecursiceDescription(action) + ","; });
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
            public GroupAction(Action<string> action, string name, string group, string actionName) : base(action, name: name)
            { GroupName = group; ActionName = actionName; }

            public override string ToString()
            { return base.ToString() + ", G:" + GroupName + ", A:" + ActionName; }

        }

        public ScriptAction GetGroupAction(string name, string group,
            Func<IMyTerminalBlock, bool> condition,
            string actionName)
        {
            return new GroupAction(
                action: param => this.ForBlocksInGoupWhereApply(group: group, condition: condition, actionName: actionName),
                name: name, group: group, actionName: actionName);
        }

        public class BlockAction : ScriptAction
        {
            public String BlockName { get; private set; }
            public String ActionName { get; private set; }
            public BlockAction(Action<string> action, string name, string blockName, string actionName) : base(action, name: name)
            { BlockName = blockName; ActionName = actionName; }

            public override string ToString()
            { return base.ToString() + ", B:" + BlockName + ", A:" + ActionName; }
        }

        public ScriptAction GetBlockAction(string name,
            Func<IMyTerminalBlock, bool> condition, string blockName, string actionName)
        {
            return new BlockAction(
                action: param => this.ForBlockWhereApplyS(name: name, condition: condition, actionName: actionName),
                name: name, blockName: blockName, actionName: actionName);
        }

        public ScriptAction GetLcdOutputAction(string name, string blockName, Func<string> data)
        {
            return new BlockAction(name: name, blockName: blockName,
                actionName: "LcdOutputAction",
                action: param =>
                {
                    this.ForBlockWhereApplyA(blockName, x => x.IsIMyTextPanel(),
                        block =>
                        {
                            (block as IMyTextPanel).WritePublicText(data());
                            (block as IMyTextPanel).ShowPublicTextOnScreen();
                        });
                });
        }

        public ScriptAction GetListActionsToLcdOutAction(string name, string blockName, ScriptAction main)
        {
            return GetLcdOutputAction(name, blockName: blockName, data: () => { return GetRecursiceDescription(main); });
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
            var helloLcd = GetLcdOutputAction(name: "helloLcd", blockName: LCD_OUT_NAME, data: () => "Hello Lcd");
            main.Add(helloLcd);
            var showActions = GetListActionsToLcdOutAction(name: "listActions", blockName: LCD_OUT_NAME, main: main);
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
    }


    /// <summary>
    /// wraps actions into a tree like stucture. 
    /// actions are implemented as delegates, because some methods can be only called 
    /// </summary>
    public class ScriptAction
    {
        public String Name { get; set; } //here name is id because this is used in game that way
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
            return "N:" + Name;
        }
    }

    public static class LinqStaticExtensions
    {
        #region linq substitutes
        //linq substitutes without templates nor static extensions
        //static extensions and templates are not supportet it seems
        public static void ForEachA(this IEnumerable<ScriptAction> source, Action<ScriptAction> action)
        { foreach (var x in source) { if (action != null) action(x); } }
        public static void ForEachIB(this IEnumerable<IMyTerminalBlock> source, Action<IMyTerminalBlock> action)
        { foreach (var x in source) { if (action != null) action(x); } }
        public static void ForEachB(this IEnumerable<IMyTerminalBlock> source, Action<IMyTerminalBlock> action)
        { foreach (var x in source) { if (action != null) action(x); } }
        public static void ForEachG(this IEnumerable<IMyBlockGroup> source, Action<IMyBlockGroup> action)
        { foreach (var x in source) { if (action != null) action(x); } }

        public static IEnumerable<ScriptAction> WhereA(this IEnumerable<ScriptAction> source, Func<ScriptAction, bool> condition)
        {
            var ret = new List<ScriptAction>();
            source.ForEachA(x => { if (condition == null || condition(x)) ret.Add(x); });
            return ret;
        }
        public static IEnumerable<IMyTerminalBlock> WhereB(this IEnumerable<IMyTerminalBlock> source, Func<IMyTerminalBlock, bool> condition)
        {
            var ret = new List<IMyTerminalBlock>();
            source.ForEachB(x => { if (condition == null || condition(x)) ret.Add(x); });
            return ret;
        }
        public static IEnumerable<IMyBlockGroup> WhereG(this IEnumerable<Sandbox.ModAPI.Ingame.IMyBlockGroup> source, Func<Sandbox.ModAPI.Ingame.IMyBlockGroup, bool> condition)
        {
            var ret = new List<Sandbox.ModAPI.Ingame.IMyBlockGroup>();
            source.ForEachG(x => { if (condition == null || condition(x)) ret.Add(x); });
            return ret;
        }
        #endregion

        #region basic operations
        public static void ApplyTerminalAction(this IMyTerminalBlock block, string actionName)
        {
            block.GetActionWithName(actionName).Apply(block);
        }

        public static List<IMyTerminalBlock> GetBlocks(this MyGridProgram This, string group)
        {
            var blocksConv = new List<IMyTerminalBlock>();
            var blocks = new List<IMyTerminalBlock>();
            var groups = new List<IMyBlockGroup>();
            This.GridTerminalSystem.GetBlockGroups(groups);
            groups.WhereG(x => x.Name == group)
                .ForEachG(x => blocks.AddRange(x.Blocks));
            blocks.ForEachIB(b => blocksConv.Add((IMyTerminalBlock)b));
            return blocksConv;
        }

        public static void ForBlocksInGoupWhereApply(this MyGridProgram This, string group, Func<IMyTerminalBlock, bool> condition, string actionName)
        {
            This.GetBlocks(group: group)
                .WhereB(x => x is IMyConveyorSorter)
                .ForEachB(x => x.ApplyTerminalAction( actionName: actionName));
        }

        public static void ForBlockWhereApplyA(this MyGridProgram This, string name, Func<IMyTerminalBlock, bool> condition, Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock> command)
        {
            var block = This.GridTerminalSystem.GetBlockWithName(name);
            if (block != null && (condition == null || condition(block)) && command != null)
                command(block);
        }

        public static void ForBlockWhereApplyS(this MyGridProgram This, string name, Func<IMyTerminalBlock, bool> condition, string actionName)
        {
            This.ForBlockWhereApplyA(name, condition, b => b.ApplyTerminalAction( actionName));
        }
        #endregion
        #region basic tests to use with Where
        public static bool IsSorter(this IMyTerminalBlock x)       {return x is IMyConveyorSorter; }
        public static bool IsTimer(this IMyTerminalBlock x)        { return x is IMyTimerBlock; }
        public static bool IsIMyTextPanel(this IMyTerminalBlock x) { return x is IMyTextPanel; }
        #endregion

        #endregion

    } // Omit this last closing brace as the game will add it back in


}
