using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static GeometryFriendsAgents.Graph;

namespace GeometryFriendsAgents
{
    static class MoveIdentification
    {

        public static void Setup_Rectangle(Graph graph)
        {

            foreach (Platform fromPlatform in graph.platforms)
            {

                StairOrGap_Rectangle(graph, fromPlatform);

                if (fromPlatform.type != platformType.GAP)
                {
                    Fall(graph, fromPlatform);
                    Morph(graph, fromPlatform);
                    Collect(graph, fromPlatform);
                }

            }

        }

        public static void Jump(Graph graph, Platform fromPlatform)
        {
            Parallel.For(0, (GameInfo.MAX_VELOCITYX / Graph.VELOCITYX_STEP), k =>
            {
                int from = fromPlatform.leftEdge + (fromPlatform.leftEdge - GameInfo.LEVEL_ORIGINAL) % (LevelRepresentation.PIXEL_LENGTH * 2);
                int to = fromPlatform.rightEdge - (fromPlatform.rightEdge - GameInfo.LEVEL_ORIGINAL) % (LevelRepresentation.PIXEL_LENGTH * 2);
                
                if (fromPlatform.type == Graph.platformType.COOPERATION && k <= 5)
                {
                    from = fromPlatform.leftEdge + (fromPlatform.leftEdge - GameInfo.LEVEL_ORIGINAL) % (LevelRepresentation.PIXEL_LENGTH * 2) + 90;
                    to = fromPlatform.rightEdge - (fromPlatform.rightEdge - GameInfo.LEVEL_ORIGINAL) % (LevelRepresentation.PIXEL_LENGTH * 2) - 90;

                    Parallel.For(0, (to - from) / (LevelRepresentation.PIXEL_LENGTH * 2) + 1, j =>
                    {
                        LevelRepresentation.Point movePoint = new LevelRepresentation.Point(from + j * LevelRepresentation.PIXEL_LENGTH * 2, fromPlatform.height - GameInfo.CIRCLE_RADIUS);
                        Trajectory(graph, fromPlatform, movePoint, graph.MAX_HEIGHT, Graph.VELOCITYX_STEP * k, true, Graph.movementType.JUMP);
                        Trajectory(graph, fromPlatform, movePoint, graph.MAX_HEIGHT, Graph.VELOCITYX_STEP * k, false, Graph.movementType.JUMP);

                        movePoint = new LevelRepresentation.Point(from + j * LevelRepresentation.PIXEL_LENGTH * 2, fromPlatform.height - 50 - GameInfo.CIRCLE_RADIUS);
                        Trajectory(graph, fromPlatform, movePoint, graph.MAX_HEIGHT, Graph.VELOCITYX_STEP * k, true, Graph.movementType.JUMP);
                        Trajectory(graph, fromPlatform, movePoint, graph.MAX_HEIGHT, Graph.VELOCITYX_STEP * k, false, Graph.movementType.JUMP);

                        //movePoint = new LevelRepresentation.Point(from + j * LevelRepresentation.PIXEL_LENGTH * 2, fromPlatform.height - 150 - GameInfo.CIRCLE_RADIUS);
                        //Trajectory(graph, fromPlatform, movePoint, graph.MAX_HEIGHT, Graph.VELOCITYX_STEP * k, true, Graph.movementType.JUMP);
                        //Trajectory(graph, fromPlatform, movePoint, graph.MAX_HEIGHT, Graph.VELOCITYX_STEP * k, false, Graph.movementType.JUMP);
                    });
                }

                else if (fromPlatform.type != Graph.platformType.COOPERATION)
                {
                    Parallel.For(0, (to - from) / (LevelRepresentation.PIXEL_LENGTH * 2) + 1, j =>
                    {
                        LevelRepresentation.Point movePoint = new LevelRepresentation.Point(from + j * LevelRepresentation.PIXEL_LENGTH * 2, fromPlatform.height - GameInfo.CIRCLE_RADIUS);
                        Trajectory(graph, fromPlatform, movePoint, graph.MAX_HEIGHT, Graph.VELOCITYX_STEP * k, true, Graph.movementType.JUMP);
                        Trajectory(graph, fromPlatform, movePoint, graph.MAX_HEIGHT, Graph.VELOCITYX_STEP * k, false, Graph.movementType.JUMP);
                    });
                }

            });
        }

