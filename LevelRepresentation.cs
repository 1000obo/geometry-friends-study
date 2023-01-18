using GeometryFriends.AI.Perceptions.Information;
using System.Collections.Generic;
using System;

namespace GeometryFriendsAgents
{
    public class LevelRepresentation
    {

        public const int OPEN = 0;
        public const int BLACK = -1;
        public const int GREEN = -2;
        public const int YELLOW = -3;
        public const int COOPERATION = -4;

        public const int MARGIN = 5;
        public const int PIXEL_LENGTH = 8;
        private int[] COLLECTIBLE_SIZE = new int[] { 2, 4, 6, 6, 4, 2 };

        public int[,] levelArray = new int[GameInfo.LEVEL_HEIGHT / PIXEL_LENGTH, GameInfo.LEVEL_WIDTH / PIXEL_LENGTH];
        
        public ObstacleRepresentation[] blackObstacles;
        public ObstacleRepresentation[] greenObstacles;
        public ObstacleRepresentation[] yellowObstacles;
        public CollectibleRepresentation[] collectibles;
        public CollectibleRepresentation[] initialCollectibles;

        public struct ArrayPoint
        {
            public int xArray;
            public int yArray;

            public ArrayPoint(int xArray, int yArray)
            {
                this.xArray = xArray;
                this.yArray = yArray;
            }
        }

        public struct Point
        {
            public int x;
            public int y;

            public Point(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }

        public static ArrayPoint ConvertPointIntoArrayPoint(Point value, bool xNegative, bool yNegative)
        {
            return new ArrayPoint(ConvertValue_PointIntoArrayPoint(value.x, xNegative), ConvertValue_PointIntoArrayPoint(value.y, yNegative));
        }

        public static Point ConvertArrayPointIntoPoint(ArrayPoint value)
        {
            return new Point(ConvertValue_ArrayPointIntoPoint(value.xArray), ConvertValue_ArrayPointIntoPoint(value.yArray));
        }

        public static int ConvertValue_PointIntoArrayPoint(int pointValue, bool negative)
        {
            int arrayValue = (pointValue - GameInfo.LEVEL_ORIGINAL) / PIXEL_LENGTH;

            if (arrayValue < 0)
            {
                arrayValue = 0;
            }

            if (negative)
            {
                if ((pointValue - GameInfo.LEVEL_ORIGINAL) % PIXEL_LENGTH == 0)
                {
                    arrayValue--;
                }
            }

            return arrayValue;
        }

        public static int ConvertValue_ArrayPointIntoPoint(int arrayValue)
        {
            return arrayValue * PIXEL_LENGTH + GameInfo.LEVEL_ORIGINAL;
        }

        public int[,] GetLevelArray()
        {
            return levelArray;
        }

        public void CreateLevelArray(CollectibleRepresentation[] col, ObstacleRepresentation[] black, ObstacleRepresentation[] green, ObstacleRepresentation[] yellow)
        {

            this.blackObstacles = black;
            this.greenObstacles = green;
            this.yellowObstacles = yellow;

            this.collectibles = col;
            this.initialCollectibles = col;

            SetCollectibles(collectibles);
            SetDefaultObstacles();
            SetObstacles(blackObstacles, BLACK);
            SetObstacles(greenObstacles, GREEN);
            SetObstacles(yellowObstacles, YELLOW);
        }

        private void SetDefaultObstacles()
        {
            for (int i = 0; i <= 3; i++)
            {
                for (int j = 0; j < levelArray.GetLength(1); j++)
                {
                    levelArray[i, j] = BLACK;
                }
            }

            for (int i = 0; i < levelArray.GetLength(0); i++)
            {
                for (int j = 0; j <= 3; j++)
                {
                    levelArray[i, j] = BLACK;
                }
            }

            for (int i = 0; i < levelArray.GetLength(0); i++)
            {
                for (int j = 154; j < levelArray.GetLength(1); j++)
                {
                    levelArray[i, j] = BLACK;
                }
            }

            for (int i = 94; i < levelArray.GetLength(0); i++)
            {
                for (int j = 0; j < levelArray.GetLength(1); j++)
                {
                    levelArray[i, j] = BLACK;
                }
            }
        }

        private void SetObstacles(ObstacleRepresentation[]obstacles, int obstacleType)
        {
            foreach (ObstacleRepresentation o in obstacles)
            {
                int xPosArray = (int)(o.X - (o.Width / 2) - GameInfo.LEVEL_ORIGINAL) / PIXEL_LENGTH;
                int yPosArray = (int)(o.Y - (o.Height / 2) - GameInfo.LEVEL_ORIGINAL) / PIXEL_LENGTH;
                int height = (int)(o.Height / PIXEL_LENGTH);
                int width = (int)(o.Width / PIXEL_LENGTH);
                
                for (int i = yPosArray; i < (yPosArray + height); i++)
                {
                    for (int j = xPosArray; j < (xPosArray + width); j++)
                    {
                        if (0 <= i && i < levelArray.GetLength(0) && 0 <= j && j < levelArray.GetLength(1))
                        {
                            levelArray[i, j] = obstacleType;
                        }
                    }
                }
            }
        }

        private void SetCollectibles(CollectibleRepresentation[] colI)
        {
            initialCollectibles = colI;

            for(int i = 0; i < colI.Length; i++)
            {
                int xPosArray = (int)(colI[i].X - GameInfo.LEVEL_ORIGINAL) / PIXEL_LENGTH;
                int yPosArray = (int)(colI[i].Y - GameInfo.LEVEL_ORIGINAL) / PIXEL_LENGTH;

                for (int j = 0; j < COLLECTIBLE_SIZE.Length; j++)
                {
                    for (int k = 0; k < COLLECTIBLE_SIZE[j]; k++)
                    {
                        levelArray[yPosArray + (j - COLLECTIBLE_SIZE.Length / 2), xPosArray + (k - COLLECTIBLE_SIZE[j] / 2)] = i + 1;
                    }
                }
            }
        }

        public static int[,] SetCooperation(int[,] levelArray, List<Graph.Platform> platforms)
        {
            foreach (Graph.Platform p in platforms)
            {

                if (p.type == Graph.platformType.COOPERATION)
                {
                    int platformWidth = p.rightEdge - p.leftEdge;
                    int platformHeight = GameInfo.MIN_RECTANGLE_HEIGHT;


                    int x0 = p.leftEdge / PIXEL_LENGTH;
                    int y0 = p.height / PIXEL_LENGTH;
                    int height = platformHeight / PIXEL_LENGTH;
                    int width = platformWidth / PIXEL_LENGTH;

                    int i = y0;

                    //for (int i = y0; i <= (y0 + height); i++)
                    //{
                        for (int j = x0; j <= (x0 + width); j++)
                        {
                            if (0 <= i && i < levelArray.GetLength(0) && 0 <= j && j < levelArray.GetLength(1))
                            {
                                levelArray[i, j] = COOPERATION;
                            }
                        }
                    //}
                }
             
            }

            return levelArray;
        }

        public bool[] GetObtainedCollectibles(int collectible)
        {
            bool[] obtainedCollectibles = new bool[initialCollectibles.Length];

            for (int i = 0; i < obtainedCollectibles.Length; i++)
            {
                obtainedCollectibles[i] = true;
            }

            foreach (CollectibleRepresentation i in collectibles)
            {
                for (int j = 0; j < initialCollectibles.Length; j++)
                {
                    if (i.Equals(initialCollectibles[j]) && (j == collectible || collectible == -1))
                    {
                        obtainedCollectibles[j] = false;                    
                    }
                }
            }

            return obtainedCollectibles;
        }

    }
}
