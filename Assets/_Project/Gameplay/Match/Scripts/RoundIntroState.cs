namespace _Project.Gameplay.Match.Scripts
{
    public static class RoundIntroState
    {
        public static bool IsActive { get; private set; }

        public static void BeginIntro()
        {
            IsActive = true;
        }

        public static void EndIntro()
        {
            IsActive = false;
        }
    }
}