        public static void Fall(Graph graph, Platform fromPlatform)
        {

            int height = Math.Min(graph.MAX_HEIGHT, GameInfo.SQUARE_HEIGHT);

            Parallel.For(0, (GameInfo.MAX_VELOCITYX / Graph.VELOCITYX_STEP), k =>
            {
                LevelRepresentation.Point movePoint = new LevelRepresentation.Point(fromPlatform.rightEdge + LevelRepresentation.PIXEL_LENGTH, fromPlatform.height - (height / 2));
                Trajectory(graph, fromPlatform, movePoint, height, Graph.VELOCITYX_STEP * k, true, Graph.movementType.FALL);

                movePoint = new LevelRepresentation.Point(fromPlatform.leftEdge - LevelRepresentation.PIXEL_LENGTH, fromPlatform.height - (height / 2));
                Trajectory(graph, fromPlatform, movePoint, height, Graph.VELOCITYX_STEP * k, false, Graph.movementType.FALL);
            });
        }

        public static void Morph(Graph graph, Platform fromPlatform)
        {
            foreach (Graph.Platform toPlatform in graph.platforms)
            {
                if (fromPlatform.Equals(toPlatform) ||
                    fromPlatform.height != toPlatform.height)
                {
                    continue;
                }

                bool rightMove;

                if (fromPlatform.rightEdge == toPlatform.leftEdge)
                {
                    rightMove = true;
                }
                else if (fromPlatform.leftEdge == toPlatform.rightEdge)
                {
                    rightMove = false;
                }
                else
                {
                    continue;
                }

                int from = rightMove ? fromPlatform.rightEdge : toPlatform.rightEdge;
                int to = rightMove ? toPlatform.leftEdge : fromPlatform.leftEdge;
              
                bool[] collectible_onPath = new bool[graph.nCollectibles];

                LevelRepresentation.Point movePoint = rightMove ? new LevelRepresentation.Point(fromPlatform.rightEdge - 100, fromPlatform.height - (toPlatform.allowedHeight / 2)) : new LevelRepresentation.Point(fromPlatform.leftEdge + 100, fromPlatform.height - (toPlatform.allowedHeight / 2));
                LevelRepresentation.Point landPoint = rightMove ? new LevelRepresentation.Point(toPlatform.rightEdge + 100, toPlatform.height - (toPlatform.allowedHeight / 2)) : new LevelRepresentation.Point(toPlatform.leftEdge - 100, toPlatform.height - (toPlatform.allowedHeight / 2));

                for (int k = from; k <= to; k += LevelRepresentation.PIXEL_LENGTH)
                {
                    List<LevelRepresentation.ArrayPoint> rectanglePixels = graph.GetFormPixels(new LevelRepresentation.Point(k, toPlatform.height - (toPlatform.allowedHeight / 2)), toPlatform.allowedHeight);
                    collectible_onPath = collectible_onPath = Utilities.GetOrMatrix(collectible_onPath, graph.GetCollectibles_onPixels(rectanglePixels));
                }

                graph.AddMove(fromPlatform, new Graph.Move(toPlatform, movePoint, landPoint, 0, rightMove, Graph.movementType.MORPH_DOWN, collectible_onPath, (fromPlatform.height - toPlatform.height) + Math.Abs(movePoint.x - landPoint.x), false, toPlatform.allowedHeight - LevelRepresentation.PIXEL_LENGTH));

            }
        }

