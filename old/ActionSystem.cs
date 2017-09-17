using System;
using System.Collections.Generic;
using VRageMath;
using VRage.Game;
using System.Text;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.EntityComponents;
using VRage.Game.Components;
using VRage.Collections;
using VRage.Game.ObjectBuilders.Definitions;
using SpaceEngineersScripts;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI;

namespace ActionSystem
{
    public class Program : MyGridProgram
    {

        #region copymeto programmable block  
        //for the most recent version see
        //see https://github.com/kummahiih/SpaceEngineersScripts/blob/master/ActionSystem.cs 
        #region implementations with block or group names 
        const string LCD_OUT_NAME = "outPanel";

        public ScriptProgram Initialize()
        {
            var usage = @"
type programs name to PB's 
parameter field and run.
examples:
'helloTerminal', 
'listActions' 
'eval.helloToEcho' or
'eval.usageToEcho' (prints this)
see the codes for more details
";

            Echo("initializing program infrastructure");
            var parser = new ArgsParser();
            var continuations = new ContinuationContainer(this, name: "eval", argsParser: parser);
            var main = new MainScriptProgram(this, name: "Main", continuations: continuations, argsParser: parser);
            Echo("initializing programs");

            Echo("initializing sinks");
            var lcdSink = new LcdSink(this, name: "lcdSink", blockName: LCD_OUT_NAME);
            var echoSink = new EchoSink(this, name: "echoSink");

            Echo("initializing hello worlds");
            {
                var helloAction = new LambdaScriptAction(this, name: "helloTerminal",
                    lambda: param => { Echo("Hello Terminal"); });
                main.Add(helloAction);
                var helloLcd = new NamedBlockProgram(
                    this, name: "helloLcd", blockName: LCD_OUT_NAME,
                    blockAction: new WriteTextOnLcd(() => "Hello lcd!"));
                main.Add(helloLcd);

                var helloSource = new LambdaScriptFunc(this, name: "helloSource",
                  argsParser: parser,
                  lambda: param => new OutputArgs<string>("Hello world"));
                main.Add(helloSource);

                var helloToEcho = new Continuation(this,
                   name: "helloToEcho",
                   getter: helloSource.Getter,
                   setter: echoSink.Input);
                main.Add(helloToEcho);

                var helloToLcd = new Continuation(this,
                    name: "helloToLcd",
                    getter: helloSource.Getter,
                    setter: lcdSink.Input);
                main.Add(helloToLcd);
            }
            Echo("initializing usefull commands");
            {
                var usageSource = new LambdaScriptFunc(this, name: "usageSource",
                  argsParser: parser,
                  lambda: param => new OutputArgs<string>(usage));
                main.Add(usageSource);

                var helloToEcho = new Continuation(this,
                   name: "usageToEcho",
                   getter: usageSource.Getter,
                   setter: echoSink.Input);
                main.Add(helloToEcho);

                var showActions = new NamedBlockProgram(
                    this, name: "listActions", blockName: LCD_OUT_NAME,
                    blockAction: new WriteTextOnLcd(() => "{" + main.ToString() + "}"));
                main.Add(showActions);
            }
            Echo("initialization done");
            return main;
        }
        #endregion
        #region script entry points. 

        // this mixes everythin up
        //ScriptProgram MainProgram;
        //public Program()
        //{
        //    MainProgram = Initialize();
        //}

        public void Main(string eventName)
        {
            var main = Initialize();
            eventName = eventName ?? "eval.usageToEcho";
            main.Main(eventName);
        }
        #endregion
    }
    public class EchoSink : ScriptProgram
    {
        protected string StrBuf;

        public EchoSink(MyGridProgram env, string name) : base(env, name) { StrBuf = ""; }

        protected override void OnInput(OutputArgs args)
        {
            StrBuf = "";
            var StrArgs = args as OutputArgs<String>;
            if (StrArgs == null) return;
            StrBuf = StrArgs.Value ?? "";
        }

        protected override void OnMain(string param = "")
        {
            Env.Echo(StrBuf);
        }
    }
    public class LcdSink : EchoSink
    {

        protected readonly WriteTextOnLcd WriteAction;
        public readonly string BlockName;
        public LcdSink(MyGridProgram env, string name, string blockName) : base(env, name)
        {
            BlockName = blockName;
            WriteAction = new WriteTextOnLcd(() => StrBuf);
        }

        protected override void OnMain(string param = "")
        {
            Env.ForNamedBlockIfApply(blockName: BlockName, action: WriteAction);
        }
    }

