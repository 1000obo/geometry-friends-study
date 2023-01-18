using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static GeometryFriendsAgents.Graph;
using static GeometryFriendsAgents.LevelRepresentation;

namespace GeometryFriendsAgents
{
    static class PlatformIdentification
    {

        public static platformType[,] GetPlatformArray_Circle(int[,] levelArray)
        {
            platformType[,] platformArray = new platformType[levelArray.GetLength(0), levelArray.GetLength(1)];

            for (int y = 0; y < levelArray.GetLength(0); y++)
            {
                Parallel.For(0, levelArray.GetLength(1), x =>
                {

                    Point circleCenter = ConvertArrayPointIntoPoint(new ArrayPoint(x, y));
                    circleCenter.y -= GameInfo.CIRCLE_RADIUS;
                    List<ArrayPoint> circlePixels = GetCirclePixels(circleCenter, GameInfo.CIRCLE_RADIUS);

                    if (!CircleAgent.IsObstacle_onPixels(levelArray, circlePixels))
                    {
                        if (levelArray[y, x - 1] == BLACK || levelArray[y, x] == BLACK)
                        {
                            platformArray[y, x] = platformType.BLACK;
                        }
                        else if (levelArray[y, x - 1] == GREEN || levelArray[y, x] == GREEN)
                        {
                            platformArray[y, x] = platformType.GREEN;
                        }
                    }
                });
            }

            return platformArray;

        }

        public static platformType[,] GetPlatformArray_Rectangle(int[,] levelArray)
        {
            Graph.platformType[,] platformArray = new Graph.platformType[levelArray.GetLength(0), levelArray.GetLength(1)];

            for (int y = 0; y < levelArray.GetLength(0); y++)
            {
                Parallel.For(0, levelArray.GetLength(1), x =>
                {

                    if (levelArray[y, x] == BLACK || levelArray[y, x] == YELLOW)
                    {

                        // RECTANGLE WITH HEIGHT 100
                        Point rectangleCenter = ConvertArrayPointIntoPoint(new ArrayPoint(x, y));
                        rectangleCenter.y -= GameInfo.SQUARE_HEIGHT / 2;
                        List<ArrayPoint> rectanglePixels = RectangleAgent.GetFormPixels(rectangleCenter, GameInfo.SQUARE_HEIGHT);

                        if (RectangleAgent.IsObstacle_onPixels(levelArray, rectanglePixels))
                        {

                            // RECTANGLE WITH HEIGHT 50
                            rectangleCenter = ConvertArrayPointIntoPoint(new ArrayPoint(x, y));
                            rectangleCenter.y -= GameInfo.MIN_RECTANGLE_HEIGHT / 2;
                            rectanglePixels = RectangleAgent.GetFormPixels(rectangleCenter, GameInfo.MIN_RECTANGLE_HEIGHT);

                            if (RectangleAgent.IsObstacle_onPixels(levelArray, rectanglePixels))
                            {

                                // RECTANGLE WITH HEIGHT 200
                                rectangleCenter = ConvertArrayPointIntoPoint(new ArrayPoint(x, y));
                                rectangleCenter.y -= GameInfo.MAX_RECTANGLE_HEIGHT / 2;
                                rectanglePixels = RectangleAgent.GetFormPixels(rectangleCenter, GameInfo.MAX_RECTANGLE_HEIGHT);

                                if (RectangleAgent.IsObstacle_onPixels(levelArray,rectanglePixels))
                                {
                                    return;
                                }
                            }
                        }

                        platformArray[y, x] = (levelArray[y, x] == BLACK) ? platformType.BLACK : platformType.YELLOW;
                    }

                });
            }

            return platformArray;

        }

        public static List<Platform> SetPlatforms_Circle(int[,] levelArray)
        {
            List<Graph.Platform> platforms = new List<Graph.Platform>();

            Graph.platformType[,] platformArray = GetPlatformArray_Circle(levelArray);

            Parallel.For(0, levelArray.GetLength(0), i =>
            {
                Graph.platformType currentPlatform = Graph.platformType.NO_PLATFORM;
                int leftEdge = 0;

                for (int j = 0; j < platformArray.GetLength(1); j++)
                {
                    if (currentPlatform == Graph.platformType.NO_PLATFORM)
                    {
                        if (platformArray[i, j] == Graph.platformType.BLACK || platformArray[i, j] == Graph.platformType.GREEN)
                        {
                            leftEdge = LevelRepresentation.ConvertValue_ArrayPointIntoPoint(j);
                            currentPlatform = platformArray[i, j];
                        }
                    }
                    else
                    {
                        if (platformArray[i, j] != currentPlatform)
                        {
                            int rightEdge = LevelRepresentation.ConvertValue_ArrayPointIntoPoint(j - 1);

                            if (rightEdge >= leftEdge)
                            {
                                lock (platforms)
                                {
                                    platforms.Add(new Graph.Platform(Graph.platformType.BLACK, LevelRepresentation.ConvertValue_ArrayPointIntoPoint(i), leftEdge, rightEdge, new List<Graph.Move>(), GameInfo.MAX_CIRCLE_HEIGHT / LevelRepresentation.PIXEL_LENGTH));
                                }
                            }

                            currentPlatform = platformArray[i, j];
                        }
                    }
                }
            });

            return SetPlatformsID(platforms);

        }