        public static void Collect(Graph graph, Platform fromPlatform)
        {
            int from = fromPlatform.leftEdge + (fromPlatform.leftEdge - GameInfo.LEVEL_ORIGINAL) % (LevelRepresentation.PIXEL_LENGTH * 2);
            int to = fromPlatform.rightEdge - (fromPlatform.rightEdge - GameInfo.LEVEL_ORIGINAL) % (LevelRepresentation.PIXEL_LENGTH * 2);

            Parallel.For(0, (to - from) / (LevelRepresentation.PIXEL_LENGTH * 2) + 1, j =>
            {
                for (int height = graph.MIN_HEIGHT; height <= graph.MAX_HEIGHT; height += 8)
                {
                    LevelRepresentation.Point movePoint = new LevelRepresentation.Point(from + j * LevelRepresentation.PIXEL_LENGTH * 2, fromPlatform.height - (height / 2));
                    List<LevelRepresentation.ArrayPoint> pixels = graph.GetFormPixels(movePoint, height);

                    if (!graph.IsObstacle_onPixels(pixels))
                    {
                        bool[] collectible_onPath = graph.GetCollectibles_onPixels(pixels);
                        graph.AddMove(fromPlatform, new Graph.Move(fromPlatform, movePoint, movePoint, 0, true, Graph.movementType.COLLECT, collectible_onPath, 0, false, height));
                    }
                }
            });

        }

        public static void StairOrGap_Circle(Graph graph, Platform fromPlatform)
        {

            foreach (Graph.Platform toPlatform in graph.platforms)
            {

                bool rightMove = false;
                bool obstacleFlag = false;
                bool[] collectible_onPath = new bool[graph.nCollectibles];

                if (fromPlatform.Equals(toPlatform) || !graph.IsStairOrGap(fromPlatform, toPlatform, ref rightMove))
                {
                    continue;
                }

                int from = rightMove ? fromPlatform.rightEdge : toPlatform.rightEdge;
                int to = rightMove ? toPlatform.leftEdge : fromPlatform.leftEdge;


                for (int k = from; k <= to; k += LevelRepresentation.PIXEL_LENGTH)
                {
                    List<LevelRepresentation.ArrayPoint> circlePixels = Graph.GetCirclePixels(new LevelRepresentation.Point(k, toPlatform.height - GameInfo.CIRCLE_RADIUS), GameInfo.CIRCLE_RADIUS);

                    if (graph.IsObstacle_onPixels(circlePixels))
                    {
                        obstacleFlag = true;
                        break;
                    }

                    collectible_onPath = Utilities.GetOrMatrix(collectible_onPath, graph.GetCollectibles_onPixels(circlePixels));
                }

                if (!obstacleFlag)
                {
                    LevelRepresentation.Point movePoint = rightMove ? new LevelRepresentation.Point(fromPlatform.rightEdge, fromPlatform.height) : new LevelRepresentation.Point(fromPlatform.leftEdge, fromPlatform.height);
                    LevelRepresentation.Point landPoint = rightMove ? new LevelRepresentation.Point(toPlatform.leftEdge, toPlatform.height) : new LevelRepresentation.Point(toPlatform.rightEdge, toPlatform.height);
                    graph.AddMove(fromPlatform, new Graph.Move(toPlatform, movePoint, landPoint, 0, rightMove, Graph.movementType.STAIR_GAP, collectible_onPath, (fromPlatform.height - toPlatform.height) + Math.Abs(movePoint.x - landPoint.x), false));
                }
            }
        }

        public static void StairOrGap_Rectangle(Graph graph, Platform fromPlatform)
        {

            foreach (Platform toPlatform in graph.platforms)
            {

                if (fromPlatform.Equals(toPlatform))
                {
                    continue;
                }

                if (fromPlatform.type == Graph.platformType.GAP && fromPlatform.height == toPlatform.height)
                {
                    Gap_Rectangle(graph, fromPlatform, toPlatform);
                }
                else
                {
                    SmallGap_Rectangle(graph, fromPlatform, toPlatform);
                    Stair_Rectangle(graph, fromPlatform, toPlatform);
                }

            }
        }

