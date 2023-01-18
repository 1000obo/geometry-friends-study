using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GeometryFriendsAgents
{
    class GraphRectangle : Graph
    {

        public GraphRectangle() : base()
        {
            this.AREA = GameInfo.RECTANGLE_AREA;
            this.MIN_HEIGHT = GameInfo.MIN_RECTANGLE_HEIGHT;
            this.MAX_HEIGHT = GameInfo.MAX_RECTANGLE_HEIGHT;
        }

        public override void SetupPlatforms()
        {
            platforms = PlatformIdentification.SetPlatforms_Rectangle(levelArray);
            platforms = PlatformIdentification.SetPlatformsID(platforms);
        }

        public override void SetupMoves()
        {
             MoveIdentification.Setup_Rectangle(this);
        }

        public void SetPossibleCollectibles(RectangleRepresentation initial_rI)
        {

            bool[] platformsChecked = new bool[platforms.Count];

            Platform? platform = GetPlatform(new LevelRepresentation.Point((int)initial_rI.X, (int)initial_rI.Y), GameInfo.SQUARE_HEIGHT);

            if (platform.HasValue)
            {
                platformsChecked = CheckCollectiblesPlatform(platformsChecked, platform.Value);
            }
        }

        public override List<LevelRepresentation.ArrayPoint> GetFormPixels(LevelRepresentation.Point center, int height)
        {
            LevelRepresentation.ArrayPoint rectangleCenterArray = LevelRepresentation.ConvertPointIntoArrayPoint(center, false, false);

            int rectangleHighestY = LevelRepresentation.ConvertValue_PointIntoArrayPoint(center.y - (height / 2), false);
            int rectangleLowestY = LevelRepresentation.ConvertValue_PointIntoArrayPoint(center.y + (height / 2), true);

            float rectangleWidth = GameInfo.RECTANGLE_AREA / height;
            int rectangleLeftX = LevelRepresentation.ConvertValue_PointIntoArrayPoint((int)(center.x - (rectangleWidth / 2)), false);
            int rectangleRightX = LevelRepresentation.ConvertValue_PointIntoArrayPoint((int)(center.x + (rectangleWidth / 2)), true);

            List<LevelRepresentation.ArrayPoint> rectanglePixels = new List<LevelRepresentation.ArrayPoint>();

            for (int i = rectangleHighestY; i <= rectangleLowestY; i++)
            {
                for (int j = rectangleLeftX; j <= rectangleRightX; j++)
                {
                    rectanglePixels.Add(new LevelRepresentation.ArrayPoint(j, i));
                }
            }

            return rectanglePixels;
        }

        private bool CheckEmptyGap(Platform fromPlatform, Platform toPlatform)
        {

            int from = LevelRepresentation.ConvertValue_PointIntoArrayPoint(fromPlatform.rightEdge, false) + 1;
            int to = LevelRepresentation.ConvertValue_PointIntoArrayPoint(toPlatform.leftEdge, true);

            int y = LevelRepresentation.ConvertValue_PointIntoArrayPoint(fromPlatform.height, false);

            for (int i = from; i <= to; i++)
            {
                if (levelArray[y, i] == LevelRepresentation.BLACK)
                {
                    return false;
                }
            }

            return true;
        }

        private List<Platform> PlatformsBelow(LevelRepresentation.Point center, int height)
        {

            List<Platform> platforms = new List<Platform>();

            int rectangleWidth = GameInfo.RECTANGLE_AREA / height;
            int rectangleLeftX = center.x - (rectangleWidth / 2);
            int rectangleRightX = center.x + (rectangleWidth / 2);

            foreach (Platform i in platforms)
            {
                if (i.leftEdge <= rectangleLeftX && rectangleLeftX <= i.rightEdge && 0 <= (i.height - center.y) && (i.height - center.y) <= height)
                {
                    platforms.Add(i);
                }
                else if (i.leftEdge <= rectangleRightX && rectangleRightX <= i.rightEdge && 0 <= (i.height - center.y) && (i.height - center.y) <= height)
                {
                    platforms.Add(i);
                }
            }

            return platforms;
        }

        protected override collideType GetCollideType(LevelRepresentation.Point center, bool ascent, bool rightMove, int radius)
        {
            LevelRepresentation.ArrayPoint centerArray = LevelRepresentation.ConvertPointIntoArrayPoint(center, false, false);
            int highestY = LevelRepresentation.ConvertValue_PointIntoArrayPoint(center.y - radius, false);
            int lowestY = LevelRepresentation.ConvertValue_PointIntoArrayPoint(center.y + radius, true);

            if (!ascent)
            {
                if (levelArray[lowestY, centerArray.xArray] == LevelRepresentation.BLACK || levelArray[lowestY, centerArray.xArray] == LevelRepresentation.YELLOW)
                {
                    return collideType.FLOOR;
                }

            }
            else
            {
                if (levelArray[highestY, centerArray.xArray] == LevelRepresentation.BLACK || levelArray[lowestY, centerArray.xArray] == LevelRepresentation.YELLOW)
                {
                    return collideType.CEILING;
                }
            }

            return collideType.OTHER;
        }

        public override bool IsObstacle_onPixels(List<LevelRepresentation.ArrayPoint> checkPixels)
        {
            if (checkPixels.Count == 0)
            {
                return true;
            }

            foreach (LevelRepresentation.ArrayPoint i in checkPixels)
            {
                if (levelArray[i.yArray, i.xArray] == LevelRepresentation.BLACK || levelArray[i.yArray, i.xArray] == LevelRepresentation.YELLOW)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
