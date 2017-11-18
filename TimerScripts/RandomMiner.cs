using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomMiner
{
    class Program : MyGridProgram
    {
        #region copymeto programmable block
        /* Copyright 2017 Pauli Rikula (MIT https://opensource.org/licenses/MIT)
         * see https://github.com/kummahiih/SpaceEngineersScripts/blob/master/
         * The state transition timer '<TIMER_NAME>' and the state persistence lcd '<LCD_NAME>'
         * are are critical for the functionality of the program and they had to be set up 
         * manually before any diagnostics can be done.
         * 
         * Set the '<TIMER_NAME>' timer to run the programmable block with empty argument
         * and set '<LCD_NAME>' to show public text on the screen.
         * Run the programmable block with the state name as a parameter. 
         * For diagnostics run 'IDLE'.
         * 
         * <description> (<state name>) ...
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

        #region idle loop and stop

        const string TIMER_NAME = "STATE TIMER";
        const string LCD_NAME = "STATE LCD";
        const string IDLE_STATE_NAME = "IDLE";

        readonly BlockAction StopAction = new BlockAction(
           TIMER_NAME, timer => (timer as IMyTimerBlock)
           ?.ApplyAction("Stop"));

        readonly BlockAction TimeAction = new BlockAction(
            LCD_NAME, lcd => (lcd as IMyTextPanel)
            ?.WritePublicText(" " + DateTime.UtcNow.ToLongTimeString()));
 
        readonly BlockAction ClearLCDAction = new BlockAction(
           LCD_NAME, lcd => (lcd as IMyTextPanel)
           ?.WritePublicText("", append: false));
        NamedState stop = new NamedState("STOP", null, 1);

        void SetUpIdleAndStop()
        {
            states.Add(stop);
            //state entry points   
            var idle = new NamedState(IDLE_STATE_NAME, ClearLCDAction, 5.0);

            //idle loop   
            states.SetUpSequence(new[] {
            idle,
                new NamedState("time", TimeAction, 5.0),
                new NamedState( "blocks", LCD_NAME,
                action:lcd => {
                    sb.Clear();
                    foreach(var name in states
                        .Where(s => s.NamedAction  is BlockAction )
                        .Select(s => s.NamedAction?.Name)
                        .Distinct())
                    {
                        var found_text =
                            this.GridTerminalSystem.GetBlockWithName(name) == null ? "" : " FOUND";
                        sb.Append("'" + name + "'" + found_text+"\n");
                    }
                    (lcd as IMyTextPanel)?.WritePublicText(sb.ToString(), append:false);
                },
                delay:5.0),
                new NamedState( "groups", LCD_NAME,
                action:lcd => {
                    sb.Clear();
                    foreach(var name in states
                        .Where(s => s.NamedAction  is GroupAction )
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

            new NamedState( "ls", LCD_NAME,
                action:lcd => (lcd as IMyTextPanel)?.WritePublicText(
                        string.Join(",\n", states.Select(s => s.to_str())) + "\n"),
                delay:5.0),
            idle});
        }
        #endregion

        #region mining
        const string REMOTE_CONTROL = "REMOTE CONTROL";

        private void StorePointToRemote(
            IMyRemoteControl rc, VRageMath.Vector3D target, string name)
        {
            PrintToStateLcd(target.AsGPS(name));
            rc.AddWaypoint(target, name);
        }

        private static VRageMath.Vector3D GetForwardUV(IMyRemoteControl rc) 
        {
            var rc_v_f = rc.WorldMatrix.GetOrientation().Forward;
            rc_v_f = rc_v_f / rc_v_f.Length();
            return rc_v_f;
        }


        void SetupPoints()
        {
            states.SetUpSequence(new[] {
                new NamedState("CROSS100", ClearLCDAction, 0.1),
                new NamedState("check_remote_control", CheckBlock(REMOTE_CONTROL), 0.1),
                new NamedState( "do_cross_points", REMOTE_CONTROL, action:rc_block =>
                {
                    var rc = rc_block as IMyRemoteControl;
                    if(rc == null) return;

                    var pos = rc.GetPosition();

                    var rc_v_f = GetForwardUV(rc);

                    var rc_v_l = rc.WorldMatrix.GetOrientation().Left;
                    rc_v_l = rc_v_l / rc_v_l.Length();

                    var rc_v_d = rc.WorldMatrix.GetOrientation().Down;
                    rc_v_d = rc_v_d / rc_v_d.Length();

                    rc.ClearWaypoints();
                    var entrance = pos;
                    StorePointToRemote(rc,entrance,"entrance");
                    var bottom = entrance + rc_v_d * 2*100;
                    StorePointToRemote(rc,bottom,"bottom");
                    var center = entrance + rc_v_d * 2*50;
                    StorePointToRemote(rc,center,"center");
                    var rear = center -  rc_v_f * 2*50;
                    StorePointToRemote(rc,rear,"rear");
                    var front = center +  rc_v_f * 2*50;
                    StorePointToRemote(rc,front,"front");
                    StorePointToRemote(rc,center,"center");
                    var left = center +  rc_v_l * 2*50;
                    StorePointToRemote(rc, left, "left");
                    var right = center -  rc_v_l * 2*50;
                    StorePointToRemote(rc, right, "right");
                    StorePointToRemote(rc,center,"center");

                },delay:1.0),
                stop
            });

            states.SetUpSequence(new[] {
                new NamedState("LINE100", ClearLCDAction, 0.1),
                new NamedState( "do_line_points", REMOTE_CONTROL, action:rc_block =>
                {
                     var rc = rc_block as IMyRemoteControl;
                     if(rc == null) return;

                     var pos = rc.GetPosition();
                     var rc_v_f = GetForwardUV(rc);

                     rc.ClearWaypoints();
                     StorePointToRemote(rc,pos,"start");
                     StorePointToRemote(rc,pos+100*rc_v_f,"end");

                },delay:1.0),
                stop
            });

            states.SetUpSequence(new[] {
                new NamedState("20x20", ClearLCDAction, 0.1),
                new NamedState( "do_line_points", REMOTE_CONTROL, action:rc_block =>
                {
                     var rc = rc_block as IMyRemoteControl;
                     if(rc == null) return;

                     var rc_v_f = GetForwardUV(rc);

                     var rc_v_l = rc.WorldMatrix.GetOrientation().Left;
                     rc_v_l = rc_v_l / rc_v_l.Length();

                     rc.ClearWaypoints();
                     var pos = rc.GetPosition();
                     
                     StorePointToRemote(rc,pos,"start");
                     for(int i=0;i<10;i++)
                     {
                        StorePointToRemote(rc,pos, "p"+i.ToString());
                        StorePointToRemote(rc,pos+rc_v_f*20, "t"+i.ToString());
                        pos += rc_v_l*2;
                     }
                },delay:1.0),
                stop
            });

        }




        #endregion

        #region transition logic
        private List<NamedState> states = new List<NamedState>();
        public Program()
        {
            states.Clear();
            SetUpIdleAndStop();
            SetupPoints();
        }

        public void Main(string eventName)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                var state_name = GetString(this);
                if (string.IsNullOrEmpty(state_name))
                    state_name = IDLE_STATE_NAME;
                states.ForEach(state => Execute(state, state_name));
                return;
            }
            states.ForEach(state => StartTimer(state, eventName));
        }

        public void StartTimer(NamedState state, string name = null)
        {
            if (state == null || name != null && state.Name != name) return;
            SaveString(state.Name);

            var timer = GridTerminalSystem.GetBlockWithName(TIMER_NAME) as IMyTimerBlock;
            if (timer == null) return;
            timer.SetValueFloat("TriggerDelay", (float)state.Delay);
            timer.ApplyAction("Start");
        }

        public void Execute(NamedState state, string name)
        {
            if (state == null || state.Name != name) return;
            var lcd = GridTerminalSystem.GetBlockWithName(LCD_NAME) as IMyTextPanel;
            lcd?.WritePublicText(">" + state.Name, true);
            state.NamedAction?.Invoke(this);
            StartTimer(state.Next);
        }
        #endregion

        #region state persistence
        public void SaveString(string text)
        {
            var lcd = GridTerminalSystem.GetBlockWithName(LCD_NAME) as IMyTextPanel;
            if (lcd == null) return;
            lcd.WritePublicTitle(text);
            lcd.WritePublicText(" -> " + text + "\n", true);
        }        

        public static string GetString(MyGridProgram env)
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

        #endregion
    }
    #region action and state classes
    public class GroupAction : NamedAction
    {
        override public string Name { get; }
        private Action<List<IMyTerminalBlock>> _action { get; }
        override public void Invoke(MyGridProgram env) => env
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


    public class BlockAction : NamedAction
    {
        override public string Name { get; }
        private Action<IMyTerminalBlock> _action { get; }
        override public void Invoke(MyGridProgram env) => env
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

    public abstract class NamedAction
    {
        abstract public string Name { get; }
        abstract public void Invoke(MyGridProgram env);
    }

    public class NamedState
    {
        public NamedAction NamedAction { get; }
        public double Delay { get; }
        public NamedState Next { get; set; }
        public string Name { get; }

        public NamedState(
            string name,
            string block_name,
            Action<IMyTerminalBlock> action,
            double delay,
            NamedState next = null) :
            this(name, new BlockAction(block_name, action), delay, next)
        { }

        public NamedState(
            string name,
            string block_name,
            Action<List<IMyTerminalBlock>> group_action,
            double delay,
            NamedState next = null) :
            this(name, new GroupAction(block_name, group_action), delay, next)
        { }

        public NamedState(
            string name,
            NamedAction block_action,
            double delay,
            NamedState next = null)
        {
            NamedAction = block_action;
            Delay = delay;
            Name = name;
            Next = next;
        }

        public string to_str() =>
            "'" + Name + "':'" + NamedAction?.Name + "'" +
            (Next != null ? (" -> " + "'" + Next.Name + "'") : "");
    }
    #endregion
    #region convenience extensions
    public static class Ext
    {
        public static string AsGPS(this VRageMath.Vector3D position, string name)
         => String.Format(
             "GPS:{0}:{1:0.00}:{2:0.00}:{3:0.00}:\n",
             name, position.X, position.Y, position.Z);

        public static void Apply(this IMyTerminalBlock block, Action<IMyTerminalBlock> action)
            => action(block);

        public static void Apply(this IMyBlockGroup group, Action<List<IMyTerminalBlock>> action)
        {
            var blocks = new List<IMyTerminalBlock>();
            group.GetBlocks(blocks);

            action(blocks);
        }

        public static List<NamedState> SetUpSequence(this List<NamedState> states,
            IEnumerable<NamedState> registered_states)
        {
            NamedState last = null;
            foreach (var state in registered_states.Reverse())
            {
                if (last != null)
                {
                    state.Next = last;
                    states.Add(state);
                }
                last = state;
            }
            return states;
        }
        #endregion
        #endregion
    }// Omit this last closing brace as the game will add it back in
}
