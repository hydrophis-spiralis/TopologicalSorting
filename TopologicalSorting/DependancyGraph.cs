﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TopologicalSorting
{
    /// <summary>
    /// A graph of processes and resources from which a topological sort can be extracted
    /// </summary>
    public class DependancyGraph
    {
        #region fields
        HashSet<OrderedProcess> processes = new HashSet<OrderedProcess>();
        /// <summary>
        /// Gets the processes which are part of this dependancy graph
        /// </summary>
        /// <value>The processes.</value>
        public IEnumerable<OrderedProcess> Processes
        {
            get
            {
                return processes;
            }
        }

        HashSet<Resource> resources = new HashSet<Resource>();
        /// <summary>
        /// Gets the resources which are part of this dependancy graph
        /// </summary>
        /// <value>The resources.</value>
        public IEnumerable<Resource> Resources
        {
            get
            {
                return resources;
            }
        }
        #endregion

        #region sorting
        /// <summary>
        /// Calculates the sort which results from this dependancy network
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown if no sort exists for the given set of constraints</exception>
        public TopologicalSort CalculateSort()
        {
            return CalculateSort(new TopologicalSort());
        }

        /// <summary>
        /// Append the result of this dependancy graph to the end of the given sorting solution
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns></returns>
        public TopologicalSort CalculateSort(TopologicalSort instance)
        {
            HashSet<OrderedProcess> unused = new HashSet<OrderedProcess>(processes);

            do
            {
                HashSet<OrderedProcess> set = new HashSet<OrderedProcess>(
                    unused.Where(p => p.Predecessors.Where(pre => unused.Contains(pre)).IsEmpty()) //select processes which have no predecessors in the unused set, which means that all their predecessors must either be used, or not exist, either way is fine
                );

                if (set.Count == 0)
                    throw new InvalidOperationException("Cannot order this set of processes");

                unused.ExceptWith(set);

                foreach (var subset in SolveResourceDependancies(set))
                    instance.Append(subset);
            }
            while (unused.Count > 0);

            return instance;
        }

        /// <summary>
        /// Given a set of processes which do not interdepend, split up into multiple sets which do not exceed available resource concurrency
        /// </summary>
        /// <param name="processes">The processes.</param>
        /// <returns></returns>
        private IEnumerable<IEnumerable<OrderedProcess>> SolveResourceDependancies(ISet<OrderedProcess> processes)
        {
            if (resources.Count == 0)
                yield return processes;
            else
            {
                HashSet<HashSet<OrderedProcess>> result = new HashSet<HashSet<OrderedProcess>>();

                foreach (var process in processes)
                {
                    //all sets this process may be added to
                    IEnumerable<HashSet<OrderedProcess>> agreeableSets = result                     //from the set of result sets
                        .Where(set => set                                                           //select a candidate set to add to
                            .Where(p => p.ResourcesSet.Overlaps(process.Resources))                        //select processes whose resource usage overlaps this one
                            .Count() == 0);                                                             //if there are none which overlap, then this is a valid set

                    //the single best set to add to
                    HashSet<OrderedProcess> agreeableSet = null;

                    if (agreeableSets.IsEmpty())
                    {
                        //no sets can hold this process, create a new one
                        agreeableSet = new HashSet<OrderedProcess>();
                        result.Add(agreeableSet);
                    }
                    else
                        agreeableSet = agreeableSets.Aggregate((a, b) => a.Count < b.Count ? a : b);    //pick the smallest set

                    //finally, add this process to the selected set
                    agreeableSet.Add(process);
                }

                foreach (var set in result)
                    yield return set;
            }
        }
        #endregion

        internal bool Add(OrderedProcess orderedProcess)
        {
            return processes.Add(orderedProcess);
        }

        internal bool Add(Resource resourceClass)
        {
            return resources.Add(resourceClass);
        }

        internal static void CheckGraph(OrderedProcess a, OrderedProcess b)
        {
            if (a.Graph != b.Graph)
                throw new ArgumentException(string.Format("process {0} is not assosciated with the same graph as process {1}", a, b));
        }

        internal static void CheckGraph(Resource a, OrderedProcess b)
        {
            if (a.Graph != b.Graph)
                throw new ArgumentException(string.Format("Resource {0} is not assosciated with the same graph as process {1}", a, b));
        }
    }
}
