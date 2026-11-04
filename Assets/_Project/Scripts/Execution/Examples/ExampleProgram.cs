using UnityEngine;

namespace Cubely.Execution.Examples
{
    /// <summary>
    /// Handwritten stand-in for future compiled code. ProgramRunner invokes Run;
    /// this component no longer starts a second execution on scene startup.
    /// </summary>
    public class ExampleProgram
    {
        public static void Run(ProgramContext context)
        {
            context.MoveForward();
            context.MoveForward();
            context.TurnRight();
            context.MoveForward();
            context.MoveForward();
            context.TurnLeft();
            context.MoveForward();
            context.MoveForward();
        }
    }
}
