using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinerScript.Drivers
{
    class Program : MyGridProgram
    {
        #region copymeto programmable block
        /* Copyright 2017 Pauli Rikula (MIT https://opensource.org/licenses/MIT)
         * see https://github.com/kummahiih/SpaceEngineersScripts/blob/master/ somewhere
         */

        const string DEBUG_LCD_NAME = "Text panel pylon 1";
        const string REMOTE_NAME = "REMOTE FOR ORIENTATION";
        const string GYRO_NAME = "GYRO FOR ORIENTATION";

        IMyTextPanel DebugPanel;
        IMyRemoteControl Remote;
        IMyGyro Gyro;

        //see https://github.com/kummahiih/SpaceEngineersScripts/blob/master/ .. somewhere 
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;


            try
            {
                DebugPanel = this.GridTerminalSystem.GetBlockWithName(DEBUG_LCD_NAME).CastOrRaise<IMyTextPanel>();
                Remote = this.GridTerminalSystem.GetBlockWithName(REMOTE_NAME).CastOrRaise<IMyRemoteControl>();
                Gyro = this.GridTerminalSystem.GetBlockWithName(GYRO_NAME).CastOrRaise<IMyGyro>();
            }
            catch (Exception e)
            {
                if(DebugPanel != null)
                    e.ToString().EchoOn(DebugPanel);
                e.ToString().EchoOn(this);
            }
        }

        public void Main(string argument, UpdateType update)
        {
            var gps = argument.AsGPS();

            if (update.HasFlag(UpdateType.Terminal))
            {
                "Hello there\n".EchoOn(DebugPanel);
                if (gps != null)
                {
                    Storage = argument;
                    Gyro.GyroOverride = true;
                }
                return;
            }
            if (update.HasFlag(UpdateType.Script))
            {
                "Called by script\n".EchoOn(DebugPanel);
                if (gps != null)
                {
                    Storage = argument;
                    Gyro.GyroOverride = true;
                }
                return;
            }
            if (!update.HasFlag(UpdateType.Update10))
            {
                "Not called by tick".EchoOn(DebugPanel);
                return;
            }
            gps = Storage.AsGPS();
            if (gps == null)
                return;

            var position = Remote.CubeGrid.GetPosition();
            var target_dir = (gps.Value - position);
            if (target_dir.Length() < 1)
            {
                "Too close\n".EchoOn(DebugPanel);
                return;
            }
            target_dir.Normalize();
            var forward = Remote.WorldMatrix.Forward; 
            //.CubeGrid.GridIntegerToWorld(VRageMath.Vector3I.Forward);
            forward.Normalize();
            var axis = VRageMath.Vector3D.Cross(forward, target_dir);
            var angle = System.Math.Asin(axis.Normalize());

            var worldToGyro = VRageMath.MatrixD.Invert(Gyro.WorldMatrix.GetOrientation());
            var localAxis = VRageMath.Vector3D.Transform(axis, worldToGyro);

            double value = Math.Log(angle + 1, 2);
            if (value < 0.001)
            {
                localAxis *= 0;
                Gyro.GyroOverride = false;
            }
            else
            {
                localAxis *= value;
            }
            
            Gyro.SetValue("Pitch", (float)localAxis.X);
            Gyro.SetValue("Yaw", (float)-localAxis.Y);
            Gyro.SetValue("Roll", (float)-localAxis.Z);

        }
    }


    public class BlocNotkFound<TExpected> : Exception where TExpected : class, IMyTerminalBlock
    {
        public Type Expected { get; }
        public BlocNotkFound(): base($"Block of type {typeof(TExpected).ToString()} was not found")
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

        public static VRageMath.Vector3D? AsGPS(this string position)
        {
            //GPS:pauli.rikula #1:4.89:-15.81:22.9:
            var match = System.Text.RegularExpressions.Regex.Match(
                position, @"^GPS:(?<name>[^:]+):(?<X>-?\d+(\.\d+)?):(?<Y>-?\d+(\.\d+)?):(?<Z>-?\d+(\.\d+)?):\s*$");

            if (!match.Success)
                return null;
            float x = float.Parse(match.Groups["X"].Value);
            float y = float.Parse(match.Groups["Y"].Value);
            float z = float.Parse(match.Groups["Z"].Value);

            return new VRageMath.Vector3D(x, y, z);
        }



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

