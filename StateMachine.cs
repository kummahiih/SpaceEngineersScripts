using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;

namespace StateMatch
{
    public class StateMatch : MyGridProgram
    {
        #region copymeto programmable block 
        //see https://github.com/kummahiih/SpaceEngineersScripts/blob/master/ .. somewhere

        //run the programmable block with the state name as a parameter
        const string TIMER_NAME = "STATE TIMER";
        const string TIMER_EVENT = "TIMER EVENT";

        const string LCD_NAME = "STATE LCD";

        const string AIR_VENT_NAME = "AIR VENT";
        const string DOOR_NAME = "DOOR";

        const string GEAR_NAME = "GEAR";



        public void Main(string eventName)
        {
            State idle = new State(this, "IDLE", 
                () => SaveString(this, DateTime.UtcNow.ToLongTimeString()), 
                (float)1.0);
            idle.Next = idle;

            State open_door = new State(this, "OPEN DOOR",
                () => this.GridTerminalSystem
                    .GetBlockWithName(DOOR_NAME)
                    ?.ApplyAction("Open_On"),
                (float)7.0);
            open_door.Next = idle;

            State press = new State(this, "PRESSURIZE",
                () => this.GridTerminalSystem
                    .GetBlockWithName(AIR_VENT_NAME)
                    ?.ApplyAction("Depressurize_Off"),
                (float)2.0);
            press.Next = idle;

            State close_door = new State(this, "CLOSE DOOR",
               () => this.GridTerminalSystem
                   .GetBlockWithName(DOOR_NAME)
                   ?.ApplyAction("Open_Off"),
               (float)1.0);
            close_door.Next = press;

            State depress = new State(this, "DEPRESSURIZE",
                () => this.GridTerminalSystem
                    .GetBlockWithName(AIR_VENT_NAME)
                    ?.ApplyAction("Depressurize_On"),
                (float)1.0);
            depress.Next = open_door;

            State detach = new State(this, "DETACH", () => {
                var gear = this.GridTerminalSystem.GetBlockWithName(GEAR_NAME);
                gear?.SetValueBool("Autolock", false);
                gear?.ApplyAction("Unlock");
                },
                (float)1.0);
            detach.Next = depress;

            State attach = new State(this, "ATTACH", () => {
                var gear = this.GridTerminalSystem.GetBlockWithName(GEAR_NAME);
                gear?.SetValueBool("Autolock", true);
                gear?.ApplyAction("Lock");
                },
                (float)1.0);
            attach.Next = depress;

            var states = new List<State> { idle, open_door, press, close_door, depress, detach, attach };

            if (eventName == TIMER_EVENT) {
                var state_name = GetString(this);
                
                states.ForEach(state => state.Execute(state_name));
            }
            else if(!string.IsNullOrEmpty(eventName))
                states.ForEach(state => state.StartTimer(eventName));

        }

        public static void SaveString(MyGridProgram env, string text)
        {
            var lcd = env.GridTerminalSystem.GetBlockWithName(LCD_NAME) as IMyTextPanel;
            if (lcd == null) return;
            lcd.WritePublicText(text);
        }

        public static string GetString(MyGridProgram env)
        {
            var lcd = env.GridTerminalSystem.GetBlockWithName(LCD_NAME) as IMyTextPanel;
            if (lcd == null) return null;
            return lcd.GetPublicText();
        }

        class State
        {
            private Action _action { get; }
            private float _delay { get; }
            public State Next { get; set; }
            public string Name { get; }
            public MyGridProgram Env { get; }

            public State(MyGridProgram env, string name, Action action, float delay)
            {
                _action = action;
                _delay = delay;
                Name = name;
                Env = env;
            }


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
                _action?.Invoke();
                Next?.StartTimer();
            }
        }

        #endregion
    }// Omit this last closing brace as the game will add it back in
}
