using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PositionTest
{
    class Program : MyGridProgram
    {
        #region copymeto programmable block 
        //see https://github.com/kummahiih/SpaceEngineersScripts/blob/master/ .. somewhere 

        //run the programmable block with the state name as a parameter 

        /*
        idle loop:
         time(idle)
         list all named blocks(blocks)
         list all registered states(ls)

        */



        const string TIMER_NAME = "STATE TIMER";
        const string LCD_NAME = "STATE LCD";
        const string IDLE_STATE_NAME = "IDLE";

        const string CAMERA_1_NAME = "CAMERA 1";
        const string REMOTE_1_NAME = "REMOTE 1";

        const string CAMERA_2_NAME = "CAMERA 2";
        const string REMOTE_2_NAME = "REMOTE 2";

        


        BlockAction CheckTarget(string name)
        {
            Action<IMyTerminalBlock> action = block =>
            {
                var lcd_block = this.GridTerminalSystem.GetBlockWithName(LCD_NAME);
                var lcd = (lcd_block as IMyTextPanel);                
                lcd?.WritePublicText(" '"+name + "' FOUND:\n" + block.DetailedInfo, append: true);
            };
            return new BlockAction(name, action);
        }

       
        // what an opportunity to refactor the code ..
        readonly BlockAction ClearLCDAction = new BlockAction(
           LCD_NAME, lcd => (lcd as IMyTextPanel)
           ?.WritePublicText("",append:false ));


        List<BlockState> states;

        public Program()
        {
            states = new List<BlockState>();
            //state entry points
            var idle = new BlockState(IDLE_STATE_NAME, null, 5.0);

            var target = new BlockState("TARGET", ClearLCDAction, 0.1);

            //idle loop
            states.SetUpSequence(new[] {
            target,
                new BlockState("check_timer", CheckTarget(TIMER_NAME),0.1),
                new BlockState("check_lcd", CheckTarget(LCD_NAME),0.1),
                new BlockState("check_camera_1", CheckTarget(CAMERA_1_NAME),0.1),
                new BlockState("check_remote_1", CheckTarget(REMOTE_1_NAME),0.1),
                new BlockState("check_camera_2", CheckTarget(CAMERA_2_NAME),0.1),
                new BlockState("check_remote_2", CheckTarget(REMOTE_2_NAME),0.1),
                new BlockState("clear_for_gps", ClearLCDAction, 0.1),
                new BlockState("get_gps", LCD_NAME, lcd =>
                {
                    (lcd as IMyTextPanel)?.WritePublicText("targeting\n", append:false);
                    var camera_1 = this.GridTerminalSystem.GetBlockWithName(CAMERA_1_NAME);
                    var remote_1 = this.GridTerminalSystem.GetBlockWithName(REMOTE_1_NAME);
                    if (camera_1 == null || remote_1 == null) return;

                    var pos_1 = camera_1.GetPosition();
                    var direction_1 = pos_1 - remote_1.GetPosition();

                    var camera_2 = this.GridTerminalSystem.GetBlockWithName(CAMERA_2_NAME);
                    var remote_2 = this.GridTerminalSystem.GetBlockWithName(REMOTE_2_NAME);
                    if (camera_2 == null || remote_2 == null) return;

                    var pos_2 = camera_2.GetPosition();
                    var direction_2 = pos_2  - remote_2.GetPosition();
                    double closest_dist = -1;
                    var closest_point = pos_1;
                    foreach(var multiplier in new[] {
                        11.0, 23.0,
                        41.0,139.0, // 20
                        157,241.0,  
                        383.0,701,  // 40
                        1109,1871,
                        3697,4111,  // 60
                        5051,8009,
                        13457,24121,  //80
                        30529, 41047,
                        51383, 67211, //100
                        92333, 104383 //120
                    }){
                        for(int i=1;i <= 5;i++)
                        {
                            var p_1 = pos_1 + direction_1 * i*multiplier;
                            var p_2 = pos_2 + direction_2 * i*multiplier;

                            var dist = VRageMath.Vector3D.Distance(p_1, p_2);
                            
                            (lcd as IMyTextPanel)?.WritePublicText(dist.ToString("f2")+"\n", append:true);

                            if(closest_dist<0 || dist <= closest_dist )
                            {
                                closest_dist = dist;
                                closest_point = p_1;
                            }
                        }
                    }
                    (lcd as IMyTextPanel)?.WritePublicText("\n"+closest_point.AsGPS("target"), append:true);
                },
                0.1),
                new BlockState("stop", null,null,0.1)
            });

            states.SetUpSequence(new[] {
                idle,
                new BlockState("time", LCD_NAME, lcd =>
                    (lcd as IMyTextPanel)?.WritePublicText(" " + DateTime.UtcNow.ToLongTimeString()),5.0),
                new BlockState("blocks", LCD_NAME,
                lcd => {
                    (lcd as IMyTextPanel)?.WritePublicText("");
                    var names = states.Select(s => s.BlockAction?.BlockName)
                        .Where(x => x != null)
                        .Concat(new[] { TIMER_NAME })
                        .Distinct();
                    foreach (var name in names)
                    {
                        var found_text =
                            this.GridTerminalSystem.GetBlockWithName(name) == null ? "\n" : " FOUND\n";
                        (lcd as IMyTextPanel)
                            ?.WritePublicText("'" + name + "'" + found_text, true);
                    }
                },
                5.0),
                new BlockState("ls", LCD_NAME,
                lcd => (lcd as IMyTextPanel)?.WritePublicText(
                        string.Join(",\n", states.Select(s => s.to_str())) + "\n"),
                5.0),
                idle
            });
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

        public void StartTimer(BlockState state, string name = null)
        {
            if (state == null || name != null && state.Name != name) return;
            SaveString(state.Name);

            var timer = GridTerminalSystem.GetBlockWithName(TIMER_NAME) as IMyTimerBlock;
            if (timer == null) return;
            timer.SetValueFloat("TriggerDelay", (float)state.Delay);
            timer.ApplyAction("Start");
        }

        public void Execute(BlockState state, string name)
        {
            if (state == null || state.Name != name) return;
            var lcd = GridTerminalSystem.GetBlockWithName(LCD_NAME) as IMyTextPanel;
            lcd?.WritePublicText(">" + state.Name, true);
            state.BlockAction?.Invoke(this);
            StartTimer(state.Next);
        }

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
    }

    public class BlockAction
    {
        public string BlockName { get; }
        private Action<IMyTerminalBlock> _action { get; }
        public void Invoke(MyGridProgram env) => env
            .GridTerminalSystem
            .GetBlockWithName(BlockName)
            ?.Apply(_action);

        public BlockAction(
            string block_name,
            Action<IMyTerminalBlock> action)
        {
            BlockName = block_name;
            _action = action;
        }
    }
    public class BlockState
    {
        public BlockAction BlockAction { get; }
        public double Delay { get; }
        public BlockState Next { get; set; }
        public string Name { get; }

        public BlockState(
            string name,
            string block_name,
            Action<IMyTerminalBlock> action,
            double delay,
            BlockState next = null) :
            this(name, new BlockAction(block_name, action), delay, next)
        { }


        public BlockState(
            string name,
            BlockAction block_action,
            double delay,
            BlockState next = null)
        {
            BlockAction = block_action;
            Delay = delay;
            Name = name;
            Next = next;
        }

        public string to_str() =>
            "'" + Name + "':'" + BlockAction?.BlockName + "'" +
            (Next != null ? (" -> " + "'" + Next.Name + "'") : "");
    }

    public static class Ext
    {
        public static void Apply(this IMyTerminalBlock block, Action<IMyTerminalBlock> action)
            => action(block);

        public static string AsGPS(this VRageMath.Vector3D position, string name)
            => String.Format(
                "GPS:{0}:{1:0.00}:{2:0.00}:{3:0.00}:\n",
                name, position.X, position.Y, position.Z);

        public static List<BlockState> SetUpSequence(this List<BlockState> states,
            IEnumerable<BlockState> registered_states)
        {
            BlockState last = null;
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


    }// Omit this last closing brace as the game will add it back in
}

