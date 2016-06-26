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

namespace BasicInventory
{
    public class Program : MyGridProgram
    {

        #region copymeto programmable block 
        //based on https://github.com/kummahiih/SpaceEngineersScripts/blob/master/ActionSystem.cs
        //written 23-06-2016
        #region implementations with block or group names
        const string LCD_OUT_NAME = "outPanel";

        public MainScriptProgram Initialize()
        {
            var main = new MainScriptProgram(this, name: "Main");

            var helloAction = new LambdaScriptProgram(this, name: "helloTerminal",
                lambda: param => { Echo("Hello Terminal"); });
            main.Add(helloAction);
            var helloLcd = new NamedBlockProgram(
                this, name: "helloLcd", blockName: LCD_OUT_NAME,
                blockAction: new WriteTextOnLcd(() => "Hello lcd!"));
            main.Add(helloLcd);
            var showActions = new NamedBlockProgram(
                this, name: "listActions", blockName: LCD_OUT_NAME,
                blockAction: new WriteTextOnLcd(() => main.ToString()));
            main.Add(showActions);


            /*var inventory = new MainScriptProgram(this, name: "Inventory");
            main.Add(inventory);

            var oxygenMeasureProgram = 
                new OxygenMeasureProgram(this, name: "measureOxygen");

            var oxygenToLcdAction = 
                new WriteTextOnLcd(this, );

            var oxygenProgram = new BlockProgram(
                this,
                name:"oxygenLevelToLcd", 
                groupCondition:null,
                blockCondition: Ext.IsOxygenTank,
                blockAction: 
                */

            return main;
        }
        #endregion


        #region script entry points.

        public void Main(string eventName)
        {
            var main = Initialize();
            main.Main(eventName);
        }

        #endregion
    }

    #region block and group related program definitions

    public class GroupProgram: BlockProgram
    {
        public string GroupName { get; private set; }
        public GroupProgram(MyGridProgram env, string name, string group, BlockAction blockAction, Func<IMyTerminalBlock, bool> blockCondition) :
            base(env, name: name, blockAction: blockAction, blockCondition: blockCondition)
        { GroupName = group; }
        public override string ToString() { return base.ToString() + ", G:" + GroupName; }
        protected override void OnMain(string param = "")
        {
            Ext.ForEach(Env.GetBlocks(groupCondition: g => g.Name == GroupName, blockCondition: BlockCondition)
, BlockAction.Execute);
        }
    }

    public class NamedBlockProgram: BlockProgram
    {
        public string BlockName { get; private set; }

        public NamedBlockProgram(
            MyGridProgram env,
            string name, string blockName, BlockAction blockAction,
            Func<IMyTerminalBlock, bool> blockCondition = null) :
            base(env, name: name, blockAction: blockAction, blockCondition: blockCondition)
        { BlockName = blockName; }

        public override string ToString()
        { return base.ToString() + ", B:" + BlockName; }

        protected override void OnMain(string param = "")
        {
            Env.ForNamedBlockIfApply(blockName: BlockName, blockCondition: BlockCondition, action: BlockAction);
        }
    }


    public class BlockProgram : ScriptBase
    {
        public readonly BlockAction BlockAction;
        public readonly Func<IMyTerminalBlock, bool> BlockCondition;
        public readonly Func<IMyBlockGroup, bool> GroupCondition;
        public BlockProgram(MyGridProgram env, string name, BlockAction blockAction, Func<IMyBlockGroup, bool> groupCondition = null, Func<IMyTerminalBlock, bool> blockCondition = null) : base(env, name)
        {
            BlockAction = blockAction;
            BlockCondition = blockCondition;
            GroupCondition = groupCondition;
        }

        protected override void OnMain(string param = "")
        {
            Ext.ForEach(Env.GetBlocks(groupCondition: GroupCondition, blockCondition: BlockCondition)
, BlockAction.Execute);
        }

