using System;
using System.Collections.Generic;
using System.Diagnostics;

using GeometryFriends.AI.Perceptions.Information;

namespace GeometryFriendsAgents
{
    class SubgoalAStar
    {
        private const bool CLOSED_LIST = false;
        private const bool OPEN_LIST = true;

        public struct State
        {
            public Graph.Platform currentPlatform;
            public LevelRepresentation.Point currentPoint;
            public bool[] obtainedCollectibles;
            public int numObtainedCollectibles;
            public int totalCost;
            public List<Graph.Move> moveHistory;

            public State(Graph.Platform currentPlatform, LevelRepresentation.Point currentPoint, bool[] obtainedCollectibles, int numObtainedCollectibles, int totalCost, List<Graph.Move> moveHistory)
            {
                this.currentPlatform = currentPlatform;
                this.currentPoint = currentPoint;
                this.obtainedCollectibles = obtainedCollectibles;
                this.numObtainedCollectibles = numObtainedCollectibles;
                this.totalCost = totalCost;
                this.moveHistory = moveHistory;
            }
        }

        private Stopwatch sw;

        public SubgoalAStar()
        {
            sw = new Stopwatch();
        }

        public Graph.Move? CalculateShortestPath(Graph.Platform currentPlatform, LevelRepresentation.Point currentPoint, bool[] goalCollectibles, bool[] obtainedCollectibles, CollectibleRepresentation[] initialCollectibles)
        {
            sw.Restart();

            List<State> openList = new List<State>();
            List<State> closedList = new List<State>();

            openList.Add(new State(currentPlatform, currentPoint, obtainedCollectibles, 0, 0, new List<Graph.Move>()));
            bool[] reachableCollectibles = new bool[initialCollectibles.Length];
            int numReachableCollectibles = 0;

            State minCostState = new State();
            List<State> connectedStates;

            while(openList.Count != 0)
            {
                if (sw.ElapsedMilliseconds >= 500)
                {
                    sw.Stop();
                    return CalculateShortestPath(currentPlatform, currentPoint, DeleteLowestCollectibles(goalCollectibles, initialCollectibles), obtainedCollectibles, initialCollectibles);
                }

                minCostState = GetMinCostState(openList);

                MoveState(openList, closedList, minCostState);

                if(IsGoalState(minCostState, goalCollectibles))
                {
                    if (minCostState.moveHistory.Count > 0)
                    {
                        return minCostState.moveHistory[0];
                    }
                    else
                    {                           
                        return null;
                    }
                }

                connectedStates = GetConnectedStates(minCostState, ref reachableCollectibles, ref numReachableCollectibles);

                foreach(State i in connectedStates)
                {
                    SetLessCostState(i, ref openList, ref closedList, goalCollectibles);
                }
            }

            sw.Stop();
            return CalculateShortestPath(currentPlatform, currentPoint, reachableCollectibles, obtainedCollectibles, initialCollectibles);     
        }

        private State GetMinCostState(List<State> targetList)
        {
            State minState = new State();
            float min = float.MaxValue;

            foreach(State i in targetList)
            {
                if(min > i.totalCost)
                {
                    minState = i;
                    min = i.totalCost;
                }
            }

            return minState;
        }

        private void MoveState(List<State> fromList, List<State> toList, State targetState)
        {
            toList.Add(targetState);
            fromList.Remove(targetState);
        }

        private bool IsGoalState(State targetState, bool[] goalCollectibles)
        {
            for (int i = 0; i < goalCollectibles.Length; i++)
            {
                if (!targetState.obtainedCollectibles[i] && goalCollectibles[i])
                {
                    return false;
                }
            }

            return true;
        }
        
