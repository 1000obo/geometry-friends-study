using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;

using GeometryFriends.AI;
using GeometryFriends.AI.Interfaces;
using GeometryFriends.AI.Perceptions.Information;

namespace GeometryFriendsAgents
{
    /// <summary>
    /// A circle agent implementation for the GeometryFriends game that demonstrates prediction and history keeping capabilities.
    /// </summary>
    public class CircleAgent : AbstractCircleAgent
    {
        //Sensors Information
        private LevelRepresentation levelInfo;
        private CircleRepresentation circleInfo;
        private RectangleRepresentation rectangleInfo;

        // Graph
        private GraphCircle graph;

        // Search Algorithm
        private SubgoalAStar subgoalAStar;

        // Reinforcement Learning
        private ActionSelector actionSelector;

        //Agent
        private GameInfo.AgentBehaviour agentBehaviour;
        private bool agentAdaptive;

        //Logs and Screen Capture
        private Logger logger;
        private GameInfo.CooperationStatus cooperationStatus;
        private String collectiblesLeft = "";
        private ScreenRecorder recorder;
        private bool startRecording = true;

        // Auxiliary Variables
        private Moves currentAction;
        private Moves currentActionRect;
        float prevRectX;
        float prevHeight;
        private Graph.Move? nextMove;
        private DateTime lastMoveTime;
        private Graph.Platform? previousPlatform, currentPlatform;
        private int leftBound;
        private int rightBound;
        //Auxiliary Collectibles Variables
        private int previousCollectiblesLen;
        private int[] collectiblesOrder = { 0, 1, 2 }; //HARDCODED LOL
        private List<int> collectiblesCollected = new List<int>();
        private int flagStepCollectible = 0;
        private float obstacleCollectible = -1;
        //Auxiliary Adaptive Behaviour Variables
        private int timerAdaptive;
        private int timerAdaptiveCollectible;

        public CircleAgent()
        {
            nextMove = null;
            currentPlatform = null;
            previousPlatform = null;
            lastMoveTime = DateTime.Now;
            timerAdaptive = 0;
            timerAdaptiveCollectible = 0;

            currentAction = Moves.NO_ACTION;
            currentActionRect = Moves.NO_ACTION;
            cooperationStatus = GameInfo.CooperationStatus.NOT_COOPERATING;
            agentBehaviour = GameInfo.AgentBehaviour.LEADER; //CHANGE HERE: FOLLOWER OR LEADER
            agentAdaptive = true; //CHANGE HERE: ADAPTIVE CAN BE TRUE OR FALSE

            graph = new GraphCircle();
            subgoalAStar = new SubgoalAStar();
            actionSelector = new ActionSelector();
            levelInfo = new LevelRepresentation();

            logger = new Logger();
            recorder = new ScreenRecorder();
        }

        //implements abstract circle interface: used to setup the initial information so that the agent has basic knowledge about the level
        public override void Setup(CountInformation nI, RectangleRepresentation rI, CircleRepresentation cI, ObstacleRepresentation[] oI, ObstacleRepresentation[] rPI, ObstacleRepresentation[] cPI, CollectibleRepresentation[] colI, Rectangle area, double timeLimit)
        {
            // Create Level Array
            levelInfo.CreateLevelArray(colI, oI, rPI, cPI);

            // Create Graph
            graph.initialRectangleInfo = rI;
            graph.SetupGraph(levelInfo.GetLevelArray(), colI.Length);
            graph.SetPossibleCollectibles(cI);

            if (rI.X < 0 || rI.Y < 0)
            {
                graph.DeleteCooperationPlatforms();
            }

            // Initial Information
            circleInfo = cI;
            rectangleInfo = rI;
            prevRectX = rectangleInfo.X;
            prevHeight = rectangleInfo.Height;
            previousCollectiblesLen = levelInfo.collectibles.Length;

            //Logger first line
            LogsCollectibles();
            logger.Log("Setup", agentBehaviour.ToString(), cooperationStatus.ToString(), currentAction.ToString(), currentActionRect.ToString(), cI.X, cI.Y, rI.X, rI.Y, collectiblesLeft);

            // Set level bounds
            ComputeLevelBounds();
        }

        
        //implements abstract circle interface: registers updates from the agent's sensors that it is up to date with the latest environment information
        /*WARNING: this method is called independently from the agent update - Update(TimeSpan elapsedGameTime) - so care should be taken when using complex 
         * structures that are modified in both (e.g. see operation on the "remaining" collection)      
         */
        public override void SensorsUpdated(int nC, RectangleRepresentation rI, CircleRepresentation cI, CollectibleRepresentation[] colI)
        {
            circleInfo = cI;
            rectangleInfo = rI;
            levelInfo.collectibles = colI;
        }