        public static List<Platform> SetPlatforms_Rectangle(int[,] levelArray)
        {

            List<Platform> platforms = new List<Platform>();

            platformType[,] platformArray = GetPlatformArray_Rectangle(levelArray);

            Parallel.For(0, platformArray.GetLength(0), y =>
            {

                int min_height_pixels = GameInfo.MIN_RECTANGLE_HEIGHT / PIXEL_LENGTH;
                int max_height_pixels = Math.Min((GameInfo.MAX_RECTANGLE_HEIGHT / PIXEL_LENGTH), y + MARGIN + min_height_pixels);

                int leftEdge = 0, allowedHeight = max_height_pixels, gap_size = 0;
                platformType currentPlatform = platformType.NO_PLATFORM;

                for (int x = 0; x < platformArray.GetLength(1); x++)
                {

                    if (currentPlatform == platformType.NO_PLATFORM)
                    {
                        if (platformArray[y, x] == platformType.BLACK || platformArray[y, x] == platformType.YELLOW)
                        {

                            if (7 <= gap_size && gap_size <= 19)
                            {
                                int rightEdge = ConvertValue_ArrayPointIntoPoint(x - 1);

                                if (rightEdge >= leftEdge)
                                {

                                    int gap_allowed_height = (GameInfo.RECTANGLE_AREA / (Math.Min((gap_size + 8) * PIXEL_LENGTH, GameInfo.MAX_RECTANGLE_HEIGHT))) / PIXEL_LENGTH;

                                    lock (platforms)
                                    {
                                        platforms.Add(new Platform(platformType.GAP, ConvertValue_ArrayPointIntoPoint(y), leftEdge, rightEdge, new List<Move>(), gap_allowed_height));
                                    }
                                }
                            }

                            gap_size = 0;
                            currentPlatform = platformArray[y, x];

                            for (int h = min_height_pixels; h <= max_height_pixels; h++)
                            {
                                if (levelArray[y - h, x] == BLACK || levelArray[y - h, x] == YELLOW)
                                {
                                    //allowedHeight = Math.Max(h-1,0);
                                    allowedHeight = h;
                                    break;
                                }
                            }

                            leftEdge = ConvertValue_ArrayPointIntoPoint(x);
                        }

                        else if (levelArray[y, x] == GREEN || levelArray[y, x] == OPEN)
                        {
                            gap_size++;
                        }
                    }

                    else
                    {
                        if (platformArray[y, x] != currentPlatform)
                        {
                            int rightEdge = ConvertValue_ArrayPointIntoPoint(x - 1);

                            if (rightEdge >= leftEdge)
                            {
                                lock (platforms)
                                {
                                    platforms.Add(new Platform(currentPlatform, ConvertValue_ArrayPointIntoPoint(y), leftEdge, rightEdge, new List<Move>(), allowedHeight));
                                }
                            }

                            allowedHeight = max_height_pixels;
                            currentPlatform = platformArray[y, x];
                            leftEdge = ConvertValue_ArrayPointIntoPoint(x);

                        }

                        if (platformArray[y, x] != platformType.NO_PLATFORM && y > MARGIN + min_height_pixels)
                        {

                            for (int h = min_height_pixels; h <= max_height_pixels; h++)
                            {

                                if (levelArray[y - h, x] == BLACK ||
                                    levelArray[y - h, x] == YELLOW ||
                                    (h == max_height_pixels && allowedHeight != max_height_pixels && levelArray[y - h, x] == OPEN))
                                {

                                    if (h != allowedHeight)
                                    {

                                        int rightEdge = ConvertValue_ArrayPointIntoPoint(x - 1);

                                        if (rightEdge >= leftEdge)
                                        {
                                            lock (platforms)
                                            {
                                                platforms.Add(new Platform(currentPlatform, ConvertValue_ArrayPointIntoPoint(y), leftEdge, rightEdge, new List<Move>(), allowedHeight));
                                            }
                                        }

                                        //allowedHeight = Math.Max(h - 1, 0);
                                        allowedHeight = h;
                                        leftEdge = ConvertValue_ArrayPointIntoPoint(x - 1);

                                    }

                                    break;

                                }
                            }
                        }
                    }
                }
            });

            return SetPlatformsID(platforms);
        }

