using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace StateMachine
{
    public class MonoStateMachine : MonoBehaviour
    {
        public State currentState;

        public void ChangeState(State newState)
        {
            currentState?.onExit?.Invoke();
            currentState = newState;
            currentState.lastEnterTime = Time.time;
            currentState.onEnter?.Invoke();
        }

        void Update()
        {
            currentState?.onUpdate?.Invoke();
            CheckTransitions();
        }

        //void FixedUpdate()
        //{
        //    currentState?.onFixedUpdate?.Invoke();
        //    CheckTransitions();
        //}

        private void CheckTransitions()
        {
            currentState?.CheckTransitions();
        }

        public State NewState(string name)
        {
            return new State(this, name);
        }

        public string GetStateName()
        {
            return currentState?.name ?? "None";
        }
    }

    public class State
    {
        internal MonoStateMachine stateMachine;
        internal string name;
        private List<Transition> _transitions = new();
        public float lastEnterTime { get; internal set; } = 0;

        internal State(MonoStateMachine stateMachine, string name)
        {
            this.name = name;
            this.stateMachine = stateMachine;
        }

        public State AddTransition(State toState, params Condition[] condition)
        {
            Assert.IsNotNull(toState, "Can not transition to a null state. Did you initialize your states first?");
            _transitions.Add(new Transition(toState, condition));
            return this;
        }

        internal UnityEvent onEnter = new();
        internal UnityEvent onExit = new();
        internal UnityEvent onUpdate = new();
        internal UnityEvent onFixedUpdate = new();

        public State SetOnEnter(UnityAction action)
        {
            onEnter.AddListener(action);
            return this;
        }

        public State SetOnExit(UnityAction action)
        {
            onExit.AddListener(action);
            return this;
        }

        public State SetOnUpdate(UnityAction action)
        {
            onUpdate.AddListener(action);
            return this;
        }

        public State SetOnFixedUpdate(UnityAction action)
        {
            onFixedUpdate.AddListener(action);
            return this;
        }

        internal void CheckTransitions()
        {
            foreach (var transition in _transitions.Where(transition => transition.MeetsConditions(this)))
            {
                stateMachine.ChangeState(transition.GetToState());
                break;
            }
        }
    }

    public class Transition
    {
        private State toState;
        private Condition[] conditions;

        internal Transition(State toState, Condition[] conditions)
        {
            this.toState = toState;
            this.conditions = conditions;
        }

        internal bool MeetsConditions(State currentState)
        {
            return conditions.All(c => c.Evaluate(currentState));
        }

        internal State GetToState()
        {
            return toState;
        }
    }

    public class Condition
    {
        private readonly Func<State, bool> _condition;

        private Condition(Func<State, bool> condition)
        {
            this._condition = condition;
        }

        internal bool Evaluate(State state)
        {
            return _condition.Invoke(state);
        }

        // Factory

        public static Condition Immediate()
        {
            return new Condition(_ => true);
        }

        public static Condition CloseTo(Transform target, float maximumDistance)
        {
            return CloseTo(() => target.position, maximumDistance);
        }

        public static Condition CloseTo(Func<Vector3> getTarget, float maximumDistance)
        {
            return new Condition(
                state =>
                    Vector3.Distance(
                        state.stateMachine.transform.position,
                        getTarget())
                    < maximumDistance);
        }

        public static Condition AwayFrom(Transform target, float minimumDistance)
        {
            return AwayFrom(() => target.position, minimumDistance);
        }

        public static Condition AwayFrom(Func<Vector3> getTarget, float minimumDistance)
        {
            return new Condition(
                state =>
                    Vector3.Distance(
                        state.stateMachine.transform.position,
                        getTarget())
                    > minimumDistance);
        }

        public static Condition Time(float seconds)
        {
            return new Condition(state => state.lastEnterTime + seconds < UnityEngine.Time.time);
        }
    }
}