        //implements abstract circle interface: signals if the agent is actually implemented or not
        public override bool ImplementedAgent()
        {
            return true;
        }

        //implements abstract circle interface: provides the name of the agent to the agents manager in GeometryFriends
        public override string AgentName()
        {
            return "IST Circle";
        }

        //implements abstract circle interface: GeometryFriends agents manager gets the current action intended to be actuated in the enviroment for this agent
        public override Moves GetAction()
        {
            return currentAction;
        }

        /*Agent Behaviour: LEADER mode + stable behaviour*/
        private void Leader(bool increment = false)
        {
            //Get current platform in contact with circle
            currentPlatform = graph.GetPlatform(new LevelRepresentation.Point((int)circleInfo.X, (int)circleInfo.Y), GameInfo.MAX_CIRCLE_HEIGHT, (int)circleInfo.VelocityY);

            if (!currentPlatform.HasValue) //Fix for circle not to jump eternally
            {              
                currentPlatform = previousPlatform;
            }

            if (currentPlatform.HasValue) //Circle touching platform/floor
            {
                bool isDifferentPlatform = IsDifferentPlatform();
                bool isGetCollectible = IsGetCollectible();
                if (isDifferentPlatform || isGetCollectible) //Just changed platform or collected diamond
                {
                    SetNextEdge(); //find new graph edges/next move
                }

                if (nextMove.HasValue)
                {
                    bool cooperating = IsRidingRectangle(); //check if circle is riding rectangle       
                    int collectibleToGet = GetCollectibleToGet(); //get target collectible

                    if (collectibleToGet == -1) //in case only the individual collectible for the rectangle is left
                    {
                        currentAction = actionSelector.GetCurrentAction(circleInfo, (int)circleInfo.X, 0, false);
                        return;
                    }

                    if (!cooperating) //circle not riding rectangle
                    {
                        if (circleInfo.Y > levelInfo.initialCollectibles[collectibleToGet].Y) //check if circle is not positioned higher than the target collectible
                        {
                            int movePointX = (int)levelInfo.initialCollectibles[collectibleToGet].X; //circle moves to target collectible position
                            bool rightMove = (movePointX >= circleInfo.X) ? true : false;

                            int possibleObstacle = FindObstacleInTheWay(movePointX, rightMove, (int) levelInfo.initialCollectibles[collectibleToGet].Y); //obstacle is in the circle's way to the target collectible

                            if (possibleObstacle != -1)
                            {
                                movePointX = possibleObstacle; //circle moves close to obstacle near target collectible position
                                rightMove = (movePointX >= circleInfo.X) ? true : false;
                            }

                            bool reachedGoal = actionSelector.IsGoal(circleInfo, movePointX, 0, true);

                            if (!reachedGoal)
                            {

                                if (IsRectangleInTheWay(movePointX, rightMove)) //rectangle is in the circle's way to the desired goal
                                {
                                    int multWidth = rightMove ? -1 : 1;
                                    //new move point depends on rectangle width, circle radius, and height difference of these elements
                                    int addMovePoint = (int)(multWidth * ((GameInfo.RECTANGLE_AREA / rectangleInfo.Height) / 2 + circleInfo.Radius + Math.Abs(rectangleInfo.Y - circleInfo.Y) / 2));
                                    //circle moves to position to jump to rectangle
                                    int goalMovePoint = (int)rectangleInfo.X + addMovePoint;

                                    currentAction = actionSelector.GetCurrentAction(circleInfo, goalMovePoint, GameInfo.JUMP_VELOCITYX / 2, rightMove);
                                    if (!actionSelector.IsGoal(circleInfo, goalMovePoint, GameInfo.JUMP_VELOCITYX / 2, rightMove))
                                    {
                                        return;
                                    }
                                    else
                                    {
                                        currentAction = Moves.JUMP; //if circle reached its goal starts jumping to jump to rectangle                              
                                    }

                                    //Increment timer close to move point - adaptive behaviour
                                    if (Math.Abs(circleInfo.X - ((int)rectangleInfo.X + addMovePoint)) < GameInfo.MIN_DIST_RECTANGLEX)
                                    {
                                        if (increment)
                                        {
                                            timerAdaptive += 1;
                                        }
                                    }

                                }
                                else //rectangle is not in the way so circle moves normally to move point
                                {
                                    currentAction = actionSelector.GetCurrentAction(circleInfo, movePointX, 0, true);
                                }

                            }
                            else //if circle reached its goal starts jumping to communicate they want to collect that specific diamond
                            {
                                currentAction = Moves.JUMP;
                            }

                            //Increment timer close to move point - adaptive behaviour                          
                            if (increment)
                            {
                                if (Math.Abs(circleInfo.X - movePointX) < GameInfo.MIN_DIST_RECTANGLEX)
                                {
                                    timerAdaptive += 1;
                                }
                            }
                            

                        }
                        else // cirle is positioned higher than the target collectible - normal behaviour
                        {
                            currentAction = actionSelector.GetCurrentAction(circleInfo, nextMove.Value.movePoint.x, nextMove.Value.velocityX, nextMove.Value.rightMove);
                        }
                    }

                    else //circle riding rectangle
                    {

                        float diffObstacle = FindObstacleUnder(levelInfo.initialCollectibles[collectibleToGet]);
                        float diffCollectible = diffObstacle == -1 ? levelInfo.initialCollectibles[collectibleToGet].X : diffObstacle;
                        float obstacleCollectible = Math.Abs(diffCollectible - rectangleInfo.X);

                        int movePointX;

                        //check if rectangle is moving away from desired collectible
                        if ((obstacleCollectible > GameInfo.MIN_DIST_RECTANGLEX && flagStepCollectible == 0) || (obstacleCollectible > GameInfo.MIN_DIST_RECTANGLEX_STEP && flagStepCollectible == 1))
                        {
                            {
                                movePointX = (int)levelInfo.initialCollectibles[collectibleToGet].X; //circle moves to target collectible position
                                currentAction = actionSelector.GetCurrentAction(circleInfo, movePointX, 0, true);
                                return;
                            }                        
                        }
                        

                        movePointX = (int)levelInfo.initialCollectibles[collectibleToGet].X; //no collectible in step: circle moves under of target collectible
                        int velocityX = 0;
                        bool rightMove = nextMove.Value.rightMove;
                        if (flagStepCollectible == 1)
                        {
                            movePointX = (int)rectangleInfo.X; //collectible in step: circle moves to center of rectangle
                            velocityX = GameInfo.JUMP_VELOCITYX;
                            rightMove = rectangleInfo.X < levelInfo.initialCollectibles[collectibleToGet].X; //move left or right depending on where target collectible is located
                        }
                        currentAction = actionSelector.GetCurrentAction(circleInfo, movePointX, velocityX, rightMove);
                        if (!actionSelector.IsGoal(circleInfo, movePointX, velocityX, rightMove))
                        {
                            return;
                        }
                        else { currentAction = Moves.JUMP; }
                     
                    }
                }
                else //default
                {
                    currentAction = actionSelector.GetCurrentAction(circleInfo, (int)circleInfo.X, 0, false);
                }
            }
        }

