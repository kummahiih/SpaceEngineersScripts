﻿using System;
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

namespace ActionSystemIOAsEvents
{
    public class Program : MyGridProgram
    {

        #region copymeto programmable block  
        //see https://github.com/kummahiih/SpaceEngineersScripts/blob/master/ActionSystem.cs 
        #region implementations with block or group names 
        const string LCD_OUT_NAME = "outPanel";

        public ScriptProgram Initialize()
        {
            Echo("initializing");
            var main = new MainScriptProgram(this, name: "Main");

            var helloAction = new LambdaScriptAction(this, name: "helloTerminal",
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

            Echo("initializing done");
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
    public class GroupProgram : BlockProgram
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
        { return base.ToString() + ", B:" + BlockName; }

        protected override void OnMain(string param = "")
        {
            Env.ForNamedBlockIfApply(blockName: BlockName, blockCondition: BlockCondition, action: BlockAction);
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
    public class MainScriptProgram : ScriptProgram
    {
        protected readonly ICollection<ScriptProgram> ScriptActions;
        protected readonly ICollection<IONode> IONodes;
        public MainScriptProgram(MyGridProgram env, string name) : base(env, name) { ScriptActions = new List<ScriptProgram>(); IONodes = new List<IONode>(); }

        public void Add(ScriptProgram action) { ScriptActions.Add(action); }

        protected override void OnMain(string param = "")
        {
            ScriptActions
                .Where(x => string.IsNullOrEmpty(param) || x.Name == param)
                .ForEach(x => x.Main(param));
        }

        //no serializers of any kind available 
        public override string ToString()
        {
            var ret = "{" + base.ToString() + ",\n";

            ret += "childs:{";
            Ext.ForEach(ScriptActions, item => { ret += item.ToString() + ",\n"; });
            ret += "},\n";

            ret += "IONodes:{";
            Ext.ForEach(IONodes, item => { ret += item.ToString() + ",\n"; });
            ret += "}\n";

            ret += "}\n";

            return ret;
        }
    }

    #region program input and output 
    #region type spesific io overloads
    public class OutputArgsString : OutputArgs
    {
        public readonly string Value;
        public OutputArgsString(ScriptProgram sender, string param, string value) : base(sender, param) { Value = value; }
    }
    public class OutputArgsFloat: OutputArgs
    {
        public readonly float Value;
        public OutputArgsFloat(ScriptProgram sender, string param, float value): base(sender,param) { Value = value; }
    }
    public class FloatIONode : IONode
    {
        public FloatIONode(string name) : base(name) { }
        public void RaiseValueChanged(OutputArgsFloat args) { base.RaiseValueChanged(args); }
        public void RegisterHandler(ScriptProgram receiver, Action<OutputArgsFloat> listener) { base.RegisterHandler(receiver, args => listener((OutputArgsFloat)args)); }
    }
    public class StringIONode : IONode
    {
        public StringIONode(string name) : base(name) { }
        public void RaiseValueChanged(OutputArgsString args) { base.RaiseValueChanged(args); }
        public void RegisterHandler(ScriptProgram receiver, Action<OutputArgsString> listener) { base.RegisterHandler(receiver, args => listener((OutputArgsString)args)); }
    }
    #endregion
    #region generic io
    public abstract class OutputArgs
    {
        public readonly string Param;
        public readonly ScriptProgram Sender;
        public OutputArgs(ScriptProgram sender, string param) { Param = param; Sender = sender; }
    }
    public class Connection
    {
        public ScriptProgram Receiver;
        public Action<OutputArgs> Update;
        public override string ToString() { return "{R:" + Receiver.Name + "}"; }
    }
    public abstract class IONode
    {
        public readonly string Name;
        protected ICollection<Connection> Connections;
        public IONode(string name) { Name = name; Connections = new List<Connection>(); }

        protected void RaiseValueChanged(OutputArgs args)
        {
            Connections.ForEach(connection =>
            { connection.Update(args); }
            );
        }

        protected void RegisterHandler(ScriptProgram receiver, Action<OutputArgs> listener)
        {
            if (receiver == null || listener == null) return;
            Connections.Add(
                new Connection()
                {
                    Receiver = receiver,
                    Update = args => listener(args)
                });
        }

        public override string ToString()
        {
            var ret2 = "{N:" + Name + ", targets:{";
            Connections.ForEach(c => { ret2 += c.ToString() + ", "; });
            ret2 += "}\n";
            return ret2;
        }
    }
    #endregion
    #endregion
    public class LambdaScriptAction : ScriptProgram
    {
        protected readonly Action<string> Lambda;

        public LambdaScriptAction(MyGridProgram env, string name, Action<string> lambda) : base(env, name) { Lambda = lambda; }

        protected override void OnMain(string param = "")
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

        public void Main(string param = "")
        {
            param = string.IsNullOrEmpty(param) ? "" : param;
            Env.Echo("executing: " + Name + "(" + param + ")");
            OnMain(param);
        }

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
        public static void ForEach(this IEnumerable<ScriptProgram> source, Action<ScriptProgram> action)
        { if (action == null || source == null) return; foreach (var x in source) { action(x); } }
        public static void ForEach(this IEnumerable<IMyTerminalBlock> source, Action<IMyTerminalBlock> action)
        { if (action == null || source == null) return; foreach (var x in source) { action(x); } }
        public static void ForEach(this IEnumerable<IMyBlockGroup> source, Action<IMyBlockGroup> action)
        { if (action == null || source == null) return; foreach (var x in source) { action(x); } }
        public static void ForEach(this IEnumerable<Connection> source, Action<Connection> action)
        { if (action == null || source == null) return; foreach (var x in source) { action(x); } }
        public static void ForEach(this IEnumerable<IONode> source, Action<IONode> action)
        { if (action == null || source == null) return; foreach (var x in source) { action(x); } }

        public static IEnumerable<ScriptProgram> Where(this IEnumerable<ScriptProgram> source, Func<ScriptProgram, bool> condition)
        {
            var ret = new List<ScriptProgram>();
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
        #endregion
        #endregion

    } // Omit this last closing brace as the game will add it back in

}