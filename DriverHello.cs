using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DriverTemplate
{
    class Program : MyGridProgram
    {
        #region copymeto programmable block
        /* Copyright 2017 Pauli Rikula (MIT https://opensource.org/licenses/MIT)
         * see https://github.com/kummahiih/SpaceEngineersScripts/blob/master/ somewhere
         */

        const string DEBUG_LCD_NAME = "PROGRAM LCD";
        IMyTextPanel DebugPanel;

        //see https://github.com/kummahiih/SpaceEngineersScripts/blob/master/ .. somewhere 
        public Program()
        {
            try
            {
                DebugPanel = this.GridTerminalSystem.GetBlockWithName(DEBUG_LCD_NAME).CastOrRaise<IMyTextPanel>();
            }
            catch (Exception e)
            {
                e.ToString().EchoOn(this);
            }
        }

        public void Main(string argument)
        {

            "Hello there".EchoOn(DebugPanel);
        }
    }


    public class BlocNotkFound<TExpected> : Exception where TExpected : class, IMyTerminalBlock
    {
        public Type Expected { get; }
        public BlocNotkFound() : base($"Block of type {typeof(TExpected).ToString()} was not found")
        {
        }
    }

    public class InvalidType : Exception
    {
        public Type Expected { get; }
        public Type Got { get; }

        public InvalidType(Type exoected, Type got, string message_prefix = "") :
            base(message: $"{message_prefix}Excepted {exoected.ToString()} got {got.ToString()}")
        {
        }
    }
    public class InvalidType<TExpected> : InvalidType where TExpected : class
    {
        public InvalidType(object o, string message_prefix = "") : base(typeof(TExpected), o.GetType(), message_prefix) { }
    }

    public class InvalidBlockType<TExpected> : InvalidType<TExpected> where TExpected : class, IMyTerminalBlock
    {
        public InvalidBlockType(IMyTerminalBlock b, string message_prefix = "") :
            base(b, $"{ message_prefix}Block '{b.Name}': ")
        { }
    }

    public static class Ext
    {
        public static TBlock CastOrRaise<TBlock>(this IMyTerminalBlock block) where TBlock : class, IMyTerminalBlock
        {
            if (block == null)
                throw new BlocNotkFound<TBlock>();
            var casted_block = block as TBlock;
            if (casted_block == null)
                throw new InvalidType<TBlock>(block);
            return casted_block;
        }

        public static string AsGPS(this VRageMath.Vector3D position, string name)
            => String.Format(
                "GPS:{0}:{1:0.00}:{2:0.00}:{3:0.00}:\n",
                name, position.X, position.Y, position.Z);

        public static void Apply<TBlock>(this TBlock block, Action<TBlock> action) where TBlock : class, IMyTerminalBlock
            => action(block);

        public static void EchoOn(this string s, MyGridProgram pb) =>
            pb.Echo(s);

        public static void EchoOn(this string s, IMyTextPanel lcd)
        {
            lcd.WritePublicText(s, true);
            lcd.ShowPublicTextOnScreen();
        }

        public static void EchoOn(this StringBuilder sb, IMyTextPanel lcd)
            => sb.ToString().EchoOn(lcd);
        public static void EchoOn(this StringBuilder sb, MyGridProgram pb)
            => sb.ToString().EchoOn(pb);
        #endregion
    }

}// Omit this last closing brace as the game will add it back in