        public static List<Platform> JoinPlatforms(List<Platform> platforms1, List<Platform> platforms2)
        {

            //Platform currentPlatform;
            //List<Platform> platformsRectangle = new List<Platform>();

            //for (int i = 0; i < platforms2.Count; i++)
            //{

            //    if (i < platforms2.Count - 1)
            //    {

            //    }

            //}


            //foreach (Platform p in platforms2)
            //{
            //    platforms1.Add(new Platform(platformType.COOPERATION, p.height - GameInfo.MIN_RECTANGLE_HEIGHT, p.leftEdge, p.rightEdge, p.moves, p.allowedHeight, p.id));
            //}

            foreach (Platform p in platforms2)
            {
                platforms1.Add(new Platform(platformType.COOPERATION, p.height - GameInfo.MIN_RECTANGLE_HEIGHT, p.leftEdge, p.rightEdge, p.moves, p.allowedHeight, p.id));
            }

            return SetPlatformsID(platforms1);

        }

        public static List<Platform> SetPlatformsID(List<Platform> platforms)
        {
            platforms.Sort((a, b) => {
                int result = a.height - b.height;
                return result != 0 ? result : a.leftEdge - b.leftEdge;
            });

            Parallel.For(0, platforms.Count, i =>
            {
                Graph.Platform tempPlatfom = platforms[i];
                tempPlatfom.id = i + 1;
                platforms[i] = tempPlatfom;
            });

            return platforms;
        }

        public static List<Platform> DeleteCooperationPlatforms(List<Platform> platforms)
        {

            foreach (Graph.Platform p in platforms)
            {
                foreach (Graph.Move m in p.moves)
                {
                    if (m.reachablePlatform.type == Graph.platformType.COOPERATION)
                    {
                        var itemToRemove = p.moves.Single(r => r.type == m.type &&
                                                                r.reachablePlatform.id == m.reachablePlatform.id &&
                                                                r.movePoint.x == m.movePoint.x &&
                                                                r.movePoint.y == m.movePoint.y);
                        p.moves.Remove(itemToRemove);
                    }
                }
            }

            foreach (Graph.Platform p in platforms)
            {
                if (p.type == Graph.platformType.COOPERATION)
                {
                    var itemToRemove = platforms.Single(r => r.id == p.id && r.type == p.type);
                    platforms.Remove(itemToRemove);
                }
            }

            return platforms;

        }

        public static List<Platform> DeleteUnreachablePlatforms(List<Platform> rectanglePlatforms, RectangleRepresentation initialRectangle)
        {
            Platform? currentPlatform = GetPlatform(rectanglePlatforms, new Point((int)initialRectangle.X, (int)initialRectangle.Y), initialRectangle.Height);

            if (currentPlatform.HasValue)
            {
                bool[] platformsChecked = VisitPlatform(new bool[rectanglePlatforms.Count], currentPlatform.Value);

                for (int p = platformsChecked.Length - 1; p >= 0; p--)
                {
                    if (!platformsChecked[p])
                    {
                        rectanglePlatforms.Remove(rectanglePlatforms[p]);
                    }
                }
            }

            foreach (Platform p in rectanglePlatforms)
            {
                p.moves.Clear();
            }

            return rectanglePlatforms;
        }

        private static bool[] VisitPlatform(bool[] platformsChecked, Platform platform)
        {
            platformsChecked[platform.id - 1] = true;

            foreach (Move m in platform.moves)
            {
                if (!platformsChecked[m.reachablePlatform.id - 1])
                {
                    platformsChecked = VisitPlatform(platformsChecked, m.reachablePlatform);
                }
            }

            return platformsChecked;
        }

        private static Platform? GetPlatform(List<Platform> platformsList, Point center, float height, int velocityY = 0)
        {

            foreach (Platform i in platformsList)
            {
                if (i.leftEdge <= center.x && center.x <= i.rightEdge && (i.height - center.y >= (height / 2) - 8) && (i.height - center.y <= (height / 2) + 8))
                {
                    return i;
                }
            }

            return null;
        }
    }
}
