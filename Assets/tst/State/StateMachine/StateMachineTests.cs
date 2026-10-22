using System.Collections.Generic;
using NUnit.Framework;
using EscapeThe90s.State;

public class StateMachineTests
{
    // Fake state that records every call into a shared log, so tests can check order.
    private class FakeState : IState
    {
        private readonly string name;
        private readonly List<string> log;

        public FakeState(string name, List<string> log)
        {
            this.name = name;
            this.log = log;
        }

        public void Enter() => log.Add($"{name}.Enter");
        public void Update() => log.Add($"{name}.Update");
        public void Exit() => log.Add($"{name}.Exit");
    }

    // Fake state that tries to change state from inside its own Enter().
    private class SwitchesOnEnterState : IState
    {
        public StateMachine<IState> Machine;
        public IState Next;

        // Enter() will try to change the state while the state machine is still in the middle of changing states. This should be blocked by the state machine.
        public void Enter() => Machine.ChangeState(Next);
        public void Update() { }
        public void Exit() { }
    }

    private List<string> log;
    private StateMachine<IState> machine;
    private FakeState a;
    private FakeState b;
    private int eventCount;
    private IState currentStateWhenEventFired;

    [SetUp]
    public void SetUp()
    {
        log = new List<string>();
        machine = new StateMachine<IState>();
        a = new FakeState("A", log);
        b = new FakeState("B", log);

        eventCount = 0;
        currentStateWhenEventFired = null;
        machine.OnStateChanged += RecordEvent;
    }

    // Runs every time the machine raises OnStateChanged.
    private void RecordEvent(IState newState)
    {
        eventCount++;
        currentStateWhenEventFired = machine.CurrentState;
    }

    [Test]
    public void FirstChange_EntersState()
    {
        bool changed = machine.ChangeState(a);

        Assert.IsTrue(changed);
        Assert.AreSame(a, machine.CurrentState);
        Assert.IsNull(machine.PreviousState);
        CollectionAssert.AreEqual(new[] { "A.Enter" }, log);
    }

    [Test]
    public void Change_ExitsOldThenEntersNew()
    {
        machine.ChangeState(a);
        log.Clear();

        machine.ChangeState(b);

        CollectionAssert.AreEqual(new[] { "A.Exit", "B.Enter" }, log);
        Assert.AreSame(b, machine.CurrentState);
        Assert.AreSame(a, machine.PreviousState);
    }

    [Test]
    public void Change_RaisesEventOnce_AfterStateIsSet()
    {
        machine.ChangeState(a);

        Assert.AreEqual(1, eventCount);
        Assert.AreSame(a, currentStateWhenEventFired);
    }

    [Test]
    public void RejectedChanges_DoNothing()
    {
        machine.ChangeState(a);
        eventCount = 0;
        log.Clear();

        bool nullChanged = machine.ChangeState(null);
        bool sameChanged = machine.ChangeState(a);

        Assert.IsFalse(nullChanged);
        Assert.IsFalse(sameChanged);
        Assert.AreSame(a, machine.CurrentState);
        Assert.AreEqual(0, eventCount);
        Assert.IsEmpty(log);
    }

    [Test]
    public void ChangeState_DuringEnter_IsBlocked()
    {
        SwitchesOnEnterState switchesOnEnter = new SwitchesOnEnterState { Machine = machine, Next = b };

        machine.ChangeState(switchesOnEnter);

        Assert.AreSame(switchesOnEnter, machine.CurrentState);

        // B never entered
        Assert.IsEmpty(log);
    }

    [Test]
    public void Update_RunsCurrentStateOnly()
    {
        machine.ChangeState(a);
        log.Clear();

        machine.Update();

        CollectionAssert.AreEqual(new[] { "A.Update" }, log);
    }

    [Test]
    public void Update_WithNoState_DoesNothing()
    {
        Assert.DoesNotThrow(() => machine.Update());
    }
}