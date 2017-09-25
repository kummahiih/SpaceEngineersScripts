using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanderStateMAchine
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

        close door sequence:
         Landing light off(CLOSE)
         Close door
         Ogygen generator off
         Oxygen tank on
         Depressurize off
         Oxygen tank off
         Ogygen generator on
         idle

        detach sequence:        
         Gyroscope on(DETACH)
         Landing gear autolock off and unlock
         Dampeners off
         Ogygen generator on
         Thrust ++
         Thrust --
         hatch on
         Landing gear autolock on
         Close the door(full sequence)


        open door sequence:
         Landing light on(OPEN)
         Ogygen generator off
         Oxygen tank on
         Depressurize on
         open the door
         Oxygen tank off
         Ogygen generator on
         idle

        attach sequence:
         set the landin gear(ATTACH)
         gyro off
         hatch off
         open door(full sequence)
        */

        const string TIMER_NAME = "STATE TIMER";
        const string LCD_NAME = "STATE LCD";
        const string IDLE_STATE_NAME = "IDLE";

        const string AIR_VENT_NAME = "AIR VENT";
        const string DOOR_NAME = "DOOR";
        const string GEAR_NAME = "GEAR";
        const string LIGHT_BACK = "LIGHT";

        const string CONTROL_STAT = "CONTROL";
        const string GYRO = "GYRO";
        const string THRUST = "THRUST";
        const string HATCH = "HATCH";

        const string O2TANK = "O2TANK";
        const string O2GEN = "O2GEN";

        // what an opportunity to refactor the code ..


        readonly BlockAction TimeAction = new BlockAction(
            LCD_NAME, lcd => (lcd as IMyTextPanel)
            ?.WritePublicText(" " + DateTime.UtcNow.ToLongTimeString()));

        BlockAction CheckBlock(string name)
        {
            Action<IMyTerminalBlock> action = block =>
            {
                PrintToStateLcd(" '" + name + "' FOUND:\n" + block.DetailedInfo);
            };
            return new BlockAction(name, action);
        }
        readonly BlockAction ClearLCDAction = new BlockAction(
           LCD_NAME, lcd => (lcd as IMyTextPanel)
           ?.WritePublicText("", append: false));

        void PrintToStateLcd(string text, string lcd_name = LCD_NAME)
        {
            var lcd = this.GridTerminalSystem.GetBlockWithName(lcd_name) as IMyTextPanel;
            lcd?.WritePublicText(text, true);
            lcd?.ShowPublicTextOnScreen();
        }

        readonly BlockAction O2GenOnAction = new BlockAction(
        O2GEN, gen => gen?.ApplyAction("OnOff_On"));

        readonly BlockAction O2TankOffAction = new BlockAction(
            O2TANK, tank => tank?.ApplyAction("OnOff_Off"));

        readonly BlockAction O2TankOnAction = new BlockAction(
            O2TANK, tank => tank?.ApplyAction("OnOff_On"));

        readonly BlockAction O2GenOffAction = new BlockAction(
            O2GEN, gen => gen?.ApplyAction("OnOff_Off"));

        private StringBuilder sb = new StringBuilder();
        List<NamedState> states;

        public Program()
        {
            states = new List<NamedState>();
            //state entry points

            var openState = new NamedState("OPEN", LIGHT_BACK,
                    light => light?.ApplyAction("OnOff_On"), 0.5);
            var closeState = new NamedState("CLOSE", LIGHT_BACK,
                    light => light?.ApplyAction("OnOff_Off"), 0.5);
            #region idle and diagnostic
            var idle = new NamedState(IDLE_STATE_NAME, ClearLCDAction, 5.0);
            var stop = new NamedState("STOP", null, 1);
            states.Add(stop);

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
            #endregion


            //open sequence
            states.SetUpSequence(new[] {
            openState,
            new NamedState("O2GEN OFF O", O2GenOffAction, 0.5),
            new NamedState("O2TANK ON O", O2TankOnAction, 0.5),
            new NamedState("DEPRESSURIZE", AIR_VENT_NAME,
                vent => vent?.ApplyAction("Depressurize_On"), 1.0),
            new NamedState( "OPEN DOOR", DOOR_NAME,
                door => door?.ApplyAction("Open_On"), 7.0),
            new NamedState( "O2TANK OFF O", O2TankOffAction, 0.5),
            new NamedState( "O2GEN ON O", O2GenOnAction, 0.5),
            idle
            });

            ///close sequence
            states.SetUpSequence(new[] {
            closeState,
            new NamedState("CLOSE DOOR", DOOR_NAME,
                door => door?.ApplyAction("Open_Off"), 1.0),
            new NamedState("O2GEN OFF C", O2GenOffAction, 0.5),
            new NamedState("O2TANK ON C", O2TankOnAction, 0.5),
            new NamedState("PRESSURIZE", AIR_VENT_NAME,
                vent => vent?.ApplyAction("Depressurize_Off"), 2.0),
            new NamedState("O2TANK OFF C", O2TankOffAction, 0.5),
            new NamedState("O2GEN ON C", O2GenOnAction, 0.5),
            idle
        });

            //detach sequence
            states.SetUpSequence(new[] {
            new NamedState("DETACH", GYRO,
                gyro => gyro?.ApplyAction("OnOff_On"), 0.5),
            new NamedState("GEAR UNLOCK", GEAR_NAME, gear => {
                gear?.ApplyAction("OnOff_On");
                gear?.SetValueBool("Autolock", false);
                gear?.ApplyAction("Unlock");},
                1.0),
            new NamedState("DAMPENERS OFF", CONTROL_STAT,
                control => control?.SetValueBool("DampenersOverride", false), 0.5),
            new NamedState("THRUST INC", THRUST,
                thrust => {
                thrust?.ApplyAction("IncreaseOverride");
                thrust?.ApplyAction("IncreaseOverride");
                thrust?.ApplyAction("IncreaseOverride");},
                0.5),
            new NamedState("THRUST DEC", THRUST,
                thrust => {
                    thrust?.ApplyAction("DecreaseOverride");
                    thrust?.ApplyAction("DecreaseOverride");
                    thrust?.ApplyAction("DecreaseOverride");},
                10.0),
            new NamedState("HATCH ON", HATCH,
                hatch => hatch?.ApplyAction("OnOff_On"), 1.0),
            new NamedState("AUTOLOCK", GEAR_NAME,
                gear => {
                    gear?.ApplyAction("OnOff_On");
                    gear?.SetValueBool("Autolock", true); },
                1.0),
            closeState
        });

            //attach sequence
            states.SetUpSequence(new[] {
            new NamedState("ATTACH", GEAR_NAME,
                gear => {
                    gear?.ApplyAction("OnOff_On");
                    // if the ship was just merged the old lock state is corrupted.  
                    // this fixes it 
                    gear?.SetValueBool("Autolock", false);
                    gear?.ApplyAction("Unlock");

                    gear?.SetValueBool("Autolock", true);
                    gear?.ApplyAction("Lock"); },
                0.5),
            new NamedState("HATCH OFF", HATCH,
                hatch => hatch?.ApplyAction("OnOff_Off"), 1.0),
            new NamedState("GYRO OFF", GYRO,
                gyro => gyro?.ApplyAction("OnOff_Off"), 0.5),
            openState
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
    }// Omit this last closing brace as the game will add it back in
}
