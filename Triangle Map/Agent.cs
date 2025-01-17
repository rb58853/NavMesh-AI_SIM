﻿using System;
using System.Collections.Generic;
using BaseNode;
using DijkstraSpace;
using Point_Map;
using Triangle_Map;
using UnityEngine;

namespace Agent_Space
{
    public class Agent
    {
        System.Random random = new System.Random();
        public AgentType type { get; private set; }
        public string name { get; private set; }
        public List<MapNode> ocupedNodes { get; private set; }
        public List<Triangle> ocupedTriangles { get => UcupedTriangles(); }
        List<Triangle> UcupedTriangles()
        {
            List<Triangle> result = new List<Triangle>();
            foreach (MapNode node in ocupedNodes)
                result.Add(node.triangle);
            return result;
        }
        public float radius { get; private set; }

        public MapNode currentNode { get; private set; }
        public MapNode endMapNodeCurrent { get; private set; }
        public MapNode initMapNodeCurrent { get; private set; }
        public Point position { get; private set; }
        public PointNode currentPosition { get; private set; }
        public PointNode nextPosition { get; private set; }

        public List<MapNode> triangleList { get; private set; }
        private PointPath pointPath;
        public Point destination { get; private set; }
        public PointNode endPointNode { get; private set; }

        public Stack<Point> visualPath { get; private set; }
        public bool inMove { get; /*private*/ set; }
        internal bool metaPath = false;
        /// <summary> Compatibility of this Agent whit a material.</summary>
        public Dictionary<Material, float> compatibility;
        public bool grupalMove { get; private set; }
        public int posInGrup { get; private set; }
        public Agent(float radius, string name = "agent", AgentType type = AgentType.normal)
        {
            compatibility = new Dictionary<Material, float>();
            this.setType(type);
            pointPath = new PointPath(this, currentNode);
            this.name = name;
            visualPath = new Stack<Point>();
            ocupedNodes = new List<MapNode>();
            this.radius = radius;
            inMove = false;
        }
        void setType(AgentType type)
        {
            this.type = type;
            if (type == AgentType.acuatic)
            {
                SetCompatibility(Material.water, 6);
                SetCompatibility(Material.fire, 60);
            }
            if (type == AgentType.fire)
            {
                SetCompatibility(Material.fire, 6);
                SetCompatibility(Material.water, 60);
            }
            if (grupalMove)
            {
                if (Environment.Interactive.groupFromType.ContainsKey(this.type))
                    Environment.Interactive.groupFromType[this.type].Add(this);
                else
                {
                    Environment.Interactive.groupFromType.Add(this.type, new List<Agent>());
                    Environment.Interactive.groupFromType[this.type].Add(this);
                }
            }
        }
        public void setGrup(bool inGrup = true)
        {
            if (inGrup)
            {
                grupalMove = true;
                posInGrup = Environment.Interactive.grup.Count;
                Environment.Interactive.grup.Add(this);
            }
            else
            {
                grupalMove = false;
                if (Environment.Interactive.grup.Contains(this))
                    Environment.Interactive.grup.Remove(this);
            }
        }
        void SetCompatibility(Material material, float value)
        {
            if (compatibility.ContainsKey(material))
                compatibility[material] = value;
            else
                compatibility.Add(material, value);
        }
        void searchCurrentNode()
        {
            foreach (Node node in Environment.map.nodes)
                if ((node as MapNode).triangle.PointIn(position))
                {
                    currentNode = node as MapNode;
                    ocupedNodes.Add(currentNode);
                    currentNode.AddAgent(this);
                    pointPath.SetCurrentTriangle(currentNode);
                    SetOcupedFromPosition(Environment.ocupedArea);
                    break;
                }
            Agent.Collision(position, position, this, currentNode);
            pointPath.SetCurrentTriangle(currentNode);
        }
        public void setPosition(Point point)
        {
            position = point;
            currentPosition = new PointNode(position);
            searchCurrentNode();
        }
        void SetOcupedFromPosition(float extendArea = 1)
        {
            Queue<MapNode> ocuped = new Queue<MapNode>();

            ocuped.Enqueue(currentNode);

            if (!ocupedNodes.Contains(currentNode.origin))
            {
                ocupedNodes.Add(currentNode.origin);
                currentNode.origin.AddAgent(this);
            }

            for (int i = 0; i < ocupedNodes.Count; i++)
            {
                MapNode node = ocupedNodes[i].origin;
                if (node != currentNode.origin)
                {
                    if (position.DistanceToTriangle(node.triangle) > radius * extendArea)
                    {
                        node.RemoveAgent(this);
                        ocupedNodes.Remove(node.origin);
                        i -= 1;
                    }
                    else
                        ocuped.Enqueue(node.origin);
                }
            }

            while (ocuped.Count > 0)
            {
                MapNode node = ocuped.Dequeue();

                foreach (MapNode adj in node.adjacents.Keys)
                    if (position.DistanceToTriangle(adj.triangle) < radius + extendArea)
                        if (!ocupedNodes.Contains(adj))
                        {
                            ocuped.Enqueue(adj.origin);
                            ocupedNodes.Add(adj.origin);
                            adj.origin.AddAgent(this);
                        }
            }
            ///Tambien cada esta frecuencia separar a los agentes    
            Agent.Collision(position, position, this, currentNode);
        }
        Tuple<MapNode[], MapNode, MapNode> LocalMap(Point endPoint)
        {
            return BFS(endPoint);
        }
        Tuple<MapNode[], MapNode, MapNode> BFS(Point endPoint)
        {
            bool endWasFound = false;
            int countAfterFound = Environment.bfsArea;

            List<MapNode> localMap = new List<MapNode>();

            Dictionary<MapNode, MapNode> r = new Dictionary<MapNode, MapNode>();
            List<MapNode> visited = new List<MapNode>();//Mejorar eficiencia de esto, no me gusta

            Queue<MapNode> q = new Queue<MapNode>();

            MapNode end = null;

            currentNode = currentNode.origin;
            r.Add(currentNode, new MapNode(currentNode, this, endPoint));
            visited.Add(currentNode);

            localMap.Add(r[currentNode]);


            if (currentNode.triangle.PointIn(endPoint))
                ///If endPoint is in Current node, return only current node as path, init and endNode
                return new Tuple<MapNode[], MapNode, MapNode>(localMap.ToArray(), r[currentNode], r[currentNode]);

            q.Enqueue(currentNode);

            while (q.Count > 0)
            {
                if (endWasFound == true)
                {
                    countAfterFound--;
                    if (countAfterFound <= 0) break;
                }

                MapNode n = q.Dequeue();
                foreach (MapNode adj in n.adjacents.Keys)
                {
                    Arist aristClone = n.adjacents[adj].Clone();

                    if (!visited.Contains(adj))
                    {
                        visited.Add(adj);
                        MapNode temp = new MapNode(adj, this, endPoint);

                        if (adj.triangle.PointIn(endPoint))
                        {
                            end = temp;
                            endWasFound = true;
                        }

                        r.Add(adj, temp);
                        r[n].AddAdjacent(temp, aristClone);
                        temp.AddAdjacent(r[n], aristClone);
                        aristClone.AddTriangle(r[n]);
                        aristClone.AddTriangle(temp);
                        q.Enqueue(adj);

                        localMap.Add(temp);
                    }
                    else
                    {
                        if (!r[n].adjacents.ContainsKey(r[adj]))
                        {
                            r[n].AddAdjacent(r[adj], aristClone);
                            r[adj].AddAdjacent(r[n], aristClone);
                            aristClone.AddTriangle(r[n]);
                            aristClone.AddTriangle(r[adj]);
                        }
                    }
                }
            }

            if (end == null)
                localMap = new List<MapNode>();

            return new Tuple<MapNode[], MapNode, MapNode>(localMap.ToArray(), r[currentNode], end);
        }
        DateTime initCreatePath;
        public MapNode[] GetTrianglePath(Point endPoint, bool push = true)
        {
            Tuple<MapNode[], MapNode, MapNode> localMap = LocalMap(endPoint);
            Node[] nodes = localMap.Item1;
            Node init = localMap.Item2;
            Node end = localMap.Item3;

            endMapNodeCurrent = end as MapNode;
            initMapNodeCurrent = init as MapNode;

            if (end == null)
                return null;

            PointNode endPointNode = new PointNode(endPoint, agent: this);
            endPointNode.SetDistance(0);

            PointNode tempPosition = new PointNode(position);
            tempPosition.SetDistance(currentPosition.distance);
            DateTime t0 = DateTime.Now;

            if (Environment.metaheuristic && endMapNodeCurrent.origin != initMapNodeCurrent.origin)
                if (/*random.Next(0, 5) != 0 &&*/
                Metaheuristic.Path(initMapNodeCurrent, endMapNodeCurrent, endPointNode, tempPosition, this))
                {
                    Debug.Log("En encontrar el camino meta demora " + (DateTime.Now - t0));

                    metaPath = true;
                    if (!endPointNode.triangles.Contains(endMapNodeCurrent))
                        endPointNode.AddTriangle(endMapNodeCurrent);
                    this.endPointNode = endPointNode;

                    tempPosition.AddTriangle(initMapNodeCurrent);

                    this.pointPath.pushMetaMap(tempPosition);
                    return null;
                }
            initCreatePath = DateTime.Now;
            // this.currentPosition.AddTriangle(currentNode);
            endPointNode.AddTriangle(endMapNodeCurrent);
            this.endPointNode = endPointNode;

            Dijkstra dijkstra = new Dijkstra(end, init, nodes);
            if (grupalMove)
            {
                if (Environment.Interactive.groupFromType.ContainsKey(type))
                {
                    List<MapNode> grup = new List<MapNode>();
                    foreach (Agent agent in Environment.Interactive.groupFromType[type])
                        grup.Add(agent.currentNode.origin);
                    dijkstra = new Dijkstra(end, init, nodes, grup);
                }
            }

            List<Node> path = dijkstra.GetPath();

            pointPath.PushCurrenTriangle(initMapNodeCurrent);
            // if (!grupalMove)
            DilatePath();
            triangleList = tools.ToListAsMapNode(path);

            MapNode[] result = tools.ToArrayAsMapNode(path);
            return result;

            void DilatePath()
            {
                int dilate = Environment.trianglePathDilatation;

                while (dilate > 0)
                {
                    dilate--;
                    MapNode temp = path[path.Count - 1] as MapNode;
                    Node[] array = path.ToArray();

                    path.Remove(temp);

                    foreach (MapNode node in array)
                        foreach (MapNode adj in node.adjacents.Keys)
                            if (!path.Contains(adj) && adj != temp)
                                path.Add(adj);

                    path.Add(temp);
                }

                // foreach (MapNode node in path.ToArray())
                //     if (node != path[0] && node != path[path.Count - 1])
                //         if (OnlyOneAdj(node))
                //             path.Remove(node);

                // bool OnlyOneAdj(MapNode triangle)
                // {
                //     int countAdj = 0;
                //     foreach (MapNode adj in triangle.adjacents.Keys)
                //         if (path.Contains(adj))
                //             countAdj++;

                //     return countAdj <= 1;
                // }
            }
        }
        public PointNode[] GetPointPath(Point endPoint)
        {
            MapNode[] tPath = GetTrianglePath(endPoint);
            if (metaPath == true) return null;

            if (tPath == null)
            {
                this.pointPath.PushPointMap(new PointNode[1] { new PointNode(position) });
                return new PointNode[1] { new PointNode(position) };///Debugguer
            }

            float density = Environment.densityPath;
            float mCost = currentNode.MaterialCost(this);

            List<PointNode> mapPoints = PointNode.Static.CreatePointMap(this.endPointNode, position, this, density, mCost);

            ///MapPoints[0] = initNode
            ///MapPoints[MapPoints.Count-1] = endNode
            Dijkstra dijkstra = new Dijkstra(mapPoints[0], mapPoints[mapPoints.Count - 1], mapPoints.ToArray());
            List<Node> pointPath = dijkstra.GetPath(false);

            initMapNodeCurrent.triangle.draw(Color.black);
            endPointNode = mapPoints[0];
            this.pointPath.PushPointMap(endPointNode, mapPoints[mapPoints.Count - 1], initMapNodeCurrent);

            Debug.Log("En crear el camino demora " + (DateTime.Now - initCreatePath));
            return tools.ToArrayAsPointNode(pointPath);///Debugguer
        }
        public void SetPointPath(Point point)
        {
            if (grupalMove)
            {
                if (Environment.Interactive.allGroupInMove == false)
                    foreach (Agent agent in Environment.Interactive.grup)
                        agent.inMove = true;
                Environment.Interactive.allGroupInMove = true;
                Environment.Interactive.countInStop = 0;
                inMoveGroup = true;
            }
            metaPath = false;
            pointPath.clear();
            pointPath.Move();
            destination = point;
            GetPointPath(point);
            if (metaPath == false)
            {
                PointNode tempPosition = new PointNode(position);
                tempPosition.SetDistance(currentPosition.distance);
                foreach (MapNode triangle in currentPosition.triangles)
                    tempPosition.AddTriangle(triangle);

                if (endMapNodeCurrent != initMapNodeCurrent && endMapNodeCurrent != null)
                    Metaheuristic.Proccess(endMapNodeCurrent, point, triangleList,
                    endPointNode, currentNode, tempPosition, this);
                currentPosition = tempPosition;
            }
            NextPoint();
        }
        bool inMoveGroup = true;
        void setInMoveGrupal()
        {
            if (!grupalMove) return;
            if (position.Distance(destination) < Environment.Interactive.distanceToStop * radius * Math.Sqrt(Environment.Interactive.countInStop))
            {
                inMoveGroup = false;
                inMove = false;
                Environment.Interactive.allGroupInMove = false;
                Environment.Interactive.countInStop += 1;
            }

            // foreach (Agent agent in Environment.Interactive.grup)
            //     if (agent.inMove == false)
            //     {
            // if (position.Distance(agent.position, false) < 3 * (radius + agent.radius))
            // {
            //     inMove = false;
            //     Environment.Interactive.allGroupInMove = false;
            //     Environment.Interactive.countInStop += 1;
            //     break;
            // }
            //     }
        }
        void DynamicSetPoint()
        {
            float dist = radius * Environment.viewLenAgent;
            Point pointDest = position + Point.VectorUnit(position, nextPosition.point) * dist;
            if (Environment.drawView)
                PointNode.Static.DrawTwoPoints(position, pointDest, Color.red);

            Tuple<bool, Agent> collision =
            Collision(position, pointDest, this, ocupedNodes, multArea: 1,
             maxDistance: Environment.distanceAnalizeCollision);
            if (collision.Item1)
                NextPoint(true);
        }