        public static void SmallGap_Rectangle(Graph graph, Platform fromPlatform, Platform toPlatform)
        {

            bool rightMove = false;
            bool obstacleFlag = false;
            bool[] collectible_onPath = new bool[graph.nCollectibles];

            if (fromPlatform.Equals(toPlatform) || !graph.IsStairOrGap(fromPlatform, toPlatform, ref rightMove))
            {
                return;
            }

            int from = rightMove ? fromPlatform.rightEdge : toPlatform.rightEdge;
            int to = rightMove ? toPlatform.leftEdge : fromPlatform.leftEdge;

            for (int k = from; k <= to; k += LevelRepresentation.PIXEL_LENGTH)
            {
                List<LevelRepresentation.ArrayPoint> rectanglePixels = graph.GetFormPixels(new LevelRepresentation.Point(k, toPlatform.height - GameInfo.SQUARE_HEIGHT), GameInfo.SQUARE_HEIGHT);

                if (graph.IsObstacle_onPixels(rectanglePixels))
                {
                    obstacleFlag = true;
                    break;
                }

                collectible_onPath = Utilities.GetOrMatrix(collectible_onPath, graph.GetCollectibles_onPixels(rectanglePixels));
            }

            if (!obstacleFlag)
            {
                LevelRepresentation.Point movePoint = rightMove ? new LevelRepresentation.Point(fromPlatform.rightEdge, fromPlatform.height) : new LevelRepresentation.Point(fromPlatform.leftEdge, fromPlatform.height);
                LevelRepresentation.Point landPoint = rightMove ? new LevelRepresentation.Point(toPlatform.leftEdge, toPlatform.height) : new LevelRepresentation.Point(toPlatform.rightEdge, toPlatform.height);

                graph.AddMove(fromPlatform, new Move(toPlatform, movePoint, landPoint, 0, rightMove, movementType.STAIR_GAP, collectible_onPath, (fromPlatform.height - toPlatform.height) + Math.Abs(movePoint.x - landPoint.x), false));
            }
        }

        private static void Gap_Rectangle(Graph graph, Platform gap, Platform platform)
        {

            GapBridge_Rectangle(graph, gap, platform);

            bool rightMove;

            if (platform.leftEdge == gap.rightEdge + LevelRepresentation.PIXEL_LENGTH)
            {
                rightMove = false;
            }
            else if (platform.rightEdge == gap.leftEdge - LevelRepresentation.PIXEL_LENGTH)
            {
                rightMove = true;
            }
            else
            {
                return;
            }

            int gap_size = (gap.rightEdge - gap.leftEdge);
            int fall_width = Math.Min(gap_size - (2 * LevelRepresentation.PIXEL_LENGTH), GameInfo.MIN_RECTANGLE_HEIGHT);
            int fall_height = GameInfo.RECTANGLE_AREA / fall_width;
            LevelRepresentation.Point movePoint = new LevelRepresentation.Point(gap.leftEdge + (gap_size / 2), gap.height - (fall_height / 2));

            float pathLength = 0;
            LevelRepresentation.Point currentCenter = movePoint;
            bool[] collectible_onPath = new bool[graph.nCollectibles];

            for (int i = 1; true; i++)
            {
                float currentTime = i * Graph.TIME_STEP;
                LevelRepresentation.Point previousCenter = currentCenter;
                currentCenter = Graph.GetCurrentCenter(movePoint, 0, GameInfo.FALL_VELOCITYY, currentTime);
                List<LevelRepresentation.ArrayPoint> pixels = graph.GetFormPixels(currentCenter, fall_height);

                if ((currentCenter.y > movePoint.y + 16) && graph.IsObstacle_onPixels(pixels))
                {
                    Graph.Platform? reachablePlatform = graph.GetPlatform(previousCenter, fall_height);

                    if (reachablePlatform != null)
                    {
                        graph.AddMove(platform, new Graph.Move((Graph.Platform)reachablePlatform, movePoint, previousCenter, 0, rightMove, Graph.movementType.GAP, collectible_onPath, (int)pathLength, false, fall_height));
                        break;
                    }

                }

                collectible_onPath = Utilities.GetOrMatrix(collectible_onPath, graph.GetCollectibles_onPixels(pixels));
                pathLength += (float)Math.Sqrt(Math.Pow(currentCenter.x - previousCenter.x, 2) + Math.Pow(currentCenter.y - previousCenter.y, 2));

                // int new_height = GameInfo.MIN_RECTANGLE_HEIGHT;
                //            pixels = GetFormPixels(currentCenter, new_height);
                //            if ((previousCenter.y > movePoint.y + 100) && IsObstacle_onPixels(pixels))
                //            {
                //                Platform? platform = GetPlatform(previousCenter, new_height);

                //                if (platform != null)
                //                {
                //                    AddMove(gap, new Move((Platform)platform, movePoint, previousCenter, 0, true, movementType.FALL, collectible_onPath, (int)pathLength, false, new_height));
                //                }

                //            }

            }

        }

