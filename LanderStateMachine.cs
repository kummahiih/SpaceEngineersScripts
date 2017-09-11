using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinerScript
{
    class LanderStateMachine: MyGridProgram
    {
        #region copymeto programmable block 
        //see https://github.com/kummahiih/SpaceEngineersScripts/blob/master/ .. somewhere 

        //run the programmable block with the state name as a parameter 

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

        //close door sequence:
        // Landing light off (CLOSE)
        // Close door
        // Ogygen generator off
        // Oxygen tank on
        // Depressurize off
        // Oxygen tank off
        // Ogygen generator on
        // idle

        //detach sequence:        
        // Gyroscope on (DETACH)
        // Landing gear autolock off and unlock
        // Dampeners off
        // Ogygen generator on
        // Thrust ++
        // Thrust --
        // hatch on
        // Landing gear autolock on
        // Close the door (full sequence)


        //open door sequence:
        // Landing light on (OPEN)
        // Ogygen generator off
        // Oxygen tank on
        // Depressurize on
        // open the door
        // Oxygen tank off
        // Ogygen generator on
        // idle

        //attach sequence:
        // set the landin gear (ATTACH)
        // gyro off
        // hatch off
        // open door (full sequence)

        public void Main(string eventName)
        {
            List<BlockState> states = new List<BlockState>();

            //open sequence
            BlockState idle = new BlockState(
                this, "IDLE", LCD_NAME, lcd =>
                (lcd as IMyTextPanel)
                ?.WritePublicText(" " + DateTime.UtcNow.ToLongTimeString(), true),
                (float)1.0);
            idle.Next = idle;
            states.Add(idle);

            BlockState O2GenOn = new BlockState(
               this, "O2GEN ON", O2GEN, gen =>
                   gen?.ApplyAction("OnOff_On"),
               (float)0.5,
               idle);
            states.Add(O2GenOn);


            BlockState O2TankOff = new BlockState(
               this, "O2TANK OFF", O2TANK, tank =>
                   tank?.ApplyAction("OnOff_Off"),
               (float)0.5,
               O2GenOn);
            states.Add(O2TankOff);

            BlockState open_door = new BlockState(
                this, "OPEN DOOR", DOOR_NAME, door =>
                    door?.ApplyAction("Open_On"),
                (float)7.0,
                O2TankOff);
            states.Add(open_door);

            BlockState depress = new BlockState(
                this, "DEPRESSURIZE", AIR_VENT_NAME, vent =>
                    vent?.ApplyAction("Depressurize_On"),
                (float)1.0,
                open_door);
            states.Add(depress);

            BlockState O2TankOn = new BlockState(
               this, "O2TANK ON", O2TANK, tank =>
                   tank?.ApplyAction("OnOff_On"),
               (float)0.5,
               depress);
            states.Add(O2TankOn);

            BlockState O2GenOff = new BlockState(
                this, "O2GEN OFF", O2GEN, gen =>
                    gen?.ApplyAction("OnOff_Off"),
                (float)0.5,
                O2TankOn);
            states.Add(O2GenOff);

            BlockState open = new BlockState(
                this, "OPEN", LIGHT_BACK, light =>
                    light?.ApplyAction("OnOff_On"),
                (float)0.5,
                O2GenOff);
            states.Add(open);

            ///close sequence
            BlockState press = new BlockState(
                this, "PRESSURIZE", AIR_VENT_NAME, vent =>
                    vent?.ApplyAction("Depressurize_Off"),
                (float)2.0,
                O2TankOff);
            states.Add(press);

            BlockState O2TankOn_C = new BlockState(
               this, "O2TANK ON C", O2TANK, tank =>
                   tank?.ApplyAction("OnOff_On"),
               (float)0.5,
               press);
            states.Add(O2TankOn_C);

            BlockState O2GenOff_C = new BlockState(
                this, "O2GEN OFF C", O2GEN, gen =>
                    gen?.ApplyAction("OnOff_Off"),
                (float)0.5,
                O2TankOn_C);
            states.Add(O2GenOff_C);

            BlockState close_door = new BlockState(
                this, "CLOSE DOOR", DOOR_NAME, door =>
                    door?.ApplyAction("Open_Off"),
               (float)1.0,
               O2GenOff_C);
            states.Add(close_door);

            BlockState close = new BlockState(
                this, "CLOSE", LIGHT_BACK, light =>
                    light?.ApplyAction("OnOff_Off"),
                (float)0.5,
                close_door);
            states.Add(close);
            //detach sequence

            BlockState autolockOn = new BlockState(
               this, "AUTOLOCK", GEAR_NAME, gear => {
                   gear?.ApplyAction("OnOff_On");
                   gear?.SetValueBool("Autolock", true);},
               (float)1.0,
               close);
            states.Add(autolockOn);

            BlockState hatchOn = new BlockState(
               this, "HATCH ON", HATCH, hatch =>
               hatch?.ApplyAction("OnOff_On"),
               (float)1.0,
               autolockOn);
            states.Add(hatchOn);

            BlockState decreaseThrust = new BlockState(
                this, "THRUST DEC", THRUST, thrust =>
                {
                    thrust?.ApplyAction("DecreaseOverride");
                    thrust?.ApplyAction("DecreaseOverride");
                    thrust?.ApplyAction("DecreaseOverride");
                },
                (float)10.0,
                hatchOn);
            states.Add(decreaseThrust);

            BlockState increaseThrust = new BlockState(
                this, "THRUST INC", THRUST, thrust =>
                {
                    thrust?.ApplyAction("IncreaseOverride");
                    thrust?.ApplyAction("IncreaseOverride");
                    thrust?.ApplyAction("IncreaseOverride");
                },
                (float)0.5,
                decreaseThrust);
            states.Add(increaseThrust);

            BlockState dampenersOff = new BlockState(
                this, "DAMPENERS OFF", CONTROL_STAT, control =>
                    control?.SetValueBool("DampenersOverride", false),
                (float)0.5,
                increaseThrust);
            states.Add(dampenersOff);

            BlockState gearUnlock = new BlockState(
                this, "GEAR UNLOCK", GEAR_NAME, gear => {
                    gear?.ApplyAction("OnOff_On");
                    gear?.SetValueBool("Autolock", false);
                    gear?.ApplyAction("Unlock");
                },
                (float)1.0,
                dampenersOff);
            states.Add(gearUnlock);

            BlockState detach = new BlockState(
                this, "DETACH", GYRO, gyro =>
                    gyro?.ApplyAction("OnOff_On"),
                (float)0.5,
                gearUnlock);
            states.Add(detach);

            //attach sequence

            BlockState gyroOff = new BlockState(
                this, "GYRO OFF", GYRO, gyro =>
                    gyro?.ApplyAction("OnOff_Off"),
                (float)0.5,
                open);
            states.Add(gyroOff);

            BlockState hatchOff = new BlockState(
               this, "HATCH OFF", HATCH, hatch =>
               hatch?.ApplyAction("OnOff_Off"),
               (float)1.0,
               gyroOff);
            states.Add(hatchOff);

            BlockState attach = new BlockState(
                this, "ATTACH", GEAR_NAME, gear => {
                    gear?.ApplyAction("OnOff_On");
                    // if the ship was just merged the old lock state is corrupted.  
                    // this fixes it 
                    gear?.SetValueBool("Autolock", false);
                    gear?.ApplyAction("Unlock");

                    gear?.SetValueBool("Autolock", true);
                    gear?.ApplyAction("Lock");
                },
                (float)0.5,
                hatchOff);
            states.Add(attach);

            BlockState ls = new BlockState(
                this, "ls", LCD_NAME, lcd =>
                (lcd as IMyTextPanel)?.WritePublicText(
                    string.Join(",\n", states.Select(s => s.to_str())) + "\n"),
                (float)1.0);
            states.Add(ls);

            BlockState blocks = new BlockState(
                this, "blocks", LCD_NAME, lcd => {
                    (lcd as IMyTextPanel)?.WritePublicText("");
                    var statenames = states.Select(s => s.BlockName)
                        .Concat(new[] { TIMER_NAME })
                        .Distinct();
                    foreach (var name in statenames)
                    {
                        var found_text =
                            this.GridTerminalSystem.GetBlockWithName(name) == null ? "\n" : " FOUND\n";
                        (lcd as IMyTextPanel)
                            ?.WritePublicText("'" + name + "'" + found_text, true);
                    }
                },
                (float)1.0);
            states.Add(blocks);


            if (string.IsNullOrEmpty(eventName))
            {
                var state_name = GetString(this);
                if (string.IsNullOrEmpty(state_name))
                    state_name = "idle";
                states.ForEach(state => state.Execute(state_name));
                return;
            }
            states.ForEach(state => state.StartTimer(eventName));
        }

        public static void SaveString(MyGridProgram env, string text)
        {
            var lcd = env.GridTerminalSystem.GetBlockWithName(LCD_NAME) as IMyTextPanel;
            if (lcd == null) return;
            lcd.WritePublicTitle(text);
            if (lcd.GetPublicText().Count(s => s == '\n') > 15)
                lcd.WritePublicText("\n");
            lcd.WritePublicText(" -> " + text + "\n", true);
        }

        public static string GetString(MyGridProgram env)
        {
            var lcd = env.GridTerminalSystem.GetBlockWithName(LCD_NAME) as IMyTextPanel;
            if (lcd == null) return null;
            return lcd.GetPublicTitle();
        }


        class BlockState
        {
            public string BlockName { get; }
            private Action _action { get; }
            private float _delay { get; }
            public BlockState Next { get; set; }
            public string Name { get; }
            public MyGridProgram Env { get; }

            public BlockState(
                MyGridProgram env,
                string name, string block_name,
                Action<IMyTerminalBlock> action,
                float delay,
                BlockState next = null)
            {
                BlockName = block_name;
                _delay = delay;
                Name = name;
                Env = env;
                Next = next;

                _action = () => env
                    .GridTerminalSystem
                    .GetBlockWithName(block_name)
                    ?.Apply(action);
            }

            public string to_str() =>
                "'" + Name + "':'" + BlockName + "'" +
                (Next != null ? (" -> " + "'" + Next.Name + "'") : "");


            public void StartTimer(string name = null)
            {
                if (name != null && Name != name) return;
                SaveString(Env, Name);

                var timer = Env.GridTerminalSystem.GetBlockWithName(TIMER_NAME) as IMyTimerBlock;
                if (timer == null) return;
                timer.SetValueFloat("TriggerDelay", _delay);
                timer.ApplyAction("Start");
            }

            public void Execute(string name)
            {
                if (Name != name) return;
                var lcd = Env.GridTerminalSystem.GetBlockWithName(LCD_NAME) as IMyTextPanel;
                lcd?.WritePublicText(">" + Name, true);

                _action?.Invoke();
                Next?.StartTimer();
            }
        }
    }

    public static class Ext
    {
        public static void Apply(this IMyTerminalBlock block, Action<IMyTerminalBlock> action)
            => action(block);
    
    #endregion
    }// Omit this last closing brace as the game will add it back in
}