        int countMoves = 1;
        public void NextMove(int n = 1)
        {
            for (int i = 0; i < n; i++)
                NextMoveBasic();

            countMoves--;
            if (countMoves <= 0)
            {
                countMoves = 1;
                SetOcupedFromPosition(Environment.ocupedArea);
            }
        }

        int freq = Environment.freqReview;
        private int stopCount = Environment.stopCountForEmpty;
        void NextMoveBasic()
        {
            if (pointPath.stop && inMove)
            {
                pointPath.EmptyMove();
                if (!pointPath.stop)
                {
                    NextPoint();
                    if (pointPath.stop)
                    {
                        stopCount--;
                        if (stopCount <= 0)
                        {
                            SetPointPath(position);/// Quitarse esto en algun momento
                            // inMove = false;
                        }
                    }
                    else
                        stopCount = Environment.stopCountForEmpty;
                }
            }

            if (inMove && !pointPath.stop)
            {
                if (freq <= 0 && !pointPath.stop && inMove)
                {
                    // freq = random.Next(1, Environment.freqReview);
                    freq = Environment.freqReview;
                    DynamicSetPoint();
                }
                freq--;

                Point temp = position;
                int count = visualPath.Count;
                if (visualPath.Count == 0) NextPoint();
                try
                {
                    if (inMove && !pointPath.stop)
                    {
                        position = visualPath.Pop();
                        // currentPosition = new PointNode(position);///Nuevo
                    }
                }
                catch { Debug.Log("Error: la pila tiene " + visualPath.Count + " elementos y esta intentando hacer Pop()."); }
                // if (temp.Distance(position, false) > 0.6f)
                // {
                //     Debug.Log("Salto desde " + temp + " hasta " + position +
                //     "  cuando el count del visual path es " + count);
                //     PointNode.Static.DrawTwoPoints(temp, position, Color.green);
                // }
            }
        }

