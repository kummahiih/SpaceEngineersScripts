using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WelderMachine
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
        const string GRINDER_NAME = "Torpedo Grinder";


        const string WELDERGROUP = "Welders";
        const string GRAVITYGENS = "Launch Gravity";


        // what an opportunity to refactor the code ..  

        readonly BlockAction TimeAction = new BlockAction(
            LCD_NAME, lcd => (lcd as IMyTextPanel)
            ?.WritePublicText(" " + DateTime.UtcNow.ToLongTimeString()));

        BlockAction CheckTarget(string name)
        {
            Action<IMyTerminalBlock> action = block =>
            {
                var lcd_block = this.GridTerminalSystem.GetBlockWithName(LCD_NAME);
                var lcd = (lcd_block as IMyTextPanel);
                lcd?.WritePublicText(" '" + name + "' FOUND:\n" + block.DetailedInfo, append: true);
            };
            return new BlockAction(name, action);
        }

        // what an opportunity to refactor the code ..  
        readonly BlockAction ClearLCDAction = new BlockAction(
           LCD_NAME, lcd => (lcd as IMyTextPanel)
           ?.WritePublicText("", append: false));

        List<BlockState> states;

        public Program()
        {
            states = new List<BlockState>();
            //state entry points  
            var idle = new BlockState(IDLE_STATE_NAME, ClearLCDAction, 5.0);



            //idle loop  
            states.SetUpSequence(new[] {
            idle,
                new BlockState("time", TimeAction, 5.0),
                new BlockState( "blocks", LCD_NAME,
                lcd => {
                    (lcd as IMyTextPanel)?.WritePublicText("");
                    var statenames = states.Select(s => s.BlockAction?.BlockName)
                        .Concat(new[] { TIMER_NAME })
                        .Distinct();
                    foreach (var name in statenames)
                    {
                        var found_text =
                            this.GridTerminalSystem.GetBlockWithName(name) == null ? "\n" : " FOUND\n";
                        (lcd as IMyTextPanel)
                            ?.WritePublicText("'" + name + "'" + found_text, true);
                    }},
                5.0),

            new BlockState( "ls", LCD_NAME,
                lcd => (lcd as IMyTextPanel)?.WritePublicText(
                        string.Join(",\n", states.Select(s => s.to_str())) + "\n"),
                5.0),
            idle});


            var weld = new BlockState("WELD", ClearLCDAction, 5.0);

            //WELDERGROUP  
            //GRAVITYGENS  
            //GRINDER_NAME  
            states.SetUpSequence(new[] {
            weld,
            new BlockState( "welders_on", LCD_NAME,
                lcd => {
                    (lcd as IMyTextPanel)?.WritePublicText("", append:false);
                    var group = this.GridTerminalSystem.GetBlockGroupWithName(WELDERGROUP);
                    if(group == null)
                        return;
                    var blocks = new List<IMyTerminalBlock>();
                    group.GetBlocks(blocks);
                    foreach (var block in blocks)
                        block.ApplyAction("OnOff_On");
                },
                0.1),
                new BlockState( "welders_off", LCD_NAME,
                    lcd => {
                        (lcd as IMyTextPanel)?.WritePublicText("", append:false);
                        var group = this.GridTerminalSystem.GetBlockGroupWithName(WELDERGROUP);
                        if(group == null)
                            return;
                        var blocks = new List<IMyTerminalBlock>();
                        group.GetBlocks(blocks);

                        foreach (var block in blocks)
                            block.ApplyAction("OnOff_Off");
                    },
                 3),

                new BlockState("grinder_on", GRINDER_NAME,
                    grinder =>  grinder.ApplyAction("OnOff_On"),
                 0.1),
                 new BlockState("grinder_off", GRINDER_NAME,
                    grinder =>  grinder.ApplyAction("OnOff_Off"),
                 7),
                 new BlockState("gravity_on", LCD_NAME,
                    lcd => {
                        (lcd as IMyTextPanel)?.WritePublicText("", append: false);
                        var group = this.GridTerminalSystem.GetBlockGroupWithName(GRAVITYGENS);
                        if (group == null)
                            return;
                        var blocks = new List<IMyTerminalBlock>();
                        group.GetBlocks(blocks);

                        foreach (var block in blocks)
                            block.ApplyAction("OnOff_On");
                    },
                 0.1),
                 new BlockState("gravity_off", LCD_NAME,
                    lcd => {
                        (lcd as IMyTextPanel)?.WritePublicText("", append: false);
                        var group = this.GridTerminalSystem.GetBlockGroupWithName(GRAVITYGENS);
                        if (group == null)
                            return;
                        var blocks = new List<IMyTerminalBlock>();
                        group.GetBlocks(blocks);

                        foreach (var block in blocks)
                            block.ApplyAction("OnOff_Off");
                    },
                 2),
            idle });
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
