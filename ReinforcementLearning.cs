using System.Globalization;
using System.IO;

using GeometryFriends.AI;
using System.Collections.Generic;
using System;
using GeometryFriends.AI.Perceptions.Information;

namespace GeometryFriendsAgents
{
    class ReinforcementLearning
    {

        private const int DISCRETIZATION_D = 4;
        private const int DISCRETIZATION_V = 10;
        private const int DISCRETIZATION_H = LevelRepresentation.PIXEL_LENGTH;

        private const int MAX_D = 200;
        private const int MAX_V = GameInfo.MAX_VELOCITYX;
        private const int MAX_H = GameInfo.MAX_RECTANGLE_HEIGHT - GameInfo.MIN_RECTANGLE_HEIGHT;

        private const int MAX_DISCRETIZED_D = MAX_D * 2 / DISCRETIZATION_D;
        private const int MAX_DISCRETIZED_V = MAX_V * 4 / DISCRETIZATION_V;
        private const int MAX_DISCRETIZED_H = MAX_H / DISCRETIZATION_H;

        public const int N_STATES = MAX_DISCRETIZED_V * MAX_DISCRETIZED_D * MAX_DISCRETIZED_H;
        //private const int N_TARGET_V = MAX_V / (DISCRETIZATION_V * 2);
        //private const int N_TARGET_H = MAX_H / (DISCRETIZATION_H * 2);

        //private const int N_ROWS = N_STATES;
        public const int N_ACTIONS = 4;
        //private const int N_COLUMNS = N_MOVES * N_TARGET_V;

        private const float E_GREEDY = 0.6F;
        private const int N_TRAININGS = 5000;
        private const float LEARNING_RATE = 0.1F;
        private const float DISCOUNT_RATE = 0.999F;

        private float[,] QTable;

        private int targetX, targetV, targetH;
        private bool rightTarget;
        private List<Moves> possibleMoves;

        private int previous_state = -1;
        private int previous_action = -1;

        public int n_iterations = 0;

        public void Setup(int targetX, int targetV, int targetH, bool rightTarget)
        {

            QTable = Utilities.ReadCsvFile(N_STATES, N_ACTIONS, "Agents\\QTableRectangle.csv");

            this.targetX = targetX;
            this.targetV = targetV;
            this.targetH = targetH;
            this.rightTarget = rightTarget;

            possibleMoves = new List<Moves>();
            possibleMoves.Add(Moves.MOVE_LEFT);
            possibleMoves.Add(Moves.MOVE_RIGHT);
            possibleMoves.Add(Moves.MORPH_UP);
            possibleMoves.Add(Moves.MORPH_DOWN);
        }

        public Moves GetBestAction(RectangleRepresentation rI)
        {

            if (Math.Abs(targetX - rI.X) > 200)
            {
                return (targetX - rI.X < 0) ? Moves.MOVE_LEFT : Moves.MOVE_RIGHT;
            }

            return possibleMoves[GetGreedyAction(GetState(rI))];
        }

        public Moves GetAction(RectangleRepresentation rI)
        {

            if (Math.Abs(targetX - rI.X) > 200)
            {
                return (targetX - rI.X < 0) ? Moves.MOVE_LEFT : Moves.MOVE_RIGHT;
            }

            Random rnd = new Random();

            int s = GetState(rI);

            if (s != previous_state && previous_state != -1)
            {
                UpdateQTableEntry(previous_state, previous_action, s);
            }

            if (s == 0)
            {
                return Moves.NO_ACTION;
            }

            //if (s != previous_state)
            //{
                previous_state = s;
                previous_action = (rnd.Next(100) < (E_GREEDY * 100)) ? GetRandomAction() : GetGreedyAction(s);
            //}

            return possibleMoves[previous_action];
        }

        private void UpdateQTableEntry(int s, int a, int new_s)
        {
            int reward = (new_s == 0 ? 200 : -1);
            float max_q = QTable[new_s, GetGreedyAction(new_s)];
            QTable[s, a] += LEARNING_RATE * (reward + (DISCOUNT_RATE * max_q) - QTable[s, a]);
            n_iterations++;
        }

        private int GetGreedyAction(int s)
        {
            int max_a = -1;
            double max_q = -1;

            for (int a = 0; a < N_ACTIONS; a++)
            {
                if (QTable[s,a] > max_q || max_a == -1) 
                {
                    max_q = QTable[s, a];
                    max_a = a;
                }
            }

            return max_a;
        }

        private int GetRandomAction()
        {    
            Random rnd = new Random();
            return rnd.Next(4);
        }

        private int GetDiscretizedV(RectangleRepresentation rI)
        {
            int discretized_V = (int)((rightTarget ? targetV - rI.VelocityX : (-targetV) - rI.VelocityX) + (2 * MAX_V)) / DISCRETIZATION_V;

            if (discretized_V < 0)
            {
                return 0;
            }
            else if (discretized_V >= MAX_DISCRETIZED_V)
            {
                return MAX_DISCRETIZED_V - 1;
            }

            return discretized_V;
        }

        private int GetDiscretizedD(RectangleRepresentation rI)
        {
            int discretized_D = (int) Math.Abs(targetX - rI.X) / DISCRETIZATION_D;

            if (discretized_D < 0)
            {
                return 0;
            }
            else if (discretized_D >= MAX_DISCRETIZED_D)
            {
                return MAX_DISCRETIZED_D - 1;
            }

            return discretized_D;
        }

        private int GetDiscretizedH(RectangleRepresentation rI)
        {
            int discretized_H = (int)((targetH - rI.Height) + MAX_H) / DISCRETIZATION_H;
            if (discretized_H < 0)
            {
                return GameInfo.MIN_RECTANGLE_HEIGHT;
            }
            else if (discretized_H >= MAX_DISCRETIZED_H)
            {
                return MAX_DISCRETIZED_H - 1;
            }

            return discretized_H;
        }

        public int GetState(RectangleRepresentation rI)
        {

            int discretized_V = GetDiscretizedV(rI);
            int discretized_D = GetDiscretizedD(rI);
            int discretized_H = GetDiscretizedH(rI);

            return discretized_V + discretized_D * MAX_DISCRETIZED_V + discretized_H * MAX_DISCRETIZED_V * MAX_DISCRETIZED_D;
        }

        public void UpdateQTable()
        {
            WriteQTable(QTable);
        }

        public static void WriteQTable(float[,] QTable)
        {

            NumberFormatInfo nfi  = new NumberFormatInfo
            {
                NumberDecimalSeparator = ".",
                NumberDecimalDigits = 5
            };

            using (var w = new StreamWriter("Agents\\QTableRectangle.csv"))
            {
                for (int row = 0; row < N_STATES; row++)
                {
                    string line = string.Format(nfi, "{0:N5}", QTable[row, 0]);

                    for (int column = 1; column < N_ACTIONS; column++)
                    {
                        line = string.Format(nfi, "{0},{1:N5}", line, QTable[row, column]);
                    }

                    w.WriteLine(line);

                }

                w.Flush();

            }

        }

    }
}
