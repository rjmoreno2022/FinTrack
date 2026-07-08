using System;

namespace FinTrack.Domain.Events;

public class GoalReachedEvent : DomainEvent
{
    public Guid GoalId { get; }
    public string GoalName { get; }

    public GoalReachedEvent(Guid goalId, string goalName)
    {
        GoalId = goalId;
        GoalName = goalName;
    }
}
