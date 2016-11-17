using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;

namespace SpaceEngineersScripts
{
    public class RandomMover : MyGridProgram
    {
        #region copymeto programmable block 
        //see https://github.com/kummahiih/SpaceEngineersScripts/blob/master/RandomMover.cs
        
        const string RGyroName = "RGyro";
        const string RThruster = "RThrust";
        const string RTSpot = "RTSpot";

        const int NumberOfMoves = 10;

        public void Main(string eventName)
        {
            var gyro = new Block<IMyGyro>(this, RGyroName);
            var truster = new Block<IMyThrust>(this, RThruster);
            var spot = new Block<IMyReflectorLight>(this, RTSpot);

            Action increaseOverride = () => truster.ApplyAction("IncreaseOverride");
            Action decreaseOverride = () => truster.ApplyAction("DecreaseOverride");
            Action increaseYaw = () => gyro.ApplyAction("IncreaseYaw");
            Action decreaseYaw = () => gyro.ApplyAction("DecreaseYaw");
            Action increasePitch = () => gyro.ApplyAction("IncreasePitch");
            Action decreasePitch = () => gyro.ApplyAction("DecreasePitch");
            Action increaseRoll = () => gyro.ApplyAction("IncreaseRoll");
            Action decreaseRoll = () => gyro.ApplyAction("DecreaseRoll");
            Action increaseRadius = () => spot.ApplyAction("IncreaseRadius");
            Action decreaseRadius = () => spot.ApplyAction("DecreaseRadius");
            Action increaseBlinkI = () => spot.ApplyAction("IncreaseBlink Interval");
            Action decreaseBlinkI = () => spot.ApplyAction("DecreaseBlink Interval");
            Action increaseBlinkL = () => spot.ApplyAction("IncreaseBlink Lenght");
            Action decreaseBlinkL = () => spot.ApplyAction("DecreaseBlink Lenght");
            

            var actions = new Action[]{
                increaseOverride,
                decreaseOverride,decreaseOverride,decreaseOverride,
                decreaseOverride,decreaseOverride,decreaseOverride,
                increaseYaw,
                decreaseYaw,
                increasePitch,
                decreasePitch,
                increaseRoll,
                decreaseRoll,
                increaseRadius,
                decreaseRadius,
                increaseBlinkI,
                decreaseBlinkI,
                increaseBlinkL,
                decreaseBlinkL
                };

            var random = new Random();

            for (int i = 0; i < NumberOfMoves; i++)
            {
                var index = random.Next(0, actions.Length - 1);
                actions[index].Invoke();
            }
        }

        class Block<TBlockType> where TBlockType : class, IMyTerminalBlock
        {
            public Block(MyGridProgram env, string name) { Env = env; Name = name; }
            MyGridProgram Env { get; }
            public string Name { get; }
            public TBlockType Get()
                => Env?.GridTerminalSystem.GetBlockWithName(Name) as TBlockType;
            public void ApplyAction(string action)
                => Get()?.ApplyAction(action);
        }




    #endregion
    }// Omit this last closing brace as the game will add it back in
}
