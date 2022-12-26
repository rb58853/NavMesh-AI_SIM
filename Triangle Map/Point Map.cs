﻿using System.Collections.Generic;
using Triangle_Map;
using BaseNode;
using DijkstraSpace;
using UnityEngine;

namespace Point_Map
{
    class PointNode : Node
    {

        PointNode end;
        Point point;
        public List<PointNode> adjacents { get; private set; }


        public PointNode(Point point, PointNode end = null)
        {
            this.point = point;
            adjacents = new List<PointNode>();
            distance = float.MaxValue;
            this.end = end;
        }
        public float get_x() { return point.x; }
        public float get_y() { return point.y; }
        public float get_z() { return point.z; }

        public void SetEnd(PointNode node) { end = node; }
        public void AddAdjacent(PointNode node) { adjacents.Add(node); }
        public float EuclideanDistance(PointNode node) { return node.point.Distance(point); }
        float Heuristic(PointNode endNode) { return EuclideanDistance(endNode); }
        public override float Value()
        {
            float g = distance;
            float h = Heuristic(end);

            return g + h;
        }
        public int CompareTo(PointNode other)
        {
            return this.value.CompareTo(other.value);
        }
        public override string ToString()
        {
            return point.ToString();
        }

        public override List<Node> GetAdyacents()
        {
            /// Esto en cuanto a eficiencia es malo, se esta sacrificando en eficiencia para ganar en genaricidad
            /// Cambiar el uso de lista por el uso de array en caso que deje usar el .toArray que no se cuanto cuesta tampoco
            List<Node> result = new List<Node>();
            foreach (Node adj in adjacents)
                result.Add(adj);
            return result;
        }

        public override float Distance(Node node)
        {
            return EuclideanDistance(node as PointNode);
        }


        internal class Static
        {
            public static List<PointNode> CreatePointMap(List<Arist> arists, Point init, Point end, float n = 1)
            {
                List<List<PointNode>> points = new List<List<PointNode>>();
                List<PointNode> result = new List<PointNode>();

                PointNode endNode = new PointNode(end);
                endNode.SetEnd(endNode);
                result.Add(endNode);

                PointNode initNode = new PointNode(init, endNode);
                result.Add(initNode);

                if (arists.Count == 0)
                    return result;

                foreach (Arist arist in arists)
                {
                    points.Add(new List<PointNode>());
                    foreach (Point point in arist.ToPoints(n))
                    {
                        PointNode node = new PointNode(point, endNode);
                        points[points.Count - 1].Add(node);
                        result.Add(node);
                    }
                }
                foreach (PointNode node in points[0])
                {
                    initNode.AddAdjacent(node);

                    //Vector3 a = new Vector3(initNode.point.x, initNode.point.y, initNode.point.z);
                    //Vector3 b = new Vector3(node.point.x, node.point.y, node.point.z);
                    //Debug.DrawLine(a, b, Color.red, 50f);
                }
                for (int i = 0; i < points.Count - 1; i++)
                {
                    for (int j = 0; j < points[i].Count; j++)
                    {
                        for (int k = 0; k < points[i + 1].Count; k++)
                        {
                            points[i][j].AddAdjacent(points[i + 1][k]);
                            //Vector3 a = new Vector3(points[i][j].point.x, points[i][j].point.y, points[i][j].point.z);
                            //Vector3 b = new Vector3(points[i + 1][k].point.x, points[i + 1][k].point.y, points[i + 1][k].point.z);
                            //Debug.DrawLine(a, b, Color.black, 50f);

                        }
                    }
                }
                foreach (PointNode node in points[points.Count - 1])
                {
                    node.AddAdjacent(endNode);
                    //Vector3 a = new Vector3(node.point.x, node.point.y, node.point.z);
                    //Vector3 b = new Vector3(endNode.point.x, endNode.point.y, endNode.point.z);
                    //Debug.DrawLine(a, b, Color.black, 50f);
                }
                return result;
            }


        }
    }

}
