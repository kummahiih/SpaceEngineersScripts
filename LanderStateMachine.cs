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

        //idle loop:
        // time(idle)
        // list all named blocks(blocks)
        // list all registered states(ls)

        //close door sequence:
        // Landing light off(CLOSE)
        // Close door
        // Ogygen generator off
        // Oxygen tank on
        // Depressurize off
        // Oxygen tank off
        // Ogygen generator on
        // idle

        //detach sequence:        
        // Gyroscope on(DETACH)
        // Landing gear autolock off and unlock
        // Dampeners off
        // Ogygen generator on
        // Thrust ++
        // Thrust --
        // hatch on
        // Landing gear autolock on
        // Close the door(full sequence)


        //open door sequence:
        // Landing light on(OPEN)
        // Ogygen generator off
        // Oxygen tank on
        // Depressurize on
        // open the door
        // Oxygen tank off
        // Ogygen generator on
        // idle

        //attach sequence:
        // set the landin gear(ATTACH)
        // gyro off
        // hatch off
        // open door(full sequence)

        const string TIMER_NAME = "STATE TIMER";
        const string LCD_NAME = "STATE LCD";
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

        readonly BlockAction O2GenOnAction = new BlockAction(
            O2GEN, gen => gen?.ApplyAction("OnOff_On"));

        readonly BlockAction O2TankOffAction = new BlockAction(
            O2TANK, tank => tank?.ApplyAction("OnOff_Off"));

        readonly BlockAction O2TankOnAction = new BlockAction(
            O2TANK, tank => tank?.ApplyAction("OnOff_On"));

        readonly BlockAction O2GenOffAction = new BlockAction(
           O2GEN, gen => gen?.ApplyAction("OnOff_Off"));

        List<BlockState> states;

        public Program()
        {
            states = new List<BlockState>();
            //state entry points
            var idle = new BlockState("IDLE", TimeAction, (float)5.0);
            var openState = new BlockState("OPEN", LIGHT_BACK,
                    light => light?.ApplyAction("OnOff_On"), (float)0.5);
            var closeState = new BlockState("CLOSE", LIGHT_BACK,
                    light => light?.ApplyAction("OnOff_Off"), (float)0.5);

            //idle loop
            states.SetUpSequence(new[] {
                idle,
                 new BlockState("time", TimeAction, (float)5.0),
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
                    (float)5.0),

                new BlockState( "ls", LCD_NAME,
                    lcd => (lcd as IMyTextPanel)?.WritePublicText(
                            string.Join(",\n", states.Select(s => s.to_str())) + "\n"),
                    (float)5.0),
                idle});

            //open sequence
            states.SetUpSequence(new[] {
               openState,
                new BlockState("O2GEN OFF O", O2GenOffAction, (float)0.5),
                new BlockState("O2TANK ON O", O2TankOnAction, (float)0.5),
                new BlockState("DEPRESSURIZE", AIR_VENT_NAME,
                    vent => vent?.ApplyAction("Depressurize_On"), (float)1.0),
                new BlockState( "OPEN DOOR", DOOR_NAME,
                    door => door?.ApplyAction("Open_On"), (float)7.0),
                new BlockState( "O2TANK OFF O", O2TankOffAction, (float)0.5),
                new BlockState( "O2GEN ON O", O2GenOnAction, (float)0.5),
                idle
             });

            ///close sequence
            states.SetUpSequence(new[] {
                closeState,
                new BlockState("CLOSE DOOR", DOOR_NAME,
                    door => door?.ApplyAction("Open_Off"), (float)1.0),
                new BlockState("O2GEN OFF C", O2GenOffAction, (float)0.5),
                new BlockState("O2TANK ON C", O2TankOnAction, (float)0.5),
                new BlockState("PRESSURIZE", AIR_VENT_NAME,
                    vent => vent?.ApplyAction("Depressurize_Off"), (float)2.0),
                new BlockState("O2TANK OFF C", O2TankOffAction, (float)0.5),
                new BlockState("O2GEN ON C", O2GenOnAction, (float)0.5),
                idle
            });

            //detach sequence
            states.SetUpSequence(new[] {
                new BlockState("DETACH", GYRO,
                    gyro => gyro?.ApplyAction("OnOff_On"), (float)0.5),
                new BlockState("GEAR UNLOCK", GEAR_NAME, gear => {
                    gear?.ApplyAction("OnOff_On");
                    gear?.SetValueBool("Autolock", false);
                    gear?.ApplyAction("Unlock");},
                    (float)1.0),
                new BlockState("DAMPENERS OFF", CONTROL_STAT,
                    control => control?.SetValueBool("DampenersOverride", false), (float)0.5),
                new BlockState("THRUST INC", THRUST,
                    thrust => {
                    thrust?.ApplyAction("IncreaseOverride");
                    thrust?.ApplyAction("IncreaseOverride");
                    thrust?.ApplyAction("IncreaseOverride");},
                    (float)0.5),
                new BlockState("THRUST DEC", THRUST,
                    thrust => {
                        thrust?.ApplyAction("DecreaseOverride");
                        thrust?.ApplyAction("DecreaseOverride");
                        thrust?.ApplyAction("DecreaseOverride");},
                    (float)10.0),
                new BlockState("HATCH ON", HATCH,
                    hatch => hatch?.ApplyAction("OnOff_On"), (float)1.0),
                new BlockState("AUTOLOCK", GEAR_NAME,
                    gear => {
                       gear?.ApplyAction("OnOff_On");
                       gear?.SetValueBool("Autolock", true); },
                    (float)1.0),
                closeState
            });

            //attach sequence
            states.SetUpSequence(new[] {
                new BlockState("ATTACH", GEAR_NAME,
                    gear => {
                        gear?.ApplyAction("OnOff_On");
                        // if the ship was just merged the old lock state is corrupted.  
                        // this fixes it 
                        gear?.SetValueBool("Autolock", false);
                        gear?.ApplyAction("Unlock");

                        gear?.SetValueBool("Autolock", true);
                        gear?.ApplyAction("Lock"); },
                    (float)0.5),
                new BlockState("HATCH OFF", HATCH,
                    hatch => hatch?.ApplyAction("OnOff_Off"), (float)1.0),
                new BlockState("GYRO OFF", GYRO,
                    gyro => gyro?.ApplyAction("OnOff_Off"), (float)0.5),
                openState
            });
        }

        public void Main(string eventName)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                var state_name = GetString(this);
                if (string.IsNullOrEmpty(state_name))
                    state_name = "idle";
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
            timer.SetValueFloat("TriggerDelay", state.Delay);
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
        public float Delay { get; }
        public BlockState Next { get; set; }
        public string Name { get; }

        public BlockState(
            string name,
            string block_name,
            Action<IMyTerminalBlock> action,
            float delay,
            BlockState next = null) :
            this(name, new BlockAction(block_name, action), delay, next)
        { }


        public BlockState(
            string name,
            BlockAction block_action,
            float delay,
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
            foreach(var state in registered_states.Reverse())
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
