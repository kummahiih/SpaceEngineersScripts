using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;

namespace MinerScript
{
    public class DiscoDrone : MyGridProgram
    {
        #region copymeto programmable block 
        //see https://github.com/kummahiih/SpaceEngineersScripts/blob/master/RandomMover.cs


        const int NumberOfMoves = 60;

        public void Main(string eventName)
        {
            var gyroGenerator = new ActionGenerator<IMyGyro>(
                this,
                new List<string>{
                    "IncreaseYaw",
                    "DecreaseYaw",
                    "IncreasePitch",
                    "DecreasePitch",
                    "IncreaseRoll",
                    "DecreaseRoll"});
            var trusterGenerator = new ActionGenerator<IMyThrust>(
                this,
                new List< string >{
                    "IncreaseOverride",
                    "DecreaseOverride", "DecreaseOverride", 
                    "DecreaseOverride", "DecreaseOverride", "DecreaseOverride" });

            var spotGenerator = new ActionGenerator<IMyReflectorLight>(
                this,
                new List < string >{
                    "IncreaseRadius",
                    "DecreaseRadius",
                    "IncreaseBlink Interval",
                    "DecreaseBlink Interval",
                    "IncreaseBlink Lenght",
                    "DecreaseBlink Lenght"});

            var actions = new List<Action>();
             new List<ActionGenerator> { gyroGenerator, trusterGenerator, trusterGenerator, spotGenerator }
                .ForEach(gen => gen.GetActions(actions));
            var random = new Random();

            for (int i = 0; i < NumberOfMoves; i++)
            {
                var index = random.Next(0, actions.Count - 1);
                actions[index].Invoke();
            }
        }
        abstract class ActionGenerator
        {
            public abstract void GetActions(List<Action> target);
        }

        class ActionGenerator<TBlockType> : ActionGenerator where TBlockType : class, IMyTerminalBlock
        {
            private List<TBlockType> _blocks { get; }
            private List<string> _names { get; }

            public ActionGenerator(MyGridProgram env, List<string> actionNames)
            {
                _blocks = new List<TBlockType>();
                _names = actionNames;
                Env = env; 
                Env.GridTerminalSystem.GetBlocksOfType(_blocks);
            }

            public MyGridProgram Env { get; }


            public override void GetActions(List<Action> target)
            => _blocks
                .ForEach(block =>
                    _names.ForEach(name => target.Add(new Action( () => block.ApplyAction(name))
                        )));
            
        }

        #endregion
    }// Omit this last closing brace as the game will add it back in
}