        public override string ToString()
        {
            return base.ToString() + ", " + BlockAction.ToString() +
                (GroupCondition != null ? ", GC:" + GroupCondition.GetType().Name : "") +
                (BlockCondition != null ? ", BC:" + BlockCondition.GetType().Name : "");
        }
    }

    #endregion

    #region programs

    /// <summary>
    /// wraps actions into a tree like stucture. 
    /// actions are implemented as delegates, because some methods can be only called 
    /// </summary>
    public class MainScriptProgram : ScriptBase
    {
        public readonly List<ScriptBase> ScriptPrograms;
        public MainScriptProgram(MyGridProgram env, string name) : base(env, name) { ScriptPrograms = new List<ScriptBase>(); }

        public void Add(ScriptBase action) { ScriptPrograms.Add(action); }

        protected override void OnMain(string param = "")
        {
            ScriptPrograms
                .Where(x => string.IsNullOrEmpty(param) || x.Name == param)
                .ForEach(x => x.Main(param));
        }

        //no serializers of any kind available
        public override string ToString()
        {
            var ret = "{" + base.ToString();
            ret += ",\n childs:{";
            Ext.ForEach(ScriptPrograms, action => { ret += action.ToString() + ",\n"; });
            ret += "}}\n";

            return ret;
        }

        public void Connect()
    }

    public class LambdaScriptProgram : ScriptBase
    {
        protected Action<string> Lambda;

        public LambdaScriptProgram(MyGridProgram env, string name, Action<string> lambda) : base(env, name) { Lambda = lambda; }

        protected override void OnMain(string param = "")
        {
            if (Lambda != null) Lambda(param);
        }
    }

    //public abstract class ScriptProgram<IT, OT>: ScriptBase
    //{
    //    public ScriptProgram(MyGridProgram env, string name) : base(env, name){}

    //    //these could be classes of their own, but I am a bit afraid that i get problems with generics
    //    public OT Result { get; protected set; }
    //    public IT Input { get; set; }
    //    private string InputToString() => (Input != null ? " ," + Input.ToString() : "");
    //    private string ResultToString() => (Result != null ? " ," + Result.ToString() : "");

    //    protected override void OnMainStart(string param = "") {
    //        base.OnMainStart(param);
    //        Env.Echo("{" + InputToString() +"}");
    //    }
    //    protected override void OnMainEnd() {
    //        base.OnMainStart();
    //        Env.Echo("{" + ResultToString() + "}");
    //    }
    //}

    #region input and output
    public class Input<TI> : Lazy<TI> { }
    public class Output<TO> : Lazy<TO> { }
    public class Connection
    {
        public ScriptBase Source { get; private set; }
        public ScriptBase Sink { get; private set; }
        public Connection(ScriptBase source, ScriptBase sink)   { Source = source; Sink = sink; }
    }
    #endregion

    public abstract class ScriptBase
    {
        public MyGridProgram Env { get; private set; }
        public readonly string Name; //here name is id because this is used in game that way

        public ScriptBase(MyGridProgram env, string name) { Env = env; Name = name; }
        public override string ToString() { return "N:" + Name; }
        public void Main(string param = "")
        {
            param = string.IsNullOrEmpty(param) ? "" : param;
            OnMainStart();
            OnMain(param);
            OnMainEnd();
        }
        protected virtual void OnMainStart(string param = "") { Env.Echo("executing: " + Name + "(" + param + ")"); }
        protected virtual void OnMainEnd() { Env.Echo("executed:" + Name); }
        protected abstract void OnMain(string param = "");
    }
    #endregion

    #region actions for blocks

    public class WriteTextOnLcd : BlockAction
    {
        public readonly Func<string> GetText;
        public WriteTextOnLcd(Func<string> getText) : base("WriteTextOnLcd") { GetText = getText; }
        public override void Execute(IMyTerminalBlock block)
        {
            var tp = block as IMyTextPanel;
            if (GetText == null || tp == null) return;
            tp.WritePublicText(GetText());
            tp.ShowPublicTextOnScreen();
        }
    }