        /*Agent Behaviour: FOLLOWER mode + stable behaviour*/
        private void Follower()
        {
            //Get current platform in contact with circle
            currentPlatform = graph.GetPlatform(new LevelRepresentation.Point((int)circleInfo.X, (int)circleInfo.Y), GameInfo.MAX_CIRCLE_HEIGHT, (int)circleInfo.VelocityY);

            if (!currentPlatform.HasValue) //Fix for circle not to jump eternally
            {
                currentPlatform = previousPlatform;
            }

            if (currentPlatform.HasValue) //Circle touching platform/floor
            {
                bool isDifferentPlatform = IsDifferentPlatform();
                bool isGetCollectible = IsGetCollectible();
                if (isDifferentPlatform || isGetCollectible) //Just changed platform or collected diamond
                {
                   SetNextEdge(); //find new graph edges/next move                  
                }

                bool cooperating = IsRidingRectangle(); //check if circle is riding rectangle       
                int collectibleToGet = GetCollectibleToGet(); //get target collectible

                if (collectibleToGet == -1) //in case only the individual collectible for the rectangle is left
                {
                    currentAction = actionSelector.GetCurrentAction(circleInfo, (int)circleInfo.X, 0, false);
                    return;
                }

                if (nextMove.HasValue)
                {

                    if (flagStepCollectible == 2 && circleInfo.Y < rectangleInfo.Y && Math.Abs(circleInfo.Y - rectangleInfo.Y) > GameInfo.MIN_DIST_RECTANGLEY) //if circle is in different platform getting target collectible
                    {

                        bool reachedGoal = actionSelector.IsGoal(circleInfo, (int) levelInfo.initialCollectibles[collectibleToGet].X, 0, true);

                        if (!reachedGoal) //circle moves to target collectible position
                        {

                            currentAction = actionSelector.GetCurrentAction(circleInfo, (int) levelInfo.initialCollectibles[collectibleToGet].X, 0, true);

                        }
                        else //if circle reached its goal start jumping to get target collectible
                        {
                            currentAction = Moves.JUMP;
                        }                
                        return;
                    }
                    //circle should wait for rectangle to be in a path where its possible to get collectibles before moving 
                    if (rectangleInfo.Y > levelInfo.initialCollectibles[collectibleToGet].Y) 
                    {

                        if (circleInfo.Y <= levelInfo.initialCollectibles[collectibleToGet].Y || (!cooperating && rectangleInfo.Y > circleInfo.Y && rectangleInfo.Y - (rectangleInfo.Height / 2) - GameInfo.CIRCLE_RADIUS >= circleInfo.Y))
                        {//circle is on top of target collectible; or is not riding rectangle and is higher than the rectangle more than a certain threshold - usual actions
                            currentAction = actionSelector.GetCurrentAction(circleInfo, nextMove.Value.movePoint.x, nextMove.Value.velocityX, nextMove.Value.rightMove);                  
                            return;
                        }

                        if (!cooperating) //circle not riding rectangle
                        {

                            int movePointX = (int)rectangleInfo.X;
                            bool rightMove = (movePointX >= circleInfo.X) ? true : false;
                          
                            int multWidth = rightMove ? -1 : 1;
                            //new move point depends on rectangle width, circle radius, and height difference of these elements
                            movePointX = (int)(movePointX + (multWidth * ((GameInfo.RECTANGLE_AREA / rectangleInfo.Height) / 2 + circleInfo.Radius + Math.Abs(rectangleInfo.Y - circleInfo.Y)/2)));

                            if (movePointX < leftBound || movePointX > rightBound)
                            {
                                // movePointX is out of bounds, handle the situation
                                rightMove = !rightMove;
                                multWidth = rightMove ? -1 : 1;
                                //new move point depends on rectangle width, circle radius, and height difference of these elements
                                movePointX = (int)(movePointX + (multWidth * ((GameInfo.RECTANGLE_AREA / rectangleInfo.Height) / 2 + circleInfo.Radius + Math.Abs(rectangleInfo.Y - circleInfo.Y) / 2)));
                            }

                            int velocityX = Math.Abs(rectangleInfo.Y - circleInfo.Y) > 20 ? GameInfo.JUMP_VELOCITYX : GameInfo.JUMP_VELOCITYX/2;

                            
                            bool reachedGoal = actionSelector.IsGoal(circleInfo, movePointX, velocityX, rightMove); //Circle wants to go to the center of the rectangle

                            if (!reachedGoal) //circle moves to position to jump to rectangle
                            {
                                currentAction = actionSelector.GetCurrentAction(circleInfo, movePointX, velocityX, rightMove);                            
                                
                            }
                            else //if circle reached its goal starts jumping to jump to rectangle
                            {
                                currentAction = Moves.JUMP;
                            }
                            return;
                                                      
                        }
                        else //circle riding rectangle
                        {
                            //rectangle close to desired collectible (consider collectibles in step)
                            if ((obstacleCollectible <= GameInfo.MIN_DIST_RECTANGLEX && flagStepCollectible == 0) || (obstacleCollectible <= GameInfo.MIN_DIST_RECTANGLEX_STEP && flagStepCollectible == 1)) 
                            {

                                timerAdaptiveCollectible = 0; //restart adaptive collectible timer here!

                                int movePointX = (int)levelInfo.initialCollectibles[collectibleToGet].X; //no collectible in step: circle moves under to target collectible
                                int velocityX = 0;
                                bool rightMove = nextMove.Value.rightMove;
                                if (flagStepCollectible == 1)
                                {
                                    movePointX = (int) rectangleInfo.X; //collectible in step: circle moves to center of rectangle
                                    velocityX = GameInfo.JUMP_VELOCITYX;
                                    rightMove = rectangleInfo.X < levelInfo.initialCollectibles[collectibleToGet].X; //move left or right depending on where target collectible is located
                                }
                                currentAction = actionSelector.GetCurrentAction(circleInfo, movePointX, velocityX, rightMove);
                                if (!actionSelector.IsGoal(circleInfo, movePointX, velocityX, rightMove))
                                {
                                    return;
                                }
                                else { currentAction = Moves.JUMP; }                                                               
                            }
                            else //circle should stay at the center of the rectangle waiting to get to the desired collectible
                            {
                                currentAction = actionSelector.GetCurrentAction(circleInfo, (int)rectangleInfo.X, 0, true);

                                if (Math.Abs(rectangleInfo.VelocityX) <= GameInfo.MIN_VELOCITYX)
                                {
                                    timerAdaptiveCollectible += 1; //increment this adaptive collectible timer when rectangle is not moving + is not close to target
                                }

                            }
                        }
                    }
                    else //if circle and rectangle are not on the same path
                    {
                        if (Math.Abs(rectangleInfo.VelocityX) > GameInfo.MIN_VELOCITYX)
                        {
                            timerAdaptiveCollectible = 0; //restart adaptive collectible timer here!
                        }

                        if (!cooperating) //no action when its not cooperating
                        {
                            currentAction = actionSelector.GetCurrentAction(circleInfo, (int)circleInfo.X, 0, false);
                            if (agentAdaptive) //if agent is adaptive always have an action!
                            {
                                if (rectangleInfo.Y < levelInfo.initialCollectibles[collectibleToGet].Y)
                                {
                                    currentAction = actionSelector.GetCurrentAction(circleInfo, (int)circleInfo.X, 0, false);
                                }
                                else
                                {
                                    currentAction = actionSelector.GetCurrentAction(circleInfo, nextMove.Value.movePoint.x, nextMove.Value.velocityX, nextMove.Value.rightMove);
                                }
                            }
                        }
                        else //stay in the middle of the rectangle if riding rectangle
                        {
                            currentAction = actionSelector.GetCurrentAction(circleInfo, (int) rectangleInfo.X, 0, true);
                        }
                    }
                }
                else //default
                {
                    currentAction = actionSelector.GetCurrentAction(circleInfo, (int)circleInfo.X, 0, false);
                }              
            }
        }

