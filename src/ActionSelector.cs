using GeometryFriends.AI;
using GeometryFriends.AI.Perceptions.Information;
using System;

namespace GeometryFriendsAgents
{
    class ActionSelector
    {
        private const int NUM_POSSIBLE_MOVES = 2;
        private const int ACCELERATE = 0;
        private const int DEACCELERATE = 1;

        private const int MAX_H = GameInfo.MAX_RECTANGLE_HEIGHT - GameInfo.MIN_RECTANGLE_HEIGHT;
        private const int MAX_D = 200;
        private const int MAX_V = GameInfo.MAX_VELOCITYX;

        private const int DISCRETIZATION_V = 10;
        private const int DISCRETIZATION_H = LevelRepresentation.PIXEL_LENGTH;
        private const int DISCRETIZATION_D = 4;

        private const int MAX_DISCRETIZED_V = MAX_V * 2 / DISCRETIZATION_V;
        private const int MAX_DISCRETIZED_D = MAX_D * 2 / DISCRETIZATION_D;
        private const int MAX_DISCRETIZED_H = MAX_H * 2 / DISCRETIZATION_H;

        private const int NUM_STATE = MAX_DISCRETIZED_V * MAX_DISCRETIZED_D;
        private const int NUM_TARGET_V = MAX_V / (DISCRETIZATION_V * 2);

        private const int NUM_ROW_QMAP = NUM_STATE;
        private const int NUM_COLUMN_QMAP = NUM_POSSIBLE_MOVES * NUM_TARGET_V;

        private float[,] Qmap;   

        public ActionSelector()
        {
            Qmap = Utilities.ReadCsvFile(NUM_ROW_QMAP, NUM_COLUMN_QMAP, "Agents\\Qmap.csv");
        }


