using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Escaper
{
    class Program : MyGridProgram
    {
        #region copymeto programmable block
        /* Copyright 2017 Pauli Rikula (MIT https://opensource.org/licenses/MIT)
         * see https://github.com/kummahiih/SpaceEngineersScripts/blob/master/ somewhere
         * 
         * All this his would be easier to do with yield.
         * 
         * The state transition timer '<TIMER_NAME>' and the state persistence lcd '<LCD_NAME>'
         * are are critical for the functionality of the program and they had to be set up 
         * manually before any diagnostics can be done.
         * 
         * Set the '<TIMER_NAME>' timer to run the programmable block with empty argument
         * and set '<LCD_NAME>' to show public text on the screen.
         * Run the programmable block with the state name as a parameter. 
         * For diagnostics run 'IDLE'.
         * 
         * 
         * stop loop (STOP)
         * 
         * idle loop (IDLE):   
         *  time(time)   
         *  list all used named blocks (blocks)
         *  list all used named groups (groups)
         *  list all registered states (ls)   
        */

        // what an opportunity to refactor the code ..

        #region convience actions
        readonly BlockAction TimeAction = new BlockAction(
            LCD_NAME, lcd => (lcd as IMyTextPanel)
            ?.WritePublicText(
                DateTime.UtcNow.ToLongTimeString() + "\n"));

        readonly BlockAction ClearLCDAction = new BlockAction(
           LCD_NAME, lcd => (lcd as IMyTextPanel)
           ?.WritePublicText("", append: false));
        #endregion

        #region idle loop, stop, continue

        const string TIMER_NAME = "STATE TIMER";
        const string LCD_NAME = "STATE LCD";
        const string IDLE_STATE_NAME = "IDLE";


        NamedState Stop;
        JumpState Continue;
        NamedState Idle;

        void SetUpIdleAndStop()
        {
            //state entry points               
            Stop = new ActionState("STOP", TIMER_NAME,
                action: timer =>
                {
                    PrintToStateLcd("stopping\n");
                    (timer as IMyTimerBlock)?.ApplyAction("Stop");
                },
                delay: 0.1);
            states.Add(Stop);

            Continue = new JumpState("CONTINUE", LCD_NAME, block_measure:
                lcd =>
                {
                    var next = GetNextStateName(this);
                    PrintToStateLcd("continuing to '" + next + "'\n");
                    return next;
                }, delay: 0.1);
            states.Add(Continue);

            Idle = new ActionState(IDLE_STATE_NAME, ClearLCDAction, 5.0);

            //idle loop   
            Register(new[] {
            Idle,
                new ActionState("time", TimeAction, 5.0),
                new ActionState( "blocks", LCD_NAME,
                action:lcd => {
                    sb.Clear();

                    var blockManipulationNames = states
                        .Where(s => s  is ActionState)
                        .Cast<ActionState>()
                        .Where(s => s.NamedAction  is BlockAction)
                        .Select(s => s.NamedAction?.Name);

                    var blockMeasureNames = states
                        .Where(s => s  is JumpState)
                        .Cast<JumpState>()
                        .Where(s => s.JumpAction  is BlockMeasureAction)
                        .Select(s => s.JumpAction?.Name);

                    foreach(var name in blockManipulationNames
                        .Union(blockMeasureNames)
                        .Distinct())
                    {
                        var found_text =
                            this.GridTerminalSystem.GetBlockWithName(name) == null ? "" : " FOUND";
                        sb.Append("'" + name + "'" + found_text+"\n");
                    }
                    (lcd as IMyTextPanel)?.WritePublicText(sb.ToString(), append:false);
                },
                delay:5.0),
                new ActionState( "groups", LCD_NAME,
                action:lcd => {
                    sb.Clear();
                    foreach(var name in states
                        .Where(s => s  is ActionState)
                        .Cast<ActionState>()
                        .Where(s => s.NamedAction  is GroupAction)
                        .Select(s => s.NamedAction?.Name)
                        .Distinct())
                    {
                        var found_text =
                            this.GridTerminalSystem.GetBlockGroupWithName(name) == null ? "" : " FOUND";
                        sb.Append("'" + name + "'" + found_text + "\n");
                    }
                    (lcd as IMyTextPanel)?.WritePublicText(sb.ToString(), append:false);
                },
                delay:5.0),

            new ActionState( "ls", LCD_NAME,
                action:lcd => (lcd as IMyTextPanel)?.WritePublicText(
                        string.Join(",\n", states.Select(s => s.to_str())) + "\n"),
                delay:5.0),
            Idle});
        }
        #endregion

        #region scanner

        const string SCANNER_CAMERA = "SCANNER CAMERA";
        const string REMOTE_CONTROL = "REMOTE CONTROL";
        const string ESCAPE_THRUSTER = "ESCAPE THRUSTER";
        const double SCAN_RANGE = 30;

        ActionState Escape;
        ActionState EscapeChecksDone;

        public void SetupEscape()
        {
            Escape = new ActionState("ESCAPE", ClearLCDAction, 1);
            EscapeChecksDone = new ActionState("ESCAPE Checks done", ClearLCDAction, 1);
            Register(new NamedState[]
            {
                Escape,
                new ActionState("check_remote", CheckBlock(REMOTE_CONTROL), 1),
                new ActionState("check_scanner", CheckBlock(SCANNER_CAMERA), 1),
                new ActionState("check_truster", CheckBlock(ESCAPE_THRUSTER), 1),
                new ActionState("enable raytrasting", SCANNER_CAMERA,
                    action: camera_block => {
                        var camera =camera_block as IMyCameraBlock;
                        if(camera == null) return;
                        camera.EnableRaycast = true;
                    }, delay:1),
                new ActionState("thruster_override_off", ESCAPE_THRUSTER,
                    action: block => {
                        var thrust =block as IMyThrust;
                        if(thrust == null) return;
                        thrust.ThrustOverridePercentage = 0;
                    }, delay:1),
                new ActionState("dampeners_on", REMOTE_CONTROL,
                    action: block => {
                        var rc =block as IMyRemoteControl;
                        if(rc == null) return;
                        rc.DampenersOverride = true;
                    }, delay:1),
                EscapeChecksDone,
                new JumpState("SCAN", SCANNER_CAMERA,
                    block_measure: camera_block =>
                    {
                        var camera = camera_block as IMyCameraBlock;
                        if (camera == null) return Escape.Name;
                        while(camera.CanScan(SCAN_RANGE))
                        {
                            var gen = new System.Random();
                            float yaw = (float) (gen.NextDouble() * 90 - 45);
                            float pitch = (float) (gen.NextDouble() * 90 - 45);
                            var info = camera.Raycast(SCAN_RANGE, pitch, yaw);
                            if(info.Type == MyDetectedEntityType.LargeGrid ||
                               info.Type == MyDetectedEntityType.SmallGrid ||
                               info.Type == MyDetectedEntityType.CharacterHuman )
                            {
                                PrintToStateLcd("Found!\n");
                                return null;
                            }                        
                        }
                        PrintToStateLcd("Can not make a scan yet\n");
                        return "SCAN";
                    },
                    delay:1),
                new ActionState("thruster_override", ESCAPE_THRUSTER, action: block => {
                    var thrust =block as IMyThrust;
                        if(thrust == null) return;
                        thrust.ThrustOverridePercentage = 1;
                }, delay:1),
                Escape
            });
        }

        #endregion

            #region transition logic
        private List<NamedState> states = new List<NamedState>();
        public Program()
        {
            PrintToStateLcd("Initializing\n");
            states.Clear();
            SetUpIdleAndStop();
            SetupEscape();
            PrintToStateLcd("Initialized\n");
        }

        public void Main(string eventName)
        {
            if(string.IsNullOrEmpty(eventName))
            {
                PrintToStateLcd("Getting next state\n");
                eventName = GetNextStateName(this);
            }
            PrintToStateLcd("Executing '"+ eventName+"'\n");
            var state = GetState(eventName);
            if (state == null)
            {
                PrintToStateLcd("Could not find state. Going to IDLE.\n");
                Execute(Idle);
                return;
            }
            Execute(state);
            return;
        }

        private NamedState GetState(string state_name)
        {
            if (string.IsNullOrEmpty(state_name))
                return Idle;
            return states.Find(s => s.Name == state_name);
        }

        public void StartTimer(NamedState state)
        {
            if (state == null) return;
            SaveNextStateName(state.Name);

            var timer = GridTerminalSystem.GetBlockWithName(TIMER_NAME) as IMyTimerBlock;
            if (timer == null) return;
            timer.SetValueFloat("TriggerDelay", (float)state.Delay);
            timer.ApplyAction("Start");
        }

        public void Execute(NamedState state)
        {
            if (state == null) return;
            var lcd = GridTerminalSystem.GetBlockWithName(LCD_NAME) as IMyTextPanel;
            lcd?.WritePublicText(">" + state.Name + "\n", true);
            state.Invoke(this, state);
            if (state == Stop)
                return;
            if( state is JumpState)
            {
                var next = GetState((state as JumpState).GetTarget());
                StartTimer(next);
            }
            else
            {
                var next = GetState(state.NextName);
                StartTimer(next);
            }
        }

        void Register(IEnumerable<NamedState> registered_states)
        {
            NamedState last = null;
            foreach (var state in registered_states.Reverse())
            {
                if (last != null)
                {
                    PrintToStateLcd("Registering: '" + state.Name + "' -> '" + last.Name + "'\n");
                    state.NextName = last.Name;
                    states.Add(state);
                }
                last = state;
            }
        }

        #endregion

        #region state persistence
        public void SaveNextStateName(string text)
        {
            var lcd = GridTerminalSystem.GetBlockWithName(LCD_NAME) as IMyTextPanel;
            if (lcd == null) return;
            lcd.WritePublicTitle(text);
            lcd.WritePublicText("\nsaving next state: '" + text + "'\n", true);
        }

        public static string GetNextStateName(MyGridProgram env)
        {
            var lcd = env.GridTerminalSystem.GetBlockWithName(LCD_NAME) as IMyTextPanel;
            if (lcd == null) return null;
            return lcd.GetPublicTitle();
        }
        #endregion
        #region convenience  functions
        private StringBuilder sb = new StringBuilder();

        void PrintToStateLcd(string text, string lcd_name = LCD_NAME)
        {
            var lcd = this.GridTerminalSystem.GetBlockWithName(lcd_name) as IMyTextPanel;
            lcd?.WritePublicText(text, true);
            lcd?.ShowPublicTextOnScreen();
        }

        BlockAction CheckBlock(string name)
        {
            Action<IMyTerminalBlock> action = block =>
            {
                PrintToStateLcd(" '" + name + "' FOUND:\n" + block.DetailedInfo);
            };
            return new BlockAction(name, action);
        }
        GroupAction CheckGroup(string name)
        {
            Action<List<IMyTerminalBlock>> action = blocks =>
            {
                PrintToStateLcd(" '" + name + "' FOUND:\n" + blocks.Count);
            };
            return new GroupAction(name, action);
        }
        #endregion
    }
    #region action and state classes

    public abstract class NamedAction
    {
        abstract public string Name { get; }
    }

    public abstract class JumpAction : NamedAction
    {
        abstract public string Invoke(MyGridProgram env, NamedState state);
    }

    public abstract class ManipulationAction : NamedAction
    {
        abstract public void Invoke(MyGridProgram env, NamedState state);
    }

    public class GroupAction : ManipulationAction
    {
        override public string Name { get; }
        private Action<List<IMyTerminalBlock>> _action { get; }
        override public void Invoke(MyGridProgram env, NamedState state) => env
            .GridTerminalSystem
            .GetBlockGroupWithName(Name)
            ?.Apply(_action);

        public GroupAction(
            string group_name,
            Action<List<IMyTerminalBlock>> action)
        {
            Name = group_name;
            _action = action;
        }
    }

    public class BlockAction : ManipulationAction
    {
        override public string Name { get; }
        private Action<IMyTerminalBlock> _action { get; }
        override public void Invoke(MyGridProgram env, NamedState state) => env
            .GridTerminalSystem
            .GetBlockWithName(Name)
            ?.Apply(_action);

        public BlockAction(
            string block_name,
            Action<IMyTerminalBlock> action)
        {
            Name = block_name;
            _action = action;
        }
    }

    public class BlockMeasureAction : JumpAction
    {
        override public string Name { get; }
        private Func<IMyTerminalBlock, string> _func { get; }
        override public string Invoke(MyGridProgram env, NamedState state) => env
            .GridTerminalSystem
            .GetBlockWithName(Name)
            ?.Apply(_func);

        public BlockMeasureAction(
            string block_name,
            Func<IMyTerminalBlock, string> func)
        {
            Name = block_name;
            _func = func;
        }
    }

    public class NamedState
    {
        public double Delay { get; }
        public string NextName { get; set; }
        public string Name { get; }
        public NamedState(
           string name,
           double delay,
           string next = null)
        {
            Delay = delay;
            Name = name;
            NextName = next;
        }

        public string to_str() =>
            "'" + Name  +
            (NextName != null ? (" -> " + "'" + NextName + "'") : "");

        internal virtual void Invoke(Program program, NamedState state) { }
    }

    public class ActionState : NamedState
    {
        public NamedAction NamedAction { get; }
        public ActionState(
          string name,
          NamedAction action,
          double delay,
          string next = null):base(name, delay, next)
        {
            NamedAction = action;
        }

        public ActionState(
            string name,
            string block_name,
            Action<IMyTerminalBlock> action,
            double delay,
            string next = null) :
            this(name, new BlockAction(block_name, action), delay, next)
        { }

        public ActionState(
            string name,
            string block_name,
            Action<List<IMyTerminalBlock>> group_action,
            double delay,
            string next = null) :
            this(name, new GroupAction(block_name, group_action), delay, next)
        { }
        internal override void Invoke(Program program, NamedState state)
            => (this.NamedAction as ManipulationAction)?.Invoke(program, state);
    }

    public class JumpState : NamedState
    {
        private string _target = null;

        public JumpAction JumpAction { get; }

        public JumpState(
            string name, 
            JumpAction jump_action, 
            double delay,
            string next = null) : base(name, delay, next)
        {
            JumpAction = jump_action;
        }
        public JumpState(
            string name,
            string block_name,
            Func<IMyTerminalBlock, string> block_measure,
            double delay,
            string next = null) : base(name, delay, next)
        {
            JumpAction = new BlockMeasureAction(block_name, block_measure);
        }

        internal override void Invoke(Program program, NamedState state)
        {
            _target = (this.JumpAction as JumpAction)?.Invoke(program, state);
        }

        internal string GetTarget()
        {
            var target = _target;
            _target = null;
            return target ?? NextName;
        }
    }

    #endregion
    #region convenience extensions
    public static class Ext
    {
        public static void Apply(this IMyTerminalBlock block, Action<IMyTerminalBlock> action)
            => action(block);

        public static string Apply(this IMyTerminalBlock block, Func<IMyTerminalBlock, string> func)
           => func(block);

        public static void Apply(this IMyBlockGroup group, Action<List<IMyTerminalBlock>> action)
        {
            var blocks = new List<IMyTerminalBlock>();
            group.GetBlocks(blocks);

            action(blocks);
        }
  
        #endregion
        #endregion
    }// Omit this last closing brace as the game will add it back in
}