        /*Agent Behaviour: Starts as leader but adapts to human behaviour*/
        private void Adaptive()
        {

            bool cooperating = IsRidingRectangle(); //check if circle is riding rectangle

            if (!cooperating) //not riding rectangle
            {
                timerAdaptiveCollectible = 0;
                if (agentBehaviour == GameInfo.AgentBehaviour.LEADER && timerAdaptive <= GameInfo.TIMER_WAITING_LEADER) //start as leader - going to closest diamond
                {
                    if (Math.Abs(circleInfo.X - rectangleInfo.X) < GameInfo.MIN_DIST_RECTANGLEX * 2) //check if rectangle follows lead or not
                    {
                        Leader();
                        timerAdaptive = 0; //Restart timer
                    }
                    else
                    {
                        Leader(true); //Check in this function if circle is in position to catch diamond to increment timer                                      
                    }

                }
                else //activate follower mode if rectangle does not follow
                {
                    agentBehaviour = GameInfo.AgentBehaviour.FOLLOWER;
                    Follower();
                }

            }
            else //riding rectangle
            {
                timerAdaptive = 0;
                
                if (agentBehaviour != GameInfo.AgentBehaviour.LEADER && timerAdaptiveCollectible <= GameInfo.TIMER_WAITING_FOLLOWER) //activate follower mode if it moves to diamond
                {
                    agentBehaviour = GameInfo.AgentBehaviour.FOLLOWER;
                    Follower(); //check in this function if circle moves closer to diamond
                }
                else //activate leader mode if it takes a long time
                {
                    agentBehaviour = GameInfo.AgentBehaviour.LEADER;
                    Leader();
                }

            }

        }

