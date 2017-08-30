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
       
        const string TIMER_EVENT   = "TIMER EVENT";


        const string TIMER_NAME     = "STATE TIMER";
        const string LCD_NAME       = "STATE LCD";
        const string AIR_VENT_NAME  = "AIR VENT";
        const string DOOR_NAME      = "DOOR";
        const string GEAR_NAME      = "GEAR";
        const string LIGHT_BACK     = "LIGHT BACK";
        const string MERGE_MED      = "MERGE MED";
        const string CONNECTOR_MED  = "CONNECTOR MED";

        

        public void Main(string eventName)
        {
            List<State> states = new List<State>();
            List<string> block_names = new List<string>{
                TIMER_NAME,
                LCD_NAME,
                AIR_VENT_NAME,
                DOOR_NAME,
                GEAR_NAME,
                LIGHT_BACK,
                MERGE_MED,
                CONNECTOR_MED
            };

            State ls = new State(this, "ls", () => {
                var lcd = this.GridTerminalSystem.GetBlockWithName(LCD_NAME) as IMyTextPanel;
                lcd?.WritePublicText(
                    string.Join(",\n", states.Select(s => s.to_str())) + "\n");
            },
            (float)1.0);

            State blocks = new State(this, "blocks", () => {
                var lcd = this.GridTerminalSystem.GetBlockWithName(LCD_NAME) as IMyTextPanel;
                lcd?.WritePublicText("");
                foreach (var name in block_names)
                {
                    lcd?.WritePublicText(
                        "'"+ name +"'"+ (
                        this.GridTerminalSystem.GetBlockWithName(name) == null ? "\n" : " FOUND\n"),
                        true);                             
                }               
            },
            (float)1.0);


            State idle = new State(this, "IDLE", 
                () => {
                    var lcd = this.GridTerminalSystem.GetBlockWithName(LCD_NAME) as IMyTextPanel;
                    lcd?.WritePublicText(" "+DateTime.UtcNow.ToLongTimeString(), true);
                }, 
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
            detach.Next = close_door;

            State attach = new State(this, "ATTACH", () => {
                var gear = this.GridTerminalSystem.GetBlockWithName(GEAR_NAME);
                gear?.SetValueBool("Autolock", true);
                gear?.ApplyAction("Lock");
                },
                (float)1.0);
            attach.Next = depress;

            State unconnect = new State(this, "UNCONNECT", () => {
                var con = this.GridTerminalSystem.GetBlockWithName(CONNECTOR_MED) as IMyShipConnector;
                con?.ApplyAction("OnOff_On");
                con?.ApplyAction("Unlock");

                var blink = this.GridTerminalSystem.GetBlockWithName(LIGHT_BACK);
                blink?.ApplyAction("OnOff_On");
            },
            (float)4.0);
            unconnect.Next = attach;

            State unmerge = new State(this, "UNMERGE", () => {
                var merge_b = this.GridTerminalSystem.GetBlockWithName(MERGE_MED) as IMyShipMergeBlock;
                merge_b?.ApplyAction("OnOff_Off");
            },
            (float)2.0);
            unmerge.Next = unconnect;

            State connect = new State(this, "CONNECT", () => {
                var con = this.GridTerminalSystem.GetBlockWithName(CONNECTOR_MED) as IMyShipConnector;
                con?.ApplyAction("OnOff_On");
                con?.ApplyAction("Lock");

                var blink = this.GridTerminalSystem.GetBlockWithName(LIGHT_BACK);
                blink?.ApplyAction("OnOff_Off");
            },
            (float)4.0);
            connect.Next = attach;

            State merge = new State(this, "MERGE", () => {
                var con = this.GridTerminalSystem.GetBlockWithName(CONNECTOR_MED) as IMyShipConnector;
                con?.ApplyAction("OnOff_On");
                con?.ApplyAction("Unlock");
                var merge_b = this.GridTerminalSystem.GetBlockWithName(MERGE_MED) as IMyShipMergeBlock;
                merge_b?.ApplyAction("OnOff_On");
            },
            (float)2.0);
            merge.Next = connect;


            states.AddRange( new []{
                ls, blocks, idle, open_door, press, close_door, depress, detach, attach,
                unconnect, unmerge,  connect, merge});



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
            lcd.WritePublicTitle(text);
            if(lcd.GetPublicText().Count(s => s =='\n') > 15)
                lcd.WritePublicText("\n");
            lcd.WritePublicText(" -> " +text+ "\n", true);
        }

        public static string GetString(MyGridProgram env)
        {
            var lcd = env.GridTerminalSystem.GetBlockWithName(LCD_NAME) as IMyTextPanel;
            if (lcd == null) return null;
            return lcd.GetPublicTitle();
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

            public string to_str() => "'"+Name +"'"+ (Next != null ? (" -> " + "'" + Next.Name + "'") : "");


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
                lcd?.WritePublicText("!" + Name, true);

                _action?.Invoke();
                Next?.StartTimer();
            }
        }

        #endregion
    }// Omit this last closing brace as the game will add it back in
}
