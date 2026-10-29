using System;
using UnityEngine;

namespace EscapeThe90s.State
{
    /// <summary>
    /// A generic state machine that switches between states implementing <see cref="IState"/>.
    /// Guards against null states, re-entering the current state, and changing state during a change.
    /// Contains no transition rules; each feature's manager decides when to call <see cref="ChangeState"/>.
    /// </summary>
    /// <remarks>
    /// States are required to implement <see cref="IState"/> so the machine can call
    /// Enter, Update, and Exit without knowing any feature's concrete state types.
    /// Use by composition: a MonoBehaviour manager holds a StateMachine field,
    /// since it cannot also inherit from this class.
    /// </remarks>
    /// <typeparam name="TState">
    /// The feature's state base type (e.g. GameState, MovementState).
    /// Generic so each machine only accepts its own states and CurrentState needs no casting.
    /// </typeparam>
    /// <seealso cref="IState"/>
    public class StateMachine<TState> where TState : class, IState
    {

        #region Attributes

        public TState CurrentState { get; private set; }
        public TState PreviousState { get; private set; }
        private bool isChanging;

        #endregion

        #region Actions

        public event Action<TState> OnStateChanged;

        #endregion

        #region Methods

        /// <summary>
        /// Changes the current state.
        /// </summary>
        /// <param name="newState">The state to set as the current state.</param>
        /// <returns>true if the state changed; otherwise, false.</returns>
        public bool ChangeState(TState newState)
        {
            if (newState == null || newState == CurrentState || isChanging)
            {
                return false;
            }
            
            isChanging = true;
            try
            {
                CurrentState?.Exit();

                PreviousState = CurrentState;
                CurrentState = newState;

                CurrentState.Enter();
                OnStateChanged?.Invoke(CurrentState);
            }
            catch (Exception)
            {
                UnityEngine.Debug.LogError($"StateMachine<{typeof(TState).Name}>: Failed changing from {PreviousState?.GetType().Name ?? "null"} to {newState?.GetType().Name ?? "null"}");

                // Allows the exception to propagate after logging it, so that it can be handled by higher-level logic if necessary.
                throw;
            }
            finally
            {
                isChanging = false;
            }

            return true;
        }

        /// <summary>
        /// Updates the current state, if one is set.
        /// </summary>
        public void Update()
        {
            CurrentState?.Update();
        }

        #endregion

    }
}