        //implements abstract circle interface: updates the agent state logic and predictions
        public override void Update(TimeSpan elapsedGameTime)
        {

            if ((DateTime.Now - lastMoveTime).TotalMilliseconds >= 20)
            {
                if (!agentAdaptive)
                {
                    if (agentBehaviour == GameInfo.AgentBehaviour.LEADER)
                    {
                        Leader();
                    }
                    else if (agentBehaviour == GameInfo.AgentBehaviour.FOLLOWER)
                    {
                        Follower();
                    }
                }
                else
                {
                    Adaptive();
                }

                RecordVideosLogs();
                
                lastMoveTime = DateTime.Now;

                return;
            }

        }

        //Logs + Videos - Main function
        private void RecordVideosLogs()
        {
            if (currentPlatform.HasValue)
            {
                if (startRecording && !circleInfo.GamePaused && levelInfo.collectibles.Length != 0)
                {
                    startRecording = false;
                    recorder.Start();

                }

                else
                {
                    if (levelInfo.collectibles.Length > 0 && !circleInfo.GamePaused)
                    {
                        ActionRectangle();
                    }
                    else
                    {
                        recorder.StopThread();
                        startRecording = true;
                    }

                    logger.Log("Playing", agentBehaviour.ToString(), cooperationStatus.ToString(), currentAction.ToString(), currentActionRect.ToString(), circleInfo.X, circleInfo.Y, rectangleInfo.X, rectangleInfo.Y, collectiblesLeft);
                }
            }
        }