        public bool IsGoal(CircleRepresentation cI, int targetPointX, int targetVelocityX, bool rightMove)
        {
            float distanceX = rightMove ? cI.X - targetPointX : targetPointX - cI.X;

            if (-DISCRETIZATION_D * 3 < distanceX && distanceX <= DISCRETIZATION_D * 3)
            {
                float relativeVelocityX = rightMove ? cI.VelocityX : -cI.VelocityX;

                if (targetVelocityX == 0)
                {
                    if (targetVelocityX - DISCRETIZATION_V <= relativeVelocityX && relativeVelocityX < targetVelocityX + DISCRETIZATION_V)
                    {
                        return true;
                    }
                }
                else
                {
                    if (targetVelocityX - relativeVelocityX <= DISCRETIZATION_V && relativeVelocityX < targetVelocityX + DISCRETIZATION_V * 2)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool IsGoal(RectangleRepresentation rI, int targetPointX, int targetVelocityX, bool rightMove)
        {
            float distanceX = rightMove ? rI.X - targetPointX : targetPointX - rI.X;

            if (targetVelocityX == 0)
            {
                distanceX = - Math.Abs(distanceX);
            }

            if (-DISCRETIZATION_D * 2 < distanceX && distanceX <= 0)
            {
                float relativeVelocityX = rightMove ? rI.VelocityX : -rI.VelocityX;

                if (targetVelocityX == 0)
                {
                    if (targetVelocityX - DISCRETIZATION_V <= relativeVelocityX && relativeVelocityX < targetVelocityX + DISCRETIZATION_V)
                    {
                        return true;
                    }
                }
                else
                {
                    if (targetVelocityX <= relativeVelocityX && relativeVelocityX < targetVelocityX + DISCRETIZATION_V * 2)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public Moves GetCurrentAction(CircleRepresentation cI, int targetPointX, int targetVelocityX, bool rightMove)
        {
            int stateNum = GetStateNum(cI, targetPointX, rightMove);

            int currentActionNum;

            float distanceX = rightMove ? cI.X - targetPointX : targetPointX - cI.X;

            if (distanceX <= -MAX_D)
            {
                currentActionNum = ACCELERATE;
            }
            else if (distanceX >= MAX_D)
            {
                currentActionNum = DEACCELERATE;
            }
            else
            {
                currentActionNum = GetOptimalActionNum(stateNum, targetVelocityX);
            }
            
            Moves currentAction;

            if (currentActionNum == ACCELERATE)
            {
                currentAction = rightMove ? Moves.ROLL_RIGHT : Moves.ROLL_LEFT;
            }
            else
            {
                currentAction = rightMove ? Moves.ROLL_LEFT : Moves.ROLL_RIGHT;
            }

            return currentAction;
        }

        public Moves GetCurrentAction(RectangleRepresentation rI, int targetPointX, int targetVelocityX, bool rightMove)
        {

            int stateNum = GetStateNum(rI, targetPointX, rightMove);

            int currentActionNum;

            float distanceX = rightMove ? rI.X - targetPointX : targetPointX - rI.X;

            if (distanceX <= -MAX_D)
            {
                currentActionNum = ACCELERATE;
            }
            else if (distanceX >= MAX_D)
            {
                currentActionNum = DEACCELERATE;
            }
            else
            {
                currentActionNum = GetOptimalActionNum(stateNum, targetVelocityX);
            }

            Moves currentAction;

            if (currentActionNum == ACCELERATE)
            {
                currentAction = rightMove ? Moves.MOVE_RIGHT : Moves.MOVE_LEFT;
            }
            else
            {
                currentAction = rightMove ? Moves.MOVE_LEFT : Moves.MOVE_RIGHT;
            }

            return currentAction;
        }

        public int GetStateNum(CircleRepresentation cI, int targetPointX, bool rightMove)
        {
            // discretized target velocity
            int discretized_V = (int)((rightMove ? cI.VelocityX : -cI.VelocityX) + MAX_V) / DISCRETIZATION_V;
            if (discretized_V < 0)
            {
                discretized_V = 0;
            }
            else if (discretized_V >= MAX_DISCRETIZED_V)
            {
                discretized_V = MAX_DISCRETIZED_V - 1;
            }

            // discretized distance to target
            int discretized_D = (int)((rightMove ? cI.X - targetPointX : targetPointX - cI.X) + MAX_D) / DISCRETIZATION_D;
            if (discretized_D < 0)
            {
                discretized_D = 0;
            }
            else if (discretized_D >= MAX_DISCRETIZED_D)
            {
                discretized_D = MAX_DISCRETIZED_D - 1;
            }

            // state number
            return discretized_V + discretized_D * MAX_DISCRETIZED_V;
        }

        public int GetStateNum(RectangleRepresentation rI, int targetPointX, bool rightMove)
        {

            // discretized target velocity
            int discretized_V = (int)((rightMove ? rI.VelocityX : -rI.VelocityX) + MAX_V) / DISCRETIZATION_V;
            if (discretized_V < 0)
            {
                discretized_V = 0;
            }
            else if (discretized_V >= MAX_DISCRETIZED_V)
            {
                discretized_V = MAX_DISCRETIZED_V - 1;
            }

            // discretized distance to target
            int discretized_D = (int)((rightMove ? rI.X - targetPointX : targetPointX - rI.X) + MAX_D) / DISCRETIZATION_D;
            if (discretized_D < 0)
            {
                discretized_D = 0;
            }
            else if (discretized_D >= MAX_DISCRETIZED_D)
            {
                discretized_D = MAX_DISCRETIZED_D - 1;
            }

            // discretized height to target
            //int discretized_H = (int) ((targetHeight - rI.Height) + MAX_H) / DISCRETIZATION_H;
            //if (discretized_H < 0)
            //{
            //    discretized_H = GameInfo.MIN_RECTANGLE_HEIGHT;
            //}
            //else if (discretized_H >= MAX_DISCRETIZED_H)
            //{
            //    discretized_H = MAX_DISCRETIZED_H - 1;
            //}

            // state number
            return discretized_V + discretized_D * MAX_DISCRETIZED_V;
            //return discretized_V + discretized_D * MAX_DISCRETIZED_V + discretized_H * MAX_DISCRETIZED_V * MAX_DISCRETIZED_D;
        }

        private int GetOptimalActionNum(int stateNum, int targetVelocityX)
        {
            int maxColumnNum = 0;
            float maxValue = float.MinValue;

            int from = (targetVelocityX / (DISCRETIZATION_V * 2)) * 2;
            int to = from + NUM_POSSIBLE_MOVES;

            for (int i = from; i < to; i++)
            {
                if (maxValue < Qmap[stateNum, i])
                {
                    maxValue = Qmap[stateNum, i];
                    maxColumnNum = i;
                }
            }

            return maxColumnNum - from;
        }
    }
}