        private List<State> GetConnectedStates(State targetState, ref bool[] reachableCollectibles, ref int numReachableCollectibles)
        {
            List<State> connectedStates = new List<State>();

            
            int totalCost;
            List<Graph.Move> moveHistory;

            foreach (Graph.Move i in targetState.currentPlatform.moves)
            {
                bool[] obtainedCollectibles = new bool[targetState.obtainedCollectibles.Length];
                int numObtainedCollectibles = targetState.numObtainedCollectibles;

                for (int j = 0; j < obtainedCollectibles.Length; j++)
                {
                    obtainedCollectibles[j] = targetState.obtainedCollectibles[j] || i.collectibles_onPath[j];
                    reachableCollectibles[j] = reachableCollectibles[j] || i.collectibles_onPath[j];

                    if (!targetState.obtainedCollectibles[j] && i.collectibles_onPath[j])
                    {
                        numObtainedCollectibles++;
                    }
                }

                if (numObtainedCollectibles > numReachableCollectibles)
                {
                    obtainedCollectibles.CopyTo(reachableCollectibles, 0);
                }

                totalCost = targetState.totalCost + CalculateDistance(targetState.currentPoint, i.movePoint) + i.pathLength;
                moveHistory = new List<Graph.Move>(targetState.moveHistory);
                moveHistory.Add(i);

                connectedStates.Add(new State(i.reachablePlatform, i.landPoint, obtainedCollectibles, numObtainedCollectibles, totalCost, moveHistory));
            }

            return connectedStates;
        }

        private int CalculateDistance(LevelRepresentation.Point p1, LevelRepresentation.Point p2)
        {
            return (int)Math.Sqrt(Math.Pow(p1.x - p2.x, 2) + Math.Pow(p1.y - p2.y, 2));
        }

        private void SetLessCostState(State targetState, ref List<State> openList, ref List<State> closedList, bool[] goalCollectibles)
        {
            State sameState = new State();
            bool sameState_SetFlag = false;
            bool sameState_Location = CLOSED_LIST;

            foreach (State i in openList)
            {
                if(IsSameState(targetState, i, goalCollectibles))
                {
                    sameState_Location = OPEN_LIST;
                    sameState_SetFlag = true;
                    sameState = i;
                    break;
                }
            }

            if (!sameState_SetFlag)
            {
                foreach (State i in closedList)
                {
                    if (IsSameState(targetState, i, goalCollectibles))
                    {
                        sameState_Location = CLOSED_LIST;
                        sameState_SetFlag = true;
                        sameState = i;
                        break;
                    }
                }
            }

            if(sameState_SetFlag)
            {
                if(sameState.totalCost > targetState.totalCost)
                {
                    openList.Add(targetState);

                    if(sameState_Location)
                    {
                        openList.Remove(sameState);
                    }
                    else
                    {
                        closedList.Remove(sameState);
                    }
                }
            }
            else
            {
                openList.Add(targetState);
            }
        }

        private bool IsSameState(State s1, State s2, bool[] goalCollectibles)
        {
            int i;

            if(s1.currentPlatform.id == s2.currentPlatform.id)
            {
                if(!s1.currentPoint.Equals(s2.currentPoint))
                {
                    return false;
                }

                for(i = 0; i < goalCollectibles.Length; i++)
                {
                    if (goalCollectibles[i])
                    {
                        if (s1.obtainedCollectibles[i] ^ s2.obtainedCollectibles[i])
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            return false;
        }

        public bool[] DeleteLowestCollectibles(bool[] goalCollectibles, CollectibleRepresentation[] initialCollectibles)
        {
            float lowestHeight = float.MinValue;
            int lowestCollectibleID = 0;
            bool[] deletedCollectibles = new bool[goalCollectibles.Length];
            goalCollectibles.CopyTo(deletedCollectibles, 0);

            for (int i = 0; i < goalCollectibles.Length; i++)
            {
                if (goalCollectibles[i])
                {
                    if (lowestHeight < initialCollectibles[i].Y)
                    {
                        lowestHeight = initialCollectibles[i].Y;
                        lowestCollectibleID = i;
                    }
                }
            }

            deletedCollectibles[lowestCollectibleID] = false;

            return deletedCollectibles;
        }       
    }
}
