using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirLockScript
{
    class AirLockStateMAchine : MyGridProgram
    {
        #region
        //see https://github.com/kummahiih/SpaceEngineersScripts/blob/master/ .. somewhere

        //run the programmable block with the state name as a parameter

        const string TIMER_NAME = "STATE TIMER S";


        const string LCD_NAME = "STATE LCD S";
        const string AIR_VENT_NAME = "AIR VENT S";
        const string DOOR_NAME = "DOOR S";
        const string LIGHT_BACK = "LIGHT S";


        public void Main(string eventName)
        {
            List<BlockState> states = new List<BlockState>();

            BlockState ls = new BlockState(
                this, "ls", LCD_NAME, lcd =>
                (lcd as IMyTextPanel)?.WritePublicText(
                    string.Join(",\n", states.Select(s => s.to_str())) + "\n"),
                (float)1.0);

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

            BlockState idle = new BlockState(
                this, "IDLE", LCD_NAME, lcd =>
                (lcd as IMyTextPanel)
                ?.WritePublicText(" " + DateTime.UtcNow.ToLongTimeString(), true),
                (float)1.0);
            idle.Next = idle;

            BlockState open_door = new BlockState(
                this, "OPEN DOOR", DOOR_NAME, door =>
                    door?.ApplyAction("Open_On"),
                (float)7.0,
                idle);

            BlockState press = new BlockState(
                this, "PRESSURIZE", AIR_VENT_NAME, vent =>
                    vent?.ApplyAction("Depressurize_Off"),
                (float)2.0,
                idle);

            BlockState close_door = new BlockState(
                this, "CLOSE DOOR", DOOR_NAME, door =>
                    door?.ApplyAction("Open_Off"),
               (float)1.0,
               press);

            BlockState depress = new BlockState(
                this, "DEPRESSURIZE", AIR_VENT_NAME, vent =>
                    vent?.ApplyAction("Depressurize_On"),
                (float)1.0,
                open_door);

            BlockState open = new BlockState(
                this, "OPEN", LIGHT_BACK, light =>
                    light?.ApplyAction("OnOff_On"),
                (float)0.5,
                depress);

            BlockState close = new BlockState(
                this, "CLOSE", LIGHT_BACK, light =>
                    light?.ApplyAction("OnOff_Off"),
                (float)0.5,
                close_door);


            states.AddRange(new[]{
                ls, blocks,
                idle, open, close, 
                open_door, press, close_door, depress});


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