        private static void GapBridge_Rectangle(Graph graph, Platform gap, Platform platform)
        {
            bool rightMove = false;
            int start_x = 0, end_x = 0;
            bool[] collectibles = new bool[graph.nCollectibles];
            int rectangleWidth = GameInfo.RECTANGLE_AREA / gap.allowedHeight;

            if (gap.rightEdge + LevelRepresentation.PIXEL_LENGTH == platform.leftEdge)
            {
                start_x = gap.rightEdge - (rectangleWidth / 2);
                end_x = platform.leftEdge + (rectangleWidth / 2);
            }
            else if (gap.leftEdge - LevelRepresentation.PIXEL_LENGTH == platform.rightEdge)
            {
                rightMove = true;
                start_x = gap.leftEdge + (rectangleWidth / 2);
                end_x = platform.rightEdge - (rectangleWidth / 2);
            }
            else
            {
                return;
            }

            LevelRepresentation.Point start = new LevelRepresentation.Point(start_x, gap.height - (gap.allowedHeight / 2));
            LevelRepresentation.Point end = new LevelRepresentation.Point(end_x, platform.height - (platform.allowedHeight / 2));

            int pathLength = (gap.height - platform.height) + Math.Abs(start.x - end.x);
            graph.AddMove(gap, new Graph.Move(platform, start, end, 0, !rightMove, Graph.movementType.STAIR_GAP, collectibles, pathLength, false, gap.allowedHeight));

            pathLength = (platform.height - gap.height) + Math.Abs(end.x - start.x);
            graph.AddMove(platform, new Graph.Move(gap, end, start, 0, rightMove, Graph.movementType.STAIR_GAP, collectibles, pathLength, false, gap.allowedHeight));
        }