        void NextPoint(bool onCollision = false)
        {
            visualPath.Clear();
            if (!pointPath.empty || !inMoveGroup)
            {
                inMove = true;
                nextPosition = pointPath.Pop(onCollision);
                currentPosition = pointPath.currentPoint;

                setInMoveGrupal();
                SetCurrentTriangle();

                if (nextPosition == currentPosition)
                {
                    visualPath.Push(currentPosition.point);
                    // NextMoveBasic();
                    return;
                }
                // float cost = currentPosition.adjacents[nextPosition] * 25;
                float cost = currentNode.MaterialCost(this) * Environment.densityVisualPath;

                List<Point> temp = new Arist(currentPosition.point, nextPosition.point).ToPoints(cost);
                for (int i = temp.Count - 1; i >= 0; i--)
                {
                    if (i > 0)
                        if (temp[i].Distance(temp[i - 1], false) > 0.2f)
                            Debug.Log("se creo un punto largo que no va");
                    visualPath.Push(temp[i]);
                }
            }
            else
            {
                Environment.Interactive.countInStop += 1;
                inMove = false;
            }
            SetOcupedFromPosition(Environment.ocupedArea);
        }
        void SetCurrentTriangle()
        {
            currentNode = pointPath.currentTriangle;
        }
        public static Tuple<bool, Agent> Collision(Point node1, Point node2, Agent agent, MapNode mapNode, float multArea = 1,
        float maxDistance = 500f)
        {
            Point l1 = node1;
            Point l2 = node2;
            if (!Environment.exactCollision)
            {
                l1 = l1 + Point.VectorUnit(l2 - l1) * agent.radius;
                // if (l1.Distance(node1, false) > l2.Distance(node1, false))
                //     l1 = l2;
            }
            float epsilon = 0.005f;

            Tuple<bool, Agent> result = new Tuple<bool, Agent>(false, null);

            foreach (Agent agentObstacle in mapNode.origin.agentsIn)
            {
                if (agentObstacle == agent) continue;
                if (agentObstacle.position.Distance(agent.position) > (agent.radius + agentObstacle.radius) * maxDistance) continue;

                if (agentObstacle.position.DistanceToSegment(l1, l2) <= (agent.radius + agentObstacle.radius) * multArea + epsilon)
                    /// Collision
                    if (result.Item2 == null ||
                        agentObstacle.position.Distance(node1, false) <
                        result.Item2.position.Distance(node1, false))///mas cercano

                        result = new Tuple<bool, Agent>(true, agentObstacle);
            }
            if (result.Item1)
            {
                float distance = result.Item2.position.Distance(agent.position, false);
                float radius = result.Item2.radius + agent.radius;
                if (distance <= radius * multArea + epsilon)
                {
                    ///Choque
                    Point vector = Point.VectorUnit(result.Item2.position, agent.position) * ((radius - distance) / 2 + epsilon);

                    agent.position = agent.position + vector * 1.1f;
                    Collision(node1, node2, agent, mapNode);/// Es lo que debe
                }
            }

            return result;
        }
        public static Tuple<bool, Agent> Collision(Point node1, Point node2, Agent agent, ICollection<MapNode> mapNodes, float multArea = 1,
        float maxDistance = 500f)
        {
            foreach (MapNode node in mapNodes)
            {
                Tuple<bool, Agent> collision = Collision(node1, node2, agent, node, multArea, maxDistance);
                if (collision.Item1)
                    return collision;
            }
            return new Tuple<bool, Agent>(false, null);
        }
        public override string ToString() { return name; }
        internal class tools
        {
            internal static MapNode[] ToArrayAsMapNode(List<Node> list)
            {
                MapNode[] result = new MapNode[list.Count];
                for (int i = 0; i < list.Count; i++)
                    result[i] = list[i] as MapNode;
                return result;
            }
            internal static List<MapNode> ToListAsMapNode(List<Node> list)
            {
                List<MapNode> result = new List<MapNode>();
                for (int i = 0; i < list.Count; i++)
                    result.Add(list[i] as MapNode);
                return result;
            }
            internal static PointNode[] ToArrayAsPointNode(List<Node> list)
            {
                PointNode[] result = new PointNode[list.Count];
                for (int i = 0; i < list.Count; i++)
                    result[i] = list[i] as PointNode;
                return result;
            }
        }
        internal static class Metaheuristic
        {

