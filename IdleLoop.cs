using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleLoop
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

        // what an opportunity to refactor the code ..   
        readonly BlockAction ClearLCDAction = new BlockAction(
           LCD_NAME, lcd => (lcd as IMyTextPanel)
           ?.WritePublicText("", append: false));

        List<NamedState> states;

        public Program()
        {
            states = new List<NamedState>();
            //state entry points   
            var idle = new NamedState(IDLE_STATE_NAME, ClearLCDAction, 5.0);



            //idle loop   
            states.SetUpSequence(new[] {
            idle,
                new NamedState("time", TimeAction, 5.0),
                new NamedState( "blocks", LCD_NAME,
                action:lcd => {
                    (lcd as IMyTextPanel)?.WritePublicText("");
                    var statenames = states
                        .Where(s => s.NamedAction  is BlockAction )
                        .Select(s => s.NamedAction?.Name)
                        .Concat(new[] { TIMER_NAME })
                        .Distinct();
                    foreach (var name in statenames)
                    {
                        var found_text =
                            this.GridTerminalSystem.GetBlockWithName(name) == null ? "\n" : " FOUND\n";
                        (lcd as IMyTextPanel)
                            ?.WritePublicText("'" + name + "'" + found_text, true);
                    }},
                delay:5.0),
                new NamedState( "groups", LCD_NAME,
                action:lcd => {
                    (lcd as IMyTextPanel)?.WritePublicText("");
                    var statenames = states
                        .Where(s => s.NamedAction  is GroupAction )
                        .Select(s => s.NamedAction?.Name)
                        .Distinct();
                    foreach (var name in statenames)
                    {
                        var found_text =
                            this.GridTerminalSystem.GetBlockGroupWithName(name) == null ? "\n" : " FOUND\n";
                        (lcd as IMyTextPanel) ?.WritePublicText("'" + name + "'" + found_text, true);
                    }},
                delay:5.0),

            new NamedState( "ls", LCD_NAME,
                action:lcd => (lcd as IMyTextPanel)?.WritePublicText(
                        string.Join(",\n", states.Select(s => s.to_str())) + "\n"),
                delay:5.0),
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

    public static class Ext
    {
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


    }// Omit this last closing brace as the game will add it back in
}
