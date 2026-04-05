namespace _Project.Gameplay.AI.Scripts
{
    public interface IBotState
    {
        string Name { get; }

        bool CanEnter(BotSenseContext sense);
        void Enter(BotSenseContext sense);
        void Tick(BotSenseContext sense);
        void Exit();

        bool IsFinished { get; }
    }
}