            public static Dictionary<AgentType, Dictionary<Triangle, Dictionary<MapNode, MapNode>>> originsFromType
            = new Dictionary<AgentType, Dictionary<Triangle, Dictionary<MapNode, MapNode>>>();

            public static bool Path(MapNode initTriangle, MapNode endTriangle,
            PointNode endPoint, PointNode initPoint, Agent agent)
            {
                if (!originsFromType.ContainsKey(agent.type))
                    originsFromType.Add(agent.type, new Dictionary<Triangle, Dictionary<MapNode, MapNode>>());
                Dictionary<Triangle, Dictionary<MapNode, MapNode>> origins = originsFromType[agent.type];

                if (origins == null || initPoint == null) return false;
                // initPoint.adjacents.Clear();
                initPoint.SetDistance(float.MaxValue);

                foreach (Triangle triangle in endTriangle.origin.triangle.trianglesSub)
                {
                    if (triangle.PointIn(endPoint.point))
                    {
                        if (origins.ContainsKey(triangle))
                            if (origins[triangle].ContainsKey(initTriangle.origin))
                            {
                                Dictionary<MapNode, MapNode> originsNodes = origins[triangle];

                                initTriangle = originsNodes[initTriangle.origin];
                                endTriangle = originsNodes[endTriangle.origin];

                                foreach (Arist arist in initTriangle.adjacents.Values)
                                    foreach (PointNode point in arist.points)
                                        initPoint.AddAdjacent(point, initTriangle.MaterialCost(agent));

                                foreach (Arist arist in endTriangle.adjacents.Values)
                                    foreach (PointNode point in arist.points)
                                        point.AddAdjacent(endPoint, endTriangle.MaterialCost(agent));

                                return true;
                            }
                        return false;
                    }
                }
                return false;
            }
            public static void Proccess(MapNode endTriangle, Point endPoint, List<MapNode> nodes,
            PointNode endPointNode, MapNode initMapNode, PointNode initPointNode, Agent agent)
            {
                if (!Environment.metaheuristic) return;
                foreach (Triangle triangle in endTriangle.origin.triangle.trianglesSub)
                {
                    if (triangle.PointIn(endPoint))
                    {
                        Merge(triangle, nodes, endPointNode, endTriangle, initPointNode, agent);
                        move.triangle = triangle;
                        triangle.draw(Color.green);
                        return;
                    }
                }

            }
            static void Merge(Triangle triangle, List<MapNode> inputNodes,
            PointNode endPointNode, MapNode endMapNode, PointNode initPointNode, Agent agent)
            {
                if (!originsFromType.ContainsKey(agent.type))
                    originsFromType.Add(agent.type, new Dictionary<Triangle, Dictionary<MapNode, MapNode>>());
                Dictionary<Triangle, Dictionary<MapNode, MapNode>> origins = originsFromType[agent.type];

                List<MapNode> tempInputNodes = new List<MapNode>();

                MapNode initMapNode = inputNodes[inputNodes.Count - 1];

                if (origins.ContainsKey(triangle))
                {
                    Dictionary<MapNode, MapNode> originsNodes = origins[triangle];

                    foreach (MapNode node in inputNodes)
                        tempInputNodes.Add(node);

                    #region Delete Triangles whit same origin
                    foreach (MapNode node in inputNodes)
                        if (originsNodes.ContainsKey(node.origin))
                        {
                            tempInputNodes.Remove(node);
                            MapNode localNode = originsNodes[node.origin];

                            foreach (Arist inputArist in node.adjacents.Values)
                                foreach (Arist localArist in localNode.adjacents.Values)
                                    if (inputArist.origin == localArist.origin)
                                    {
                                        ///Actualizar para la mejor distancia por todos los puntos del triangulo
                                        for (int i = 0; i < localArist.points.Count; i++)
                                        {
                                            float min = Math.Min(localArist.points[i].distance, inputArist.points[i].distance);
                                            localArist.points[i].SetDistance(min);
                                        }
                                        break;
                                    }

                            if (node == endMapNode)
                            {
                                MapNode localEnd = originsNodes[node.origin];

                                foreach (Arist arist in localEnd.adjacents.Values)
                                    foreach (PointNode point in arist.points)
                                        ///Crear adyacencia hacia el nuevo nodo final
                                        point.AddAdjacent(endPointNode);
                            }

                            if (node == initMapNode)                                //
                            {//
                                initPointNode.adjacents.Clear();//
                                MapNode localInit = originsNodes[node.origin];//
                                foreach (Arist arist in localInit.adjacents.Values)//
                                    foreach (PointNode point in arist.points)//
                                    {//
                                        ///Crear adyacencia desde el nodo inicio
                                        initPointNode.AddAdjacent(point);//
                                        Point p = new Point(-0.05f, 0, 0);//
                                    }//
                            }//
                        }
                    #endregion
                    #region Create adj in news arists and triangles
                    foreach (MapNode inputNode in tempInputNodes)
                    {
                        List<Node> adjacentsOfInputMapNode = inputNode.GetAdyacents();
                        foreach (MapNode adj in adjacentsOfInputMapNode)
                        {
                            if (originsNodes.ContainsKey(adj.origin))
                            {
                                MapNode localNodeTemp = originsNodes[adj.origin];

                                List<Arist> addArists = new List<Arist>();
                                List<Arist> deleteArists = new List<Arist>();

                                foreach (MapNode inputNodeAdj in inputNode.GetAdyacents())
                                    foreach (MapNode localNodeAdj in localNodeTemp.adjacents.Keys)
                                    {
                                        Arist inputArist = inputNode.adjacents[inputNodeAdj];
                                        Arist localArist = localNodeTemp.adjacents[localNodeAdj];
                                        if (inputArist.origin == localArist.origin)
                                        {
                                            ///Crear la adyacencia entre triangulos y remover las obsoletas
                                            inputNode.adjacents.Remove(inputNodeAdj);
                                            if (!inputNode.adjacents.ContainsKey(localNodeAdj))
                                                inputNode.adjacents.Add(localNodeAdj, localArist);


                                            addArists.Add(localArist);
                                            deleteArists.Add(inputArist);
                                            for (int i = 0; i < localArist.points.Count; i++)
                                            {
                                                ///Actualizar para la mejor distancia
                                                float min = Math.Min(localArist.points[i].distance, inputArist.points[i].distance);
                                                localArist.points[i].SetDistance(min);
                                            }
                                            break;
                                        }
                                    }
                                foreach (Arist aristAdd1 in addArists)
                                    foreach (Arist aristAdd2 in addArists)
                                        if (aristAdd1 != aristAdd2)
                                            foreach (PointNode point1 in aristAdd1.points)
                                                foreach (PointNode point2 in aristAdd2.points)
                                                {
                                                    /// Caso que una arista cierra un triangulo
                                                    point1.AddAdjacent(point2);
                                                    point2.AddAdjacent(point1);
                                                    point1.AddTriangle(inputNode);
                                                    point2.AddTriangle(inputNode);
                                                    // PointNode.Static.DrawTwoPoints(point1.point, point2.point, Color.magenta);
                                                }

                                foreach (Arist aristOfInputNode in inputNode.adjacents.Values)
                                {
                                    if (deleteArists.Contains(aristOfInputNode))
                                    {
                                        foreach (PointNode pointToDeleteAdj in aristOfInputNode.points)
                                        {
                                            List<Node> temp = pointToDeleteAdj.GetAdyacents();
                                            foreach (PointNode adjOfDeletePoint in temp)//
                                            {
                                                adjOfDeletePoint.RemoveAdjacent(pointToDeleteAdj);
                                                pointToDeleteAdj.RemoveAdjacent(adjOfDeletePoint);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        foreach (PointNode pointInputNode in aristOfInputNode.points)
                                            foreach (Arist localArist in addArists)
                                                foreach (PointNode pointInLocal in localArist.points)
                                                {
                                                    if (!pointInputNode.adjacents.ContainsKey(pointInLocal))
                                                        pointInputNode.AddAdjacent(pointInLocal, inputNode.MaterialCost(agent));
                                                    if (!pointInLocal.adjacents.ContainsKey(pointInputNode))
                                                        pointInLocal.AddAdjacent(pointInputNode, inputNode.MaterialCost(agent));

                                                    if (inputNode == initMapNode)
                                                        if (!initPointNode.adjacents.ContainsKey(pointInLocal))
                                                        {
                                                            initPointNode.AddAdjacent(pointInLocal, inputNode.MaterialCost(agent));
                                                            if (!initPointNode.triangles.Contains(inputNode))
                                                                pointInLocal.AddTriangle(inputNode);
                                                            // PointNode.Static.DrawTwoPoints(initPointNode.point, pointInLocal.point, Color.red);

                                                        }

                                                    // PointNode.Static.DrawTwoPoints(pointInputNode.point, pointInLocal.point, Color.white);
                                                    if (!pointInLocal.triangles.Contains(inputNode))
                                                        pointInLocal.AddTriangle(inputNode);
                                                }
                                    }
                                }
                            }
                        }
                    }
                    #endregion
                    foreach (MapNode inputNode in tempInputNodes)
                        originsNodes.Add(inputNode.origin, inputNode);
                }
                else
                {
                    Dictionary<MapNode, MapNode> originNew = new Dictionary<MapNode, MapNode>();
                    foreach (MapNode node in inputNodes)
                        originNew.Add(node.origin, node);
                    origins.Add(triangle, originNew);
                }
            }
        }
    }
}