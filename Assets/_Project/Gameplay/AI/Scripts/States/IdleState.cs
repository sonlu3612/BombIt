using UnityEngine;

namespace _Project.Gameplay.AI.Scripts.States
{
    public class IdleState : IBotState
    {
        private readonly BotBlackboard blackboard;
        private readonly BotActionExecutor executor;
        private readonly BotConfig config;

        private float endTime;
        private bool finished;

        public string Name => "Idle";
        public bool IsFinished => finished;

        public IdleState(BotBlackboard blackboard, BotActionExecutor executor, BotConfig config)
        {
            this.blackboard = blackboard;
            this.executor = executor;
            this.config = config;
        }

        public bool CanEnter(BotSenseContext sense)
        {
            return !sense.IsInDanger && Random.value <= config.idleChance;
        }

        public void Enter(BotSenseContext sense)
        {
            finished = false;
            blackboard.LastStateName = Name;
            endTime = Time.time + Random.Range(config.idleDurationRange.x, config.idleDurationRange.y);
            executor.Stop();
        }

        public void Tick(BotSenseContext sense)
        {
            executor.Stop();

            if (sense.IsInDanger || Time.time >= endTime)
                finished = true;
        }

        public void Exit()
        {
            executor.Stop();
        }
    }
}
