using System.Collections.Generic;

namespace _Project.Gameplay.AI.Scripts
{
    public class BotStateMachine
    {
        private readonly List<IBotState> states;
        private IBotState currentState;

        public IBotState CurrentState => currentState;

        public BotStateMachine(List<IBotState> states)
        {
            this.states = states;
        }

        public void Update(BotSenseContext sense)
        {
            int currentIndex = currentState != null ? states.IndexOf(currentState) : int.MaxValue;

            IBotState candidate = null;
            int candidateIndex = int.MaxValue;

            for (int i = 0; i < states.Count; i++)
            {
                if (!states[i].CanEnter(sense))
                    continue;

                candidate = states[i];
                candidateIndex = i;
                break;
            }

            bool mustSwitch = currentState == null
                              || currentState.IsFinished
                              || (candidate != null && candidate != currentState && candidateIndex < currentIndex);

            if (mustSwitch)
            {
                currentState?.Exit();
                currentState = candidate;
                currentState?.Enter(sense);
            }

            if (currentState == null)
                return;

            currentState.Tick(sense);

            if (currentState.IsFinished)
            {
                currentState.Exit();
                currentState = null;
            }
        }
    }
}
