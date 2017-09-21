using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoveTest
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


        // what an opportunity to refactor the code ..

        readonly BlockAction TimeAction = new BlockAction(
            LCD_NAME, lcd => (lcd as IMyTextPanel)
            ?.WritePublicText(" " + DateTime.UtcNow.ToLongTimeString()));

        List<BlockState> states;

        public Program()
        {
            states = new List<BlockState>();
            //state entry points
            var idle = new BlockState(IDLE_STATE_NAME, null, 5.0);

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