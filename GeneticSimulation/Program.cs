﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="">
//   
// </copyright>
// <summary>
//   The program.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace GeneticSimulation
{
    using System.Linq;

    /// <summary>
    /// The program.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// The main.
        /// </summary>
        private static void Main()
        {
            // TODO: Use contracts everywhere?
            var universe = new World();
            var list = Enumerable.Range(1, 8).Select(
                _ =>
                    {
                        universe.Run(128);
                        return universe.Statistic;
                    });
        }
    }
}