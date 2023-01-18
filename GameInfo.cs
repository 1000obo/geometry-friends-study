namespace GeometryFriendsAgents
{
    class GameInfo
    {
        public const int CIRCLE_RADIUS = 40;
        public const int MIN_CIRCLE_HEIGHT = 80;
        public const int MAX_CIRCLE_HEIGHT = 80;
        public const int CIRCLE_AREA = 6400;  // FAKE AREA

        public const int SQUARE_HEIGHT = 100;
        public const int MIN_RECTANGLE_HEIGHT = 50;
        public const int MAX_RECTANGLE_HEIGHT = 200;
        public const int RECTANGLE_AREA = 10000;

        public const int MAX_VELOCITYX = 200;
        public const int MAX_VELOCITYY = 20;
        public const int MIN_VELOCITYX = 10;
        public const int JUMP_VELOCITYX = 100;
        public const int MIN_DIST_RECTANGLEX = 100;
        public const int MIN_DIST_RECTANGLEY = 30;
        public const int MIN_DIST_RECTANGLEX_STEP = 160;

        public const float JUMP_VELOCITYY = 437f;
        public const float FALL_VELOCITYY = 0;
        public const float GRAVITY = 299.1f;

        public const int LEVEL_WIDTH = 1280;
        public const int LEVEL_HEIGHT = 800;
        public const int LEVEL_ORIGINAL = 8;
        public const int LEVEL_WALL_WIDTH = 40;

        public const int TIMER_WAITING_FOLLOWER = 100; //about 2-3 seconds
        public const int TIMER_WAITING_LEADER = 150; //about 3 seconds


        public const string IST_CIRCLE_PLAYING = "IST Circle Playing";
        public const string IST_RECTANGLE_PLAYING = "IST Rectangle Playing";
        public const string UNREACHABLE_PLATFORMS = "Unreachable Platforms";
        public const string AWAITING_RIDE = "Awaiting Ride";
        public const string AWAITING_MORPH = "Awaiting Morph";
        public const string RIDE_READY = "Ride Ready";
        public const string MORPH_READY = "Morph Ready";
        public const string RIDING = "Riding";
        public const string COOPERATION_FINISHED = "Cooperation Finished";
        public const string JUMPED = "Jumped";
        public const string IN_POSITION = "In Position";
        public const string INDIVIDUAL_MOVE = "Taking Individual Move";
        public const string RIDE_HELP = "Ride Help";
        public const string CLEAN_RIDES = "Clean Rides";


        public enum CooperationStatus
        {
            SINGLE_MODE, NOT_COOPERATING, AWAITING_RIDE, RIDE_READY, RIDING, AWAITING_MORPH, MORPH_READY, IN_POSITION, RIDE_HELP
        }

        public enum AgentBehaviour
        {
            LEADER, FOLLOWER
        }
    }
}