        //Logs - Rectangle Action
        private void ActionRectangle()
        {
            currentActionRect = Moves.NO_ACTION;
            float currRectX = rectangleInfo.X;
            float currHeight = rectangleInfo.Height;
            float compareVar = 0.05f;
            
            if (currRectX - prevRectX > compareVar)
            {
                currentActionRect = Moves.MOVE_RIGHT;
            }
            else if (currRectX - prevRectX < -compareVar)
            {
                currentActionRect = Moves.MOVE_LEFT;
            }

            if (currHeight < prevHeight)
            {
                currentActionRect = Moves.MORPH_DOWN;
            }
            else if (currHeight > prevHeight)
            {
                currentActionRect = Moves.MORPH_UP;
            }
            prevRectX = currRectX;
            prevHeight = currHeight;
        }

        //Logs - Collectibles List
        private void LogsCollectibles()
        {
            collectiblesLeft = "[";
            bool collectibleFirst = true;
            foreach (CollectibleRepresentation c in levelInfo.collectibles)
            {
                string extra = collectibleFirst ? "" : ", ";
                collectiblesLeft += extra + "(" + c.X + ", " + c.Y + ")";
                collectibleFirst = false;
            }
            collectiblesLeft += "]";
        }

        private bool IsGetCollectible() //Check if caught collectible
        {
            int currentCollectiblesLen = levelInfo.collectibles.Length;
            if (previousCollectiblesLen != currentCollectiblesLen)
            {
                LogsCollectibles();
            }
            if (currentCollectiblesLen > 0)
            {
                int collectibleToGet = GetCollectibleToGet();
                if (previousCollectiblesLen == currentCollectiblesLen)
                {
                    return false;
                }
                else //Deal with cases where caught collectible was not necessarily the collectible to get (just in case)
                {                  
                    bool diffCollectible = false;
                    foreach (CollectibleRepresentation c in levelInfo.collectibles)
                    {
                        //means that collectible to get is still here
                        if (levelInfo.initialCollectibles[collectibleToGet].X == c.X && levelInfo.initialCollectibles[collectibleToGet].Y == c.Y) 
                        {
                            diffCollectible = true;                         
                        }
                    }

                    if (diffCollectible)
                    {
                        int cIdx = 0;
                        int possibleCollectible = -1;
                        //need to find caught collectible
                        foreach (CollectibleRepresentation cI in levelInfo.initialCollectibles)
                        {
                            foreach (CollectibleRepresentation c in levelInfo.collectibles)
                            {
                                if (!collectiblesCollected.Contains(cIdx))
                                {
                                    if (cI.X == c.X && cI.Y == c.Y)
                                    {
                                        possibleCollectible = -1;
                                        break;
                                    }
                                    else
                                    {
                                        possibleCollectible = cIdx;
                                    }
                                }
                                
                            }
                            cIdx += 1;
                        }
                        collectibleToGet = possibleCollectible;
                    }
                }

                collectiblesCollected.Add(collectibleToGet);

                foreach (Graph.Platform p in graph.platforms)
                {
                    foreach (Graph.Move m in p.moves)
                    {
                        m.collectibles_onPath[collectibleToGet] = false;

                    }

                }

                previousCollectiblesLen = currentCollectiblesLen;
            }

            return true;
        }

