namespace EventStore.Projections
{
    using ClientAPI;

    class PipelineContext
    {
        internal ResolvedEvent Event { get; }

        internal PipelineContext(ResolvedEvent @event)
        {
            Guard.AgainstNullArgumentIfNullable(nameof(@event), @event);
            
            Event = @event;
        }
    }
}