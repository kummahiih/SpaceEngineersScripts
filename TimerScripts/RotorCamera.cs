using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RotorCamera
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

        #region state setups
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
                    PrintToStateLcd($"continuing to '{next}'\n");
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
                        sb.Append($"'{name}' {found_text}\n");
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
                        sb.Append($"'{ name }' {found_text}\n");
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

        #region rotor cameras

        const string ROT_CAMERA_NAME = "rotor raycast camera";
        const string ROT_LCD_NAME = "rotor raycast LCD";
        const string JUMP_DRIVE_NAME = "jump drive";
        const float SCAN_RANGE = 15000;

        NamedState RayCastLoop;
        NamedState Scan;
        NamedState ScanDistance;
        NamedState ScanAndJump;

        public void SetupRotorCameras()
        {
            var check_camera = CheckBlock(ROT_CAMERA_NAME);
            var check_lcd = CheckBlock(ROT_CAMERA_NAME);

            var enable_raytracing = new BlockAction(ROT_CAMERA_NAME, action: camera_block => {
                var camera = camera_block.CastOrRaise<IMyCameraBlock>();
                camera.EnableRaycast = true;
            });

            RayCastLoop = new ActionState("RAYCAST", ClearLCDAction, 1);
            Scan = new ActionState("SCAN", ClearLCDAction, 1);
            Register(new[] {
                RayCastLoop,
                new ActionState(check_camera, 1),
                new ActionState(check_lcd, 1),
                new ActionState(enable_raytracing, 1),
                Scan,
                new JumpState("raycast_loop", ROT_CAMERA_NAME,
                    block_measure: camera_block =>
                    {
                        var camera = camera_block.CastOrRaise<IMyCameraBlock>();
                        if (!camera.CanScan(SCAN_RANGE))
                        {
                            PrintToStateLcd("Can not make a scan yet\n");
                            return "SCAN";
                        }

                        var info = camera.Raycast(SCAN_RANGE,0,0);
                        if(info.IsEmpty())
                        {
                            PrintToStateLcd("Found nothing\n");
                            return "SCAN";
                        }
                        sb.Clear();
                        sb.Append(DateTime.Now.ToLongDateString());
                        sb.AppendLine();
                        sb.Append(info.Position.AsGPS("ROT SCAN CENTER"));
                        sb.Append(info.HitPosition.Value.AsGPS("ROT SCAN"));
                        sb.AppendLine();
                        sb.Append(info.Name);
                        sb.AppendLine();

                        PrintToStateLcd(sb.ToString(),ROT_LCD_NAME, false);
                        return "SCAN";
                    }, delay:1),
                RayCastLoop
            });

            ScanDistance = new ActionState("SCANDIST", ClearLCDAction, 1);
            Register(new[] {
                ScanDistance,
                 new ActionState("reset raytrasting 1", ROT_CAMERA_NAME,
                    action: camera_block => {
                        var camera = camera_block.CastOrRaise<IMyCameraBlock>();
                        camera.EnableRaycast = false;
                    }, delay:1),
                new ActionState("reset raytrasting 2", ROT_CAMERA_NAME,
                    action: camera_block => {
                        var camera = camera_block.CastOrRaise<IMyCameraBlock>();
                        camera.EnableRaycast = true;
                    }, delay:1),

                new  JumpState("check_scan", ROT_CAMERA_NAME, block_measure: camera_block => {
                    var camera = camera_block.CastOrRaise<IMyCameraBlock>();
                    float dist = 1;
                    while(camera.CanScan(dist)) {dist+=100;}
                    PrintToStateLcd($"max distance {dist.ToString()}");
                    return  "check_scan";
                }, delay:1 ),

                ScanDistance
            });

            ScanAndJump = new ActionState("SCANJUMP", ClearLCDAction, 1);

            Register(new[] {
                ScanAndJump,
                new ActionState(CheckBlock(JUMP_DRIVE_NAME), 1),
                new ActionState(check_camera, 1),
                new ActionState(check_lcd, 1),
                new ActionState(enable_raytracing, 1),
                 new JumpState("clear_jump", JUMP_DRIVE_NAME, block_measure: jump_block => {
                    var jd = jump_block.CastOrRaise<IMyJumpDrive>();
                    var list = new List<ITerminalAction>();
                     jd.GetActions(list);
                     foreach(var ac in  list)
                     {
                         PrintToStateLcd($"{ac.Name}: {ac.ToString()}\n");
                     }

                     var plist = new List<ITerminalProperty>();
                     jd.GetProperties(plist);

                     foreach(var prop in  plist)
                     {
                         PrintToStateLcd($"{prop.Id} {prop.TypeName}: {prop.ToString()} \n");
                     }

                    return null;
                },delay:1),
                ScanAndJump,


            });

        }
        #endregion

        private List<NamedState> states = new List<NamedState>();
        public Program()
        {
            PrintToStateLcd("Initializing\n");
            states.Clear();
            SetUpIdleAndStop();
            SetupRotorCameras();
            PrintToStateLcd("Initialized\n");
        }

        void Register(IEnumerable<NamedState> registered_states)
        {
            NamedState last = null;
            foreach (var state in registered_states.Reverse())
            {
                if (last != null)
                {
                    PrintToStateLcd($"Registering: '{state.Name}' -> '{ last.Name}'\n");
                    state.NextName = last.Name;
                    states.Add(state);
                }
                last = state;
            }
        }

        #endregion

        #region transition logic
        public void Main(string eventName)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                PrintToStateLcd("Getting next state\n");
                eventName = GetNextStateName(this);
            }
            PrintToStateLcd($"Executing '{ eventName }'\n");
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
            PrintToStateLcd($"> {state.Name}\n");
            try
            {
                state.Invoke(this, state);
            }
            catch (SomethingWrongException)
            {
                PrintToStateLcd($"> something went wrong and exception handlind works now btw.\n");
            }
            if (state == Stop)
                return;
            if (state is JumpState)
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
        #endregion

        #region state persistence
        public void SaveNextStateName(string text)
        {
            var lcd = GridTerminalSystem.GetBlockWithName(LCD_NAME) as IMyTextPanel;
            if (lcd == null) return;
            lcd.WritePublicTitle(text);
            lcd.WritePublicText($"\nsaving next state: '{text}'\n", true);
        }

        public static string GetNextStateName(MyGridProgram env)
        {
            var lcd = env.GridTerminalSystem.GetBlockWithName(LCD_NAME) as IMyTextPanel;
            if (lcd == null) return null;
            return lcd.GetPublicTitle();
        }
        #endregion

        #region convience actions
        readonly BlockAction TimeAction = new BlockAction(
            LCD_NAME, lcd => (lcd as IMyTextPanel)
            ?.WritePublicText(
                DateTime.UtcNow.ToLongTimeString() + "\n"));

        readonly BlockAction ClearLCDAction = new BlockAction(
           LCD_NAME, lcd => (lcd as IMyTextPanel)
           ?.WritePublicText("", append: false));

        BlockAction CheckBlock(string name)
        {
            Action<IMyTerminalBlock> action = block =>
            {
                PrintToStateLcd($" '{name}' FOUND:\n{block.DetailedInfo}\n");
            };
            return new BlockAction(name, action);
        }

        GroupAction CheckGroup(string name)
        {
            Action<List<IMyTerminalBlock>> action = blocks =>
            {
                PrintToStateLcd($" '{name}' FOUND:{blocks.Count}\n");
            };
            return new GroupAction(name, action);
        }

        #endregion

        #region convenience  functions
        private StringBuilder sb = new StringBuilder();

        void PrintToStateLcd(string text, string lcd_name = LCD_NAME, bool append = true)
        {
            var lcd = this.GridTerminalSystem.GetBlockWithName(lcd_name) as IMyTextPanel;
            lcd?.WritePublicText(text, append);
            lcd?.ShowPublicTextOnScreen();
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

        static int stateCount = 0;

        public NamedState(
           double delay,
           string next = null):this($"nameless state {stateCount++}", delay, next)
        { }

        public string to_str() =>
            $"'{Name}'" +
                (NextName != null ? ($" -> '{ NextName}'") : "");

        internal virtual void Invoke(Program program, NamedState state) { }
    }

    public class ActionState : NamedState
    {
        public NamedAction NamedAction { get; }
        public ActionState(
          string name,
          NamedAction action,
          double delay,
          string next = null) : base(name, delay, next)
        {
            NamedAction = action;
        }

        public ActionState(
          NamedAction action,
          double delay,
          string next = null) : base(delay, next)
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

    // No worth of effort to inherit exceptions because they can not be catched
    public class SomethingWrongException : Exception
    {
        public SomethingWrongException() { }
    }
    #endregion

    #region convenience extensions
    public static class Ext
    {
        public static TBlock CastOrRaise<TBlock>(this IMyTerminalBlock block) where TBlock : class, IMyTerminalBlock
        {
            var casted_block = block as TBlock;
            if (casted_block == null)
                throw new SomethingWrongException();
            return casted_block;
        }

        public static string AsGPS(this VRageMath.Vector3D position, string name)
            => String.Format(
                "GPS:{0}:{1:0.00}:{2:0.00}:{3:0.00}:\n",
                name, position.X, position.Y, position.Z);

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