        private int GetCollectibleToGet()
        {
            if (agentBehaviour == GameInfo.AgentBehaviour.LEADER)
            {

                foreach (int c in collectiblesOrder)
                {
                    bool alreadyCollected = collectiblesCollected.Contains(c);
                    if (!alreadyCollected)
                    {                 
                        float diffObstacle = FindObstacleUnder(levelInfo.initialCollectibles[c]);
                        flagStepCollectible = diffObstacle == -1 ? 0 : 1;
                        return c;
                    }
                }
                
            }
            else if (agentBehaviour == GameInfo.AgentBehaviour.FOLLOWER)
            {
                int closestCollectible = -1;
                float minClosest = -1;
                int nrCollectible = 0;
                bool alreadyCollected;
               
                foreach (CollectibleRepresentation c in levelInfo.initialCollectibles)
                {
                    if (nrCollectible != 3) //individual collectible rectangle - HARDCODED LOL
                    { 
                        bool rightMove = c.X >= rectangleInfo.X;
                        float diffObstacle = FindObstacleUnder(c);
                        if (diffObstacle != -1 && ((rightMove && circleInfo.X >= (diffObstacle + circleInfo.Radius * 2)) || (!rightMove && circleInfo.X <= (diffObstacle - circleInfo.Radius * 2))))
                        {
                            alreadyCollected = collectiblesCollected.Contains(nrCollectible);
                            if (!alreadyCollected)
                            {
                                flagStepCollectible = 2;
                                return nrCollectible;
                            }
                        }
                        float diffCollectible = diffObstacle == -1 ? c.X : diffObstacle;

                        float diffCollectibleRect = Math.Abs(diffCollectible - rectangleInfo.X);
                        alreadyCollected = collectiblesCollected.Contains(nrCollectible);
                        if (!alreadyCollected && (minClosest == -1 || diffCollectibleRect < minClosest)) //initialize or check if minimum
                        {
                            closestCollectible = nrCollectible;
                            minClosest = diffCollectibleRect;
                            flagStepCollectible = diffObstacle == -1 ? 0 : 1;

                        }
                    }
                    nrCollectible += 1;
                }

                obstacleCollectible = minClosest;
                return closestCollectible;
            }
            return -1;
        }

        private bool IsDifferentPlatform() //Check if changed platforms
        {

            if (currentPlatform.HasValue)
            {
                if (!previousPlatform.HasValue)
                {
                    previousPlatform = currentPlatform;
                    return true;
                }
                else if (currentPlatform.Value.id != previousPlatform.Value.id)
                {
                    previousPlatform = currentPlatform;
                    return true;
                }
            }

            previousPlatform = currentPlatform;
            return false;
        }

        private void SetNextEdge()
        {
            int collectibleToGet = GetCollectibleToGet();

            Graph.Move? previousMove = nextMove;

            nextMove = null;

            nextMove = subgoalAStar.CalculateShortestPath(currentPlatform.Value, new LevelRepresentation.Point((int)circleInfo.X, (int)circleInfo.Y),
                 Enumerable.Repeat<bool>(true, levelInfo.initialCollectibles.Length).ToArray(),
                 levelInfo.GetObtainedCollectibles(collectibleToGet), levelInfo.initialCollectibles);
        }