        private static void Stair_Rectangle(Graph graph, Platform fromPlatform, Platform toPlatform)
        {
            
            int stairHeight = fromPlatform.height - toPlatform.height;

            if (1 <= stairHeight && stairHeight <= 90)
            {

                if (toPlatform.leftEdge - 33 <= fromPlatform.rightEdge && fromPlatform.rightEdge <= toPlatform.leftEdge)
                {
                    LevelRepresentation.Point start = new LevelRepresentation.Point(fromPlatform.rightEdge, fromPlatform.height);
                    LevelRepresentation.Point end = new LevelRepresentation.Point(toPlatform.leftEdge, toPlatform.height);                 

                    int pathLength = (fromPlatform.height - toPlatform.height) + Math.Abs(start.x - end.x);
                    int requiredHeight = 3 * stairHeight + LevelRepresentation.PIXEL_LENGTH;

                    graph.AddMove(fromPlatform, new Graph.Move(toPlatform, start, end, 150, true, Graph.movementType.MORPH_UP, new bool[graph.nCollectibles], pathLength, false, requiredHeight));
                }
                else if (toPlatform.rightEdge <= fromPlatform.leftEdge && fromPlatform.leftEdge <= toPlatform.rightEdge + 33)
                {
                    LevelRepresentation.Point start = new LevelRepresentation.Point(fromPlatform.leftEdge, fromPlatform.height);
                    LevelRepresentation.Point end = new LevelRepresentation.Point(toPlatform.rightEdge, toPlatform.height);

                    int pathLength = (fromPlatform.height - toPlatform.height) + Math.Abs(start.x - end.x);
                    int requiredHeight = 3 * stairHeight + LevelRepresentation.PIXEL_LENGTH;

                    graph.AddMove(fromPlatform, new Graph.Move(toPlatform, start, end, 150, false, Graph.movementType.MORPH_UP, new bool[graph.nCollectibles], pathLength, false, requiredHeight));
                }
            }
        }

        private static void Trajectory(Graph graph, Platform fromPlatform, LevelRepresentation.Point movePoint, int height, int velocityX, bool rightMove, movementType movementType)
        {

            if (!graph.IsEnoughLengthToAccelerate(fromPlatform, movePoint, velocityX, rightMove))
            {
                return;
            }

            bool[] collectible_onPath = new bool[graph.nCollectibles];
            float pathLength = 0;

            LevelRepresentation.Point collidePoint = movePoint;
            LevelRepresentation.Point prevCollidePoint;

            collideType collideType = collideType.OTHER;
            float collideVelocityX = rightMove ? velocityX : -velocityX;
            float collideVelocityY = (movementType == movementType.JUMP) ? GameInfo.JUMP_VELOCITYY : GameInfo.FALL_VELOCITYY;
            bool collideCeiling = false;

            do
            {
                prevCollidePoint = collidePoint;

                graph.GetPathInfo(collidePoint, collideVelocityX, collideVelocityY, ref collidePoint, ref collideType, ref collideVelocityX, ref collideVelocityY, ref collectible_onPath, ref pathLength, (Math.Min(graph.AREA / height, height) / 2));

                if (collideType == collideType.CEILING)
                {
                    collideCeiling = true;
                }

                if (collideType == collideType.COOPERATION)
                {
                    Platform? toPlatform = graph.GetPlatform(collidePoint, height);

                    if (toPlatform.HasValue)
                    {
                        if (movementType == movementType.FALL)
                        {
                            movePoint.x = rightMove ? movePoint.x - LevelRepresentation.PIXEL_LENGTH : movePoint.x + LevelRepresentation.PIXEL_LENGTH;
                        }

                        int distance_to_land = Math.Abs(collidePoint.x - movePoint.x);

                        if (distance_to_land >= 140 || fromPlatform.id == toPlatform.Value.id)
                        {
                            graph.AddMove(fromPlatform, new Move(toPlatform.Value, movePoint, collidePoint, velocityX, rightMove, movementType, collectible_onPath, (int)pathLength, collideCeiling, height));
                        }

                    }

                }

                if (prevCollidePoint.Equals(collidePoint))
                {
                    break;
                }
            }
            while (!(collideType == collideType.FLOOR));

            if (collideType == collideType.FLOOR)
            {

                Platform? toPlatform = graph.GetPlatform(collidePoint, height);

                if (toPlatform.HasValue)
                {
                    if (movementType == movementType.FALL)
                    {
                        movePoint.x = rightMove ? movePoint.x - LevelRepresentation.PIXEL_LENGTH : movePoint.x + LevelRepresentation.PIXEL_LENGTH;
                    }

                    graph.AddMove(fromPlatform, new Move(toPlatform.Value, movePoint, collidePoint, velocityX, rightMove, movementType, collectible_onPath, (int)pathLength, collideCeiling, height));

                }
            }

        }

    }
}
