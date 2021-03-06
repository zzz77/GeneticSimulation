﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Creature.cs" company="MZ">
//   This work is licensed under a Creative Commons Attribution 4.0 International License
// </copyright>
// <summary>
//   The creature.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace MZ.GeneticSimulation.DataModel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Linq;

    using MZ.GeneticSimulation.Helpers;

    /// <summary>
    ///     The creature.
    /// </summary>
    public class Creature
    {
        /// <summary>
        ///     Basic "strength" of gene
        /// </summary>
        private const int GeneStrength = 128;

        /// <summary>
        ///     List of child creatures
        /// </summary>
        private readonly List<Creature> childs = new List<Creature>(8);

        /// <summary>
        ///     Genes.
        /// </summary>
        private readonly Gene[] genes = new Gene[128];

        /// <summary>
        ///     The id of species.
        /// </summary>
        public readonly int IdOfSpecies;

        /// <summary>
        ///     Reference to world where this creature lives.
        /// </summary>
        private readonly World world;

        /// <summary>
        ///     The father of this creature.
        /// </summary>
        private Creature father;

        /// <summary>
        ///     The mother of this creature.
        /// </summary>
        private Creature mother;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Creature" /> class.
        /// </summary>
        /// <param name="idOfSpecies">
        ///     The id of species.
        /// </param>
        /// <param name="world">
        ///     The world.
        /// </param>
        public Creature(int idOfSpecies, World world)
        {
            Contract.Requires<ArgumentNullException>(world != null);
            Contract.Ensures(this.IdOfSpecies == idOfSpecies);
            this.IdOfSpecies = idOfSpecies;
            this.world = world;
            for (var i = 0; i < this.genes.Length; i++)
            {
                this.genes[i] = EnumHelper.CreateRandomGene();
            }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Creature" /> class.
        /// </summary>
        /// <param name="mommy">
        ///     The mommy.
        /// </param>
        /// <param name="daddy">
        ///     The daddy.
        /// </param>
        public Creature(Creature mommy, Creature daddy)
        {
            Debug.Assert(mommy.IdOfSpecies == daddy.IdOfSpecies, "Interspecies relation are FORBIDDEN!!!");
            this.mother = mommy;
            this.father = daddy;
            mommy.childs.Add(this);
            daddy.childs.Add(this);
            this.world = mommy.world;
            this.IdOfSpecies = mommy.IdOfSpecies;
            for (var i = 0; i < this.genes.Length; i++)
            {
                this.genes[i] = EnumHelper.ChooseRandomGene(mommy.genes[i], daddy.genes[i]);
            }
        }

        /// <summary>
        ///     Gets the strength of this creature.
        /// </summary>
        public int SummaryStrength
        {
            get
            {
                var sum = 0.0;
                var world = this.world;
                string cacheKey = $"AltruisticGenesOutStrength{this.IdOfSpecies}";
                var cachedValue = Cache.Get(cacheKey, world.Age);
                if (cachedValue != null)
                {
                    sum = (double)cachedValue;
                }
                else
                {
                    for (var i = 0; i < world.Species[this.IdOfSpecies].Count; i++)
                    {
                        if (world.Species[this.IdOfSpecies][i] != this)
                        {
                            sum += world.Species[this.IdOfSpecies][i].AltruisticGenesOutStrength;
                        }
                    }

                    Cache.Put(cacheKey, world.Age, sum);
                }

                return this.ThisCreatureGenesStrength + (int)sum + (int)this.HelpFromRelations;
            }
        }

        /// <summary>
        ///     Gets number of selfish genes (in this creature).
        /// </summary>
        public int SelfishGenes => this.genes.Count(g => g == Gene.SelfishGene);

        /// <summary>
        ///     Gets number of altruistic genes (in this creature).
        /// </summary>
        public int AltruisticGenes => this.genes.Count(g => g == Gene.AltruisticGene);

        /// <summary>
        ///     Gets number of creature_level genes (in this creature).
        /// </summary>
        public int CreatureLevelGenes => this.genes.Count(g => g == Gene.CreatureLevelGene);

        /// <summary>
        ///     Gets strength of this creature which was given it by its genes.
        /// </summary>
        private int ThisCreatureGenesStrength
            => this.genes.Sum(g => g == Gene.CreatureLevelGene ? GeneStrength : GeneStrength >> 1);

        /// <summary>
        ///     Gets strength which is given to all creatures of same species by this creature.
        /// </summary>
        private double AltruisticGenesOutStrength
        {
            get
            {
                var sum = 0;
                for (var i = 0; i < this.genes.Length; i++)
                {
                    var gene = this.genes[i];
                    if (gene == Gene.AltruisticGene)
                    {
                        sum += GeneStrength >> 1;
                    }
                }

                return (double)sum / (this.world.Species[this.IdOfSpecies].Count - 1);
            }
        }

        /// <summary>
        ///     Gets strength which was took by this creature from its relations.
        /// </summary>
        private double HelpFromRelations
        {
            get
            {
                var mommy = this.mother;
                var daddy = this.father;
                if (mommy == null)
                {
                    return 0;
                }

                if (mommy.mother == null)
                {
                    return mommy.GetSelfishGenesOutStrength(Relation.Child)
                           + daddy.GetSelfishGenesOutStrength(Relation.Child)
                           + mommy.childs.Sum(
                               brother =>
                               brother == this ? 0 : brother.GetSelfishGenesOutStrength(Relation.BrotherOrSister));
                }

                return mommy.GetSelfishGenesOutStrength(Relation.Child)
                       + daddy.GetSelfishGenesOutStrength(Relation.Child)
                       + mommy.childs.Sum(
                           brother => brother == this ? 0 : brother.GetSelfishGenesOutStrength(Relation.BrotherOrSister))
                       + mommy.mother.GetSelfishGenesOutStrength(Relation.GrandChild)
                       + mommy.father.GetSelfishGenesOutStrength(Relation.GrandChild)
                       + daddy.mother.GetSelfishGenesOutStrength(Relation.GrandChild)
                       + daddy.father.GetSelfishGenesOutStrength(Relation.GrandChild)
                       + mommy.mother.childs.Sum(
                           aunt => aunt == mommy ? 0 : aunt.GetSelfishGenesOutStrength(Relation.NephewOrNiece))
                       + daddy.mother.childs.Sum(
                           uncle => uncle == daddy ? 0 : uncle.GetSelfishGenesOutStrength(Relation.NephewOrNiece))
                       + mommy.mother.childs.Sum(
                           aunt =>
                           aunt == mommy
                               ? 0
                               : aunt.childs.Sum(cousin => cousin.GetSelfishGenesOutStrength(Relation.Cousin)))
                       + daddy.mother.childs.Sum(
                           uncle =>
                           uncle == daddy
                               ? 0
                               : uncle.childs.Sum(cousin => cousin.GetSelfishGenesOutStrength(Relation.Cousin)));
            }
        }

        /// <summary>
        ///     Mutates creature.
        /// </summary>
        public void Mutate()
        {
            // Tries to change 6 genes with 50% probability
            var length = this.genes.Length;
            var rnd = RandomProvider.GetThreadRandom().Next(length << 1);
            var limit = Math.Min(length, rnd + 6);
            for (; rnd < limit; rnd++)
            {
                this.genes[rnd] = EnumHelper.CreateRandomGene();
            }
        }

        /// <summary>
        ///     Breaks connection with unused grandparents.
        /// </summary>
        public void BreakRedundantConnections()
        {
            var mommy = this.mother;
            var daddy = this.father;
            if (mommy?.mother?.mother != null)
            {
                mommy.mother.mother?.childs.Clear();
                mommy.mother.mother = null;
                mommy.mother.father?.childs.Clear();
                mommy.mother.father = null;
                mommy.father.mother?.childs.Clear();
                mommy.father.mother = null;
                mommy.father.father?.childs.Clear();
                mommy.father.father = null;
                daddy.mother.mother?.childs.Clear();
                daddy.mother.mother = null;
                daddy.mother.father?.childs.Clear();
                daddy.mother.father = null;
                daddy.father.mother?.childs.Clear();
                daddy.father.mother = null;
                daddy.father.father?.childs.Clear();
                daddy.father.father = null;
            }
        }

        /// <summary>
        ///     Gets strength which is given to relation by this creature.
        /// </summary>
        /// <param name="whoAreYou">
        ///     Type of relation.
        /// </param>
        /// <returns>
        ///     The strength.
        /// </returns>
        private double GetSelfishGenesOutStrength(Relation whoAreYou)
        {
            var mommy = this.mother;
            var daddy = this.father;
            var summarySelfishStrength = this.genes.Sum(g => g == Gene.SelfishGene ? GeneStrength >> 1 : 0);
            switch (whoAreYou)
            {
                case Relation.Child:
                    return summarySelfishStrength / this.childs.Count * 30.78;
                case Relation.BrotherOrSister:
                    Debug.Assert(mommy.childs.Count > 1, "LIER! He is not our brother!");
                    return summarySelfishStrength / (mommy.childs.Count - 1) * 30.78;
                case Relation.GrandChild:
                    return summarySelfishStrength / this.childs.Sum(creature => creature.childs.Count) * 15.38;
                case Relation.NephewOrNiece:
                    Debug.Assert(mommy.childs.Count > 1, "LIER! We don't have any brothers!");
                    return summarySelfishStrength
                           / mommy.childs.Sum(brother => brother == this ? 0 : brother.childs.Count) * 15.38;
                case Relation.Cousin:
                    return summarySelfishStrength
                           / (mommy.mother.childs.Sum(aunt => aunt == mommy ? 0 : aunt.childs.Count)
                              + daddy.mother.childs.Sum(uncle => uncle == daddy ? 0 : uncle.childs.Count)) * 7.68;
                default:
                    throw new NotImplementedException("Unknown enum value");
            }
        }
    }
}