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

        #region implementations with block or group names
        const string LCD_OUT_NAME = "outPanel";

        public ScriptProgram Initialize()
        {
            var main = new MainScriptProgram(this,name: "Main");

            var helloAction = new LambdaScriptAction(this, name: "helloTerminal", 
                lambda: param => { Echo("Hello Terminal"); });
            main.Add(helloAction);
            var helloLcd = new NamedBlockProgram( 
                this, name: "helloLcd", blockName: LCD_OUT_NAME, 
                blockAction:new WriteTextOnLcd( () => "Hello lcd!"));
            main.Add(helloLcd);
            var showActions = new NamedBlockProgram(
                this, name: "listActions", blockName: LCD_OUT_NAME,
                blockAction: new WriteTextOnLcd(() => main.ToString()));
            main.Add(showActions);

            return main;
        }
        #endregion


        #region script entry points.

        public void Main(string eventName)
        {
            var main = Initialize();
            main.Execute( eventName);
        }

        #endregion
    }

    #region block and group related program definitions

    public class GroupProgram : BlockProgram
    {
        public string GroupName { get; private set; }
        public GroupProgram(MyGridProgram env, string name, string group, BlockAction blockAction, Func<IMyTerminalBlock, bool> blockCondition ) : 
            base(env, name: name, blockAction: blockAction, blockCondition: blockCondition)
        { GroupName = group; }
        public override string ToString() { return base.ToString() + ", G:" + GroupName; }
        protected override void OnExecute(string param = "")
        {
            Env.GetBlocks(groupCondition: g => g.Name ==GroupName, blockCondition: BlockCondition)
                .ForEachB(BlockAction.Execute);                
        }
    }

    public class NamedBlockProgram : BlockProgram
    {
        public string BlockName { get; private set; }

        public NamedBlockProgram(
            MyGridProgram env,            
            string name, string blockName, BlockAction blockAction,
            Func<IMyTerminalBlock, bool> blockCondition = null) : 
            base(env, name: name, blockAction: blockAction, blockCondition: blockCondition)
        { BlockName = blockName; }

        public override string ToString()
        { return base.ToString() + ", B:" + BlockName;  }

        protected override void OnExecute(string param = "")
        {
            Env.ForBlockWhereApply(blockName: BlockName, blockCondition: BlockCondition, action:BlockAction);
        }
    }
   

    public class BlockProgram : ScriptProgram
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
      
        protected override void OnExecute(string param = "")
        {
            Env.GetBlocks(groupCondition: GroupCondition, blockCondition: BlockCondition)
                .ForEachB(BlockAction.Execute);
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
    public class MainScriptProgram: ScriptProgram
    {
        public readonly List<ScriptProgram> ScriptActions;
        public MainScriptProgram(MyGridProgram env, string name ): base(env, name) { ScriptActions = new List<ScriptProgram>(); }

        public void Add(ScriptProgram action) { ScriptActions.Add(action); }

        protected override void OnExecute(string param = "")
        {
            ScriptActions
                .WhereA(x => string.IsNullOrEmpty(param) || x.Name == param)
                .ForEachA(x => x.Execute(param));
        }

        //no serializers of any kind available
        public override string ToString()
        {          
            var ret = "{" + base.ToString();
            ret += ",\n childs:{";
            ScriptActions
                .ForEachA(action => { ret += action.ToString() + ",\n"; });
            ret += "}}\n";

            return ret;
        }
    }

    public class LambdaScriptAction : ScriptProgram
    {
        protected Action<string> Lambda;

        public LambdaScriptAction(MyGridProgram env, string name, Action<string> lambda) : base(env, name) { Lambda = lambda;}

        protected override void OnExecute(string param = "")
        {
            if (Lambda != null) Lambda(param);
        }
    }

    public abstract class ScriptProgram
    {
        public MyGridProgram Env { get; private set; }
        public readonly string Name; //here name is id because this is used in game that way

        public ScriptProgram(MyGridProgram env, string name) { Env = env; Name = name; }

        public override string ToString() { return "N:" + Name; }

        public void Execute(string param = "")
        {
            param = string.IsNullOrEmpty(param) ? "" : param;
            Env.Echo("executing: " + Name + "(" + param + ")");
            OnExecute(param);
        }

        protected abstract void OnExecute(string param = "");
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
        public static void ForEachA(this IEnumerable<ScriptProgram> source, Action<ScriptProgram> action)
        { foreach (var x in source) { if (action != null) action(x); } }
        public static void ForEachB(this IEnumerable<IMyTerminalBlock> source, Action<IMyTerminalBlock> action)
        { foreach (var x in source) { if (action != null) action(x); } }
        public static void ForEachG(this IEnumerable<IMyBlockGroup> source, Action<IMyBlockGroup> action)
        { foreach (var x in source) { if (action != null) action(x); } }

        public static IEnumerable<ScriptProgram> WhereA(this IEnumerable<ScriptProgram> source, Func<ScriptProgram, bool> condition)
        {
            var ret = new List<ScriptProgram>();
            source.ForEachA(x => { if (condition == null || condition(x)) ret.Add(x); });
            return ret;
        }
        public static IEnumerable<IMyTerminalBlock> WhereB(this IEnumerable<IMyTerminalBlock> source, Func<IMyTerminalBlock, bool> condition)
        {
            var ret = new List<IMyTerminalBlock>();
            source.ForEachB(x => { if (condition == null || condition(x)) ret.Add(x); });
            return ret;
        }
        public static IEnumerable<IMyBlockGroup> WhereG(this IEnumerable<IMyBlockGroup> source, Func<IMyBlockGroup, bool> condition)
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
        public static List<IMyTerminalBlock> GetBlocks(this MyGridProgram This, Func<IMyBlockGroup, bool> groupCondition = null, Func<IMyTerminalBlock, bool> blockCondition = null)
        {
            var blocksConv = new List<IMyTerminalBlock>();
            var blocks = new List<IMyTerminalBlock>();
            var groups = new List<IMyBlockGroup>();
            This.GridTerminalSystem.GetBlockGroups(groups);
            groups.WhereG(CheckNull(groupCondition))
                .ForEachG(x => blocks.AddRange(x.Blocks));
            blocks.WhereB(CheckNull(blockCondition))
                .ForEachB(b => blocksConv.Add(b));
            return blocksConv;
        }
        
        public static void ForBlockWhereApply(this MyGridProgram This, string blockName, BlockAction action, Func<IMyTerminalBlock, bool> blockCondition = null)
        {
            var block = This.GridTerminalSystem.GetBlockWithName(blockName);
            if (block != null && (CheckNull(blockCondition)(block)) && action != null)
                action.Execute(block);
        }
        
        #endregion
        public static Func<IMyTerminalBlock, bool> CheckNull(Func<IMyTerminalBlock, bool> x){ return x!=null? x : _ => true; }
        public static Func<IMyBlockGroup, bool> CheckNull(Func<IMyBlockGroup, bool> x) { return x != null ? x : _ => true; }
        #region basic tests to use with Where
        public static bool IsSorter(this IMyTerminalBlock x)       {return x is IMyConveyorSorter; }
        public static bool IsTimer(this IMyTerminalBlock x)        { return x is IMyTimerBlock; }
        public static bool IsIMyTextPanel(this IMyTerminalBlock x) { return x is IMyTextPanel; }
        #endregion
        #endregion

    } // Omit this last closing brace as the game will add it back in

}

