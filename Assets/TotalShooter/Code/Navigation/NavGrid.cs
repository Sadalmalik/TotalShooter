using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sadalmalik.GridNavigation
{
    public static class NavGrid
    {
        private static List<NavGridNode> _allNodes;

        public static List<NavGridNode> AllNodes
        {
            get
            {
                if (_allNodes == null)
                    _allNodes = Object.FindObjectsByType<NavGridNode>(FindObjectsSortMode.None).ToList();
                return _allNodes;
            }
        }

        public static void Refresh()
        {
            _allNodes = null;
        }

        public static NavGridNode GetNearestNode(Vector3 position)
        {
            return AllNodes
                .OrderBy(n => Vector3.Distance(n.transform.position, position))
                .First();
        }

        public static List<NavGridNode> FindPath(NavGridNode start, NavGridNode end, int limit = 10000)
        {
            foreach (var node in NavGridNode.AllNodes)
                node.Marked = false;

            var nodes = new List<PathNode>();
            nodes.Add(new PathNode { value = start, fitness = Fitness(start, end) });
            PathNode endPathNode = null;

            while (limit-- > 0 && nodes.Count > 0)
            {
                nodes.Sort((a, b) => b.fitness.CompareTo(a.fitness));

                var last = nodes.Count - 1;
                var node = nodes[last];
                nodes.RemoveAt(last);

                node.value.Marked = true;

                if (node.value == end)
                {
                    endPathNode = node;
                    break;
                }

                foreach (var neibour in node.value.neibours)
                {
                    if (neibour.Marked)
                        continue;

                    neibour.Marked = true;
                    nodes.Add(new PathNode
                    {
                        next = node,
                        value = neibour,
                        fitness = Fitness(neibour, end)
                    });
                }
            }

            var steps = new List<NavGridNode>();
            while (endPathNode != null)
            {
                steps.Add(endPathNode.value);
                endPathNode = endPathNode.next;
            }
            steps.Reverse();

            return steps;
        }

        private static float Fitness(NavGridNode node, NavGridNode target)
        {
            return Vector3.Distance(node.transform.position, target.transform.position);
        }

        private class PathNode
        {
            public PathNode next = null;
            public float fitness = 0;
            public NavGridNode value = null;
        }
    }
}