    public class BlockAction
    {
        public readonly string ActionName;
        public BlockAction(string actionName)
        { ActionName = actionName; }
        public virtual void Execute(IMyTerminalBlock block)
        {
            block.ApplyTerminalAction(actionName: ActionName);
        }
        public override string ToString()
        {
            return "A:" + ActionName;
        }
    }

    #endregion

    public static class Ext
    {
        #region linq substitutes
        //linq substitutes without templates nor static extensions
        //static extensions and templates are not supportet it seems
        public static void ForEach(this IEnumerable<ScriptBase> source, Action<ScriptBase> action)
        { foreach (var x in source) { if (action != null) action(x); } }
        public static void ForEach(this IEnumerable<IMyTerminalBlock> source, Action<IMyTerminalBlock> action)
        { foreach (var x in source) { if (action != null) action(x); } }
        public static void ForEach(this IEnumerable<IMyBlockGroup> source, Action<IMyBlockGroup> action)
        { foreach (var x in source) { if (action != null) action(x); } }

        public static IEnumerable<ScriptBase> Where(this IEnumerable<ScriptBase> source, Func<ScriptBase, bool> condition)
        {
            var ret = new List<ScriptBase>();
            source.ForEach(x => { if (condition == null || condition(x)) ret.Add(x); });
            return ret;
        }
        public static IEnumerable<IMyTerminalBlock> Where(this IEnumerable<IMyTerminalBlock> source, Func<IMyTerminalBlock, bool> condition)
        {
            var ret = new List<IMyTerminalBlock>();
            source.ForEach(x => { if (condition == null || condition(x)) ret.Add(x); });
            return ret;
        }
        public static IEnumerable<IMyBlockGroup> Where(this IEnumerable<IMyBlockGroup> source, Func<IMyBlockGroup, bool> condition)
        {
            var ret = new List<IMyBlockGroup>();
            source.ForEach(x => { if (condition == null || condition(x)) ret.Add(x); });
            return ret;
        }
        #endregion
        #region basic operations
        public static void ApplyTerminalAction(this IMyTerminalBlock block, string actionName)
        {
            block.GetActionWithName(actionName).Apply(block);
        }
        public static List<IMyTerminalBlock> GetBlocks(this MyGridProgram This, Func<IMyBlockGroup, bool> groupCondition = null, Func<IMyTerminalBlock, bool> blockCondition = null)
        {
            var blocksConv = new List<IMyTerminalBlock>();
            var blocks = new List<IMyTerminalBlock>();
            var groups = new List<IMyBlockGroup>();
            This.GridTerminalSystem.GetBlockGroups(groups);
            groups.Where(ReplaceNullCondition(groupCondition))
                .ForEach(x => blocks.AddRange(x.Blocks));
            blocks.Where(ReplaceNullCondition(blockCondition))
                .ForEach(b => blocksConv.Add(b));
            return blocksConv;
        }

        public static void ForNamedBlockIfApply(this MyGridProgram This, string blockName, BlockAction action, Func<IMyTerminalBlock, bool> blockCondition = null)
        {
            var block = This.GridTerminalSystem.GetBlockWithName(blockName);
            if (block != null && (ReplaceNullCondition(blockCondition)(block)) && action != null)
                action.Execute(block);
        }

        #endregion
        public static Func<IMyTerminalBlock, bool> ReplaceNullCondition(Func<IMyTerminalBlock, bool> x) { return x != null ? x : _ => true; }
        public static Func<IMyBlockGroup, bool> ReplaceNullCondition(Func<IMyBlockGroup, bool> x) { return x != null ? x : _ => true; }
        #region basic tests to use with Where
        public static bool IsSorter(this IMyTerminalBlock x) { return x is IMyConveyorSorter; }
        public static bool IsTimer(this IMyTerminalBlock x) { return x is IMyTimerBlock; }
        public static bool IsIMyTextPanel(this IMyTerminalBlock x) { return x is IMyTextPanel; }
        public static bool IsOxygenTank(this IMyTerminalBlock x) { return x is IMyOxygenTank; }
        #endregion
        #endregion

    } // Omit this last closing brace as the game will add it back in

}