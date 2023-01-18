using GeometryFriends.AI;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GeometryFriendsAgents
{
    class GraphCircle : Graph
    {

        bool[] platformsChecked;
        int[,] previous_levelArray;
        int obstacleColour;
        public RectangleRepresentation initialRectangleInfo;

        public GraphCircle() : base()
        {
            AREA = GameInfo.CIRCLE_AREA;
            MIN_HEIGHT = GameInfo.MIN_CIRCLE_HEIGHT;
            MAX_HEIGHT = GameInfo.MAX_CIRCLE_HEIGHT;
            obstacleColour = LevelRepresentation.GREEN;
        }

        public override void SetupPlatforms()
        {
            obstacleColour = LevelRepresentation.YELLOW;

            platforms = PlatformIdentification.SetPlatforms_Rectangle(levelArray);
            MoveIdentification.Setup_Rectangle(this);
            platforms = PlatformIdentification.DeleteUnreachablePlatforms(platforms, initialRectangleInfo);

            obstacleColour = LevelRepresentation.GREEN;

            List<Platform> platformsCircle = PlatformIdentification.SetPlatforms_Circle(levelArray);
            platforms = PlatformIdentification.JoinPlatforms(platformsCircle, platforms);
            platforms = PlatformIdentification.SetPlatformsID(platforms);

            previous_levelArray = new int[levelArray.GetLength(0), levelArray.GetLength(1)];
            Array.Copy(levelArray, previous_levelArray, levelArray.Length);
            levelArray = LevelRepresentation.SetCooperation(levelArray, platforms);

            platformsChecked = new bool[platforms.Count];
        }

        public override void SetupMoves()
        {
            foreach (Platform fromPlatform in platforms)
            {

                MoveIdentification.StairOrGap_Circle(this, fromPlatform);

                if (fromPlatform.type != platformType.GAP)
                {
                    MoveIdentification.Fall(this, fromPlatform);
                    MoveIdentification.Jump(this, fromPlatform);
                    MoveIdentification.Collect(this, fromPlatform);
                }

            }
        }

        public void SetPossibleCollectibles(CircleRepresentation initial_cI)
        {

            Platform? platform = GetPlatform(new LevelRepresentation.Point((int)initial_cI.X, (int)initial_cI.Y), GameInfo.MAX_CIRCLE_HEIGHT);

            if (platform.HasValue)
            {
                platformsChecked = CheckCollectiblesPlatform(platformsChecked, platform.Value);
            }
        }

        public override List<LevelRepresentation.ArrayPoint> GetFormPixels(LevelRepresentation.Point center, int height)
        {
            return GetCirclePixels(center, height/2);
        }

        public override bool IsObstacle_onPixels(List<LevelRepresentation.ArrayPoint> checkPixels)
        {
            if (checkPixels.Count == 0)
            {
                return true;
            }

            foreach (LevelRepresentation.ArrayPoint i in checkPixels)
            {
                if (levelArray[i.yArray, i.xArray] == LevelRepresentation.BLACK || levelArray[i.yArray, i.xArray] == obstacleColour)
                {
                    return true;
                }
            }

            return false;
        }

        protected override collideType GetCollideType(LevelRepresentation.Point center, bool ascent, bool rightMove, int radius)
        {
            LevelRepresentation.ArrayPoint centerArray = LevelRepresentation.ConvertPointIntoArrayPoint(center, false, false);
            int highestY = LevelRepresentation.ConvertValue_PointIntoArrayPoint(center.y - radius, false);
            int lowestY = LevelRepresentation.ConvertValue_PointIntoArrayPoint(center.y + radius, true);

            if (!ascent)
            {
                if (levelArray[lowestY, centerArray.xArray] == LevelRepresentation.BLACK || levelArray[lowestY, centerArray.xArray] == LevelRepresentation.GREEN)
                {
                    return collideType.FLOOR;
                }
            }
            else
            {
                if (levelArray[highestY, centerArray.xArray] == LevelRepresentation.BLACK || levelArray[lowestY, centerArray.xArray] == LevelRepresentation.GREEN)
                {
                    return collideType.CEILING;
                }
            }

            return collideType.OTHER;
        }

        public void DeleteCooperationPlatforms()
        {
            int p = 0;
            while (p < platforms.Count)
            {
                if (platforms[p].type == Graph.platformType.COOPERATION)
                {
                    platforms.Remove(platforms[p]);
                }
                else
                {
                    platforms[p].moves.Clear();
                    p++;
                }
            }

            platforms = PlatformIdentification.SetPlatformsID(platforms);
            levelArray = previous_levelArray;
            SetupMoves();
        }

        public bool[] GetCollectiblesFromCooperation(Move cooperationMove)
        {
            bool[] collectiblesWithoutCooperation = new bool[nCollectibles];
            Array.Copy(possibleCollectibles, collectiblesWithoutCooperation, possibleCollectibles.Length);

            bool[] collectibles = cooperationMove.collectibles_onPath;

            platformsChecked = CheckCollectiblesPlatform(platformsChecked, cooperationMove.reachablePlatform, true);

            collectibles = Utilities.GetOrMatrix(collectibles, Utilities.GetXorMatrix(collectiblesWithoutCooperation, possibleCollectibles));

            Array.Copy(collectiblesWithoutCooperation, possibleCollectibles, possibleCollectibles.Length);

            return collectibles;
        }

    }
}
