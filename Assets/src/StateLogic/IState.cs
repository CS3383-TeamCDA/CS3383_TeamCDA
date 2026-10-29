using UnityEngine;

namespace EscapeThe90s.State
{
    public interface IState
    {
        void Enter();
        void Update();
        void Exit();
    }
}