        public static bool IsObstacle_onPixels(int[,] levelArray, List<LevelRepresentation.ArrayPoint> checkPixels)
        {
            if (checkPixels.Count == 0)
            {
                return true;
            }

            foreach (LevelRepresentation.ArrayPoint i in checkPixels)
            {
                if (levelArray[i.yArray, i.xArray] == LevelRepresentation.BLACK || levelArray[i.yArray, i.xArray] == LevelRepresentation.GREEN)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsRidingRectangle()
        {
            int rectangleWidth = GameInfo.RECTANGLE_AREA / (int)rectangleInfo.Height;

            if ((circleInfo.X >= rectangleInfo.X - (rectangleWidth / 2)) &&
                (circleInfo.X <= rectangleInfo.X + (rectangleWidth / 2)) &&
                Math.Abs(rectangleInfo.Y - (rectangleInfo.Height / 2) - GameInfo.CIRCLE_RADIUS - circleInfo.Y) <= 8.0f)
            {
                cooperationStatus = GameInfo.CooperationStatus.RIDING;
                return true;
            }

            cooperationStatus = GameInfo.CooperationStatus.NOT_COOPERATING;
            return false;

        }

        private bool IsRectangleInTheWay(int movePointX, bool rightMove)
        {

            if (circleInfo.Y <= rectangleInfo.Y + (rectangleInfo.Height / 2) &&
                circleInfo.Y >= rectangleInfo.Y - (rectangleInfo.Height / 2))
            {
                int rectangleWidth = GameInfo.RECTANGLE_AREA / (int)rectangleInfo.Height;

                if (!rightMove &&
                    circleInfo.X >= rectangleInfo.X &&
                    circleInfo.X <= movePointX + rectangleWidth + GameInfo.CIRCLE_RADIUS)
                {
                    return true;
                }

                if (rightMove &&
                    circleInfo.X <= rectangleInfo.X &&
                    circleInfo.X >= movePointX - rectangleWidth - GameInfo.CIRCLE_RADIUS)
                {
                    return true;
                }
                
            }

            return false;
        }

        private int FindObstacleInTheWay(int movePointX, bool rightMove, int currCollectibleY)
        {
            foreach (ObstacleRepresentation obs in levelInfo.blackObstacles)
            {
                int fakeMovePoint;
                float rectangleWidth;
                if (!rightMove) { fakeMovePoint = (int)(obs.X + obs.Width/2 + circleInfo.Radius * 1.5); }
                else { fakeMovePoint = (int)(obs.X - obs.Width/2 - circleInfo.Radius * 1.5); }
                if (IsRectangleInTheWay(fakeMovePoint, rightMove))
                {
                    rectangleWidth = GameInfo.RECTANGLE_AREA / (int)rectangleInfo.Height;
                }
                else
                {
                    rectangleWidth = 0;
                }

          
                if (circleInfo.Y >= obs.Y - (obs.Height / 2) && obs.Y - (obs.Height / 2) >= currCollectibleY)
                {
                    if (!rightMove &&
                        circleInfo.X >= obs.X && movePointX <= obs.X + obs.Width && movePointX >= obs.X - obs.Width &&
                        circleInfo.X <= movePointX + obs.X + obs.Width + rectangleWidth + GameInfo.CIRCLE_RADIUS)
                    {
                        return fakeMovePoint;
                    }

                    if (rightMove &&
                        circleInfo.X <= obs.X && movePointX >= obs.X - obs.Width && movePointX <= obs.X + obs.Width &&
                        circleInfo.X >= movePointX - obs.X - obs.Width - rectangleWidth - GameInfo.CIRCLE_RADIUS)
                    {
                        return fakeMovePoint;
                    }

                }


            }

            return -1;
        }

        private float FindObstacleUnder(CollectibleRepresentation c)
        {
            foreach (ObstacleRepresentation obs in levelInfo.blackObstacles)
            {
                float obstacleLeft = obs.X - obs.Width / 2;
                float obstacleRight = obs.X + obs.Width / 2;
                float obstacleTop = obs.Y - obs.Height / 2;

                if (c.Y < obstacleTop && c.X > obstacleLeft && c.X < obstacleRight)
                {                  
                    if (rectangleInfo.X > obstacleLeft)
                    {
                        return obstacleRight + circleInfo.Radius*2;
                    }
                    else
                    {
                        return obstacleLeft - circleInfo.Radius*2;
                    }
                }

            }
            return -1;
        }

        private void ComputeLevelBounds()
        {
            //walls
            leftBound = GameInfo.LEVEL_WALL_WIDTH + (int) circleInfo.Radius;
            rightBound = GameInfo.LEVEL_WIDTH - GameInfo.LEVEL_WALL_WIDTH - (int)circleInfo.Radius;

            //check if obstacles limit bounds
            foreach (ObstacleRepresentation obs in levelInfo.blackObstacles)
            {
                if (obs.Height > circleInfo.Radius*2)
                {
                    float obstacleLeft = obs.X - obs.Width / 2;
                    float obstacleRight = obs.X + obs.Width / 2;
                    if (obstacleLeft <= leftBound)
                    {
                        
                        leftBound = (int) obstacleRight + (int)circleInfo.Radius * 2;
                    }
                    if (obstacleRight >= rightBound)
                    {
                        rightBound = (int)obstacleLeft - (int)circleInfo.Radius * 2;
                    }
                }             

            }        
        }

    }
}