    #region program infrastructure 
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
            Env.GetBlocks(groupCondition: g => g.Name == GroupName, blockCondition: BlockCondition)
                .ForEach(BlockAction.Execute);
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
            Env
                .GetBlocks(groupCondition: GroupCondition, blockCondition: BlockCondition)
                .ForEach(BlockAction.Execute);
        }

        public override string ToString()
        {
            return base.ToString() + ", " + BlockAction.ToString() +
                (GroupCondition != null ? ", GC:" + GroupCondition.GetType().Name : "") +
                (BlockCondition != null ? ", BC:" + BlockCondition.GetType().Name : "");
        }
    }
    #endregion
    #region program containers and management
    public class ArgsParser
    {
        public string Args { get; protected set; } = "";
        public string FirstParam { get; protected set; } = "";
        public string Rest { get; protected set; } = "";
        public ArgsParser() { }
        public void Parse(string args) { FirstParam = Args = (args ?? ""); Rest = ""; OnParse(); }

        protected virtual void OnParse()
        {
            int i = 0;
            while (i < Args.Length)
            {
                var rest = Args.Substring(i);
                if (rest.StartsWith(".")) { break; }
                if (rest.StartsWith("\\")) { i += 2; continue; }
                if (rest.StartsWith(@"\.")) { i += 2; continue; }
                i++;
            }
            if (i < Args.Length) { FirstParam = Args.Substring(0, i); }
            if (i + 1 < Args.Length) { Rest = Args.Substring(i + 1); }
        }
    }

    /// <summary> 
    /// wraps actions into a tree like stucture.  
    /// actions are implemented as delegates, because some methods can be only called  
    /// </summary> 
    public class MainScriptProgram : ScriptProgram
    {
        protected readonly HashSet<ScriptProgram> Programs;
        protected readonly ContinuationContainer Continuations;
        protected readonly ArgsParser ArgsParser;


        public MainScriptProgram(MyGridProgram env, string name, ContinuationContainer continuations, ArgsParser argsParser
            ) :
            base(env, name)
        { Programs = new HashSet<ScriptProgram>() { continuations }; Continuations = continuations; ArgsParser = argsParser; }

        public void Add(ScriptProgram program)
        {
            Programs.Add(program);
        }
        public void Add(Continuation continuation)
        { Continuations.Register(continuation, Programs); }

        protected override void OnMain(string param = "")
        {
            param = param ?? "";

            ArgsParser.Parse(param);

            Programs
                .Where(x => x.Name == ArgsParser.FirstParam)
                .ForEach(x => x.Main(ArgsParser.Rest));
        }

        //no serializers of any kind available 
        public override string ToString()
        {
            var ret = "{" + base.ToString() + ",\n";

            ret += "childs:{\n";
            Programs.ForEach(item => { ret += "{" + item.ToString() + "},\n"; });
            ret += "},\n";

            ret += "}\n";

            return ret;
        }
    }


    #region program input and output

    public class OutputArgs<T> : OutputArgs
    {
        public readonly T Value;
        public OutputArgs(T value) : base() { Value = value; }
    }
    public abstract class OutputArgs
    {
        public ScriptProgram Sender;
        public ScriptProgram Receiver;
        public string Param;
        public OutputArgs() { }
        public override string ToString() { return "{S:" + (Sender?.Name ?? "") + ", R:" + (Receiver?.Name ?? "") + ", P:" + (Param ?? "") + "}"; }
    }

    public class Continuation : ScriptProgram
    {
        private Func<string, OutputArgs> Getter;
        private Action<OutputArgs> Setter;
        public OutputArgs LatestValue { get; private set; }
        public Continuation(MyGridProgram env, string name, Func<string, OutputArgs> getter, Action<OutputArgs> setter) : base(env, name) { Getter = getter; Setter = setter; }

        protected override void OnMain(string param = "") { LatestValue = Getter(param); }

        public void Continue() { Setter(LatestValue); }
    }

    public class ContinuationContainer : ScriptProgram
    {
        protected ICollection<Continuation> Continuations;
        protected readonly ArgsParser ArgsParser;
        public ContinuationContainer(MyGridProgram env, string name, ArgsParser argsParser) : base(env, name) { Continuations = new List<Continuation>(); ArgsParser = argsParser; }

        protected override void OnMain(string param = "")
        {
            ArgsParser.Parse(param);
            Env.Echo("Getting new values for:'" + ArgsParser.FirstParam + "' with:'" + ArgsParser.Rest + "'");
            var cons = Continuations
                .Where(c => string.IsNullOrEmpty(ArgsParser.FirstParam) || c?.Name == ArgsParser.FirstParam);
            cons.ForEach(c => c.Main(ArgsParser.Rest));
            Env.Echo("Setting new values");
            cons.ForEach(c => c.Continue());
        }
        public void Register(Continuation connection, ICollection<ScriptProgram> programs)
        {
            if (connection == null || programs == null) return;
            Env.Echo("Registering: " + connection.ToString());
            connection.Main(PARAM_REGISTERED); //sender set here
            connection.Continue();   //receiver set here 
            programs.Add(connection.LatestValue.Sender);
            programs.Add(connection.LatestValue.Receiver);
            Continuations.Add(connection);
            Env.Echo("Registered: " + connection.ToString());
        }
        public override string ToString()
        {
            var ret2 = "N:" + Name + ", childs:{\n";
            Continuations.ForEach(c => { ret2 += "{" + c.ToString() + "}, \n"; });
            ret2 += "}\n";
            return ret2;
        }
    }
    #endregion

    #endregion
    #region program base classes

    public class LambdaScriptFunc : ScriptProgram
    {
        protected readonly Func<string, OutputArgs> Lambda;
        public OutputArgs Output { protected set; get; }
        protected readonly ArgsParser ArgsParser;
        public LambdaScriptFunc(MyGridProgram env, string name, ArgsParser argsParser, Func<string, OutputArgs> lambda) : base(env, name) { Lambda = lambda; ArgsParser = argsParser; }

        public OutputArgs Getter(string param)
        {
            OnMain(param);
            return Output;
        }

        protected override void OnMain(string param = "")
        {
            if (Lambda != null)
            {
                ArgsParser.Parse(param);
                Output = Lambda(ArgsParser.FirstParam);
                Output.Sender = this;
                Output.Param = ArgsParser.Rest;
            }
        }
    }


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
        public static readonly string PARAM_REGISTERED = "REGISTERED";

        public MyGridProgram Env { get; private set; }
        public readonly string Name; //here name is id because this is used in game that way 

        public ScriptProgram(MyGridProgram env, string name) { Env = env; Name = name; }

        public override string ToString() { return "N:" + Name; }

        public void Input(OutputArgs args)
        {
            if (args == null) return;
            Env.Echo("input args: " + "(" + args.ToString() + ")");
            args.Receiver = this;
            OnInput(args);
            if (args.Param == PARAM_REGISTERED) return;
            Main(args.Param);
        }

        protected virtual void OnInput(OutputArgs args) { }

        public void Main(string param = "")
        {
            param = string.IsNullOrEmpty(param) ? "" : param;


            Env.Echo("executing: " + Name + "(" + param + ")");
            OnMain(param);
        }

        protected abstract void OnMain(string param = "");
    }
    #endregion
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
        //linq substitutes because can not write 'using System.Linq;'
        //static extensions and templates are not supportet it seems 
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        { if (action == null || source == null) return; foreach (var x in source) { action(x); } }

        public static IEnumerable<T> Where<T>(this IEnumerable<T> source, Func<T, bool> condition)
        {
            var ret = new List<T>();
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
            var blocksCollected = new List<IMyTerminalBlock>();
            var blocksTemp = new List<IMyTerminalBlock>();
            var groups = new List<IMyBlockGroup>();
            This.GridTerminalSystem.GetBlockGroups(groups);

            groups.Where(ReplaceNullCondition(groupCondition))
                .ForEach(
                g =>
                {
                    g.GetBlocks(blocksTemp, ReplaceNullCondition(blockCondition));
                    blocksCollected.AddRange(blocksTemp);
                }
                );

            return blocksCollected;
        }

        public static void ForNamedBlockIfApply(this MyGridProgram This, string blockName, BlockAction action, Func<IMyTerminalBlock, bool> blockCondition = null)
        {
            var block = This.GridTerminalSystem.GetBlockWithName(blockName);
            if (block != null && (ReplaceNullCondition(blockCondition)(block)) && action != null)
                action.Execute(block);
        }

        #endregion
        #region checks and tests
        public static Func<IMyTerminalBlock, bool> ReplaceNullCondition(Func<IMyTerminalBlock, bool> x) { return x != null ? x : _ => true; }
        public static Func<IMyBlockGroup, bool> ReplaceNullCondition(Func<IMyBlockGroup, bool> x) { return x != null ? x : _ => true; }
        public static bool IsSorter(this IMyTerminalBlock x) { return x is IMyConveyorSorter; }
        public static bool IsTimer(this IMyTerminalBlock x) { return x is IMyTimerBlock; }
        public static bool IsIMyTextPanel(this IMyTerminalBlock x) { return x is IMyTextPanel; }
        #endregion
        #endregion
    } // Omit this last closing brace as the game will add it back in

}

